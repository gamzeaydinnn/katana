using Katana.Business.Interfaces;
using Katana.Core.Enums;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Katana.Business.Services;

/// <summary>
/// Order senkronizasyon paneli için database sorgularını handle eder.
/// OrderInvoiceSyncController'dan DB logic'i ayırarak controller'ı ince tutar.
/// </summary>
public class OrderSyncQueryService : IOrderSyncQueryService
{
    private readonly IntegrationDbContext _context;
    private readonly ILogger<OrderSyncQueryService> _logger;

    public OrderSyncQueryService(IntegrationDbContext context, ILogger<OrderSyncQueryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Senkronizasyon panelinde gösterilecek siparişleri sayfalanmış olarak getirir.
    /// </summary>
    public async Task<(List<OrderListItemDto> Orders, int TotalCount)> GetSyncPanelOrdersAsync(
        string? syncStatus = null,
        int page = 1,
        int pageSize = 50)
    {
        try
        {
            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Items)
                .AsQueryable();

            // Senkronizasyon durumu filtrelemesi (SYNCED, PENDING, CANCELLED)
            if (!string.IsNullOrEmpty(syncStatus))
            {
                var upperStatus = syncStatus.ToUpperInvariant();
                query = upperStatus switch
                {
                    "SYNCED" => query.Where(o => o.IsSynced),
                    "PENDING" => query.Where(o => !o.IsSynced && o.Status != OrderStatus.Cancelled),
                    "CANCELLED" => query.Where(o => o.Status == OrderStatus.Cancelled),
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
                    CustomerId = o.CustomerId,
                    Customer = o.Customer != null ? o.Customer.Title : "Bilinmeyen",
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

            return (orders, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sync panel orders");
            throw;
        }
    }

    /// <summary>
    /// Tek bir siparişin detayını getirir (customer, items, ürün bilgileriyle).
    /// </summary>
    public async Task<OrderDetailDto?> GetOrderDetailAsync(int orderId)
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
                return null;
            }

            return new OrderDetailDto
            {
                Id = order.Id,
                OrderNo = order.OrderNo,
                Customer = new CustomerRefDto
                {
                    Id = order.CustomerId,
                    Name = order.Customer?.Title ?? "Bilinmeyen",
                    TaxNo = order.Customer?.TaxNo,
                    Email = order.Customer?.Email
                },
                OrderDate = order.OrderDate,
                Total = order.TotalAmount,
                Currency = order.Currency,
                Status = order.Status.ToString(),
                IsSynced = order.IsSynced,
                Items = order.Items.Select(i => new OrderItemDetailDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product?.Name ?? "Bilinmeyen",
                    Sku = i.Product?.SKU,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    LineTotal = i.Quantity * i.UnitPrice
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching order detail for order {OrderId}", orderId);
            throw;
        }
    }

    /// <summary>
    /// Senkronizasyon dashboard istatistiklerini getirir.
    /// </summary>
    public async Task<OrderSyncDashboardStatsDto> GetSyncStatisticsAsync()
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var weekAgo = today.AddDays(-7);

            var totalOrders = await _context.Orders.CountAsync();
            var syncedOrders = await _context.Orders.CountAsync(o => o.IsSynced);
            var pendingOrders = await _context.Orders.CountAsync(o => !o.IsSynced && o.Status != OrderStatus.Cancelled);
            var cancelledOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Cancelled);
            var todayOrders = await _context.Orders.CountAsync(o => o.OrderDate.Date == today);
            var weekOrders = await _context.Orders.CountAsync(o => o.OrderDate.Date >= weekAgo);

            var syncPercentage = totalOrders > 0
                ? Math.Round(syncedOrders * 100.0 / totalOrders, 1)
                : 0.0;

            return new OrderSyncDashboardStatsDto
            {
                TotalOrders = totalOrders,
                SyncedOrders = syncedOrders,
                PendingOrders = pendingOrders,
                CancelledOrders = cancelledOrders,
                TodayOrders = todayOrders,
                WeekOrders = weekOrders,
                SyncPercentage = syncPercentage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sync statistics");
            throw;
        }
    }

    /// <summary>
    /// Sipariş-Luca fatura mapping'ini getirir.
    /// </summary>
    public async Task<OrderLucaMappingDto?> GetOrderLucaMappingAsync(int orderId)
    {
        try
        {
            // TODO: OrderLucaMapping tablosu henüz database'de yok
            // Mapping'i tutacak tablo eklenince bunu implement et
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching order Luca mapping for order {OrderId}", orderId);
            throw;
        }
    }
}
