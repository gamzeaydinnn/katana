using System;

namespace Katana.Core.Events;

/// <summary>
/// Yeni stok düzeltmesi oluşturulduğunda tetiklenen event
/// </summary>
public class StockAdjustmentCreatedEvent
{
    public int AdjustmentId { get; }
    public string DocumentNo { get; }
    public string? Sku { get; }
    public int Quantity { get; }
    public string? Reason { get; }
    public DateTimeOffset CreatedAt { get; }

    public StockAdjustmentCreatedEvent(int adjustmentId, string documentNo, string? sku, int quantity, string? reason, DateTimeOffset createdAt)
    {
        AdjustmentId = adjustmentId;
        DocumentNo = documentNo;
        Sku = sku;
        Quantity = quantity;
        Reason = reason;
        CreatedAt = createdAt;
    }
}
