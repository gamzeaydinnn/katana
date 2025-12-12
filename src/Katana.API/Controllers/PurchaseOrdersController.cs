using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Enums;
using Katana.Core.Helpers;
using Katana.Core.Interfaces;
using Katana.Data.Context;
using Katana.API.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Katana.API.Controllers;

[Authorize]
[ApiController]
[Route("api/purchase-orders")]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IntegrationDbContext _context;
    private readonly ILucaService _lucaService;
    private readonly ILoggingService _loggingService;
    private readonly IAuditService _auditService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PurchaseOrdersController> _logger;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IKatanaService _katanaService;

    public PurchaseOrdersController(
        IntegrationDbContext context,
        ILucaService lucaService,
        ILoggingService loggingService,
        IAuditService auditService,
        IMemoryCache cache,
        ILogger<PurchaseOrdersController> logger,
        IHubContext<NotificationHub> hubContext,
        IKatanaService katanaService)
    {
        _context = context;
        _lucaService = lucaService;
        _loggingService = loggingService;
        _auditService = auditService;
        _cache = cache;
        _logger = logger;
        _hubContext = hubContext;
        _katanaService = katanaService;
    }

    // ===== LIST & DETAIL ENDPOINTS =====

    /// <summary>
    /// Tüm satınalma siparişlerini listele
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<PurchaseOrderListDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? status = null,
        [FromQuery] string? syncStatus = null,
        [FromQuery] string? search = null)
    {
        try
        {
            var query = _context.PurchaseOrders
                .Include(p => p.Supplier)
                .AsQueryable();

            // Filter by status
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<PurchaseOrderStatus>(status, true, out var statusEnum))
            {
                query = query.Where(p => p.Status == statusEnum);
            }

            // Filter by sync status
            if (!string.IsNullOrEmpty(syncStatus))
            {
                query = syncStatus switch
                {
                    "synced" => query.Where(p => p.IsSyncedToLuca && string.IsNullOrEmpty(p.LastSyncError)),
                    "error" => query.Where(p => !string.IsNullOrEmpty(p.LastSyncError)),
                    "not_synced" => query.Where(p => !p.IsSyncedToLuca && string.IsNullOrEmpty(p.LastSyncError)),
                    _ => query
                };
            }

            // Filter by search (OrderNo veya Supplier Name)
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => 
                    p.OrderNo.Contains(search) || 
                    (p.Supplier != null && p.Supplier.Name.Contains(search)));
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            
            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new 
                {
                    Id = p.Id,
                    OrderNumber = p.OrderNo,
                    SupplierName = p.Supplier != null ? p.Supplier.Name : null,
                    TotalAmount = p.TotalAmount,
                    Status = p.Status.ToString(),
                    CreatedDate = p.CreatedAt
                })
                .ToListAsync();

            return Ok(new 
            { 
                items, 
                pagination = new 
                { 
                    currentPage = page, 
                    pageSize, 
                    totalCount, 
                    totalPages 
                } 
            });
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"PurchaseOrders GetAll failed: {ex.Message}", ex);
            return StatusCode(500, new { message = "Satınalma siparişleri yüklenirken hata oluştu", error = ex.Message });
        }
    }

    /// <summary>
    /// Satınalma siparişi detayını getir
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<PurchaseOrderDetailDto>> GetById(int id)
    {
        var order = await _context.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (order == null)
        {
            return NotFound(new { message = $"Satınalma siparişi bulunamadı: {id}" });
        }

        var dto = new PurchaseOrderDetailDto
        {
            Id = order.Id,
            OrderNo = order.OrderNo,
            SupplierId = order.SupplierId,
            SupplierCode = order.SupplierCode,
            SupplierName = order.Supplier?.Name,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount,
            OrderDate = order.OrderDate,
            ExpectedDate = order.ExpectedDate,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            // Luca alanları
            LucaPurchaseOrderId = order.LucaPurchaseOrderId,
            LucaDocumentNo = order.LucaDocumentNo,
            DocumentSeries = order.DocumentSeries,
            DocumentTypeDetailId = order.DocumentTypeDetailId,
            VatIncluded = order.VatIncluded,
            ReferenceCode = order.ReferenceCode,
            ProjectCode = order.ProjectCode,
            Description = order.Description,
            IsSyncedToLuca = order.IsSyncedToLuca,
            LastSyncAt = order.LastSyncAt,
            LastSyncError = order.LastSyncError,
            SyncRetryCount = order.SyncRetryCount,
            // Kalemler
            Items = order.Items.Select(i => new PurchaseOrderItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product?.Name,
                ProductSku = i.Product?.SKU,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LucaStockCode = i.LucaStockCode,
                WarehouseCode = i.WarehouseCode,
                VatRate = i.VatRate,
                UnitCode = i.UnitCode,
                DiscountAmount = i.DiscountAmount,
                LucaDetailId = i.LucaDetailId
            }).ToList()
        };

        return Ok(dto);
    }

    // ===== CREATE & UPDATE ENDPOINTS =====

    /// <summary>
    /// Yeni satınalma siparişi oluştur
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<PurchaseOrderDetailDto>> Create([FromBody] CreatePurchaseOrderRequest request)
    {
        // Validate supplier
        var supplier = await _context.Suppliers.FindAsync(request.SupplierId);
        if (supplier == null)
        {
            return BadRequest(new { message = $"Tedarikçi bulunamadı: {request.SupplierId}" });
        }

        // Generate order number
        var orderNo = $"PO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

        var order = new PurchaseOrder
        {
            OrderNo = orderNo,
            SupplierId = request.SupplierId,
            SupplierCode = supplier.Code,
            Status = PurchaseOrderStatus.Pending,
            OrderDate = request.OrderDate ?? DateTime.UtcNow,
            ExpectedDate = request.ExpectedDate,
            CreatedAt = DateTime.UtcNow,
            // Luca alanları
            DocumentSeries = request.DocumentSeries ?? "A",
            DocumentTypeDetailId = request.DocumentTypeDetailId ?? 2,
            VatIncluded = request.VatIncluded ?? true,
            ReferenceCode = $"KAT-PO-{DateTime.UtcNow.Ticks}",
            ProjectCode = request.ProjectCode,
            Description = request.Description,
            IsSyncedToLuca = false
        };

        // Add items
        decimal totalAmount = 0;
        foreach (var itemReq in request.Items)
        {
            var product = await _context.Products.FindAsync(itemReq.ProductId);
            if (product == null)
            {
                return BadRequest(new { message = $"Ürün bulunamadı: {itemReq.ProductId}" });
            }

            var item = new PurchaseOrderItem
            {
                ProductId = itemReq.ProductId,
                Quantity = itemReq.Quantity,
                UnitPrice = itemReq.UnitPrice,
                LucaStockCode = itemReq.LucaStockCode ?? product.SKU,
                WarehouseCode = itemReq.WarehouseCode ?? "01",
                VatRate = itemReq.VatRate ?? 20,
                UnitCode = itemReq.UnitCode ?? "AD",
                DiscountAmount = itemReq.DiscountAmount ?? 0
            };

            totalAmount += (item.UnitPrice * item.Quantity) - item.DiscountAmount;
            order.Items.Add(item);
        }

        order.TotalAmount = totalAmount;

        _context.PurchaseOrders.Add(order);
        await _context.SaveChangesAsync();

        _auditService.LogCreate(
            "PurchaseOrder",
            order.Id.ToString(),
            User.Identity?.Name ?? "System",
            $"Yeni satınalma siparişi oluşturuldu: {orderNo}");

        var result = await GetById(order.Id);
        var createdOrder = (result.Result as OkObjectResult)?.Value as PurchaseOrderDetailDto;
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, createdOrder);
    }

    /// <summary>
    /// Satınalma siparişi Luca alanlarını güncelle
    /// </summary>
    [HttpPatch("{id}/luca-fields")]
    public async Task<ActionResult> UpdateLucaFields(int id, [FromBody] UpdatePurchaseOrderLucaFieldsRequest request)
    {
        var order = await _context.PurchaseOrders.FindAsync(id);
        if (order == null)
        {
            return NotFound(new { message = $"Satınalma siparişi bulunamadı: {id}" });
        }

        // Update Luca fields
        if (request.DocumentSeries != null) order.DocumentSeries = request.DocumentSeries;
        if (request.DocumentTypeDetailId.HasValue) order.DocumentTypeDetailId = request.DocumentTypeDetailId.Value;
        if (request.VatIncluded.HasValue) order.VatIncluded = request.VatIncluded.Value;
        if (request.ReferenceCode != null) order.ReferenceCode = request.ReferenceCode;
        if (request.ProjectCode != null) order.ProjectCode = request.ProjectCode;
        if (request.Description != null) order.Description = request.Description;
        if (request.ShippingAddressId.HasValue) order.ShippingAddressId = request.ShippingAddressId;

        order.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _auditService.LogUpdate(
            "PurchaseOrder",
            id.ToString(),
            User.Identity?.Name ?? "System",
            null,
            "Luca alanları güncellendi");

        return Ok(new { message = "Luca alanları güncellendi" });
    }

    // ===== SYNC ENDPOINTS =====

    /// <summary>
    /// Tek siparişi Luca'ya fatura olarak senkronize et
    /// </summary>
    [HttpPost("{id}/sync")]
    public async Task<ActionResult<PurchaseOrderSyncResultDto>> SyncToLuca(int id)
    {
        var order = await _context.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (order == null)
        {
            return NotFound(new { message = $"Satınalma siparişi bulunamadı: {id}" });
        }

        if (order.Supplier == null)
        {
            return BadRequest(new { message = "Tedarikçi bilgisi eksik" });
        }

        try
        {
            // Map to Luca INVOICE request (not purchase order)
            var lucaInvoiceRequest = MappingHelper.MapToLucaInvoiceFromPurchaseOrder(order, order.Supplier);

            _loggingService.LogInfo($"Luca'ya satınalma faturası gönderiliyor: {order.OrderNo}", "PurchaseOrderInvoiceSync");

            // ❌ KALDIRILDI: ForceSessionRefreshAsync() çok uzun sürüyor
            // SendInvoiceAsync içinde zaten HTML response kontrolü ve retry var
            // Session gerekirse otomatik yenilenecek
            
            _logger.LogInformation("📤 Fatura gönderiliyor: {OrderNo}", order.OrderNo);

            // Call Luca API to create invoice
            var syncResult = await _lucaService.SendInvoiceAsync(lucaInvoiceRequest);

            if (syncResult.IsSuccess)
            {
                // Update order
                order.IsSyncedToLuca = true;
                order.LastSyncAt = DateTime.UtcNow;
                order.LastSyncError = null;
                order.SyncRetryCount = 0;
                order.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _auditService.LogSync(
                    "PurchaseOrderInvoiceSync",
                    User.Identity?.Name ?? "System",
                    $"Luca'ya fatura olarak başarıyla senkronize edildi: {order.OrderNo}");

                return Ok(new PurchaseOrderSyncResultDto
                {
                    Success = true,
                    LucaPurchaseOrderId = null,
                    LucaDocumentNo = order.OrderNo,
                    Message = "Fatura başarıyla Luca'ya aktarıldı"
                });
            }
            else
            {
                var errorMessage = syncResult.Message ?? "Bilinmeyen hata";

                order.LastSyncError = errorMessage;
                order.SyncRetryCount++;
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new PurchaseOrderSyncResultDto
                {
                    Success = false,
                    Message = errorMessage
                });
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"Luca fatura sync hatası: {ex.Message}", ex, "PurchaseOrderInvoiceSync");

            order.LastSyncError = ex.Message;
            order.SyncRetryCount++;
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return StatusCode(500, new PurchaseOrderSyncResultDto
            {
                Success = false,
                Message = $"Senkronizasyon hatası: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Sync durumunu sorgula
    /// </summary>
    [HttpGet("{id}/sync-status")]
    public async Task<ActionResult> GetSyncStatus(int id)
    {
        var order = await _context.PurchaseOrders
            .Select(p => new
            {
                p.Id,
                p.OrderNo,
                p.IsSyncedToLuca,
                p.LucaPurchaseOrderId,
                p.LucaDocumentNo,
                p.LastSyncAt,
                p.LastSyncError,
                p.SyncRetryCount
            })
            .FirstOrDefaultAsync(p => p.Id == id);

        if (order == null)
        {
            return NotFound(new { message = $"Satınalma siparişi bulunamadı: {id}" });
        }

        return Ok(order);
    }

    /// <summary>
    /// Bekleyen tüm siparişleri senkronize et
    /// </summary>
    [HttpPost("sync-all")]
    public async Task<ActionResult> SyncAll([FromQuery] int maxCount = 50)
    {
        // ✅ Performance metrics tracking
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        var pendingOrders = await _context.PurchaseOrders
            .Where(p => !p.IsSyncedToLuca && string.IsNullOrEmpty(p.LastSyncError))
            .OrderBy(p => p.CreatedAt)
            .Take(maxCount)
            .Select(p => p.Id)
            .ToListAsync();

        var results = new ConcurrentBag<object>();
        int successCount = 0;
        int failCount = 0;
        
        // ✅ Parallel batch processing (5 concurrent requests)
        await Parallel.ForEachAsync(pendingOrders,
            new ParallelOptions { MaxDegreeOfParallelism = 5 },
            async (orderId, ct) =>
            {
                try
                {
                    var syncResult = await SyncToLuca(orderId);
                    results.Add(new { orderId, success = true });
                    Interlocked.Increment(ref successCount);
                }
                catch (Exception ex)
                {
                    results.Add(new { orderId, success = false, error = ex.Message });
                    Interlocked.Increment(ref failCount);
                }
            });
        
        sw.Stop();
        
        // ✅ Performance metrics
        var rate = successCount > 0 ? successCount * 60000.0 / sw.ElapsedMilliseconds : 0;
        _logger.LogInformation(
            "PurchaseOrder SyncAll completed: {Success}/{Total}, Failed: {Failed}, " +
            "Duration: {Duration}ms, Rate: {Rate:F2} orders/min",
            successCount, pendingOrders.Count, failCount, sw.ElapsedMilliseconds, rate);

        return Ok(new
        {
            message = $"{pendingOrders.Count} sipariş işlendi",
            totalProcessed = pendingOrders.Count,
            successCount,
            failCount,
            durationMs = sw.ElapsedMilliseconds,
            rateOrdersPerMinute = rate,
            results
        });
    }

    /// <summary>
    /// Hatalı siparişleri yeniden dene
    /// </summary>
    [HttpPost("retry-failed")]
    public async Task<ActionResult> RetryFailed([FromQuery] int maxRetries = 3)
    {
        // ✅ Performance metrics tracking
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        var failedOrders = await _context.PurchaseOrders
            .Where(p => !p.IsSyncedToLuca && 
                        !string.IsNullOrEmpty(p.LastSyncError) && 
                        p.SyncRetryCount < maxRetries)
            .OrderBy(p => p.SyncRetryCount)
            .Take(20)
            .Select(p => p.Id)
            .ToListAsync();

        var results = new ConcurrentBag<object>();
        int successCount = 0;
        int failCount = 0;
        
        // ✅ Parallel retry processing (3 concurrent requests)
        await Parallel.ForEachAsync(failedOrders,
            new ParallelOptions { MaxDegreeOfParallelism = 3 },
            async (orderId, ct) =>
            {
                try
                {
                    var syncResult = await SyncToLuca(orderId);
                    results.Add(new { orderId, success = true });
                    Interlocked.Increment(ref successCount);
                }
                catch (Exception ex)
                {
                    results.Add(new { orderId, success = false, error = ex.Message });
                    Interlocked.Increment(ref failCount);
                }
            });
        
        sw.Stop();
        
        // ✅ Performance metrics
        var rate = successCount > 0 ? successCount * 60000.0 / sw.ElapsedMilliseconds : 0;
        _logger.LogInformation(
            "PurchaseOrder RetryFailed completed: {Success}/{Total}, Failed: {Failed}, " +
            "Duration: {Duration}ms, Rate: {Rate:F2} orders/min",
            successCount, failedOrders.Count, failCount, sw.ElapsedMilliseconds, rate);

        return Ok(new
        {
            message = $"{failedOrders.Count} hatalı sipariş yeniden denendi",
            totalProcessed = failedOrders.Count,
            successCount,
            failCount,
            durationMs = sw.ElapsedMilliseconds,
            rateOrdersPerMinute = rate,
            results
        });
    }

    // ===== STATS ENDPOINT =====

    /// <summary>
    /// Satınalma siparişi istatistikleri
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult> GetStats()
    {
        const string cacheKey = "purchase-order-stats";
        
        // Cache'ten dene
        if (_cache.TryGetValue(cacheKey, out object? cachedStats))
        {
            return Ok(cachedStats);
        }

        try
        {
            var stats = await _context.PurchaseOrders
                .GroupBy(p => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Synced = g.Count(p => p.IsSyncedToLuca),
                    NotSynced = g.Count(p => !p.IsSyncedToLuca && string.IsNullOrEmpty(p.LastSyncError)),
                    WithErrors = g.Count(p => !string.IsNullOrEmpty(p.LastSyncError)),
                    Pending = g.Count(p => p.Status == PurchaseOrderStatus.Pending),
                    Approved = g.Count(p => p.Status == PurchaseOrderStatus.Approved),
                    Received = g.Count(p => p.Status == PurchaseOrderStatus.Received),
                    Cancelled = g.Count(p => p.Status == PurchaseOrderStatus.Cancelled)
                })
                .FirstOrDefaultAsync();

            var result = stats ?? new
            {
                Total = 0,
                Synced = 0,
                NotSynced = 0,
                WithErrors = 0,
                Pending = 0,
                Approved = 0,
                Received = 0,
                Cancelled = 0
            };

            // 1 dakika cache'le
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(1));
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            // 🔥 DEBUG: GetStats hatasını detaylı logla
            _logger.LogError(ex, "❌ PurchaseOrders GetStats error: {Message}, Type: {Type}", ex.Message, ex.GetType().Name);
            _loggingService.LogError($"PurchaseOrders GetStats failed: {ex.Message}", ex);
            
            var fallbackStats = new
            {
                Total = 0,
                Synced = 0,
                NotSynced = 0,
                WithErrors = 0,
                Pending = 0,
                Approved = 0,
                Received = 0,
                Cancelled = 0
            };
            
            return Ok(fallbackStats);
        }
    }

    /// <summary>
    /// Sipariş durumunu güncelle (Pending -> Approved -> Received)
    /// </summary>
    [HttpPatch("{id}/status")]
    public async Task<ActionResult> UpdateStatus(int id, [FromBody] UpdatePurchaseOrderStatusRequest request)
    {
        var order = await _context.PurchaseOrders
            .Include(p => p.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (order == null)
        {
            return NotFound(new { message = $"Satınalma siparişi bulunamadı: {id}" });
        }

        // Durum geçişi kontrolü
        var isValidTransition = StatusMapper.IsValidTransition(order.Status, request.NewStatus);
        if (!isValidTransition)
        {
            return BadRequest(new { message = $"Geçersiz durum değişikliği: {order.Status} -> {request.NewStatus}" });
        }

        var oldStatus = order.Status;
        order.Status = request.NewStatus;
        order.UpdatedAt = DateTime.UtcNow;

        // 🔥 KRİTİK: "Approved" durumuna geçildiğinde KATANA'YA ÜRÜN EKLE/GÜNCELLE
        if (request.NewStatus == PurchaseOrderStatus.Approved && oldStatus != PurchaseOrderStatus.Approved)
        {
            _logger.LogInformation("✅ Sipariş onaylandı, Katana'ya ürünler ekleniyor/güncelleniyor: {OrderNo}", order.OrderNo);

            // Arka planda Katana'ya ürün ekle/güncelle
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(1000); // 1 saniye bekle (DB commit olsun)
                    
                    foreach (var item in order.Items)
                    {
                        if (item.Product == null)
                        {
                            _logger.LogWarning("⚠️ Ürün bulunamadı: ProductId={ProductId}, atlanıyor", item.ProductId);
                            continue;
                        }

                        try
                        {
                            // Önce Katana'da ürün var mı kontrol et
                            var existingProduct = await _katanaService.GetProductBySkuAsync(item.Product.SKU);
                            
                            if (existingProduct != null)
                            {
                                // Ürün varsa güncelle (stok artışı)
                                _logger.LogInformation("🔄 Katana'da ürün bulundu, güncelleniyor: {SKU}", item.Product.SKU);
                                
                                if (!int.TryParse(existingProduct.Id, out var katanaProductId))
                                {
                                    _logger.LogWarning("⚠️ Katana ürün ID sayısal değil: {Id}, SKU={SKU}", existingProduct.Id, item.Product.SKU);
                                    continue;
                                }

                                var newStock = (existingProduct.InStock ?? 0) + item.Quantity;
                                var updated = await _katanaService.UpdateProductAsync(
                                    katanaProductId,
                                    existingProduct.Name,
                                    existingProduct.SalesPrice,
                                    (int)newStock
                                );
                                
                                if (updated)
                                {
                                    _logger.LogInformation("✅ Katana ürün güncellendi: {SKU}, Yeni Stok: {Stock}", 
                                        item.Product.SKU, newStock);
                                }
                                else
                                {
                                    _logger.LogWarning("⚠️ Katana ürün güncellenemedi: {SKU}", item.Product.SKU);
                                }
                            }
                            else
                            {
                                // Ürün yoksa oluştur
                                _logger.LogInformation("➕ Katana'da ürün yok, oluşturuluyor: {SKU}", item.Product.SKU);
                                
                                var newProduct = new KatanaProductDto
                                {
                                    Name = item.Product.Name,
                                    SKU = item.Product.SKU,
                                    SalesPrice = item.UnitPrice,
                                    InStock = item.Quantity,
                                    Description = item.Product.Description,
                                    IsActive = true
                                };
                                
                                var created = await _katanaService.CreateProductAsync(newProduct);
                                
                                if (created != null)
                                {
                                    _logger.LogInformation("✅ Katana ürün oluşturuldu: {SKU}, Stok: {Stock}", 
                                        item.Product.SKU, item.Quantity);
                                }
                                else
                                {
                                    _logger.LogWarning("⚠️ Katana ürün oluşturulamadı: {SKU}", item.Product.SKU);
                                }
                            }
                        }
                        catch (Exception itemEx)
                        {
                            _logger.LogError(itemEx, "❌ Katana ürün sync hatası: {SKU}", item.Product.SKU);
                        }
                    }
                    
                    _logger.LogInformation("✅ Katana ürün sync tamamlandı: {OrderNo}", order.OrderNo);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Katana ürün sync genel hatası: {OrderNo}", order.OrderNo);
                }
            });
        }

        // 🔥 KRİTİK: "Received" durumuna geçildiğinde STOK ARTIŞI yap
        if (request.NewStatus == PurchaseOrderStatus.Received && oldStatus != PurchaseOrderStatus.Received)
        {
            _logger.LogInformation("📦 Sipariş teslim alındı, stok artışı yapılıyor: {OrderNo}", order.OrderNo);

            var stockMovements = new List<object>();
            foreach (var item in order.Items)
            {
                if (item.Product == null)
                {
                    _logger.LogWarning("⚠️ Ürün bulunamadı: ProductId={ProductId}, atlanıyor", item.ProductId);
                    continue;
                }

                // StockMovement kaydı oluştur
                var movement = new StockMovement
                {
                    ProductId = item.ProductId,
                    ProductSku = item.Product.SKU,
                    ChangeQuantity = item.Quantity, // Pozitif miktar (giriş)
                    MovementType = MovementType.In,
                    SourceDocument = $"PO-{order.OrderNo}",
                    Timestamp = DateTime.UtcNow,
                    WarehouseCode = item.WarehouseCode ?? "MAIN",
                    IsSynced = false
                };
                _context.StockMovements.Add(movement);

                // Stock kaydı oluştur
                var stockEntry = new Stock
                {
                    ProductId = item.ProductId,
                    Location = item.WarehouseCode ?? "MAIN",
                    Quantity = item.Quantity,
                    Type = "IN",
                    Reason = $"Satınalma siparişi teslim alındı: {order.OrderNo}",
                    Reference = order.OrderNo,
                    Timestamp = DateTime.UtcNow,
                    IsSynced = false
                };
                _context.Stocks.Add(stockEntry);

                stockMovements.Add(new { sku = item.Product.SKU, quantity = item.Quantity, warehouse = item.WarehouseCode ?? "MAIN" });

                _logger.LogInformation("✅ Stok artışı: {SKU} +{Qty} ({Warehouse})", 
                    item.Product.SKU, item.Quantity, item.WarehouseCode ?? "MAIN");
            }

            // 🔔 Stok hareketi bildirimi oluştur
            try
            {
                var notification = new Notification
                {
                    Type = "StockMovement",
                    Title = $"Stok Girişi: {order.OrderNo}",
                    Payload = JsonSerializer.Serialize(new
                    {
                        orderNo = order.OrderNo,
                        orderId = order.Id,
                        itemCount = stockMovements.Count,
                        movements = stockMovements
                    }),
                    Link = $"/purchase-orders/{order.Id}",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);

                // SignalR ile bildirim gönder
                await _hubContext.Clients.All.SendAsync("StockMovement", new
                {
                    orderNo = order.OrderNo,
                    orderId = order.Id,
                    itemCount = stockMovements.Count,
                    message = $"Stok girişi yapıldı: {order.OrderNo} ({stockMovements.Count} kalem)"
                });
                _logger.LogInformation("🔔 Stok hareketi bildirimi gönderildi: {OrderNo}", order.OrderNo);
            }
            catch (Exception notifEx)
            {
                _logger.LogWarning(notifEx, "Stok hareketi bildirimi oluşturulurken hata: {OrderNo}", order.OrderNo);
            }

            // 🔥 Luca'ya stok kartı senkronizasyonu tetikle (arka planda)
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(2000); // 2 saniye bekle (DB commit olsun)
                    
                    foreach (var item in order.Items)
                    {
                        if (item.Product == null) continue;
                        
                        _logger.LogInformation("🔄 Luca stok kartı senkronizasyonu tetikleniyor: {SKU}", item.Product.SKU);
                        
                        // Katana'ya ürün ekle/güncelle
                        // TODO: KatanaService ile senkronizasyon yapılacak
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Luca sync tetikleme hatası");
                }
            });
        }

        await _context.SaveChangesAsync();

        _auditService.LogUpdate(
            "PurchaseOrder",
            id.ToString(),
            User.Identity?.Name ?? "System",
            $"Status: {oldStatus} -> {request.NewStatus}",
            $"Sipariş durumu güncellendi");

        _logger.LogInformation("📝 Sipariş durumu güncellendi: {OrderNo} ({OldStatus} -> {NewStatus})", 
            order.OrderNo, oldStatus, request.NewStatus);

        return Ok(new { 
            message = "Sipariş durumu güncellendi",
            oldStatus = oldStatus.ToString(),
            newStatus = request.NewStatus.ToString(),
            stockUpdated = request.NewStatus == PurchaseOrderStatus.Received
        });
    }

    /// <summary>
    /// Siparişi sil
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var order = await _context.PurchaseOrders
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (order == null)
        {
            return NotFound(new { message = $"Satınalma siparişi bulunamadı: {id}" });
        }

        if (order.IsSyncedToLuca)
        {
            return BadRequest(new { message = "Luca'ya senkronize edilmiş siparişler silinemez" });
        }

        _context.PurchaseOrderItems.RemoveRange(order.Items);
        _context.PurchaseOrders.Remove(order);
        await _context.SaveChangesAsync();

        _auditService.LogDelete(
            "PurchaseOrder",
            id.ToString(),
            User.Identity?.Name ?? "System",
            $"Satınalma siparişi silindi: {order.OrderNo}");

        return Ok(new { message = "Sipariş silindi" });
    }
}

// ===== DTO'LAR =====

public class PurchaseOrderListDto
{
    public int Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public string? SupplierName { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public long? LucaPurchaseOrderId { get; set; }
    public string? LucaDocumentNo { get; set; }
    public bool IsSyncedToLuca { get; set; }
    public string? LastSyncError { get; set; }
    public DateTime? LastSyncAt { get; set; }
}

public class PurchaseOrderDetailDto
{
    public int Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public string? SupplierCode { get; set; }
    public string? SupplierName { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    // Luca alanları
    public long? LucaPurchaseOrderId { get; set; }
    public string? LucaDocumentNo { get; set; }
    public string? DocumentSeries { get; set; }
    public int DocumentTypeDetailId { get; set; }
    public bool VatIncluded { get; set; }
    public string? ReferenceCode { get; set; }
    public string? ProjectCode { get; set; }
    public string? Description { get; set; }
    public bool IsSyncedToLuca { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public string? LastSyncError { get; set; }
    public int SyncRetryCount { get; set; }
    public List<PurchaseOrderItemDto> Items { get; set; } = new();
}

public class PurchaseOrderItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? ProductSku { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? LucaStockCode { get; set; }
    public string? WarehouseCode { get; set; }
    public decimal VatRate { get; set; }
    public string? UnitCode { get; set; }
    public decimal DiscountAmount { get; set; }
    public long? LucaDetailId { get; set; }
}

public class CreatePurchaseOrderRequest
{
    public int SupplierId { get; set; }
    public DateTime? OrderDate { get; set; }
    public DateTime? ExpectedDate { get; set; }
    // Luca alanları
    public string? DocumentSeries { get; set; }
    public int? DocumentTypeDetailId { get; set; }
    public bool? VatIncluded { get; set; }
    public string? ProjectCode { get; set; }
    public string? Description { get; set; }
    public List<CreatePurchaseOrderItemRequest> Items { get; set; } = new();
}

public class CreatePurchaseOrderItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? LucaStockCode { get; set; }
    public string? WarehouseCode { get; set; }
    public decimal? VatRate { get; set; }
    public string? UnitCode { get; set; }
    public decimal? DiscountAmount { get; set; }
}

public class UpdatePurchaseOrderLucaFieldsRequest
{
    public string? DocumentSeries { get; set; }
    public int? DocumentTypeDetailId { get; set; }
    public bool? VatIncluded { get; set; }
    public string? ReferenceCode { get; set; }
    public string? ProjectCode { get; set; }
    public string? Description { get; set; }
    public long? ShippingAddressId { get; set; }
}

public class UpdatePurchaseOrderStatusRequest
{
    public PurchaseOrderStatus NewStatus { get; set; }
}

public class PurchaseOrderSyncResultDto
{
    public bool Success { get; set; }
    public long? LucaPurchaseOrderId { get; set; }
    public string? LucaDocumentNo { get; set; }
    public string? Message { get; set; }
}
