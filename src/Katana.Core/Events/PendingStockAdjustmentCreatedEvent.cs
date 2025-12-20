using System;

namespace Katana.Core.Events;

public class PendingStockAdjustmentCreatedEvent
{
    public long Id { get; }
    public string? ExternalOrderId { get; }
    public string? Sku { get; }
    public int Quantity { get; }
    public string? RequestedBy { get; }
    public DateTimeOffset RequestedAt { get; }

    public PendingStockAdjustmentCreatedEvent(long id, string? externalOrderId, string? sku, int quantity, string? requestedBy, DateTimeOffset requestedAt)
    {
        Id = id;
        ExternalOrderId = externalOrderId;
        Sku = sku;
        Quantity = quantity;
        RequestedBy = requestedBy;
        RequestedAt = requestedAt;
    }
}
