using Katana.Core.Events;
using System.Threading.Tasks;

namespace Katana.Core.Interfaces;

/// <summary>
/// Abstraction for publishing lightweight pending-stock notifications to infrastructure
/// implementations (SignalR, webhook, email enqueues, etc.).
/// Implemented in API/Infrastructure so business layer stays independent.
/// </summary>
public interface IPendingNotificationPublisher
{
    Task PublishPendingCreatedAsync(PendingStockAdjustmentCreatedEvent evt);
    Task PublishPendingApprovedAsync(PendingStockAdjustmentApprovedEvent evt);
}
