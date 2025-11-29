using Katana.Data.Context;
using Katana.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Katana.Data.Repositories;

public class FailedSyncRecordRepository
{
    private readonly IntegrationDbContext _context;
    private readonly ILogger<FailedSyncRecordRepository> _logger;

    public FailedSyncRecordRepository(IntegrationDbContext context, ILogger<FailedSyncRecordRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    
    
    
    public async Task AddFailedRecordAsync(int integrationLogId, string recordType, string recordId, string originalData, string errorMessage, string? errorCode = null)
    {
        var failedRecord = new FailedSyncRecord
        {
            IntegrationLogId = integrationLogId,
            RecordType = recordType,
            RecordId = recordId,
            OriginalData = originalData,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode,
            FailedAt = DateTime.UtcNow,
            Status = "FAILED"
        };

        _context.FailedSyncRecords.Add(failedRecord);
        await _context.SaveChangesAsync();

        _logger.LogWarning("Failed sync record added: {RecordType} - {RecordId}", recordType, recordId);
    }

    
    
    
    public async Task<List<FailedSyncRecord>> GetRetryableRecordsAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.FailedSyncRecords
            .Where(r => (r.Status == "FAILED" || r.Status == "RETRYING") && r.NextRetryAt != null && r.NextRetryAt <= now)
            .OrderBy(r => r.NextRetryAt)
            .Take(50)
            .ToListAsync();
    }

    
    
    
    public async Task ScheduleRetryAsync(int recordId, int delayMinutes = 30)
    {
        var record = await _context.FailedSyncRecords.FindAsync(recordId);
        if (record == null)
        {
            _logger.LogWarning("Retry schedule attempted for non-existent record Id {Id}", recordId);
            return;
        }

        record.Status = "RETRYING";
        record.RetryCount += 1;
        record.LastRetryAt = DateTime.UtcNow;
        record.NextRetryAt = DateTime.UtcNow.AddMinutes(delayMinutes);

        await _context.SaveChangesAsync();
        _logger.LogInformation("Retry scheduled for record {Id} at {Time}", recordId, record.NextRetryAt);
    }

    
    
    
    public async Task MarkAsResolvedAsync(int recordId, string? resolution = null, string? resolvedBy = "System")
    {
        var record = await _context.FailedSyncRecords.FindAsync(recordId);
        if (record == null) return;

        record.Status = "RESOLVED";
        record.Resolution = resolution ?? "Retry succeeded";
        record.ResolvedAt = DateTime.UtcNow;
        record.ResolvedBy = resolvedBy;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Failed record {Id} marked as resolved", recordId);
    }

    
    
    
    public async Task<int> CleanupOldFailedRecordsAsync(int daysToKeep = 30)
    {
        var threshold = DateTime.UtcNow.AddDays(-daysToKeep);
        var oldRecords = await _context.FailedSyncRecords
            .Where(r => r.FailedAt < threshold && r.Status == "RESOLVED")
            .ToListAsync();

        _context.FailedSyncRecords.RemoveRange(oldRecords);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Cleaned up {Count} old resolved failed records", oldRecords.Count);
        return oldRecords.Count;
    }
}
