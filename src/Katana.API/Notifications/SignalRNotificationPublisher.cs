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

        public SignalRNotificationPublisher(IHubContext<NotificationHub> hub, ILogger<SignalRNotificationPublisher> logger)
        {
            _hub = hub;
            _logger = logger;
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
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to publish PendingStockAdjustmentApproved for PendingId {PendingId}", evt.Id);
            }
        }
    }
}
