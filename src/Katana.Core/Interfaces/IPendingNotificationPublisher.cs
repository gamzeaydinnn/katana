using Katana.Core.Events;
using System.Threading.Tasks;

namespace Katana.Core.Interfaces;

/// <summary>
/// Bildirim yayınlama interface'i - SignalR üzerinden gerçek zamanlı bildirimler
/// </summary>
public interface IPendingNotificationPublisher
{
    // Mevcut - Pending Stock Adjustment bildirimleri
    Task PublishPendingCreatedAsync(PendingStockAdjustmentCreatedEvent evt);
    Task PublishPendingApprovedAsync(PendingStockAdjustmentApprovedEvent evt);
    
    // Yeni - Ürün bildirimleri
    Task PublishProductCreatedAsync(ProductCreatedEvent evt);
    Task PublishProductUpdatedAsync(ProductUpdatedEvent evt);
    
    // Yeni - Stok hareketi bildirimleri
    Task PublishStockTransferCreatedAsync(StockTransferCreatedEvent evt);
    Task PublishStockAdjustmentCreatedAsync(StockAdjustmentCreatedEvent evt);
    Task PublishStockMovementSyncedAsync(StockMovementSyncedEvent evt);
    Task PublishStockMovementFailedAsync(StockMovementFailedEvent evt);
}
