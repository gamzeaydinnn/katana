using Katana.Core.Events;
using Katana.Core.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Katana.API.Hubs;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Retry;
using System.Text.Json;

namespace Katana.API.Notifications
{
    
    
    
    
    public class SignalRNotificationPublisher : IPendingNotificationPublisher
    {
        private static readonly AsyncRetryPolicy _publishRetryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)),
                onRetry: (exception, delay, attempt, context) =>
                {
                    if (context.TryGetLogger(out var logger, out var eventName, out var entityId))
                    {
                        logger.LogWarning(exception,
                            "Retrying SignalR publish for {EventName} (PendingId: {PendingId}) attempt {Attempt}/3 after {Delay}s",
                            eventName,
                            entityId,
                            attempt,
                            delay.TotalSeconds);
                    }
                });

        private readonly IHubContext<NotificationHub> _hub;
        private readonly ILogger<SignalRNotificationPublisher> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public SignalRNotificationPublisher(
            IHubContext<NotificationHub> hub,
            ILogger<SignalRNotificationPublisher> logger,
            IServiceScopeFactory scopeFactory)
        {
            _hub = hub;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        private async Task PersistNotificationAsync(Katana.Core.Entities.Notification notif)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<Katana.Data.Context.IntegrationDbContext>();
                db.Notifications.Add(notif);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to persist notification: {Type}", notif.Type);
            }
        }

        public async Task PublishPendingCreatedAsync(PendingStockAdjustmentCreatedEvent evt)
        {
            var payload = new
            {
                id = evt.Id,
                eventType = "PendingStockAdjustmentCreated",
                externalOrderId = evt.ExternalOrderId,
                sku = evt.Sku,
                quantity = evt.Quantity,
                requestedBy = evt.RequestedBy,
                requestedAt = evt.RequestedAt,
                link = $"/admin/pending/{evt.Id}"
            };

            var payloadJson = JsonSerializer.Serialize(payload);
            var published = await TryPublishAsync("PendingStockAdjustmentCreated", payload, payloadJson, evt.Id);

            if (published)
            {
                _logger?.LogInformation("Publishing PendingStockAdjustmentCreated for PendingId {PendingId}", evt.Id);
                _logger?.LogInformation("Published PendingStockAdjustmentCreated for PendingId {PendingId}", evt.Id);
                
                await PersistNotificationAsync(new Katana.Core.Entities.Notification
                {
                    Type = "PendingStockAdjustmentCreated",
                    Title = $"Yeni bekleyen stok #{evt.Id}",
                    Payload = payloadJson,
                    Link = $"/admin?focusPending={evt.Id}",
                    RelatedPendingId = evt.Id,
                    CreatedAt = evt.RequestedAt.UtcDateTime
                });
            }
        }

        public async Task PublishPendingApprovedAsync(PendingStockAdjustmentApprovedEvent evt)
        {
            var payload = new
            {
                id = evt.Id,
                eventType = "PendingStockAdjustmentApproved",
                externalOrderId = evt.ExternalOrderId,
                sku = evt.Sku,
                quantity = evt.Quantity,
                approvedBy = evt.ApprovedBy,
                approvedAt = evt.ApprovedAt,
                link = $"/admin/pending/{evt.Id}"
            };

            var payloadJson = JsonSerializer.Serialize(payload);
            var published = await TryPublishAsync("PendingStockAdjustmentApproved", payload, payloadJson, evt.Id);

            if (published)
            {
                _logger?.LogInformation("Publishing PendingStockAdjustmentApproved for PendingId {PendingId}", evt.Id);
                _logger?.LogInformation("Published PendingStockAdjustmentApproved for PendingId {PendingId}", evt.Id);
                
                await PersistNotificationAsync(new Katana.Core.Entities.Notification
                {
                    Type = "PendingStockAdjustmentApproved",
                    Title = $"Stok ayarlaması #{evt.Id} onaylandı",
                    Payload = payloadJson,
                    Link = $"/admin?focusPending={evt.Id}",
                    RelatedPendingId = evt.Id,
                    CreatedAt = evt.ApprovedAt.UtcDateTime
                });
            }
        }

        #region Yeni Ürün Bildirimleri

        public async Task PublishProductCreatedAsync(ProductCreatedEvent evt)
        {
            var payload = new
            {
                id = evt.ProductId,
                eventType = "ProductCreated",
                productId = evt.ProductId,
                sku = evt.Sku,
                name = evt.Name,
                source = evt.Source,
                createdAt = evt.CreatedAt,
                link = $"/stock?productId={evt.ProductId}"
            };

            var payloadJson = JsonSerializer.Serialize(payload);
            var published = await TryPublishAsync("ProductCreated", payload, payloadJson, evt.ProductId);

            if (published)
            {
                _logger?.LogInformation("📦 Yeni ürün bildirimi gönderildi: {Sku} ({Source})", evt.Sku, evt.Source);
                
                await PersistNotificationAsync(new Katana.Core.Entities.Notification
                {
                    Type = "ProductCreated",
                    Title = $"Yeni ürün: {evt.Sku ?? evt.Name}",
                    Payload = payloadJson,
                    Link = $"/stock?productId={evt.ProductId}",
                    CreatedAt = evt.CreatedAt.UtcDateTime
                });
            }
        }

        public async Task PublishProductUpdatedAsync(ProductUpdatedEvent evt)
        {
            var payload = new
            {
                id = evt.ProductId,
                eventType = "ProductUpdated",
                productId = evt.ProductId,
                sku = evt.Sku,
                name = evt.Name,
                changedFields = evt.ChangedFields,
                updatedAt = evt.UpdatedAt,
                link = $"/stock?productId={evt.ProductId}"
            };

            var payloadJson = JsonSerializer.Serialize(payload);
            await TryPublishAsync("ProductUpdated", payload, payloadJson, evt.ProductId);
        }

        #endregion

        #region Stok Hareketi Bildirimleri

        public async Task PublishStockTransferCreatedAsync(StockTransferCreatedEvent evt)
        {
            var payload = new
            {
                id = evt.TransferId,
                eventType = "StockTransferCreated",
                transferId = evt.TransferId,
                documentNo = evt.DocumentNo,
                fromWarehouse = evt.FromWarehouse,
                toWarehouse = evt.ToWarehouse,
                quantity = evt.Quantity,
                productSku = evt.ProductSku,
                createdAt = evt.CreatedAt,
                link = "/stock-movement-sync"
            };

            var payloadJson = JsonSerializer.Serialize(payload);
            var published = await TryPublishAsync("StockTransferCreated", payload, payloadJson, evt.TransferId);

            if (published)
            {
                _logger?.LogInformation("🔄 Stok transfer bildirimi gönderildi: {DocumentNo}", evt.DocumentNo);

                await PersistNotificationAsync(new Katana.Core.Entities.Notification
                {
                    Type = "StockTransferCreated",
                    Title = $"Yeni transfer: {evt.DocumentNo}",
                    Payload = payloadJson,
                    Link = "/stock-movement-sync",
                    CreatedAt = evt.CreatedAt.UtcDateTime
                });
            }
        }

        public async Task PublishStockAdjustmentCreatedAsync(StockAdjustmentCreatedEvent evt)
        {
            var payload = new
            {
                id = evt.AdjustmentId,
                eventType = "StockAdjustmentCreated",
                adjustmentId = evt.AdjustmentId,
                documentNo = evt.DocumentNo,
                sku = evt.Sku,
                quantity = evt.Quantity,
                reason = evt.Reason,
                createdAt = evt.CreatedAt,
                link = "/stock-movement-sync"
            };

            var payloadJson = JsonSerializer.Serialize(payload);
            var published = await TryPublishAsync("StockAdjustmentCreated", payload, payloadJson, evt.AdjustmentId);

            if (published)
            {
                _logger?.LogInformation("📝 Stok düzeltme bildirimi gönderildi: {DocumentNo}", evt.DocumentNo);

                await PersistNotificationAsync(new Katana.Core.Entities.Notification
                {
                    Type = "StockAdjustmentCreated",
                    Title = $"Yeni düzeltme: {evt.DocumentNo}",
                    Payload = payloadJson,
                    Link = "/stock-movement-sync",
                    CreatedAt = evt.CreatedAt.UtcDateTime
                });
            }
        }

        public async Task PublishStockMovementSyncedAsync(StockMovementSyncedEvent evt)
        {
            var payload = new
            {
                id = evt.MovementId,
                eventType = "StockMovementSynced",
                movementId = evt.MovementId,
                movementType = evt.MovementType,
                documentNo = evt.DocumentNo,
                lucaDocumentId = evt.LucaDocumentId,
                syncedAt = evt.SyncedAt,
                link = "/stock-movement-sync"
            };

            var payloadJson = JsonSerializer.Serialize(payload);
            var published = await TryPublishAsync("StockMovementSynced", payload, payloadJson, evt.MovementId);

            if (published)
            {
                _logger?.LogInformation("✅ Stok hareketi Luca'ya aktarıldı: {DocumentNo} → Luca#{LucaId}", evt.DocumentNo, evt.LucaDocumentId);

                await PersistNotificationAsync(new Katana.Core.Entities.Notification
                {
                    Type = "StockMovementSynced",
                    Title = $"Aktarıldı: {evt.DocumentNo}",
                    Payload = payloadJson,
                    Link = "/stock-movement-sync",
                    CreatedAt = evt.SyncedAt.UtcDateTime
                });
            }
        }

        public async Task PublishStockMovementFailedAsync(StockMovementFailedEvent evt)
        {
            var payload = new
            {
                id = evt.MovementId,
                eventType = "StockMovementFailed",
                movementId = evt.MovementId,
                movementType = evt.MovementType,
                documentNo = evt.DocumentNo,
                errorMessage = evt.ErrorMessage,
                failedAt = evt.FailedAt,
                link = "/stock-movement-sync"
            };

            var payloadJson = JsonSerializer.Serialize(payload);
            var published = await TryPublishAsync("StockMovementFailed", payload, payloadJson, evt.MovementId);

            if (published)
            {
                _logger?.LogWarning("❌ Stok hareketi aktarım hatası: {DocumentNo} - {Error}", evt.DocumentNo, evt.ErrorMessage);

                await PersistNotificationAsync(new Katana.Core.Entities.Notification
                {
                    Type = "StockMovementFailed",
                    Title = $"Hata: {evt.DocumentNo}",
                    Payload = payloadJson,
                    Link = "/stock-movement-sync",
                    CreatedAt = evt.FailedAt.UtcDateTime
                });
            }
        }

        #endregion

        private async Task<bool> TryPublishAsync(string eventName, object payload, string payloadJson, long pendingId)
        {
            var attempt = 0;
            try
            {
                var context = new Context()
                    .WithLogger(_logger, eventName, pendingId);

                await _publishRetryPolicy.ExecuteAsync(async (ctx, cancellationToken) =>
                {
                    attempt++;
                    await _hub.Clients.All.SendAsync(eventName, payload, cancellationToken);
                }, context, CancellationToken.None);

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex,
                    "Failed to publish {EventName} for PendingId {PendingId} after {Attempts} attempts",
                    eventName,
                    pendingId,
                    attempt);

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<Katana.Data.Context.IntegrationDbContext>();
                    db.FailedNotifications.Add(new Katana.Core.Entities.FailedNotification
                    {
                        EventType = eventName,
                        Payload = payloadJson,
                        RetryCount = attempt,
                        CreatedAt = DateTime.UtcNow
                    });
                    await db.SaveChangesAsync();
                }
                catch (Exception dbEx)
                {
                    _logger?.LogError(dbEx,
                        "Failed to persist FailedNotification for {EventName} (PendingId {PendingId})",
                        eventName,
                        pendingId);
                }

                return false;
            }
        }
    }
}

file static class SignalRNotificationPublisherPollyExtensions
{
    public static Context WithLogger(this Context context, ILogger logger, string eventName, long entityId)
    {
        context["__logger"] = logger;
        context["__eventName"] = eventName;
        context["__entityId"] = entityId;
        return context;
    }

    public static bool TryGetLogger(this Context context, out ILogger logger, out string eventName, out long entityId)
    {
        entityId = 0;
        logger = context.ContainsKey("__logger") && context["__logger"] is ILogger l ? l : default!;
        eventName = context.ContainsKey("__eventName") && context["__eventName"] is string evt ? evt : string.Empty;
        if (context.ContainsKey("__entityId"))
        {
            switch (context["__entityId"])
            {
                case long longId:
                    entityId = longId;
                    break;
                case int intId:
                    entityId = intId;
                    break;
            }
        }

        return logger != null;
    }
}
