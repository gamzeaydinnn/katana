using Katana.Core.DTOs;

namespace Katana.Business.Interfaces;

/// <summary>
/// Order senkronizasyon paneli için database sorgularını (query logic) encapsulate eder.
/// OrderInvoiceSyncController'dan DB logic'i ayırır.
/// </summary>
public interface IOrderSyncQueryService
{
    /// <summary>
    /// Senkronizasyon panelinde gösterilecek siparişleri sayfalanmış olarak getirir.
    /// Filtrelenmiş, sorted ve DTO'ya mapped.
    /// </summary>
    Task<(List<OrderListItemDto> Orders, int TotalCount)> GetSyncPanelOrdersAsync(
        string? syncStatus = null,
        int page = 1,
        int pageSize = 50);

    /// <summary>
    /// Tek bir siparişin detayını getirir (customer, items, ürün bilgileriyle birlikte).
    /// </summary>
    Task<OrderDetailDto?> GetOrderDetailAsync(int orderId);

    /// <summary>
    /// Senkronizasyon dashboard istatistiklerini getirir
    /// (toplam, synced, pending, cancelled siparişler ve yüzdeler).
    /// </summary>
    Task<OrderSyncDashboardStatsDto> GetSyncStatisticsAsync();

    /// <summary>
    /// Belirli bir siparişin Luca'daki mapping'ini getirir (varsa).
    /// </summary>
    Task<OrderLucaMappingDto?> GetOrderLucaMappingAsync(int orderId);
}

/// <summary>
/// Senkronizasyon panelinde gösterilen sipariş listesi item'ı.
/// </summary>
public class OrderListItemDto
{
    public int Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string Customer { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Currency { get; set; } = "TRY";
    /// <summary>
    /// Senkronizasyon durumu: "SYNCED", "PENDING", "CANCELLED"
    /// </summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Orijinal sipariş durumu enum'u
    /// </summary>
    public string OrderStatus { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public string? LucaId { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Sipariş detay sayfasında gösterilen tam veri.
/// </summary>
public class OrderDetailDto
{
    public int Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public CustomerRefDto Customer { get; set; } = new();
    public DateTime OrderDate { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "TRY";
    public string Status { get; set; } = string.Empty;
    public bool IsSynced { get; set; }
    public List<OrderItemDetailDto> Items { get; set; } = new();
}

public class CustomerRefDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TaxNo { get; set; }
    public string? Email { get; set; }
}

public class OrderItemDetailDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

/// <summary>
/// Senkronizasyon dashboard istatistikleri.
/// </summary>
public class OrderSyncDashboardStatsDto
{
    public int TotalOrders { get; set; }
    public int SyncedOrders { get; set; }
    public int PendingOrders { get; set; }
    public int CancelledOrders { get; set; }
    public int TodayOrders { get; set; }
    public int WeekOrders { get; set; }
    public double SyncPercentage { get; set; }
}

/// <summary>
/// Sipariş-Luca fatura mapping'i.
/// </summary>
public class OrderLucaMappingDto
{
    public int OrderId { get; set; }
    public string? LucaFaturaId { get; set; }
    public DateTime? MappedAt { get; set; }
}
