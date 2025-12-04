using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Enums;
using Katana.Core.Helpers;
using Katana.Core.Interfaces;
using Katana.Data.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    public PurchaseOrdersController(
        IntegrationDbContext context,
        ILucaService lucaService,
        ILoggingService loggingService,
        IAuditService auditService,
        IMemoryCache cache,
        ILogger<PurchaseOrdersController> logger)
    {
        _context = context;
        _lucaService = lucaService;
        _loggingService = loggingService;
        _auditService = auditService;
        _cache = cache;
        _logger = logger;
    }

    // ===== LIST & DETAIL ENDPOINTS =====

    /// <summary>
    /// Tüm satınalma siparişlerini listele
    /// </summary>
    [HttpGet]
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

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, await GetById(order.Id));
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
    /// Tek siparişi Luca'ya senkronize et
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
            // Map to Luca request
            var lucaRequest = MappingHelper.MapToLucaPurchaseOrderFromEntity(order, order.Supplier);

            _loggingService.LogInfo($"Luca'ya satınalma siparişi gönderiliyor: {order.OrderNo}", "PurchaseOrderSync");

            // Call Luca API
            var response = await _lucaService.CreatePurchaseOrderAsync(lucaRequest);

            // Parse response
            long? lucaPurchaseOrderId = null;
            string? lucaDocumentNo = null;

            if (response.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
            {
                if (response.TryGetProperty("data", out var dataProp))
                {
                    if (dataProp.TryGetProperty("ssSatinalmaSiparisBaslikId", out var baslikIdProp))
                    {
                        lucaPurchaseOrderId = baslikIdProp.GetInt64();
                    }
                    if (dataProp.TryGetProperty("belgeNo", out var belgeNoProp))
                    {
                        lucaDocumentNo = belgeNoProp.GetString();
                    }
                }

                // Update order
                order.LucaPurchaseOrderId = lucaPurchaseOrderId;
                order.LucaDocumentNo = lucaDocumentNo;
                order.IsSyncedToLuca = true;
                order.LastSyncAt = DateTime.UtcNow;
                order.LastSyncError = null;
                order.SyncRetryCount = 0;
                order.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _auditService.LogSync(
                    "PurchaseOrderSync",
                    User.Identity?.Name ?? "System",
                    $"Luca'ya başarıyla senkronize edildi. LucaId: {lucaPurchaseOrderId}");

                return Ok(new PurchaseOrderSyncResultDto
                {
                    Success = true,
                    LucaPurchaseOrderId = lucaPurchaseOrderId,
                    LucaDocumentNo = lucaDocumentNo,
                    Message = "Senkronizasyon başarılı"
                });
            }
            else
            {
                var errorMessage = response.TryGetProperty("message", out var msgProp) 
                    ? msgProp.GetString() 
                    : "Bilinmeyen hata";

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
            _loggingService.LogError($"Luca sync hatası: {ex.Message}", ex, "PurchaseOrderSync");

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

public class PurchaseOrderSyncResultDto
{
    public bool Success { get; set; }
    public long? LucaPurchaseOrderId { get; set; }
    public string? LucaDocumentNo { get; set; }
    public string? Message { get; set; }
}
