using System;

namespace Katana.Core.Events
{
    public class PendingStockAdjustmentApprovedEvent
    {
        public long Id { get; }
        public string? ExternalOrderId { get; }
        public string? Sku { get; }
        public int Quantity { get; }
        public string? ApprovedBy { get; }
        public DateTimeOffset ApprovedAt { get; }

        public PendingStockAdjustmentApprovedEvent(long id, string? externalOrderId, string? sku, int quantity, string? approvedBy, DateTimeOffset approvedAt)
        {
            Id = id;
            ExternalOrderId = externalOrderId;
            Sku = sku;
            Quantity = quantity;
            ApprovedBy = approvedBy;
            ApprovedAt = approvedAt;
        }
    }
}
