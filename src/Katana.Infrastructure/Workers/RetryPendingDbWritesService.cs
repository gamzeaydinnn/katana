using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Katana.Core.Services;
using Katana.Data.Context;
using Katana.Data.Models;

namespace Katana.Infrastructure.Workers
{
    public class RetryPendingDbWritesService : BackgroundService
    {
        private readonly ILogger<RetryPendingDbWritesService> _logger;
        private readonly IServiceProvider _services;
    private readonly PendingDbWriteQueue _queue;

        public RetryPendingDbWritesService(ILogger<RetryPendingDbWritesService> logger, IServiceProvider services, PendingDbWriteQueue queue)
        {
            _logger = logger;
            _services = services;
            _queue = queue;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RetryPendingDbWritesService starting. Interval: 15s");
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        if (_queue.Count > 0)
                        {
                            using var scope = _services.CreateScope();
                            var db = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
                            int flushed = 0;
                            while (_queue.TryDequeue(out var pending))
                            {
                                if (pending is null)
                                {
                                    continue;
                                }
                                try
                                {
                                    
                                    var audit = new AuditLog
                                    {
                                        ActionType = pending.ActionType,
                                        EntityName = pending.EntityName,
                                        EntityId = pending.EntityId,
                                        PerformedBy = pending.PerformedBy,
                                        Timestamp = pending.Timestamp,
                                        Details = pending.Details,
                                        Changes = pending.Changes,
                                        IpAddress = pending.IpAddress,
                                        UserAgent = pending.UserAgent
                                    };
                                    db.AuditLogs.Add(audit);
                                    await db.SaveChangesAsync(stoppingToken);
                                    flushed++;
                                }
                                catch (OperationCanceledException)
                                {
                                    
                                    return;
                                }
                                catch (Exception ex)
                                {
                                    
                                    _logger.LogWarning(ex, "RetryPendingDbWritesService: failed to flush audit, re-enqueueing and will retry later.");
                                    _queue.EnqueueAudit(pending);
                                    break;
                                }
                            }

                            if (flushed > 0) _logger.LogInformation("Flushed {Count} pending audit records to DB.", flushed);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error while flushing pending DB writes");
                    }

                    
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
            finally
            {
                _logger.LogInformation("RetryPendingDbWritesService stopping.");
            }
        }
    }
}
