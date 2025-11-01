using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Katana.Infrastructure.Workers;

public class HourlyMetricsAggregator : BackgroundService
{
    private readonly ILogger<HourlyMetricsAggregator> _logger;
    private readonly IServiceProvider _services;

    public HourlyMetricsAggregator(ILogger<HourlyMetricsAggregator> logger, IServiceProvider services)
    {
        _logger = logger;
        _services = services;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HourlyMetricsAggregator starting");
        try
        {
            // Initial catch-up for previous 1 hour to ensure a datapoint exists
            await AggregatePreviousHour(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                var nextHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc).AddHours(1);
                var delay = nextHour - now;
                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                await AggregatePreviousHour(stoppingToken);
            }
        }
        finally
        {
            _logger.LogInformation("HourlyMetricsAggregator stopping");
        }
    }

    private async Task AggregatePreviousHour(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
        var now = DateTime.UtcNow;
        var hourStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc).AddHours(-1);
        var hourEnd = hourStart.AddHours(1);

        try
        {
            var errorCount = await db.ErrorLogs.CountAsync(e => e.CreatedAt >= hourStart && e.CreatedAt < hourEnd, ct);
            var auditCount = await db.AuditLogs.CountAsync(a => a.Timestamp >= hourStart && a.Timestamp < hourEnd, ct);

            var existing = await db.Set<Katana.Data.Models.DashboardMetric>().SingleOrDefaultAsync(x => x.Hour == hourStart, ct);
            if (existing == null)
            {
                db.Set<Katana.Data.Models.DashboardMetric>().Add(new Katana.Data.Models.DashboardMetric
                {
                    Hour = hourStart,
                    ErrorCount = errorCount,
                    AuditCount = auditCount,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.ErrorCount = errorCount;
                existing.AuditCount = auditCount;
            }
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Aggregated metrics for {Hour}: errors={Errors}, audits={Audits}", hourStart, errorCount, auditCount);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to aggregate metrics for {Hour}", hourStart);
        }
    }
}

