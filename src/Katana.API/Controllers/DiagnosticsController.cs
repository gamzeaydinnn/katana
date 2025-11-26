using Katana.Data.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Katana.API.Controllers;

[ApiController]
[Route("api/adminpanel/diagnostics")]
public class DiagnosticsController : ControllerBase
{
    private readonly IntegrationDbContext _db;
    private readonly ILogger<DiagnosticsController> _logger;
    private readonly IWebHostEnvironment _env;

    public DiagnosticsController(IntegrationDbContext db, ILogger<DiagnosticsController> logger, IWebHostEnvironment env)
    {
        _db = db;
        _logger = logger;
        _env = env;
    }

    [HttpGet("katana-luca-issues")]
    public async Task<IActionResult> GetKatanaLucaIssues(CancellationToken ct = default)
    {
        var result = new Dictionary<string, object?>();

        
        try
        {
            var logsDir = Path.Combine(_env.ContentRootPath, "logs");
            if (!Directory.Exists(logsDir))
            {
                
                logsDir = Path.Combine(_env.ContentRootPath, "src", "Katana.API", "logs");
            }

            var skipped = new List<object>();
            if (Directory.Exists(logsDir))
            {
                var files = Directory.GetFiles(logsDir, "*app-*.log").OrderByDescending(f => f).Take(6);
                foreach (var f in files)
                {
                    try
                    {
                        var lines = System.IO.File.ReadAllLines(f);
                        foreach (var l in lines)
                        {
                            if (l.Contains("ExtractorService => Product skipped." ) || l.Contains("Product skipped. SKU="))
                            {
                                skipped.Add(new { file = Path.GetFileName(f), line = l });
                                if (skipped.Count >= 200) break;
                            }
                        }
                        if (skipped.Count >= 200) break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Diagnostics => Failed reading log file {File}", f);
                    }
                }
            }

            result["skippedProductsFromLogs"] = skipped;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Diagnostics => Error while parsing logs");
            result["skippedProductsFromLogs_error"] = ex.Message;
        }

        
        try
        {
            var recentIntegration = await _db.IntegrationLogs
                .AsNoTracking()
                .Where(l => l.StartTime >= DateTime.UtcNow.AddDays(-7))
                .OrderByDescending(l => l.StartTime)
                .Take(100)
                .Select(l => new
                {
                    l.Id,
                    l.SyncType,
                    l.Status,
                    l.StartTime,
                    l.EndTime,
                    l.ProcessedRecords,
                    l.SuccessfulRecords,
                    l.FailedRecordsCount,
                    l.ErrorMessage
                })
                .ToListAsync(ct);

            var failedIds = recentIntegration.Where(i => i.FailedRecordsCount > 0).Select(i => i.Id).ToList();
            var failedRecords = new List<object>();
            if (failedIds.Any())
            {
                var recs = await _db.FailedSyncRecords
                    .AsNoTracking()
                    .Where(r => failedIds.Contains(r.IntegrationLogId))
                    .OrderByDescending(r => r.FailedAt)
                    .Take(200)
                    .Select(r => new { r.Id, r.IntegrationLogId, r.RecordType, r.RecordId, r.ErrorMessage, r.FailedAt })
                    .ToListAsync(ct);
                failedRecords.AddRange(recs);
            }

            result["recentIntegrationLogs"] = recentIntegration;
            result["recentFailedRecords"] = failedRecords;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Diagnostics => Error while querying IntegrationLogs");
            result["integrationLogs_error"] = ex.Message;
        }

        return Ok(result);
    }
}
