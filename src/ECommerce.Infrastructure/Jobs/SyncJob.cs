using ECommerce.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Quartz;

namespace ECommerce.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public class SyncJob : IJob
{
    private readonly ISyncService _syncService;
    private readonly ILogger<SyncJob> _logger;

    public SyncJob(ISyncService syncService, ILogger<SyncJob> logger)
    {
        _syncService = syncService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var syncType = context.JobDetail.JobDataMap.GetString("SyncType") ?? "ALL";
        
        _logger.LogInformation("Starting scheduled sync job for {SyncType}", syncType);

        try
        {
            switch (syncType.ToUpper())
            {
                case "STOCK":
                    await _syncService.SyncStockAsync();
                    break;
                case "INVOICE":
                    await _syncService.SyncInvoicesAsync();
                    break;
                case "CUSTOMER":
                    await _syncService.SyncCustomersAsync();
                    break;
                case "ALL":
                default:
                    await _syncService.SyncAllAsync();
                    break;
            }

            _logger.LogInformation("Completed scheduled sync job for {SyncType}", syncType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing scheduled sync job for {SyncType}", syncType);
            
            // Create a JobExecutionException to indicate the job failed
            var jobException = new JobExecutionException(ex)
            {
                RefireImmediately = false // Don't retry immediately
            };
            
            throw jobException;
        }
    }
}