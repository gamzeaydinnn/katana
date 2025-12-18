using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Enums;
using Katana.Core.Interfaces;
using Katana.Core.Helpers;
using Katana.Data.Configuration;
using Katana.Data.Context;
using Katana.Business.Mappers;
using Katana.API.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace Katana.API.Controllers;





[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IKatanaService _katanaService;
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly ILucaService _lucaService;
    private readonly ISyncService _syncService;
    private readonly ILogger<ProductsController> _logger;
    private readonly ILoggingService _loggingService;
    private readonly IAuditService _auditService;
    private readonly LucaApiSettings _lucaSettings;
    private readonly IOptionsSnapshot<CatalogVisibilitySettings> _catalogVisibility;
    private readonly IntegrationDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;

    public ProductsController(
        IKatanaService katanaService,
        ILucaService lucaService,
        ISyncService syncService,
        IProductService productService,
        ICategoryService categoryService,
        IOptionsSnapshot<LucaApiSettings> lucaSettings,
        IOptionsSnapshot<CatalogVisibilitySettings> catalogVisibility,
        ILogger<ProductsController> logger,
        ILoggingService loggingService,
        IAuditService auditService,
        IntegrationDbContext context,
        IHubContext<NotificationHub> hubContext)
    {
        _katanaService = katanaService;
        _lucaService = lucaService;
        _syncService = syncService;
        _productService = productService;
        _categoryService = categoryService;
        _lucaSettings = lucaSettings.Value;
        _catalogVisibility = catalogVisibility;
        _logger = logger;
        _loggingService = loggingService;
        _auditService = auditService;
        _context = context;
        _hubContext = hubContext;
    }

    
    
    

    
    
    
    [HttpGet("katana")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetKatanaProducts([FromQuery] int? page = null, [FromQuery] int? pageSize = null, [FromQuery] bool sync = false)
    {
        try
        {
            _loggingService.LogInfo("Fetching products from Katana API", User?.Identity?.Name, $"Page: {page}, PageSize: {pageSize}, Sync: {sync}", LogCategory.ExternalAPI);
            
            List<KatanaProductDto> allProducts;
            try
            {
                allProducts = await _katanaService.GetProductsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch products from Katana API, falling back to local database");
                allProducts = new List<KatanaProductDto>();
            }
            
            // Eğer Katana API boş dönüyorsa, local database'den çek
            if (allProducts == null || allProducts.Count == 0)
            {
                _logger.LogInformation("Katana API returned no products, fetching from local database");
                var dbProducts = await _productService.GetAllProductsAsync();
                
                // Local product'ları KatanaProductDto formatına çevir
                allProducts = dbProducts.Select(p => new KatanaProductDto
                {
                    Id = p.Id.ToString(),
                    SKU = p.SKU,
                    Name = p.Name,
                    SalesPrice = p.Price,
                    OnHand = (int)p.Stock,
                    Category = p.CategoryId.ToString()
                }).ToList();
                
                _logger.LogInformation("Retrieved {Count} products from local database", allProducts.Count);
            }
            
            
            var products = allProducts
                .GroupBy(p => p.SKU)
                .Select(g => g.First())
                .ToList();
            
            if (products.Count < allProducts.Count)
            {
                _logger.LogWarning($"Removed {allProducts.Count - products.Count} duplicate products from Katana API response");
            }
            
            var pageNumber = page.GetValueOrDefault(1);
            if (pageNumber < 1) pageNumber = 1;
            var pageSizeValue = pageSize.GetValueOrDefault(50);
            if (pageSizeValue <= 0) pageSizeValue = 50;
            
            
            if (sync)
            {
                
                var categories = await _categoryService.GetAllAsync();
                int defaultCategoryId;
                
                if (categories == null || !categories.Any())
                {
                    
                    var newCategory = await _categoryService.CreateAsync(new CreateCategoryDto
                    {
                        Name = "Genel",
                        Description = "Varsayılan kategori"
                    });
                    defaultCategoryId = newCategory.Id;
                    _logger.LogInformation("Created default category with ID: {CategoryId}", defaultCategoryId);
                }
                else
                {
                    defaultCategoryId = categories.First().Id;
                }
                
                
                var allLocalProducts = await _productService.GetAllProductsAsync();
                var localProductsBySku = allLocalProducts.ToDictionary(p => p.SKU, p => p);
                
                
                var productsToSync = new List<CreateProductDto>();
                foreach (var katanaProduct in products)
                {
                    if (!localProductsBySku.ContainsKey(katanaProduct.SKU))
                    {
                        productsToSync.Add(new CreateProductDto
                        {
                            Name = katanaProduct.Name ?? katanaProduct.SKU,
                            SKU = katanaProduct.SKU,
                            Price = katanaProduct.SalesPrice ?? 0,
                            Stock = katanaProduct.OnHand ?? 0,
                            CategoryId = defaultCategoryId
                        });
                    }
                }
                
                
                var (created, updated, skipped, syncErrors) = await _productService.BulkSyncProductsAsync(
                    productsToSync, 
                    defaultCategoryId
                );
                
                if (syncErrors.Any())
                {
                    _logger.LogWarning("Sync completed with {ErrorCount} errors: {Errors}", 
                        syncErrors.Count, string.Join("; ", syncErrors));
                }
                
                _logger.LogInformation("Sync completed: {Created} created, {Updated} updated, {Skipped} skipped", 
                    created, updated, skipped);
                
                
                allLocalProducts = await _productService.GetAllProductsAsync();
                localProductsBySku = allLocalProducts.ToDictionary(p => p.SKU, p => p);
                
                
                var enrichedProducts = new List<object>();
                foreach (var katanaProduct in products)
                {
                    var localProduct = localProductsBySku.GetValueOrDefault(katanaProduct.SKU);
                    enrichedProducts.Add(new
                    {
                        id = localProduct?.Id.ToString() ?? katanaProduct.Id,
                        katanaId = katanaProduct.Id,
                        sku = localProduct?.SKU ?? katanaProduct.SKU,
                        name = localProduct?.Name ?? katanaProduct.Name,
                        category = katanaProduct.Category,
                        unit = katanaProduct.Unit,
                        inStock = katanaProduct.InStock,
                        committed = katanaProduct.Committed,
                        available = katanaProduct.Available,
                        onHand = localProduct?.Stock ?? katanaProduct.OnHand,
                        salesPrice = localProduct?.Price ?? katanaProduct.SalesPrice,
                        costPrice = katanaProduct.CostPrice,
                        isActive = localProduct?.IsActive ?? katanaProduct.IsActive
                    });
                }

                var total = enrichedProducts.Count;
                var paged = enrichedProducts
                    .Skip((pageNumber - 1) * pageSizeValue)
                    .Take(pageSizeValue)
                    .ToList();
                
                return Ok(new 
                { 
                    data = paged, 
                    count = total, 
                    page = pageNumber,
                    pageSize = pageSizeValue,
                    totalPages = (int)Math.Ceiling(total / (double)pageSizeValue),
                    sync = new 
                    { 
                        created, 
                        updated, 
                        skipped, 
                        errors = syncErrors.Count > 0 ? syncErrors : null 
                    } 
                });
            }
            
            // Tüm Katana ürünlerini göster (local'de olsun olmasın)
            var localProducts = await _productService.GetAllProductsAsync();
            var localProductsMap = localProducts.ToDictionary(p => p.SKU, p => p);
            
            var result = new List<object>();
            foreach (var katanaProduct in products)
            {
                var localProduct = localProductsMap.GetValueOrDefault(katanaProduct.SKU);
                
                // Local'de varsa local verileri kullan, yoksa Katana verilerini göster
                result.Add(new
                {
                    id = localProduct?.Id.ToString() ?? katanaProduct.Id,
                    katanaId = katanaProduct.Id,
                    sku = localProduct?.SKU ?? katanaProduct.SKU,
                    name = localProduct?.Name ?? katanaProduct.Name,
                    category = katanaProduct.Category,
                    unit = katanaProduct.Unit,
                    inStock = katanaProduct.InStock,
                    committed = katanaProduct.Committed,
                    available = katanaProduct.Available,
                    onHand = localProduct?.Stock ?? katanaProduct.OnHand,
                    salesPrice = localProduct?.Price ?? katanaProduct.SalesPrice,
                    costPrice = katanaProduct.CostPrice,
                    isActive = localProduct?.IsActive ?? katanaProduct.IsActive,
                    syncedToLocal = localProduct != null
                });
            }
            
            var totalCount = result.Count;
            var pagedResult = result
                .Skip((pageNumber - 1) * pageSizeValue)
                .Take(pageSizeValue)
                .ToList();
            
            return Ok(new 
            { 
                data = pagedResult, 
                count = totalCount,
                page = pageNumber,
                pageSize = pageSizeValue,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSizeValue)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching products from Katana API");
            _loggingService.LogError("Katana products fetch failed", ex, User?.Identity?.Name, null, LogCategory.ExternalAPI);
            return StatusCode(500, new { error = "Failed to fetch products from Katana API", details = ex.Message });
        }
    }

    
    
    
    
    
    [HttpGet("luca")]
    [HttpGet("~/api/Luca/products")] 
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetLucaStyleProducts()
    {
        try
        {
            var local = await _productService.GetAllProductsAsync();
            var mapped = local.Select(p => new
            {
                id = p.Id,
                productCode = p.SKU,
                productName = p.Name,
                unit = "Adet",
                quantity = p.Stock,
                unitPrice = p.Price,
                vatRate = 20,
                isActive = p.IsActive
            }).ToList();

            
            if (mapped.Count == 0)
            {
                var demo = GetDemoLucaProducts();
                return Ok(new { data = demo, count = demo.Count });
            }

            return Ok(new { data = mapped, count = mapped.Count });
        }
        catch (Exception ex)
        {
            
            _logger.LogError(ex, "Error creating Luca-style product list from local DB. Serving demo data.");
            var demo = GetDemoLucaProducts();
            return Ok(new { data = demo, count = demo.Count });
        }
    }

    private static List<object> GetDemoLucaProducts()
    {
        return new List<object>
        {
            new { id = 1001, productCode = "SKU-1001", productName = "Demo Vida 5mm", unit = "Adet", quantity = 150, unitPrice = 1.25m, vatRate = 20, isActive = true },
            new { id = 1002, productCode = "SKU-1002", productName = "Demo Somun 10mm", unit = "Adet", quantity = 80, unitPrice = 2.90m, vatRate = 20, isActive = true },
            new { id = 1003, productCode = "SKU-1003", productName = "Demo Pul 8mm", unit = "Adet", quantity = 0, unitPrice = 0.75m, vatRate = 20, isActive = false },
            new { id = 1004, productCode = "SKU-1004", productName = "Demo Çelik Profil", unit = "Adet", quantity = 22, unitPrice = 75.00m, vatRate = 20, isActive = true },
            new { id = 1005, productCode = "SKU-1005", productName = "Demo Alüminyum Levha", unit = "Adet", quantity = 45, unitPrice = 120.50m, vatRate = 20, isActive = true }
        };
    }

    
    [HttpPost("sync-products")]
    public async Task<IActionResult> SyncProductsToLuca([FromBody] SyncOptionsDto options)
    {
        try
        {
            var result = await _syncService.SyncProductsToLucaAsync(options ?? new SyncOptionsDto());
            return Ok(result);
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Sync failed", ex, User?.Identity?.Name, null, LogCategory.ExternalAPI);
            _logger.LogError(ex, "SyncProductsToLuca failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("~/api/Luca/stock-cards")]
    public async Task<IActionResult> GetLucaStockCards()
    {
        try
        {
            var cards = await _lucaService.ListStockCardsAsync(CancellationToken.None);
            _logger.LogInformation("[CONTROLLER] Frontend'e {Count} adet stok kartı gönderiliyor.", cards?.Count ?? 0);
            return Ok(cards);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CONTROLLER] Stok kartları çekilirken hata oldu.");
            return StatusCode(500, "Luca bağlantı hatası.");
        }
    }

    [HttpGet("~/api/Sync/comparison")]
    public async Task<IActionResult> GetStockComparison()
    {
        var comparison = await _syncService.CompareStockCardsAsync();
        return Ok(comparison);
    }

    
    
    [HttpGet("katana/{sku}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetKatanaProduct(string sku)
    {
        try
        {
            var product = await _katanaService.GetProductBySkuAsync(sku);
            
            if (product == null)
            {
                return NotFound(new { error = $"Product with SKU {sku} not found" });
            }
            
            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching product {ProductSku} from Katana API", sku);
            return StatusCode(500, new { error = "Failed to fetch product from Katana API", details = ex.Message });
        }
    }

    
    
    

    [HttpGet]
    [AllowAnonymous]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any, NoStore = false)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll()
    {
        _loggingService.LogInfo("Products listed", User?.Identity?.Name, null, LogCategory.UserAction);
        var products = await _productService.GetAllProductsAsync();
        return Ok(products);
    }

    
    
    
    
    [HttpGet("catalog")]
    [AllowAnonymous]
    public async Task<ActionResult<CustomerCatalogResponse>> GetCustomerCatalog([FromQuery] bool? hideZeroStockProducts = null)
    {
        var products = (await _productService.GetAllProductsAsync()).ToList();
        var categories = (await _categoryService.GetAllAsync())?.ToList() ?? new List<CategoryDto>();
        var publishedCategoryIds = new HashSet<int>(categories.Where(c => c.IsActive).Select(c => c.Id));

        var appliedHideZeroFlag = hideZeroStockProducts
            ?? SettingsController.GetCachedSettings()?.HideZeroStockProducts
            ?? _catalogVisibility.Value.HideZeroStockProducts;

        var visibleProducts = new List<ProductDto>();
        var hiddenCount = 0;

        foreach (var product in products)
        {
            var visibility = EvaluateCatalogVisibility(product, publishedCategoryIds, appliedHideZeroFlag);
            if (visibility.isVisible)
            {
                visibleProducts.Add(product);
            }
            else
            {
                hiddenCount++;
                _logger.LogDebug("Customer catalog skipped SKU {Sku}: {Reasons}", product.SKU, string.Join(", ", visibility.reasons));
            }
        }

        var response = new CustomerCatalogResponse
        {
            Data = visibleProducts,
            Total = visibleProducts.Count,
            HiddenCount = hiddenCount,
            Filters = new CatalogFilterMetadata
            {
                HideZeroStockProducts = appliedHideZeroFlag,
                RequirePublishedCategory = true,
                RequireActiveStatus = true
            }
        };

        return Ok(response);
    }

    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ProductSummaryDto>>> GetActive()
    {
        var products = await _productService.GetActiveProductsAsync();
        return Ok(products);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductDto>> GetById(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null)
            return NotFound($"Ürün bulunamadı: {id}");

        return Ok(product);
    }

    [HttpGet("by-sku/{sku}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductDto>> GetBySku(string sku)
    {
        var product = await _productService.GetProductBySkuAsync(sku);
        if (product == null)
            return NotFound($"SKU bulunamadı: {sku}");

        return Ok(product);
    }

    [HttpGet("category/{categoryId}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetByCategory(int categoryId)
    {
        var products = await _productService.GetProductsByCategoryAsync(categoryId);
        return Ok(products);
    }

    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ProductDto>>> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest("Arama terimi boş olamaz");

        var products = await _productService.SearchProductsAsync(q);
        return Ok(products);
    }

    [HttpGet("low-stock")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetLowStock([FromQuery] int threshold = 10)
    {
        var products = await _productService.GetLowStockProductsAsync(threshold);
        return Ok(products);
    }

    [HttpGet("out-of-stock")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetOutOfStock()
    {
        var products = await _productService.GetOutOfStockProductsAsync();
        return Ok(products);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,StokYonetici")] 
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto dto)
    {
        var validationErrors = Katana.Business.Validators.ProductValidator.ValidateCreate(dto);
        if (validationErrors.Any())
            return BadRequest(new { errors = validationErrors });

        try
        {
            var product = await _productService.CreateProductAsync(dto);
            _auditService.LogCreate("Product", product.Id.ToString(), User?.Identity?.Name ?? "system", 
                $"SKU: {product.SKU}, Name: {product.Name}");
            _loggingService.LogInfo($"Product created: {product.SKU}", User?.Identity?.Name, null, LogCategory.UserAction);

            // 🔔 Yeni ürün bildirimi oluştur
            try
            {
                var notification = new Notification
                {
                    Type = "ProductCreated",
                    Title = "Yeni Ürün Eklendi",
                    Payload = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        productId = product.Id,
                        sku = product.SKU,
                        name = product.Name,
                        stock = product.Stock,
                        price = product.Price
                    }),
                    Link = $"/products/{product.Id}",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // SignalR ile bildirim gönder
                await _hubContext.Clients.All.SendAsync("ProductCreated", new
                {
                    productId = product.Id,
                    sku = product.SKU,
                    name = product.Name,
                    stock = product.Stock,
                    message = $"Yeni ürün eklendi: {product.Name} ({product.SKU})"
                });
                _logger.LogInformation("🔔 Yeni ürün bildirimi gönderildi: {SKU}", product.SKU);
            }
            catch (Exception notifEx)
            {
                _logger.LogWarning(notifEx, "Bildirim oluşturulurken hata: {SKU}", product.SKU);
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("[AUTO-SYNC] Product created, sending stock card to Luca. SKU={Sku}, Id={Id}", product.SKU, product.Id);

                    var entity = new Product
                    {
                        Id = product.Id,
                        SKU = product.SKU,
                        Name = product.Name,
                        CategoryId = product.CategoryId,
                        Description = product.Description,
                        Price = product.Price,
                        StockSnapshot = product.Stock,
                        MainImageUrl = product.MainImageUrl,
                        IsActive = product.IsActive,
                        CreatedAt = product.CreatedAt ?? DateTime.UtcNow,
                        UpdatedAt = product.UpdatedAt ?? DateTime.UtcNow
                    };

                    var stockCard = KatanaToLucaMapper.MapProductToStockCard(
                        entity,
                        defaultVat: _lucaSettings.DefaultKdvOran,
                        defaultOlcumBirimiId: _lucaSettings.DefaultOlcumBirimiId,
                        defaultKartTipi: _lucaSettings.DefaultKartTipi,
                        defaultKategoriKod: _lucaSettings.DefaultKategoriKodu
                    );

                    KatanaToLucaMapper.ValidateLucaStockCard(stockCard);

                    var sendResult = await _lucaService.SendStockCardsAsync(new List<LucaCreateStokKartiRequest> { stockCard });

                    if (!sendResult.IsSuccess)
                    {
                        _logger.LogError("[AUTO-SYNC ERROR] Luca stock card sync failed for SKU={Sku}. Processed={Processed}, Success={Success}, Failed={Failed}, Errors={Errors}",
                            product.SKU,
                            sendResult.ProcessedRecords,
                            sendResult.SuccessfulRecords,
                            sendResult.FailedRecords,
                            string.Join("; ", sendResult.Errors ?? new List<string>()));
                    }
                    else
                    {
                        _logger.LogInformation("[AUTO-SYNC SUCCESS] Luca stock card created for SKU={Sku}. Processed={Processed}, Success={Success}",
                            product.SKU,
                            sendResult.ProcessedRecords,
                            sendResult.SuccessfulRecords);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[AUTO-SYNC EXCEPTION] Error while sending stock card to Luca for SKU={Sku}", product.SKU);
                }
            });

            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }
        catch (InvalidOperationException ex)
        {
            _loggingService.LogError("Product creation failed", ex, User?.Identity?.Name, null, LogCategory.Business);
            return Conflict(ex.Message);
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,StokYonetici")] 
    public async Task<ActionResult<ProductDto>> Update(int id, [FromBody] UpdateProductDto dto)
    {
        _logger.LogInformation("Update product called: ID={Id}, DTO={@Dto}", id, dto);
        
        if (dto == null)
        {
            _logger.LogWarning("Update received null DTO for product {Id}", id);
            return BadRequest(new { error = "Ürün verisi boş olamaz" });
        }

        var validationErrors = Katana.Business.Validators.ProductValidator.ValidateUpdate(dto);
        if (validationErrors.Any())
        {
            _logger.LogWarning("Validation failed for product {Id}: {Errors}", id, string.Join(", ", validationErrors));
            return BadRequest(new { errors = validationErrors });
        }

        try
        {
            
            var product = await _productService.UpdateProductAsync(id, dto);
            
            
            var katanaProduct = await _katanaService.GetProductBySkuAsync(product.SKU);
            if (katanaProduct != null && int.TryParse(katanaProduct.Id, out int katanaProductId))
            {
                var katanaUpdated = await _katanaService.UpdateProductAsync(
                    katanaProductId, 
                    dto.Name, 
                    dto.Price, 
                    dto.Stock
                );
                
                if (katanaUpdated)
                {
                    _logger.LogInformation("Product {SKU} updated in both local DB and Katana API", product.SKU);
                }
                else
                {
                    _logger.LogWarning("Product {SKU} updated in local DB but failed to update in Katana API", product.SKU);
                }
            }
            
            // 🔄 AUTO-SYNC: Luca'ya da gönder (arka planda)
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("[AUTO-SYNC UPDATE] Product updated, syncing to Luca. SKU={Sku}, Id={Id}", product.SKU, product.Id);

                    var entity = new Product
                    {
                        Id = product.Id,
                        SKU = product.SKU,
                        Name = product.Name,
                        CategoryId = product.CategoryId,
                        Description = product.Description,
                        Price = product.Price,
                        StockSnapshot = product.Stock,
                        MainImageUrl = product.MainImageUrl,
                        IsActive = product.IsActive,
                        CreatedAt = product.CreatedAt ?? DateTime.UtcNow,
                        UpdatedAt = product.UpdatedAt ?? DateTime.UtcNow
                    };

                    var stockCard = KatanaToLucaMapper.MapProductToStockCard(
                        entity,
                        defaultVat: _lucaSettings.DefaultKdvOran,
                        defaultOlcumBirimiId: _lucaSettings.DefaultOlcumBirimiId,
                        defaultKartTipi: _lucaSettings.DefaultKartTipi,
                        defaultKategoriKod: _lucaSettings.DefaultKategoriKodu
                    );

                    KatanaToLucaMapper.ValidateLucaStockCard(stockCard);

                    var sendResult = await _lucaService.SendStockCardsAsync(new List<LucaCreateStokKartiRequest> { stockCard });

                    if (!sendResult.IsSuccess)
                    {
                        _logger.LogError("[AUTO-SYNC UPDATE ERROR] Luca stock card sync failed for SKU={Sku}. Errors={Errors}",
                            product.SKU,
                            string.Join("; ", sendResult.Errors ?? new List<string>()));
                    }
                    else
                    {
                        _logger.LogInformation("[AUTO-SYNC UPDATE SUCCESS] Luca stock card synced for SKU={Sku}. Success={Success}, Duplicate={Duplicate}",
                            product.SKU,
                            sendResult.SuccessfulRecords,
                            sendResult.DuplicateRecords);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[AUTO-SYNC UPDATE EXCEPTION] Error while syncing to Luca for SKU={Sku}", product.SKU);
                }
            });
            
            _auditService.LogUpdate("Product", id.ToString(), User?.Identity?.Name ?? "system", null, 
                $"Updated: {product.SKU}");
            _loggingService.LogInfo($"Product updated successfully: {id}", User?.Identity?.Name, null, LogCategory.UserAction);
            return Ok(product);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex, "Product {Id} not found during update", id);
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation updating product {Id}", id);
            _loggingService.LogError("Product update failed", ex, User?.Identity?.Name, null, LogCategory.Business);
            
            
            if (ex.Message.Contains("kategori") || ex.Message.Contains("CategoryId"))
            {
                return BadRequest(new { error = ex.Message, details = ex.InnerException?.Message });
            }
            
            return Conflict(new { error = ex.Message, details = ex.InnerException?.Message });
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _logger.LogError(ex, "Database update error for product {Id}. InnerException: {InnerException}", 
                id, ex.InnerException?.Message);
            _loggingService.LogError("Product update DB error", ex, User?.Identity?.Name, null, LogCategory.Business);
            
            
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            if (innerMessage.Contains("FK_Products_Categories_CategoryId") || 
                innerMessage.Contains("FOREIGN KEY constraint"))
            {
                return BadRequest(new 
                { 
                    error = "Geçersiz kategori ID'si. Bu kategori veritabanında mevcut değil.",
                    message = "Invalid CategoryId or category does not exist",
                    details = innerMessage
                });
            }
            
            return StatusCode(500, new 
            { 
                message = "Veritabanı güncelleme hatası", 
                error = ex.Message,
                details = innerMessage,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating product {Id}. DTO: {@Dto}, InnerException: {InnerException}", 
                id, dto, ex.InnerException?.Message);
            _loggingService.LogError("Product update failed", ex, User?.Identity?.Name, null, LogCategory.Business);
            return StatusCode(500, new 
            { 
                message = "Ürün güncellenirken bir hata oluştu", 
                error = ex.Message,
                details = ex.InnerException?.Message ?? ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpPatch("{id}/stock")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> UpdateStock(int id, [FromBody] int quantity)
    {
        var validationErrors = Katana.Business.Validators.ProductValidator.ValidateStock(quantity);
        if (validationErrors.Any())
            return BadRequest(new { errors = validationErrors });

        var result = await _productService.UpdateStockAsync(id, quantity);
        if (!result)
            return NotFound($"Ürün bulunamadı: {id}");

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(int id, [FromQuery] bool force = false)
    {
        try
        {
            var result = await _productService.DeleteProductAsync(id, force);
            if (!result)
                return NotFound($"Ürün bulunamadı: {id}");

            _auditService.LogDelete("Product", id.ToString(), User?.Identity?.Name ?? "system", null);
            _loggingService.LogInfo($"Product deleted: {id}", User?.Identity?.Name, null, LogCategory.UserAction);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _loggingService.LogError("Product deletion failed", ex, User?.Identity?.Name, null, LogCategory.Business);
            return Conflict(ex.Message);
        }
    }

    
    
    
    [HttpPut("luca/{id}")]
    [Authorize(Roles = "Admin,StokYonetici")] 
    public async Task<ActionResult> UpdateLucaProduct(int id, [FromBody] LucaProductUpdateDto dto)
    {
        _logger.LogInformation("UpdateLucaProduct called: ID={Id}, DTO={@Dto}", id, dto);
        
        if (dto == null)
        {
            _logger.LogWarning("UpdateLucaProduct received null DTO for product {Id}", id);
            return BadRequest(new { error = "Ürün verisi boş olamaz" });
        }

        
        if (string.IsNullOrWhiteSpace(dto.ProductName))
        {
            _logger.LogWarning("UpdateLucaProduct missing ProductName for product {Id}", id);
            return BadRequest(new { error = "Ürün adı gereklidir" });
        }

        if (string.IsNullOrWhiteSpace(dto.ProductCode))
        {
            _logger.LogWarning("UpdateLucaProduct missing ProductCode for product {Id}", id);
            return BadRequest(new { error = "Ürün kodu gereklidir" });
        }

        try
        {
            
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                _logger.LogWarning("UpdateLucaProduct: Product {Id} not found", id);
                return NotFound(new { error = $"Ürün bulunamadı: {id}" });
            }

            _logger.LogInformation("Existing product found: {@Product}", product);

            
            int validCategoryId = product.CategoryId;
            if (validCategoryId <= 0)
            {
                _logger.LogWarning("Product {Id} has invalid CategoryId {CategoryId}, fetching default", id, validCategoryId);
                var categories = await _categoryService.GetAllAsync();
                if (categories != null && categories.Any())
                {
                    validCategoryId = categories.First().Id;
                }
                else
                {
                    
                    _logger.LogWarning("No categories found, creating default category");
                    var newCategory = await _categoryService.CreateAsync(new CreateCategoryDto
                    {
                        Name = "Genel",
                        Description = "Varsayılan kategori"
                    });
                    validCategoryId = newCategory.Id;
                }
            }

            
            var updateDto = new UpdateProductDto
            {
                Name = dto.ProductName,
                SKU = dto.ProductCode,
                Price = dto.UnitPrice,
                Stock = dto.Quantity,
                CategoryId = validCategoryId,
                IsActive = true
            };

            _logger.LogInformation("Mapped to UpdateProductDto: {@UpdateDto}", updateDto);

            
            var validationErrors = Katana.Business.Validators.ProductValidator.ValidateUpdate(updateDto);
            if (validationErrors.Any())
            {
                _logger.LogWarning("Validation failed for product {Id}: {Errors}", id, string.Join(", ", validationErrors));
                return BadRequest(new { errors = validationErrors });
            }

            
            var updatedProduct = await _productService.UpdateProductAsync(id, updateDto);

            
            var result = new
            {
                id = updatedProduct.Id,
                productCode = updatedProduct.SKU,
                productName = updatedProduct.Name,
                unit = dto.Unit ?? "Adet",
                quantity = updatedProduct.Stock,
                unitPrice = updatedProduct.Price,
                vatRate = dto.VatRate ?? 20,
                isActive = updatedProduct.IsActive
            };

            _auditService.LogUpdate("Product (Luca)", id.ToString(), User?.Identity?.Name ?? "system", null,
                $"Updated: {updatedProduct.SKU}");
            _loggingService.LogInfo($"Luca product updated successfully: {id}", User?.Identity?.Name, null, LogCategory.UserAction);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex, "Product {Id} not found during update", id);
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation updating product {Id}", id);
            _loggingService.LogError("Luca product update failed", ex, User?.Identity?.Name, null, LogCategory.Business);
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating Luca product {ProductId}. DTO: {@Dto}", id, dto);
            return StatusCode(500, new { error = "Ürün güncelleme sırasında bir hata oluştu", details = ex.Message });
        }
    }

    
    
    
    [HttpPost("luca/{id}/push")]
    [Authorize(Roles = "Admin,StokYonetici")]
    public async Task<ActionResult> PushProductToLuca(int id)
    {
        var productDto = await _productService.GetProductByIdAsync(id);
        if (productDto == null)
        {
            return NotFound(new { error = $"Ürün bulunamadı: {id}" });
        }

        
        var product = new Product
        {
            Id = productDto.Id,
            SKU = productDto.SKU,
            Name = productDto.Name,
            CategoryId = productDto.CategoryId,
            Description = productDto.Description,
            Price = productDto.Price,
            StockSnapshot = productDto.Stock,
            MainImageUrl = productDto.MainImageUrl,
            IsActive = productDto.IsActive,
            CreatedAt = productDto.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = productDto.UpdatedAt ?? DateTime.UtcNow
        };

        var lucaStockCard = MappingHelper.MapToLucaStockCard(product);
        var result = await _lucaService.SendStockCardsAsync(new List<LucaCreateStokKartiRequest> { lucaStockCard });

        if (!result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                error = "Luca'ya gönderim başarısız",
                details = result.Errors,
                processed = result.ProcessedRecords,
                success = result.SuccessfulRecords,
                failed = result.FailedRecords
            });
        }

        _auditService.LogSync("LucaProductPush", User?.Identity?.Name ?? "system", $"SKU: {product.SKU}");
        _loggingService.LogInfo($"Luca product push succeeded for {product.SKU}", User?.Identity?.Name, null, LogCategory.ExternalAPI);

        return Ok(new
        {
            message = "Luca'ya gönderildi",
            processed = result.ProcessedRecords,
            success = result.SuccessfulRecords,
            failed = result.FailedRecords
        });
    }

    [HttpPut("{id}/activate")]
    [Authorize(Roles = "Admin,StokYonetici")] 
    public async Task<ActionResult> Activate(int id)
    {
        var result = await _productService.ActivateProductAsync(id);
        if (!result)
            return NotFound($"Ürün bulunamadı: {id}");

        return NoContent();
    }

    [HttpPut("{id}/deactivate")]
    [Authorize(Roles = "Admin,StokYonetici")] 
    public async Task<ActionResult> Deactivate(int id)
    {
        var result = await _productService.DeactivateProductAsync(id);
        if (!result)
            return NotFound($"Ürün bulunamadı: {id}");

        return NoContent();
    }

    [HttpGet("statistics")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductStatisticsDto>> GetStatistics()
    {
        var stats = await _productService.GetProductStatisticsAsync();
        return Ok(stats);
    }

    private static (bool isVisible, List<string> reasons) EvaluateCatalogVisibility(
        ProductDto product,
        HashSet<int> publishedCategoryIds,
        bool hideZeroStockProducts)
    {
        var reasons = new List<string>();

        if (product.CategoryId <= 0 || !publishedCategoryIds.Contains(product.CategoryId))
        {
            reasons.Add("CATEGORY_NOT_PUBLISHED");
        }

        if (!product.IsActive)
        {
            reasons.Add("INACTIVE_STATUS");
        }

        if (hideZeroStockProducts && product.Stock <= 0)
        {
            reasons.Add("ZERO_STOCK");
        }

        return (reasons.Count == 0, reasons);
    }
}

public class CustomerCatalogResponse
{
    public List<ProductDto> Data { get; set; } = new();
    public int Total { get; set; }
    public int HiddenCount { get; set; }
    public CatalogFilterMetadata Filters { get; set; } = new();
}

public class CatalogFilterMetadata
{
    public bool HideZeroStockProducts { get; set; }
    public bool RequirePublishedCategory { get; set; }
    public bool RequireActiveStatus { get; set; }
}
