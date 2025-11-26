using Katana.Core.Events;
using Katana.Core.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Katana.API.Hubs;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
        private readonly Katana.Data.Context.IntegrationDbContext _db;

        public SignalRNotificationPublisher(IHubContext<NotificationHub> hub, ILogger<SignalRNotificationPublisher> logger, Katana.Data.Context.IntegrationDbContext db)
        {
            _hub = hub;
            _logger = logger;
            _db = db;
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
                
                try
                {
                    var notif = new Katana.Core.Entities.Notification
                    {
                        Type = "PendingStockAdjustmentCreated",
                        Title = $"Yeni bekleyen stok #{evt.Id}",
                        Payload = payloadJson,
                        Link = $"/admin?focusPending={evt.Id}",
                        RelatedPendingId = evt.Id,
                        CreatedAt = evt.RequestedAt.UtcDateTime
                    };
                    _db.Notifications.Add(notif);
                    await _db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to persist notification for PendingId {PendingId}", evt.Id);
                }
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
                
                try
                {
                    var notif = new Katana.Core.Entities.Notification
                    {
                        Type = "PendingStockAdjustmentApproved",
                        Title = $"Stok ayarlaması #{evt.Id} onaylandı",
                        Payload = payloadJson,
                        Link = $"/admin?focusPending={evt.Id}",
                        RelatedPendingId = evt.Id,
                        CreatedAt = evt.ApprovedAt.UtcDateTime
                    };
                    _db.Notifications.Add(notif);
                    await _db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to persist approval notification for PendingId {PendingId}", evt.Id);
                }
            }
        }

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
                    _db.FailedNotifications.Add(new Katana.Core.Entities.FailedNotification
                    {
                        EventType = eventName,
                        Payload = payloadJson,
                        RetryCount = attempt,
                        CreatedAt = DateTime.UtcNow
                    });
                    await _db.SaveChangesAsync();
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
