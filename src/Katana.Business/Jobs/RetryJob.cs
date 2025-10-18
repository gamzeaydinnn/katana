// /Users/dilarasara/katana/src/Katana.Business/Jobs/RetryJob.cs
using System;
using System.Linq;
using System.Threading.Tasks;
using Katana.Business.Interfaces;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Katana.Business.Jobs;

[DisallowConcurrentExecution]
public class RetryJob : IJob
{
    private readonly IntegrationDbContext _context;
    private readonly ISyncService _syncService;
    private readonly ILogger<RetryJob> _logger;

    public RetryJob(IntegrationDbContext context, ISyncService syncService, ILogger<RetryJob> logger)
    {
        _context = context;
        _syncService = syncService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
{
    _logger.LogInformation("🔁 Retry job started at {Time}", DateTime.UtcNow);

    // Yalnızca başarısız kayıtları al
    var failedRecords = await _context.FailedSyncRecords
        .Where(f => f.Status == "FAILED")
        .OrderBy(f => f.Id)
        .Take(50)
        .ToListAsync();

    if (!failedRecords.Any())
    {
        _logger.LogInformation("No failed records found for retry.");
        return;
    }

    foreach (var record in failedRecords)
    {
        try
        {
            _logger.LogInformation("Retrying failed sync record {Id} of type {Type}", record.Id, record.RecordType);

            switch (record.RecordType.ToUpperInvariant())
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
                default:
                    _logger.LogWarning("Unknown record type: {Type}", record.RecordType);
                    break;
            }

            record.Status = "RESOLVED";
            record.ResolvedAt = DateTime.UtcNow;
            record.RetryCount += 1;
        }
        catch (Exception ex)
        {
            record.RetryCount += 1;
            record.LastRetryAt = DateTime.UtcNow;
            record.Status = "FAILED";
            record.ErrorMessage = ex.Message;

            _logger.LogError(ex, "Error retrying record {Id}", record.Id);
        }
    }

    await _context.SaveChangesAsync();
    _logger.LogInformation("Retry job completed at {Time}", DateTime.UtcNow);
}

}
