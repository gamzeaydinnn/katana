using Katana.Core.Entities;

namespace Katana.Core.Events;




public class StockSyncedEvent
{
    public int StockId { get; }
    public string ProductSku { get; }
    public int Quantity { get; }
    public DateTime SyncedAt { get; }
    public string? TriggeredBy { get; }

    public StockSyncedEvent(Stock stock, string? triggeredBy = null)
    {
        StockId = stock.Id;
        ProductSku = stock.Product?.SKU ?? $"PRODUCT_{stock.ProductId}";
        Quantity = stock.Quantity;
        SyncedAt = DateTime.UtcNow;
        TriggeredBy = triggeredBy ?? "System";
    }

    public override string ToString()
    {
        return $"StockSyncedEvent: {ProductSku} ({Quantity}) synced at {SyncedAt:u}";
    }
}
