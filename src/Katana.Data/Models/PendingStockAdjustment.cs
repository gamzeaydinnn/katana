using System;

namespace Katana.Data.Models
{
    public class PendingStockAdjustment
    {
        public long Id { get; set; }
        public string ExternalOrderId { get; set; } = string.Empty;
        public long ProductId { get; set; }
        public string? Sku { get; set; }
        public int Quantity { get; set; }
        public string RequestedBy { get; set; } = "system";
        public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Failed
        public string? ApprovedBy { get; set; }
        public DateTimeOffset? ApprovedAt { get; set; }
        public string? RejectionReason { get; set; }
        public string? Notes { get; set; }
    }
}
