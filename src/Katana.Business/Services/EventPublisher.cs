using Katana.Core.Events;
using Katana.Core.Interfaces;
using Katana.Data.Context;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Katana.Business.Services;

/// <summary>
/// Basit event publisher implementasyonu
/// Event'leri log'lar ve Notification oluşturur
/// Gelecekte message queue (RabbitMQ, Azure Service Bus) eklenebilir
/// </summary>
public class EventPublisher : IEventPublisher
{
    private readonly ILogger<EventPublisher> _logger;
    private readonly IntegrationDbContext _context;

    public EventPublisher(ILogger<EventPublisher> logger, IntegrationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : class
    {
        try
        {
            _logger.LogInformation("Publishing event: {EventType}", typeof(TEvent).Name);

            // InvoiceSyncedEvent için özel işlem
            if (@event is InvoiceSyncedEvent invoiceEvent)
            {
                await HandleInvoiceSyncedEventAsync(invoiceEvent);
            }
            // OrderStatusChangedEvent için özel işlem
            else if (@event is OrderStatusChangedEvent orderEvent)
            {
                await HandleOrderStatusChangedEventAsync(orderEvent);
            }
            // PurchaseOrderStatusChangedEvent için özel işlem
            else if (@event is PurchaseOrderStatusChangedEvent poEvent)
            {
                await HandlePurchaseOrderStatusChangedEventAsync(poEvent);
            }

            // Diğer event türleri için genişletilebilir
            
            _logger.LogInformation("Event published successfully: {EventType}", typeof(TEvent).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event: {EventType}", typeof(TEvent).Name);
            // Event publish hatası ana işlemi etkilememeli
        }
    }

    private async Task HandleInvoiceSyncedEventAsync(InvoiceSyncedEvent evt)
    {
        try
        {
            // Notification oluştur
            var notification = new Katana.Core.Entities.Notification
            {
                Type = "InvoiceSynced",
                Title = $"✅ Fatura Senkronize Edildi",
                Payload = JsonSerializer.Serialize(new
                {
                    invoiceId = evt.InvoiceId,
                    invoiceNo = evt.InvoiceNo,
                    syncedAt = evt.SyncedAt,
                    triggeredBy = evt.TriggeredBy
                }),
                Link = $"/invoices/{evt.InvoiceId}",
                CreatedAt = evt.SyncedAt,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "InvoiceSyncedEvent notification created for Invoice {InvoiceNo}",
                evt.InvoiceNo
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle InvoiceSyncedEvent notification");
        }
    }

    private async Task HandleOrderStatusChangedEventAsync(OrderStatusChangedEvent evt)
    {
        try
        {
            // Cancelled status'e geçişte Invoice ve Payment'ları iptal et
            if (evt.NewStatus == Core.Enums.OrderStatus.Cancelled)
            {
                await HandleOrderCancellationAsync(evt.OrderId);
            }
            // Returned status'e geçişte stok iade işlemlerini başlat
            else if (evt.NewStatus == Core.Enums.OrderStatus.Returned)
            {
                await HandleOrderReturnAsync(evt.OrderId);
            }

            // Notification oluştur
            var statusText = evt.NewStatus switch
            {
                Core.Enums.OrderStatus.Cancelled => "❌ İptal Edildi",
                Core.Enums.OrderStatus.Returned => "🔄 İade Edildi",
                Core.Enums.OrderStatus.Delivered => "✅ Teslim Edildi",
                Core.Enums.OrderStatus.Shipped => "📦 Kargoya Verildi",
                _ => $"📋 {evt.NewStatus}"
            };

            var notification = new Katana.Core.Entities.Notification
            {
                Type = "OrderStatusChanged",
                Title = $"Sipariş #{evt.OrderId} {statusText}",
                Payload = JsonSerializer.Serialize(new
                {
                    orderId = evt.OrderId,
                    oldStatus = evt.OldStatus.ToString(),
                    newStatus = evt.NewStatus.ToString(),
                    changedBy = evt.ChangedBy,
                    changedAt = evt.ChangedAt,
                    reason = evt.Reason
                }),
                Link = $"/orders/{evt.OrderId}",
                CreatedAt = evt.ChangedAt,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "OrderStatusChangedEvent notification created for Order {OrderId}: {OldStatus} -> {NewStatus}",
                evt.OrderId, evt.OldStatus, evt.NewStatus
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle OrderStatusChangedEvent");
        }
    }

    private async Task HandlePurchaseOrderStatusChangedEventAsync(PurchaseOrderStatusChangedEvent evt)
    {
        try
        {
            // Cancelled status'e geçişte özel işlemler
            if (evt.NewStatus == Core.Enums.PurchaseOrderStatus.Cancelled)
            {
                await HandlePurchaseOrderCancellationAsync(evt.PurchaseOrderId);
            }

            // Notification oluştur
            var statusText = evt.NewStatus switch
            {
                Core.Enums.PurchaseOrderStatus.Cancelled => "❌ İptal Edildi",
                Core.Enums.PurchaseOrderStatus.Received => "✅ Teslim Alındı",
                Core.Enums.PurchaseOrderStatus.Approved => "👍 Onaylandı",
                _ => $"📋 {evt.NewStatus}"
            };

            var notification = new Katana.Core.Entities.Notification
            {
                Type = "PurchaseOrderStatusChanged",
                Title = $"Satınalma Siparişi #{evt.PurchaseOrderId} {statusText}",
                Payload = JsonSerializer.Serialize(new
                {
                    purchaseOrderId = evt.PurchaseOrderId,
                    oldStatus = evt.OldStatus.ToString(),
                    newStatus = evt.NewStatus.ToString(),
                    changedBy = evt.ChangedBy,
                    changedAt = evt.ChangedAt
                }),
                Link = $"/purchase-orders/{evt.PurchaseOrderId}",
                CreatedAt = evt.ChangedAt,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "PurchaseOrderStatusChangedEvent notification created for PO {POId}: {OldStatus} -> {NewStatus}",
                evt.PurchaseOrderId, evt.OldStatus, evt.NewStatus
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle PurchaseOrderStatusChangedEvent");
        }
    }

    private async Task HandleOrderCancellationAsync(int orderId)
    {
        try
        {
            // İlişkili Invoice'ları iptal et
            var invoices = await _context.Invoices
                .Where(i => i.CustomerId == orderId) // TODO: Order-Invoice ilişkisi düzeltilmeli
                .ToListAsync();

            foreach (var invoice in invoices)
            {
                invoice.Status = "CANCELLED";
                invoice.UpdatedAt = DateTime.UtcNow;
            }

            // İlişkili Payment'ları iptal et (Payment entity'de Status alanı yok, silme yap)
            var payments = await _context.Payments
                .Where(p => invoices.Select(i => i.Id).Contains(p.InvoiceId))
                .ToListAsync();

            if (payments.Any())
            {
                _context.Payments.RemoveRange(payments);
                _logger.LogInformation("Removed {Count} payments for cancelled order {OrderId}", payments.Count, orderId);
            }

            // PendingStockAdjustments'ı iptal et
            var pendingAdjustments = await _context.PendingStockAdjustments
                .Where(p => p.ExternalOrderId == orderId.ToString())
                .ToListAsync();

            foreach (var adjustment in pendingAdjustments)
            {
                adjustment.Status = "Cancelled";
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Order {OrderId} cancellation cascaded to related entities", orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle order cancellation cascade for Order {OrderId}", orderId);
        }
    }

    private async Task HandleOrderReturnAsync(int orderId)
    {
        try
        {
            // İade için ters stok hareketi oluştur
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order != null)
            {
                foreach (var item in order.Items)
                {
                    var stockMovement = new Katana.Core.Entities.StockMovement
                    {
                        ProductId = item.ProductId,
                        ChangeQuantity = item.Quantity, // Pozitif (iade = stok artışı)
                        Timestamp = DateTime.UtcNow,
                        MovementType = Core.Enums.MovementType.In,
                        SourceDocument = $"RETURN-ORDER-{orderId}"
                    };

                    _context.StockMovements.Add(stockMovement);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Order {OrderId} return created reverse stock movements", orderId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle order return for Order {OrderId}", orderId);
        }
    }

    private async Task HandlePurchaseOrderCancellationAsync(int purchaseOrderId)
    {
        try
        {
            // İlgili PendingStockAdjustments'ı iptal et
            var pendingAdjustments = await _context.PendingStockAdjustments
                .Where(p => p.ExternalOrderId == purchaseOrderId.ToString())
                .ToListAsync();

            foreach (var adjustment in pendingAdjustments)
            {
                adjustment.Status = "Cancelled";
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("PurchaseOrder {POId} cancellation cascaded", purchaseOrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle purchase order cancellation for PO {POId}", purchaseOrderId);
        }
    }
}
