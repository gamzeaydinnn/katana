using Microsoft.AspNetCore.SignalR;

namespace Katana.API.Hubs;

/// <summary>
/// Simple SignalR hub used to broadcast admin notifications (pending adjustments, alerts, etc.).
/// Clients should connect to /hubs/notifications and listen for events such as
/// "PendingStockAdjustmentCreated".
/// </summary>
public class NotificationHub : Hub
{
}
