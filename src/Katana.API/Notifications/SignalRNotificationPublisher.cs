using Katana.Core.Events;
using Katana.Core.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Katana.API.Hubs;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Katana.API.Notifications
{
    /// <summary>
    /// Publishes pending-created notifications to connected clients via SignalR.
    /// Implemented inside the API project so SignalR types are available.
    /// </summary>
    public class SignalRNotificationPublisher : IPendingNotificationPublisher
    {
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

            try
            {
                _logger?.LogInformation("Publishing PendingStockAdjustmentCreated for PendingId {PendingId}", evt.Id);
                await _hub.Clients.All.SendAsync("PendingStockAdjustmentCreated", payload);
                _logger?.LogInformation("Published PendingStockAdjustmentCreated for PendingId {PendingId}", evt.Id);
                // Persist a server-side notification record
                try
                {
                    var notif = new Katana.Core.Entities.Notification
                    {
                        Type = "PendingStockAdjustmentCreated",
                        Title = $"Yeni bekleyen stok #{evt.Id}",
                        Payload = System.Text.Json.JsonSerializer.Serialize(payload),
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
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to publish PendingStockAdjustmentCreated for PendingId {PendingId}", evt.Id);
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

            try
            {
                _logger?.LogInformation("Publishing PendingStockAdjustmentApproved for PendingId {PendingId}", evt.Id);
                await _hub.Clients.All.SendAsync("PendingStockAdjustmentApproved", payload);
                _logger?.LogInformation("Published PendingStockAdjustmentApproved for PendingId {PendingId}", evt.Id);
                // Persist approval notification
                try
                {
                    var notif = new Katana.Core.Entities.Notification
                    {
                        Type = "PendingStockAdjustmentApproved",
                        Title = $"Stok ayarlaması #{evt.Id} onaylandı",
                        Payload = System.Text.Json.JsonSerializer.Serialize(payload),
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
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to publish PendingStockAdjustmentApproved for PendingId {PendingId}", evt.Id);
            }
        }
    }
}
