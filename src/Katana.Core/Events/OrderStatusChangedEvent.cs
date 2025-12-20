using Katana.Core.Enums;

namespace Katana.Core.Events;

/// <summary>
/// Order status değiştiğinde tetiklenen event
/// </summary>
public class OrderStatusChangedEvent
{
    public int OrderId { get; set; }
    public OrderStatus OldStatus { get; set; }
    public OrderStatus NewStatus { get; set; }
    public string ChangedBy { get; set; } = "System";
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }
}

/// <summary>
/// PurchaseOrder status değiştiğinde tetiklenen event
/// </summary>
public class PurchaseOrderStatusChangedEvent
{
    public int PurchaseOrderId { get; set; }
    public PurchaseOrderStatus OldStatus { get; set; }
    public PurchaseOrderStatus NewStatus { get; set; }
    public string ChangedBy { get; set; } = "System";
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }
}
