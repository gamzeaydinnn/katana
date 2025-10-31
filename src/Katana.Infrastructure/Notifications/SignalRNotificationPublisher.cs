using Katana.Core.Events;
using Katana.Core.Interfaces;
using System.Threading.Tasks;

namespace Katana.Infrastructure.Notifications
{
    /// <summary>
    /// Placeholder publisher in Infrastructure. The real SignalR-enabled publisher lives in the API project.
    /// This implementation is a no-op to avoid hard dependency on ASP.NET Core SignalR in the class library.
    /// </summary>
    public class SignalRNotificationPublisher : IPendingNotificationPublisher
    {
        public Task PublishPendingCreatedAsync(PendingStockAdjustmentCreatedEvent evt)
        {
            // Intentionally no-op. API project provides a SignalR-backed implementation.
            return Task.CompletedTask;
        }
        public Task PublishPendingApprovedAsync(PendingStockAdjustmentApprovedEvent evt)
        {
            // No-op placeholder for non-API environments
            return Task.CompletedTask;
        }
    }
}
