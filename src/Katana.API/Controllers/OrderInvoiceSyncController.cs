using Katana.Business.Interfaces;
using Katana.Business.Services;
using Katana.Core.Enums;
using Katana.Data.Context;
using Katana.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Katana.API.Controllers;

/// <summary>
/// Katana Siparişleri → Luca Fatura entegrasyon API'si.
/// Frontend admin panelinden sipariş senkronizasyonu yönetimi için.
/// </summary>
[Route("api/[controller]")]
[ApiController]
// [Authorize(Roles = "Admin,Manager")] // TEMPORARY: Disabled for testing
public class OrderInvoiceSyncController : ControllerBase
{
    private readonly IntegrationDbContext _context;
    private readonly IOrderInvoiceSyncService _syncService;
    private readonly ILogger<OrderInvoiceSyncController> _logger;

    public OrderInvoiceSyncController(
        IntegrationDbContext context,
        IOrderInvoiceSyncService syncService,
        ILogger<OrderInvoiceSyncController> logger)
    {
        _context = context;
        _syncService = syncService;
        _logger = logger;
    }

    #region Sipariş Listeleme

    /// <summary>
    /// Tüm siparişleri ve Luca senkronizasyon durumlarını getirir.
    /// Frontend'de sipariş listesi görüntüleme için.
    /// </summary>
    [HttpGet("orders")]
    [AllowAnonymous] // TEMPORARY: For testing
    public async Task<IActionResult> GetOrders(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var query = _context.SalesOrders
                .Include(o => o.Customer)
                .Include(o => o.Lines)
                .AsQueryable();

            // Status filtresi
            if (!string.IsNullOrEmpty(status))
            {
                var syncStatus = status.ToUpperInvariant();
                query = syncStatus switch
                {
                    "SYNCED" => query.Where(o => o.IsSyncedToLuca),
                    "PENDING" => query.Where(o => !o.IsSyncedToLuca && o.Status != "CANCELLED"),
                    "ERROR" => query.Where(o => !o.IsSyncedToLuca && !string.IsNullOrEmpty(o.LastSyncError)),
                    _ => query
                };
            }

            var totalCount = await query.CountAsync();

            // Önce siparişleri çek
            var ordersList = await query
                .OrderByDescending(o => o.OrderCreatedDate ?? o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Sipariş ID'lerini al
            var orderIds = ordersList.Select(o => o.Id).ToList();

            // İlgili mapping'leri çek
            var mappingDict = new Dictionary<int, OrderMapping>();
            try
            {
                var mappings = await _context.OrderMappings
                    .Where(om => orderIds.Contains(om.OrderId))
                    .ToListAsync();

                // Mapping'leri dictionary'ye çevir (her order için ilk mapping'i al)
                mappingDict = mappings
                    .GroupBy(m => m.OrderId)
                    .ToDictionary(g => g.Key, g => g.First());
            }
            catch (Exception ex)
            {
                // Mapping tablosu veya schema eksikse sipariş listesini yine göster.
                _logger.LogWarning(ex, "OrderMappings lookup failed; continuing without mapping data.");
            }

            // DTO'ları oluştur
            var orders = ordersList.Select(o =>
            {
                mappingDict.TryGetValue(o.Id, out var mapping);
                return new OrderListItemDto
                {
                    Id = o.Id,
                    OrderNo = o.OrderNo,
                    Customer = o.Customer != null ? o.Customer.Title : "Bilinmeyen",
                    CustomerId = o.CustomerId,
                    Date = (o.OrderCreatedDate ?? o.CreatedAt).ToString("yyyy-MM-dd"),
                    Total = o.Total ?? 0,
                    Currency = o.Currency ?? "TRY",
                    Status = o.IsSyncedToLuca ? "SYNCED" : 
                             o.Status == "CANCELLED" ? "CANCELLED" : 
                             !string.IsNullOrEmpty(o.LastSyncError) ? "ERROR" : "PENDING",
                    OrderStatus = o.Status,
                    LucaId = mapping != null ? (long?)mapping.LucaInvoiceId : null,
                    BelgeSeri = mapping != null ? mapping.BelgeSeri : o.BelgeSeri,
                    BelgeNo = mapping != null ? mapping.BelgeNo : o.BelgeNo,
                    BelgeTakipNo = mapping != null ? mapping.BelgeTakipNo : o.OrderNo,
                    ErrorMessage = o.LastSyncError,
                    ItemCount = o.Lines.Count
                };
            }).ToList();

            return Ok(new
            {
                success = true,
                data = orders,
                pagination = new
                {
                    page,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching orders for sync panel");
            return StatusCode(500, new { success = false, message = "Siparişler getirilirken hata oluştu" });
        }
    }

    /// <summary>
    /// Tek bir siparişin detaylarını getirir.
    /// </summary>
    [HttpGet("orders/{orderId}")]
    public async Task<IActionResult> GetOrderDetail(int orderId)
    {
        try
        {
            var order = await _context.SalesOrders
                .Include(o => o.Customer)
                .Include(o => o.Lines)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound(new { success = false, message = "Sipariş bulunamadı" });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    id = order.Id,
                    orderNo = order.OrderNo,
                    customer = new
                    {
                        id = order.CustomerId,
                        name = order.Customer?.Title,
                        taxNo = order.Customer?.TaxNo,
                        email = order.Customer?.Email
                    },
                    date = order.OrderCreatedDate ?? order.CreatedAt,
                    total = order.Total ?? 0,
                    currency = order.Currency ?? "TRY",
                    status = order.Status,
                    isSynced = order.IsSyncedToLuca,
                    items = order.Lines.Select(i => new
                    {
                        productId = i.VariantId,
                        productName = i.ProductName,
                        sku = i.SKU,
                        quantity = i.Quantity,
                        unitPrice = i.PricePerUnit,
                        lineTotal = i.Total ?? 0
                    })
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching order detail {OrderId}", orderId);
            return StatusCode(500, new { success = false, message = "Sipariş detayı getirilirken hata oluştu" });
        }
    }

    #endregion

    #region Senkronizasyon İşlemleri

    /// <summary>
    /// Tek bir siparişi Luca'ya fatura olarak gönderir.
    /// </summary>
    [HttpPost("sync/{orderId}")]
    public async Task<IActionResult> SyncOrder(int orderId)
    {
        try
        {
            _logger.LogInformation("Manual sync triggered for Order {OrderId}", orderId);

            var result = await _syncService.SyncSalesOrderToLucaAsync(orderId);

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    lucaId = result.LucaFaturaId,
                    message = result.Message
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing order {OrderId} to Luca", orderId);
            return StatusCode(500, new
            {
                success = false,
                message = $"Senkronizasyon hatası: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Birden fazla siparişi toplu olarak Luca'ya gönderir.
    /// </summary>
    [HttpPost("sync/batch")]
    public async Task<IActionResult> SyncBatch([FromBody] BatchSyncRequest request)
    {
        try
        {
            if (request.OrderIds == null || !request.OrderIds.Any())
            {
                return BadRequest(new { success = false, message = "En az bir sipariş seçilmelidir" });
            }

            _logger.LogInformation("Batch sync triggered for {Count} orders", request.OrderIds.Count);

            var results = new List<object>();
            var successCount = 0;
            var failCount = 0;

            foreach (var orderId in request.OrderIds)
            {
                var result = await _syncService.SyncSalesOrderToLucaAsync(orderId);
                results.Add(new
                {
                    orderId,
                    success = result.Success,
                    message = result.Message,
                    lucaId = result.LucaFaturaId
                });

                if (result.Success) successCount++;
                else failCount++;
            }

            return Ok(new
            {
                success = failCount == 0,
                message = $"Toplam: {request.OrderIds.Count}, Başarılı: {successCount}, Başarısız: {failCount}",
                results
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch sync");
            return StatusCode(500, new { success = false, message = $"Toplu senkronizasyon hatası: {ex.Message}" });
        }
    }

    /// <summary>
    /// Bekleyen tüm siparişleri otomatik olarak Luca'ya gönderir.
    /// </summary>
    [HttpPost("sync/all-pending")]
    public async Task<IActionResult> SyncAllPending()
    {
        try
        {
            _logger.LogInformation("Sync all pending orders triggered");

            var result = await _syncService.SyncPendingSalesOrdersAsync();

            return Ok(new
            {
                success = result.FailCount == 0,
                message = result.Message,
                totalCount = result.TotalCount,
                successCount = result.SuccessCount,
                failCount = result.FailCount,
                failedOrderIds = result.FailedOrderIds
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing all pending orders");
            return StatusCode(500, new { success = false, message = $"Hata: {ex.Message}" });
        }
    }

    /// <summary>
    /// Luca'ya gönderilmiş tüm faturaları getirir (sadece sync edilmiş siparişler).
    /// </summary>
    [HttpGet("synced-invoices")]
    public async Task<IActionResult> GetSyncedInvoices(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var query = _context.OrderMappings
                .Where(m => m.EntityType == "SalesOrder" && m.LucaInvoiceId > 0)
                .AsQueryable();

            var totalCount = await query.CountAsync();

            var mappings = await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var orderIds = mappings.Select(m => m.OrderId).ToList();
            var orders = await _context.SalesOrders
                .Include(o => o.Customer)
                .Where(o => orderIds.Contains(o.Id))
                .ToDictionaryAsync(o => o.Id);

            var invoices = mappings.Select(m => new InvoiceSyncDto
            {
                OrderId = m.OrderId,
                OrderNo = m.ExternalOrderId ?? (orders.ContainsKey(m.OrderId) ? orders[m.OrderId].OrderNo ?? "" : ""),
                CustomerName = orders.ContainsKey(m.OrderId) && orders[m.OrderId].Customer != null 
                    ? orders[m.OrderId].Customer!.Title ?? "Bilinmeyen" 
                    : "Bilinmeyen",
                LucaFaturaId = m.LucaInvoiceId,
                BelgeSeri = m.BelgeSeri,
                BelgeNo = m.BelgeNo,
                BelgeTakipNo = m.BelgeTakipNo,
                SyncedAt = m.CreatedAt,
                TotalAmount = orders.ContainsKey(m.OrderId) ? (orders[m.OrderId].Total ?? 0) : 0,
                Currency = orders.ContainsKey(m.OrderId) ? (orders[m.OrderId].Currency ?? "TRY") : "TRY"
            }).ToList();

            return Ok(new
            {
                success = true,
                data = invoices,
                pagination = new
                {
                    page,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching synced invoices");
            return StatusCode(500, new { success = false, message = "Faturalar getirilirken hata oluştu" });
        }
    }

    #endregion

    #region Fatura Kapama ve Silme

    /// <summary>
    /// Luca'daki faturayı kapatır (ödeme işlemi).
    /// </summary>
    [HttpPost("close/{orderId}")]
    public async Task<IActionResult> CloseInvoice(int orderId, [FromBody] CloseInvoiceRequest request)
    {
        try
        {
            var result = await _syncService.CloseInvoiceAsync(orderId, "SalesOrder", request.Amount);

            if (result.Success)
            {
                return Ok(new { success = true, message = result.Message });
            }
            else
            {
                return BadRequest(new { success = false, message = result.Message });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing invoice for order {OrderId}", orderId);
            return StatusCode(500, new { success = false, message = $"Fatura kapama hatası: {ex.Message}" });
        }
    }

    /// <summary>
    /// Luca'daki faturayı siler (iptal durumu).
    /// </summary>
    [HttpDelete("invoice/{orderId}")]
    public async Task<IActionResult> DeleteInvoice(int orderId)
    {
        try
        {
            var result = await _syncService.DeleteInvoiceAsync(orderId, "SalesOrder");

            if (result.Success)
            {
                return Ok(new { success = true, message = result.Message });
            }
            else
            {
                return BadRequest(new { success = false, message = result.Message });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting invoice for order {OrderId}", orderId);
            return StatusCode(500, new { success = false, message = $"Fatura silme hatası: {ex.Message}" });
        }
    }

    #endregion

    #region Dashboard / İstatistikler

    /// <summary>
    /// Senkronizasyon dashboard istatistikleri.
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var weekAgo = today.AddDays(-7);

            var stats = new
            {
                totalOrders = await _context.SalesOrders.CountAsync(),
                syncedOrders = await _context.SalesOrders.CountAsync(o => o.IsSyncedToLuca),
                pendingOrders = await _context.SalesOrders.CountAsync(o => !o.IsSyncedToLuca && o.Status != "CANCELLED"),
                cancelledOrders = await _context.SalesOrders.CountAsync(o => o.Status == "CANCELLED"),
                todayOrders = await _context.SalesOrders.CountAsync(o => (o.OrderCreatedDate ?? o.CreatedAt).Date == today),
                weekOrders = await _context.SalesOrders.CountAsync(o => (o.OrderCreatedDate ?? o.CreatedAt).Date >= weekAgo),
                syncPercentage = await _context.SalesOrders.AnyAsync() 
                    ? Math.Round(await _context.SalesOrders.CountAsync(o => o.IsSyncedToLuca) * 100.0 / await _context.SalesOrders.CountAsync(), 1)
                    : 0
            };

            return Ok(new { success = true, data = stats });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching dashboard stats");
            return StatusCode(500, new { success = false, message = "Dashboard verileri alınamadı" });
        }
    }

    #endregion

    #region Validation & Diagnostics

    /// <summary>
    /// Fatura/Sipariş senkronizasyon durumunu doğrular.
    /// Katana'dan gönderilen siparişlerin Luca'da olup olmadığını kontrol eder.
    /// </summary>
    [HttpGet("validate")]
    public async Task<IActionResult> ValidateOrderSync()
    {
        try
        {
            var validation = new OrderSyncValidationDto();

            // 1. Tüm siparişler ve mapping durumu
            var ordersWithMapping = await _context.SalesOrders
                .Where(o => o.Status == "APPROVED" || 
                           o.Status == "DELIVERED" || 
                           o.Status == "SHIPPED")
                .GroupJoin(
                    _context.OrderMappings,
                    o => o.Id,
                    om => om.OrderId,
                    (o, mappings) => new { Order = o, Mappings = mappings })
                .SelectMany(
                    x => x.Mappings.DefaultIfEmpty(),
                    (x, mapping) => new OrderValidationItemDto
                    {
                        OrderId = x.Order.Id,
                        OrderNo = x.Order.OrderNo,
                        OrderDate = x.Order.OrderCreatedDate ?? x.Order.CreatedAt,
                        Status = x.Order.Status ?? "",
                        TotalAmount = x.Order.Total ?? 0,
                        IsSynced = x.Order.IsSyncedToLuca,
                        LucaInvoiceId = mapping != null ? mapping.LucaInvoiceId : (long?)null,
                        EntityType = mapping != null ? mapping.EntityType : null,
                        MappingCreatedAt = mapping != null ? mapping.CreatedAt : (DateTime?)null,
                        ValidationStatus = mapping != null && mapping.LucaInvoiceId > 0 
                            ? "✅ VAR" 
                            : x.Order.IsSyncedToLuca 
                                ? "⚠️ SYNC FLAG VAR AMA MAPPING YOK" 
                                : "❌ YOK"
                    })
                .OrderByDescending(x => x.OrderDate)
                .Take(100)
                .ToListAsync();

            validation.Orders = ordersWithMapping;

            // 2. Sync edilmiş ama mapping olmayan siparişler (SORUNLU)
            var problematicOrders = await _context.SalesOrders
                .Where(o => o.IsSyncedToLuca)
                .Where(o => !_context.OrderMappings.Any(om => om.OrderId == o.Id))
                .Select(o => new ProblematicOrderDto
                {
                    OrderId = o.Id,
                    OrderNo = o.OrderNo,
                    OrderDate = o.OrderCreatedDate ?? o.CreatedAt,
                    Status = o.Status,
                    UpdatedAt = o.UpdatedAt
                })
                .ToListAsync();

            validation.ProblematicOrders = problematicOrders;

            // 3. İstatistikler
            var totalOrders = await _context.SalesOrders
                .Where(o => o.Status == "APPROVED" || 
                           o.Status == "DELIVERED" || 
                           o.Status == "SHIPPED")
                .CountAsync();

            var syncedOrders = await _context.SalesOrders
                .Where(o => o.IsSyncedToLuca)
                .CountAsync();

            var mappedOrders = await _context.OrderMappings
                .Select(om => om.OrderId)
                .Distinct()
                .CountAsync();

            var problematicCount = await _context.SalesOrders
                .Where(o => o.IsSyncedToLuca)
                .Where(o => !_context.OrderMappings.Any(om => om.OrderId == o.Id))
                .CountAsync();

            validation.Statistics = new SyncStatisticsDto
            {
                TotalOrders = totalOrders,
                SyncedOrders = syncedOrders,
                MappedOrders = mappedOrders,
                ProblematicOrders = problematicCount,
                SuccessRate = totalOrders > 0 ? (double)mappedOrders / totalOrders * 100 : 0
            };

            // 4. Entity type dağılımı
            var entityTypeDistribution = await _context.OrderMappings
                .GroupBy(om => om.EntityType)
                .Select(g => new EntityTypeDistributionDto
                {
                    EntityType = g.Key,
                    Count = g.Count(),
                    FirstSync = g.Min(x => x.CreatedAt),
                    LastSync = g.Max(x => x.CreatedAt)
                })
                .ToListAsync();

            validation.EntityTypeDistribution = entityTypeDistribution;

            // 5. Son sync logları (SyncOperationLogs tablosundan)
            var recentLogs = await _context.SyncOperationLogs
                .Where(sl => sl.SyncType.Contains("ORDER") || 
                            sl.SyncType.Contains("INVOICE"))
                .OrderByDescending(sl => sl.StartTime)
                .Take(20)
                .Select(sl => new SyncLogItemDto
                {
                    Id = sl.Id,
                    IntegrationName = sl.SyncType,
                    CreatedAt = sl.StartTime,
                    IsSuccess = sl.Status == "SUCCESS",
                    Details = sl.Details
                })
                .ToListAsync();

            validation.RecentLogs = recentLogs;

            return Ok(new { success = true, data = validation });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating order sync");
            return StatusCode(500, new { success = false, message = "Doğrulama yapılamadı", error = ex.Message });
        }
    }

    /// <summary>
    /// Duplicate mapping kontrolü - Aynı order için birden fazla mapping var mı?
    /// </summary>
    [HttpGet("validate/duplicates")]
    public async Task<IActionResult> CheckDuplicateMappings()
    {
        try
        {
            var duplicates = await _context.OrderMappings
                .GroupBy(om => om.OrderId)
                .Where(g => g.Count() > 1)
                .Select(g => new
                {
                    OrderId = g.Key,
                    MappingCount = g.Count(),
                    LucaInvoiceIds = g.Select(x => x.LucaInvoiceId).ToList(),
                    EntityTypes = g.Select(x => x.EntityType ?? "").ToList()
                })
                .ToListAsync();

            return Ok(new { success = true, duplicateCount = duplicates.Count, duplicates });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking duplicate mappings");
            return StatusCode(500, new { success = false, message = "Duplicate kontrolü yapılamadı" });
        }
    }

    #endregion
}

#region Request/Response DTOs

public class OrderListItemDto
{
    public int Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public string Customer { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string Date { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Currency { get; set; } = "TRY";
    public string Status { get; set; } = "PENDING"; // SYNCED, PENDING, ERROR, CANCELLED
    public string OrderStatus { get; set; } = string.Empty;
    public long? LucaId { get; set; }
    public string? BelgeSeri { get; set; }
    public string? BelgeNo { get; set; }
    public string? BelgeTakipNo { get; set; }
    public string? ErrorMessage { get; set; }
    public int ItemCount { get; set; }
}

public class BatchSyncRequest
{
    public List<int> OrderIds { get; set; } = new();
}

public class CloseInvoiceRequest
{
    public decimal Amount { get; set; }
}

public class OrderSyncValidationDto
{
    public List<OrderValidationItemDto> Orders { get; set; } = new();
    public List<ProblematicOrderDto> ProblematicOrders { get; set; } = new();
    public SyncStatisticsDto Statistics { get; set; } = new();
    public List<EntityTypeDistributionDto> EntityTypeDistribution { get; set; } = new();
    public List<SyncLogItemDto> RecentLogs { get; set; } = new();
}

public class OrderValidationItemDto
{
    public int OrderId { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public bool IsSynced { get; set; }
    public long? LucaInvoiceId { get; set; }
    public string? EntityType { get; set; }
    public DateTime? MappingCreatedAt { get; set; }
    public string ValidationStatus { get; set; } = string.Empty;
}

public class ProblematicOrderDto
{
    public int OrderId { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
}

public class SyncStatisticsDto
{
    public int TotalOrders { get; set; }
    public int SyncedOrders { get; set; }
    public int MappedOrders { get; set; }
    public int ProblematicOrders { get; set; }
    public double SuccessRate { get; set; }
}

public class EntityTypeDistributionDto
{
    public string EntityType { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime FirstSync { get; set; }
    public DateTime LastSync { get; set; }
}

public class SyncLogItemDto
{
    public int Id { get; set; }
    public string IntegrationName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsSuccess { get; set; }
    public string? Details { get; set; }
}

public class InvoiceSyncDto
{
    public int OrderId { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public long LucaFaturaId { get; set; }
    public string? BelgeSeri { get; set; }
    public string? BelgeNo { get; set; }
    public string? BelgeTakipNo { get; set; }
    public DateTime SyncedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "TRY";
}

#endregion
