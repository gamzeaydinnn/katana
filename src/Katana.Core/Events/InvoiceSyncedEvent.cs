
using Katana.Core.Entities;

namespace Katana.Core.Events;




public class InvoiceSyncedEvent
{
    public int InvoiceId { get; }
    public string InvoiceNo { get; }
    public DateTime SyncedAt { get; }
    public string? TriggeredBy { get; }

    public InvoiceSyncedEvent(Invoice invoice, string? triggeredBy = null)
    {
        InvoiceId = invoice.Id;
        InvoiceNo = invoice.InvoiceNo;
        SyncedAt = DateTime.UtcNow;
        TriggeredBy = triggeredBy ?? "System";
    }

    public override string ToString()
    {
        return $"InvoiceSyncedEvent: {InvoiceNo} successfully synced at {SyncedAt:u}";
    }
}
