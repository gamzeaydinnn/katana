using Katana.Business.DTOs;
using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
/*Amacı: Kullanıcının arayüzden tek tuşla senkronizasyonu tetiklemesini sağlamak ve logları görüntülemek.*/
namespace Katana.API.Controllers;
/*Sorumlulukları (Yeni):

Belirli bir veri türü (örn: Stok, Fatura) için manuel senkronizasyonu başlatan endpoint (POST /api/sync/run/{type}).

Tüm senkronizasyon geçmişini (logları) getiren endpoint (GET /api/sync/history).

Belirli bir senkronizasyon işleminin detaylarını (başarılı/hatalı kayıtlar) getiren endpoint (GET /api/sync/history/{logId}).*/
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SyncController : ControllerBase
{
    private readonly ISyncService _syncService;
    private readonly ILogger<SyncController> _logger;

    public SyncController(ISyncService syncService, ILogger<SyncController> logger)
    {
        _syncService = syncService;
        _logger = logger;
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

