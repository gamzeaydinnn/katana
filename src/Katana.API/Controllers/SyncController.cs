using Katana.Business.DTOs;
using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Data.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Katana.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class SyncController : ControllerBase
{
    private readonly ISyncService _syncService;
    private readonly IntegrationDbContext _context;
    private readonly ILogger<SyncController> _logger;

    public SyncController(ISyncService syncService, IntegrationDbContext context, ILogger<SyncController> logger)
    {
        _syncService = syncService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/Sync/history - Senkronizasyon geçmişini getirir
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetSyncHistory()
    {
        try
        {
            // Tablo yoksa boş liste döndür
            var logs = await _context.SyncOperationLogs
                .OrderByDescending(l => l.StartTime)
                .Take(50)
                .Select(l => new
                {
                    id = l.Id,
                    syncType = l.SyncType,
                    status = l.Status,
                    startTime = l.StartTime,
                    endTime = l.EndTime,
                    successCount = l.SuccessfulRecords,
                    failCount = l.FailedRecords,
                    errorMessage = l.ErrorMessage
                })
                .ToListAsync();

            return Ok(logs);
        }
        catch (Microsoft.Data.Sqlite.SqliteException)
        {
            // Tablo henüz oluşturulmamış, boş liste döndür
            return Ok(new List<object>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sync history");
            return StatusCode(500, new { message = "Sync geçmişi alınamadı" });
        }
    }

    /// <summary>
    /// POST /api/Sync/start - Yeni senkronizasyon başlatır
    /// </summary>
    [HttpPost("start")]
    public async Task<IActionResult> StartSync([FromBody] StartSyncRequest request)
    {
        try
        {
            _logger.LogInformation("Starting sync: {SyncType}", request.SyncType);
            
            var result = request.SyncType?.ToUpper() switch
            {
                "STOCK" => await _syncService.SyncStockAsync(null),
                "INVOICE" => await _syncService.SyncInvoicesAsync(null),
                "CUSTOMER" => await _syncService.SyncCustomersAsync(null),
                "ALL" => await ConvertBatchResult(await _syncService.SyncAllAsync(null)),
                _ => throw new ArgumentException("Geçersiz sync tipi")
            };

            return Ok(new { success = result.IsSuccess, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting sync");
            return StatusCode(500, new { message = "Sync başlatılamadı" });
        }
    }

    private Task<SyncResultDto> ConvertBatchResult(BatchSyncResultDto batch)
    {
        return Task.FromResult(new SyncResultDto
        {
            IsSuccess = batch.OverallSuccess,
            Message = $"Toplam {batch.TotalProcessedRecords} kayıt işlendi",
            ProcessedRecords = batch.TotalProcessedRecords,
            SuccessfulRecords = batch.TotalSuccessfulRecords,
            FailedRecords = batch.TotalFailedRecords
        });
    }

    /// <summary>
    /// Runs complete synchronization for all data types
    /// </summary>
    [HttpPost("run")]
    public async Task<ActionResult<BatchSyncResultDto>> RunCompleteSync([FromQuery] DateTime? fromDate = null)
    {
        try
        {
            _logger.LogInformation("Manual sync triggered from API");
            var result = await _syncService.SyncAllAsync(fromDate);
            
            if (result.OverallSuccess)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running complete sync");
            return StatusCode(500, new { error = "Internal server error during sync operation" });
        }
    }

    /// <summary>
    /// Runs stock synchronization only
    /// </summary>
    [HttpPost("stock")]
    public async Task<ActionResult<SyncResultDto>> RunStockSync([FromQuery] DateTime? fromDate = null)
    {
        try
        {
            _logger.LogInformation("Manual stock sync triggered from API");
            var result = await _syncService.SyncStockAsync(fromDate);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running stock sync");
            return StatusCode(500, new { error = "Internal server error during stock sync" });
        }
    }

    /// <summary>
    /// Runs invoice synchronization only
    /// </summary>
    [HttpPost("invoices")]
    public async Task<ActionResult<SyncResultDto>> RunInvoiceSync([FromQuery] DateTime? fromDate = null)
    {
        try
        {
            _logger.LogInformation("Manual invoice sync triggered from API");
            var result = await _syncService.SyncInvoicesAsync(fromDate);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running invoice sync");
            return StatusCode(500, new { error = "Internal server error during invoice sync" });
        }
    }

    /// <summary>
    /// Runs customer synchronization only
    /// </summary>
    [HttpPost("customers")]
    public async Task<ActionResult<SyncResultDto>> RunCustomerSync([FromQuery] DateTime? fromDate = null)
    {
        try
        {
            _logger.LogInformation("Manual customer sync triggered from API");
            var result = await _syncService.SyncCustomersAsync(fromDate);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running customer sync");
            return StatusCode(500, new { error = "Internal server error during customer sync" });
        }
    }

    /// <summary>
    /// Gets the status of all sync operations
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<List<SyncStatusDto>>> GetSyncStatus()
    {
        try
        {
            var status = await _syncService.GetSyncStatusAsync();
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sync status");
            return StatusCode(500, new { error = "Internal server error getting sync status" });
        }
    }

    /// <summary>
    /// Checks if a specific sync type is currently running
    /// </summary>
    [HttpGet("status/{syncType}")]
    public async Task<ActionResult<object>> GetSyncTypeStatus(string syncType)
    {
        try
        {
            var isRunning = await _syncService.IsSyncRunningAsync(syncType.ToUpper());
            return Ok(new { syncType = syncType.ToUpper(), isRunning });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking sync status for {SyncType}", syncType);
            return StatusCode(500, new { error = "Internal server error checking sync status" });
        }
    }
}

public class StartSyncRequest
{
    public string SyncType { get; set; } = string.Empty;
}

