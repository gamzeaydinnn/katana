using Katana.API.Hubs;
using Katana.Core.DTOs;
using Katana.Core.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Katana.API.Notifications;

public sealed class SignalRSyncProgressReporter : ISyncProgressReporter
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<SignalRSyncProgressReporter> _logger;

    public SignalRSyncProgressReporter(IHubContext<NotificationHub> hubContext, ILogger<SignalRSyncProgressReporter> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task ReportAsync(SyncProgressDto progress, CancellationToken ct = default)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("syncProgress", progress, ct);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "SignalR SyncProgress gönderilemedi");
        }
    }
}
