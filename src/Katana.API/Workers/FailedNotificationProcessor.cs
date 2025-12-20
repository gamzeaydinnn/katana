using System.Text.Json;
using Katana.API.Hubs;
using Katana.Data.Context;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Katana.API.Workers;

public class FailedNotificationProcessor : BackgroundService
{
    private readonly ILogger<FailedNotificationProcessor> _logger;
    private readonly IServiceProvider _services;
    private readonly IHubContext<NotificationHub> _hub;

    public FailedNotificationProcessor(
        ILogger<FailedNotificationProcessor> logger,
        IServiceProvider services,
        IHubContext<NotificationHub> hub)
    {
        _logger = logger;
        _services = services;
        _hub = hub;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FailedNotificationProcessor starting (poll 30s)");
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessOnce(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "FailedNotificationProcessor: error in loop");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        finally
        {
            _logger.LogInformation("FailedNotificationProcessor stopping");
        }
    }

    private async Task ProcessOnce(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();

        var now = DateTime.UtcNow;
        var batch = await db.FailedNotifications
            .Where(n => n.NextRetryAt == null || n.NextRetryAt <= now)
            .OrderBy(n => n.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        if (batch.Count == 0)
        {
            return;
        }

        _logger.LogInformation("FailedNotificationProcessor picked {Count} items to retry", batch.Count);

        foreach (var fn in batch)
        {
            try
            {
                var payloadObj = JsonSerializer.Deserialize<object>(fn.Payload);
                await _hub.Clients.All.SendAsync(fn.EventName, payloadObj, ct);
                db.FailedNotifications.Remove(fn);
                await db.SaveChangesAsync(ct);
                _logger.LogInformation("Re-published failed notification {Id} ({EventName})", fn.Id, fn.EventName);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                
                fn.RetryCount += 1;
                fn.LastRetryAt = DateTime.UtcNow;
                var delaySeconds = Math.Min((int)Math.Pow(2, Math.Max(0, fn.RetryCount - 1)), 300);
                fn.NextRetryAt = DateTime.UtcNow.AddSeconds(Math.Max(5, delaySeconds));
                fn.LastError = ex.Message;
                await db.SaveChangesAsync(ct);
                _logger.LogWarning(ex, "Re-publish failed for notification {Id} ({EventName}). NextRetryIn={Delay}s", fn.Id, fn.EventName, delaySeconds);
            }
        }
    }
}
