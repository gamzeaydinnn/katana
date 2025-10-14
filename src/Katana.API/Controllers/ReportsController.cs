using Katana.Data.Context;
using Katana.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Katana.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IntegrationDbContext _context;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IntegrationDbContext context, ILogger<ReportsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets the latest integration logs
    /// </summary>
    [HttpGet("logs")]
    public async Task<ActionResult<object>> GetIntegrationLogs(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 50,
        [FromQuery] string? syncType = null,
        [FromQuery] string? status = null)
    {
        try
        {
            var query = _context.IntegrationLogs.AsQueryable();

            if (!string.IsNullOrEmpty(syncType))
            {
                query = query.Where(l => l.SyncType == syncType.ToUpper());
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(l => l.Status == status.ToUpper());
            }

            var totalCount = await query.CountAsync();
            var logs = await query
                .OrderByDescending(l => l.StartTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new
                {
                    l.Id,
                    l.SyncType,
                    l.Status,
                    l.StartTime,
                    l.EndTime,
                    l.Duration,
                    l.ProcessedRecords,
                    l.SuccessfulRecords,
                    l.FailedRecords,
                    l.ErrorMessage,
                    l.TriggeredBy
                })
                .ToListAsync();

            return Ok(new
            {
                logs,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving integration logs");
            return StatusCode(500, new { error = "Internal server error retrieving logs" });
        }
    }

    /// <summary>
    /// Gets the last sync report for each sync type
    /// </summary>
    [HttpGet("last")]
    public async Task<ActionResult<object>> GetLastSyncReports()
    {
        try
        {
            var syncTypes = new[] { "STOCK", "INVOICE", "CUSTOMER" };
            var reports = new List<object>();

            foreach (var syncType in syncTypes)
            {
                var lastLog = await _context.IntegrationLogs
                    .Where(l => l.SyncType == syncType)
                    .OrderByDescending(l => l.StartTime)
                    .FirstOrDefaultAsync();

                if (lastLog != null)
                {
                    reports.Add(new
                    {
                        syncType,
                        lastLog.Status,
                        lastLog.StartTime,
                        lastLog.EndTime,
                        lastLog.Duration,
                        lastLog.ProcessedRecords,
                        lastLog.SuccessfulRecords,
                        lastLog.FailedRecords,
                        lastLog.ErrorMessage
                    });
                }
                else
                {
                    reports.Add(new
                    {
                        syncType,
                        Status = "NEVER_RUN",
                        StartTime = (DateTime?)null,
                        EndTime = (DateTime?)null,
                        Duration = (TimeSpan?)null,
                        ProcessedRecords = 0,
                        SuccessfulRecords = 0,
                        FailedRecords = 0,
                        ErrorMessage = (string?)null
                    });
                }
            }

            return Ok(reports);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving last sync reports");
            return StatusCode(500, new { error = "Internal server error retrieving reports" });
        }
    }

    /// <summary>
    /// Gets failed sync records that need attention
    /// </summary>
    [HttpGet("failed")]
    public async Task<ActionResult<object>> GetFailedRecords(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? recordType = null)
    {
        try
        {
            var query = _context.FailedSyncRecords
                .Include(f => f.IntegrationLog)
                .Where(f => f.Status == "FAILED");

            if (!string.IsNullOrEmpty(recordType))
            {
                query = query.Where(f => f.RecordType == recordType.ToUpper());
            }

            var totalCount = await query.CountAsync();
            var failedRecords = await query
                .OrderByDescending(f => f.FailedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new
                {
                    f.Id,
                    f.RecordType,
                    f.RecordId,
                    f.ErrorMessage,
                    f.ErrorCode,
                    f.FailedAt,
                    f.RetryCount,
                    f.LastRetryAt,
                    f.NextRetryAt,
                    f.Status,
                    IntegrationLog = new
                    {
                        f.IntegrationLog.SyncType,
                        f.IntegrationLog.StartTime
                    }
                })
                .ToListAsync();

            return Ok(new
            {
                failedRecords,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving failed records");
            return StatusCode(500, new { error = "Internal server error retrieving failed records" });
        }
    }

    /// <summary>
    /// Gets sync statistics for dashboard
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<object>> GetSyncStatistics([FromQuery] int days = 7)
    {
        try
        {
            var fromDate = DateTime.UtcNow.AddDays(-days);
            
            var statistics = await _context.IntegrationLogs
                .Where(l => l.StartTime >= fromDate)
                .GroupBy(l => l.SyncType)
                .Select(g => new
                {
                    SyncType = g.Key,
                    TotalRuns = g.Count(),
                    SuccessfulRuns = g.Count(l => l.Status == "SUCCESS"),
                    FailedRuns = g.Count(l => l.Status == "FAILED"),
                    TotalProcessedRecords = g.Sum(l => l.ProcessedRecords),
                    TotalSuccessfulRecords = g.Sum(l => l.SuccessfulRecords),
                    TotalFailedRecords = g.Sum(l => l.FailedRecordsCount),
                    AverageDuration = g.Where(l => l.Duration.HasValue)
                                     .Average(l => l.Duration!.Value.TotalSeconds),
                    LastRunTime = g.Max(l => l.StartTime)
                })
                .ToListAsync();

            var overallStats = new
            {
                TotalSyncRuns = await _context.IntegrationLogs.CountAsync(l => l.StartTime >= fromDate),
                TotalFailedRecords = await _context.FailedSyncRecords.CountAsync(f => f.FailedAt >= fromDate),
                ActiveFailedRecords = await _context.FailedSyncRecords.CountAsync(f => f.Status == "FAILED"),
                SyncTypeStatistics = statistics
            };

            return Ok(overallStats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sync statistics");
            return StatusCode(500, new { error = "Internal server error retrieving statistics" });
        }
    }
}

