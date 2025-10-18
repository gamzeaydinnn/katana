using Katana.Core.Enums;
using Katana.Data.Context;
using Katana.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Katana.Data.Repositories;

/// <summary>
/// Senkronizasyon süreçlerinin log kayıtlarını yönetir (IntegrationLog tablosu).
/// </summary>
public class IntegrationLogRepository
{
    private readonly IntegrationDbContext _context;
    private readonly ILogger<IntegrationLogRepository> _logger;

    public IntegrationLogRepository(IntegrationDbContext context, ILogger<IntegrationLogRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Yeni bir senkronizasyon logu oluşturur (başlangıç aşaması).
    /// </summary>
    public async Task<IntegrationLog> CreateLogAsync(string syncType, string triggeredBy = "System")
    {
        var log = new IntegrationLog
        {
            SyncType = syncType,
            Status = SyncStatus.Running,
            StartTime = DateTime.UtcNow,
            TriggeredBy = triggeredBy
        };

        _context.IntegrationLogs.Add(log);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Integration log created for {SyncType} (Id={Id})", syncType, log.Id);
        return log;
    }

    /// <summary>
    /// Belirtilen log kaydını "başarılı" olarak günceller.
    /// </summary>
    public async Task MarkAsSuccessAsync(int logId, int processed, int success, int failed)
    {
        var log = await _context.IntegrationLogs.FindAsync(logId);
        if (log == null)
        {
            _logger.LogWarning("Integration log not found: {Id}", logId);
            return;
        }

        log.Status = SyncStatus.Success;
        log.EndTime = DateTime.UtcNow;
        log.ProcessedRecords = processed;
        log.SuccessfulRecords = success;
        log.FailedRecordsCount = failed;
        log.ErrorMessage = null;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Integration log {Id} marked as SUCCESS", logId);
    }

    /// <summary>
    /// Belirtilen log kaydını hata bilgisiyle birlikte "FAILED" durumuna geçirir.
    /// </summary>
    public async Task MarkAsFailedAsync(int logId, string errorMessage)
    {
        var log = await _context.IntegrationLogs.FindAsync(logId);
        if (log == null)
        {
            _logger.LogWarning("Integration log not found: {Id}", logId);
            return;
        }

        log.Status = SyncStatus.Failed;
        log.EndTime = DateTime.UtcNow;
        log.ErrorMessage = errorMessage;

        await _context.SaveChangesAsync();
        _logger.LogError("Integration log {Id} marked as FAILED: {Error}", logId, errorMessage);
    }

    /// <summary>
    /// En son senkronizasyon loglarını getirir.
    /// </summary>
    public async Task<List<IntegrationLog>> GetRecentLogsAsync(int count = 20)
    {
        return await _context.IntegrationLogs
            .OrderByDescending(l => l.StartTime)
            .Take(count)
            .ToListAsync();
    }

    /// <summary>
    /// Belirli bir türdeki (ör. STOCK, INVOICE) son başarılı logu döner.
    /// </summary>
    public async Task<IntegrationLog?> GetLastSuccessfulLogAsync(string syncType)
    {
        return await _context.IntegrationLogs
            .Where(l => l.SyncType == syncType && l.Status == SyncStatus.Success)
            .OrderByDescending(l => l.EndTime)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Hâlen devam eden (RUNNING) bir senkronizasyon var mı kontrol eder.
    /// </summary>
    public async Task<bool> IsSyncRunningAsync(string syncType)
    {
        return await _context.IntegrationLogs
            .AnyAsync(l => l.SyncType == syncType && l.Status == SyncStatus.Running);
    }

    /// <summary>
    /// Eski log kayıtlarını temizler (ör. 30 günden eski olanları).
    /// </summary>
    public async Task<int> CleanupOldLogsAsync(int daysToKeep = 30)
    {
        var threshold = DateTime.UtcNow.AddDays(-daysToKeep);
        var oldLogs = await _context.IntegrationLogs
            .Where(l => l.EndTime < threshold)
            .ToListAsync();

        _context.IntegrationLogs.RemoveRange(oldLogs);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Cleaned up {Count} old integration logs", oldLogs.Count);
        return oldLogs.Count;
    }
}
