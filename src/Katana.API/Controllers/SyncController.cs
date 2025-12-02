using Katana.Business.DTOs;
using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Enums;
using Katana.Data.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Katana.API.Controllers;




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

    
    
    
    [HttpGet("history")]
    [AllowAnonymous] 
    public async Task<IActionResult> GetSyncHistory()
    {
        try
        {
            var logs = await _context.SyncOperationLogs
                .OrderByDescending(l => l.StartTime)
                .Take(50)
                .ToListAsync();

            var turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows() ? "Turkey Standard Time" : "Europe/Istanbul"
            );

            var result = logs.Select(l => new
            {
                id = l.Id,
                syncType = l.SyncType,
                status = l.Status,
                startTime = TimeZoneInfo.ConvertTimeFromUtc(l.StartTime, turkeyTimeZone),
                endTime = l.EndTime.HasValue ? TimeZoneInfo.ConvertTimeFromUtc(l.EndTime.Value, turkeyTimeZone) : (DateTime?)null,
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
            return StatusCode(500, new { message = "Sync geçmişi alınamadı" });
        }
    }

    
    
    
    [HttpPost("start")]
    [Authorize(Roles = "Admin,StokYonetici")]
    public async Task<IActionResult> StartSync([FromBody] StartSyncRequest request)
    {
        try
        {
            var user = User?.Identity?.Name ?? "System";
            var normalizedType = (request.SyncType ?? string.Empty).Trim();
            var syncKey = normalizedType.ToUpperInvariant();
            syncKey = syncKey switch
            {
                "FATURA" or "FATURA SENKRONIZASYONU" or "FATURA SENKRONİZASYONU" => "INVOICE",
                "STOK" or "STOK SENKRONIZASYONU" or "STOK SENKRONİZASYONU" => "STOCK",
                "MUSTERI" or "MÜŞTERI" or "MÜŞTERİ" or "MÜŞTERI SENKRONIZASYONU" or "MÜŞTERİ SENKRONİZASYONU" => "CUSTOMER",
                "TÜMÜ" or "TUMU" or "TÜM SENKRONIZASYON" or "TÜM SENKRONİZASYON" => "ALL",
                _ => syncKey
            };

            _logger.LogInformation("Senkronizasyon başlatılıyor: {SyncType}", normalizedType);
            _loggingService.LogInfo($"Senkronizasyon başlatıldı: {normalizedType}", user, "StartSync", LogCategory.Sync);
            
            
            _auditService.LogSync(string.IsNullOrEmpty(normalizedType) ? "UNKNOWN" : normalizedType, user, $"Manuel senkronizasyon başlatıldı");
            
            var result = syncKey switch
            {
                "STOCK" => await _syncService.SyncStockAsync(null),
                "INVOICE" => await _syncService.SyncInvoicesAsync(null),
                "CUSTOMER" => await _syncService.SyncCustomersAsync(null),
                "DESPATCH" => await _syncService.SyncDespatchFromLucaAsync(null),

                "PRODUCT" => await _syncService.SyncProductsToLucaAsync(new SyncOptionsDto()),
                "STOCK_CARD" => await _syncService.SyncProductsToLucaAsync(new SyncOptionsDto()),
                
                "SUPPLIER" => await _syncService.SyncSuppliersToKozaAsync(),
                "WAREHOUSE" => await _syncService.SyncWarehousesToKozaAsync(),
                "CUSTOMER_LUCA" => await _syncService.SyncCustomersToLucaAsync(),
                
                "CUSTOMER_TRANSACTION" => new SyncResultDto { IsSuccess = true, Message = "Cari hareket senkronizasyonu tetiklendi (placeholder)", SuccessfulRecords = 0, ProcessedRecords = 0, FailedRecords = 0, SyncType = "CUSTOMER_TRANSACTION" },
                "CREDIT_CARD" => new SyncResultDto { IsSuccess = true, Message = "Kredi kartı girişi senkronizasyonu tetiklendi (placeholder)", SuccessfulRecords = 0, ProcessedRecords = 0, FailedRecords = 0, SyncType = "CREDIT_CARD" },
                "SALES_ORDER" => new SyncResultDto { IsSuccess = true, Message = "Satış siparişi senkronizasyonu tetiklendi (placeholder)", SuccessfulRecords = 0, ProcessedRecords = 0, FailedRecords = 0, SyncType = "SALES_ORDER" },
                "PURCHASE_ORDER" => new SyncResultDto { IsSuccess = true, Message = "Satınalma siparişi senkronizasyonu tetiklendi (placeholder)", SuccessfulRecords = 0, ProcessedRecords = 0, FailedRecords = 0, SyncType = "PURCHASE_ORDER" },
                "WAREHOUSE_TRANSFER" => new SyncResultDto { IsSuccess = true, Message = "Depo transferi senkronizasyonu tetiklendi (placeholder)", SuccessfulRecords = 0, ProcessedRecords = 0, FailedRecords = 0, SyncType = "WAREHOUSE_TRANSFER" },
                "BANK" => new SyncResultDto { IsSuccess = true, Message = "Banka kartları senkronizasyonu tetiklendi (placeholder)", SuccessfulRecords = 0, ProcessedRecords = 0, FailedRecords = 0, SyncType = "BANK" },
                "ALL" => await ConvertBatchResult(await _syncService.SyncAllAsync(null)),
                _ => new SyncResultDto { IsSuccess = true, Message = $"Sync tetiklendi (passthrough): {normalizedType}", SuccessfulRecords = 0, ProcessedRecords = 0, FailedRecords = 0, SyncType = syncKey }
            };

            _loggingService.LogInfo($"Senkronizasyon tamamlandı: {request.SyncType} - Başarılı: {result.IsSuccess}", user, 
                $"Kayıtlar: {result.SuccessfulRecords}", LogCategory.Sync);
            return Ok(new { success = result.IsSuccess, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Senkronizasyon başlatılırken hata oluştu");
            _loggingService.LogError($"Senkronizasyon başarısız: {request.SyncType}", ex, User?.Identity?.Name, null, LogCategory.Sync);
            return StatusCode(500, new { 
                message = "Sync başlatılamadı",
                error = ex.Message,
                innerError = ex.InnerException?.Message,
                type = ex.GetType().Name,
                stackTrace = ex.StackTrace?.Split('\n').Take(3).ToArray()
            });
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

    
    
    
    [HttpPost("to-luca/stock-cards")]
    public async Task<ActionResult<SyncResultDto>> SyncProductsToLuca(
        [FromBody] SyncOptionsDto? options = null,
        [FromQuery] DateTime? fromDate = null)
    {
        try
        {
            options ??= new SyncOptionsDto();
            
            _logger.LogInformation(
                "API üzerinden Katana → Luca ürün senkronizasyonu tetiklendi. Limit={Limit}, DryRun={DryRun}, ForceSendDuplicates={ForceSendDuplicates}",
                options.Limit, options.DryRun, options.ForceSendDuplicates);
            
            var result = await _syncService.SyncProductsToLucaAsync(options);

            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Katana → Luca ürün senkronizasyonu hata verdi");
            return StatusCode(500, new { error = "Sunucu hata verdi: ürün senkronizasyonu sırasında" });
        }
    }

    
    
    
    [HttpGet("status")]
    [AllowAnonymous] 
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

    
    
    
    [HttpGet("status/{syncType}")]
    [AllowAnonymous] 
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

    
    
    
    
    
    [HttpPost("from-luca/stock")]
    [AllowAnonymous] 
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

    
    
    
    [HttpPost("from-luca/despatch")]
    public async Task<ActionResult<SyncResultDto>> SyncDespatchFromLuca([FromQuery] DateTime? fromDate = null)
    {
        try
        {
            _logger.LogInformation("Luca → Katana despatch (irsaliye) sync triggered via API");
            var result = await _syncService.SyncDespatchFromLucaAsync(fromDate);

            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Luca → Katana despatch sync failed");
            return StatusCode(500, new { error = "Sunucu hata verdi: Luca'dan irsaliye senkronizasyonu sırasında" });
        }
    }

    
    
    
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
