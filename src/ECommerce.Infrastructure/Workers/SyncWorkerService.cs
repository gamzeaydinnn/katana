using System;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Workers;

public class SyncWorkerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SyncWorkerService> _logger;
    private readonly TimeSpan _syncInterval = TimeSpan.FromHours(6); // Default 6 hours

    public SyncWorkerService(IServiceProvider serviceProvider, ILogger<SyncWorkerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Sync Worker Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<ISyncService>();

                _logger.LogInformation("Starting background sync operation");

                // Full sync operation
                var result = await syncService.SyncAllAsync();

                if (result.OverallSuccess)
                {
                    _logger.LogInformation("Background sync completed successfully. Total records: {TotalRecords}",
                        result.TotalProcessedRecords);
                }
                else
                {
                    _logger.LogWarning("Background sync completed with errors. Successful: {Successful}, Failed: {Failed}",
                        result.TotalSuccessfulRecords, result.TotalFailedRecords);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during background sync operation");
            }

            // Wait for next sync cycle or cancellation
            try
            {
                await Task.Delay(_syncInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
        }

        _logger.LogInformation("Sync Worker Service stopped");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sync Worker Service is stopping");
        await base.StopAsync(cancellationToken);
    }
}