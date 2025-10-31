using System;

namespace Katana.Core.Events;

/// <summary>
/// Lightweight event payload for a newly created pending stock adjustment.
/// Business layer will publish this to infrastructure implementations (e.g. SignalR, email, webhook).
/// </summary>
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
