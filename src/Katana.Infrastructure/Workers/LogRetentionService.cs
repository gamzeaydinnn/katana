using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Katana.Infrastructure.Workers;

public class LogRetentionService : BackgroundService
{
    private const int DefaultRetentionDays = 90;
    private readonly IServiceProvider _services;
    private readonly ILogger<LogRetentionService> _logger;
    private readonly IConfiguration _configuration;

    public LogRetentionService(
        IServiceProvider services,
        ILogger<LogRetentionService> logger,
        IConfiguration configuration)
    {
        _services = services;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LogRetentionService starting");
        try
        {
            
            await CleanupAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var nextRun = GetNextRunUtc();
                var delay = nextRun - DateTime.UtcNow;

                try
                {
                    if (delay > TimeSpan.Zero)
                    {
                        await Task.Delay(delay, stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                await CleanupAsync(stoppingToken);
            }
        }
        finally
        {
            _logger.LogInformation("LogRetentionService stopping");
        }
    }

    private DateTime GetNextRunUtc()
    {
        var now = DateTime.UtcNow;
        var next = new DateTime(now.Year, now.Month, now.Day, 2, 0, 0, DateTimeKind.Utc);
        if (next <= now)
        {
            next = next.AddDays(1);
        }

        return next;
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();

        var retentionDays = _configuration.GetValue<int?>("LogRetention:Days") ?? DefaultRetentionDays;
        retentionDays = Math.Max(retentionDays, 1);

        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);

        try
        {
            var errorRemoved = await db.ErrorLogs
                .Where(e => e.CreatedAt < cutoff)
                .ExecuteDeleteAsync(ct);

            var auditRemoved = await db.AuditLogs
                .Where(a => a.Timestamp < cutoff)
                .ExecuteDeleteAsync(ct);

            _logger.LogInformation(
                "Log retention cleanup completed. Removed {ErrorCount} error logs and {AuditCount} audit logs older than {Cutoff}.",
                errorRemoved,
                auditRemoved,
                cutoff);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Log retention cleanup failed");
        }
    }
}
