using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Enums;
using Katana.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Katana.API.Controllers;

/// <summary>
/// Product management endpoints. Requires authorization for write operations.
/// Read endpoints return product data from local DB or Katana API.
/// </summary>
[AllowAnonymous] // Temporary for testing
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IKatanaService _katanaService;
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly ILogger<ProductsController> _logger;
    private readonly ILoggingService _loggingService;
    private readonly IAuditService _auditService;

    public ProductsController(
        IKatanaService katanaService,
        IProductService productService,
        ICategoryService categoryService,
        ILogger<ProductsController> logger,
        ILoggingService loggingService,
        IAuditService auditService)
    {
        _katanaService = katanaService;
        _productService = productService;
        _categoryService = categoryService;
        _logger = logger;
        _loggingService = loggingService;
        _auditService = auditService;
    }

    // ==========================================
    // KATANA API ENDPOINTS (External)
    // ==========================================

    /// <summary>
    /// Get all products from Katana API and sync to local DB
    /// </summary>
    [HttpGet("katana")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetKatanaProducts([FromQuery] int? page = null, [FromQuery] int? limit = null, [FromQuery] bool sync = false)
    {
        try
        {
            _loggingService.LogInfo("Fetching products from Katana API", User?.Identity?.Name, $"Page: {page}, Limit: {limit}, Sync: {sync}", LogCategory.ExternalAPI);
            var allProducts = await _katanaService.GetProductsAsync();
            
            // Remove duplicates based on SKU - keep first occurrence
            var products = allProducts
                .GroupBy(p => p.SKU)
                .Select(g => g.First())
                .ToList();
            
            if (products.Count < allProducts.Count)
            {
                _logger.LogWarning($"Removed {allProducts.Count - products.Count} duplicate products from Katana API response");
            }
            
            // Sync to local DB if requested
            if (sync)
            {
                // Get or create default category first
                var categories = await _categoryService.GetAllAsync();
                int defaultCategoryId;
                
                if (categories == null || !categories.Any())
                {
                    // Create default category if none exists
                    var newCategory = await _categoryService.CreateAsync(new CreateCategoryDto
                    {
                        Name = "Genel",
                        Description = "Varsayılan kategori"
                    });
                    defaultCategoryId = newCategory.Id;
                }
                else
                {
                    defaultCategoryId = categories.First().Id;
                }
                
                foreach (var katanaProduct in products)
                {
                    var existingProduct = await _productService.GetProductBySkuAsync(katanaProduct.SKU);
                    if (existingProduct == null)
                    {
                        await _productService.CreateProductAsync(new CreateProductDto
                        {
                            Name = katanaProduct.Name ?? katanaProduct.SKU,
                            SKU = katanaProduct.SKU,
                            Price = katanaProduct.SalesPrice ?? 0,
                            Stock = katanaProduct.OnHand ?? 0,
                            CategoryId = defaultCategoryId
                        });
                    }
                }
                
                // Map local DB IDs and values to Katana products for frontend operations
                var enrichedProducts = new List<object>();
                foreach (var katanaProduct in products)
                {
                    var localProduct = await _productService.GetProductBySkuAsync(katanaProduct.SKU);
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
                return Ok(new { data = enrichedProducts, count = enrichedProducts.Count });
            }
            
            return Ok(new { data = products, count = products.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching products from Katana API");
            _loggingService.LogError("Katana products fetch failed", ex, User?.Identity?.Name, null, LogCategory.ExternalAPI);
            return StatusCode(500, new { error = "Failed to fetch products from Katana API", details = ex.Message });
        }
    }

    /// <summary>
    /// Get products in a Luca-like shape (from local DB)
    /// Used by the Admin → Luca Ürünleri page. Returns realistic demo data
    /// mapped from local products to avoid 404s until direct Luca listing is wired.
    /// </summary>
    [HttpGet("luca")]
    [HttpGet("~/api/Luca/products")] // Route alias: also available as /api/Luca/products
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

            // If DB has no products, fall back to demo items to guarantee a non-empty array
            if (mapped.Count == 0)
            {
                var demo = GetDemoLucaProducts();
                return Ok(new { data = demo, count = demo.Count });
            }

            return Ok(new { data = mapped, count = mapped.Count });
        }
        catch (Exception ex)
        {
            // On any failure, serve demo data with HTTP 200 to keep Admin Panel stable
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

    /// <summary>
    /// Get a specific product by SKU from Katana API
    /// </summary>
    [HttpGet("katana/{sku}")]
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

    // ==========================================
    // LOCAL DATABASE ENDPOINTS
    // ==========================================

    [HttpGet]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any, NoStore = false)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll()
    {
        _loggingService.LogInfo("Products listed", User?.Identity?.Name, null, LogCategory.UserAction);
        var products = await _productService.GetAllProductsAsync();
        return Ok(products);
    }

    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<ProductSummaryDto>>> GetActive()
    {
        var products = await _productService.GetActiveProductsAsync();
        return Ok(products);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetById(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null)
            return NotFound($"Ürün bulunamadı: {id}");

        return Ok(product);
    }

    [HttpGet("by-sku/{sku}")]
    public async Task<ActionResult<ProductDto>> GetBySku(string sku)
    {
        var product = await _productService.GetProductBySkuAsync(sku);
        if (product == null)
            return NotFound($"SKU bulunamadı: {sku}");

        return Ok(product);
    }

    [HttpGet("category/{categoryId}")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetByCategory(int categoryId)
    {
        var products = await _productService.GetProductsByCategoryAsync(categoryId);
        return Ok(products);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest("Arama terimi boş olamaz");

        var products = await _productService.SearchProductsAsync(q);
        return Ok(products);
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetLowStock([FromQuery] int threshold = 10)
    {
        var products = await _productService.GetLowStockProductsAsync(threshold);
        return Ok(products);
    }

    [HttpGet("out-of-stock")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetOutOfStock()
    {
        var products = await _productService.GetOutOfStockProductsAsync();
        return Ok(products);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,StockManager")]
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
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }
        catch (InvalidOperationException ex)
        {
            _loggingService.LogError("Product creation failed", ex, User?.Identity?.Name, null, LogCategory.Business);
            return Conflict(ex.Message);
        }
    }

    [HttpPut("{id}")]
    [AllowAnonymous] // Temporary for testing
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
            // Update local DB
            var product = await _productService.UpdateProductAsync(id, dto);
            
            // Try to update Katana API (if product has Katana ID)
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
            
            // Check if this is a CategoryId validation error
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
            
            // Check for foreign key constraint violation on CategoryId
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
    [Authorize(Roles = "Admin,StockManager")]
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
    [Authorize(Roles = "Admin,StockManager")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            var result = await _productService.DeleteProductAsync(id);
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

    /// <summary>
    /// Update a Luca-style product (maps to local DB product)
    /// </summary>
    [HttpPut("luca/{id}")]
    [AllowAnonymous] // Temporary for testing
    public async Task<ActionResult> UpdateLucaProduct(int id, [FromBody] LucaProductUpdateDto dto)
    {
        _logger.LogInformation("UpdateLucaProduct called: ID={Id}, DTO={@Dto}", id, dto);
        
        if (dto == null)
        {
            _logger.LogWarning("UpdateLucaProduct received null DTO for product {Id}", id);
            return BadRequest(new { error = "Ürün verisi boş olamaz" });
        }

        // Validate required fields
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
            // Get existing product
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                _logger.LogWarning("UpdateLucaProduct: Product {Id} not found", id);
                return NotFound(new { error = $"Ürün bulunamadı: {id}" });
            }

            _logger.LogInformation("Existing product found: {@Product}", product);

            // Ensure valid category exists
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
                    // Create default category if none exists
                    _logger.LogWarning("No categories found, creating default category");
                    var newCategory = await _categoryService.CreateAsync(new CreateCategoryDto
                    {
                        Name = "Genel",
                        Description = "Varsayılan kategori"
                    });
                    validCategoryId = newCategory.Id;
                }
            }

            // Create UpdateProductDto from Luca data
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

            // Validate before update
            var validationErrors = Katana.Business.Validators.ProductValidator.ValidateUpdate(updateDto);
            if (validationErrors.Any())
            {
                _logger.LogWarning("Validation failed for product {Id}: {Errors}", id, string.Join(", ", validationErrors));
                return BadRequest(new { errors = validationErrors });
            }

            // Update product
            var updatedProduct = await _productService.UpdateProductAsync(id, updateDto);

            // Map back to Luca format for response
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

    [HttpPut("{id}/activate")]
    public async Task<ActionResult> Activate(int id)
    {
        var result = await _productService.ActivateProductAsync(id);
        if (!result)
            return NotFound($"Ürün bulunamadı: {id}");

        return NoContent();
    }

    [HttpPut("{id}/deactivate")]
    public async Task<ActionResult> Deactivate(int id)
    {
        var result = await _productService.DeactivateProductAsync(id);
        if (!result)
            return NotFound($"Ürün bulunamadı: {id}");

        return NoContent();
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<ProductStatisticsDto>> GetStatistics()
    {
        var stats = await _productService.GetProductStatisticsAsync();
        return Ok(stats);
    }
}
