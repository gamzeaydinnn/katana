using Katana.Business.Interfaces;
using Katana.Core.Enums;
using Katana.Data.Context;
using Katana.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Katana.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LogsController : ControllerBase
{
    private readonly IntegrationDbContext _context;
    private readonly ILogger<LogsController> _logger;
    private readonly ILoggingService _loggingService;

    public LogsController(IntegrationDbContext context, ILogger<LogsController> logger, ILoggingService loggingService)
    {
        _context = context;
        _logger = logger;
        _loggingService = loggingService;
    }

    /// <summary>
    /// GET /api/Logs/errors - Error loglarını getirir (sayfalama + filtreleme)
    /// </summary>
    [HttpGet("errors")]
    public async Task<IActionResult> GetErrorLogs(
        [FromQuery] int pageSize = 50,
        [FromQuery] string? level = null,
        [FromQuery] string? category = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] DateTime? cursorCreatedAt = null,
        [FromQuery] int? cursorId = null)
    {
        try
        {
            _loggingService.LogInfo("Error logs requested (keyset pagination)", User?.Identity?.Name, "GetErrorLogs", LogCategory.UserAction);

            var query = _context.ErrorLogs.AsQueryable();

            if (!string.IsNullOrEmpty(level))
                query = query.Where(e => e.Level == level);

            if (!string.IsNullOrEmpty(category))
                query = query.Where(e => e.Category == category);

            if (fromDate.HasValue)
                query = query.Where(e => e.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(e => e.CreatedAt <= toDate.Value);

            if (cursorCreatedAt.HasValue && cursorId.HasValue)
            {
                var createdAt = cursorCreatedAt.Value;
                var id = cursorId.Value;
                query = query.Where(e => e.CreatedAt < createdAt || (e.CreatedAt == createdAt && e.Id < id));
            }

            pageSize = Math.Clamp(pageSize, 1, 200);
            var orderedQuery = query
                .OrderByDescending(e => e.CreatedAt)
                .ThenByDescending(e => e.Id);

            var items = await orderedQuery
                .Take(pageSize + 1)
                .Select(e => new
                {
                    e.Id,
                    e.Level,
                    e.Category,
                    e.Message,
                    e.User,
                    e.ContextData,
                    e.CreatedAt
                })
                .ToListAsync();

            var hasMore = items.Count > pageSize;
            var logs = hasMore ? items.Take(pageSize).ToList() : items;
            var nextCursor = hasMore
                ? new
                {
                    createdAt = logs.Last().CreatedAt,
                    id = logs.Last().Id
                }
                : null;

            var total = await _context.ErrorLogs.CountAsync();

            return Ok(new
            {
                logs,
                total,
                pageSize,
                nextCursor
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching error logs");
            _loggingService.LogError("Failed to fetch error logs", ex, User?.Identity?.Name, null, LogCategory.System);
            return StatusCode(500, new { error = "Failed to fetch logs" });
        }
    }

    /// <summary>
    /// GET /api/Logs/audits - Audit loglarını getirir (sayfalama + filtreleme)
    /// </summary>
    [HttpGet("audits")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int pageSize = 50,
        [FromQuery] string? actionType = null,
        [FromQuery] string? entityName = null,
        [FromQuery] string? performedBy = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] DateTime? cursorTimestamp = null,
        [FromQuery] int? cursorId = null)
    {
        try
        {
            _loggingService.LogInfo("Audit logs requested (keyset pagination)", User?.Identity?.Name, "GetAuditLogs", LogCategory.UserAction);

            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(actionType))
                query = query.Where(a => a.ActionType == actionType);

            if (!string.IsNullOrEmpty(entityName))
                query = query.Where(a => a.EntityName == entityName);

            if (!string.IsNullOrEmpty(performedBy))
                query = query.Where(a => a.PerformedBy == performedBy);

            if (fromDate.HasValue)
                query = query.Where(a => a.Timestamp >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(a => a.Timestamp <= toDate.Value);

            if (cursorTimestamp.HasValue && cursorId.HasValue)
            {
                var timestamp = cursorTimestamp.Value;
                var id = cursorId.Value;
                query = query.Where(a => a.Timestamp < timestamp || (a.Timestamp == timestamp && a.Id < id));
            }

            pageSize = Math.Clamp(pageSize, 1, 200);
            var orderedQuery = query
                .OrderByDescending(a => a.Timestamp)
                .ThenByDescending(a => a.Id);

            var items = await orderedQuery
                .Take(pageSize + 1)
                .Select(a => new
                {
                    a.Id,
                    a.ActionType,
                    a.EntityName,
                    a.EntityId,
                    a.PerformedBy,
                    a.Details,
                    a.IpAddress,
                    a.Timestamp
                })
                .ToListAsync();

            var hasMore = items.Count > pageSize;
            var logs = hasMore ? items.Take(pageSize).ToList() : items;
            var nextCursor = hasMore
                ? new
                {
                    timestamp = logs.Last().Timestamp,
                    id = logs.Last().Id
                }
                : null;

            var total = await _context.AuditLogs.CountAsync();

            return Ok(new
            {
                logs,
                total,
                pageSize,
                nextCursor
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching audit logs");
            _loggingService.LogError("Failed to fetch audit logs", ex, User?.Identity?.Name, null, LogCategory.System);
            return StatusCode(500, new { error = "Failed to fetch logs" });
        }
    }

    /// <summary>
    /// GET /api/Logs/stats - Log istatistikleri
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetLogStats([FromQuery] DateTime? fromDate = null)
    {
        try
        {
            var startDate = fromDate ?? DateTime.UtcNow.AddDays(-7);

            var errorStats = await _context.ErrorLogs
                .Where(e => e.CreatedAt >= startDate)
                .GroupBy(e => e.Level)
                .Select(g => new { level = g.Key, count = g.Count() })
                .ToListAsync();

            var auditStats = await _context.AuditLogs
                .Where(a => a.Timestamp >= startDate)
                .GroupBy(a => a.ActionType)
                .Select(g => new { actionType = g.Key, count = g.Count() })
                .ToListAsync();

            var categoryStats = await _context.ErrorLogs
                .Where(e => e.CreatedAt >= startDate && e.Category != null)
                .GroupBy(e => e.Category)
                .Select(g => new { category = g.Key, count = g.Count() })
                .ToListAsync();

            return Ok(new 
            { 
                errorStats, 
                auditStats, 
                categoryStats,
                period = $"Last {(DateTime.UtcNow - startDate).Days} days"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching log stats");
            return StatusCode(500, new { error = "Failed to fetch stats" });
        }
    }

    /// <summary>
    /// POST /api/Logs/frontend-error - Frontend hatalarını loglar
    /// </summary>
    [HttpPost("frontend-error")]
    public IActionResult LogFrontendError([FromBody] FrontendErrorDto error)
    {
        try
        {
            var contextData = System.Text.Json.JsonSerializer.Serialize(new
            {
                error.url,
                error.userAgent,
                error.stack,
                error.componentStack,
                error.timestamp
            });

            _loggingService.LogError(
                $"Frontend Error: {error.message}",
                null,
                "frontend",
                contextData,
                LogCategory.System
            );

            return Ok(new { message = "Error logged successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log frontend error");
            return StatusCode(500, new { error = "Failed to log error" });
        }
    }

    [HttpDelete("clear-old-errors")]
    public IActionResult ClearOldErrors()
    {
        var oldErrors = _context.ErrorLogs
            .Where(e => e.Message.Contains("IOrderService") || e.Message.Contains("AdminController"))
            .ToList();

        if (oldErrors.Any())
        {
            _context.ErrorLogs.RemoveRange(oldErrors);
            _context.SaveChanges();
            return Ok(new { message = $"{oldErrors.Count} eski hata silindi." });
        }

        return Ok(new { message = "Silinecek hata bulunamadı." });
    }
}

public class FrontendErrorDto
{
    public string message { get; set; } = string.Empty;
    public string? stack { get; set; }
    public string? componentStack { get; set; }
    public string url { get; set; } = string.Empty;
    public string userAgent { get; set; } = string.Empty;
    public string timestamp { get; set; } = string.Empty;
}
