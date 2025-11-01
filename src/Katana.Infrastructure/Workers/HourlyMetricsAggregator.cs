using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Katana.Infrastructure.Workers;

public class HourlyMetricsAggregator : BackgroundService
{
    private static readonly TimeSpan AggregationInterval = TimeSpan.FromMinutes(10);
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
            // Initial catch-up for previously completed slice
            await AggregatePreviousSlice(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var nextRunAt = GetNextRunUtc();
                var delay = nextRunAt - DateTime.UtcNow;
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

                await AggregatePreviousSlice(stoppingToken);
            }
        }
        finally
        {
            _logger.LogInformation("HourlyMetricsAggregator stopping");
        }
    }

    private static DateTime GetNextRunUtc()
    {
        var now = DateTime.UtcNow;
        var aligned = AlignToInterval(now, AggregationInterval);
        return aligned.Add(AggregationInterval);
    }

    private static DateTime AlignToInterval(DateTime input, TimeSpan interval)
    {
        var ticks = input.Ticks / interval.Ticks;
        return new DateTime(ticks * interval.Ticks, DateTimeKind.Utc);
    }

    private async Task AggregatePreviousSlice(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
        var now = DateTime.UtcNow;
        var sliceEnd = AlignToInterval(now, AggregationInterval);
        if (sliceEnd > now)
        {
            sliceEnd = sliceEnd - AggregationInterval;
        }
        var sliceStart = sliceEnd - AggregationInterval;

        try
        {
            var errorCount = await db.ErrorLogs.CountAsync(e => e.CreatedAt >= sliceStart && e.CreatedAt < sliceEnd, ct);
            var auditCount = await db.AuditLogs.CountAsync(a => a.Timestamp >= sliceStart && a.Timestamp < sliceEnd, ct);

            var existing = await db.DashboardMetrics.SingleOrDefaultAsync(x => x.Hour == sliceStart, ct);
            if (existing == null)
            {
                db.DashboardMetrics.Add(new Katana.Data.Models.DashboardMetric
                {
                    Hour = sliceStart,
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
            _logger.LogInformation("Aggregated metrics for slice starting {SliceStart}: errors={Errors}, audits={Audits}", sliceStart, errorCount, auditCount);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to aggregate metrics for slice starting {SliceStart}", sliceStart);
        }
    }
}
