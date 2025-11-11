using Katana.Business.DTOs;
using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Enums;
using Katana.Data.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Katana.API.Controllers;

/// <summary>
/// Manual and status endpoints for synchronization operations. Admin-only.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class SyncController : ControllerBase
{
    private readonly ISyncService _syncService;
    private readonly IntegrationDbContext _context;
    private readonly ILogger<SyncController> _logger;
    private readonly ILoggingService _loggingService;
    private readonly IAuditService _auditService;

    public SyncController(ISyncService syncService, IntegrationDbContext context, ILogger<SyncController> logger, 
        ILoggingService loggingService, IAuditService auditService)
    {
        _syncService = syncService;
        _context = context;
        _logger = logger;
        _loggingService = loggingService;
        _auditService = auditService;
    }

    /// <summary>
    /// GET /api/Sync/history - Senkronizasyon ge�mi�ini getirir
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetSyncHistory()
    {
        try
        {
            // Tablo yoksa bo� liste d�nd�r
            var logs = await _context.SyncOperationLogs
                .OrderByDescending(l => l.StartTime)
                .Take(50)
                .ToListAsync();

            var result = logs.Select(l => new
            {
                id = l.Id,
                syncType = l.SyncType,
                status = l.Status,
                startTime = TimeZoneInfo.ConvertTimeFromUtc(l.StartTime, TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time")),
                endTime = l.EndTime.HasValue ? TimeZoneInfo.ConvertTimeFromUtc(l.EndTime.Value, TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time")) : (DateTime?)null,
                processedRecords = l.ProcessedRecords,
                successfulRecords = l.SuccessfulRecords,
                failedRecords = l.FailedRecords,
                errorMessage = l.ErrorMessage
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sync history");
            return StatusCode(500, new { message = "Sync ge�mi�i al�namad�" });
        }
    }

    /// <summary>
    /// POST /api/Sync/start - Yeni senkronizasyon ba�lat�r
    /// </summary>
    [HttpPost("start")]
    public async Task<IActionResult> StartSync([FromBody] StartSyncRequest request)
    {
        try
        {
            var user = User?.Identity?.Name ?? "System";
            _logger.LogInformation("Senkronizasyon başlatılıyor: {SyncType}", request.SyncType);
            _loggingService.LogInfo($"Senkronizasyon başlatıldı: {request.SyncType}", user, "StartSync", LogCategory.Sync);
            
            // Audit log: Sync ba�lat�ld�
            _auditService.LogSync(request.SyncType ?? "UNKNOWN", user, $"Manuel senkronizasyon başlatıldı");
            
            var result = request.SyncType?.ToUpper() switch
            {
                "STOCK" => await _syncService.SyncStockAsync(null),
                "INVOICE" => await _syncService.SyncInvoicesAsync(null),
                "CUSTOMER" => await _syncService.SyncCustomersAsync(null),
                "ALL" => await ConvertBatchResult(await _syncService.SyncAllAsync(null)),
                _ => throw new ArgumentException("Ge�ersiz sync tipi")
            };

            _loggingService.LogInfo($"Senkronizasyon tamamlandı: {request.SyncType} - Başarılı: {result.IsSuccess}", user, 
                $"Kayıtlar: {result.SuccessfulRecords}", LogCategory.Sync);
            return Ok(new { success = result.IsSuccess, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Senkronizasyon başlatılırken hata oluştu");
            _loggingService.LogError($"Senkronizasyon başarısız: {request.SyncType}", ex, User?.Identity?.Name, null, LogCategory.Sync);
            return StatusCode(500, new { message = "Sync ba�lat�lamad�" });
        }
    }

    private Task<SyncResultDto> ConvertBatchResult(BatchSyncResultDto batch)
    {
        return Task.FromResult(new SyncResultDto
        {
            IsSuccess = batch.OverallSuccess,
            Message = $"Toplam {batch.TotalProcessedRecords} kay�t i�lendi",
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
            _logger.LogInformation("API üzerinden manuel senkronizasyon tetiklendi");
            var result = await _syncService.SyncAllAsync(fromDate);
            
            if (result.OverallSuccess)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tam senkronizasyon çalıştırılırken hata oluştu");
            return StatusCode(500, new { error = "Sunucu hata verdi: senkronizasyon sırasında" });
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
            _logger.LogInformation("API üzerinden manuel stok senkronizasyonu tetiklendi");
            var result = await _syncService.SyncStockAsync(fromDate);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stok senkronizasyonu çalıştırılırken hata oluştu");
            return StatusCode(500, new { error = "Sunucu hata verdi: stok senkronizasyonu sırasında" });
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
            _logger.LogInformation("API üzerinden manuel fatura senkronizasyonu tetiklendi");
            var result = await _syncService.SyncInvoicesAsync(fromDate);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatura senkronizasyonu çalıştırılırken hata oluştu");
            return StatusCode(500, new { error = "Sunucu hata verdi: fatura senkronizasyonu sırasında" });
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
            _logger.LogInformation("API üzerinden manuel müşteri senkronizasyonu tetiklendi");
            var result = await _syncService.SyncCustomersAsync(fromDate);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Müşteri senkronizasyonu çalıştırılırken hata oluştu");
            return StatusCode(500, new { error = "Sunucu hata verdi: müşteri senkronizasyonu sırasında" });
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
            _logger.LogError(ex, "Senkronizasyon durumu alınırken hata oluştu");
            return StatusCode(500, new { error = "Sunucu hata verdi: senkronizasyon durumu alınamadı" });
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
            _logger.LogError(ex, "{SyncType} için senkronizasyon durumu kontrol edilirken hata oluştu", syncType);
            return StatusCode(500, new { error = "Sunucu hata verdi: senkronizasyon durumu kontrol edilemedi" });
        }
    }

    // Luca → Katana (Reverse Sync) endpoints
    
    /// <summary>
    /// POST /api/Sync/from-luca/stock - Luca'dan stok hareketlerini çeker
    /// </summary>
    [HttpPost("from-luca/stock")]
    [AllowAnonymous] // Temporarily for testing
    public async Task<ActionResult<SyncResultDto>> SyncStockFromLuca([FromQuery] DateTime? fromDate = null)
    {
        try
        {
            _logger.LogInformation("Luca → Katana stock sync triggered via API");
            var result = await _syncService.SyncStockFromLucaAsync(fromDate);
            
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Luca → Katana stock sync failed");
            return StatusCode(500, new { error = "Sunucu hata verdi: Luca'dan stok senkronizasyonu sırasında" });
        }
    }

    /// <summary>
    /// POST /api/Sync/from-luca/invoices - Luca'dan faturaları çeker
    /// </summary>
    [HttpPost("from-luca/invoices")]
    public async Task<ActionResult<SyncResultDto>> SyncInvoicesFromLuca([FromQuery] DateTime? fromDate = null)
    {
        try
        {
            _logger.LogInformation("Luca → Katana invoice sync triggered via API");
            var result = await _syncService.SyncInvoicesFromLucaAsync(fromDate);
            
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Luca → Katana invoice sync failed");
            return StatusCode(500, new { error = "Sunucu hata verdi: Luca'dan fatura senkronizasyonu sırasında" });
        }
    }

    /// <summary>
    /// POST /api/Sync/from-luca/customers - Luca'dan müşterileri çeker
    /// </summary>
    [HttpPost("from-luca/customers")]
    public async Task<ActionResult<SyncResultDto>> SyncCustomersFromLuca([FromQuery] DateTime? fromDate = null)
    {
        try
        {
            _logger.LogInformation("Luca → Katana customer sync triggered via API");
            var result = await _syncService.SyncCustomersFromLucaAsync(fromDate);
            
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Luca → Katana customer sync failed");
            return StatusCode(500, new { error = "Sunucu hata verdi: Luca'dan müşteri senkronizasyonu sırasında" });
        }
    }

    /// <summary>
    /// POST /api/Sync/from-luca/all - Luca'dan tüm verileri çeker
    /// </summary>
    [HttpPost("from-luca/all")]
    public async Task<ActionResult<BatchSyncResultDto>> SyncAllFromLuca([FromQuery] DateTime? fromDate = null)
    {
        try
        {
            _logger.LogInformation("Luca → Katana complete sync triggered via API");
            var result = await _syncService.SyncAllFromLucaAsync(fromDate);
            
            return result.OverallSuccess ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Luca → Katana complete sync failed");
            return StatusCode(500, new { error = "Sunucu hata verdi: Luca'dan tam senkronizasyon sırasında" });
        }
    }
}

public class StartSyncRequest
{
    public string SyncType { get; set; } = string.Empty;
}
