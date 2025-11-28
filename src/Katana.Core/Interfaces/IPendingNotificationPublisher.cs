using Katana.Core.Events;
using System.Threading.Tasks;

namespace Katana.Core.Interfaces;

public interface IPendingNotificationPublisher
{
    Task PublishPendingCreatedAsync(PendingStockAdjustmentCreatedEvent evt);
    Task PublishPendingApprovedAsync(PendingStockAdjustmentApprovedEvent evt);
}
