using Katana.Business.Interfaces;
using Katana.Business.UseCases.Sync;
using Katana.Core.Enums;
using Katana.Data.Context;
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
[Authorize(Roles = "Admin,Manager")]
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
    public async Task<IActionResult> GetOrders(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Items)
                .AsQueryable();

            // Status filtresi
            if (!string.IsNullOrEmpty(status))
            {
                var syncStatus = status.ToUpperInvariant();
                query = syncStatus switch
                {
                    "SYNCED" => query.Where(o => o.IsSynced),
                    "PENDING" => query.Where(o => !o.IsSynced && o.Status != OrderStatus.Cancelled),
                    "ERROR" => query.Where(o => !o.IsSynced), // Error durumu ayrı tabloda tutulabilir
                    _ => query
                };
            }

            var totalCount = await query.CountAsync();

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OrderListItemDto
                {
                    Id = o.Id,
                    OrderNo = o.OrderNo,
                    Customer = o.Customer != null ? o.Customer.Title : "Bilinmeyen",
                    CustomerId = o.CustomerId,
                    Date = o.OrderDate.ToString("yyyy-MM-dd"),
                    Total = o.TotalAmount,
                    Currency = o.Currency,
                    Status = o.IsSynced ? "SYNCED" : 
                             o.Status == OrderStatus.Cancelled ? "CANCELLED" : "PENDING",
                    OrderStatus = o.Status.ToString(),
                    LucaId = null, // Mapping tablosundan çekilebilir
                    ErrorMessage = null,
                    ItemCount = o.Items.Count
                })
                .ToListAsync();

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
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
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
                    date = order.OrderDate,
                    total = order.TotalAmount,
                    currency = order.Currency,
                    status = order.Status.ToString(),
                    isSynced = order.IsSynced,
                    items = order.Items.Select(i => new
                    {
                        productId = i.ProductId,
                        productName = i.Product?.Name,
                        sku = i.Product?.SKU,
                        quantity = i.Quantity,
                        unitPrice = i.UnitPrice,
                        lineTotal = i.Quantity * i.UnitPrice
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
                totalOrders = await _context.Orders.CountAsync(),
                syncedOrders = await _context.Orders.CountAsync(o => o.IsSynced),
                pendingOrders = await _context.Orders.CountAsync(o => !o.IsSynced && o.Status != OrderStatus.Cancelled),
                cancelledOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Cancelled),
                todayOrders = await _context.Orders.CountAsync(o => o.OrderDate.Date == today),
                weekOrders = await _context.Orders.CountAsync(o => o.OrderDate.Date >= weekAgo),
                syncPercentage = await _context.Orders.AnyAsync() 
                    ? Math.Round(await _context.Orders.CountAsync(o => o.IsSynced) * 100.0 / await _context.Orders.CountAsync(), 1)
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

#endregion
