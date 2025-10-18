using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using System.Threading;
using System.Threading.Tasks;

namespace Katana.Business.Jobs;

/// <summary>
/// Uygulama başladığında Quartz scheduler’ı başlatır ve senkronizasyon işleri planlar.
/// </summary>
public class SyncWorkerService : IHostedService
{
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly ILogger<SyncWorkerService> _logger;
    private IScheduler? _scheduler;

    public SyncWorkerService(ISchedulerFactory schedulerFactory, ILogger<SyncWorkerService> logger)
    {
        _schedulerFactory = schedulerFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("⏳ SyncWorkerService starting...");

        _scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

        // SyncJob (6 saatte bir)
        var syncJob = JobBuilder.Create<SyncJob>()
            .WithIdentity("SyncJob")
            .UsingJobData("SyncType", "ALL")
            .Build();

        var syncTrigger = TriggerBuilder.Create()
            .WithCronSchedule("0 0 */6 * * ?") // her 6 saatte bir
            .Build();

        // RetryJob (günde bir kez sabah 3'te)
        var retryJob = JobBuilder.Create<RetryJob>()
            .WithIdentity("RetryJob")
            .Build();

        var retryTrigger = TriggerBuilder.Create()
            .WithCronSchedule("0 0 3 * * ?")
            .Build();

        await _scheduler.ScheduleJob(syncJob, syncTrigger, cancellationToken);
        await _scheduler.ScheduleJob(retryJob, retryTrigger, cancellationToken);

        await _scheduler.Start(cancellationToken);

        _logger.LogInformation("✅ SyncWorkerService started successfully");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🛑 SyncWorkerService stopping...");
        if (_scheduler != null)
            await _scheduler.Shutdown(cancellationToken);
    }
}
