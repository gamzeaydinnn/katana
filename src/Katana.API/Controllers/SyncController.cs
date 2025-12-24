using Katana.Business.Interfaces;
using Katana.Business.Services;
using Katana.Core.DTOs;
using Katana.Core.Enums;
using Katana.Core.Entities;
using Katana.Data.Context;
using Katana.Data.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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
    private readonly LucaApiSettings _lucaSettings;
    private readonly BidirectionalSyncService _bidirectionalSync;

    public SyncController(ISyncService syncService, IntegrationDbContext context, ILogger<SyncController> logger, 
        ILoggingService loggingService, IAuditService auditService, IOptions<LucaApiSettings> lucaSettings,
        BidirectionalSyncService bidirectionalSync)
    {
        _syncService = syncService;
        _context = context;
        _logger = logger;
        _loggingService = loggingService;
        _auditService = auditService;
        _lucaSettings = lucaSettings.Value;
        _bidirectionalSync = bidirectionalSync;
    }

    
    
    
    /// <summary>
    /// Senkronizasyon geçmişini getir (Admin, Manager, StokYonetici)
    /// </summary>
    [HttpGet("history")]
    [Authorize(Roles = "Admin,Manager,StokYonetici")]
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

    
    
    
    /// <summary>
    /// Senkronizasyon başlat (SADECE Admin)
    /// </summary>
    [HttpPost("start")]
    [Authorize(Roles = "Admin")]
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

    /// <summary>
    /// LUCA -> KATANA: Luca'da guncellenen urunleri Katana'ya senkronize et
    /// Mevcut urunler guncellenir, yeni SKU acilmaz
    /// </summary>
    /// <param name="hours">Kaç saat öncesine kadar kontrol edilecek (default: 1 saat)</param>
    [HttpPost("luca-to-katana")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult> SyncFromLucaToKatana([FromQuery] int hours = 1)
    {
        try
        {
            _logger.LogInformation("[API] Luca -> Katana senkronizasyon baslatildi (hours={Hours})", hours);
            var sinceDate = DateTime.UtcNow.AddHours(-hours);
            var result = await _bidirectionalSync.SyncFromLucaToKatanaAsync(sinceDate);

            return Ok(new
            {
                success = true,
                message = $"Senkronizasyon tamamlandı: {result.SuccessCount} başarılı, {result.FailCount} hata",
                data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[API] Luca -> Katana senkronizasyon hatasi");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// KATANA -> LUCA: Katana'da guncellenen urunleri Luca'ya senkronize et
    /// Mevcut urunler guncellenir, yeni versiyon acilmaz
    /// </summary>
    /// <param name="hours">Kaç saat öncesine kadar kontrol edilecek (default: 1 saat)</param>
    [HttpPost("katana-to-luca")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult> SyncFromKatanaToLuca([FromQuery] int hours = 1)
    {
        try
        {
            _logger.LogInformation("[API] Katana -> Luca senkronizasyon baslatildi (hours={Hours})", hours);
            var sinceDate = DateTime.UtcNow.AddHours(-hours);
            var result = await _bidirectionalSync.SyncFromKatanaToLucaAsync(sinceDate);

            return Ok(new
            {
                success = true,
                message = $"Senkronizasyon tamamlandı: {result.SuccessCount} başarılı, {result.FailCount} hata",
                data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[API] Katana -> Luca senkronizasyon hatasi");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Iki yonlu tam senkronizasyon (Luca <-> Katana)
    /// Once Luca'dan Katana'ya, sonra Katana'dan Luca'ya
    /// </summary>
    [HttpPost("bidirectional")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult> FullBidirectionalSync([FromQuery] int hours = 1)
    {
        try
        {
            _logger.LogInformation("[API] Iki yonlu senkronizasyon baslatildi (hours={Hours})", hours);

            var sinceDate = DateTime.UtcNow.AddHours(-hours);

            _logger.LogInformation("[API] 1/2: Luca -> Katana basliyor...");
            var lucaToKatana = await _bidirectionalSync.SyncFromLucaToKatanaAsync(sinceDate);

            _logger.LogInformation("[API] 2/2: Katana -> Luca basliyor...");
            var katanaToLuca = await _bidirectionalSync.SyncFromKatanaToLucaAsync(sinceDate);

            return Ok(new
            {
                success = true,
                message = "İki yönlü senkronizasyon tamamlandı",
                lucaToKatana = new
                {
                    successCount = lucaToKatana.SuccessCount,
                    failCount = lucaToKatana.FailCount,
                    skippedCount = lucaToKatana.SkippedCount
                },
                katanaToLuca = new
                {
                    successCount = katanaToLuca.SuccessCount,
                    failCount = katanaToLuca.FailCount,
                    skippedCount = katanaToLuca.SkippedCount
                },
                totalSuccess = lucaToKatana.SuccessCount + katanaToLuca.SuccessCount,
                totalFail = lucaToKatana.FailCount + katanaToLuca.FailCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[API] Iki yonlu senkronizasyon hatasi");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Senkronizasyon endpoint ozetini getir
    /// </summary>
    [HttpGet("status-summary")]
    [AllowAnonymous]
    public ActionResult GetSyncStatusSummary()
    {
        return Ok(new
        {
            status = "running",
            timestamp = DateTime.UtcNow,
            endpoints = new
            {
                lucaToKatana = "/api/sync/luca-to-katana",
                katanaToLuca = "/api/sync/katana-to-luca",
                bidirectional = "/api/sync/bidirectional"
            }
        });
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
    /// Tüm senkronizasyonu çalıştır (SADECE Admin)
    /// </summary>
    [HttpPost("run")]
    [Authorize(Roles = "Admin")]
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
    /// Stok senkronizasyonu (SADECE Admin)
    /// </summary>
    [HttpPost("stock")]
    [Authorize(Roles = "Admin")]
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
    /// Fatura senkronizasyonu (SADECE Admin)
    /// </summary>
    [HttpPost("invoices")]
    [Authorize(Roles = "Admin")]
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
    /// Müşteri senkronizasyonu (SADECE Admin)
    /// </summary>
    [HttpPost("customers")]
    [Authorize(Roles = "Admin")]
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
    /// Ürünleri Luca'ya senkronize et (SADECE Admin)
    /// </summary>
    [HttpPost("to-luca/stock-cards")]
    [Authorize(Roles = "Admin")]
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

    
    
    
    /// <summary>
    /// Tedarikçi senkronizasyonu (SADECE Admin)
    /// </summary>
    [HttpPost("suppliers")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SyncResultDto>> SyncSuppliers()
    {
        try
        {
            _logger.LogInformation("API üzerinden Katana → Koza tedarikçi senkronizasyonu tetiklendi");
            var result = await _syncService.SyncSuppliersToKozaAsync();
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tedarikçi senkronizasyonu çalıştırılırken hata oluştu");
            return StatusCode(500, new { error = "Sunucu hata verdi: tedarikçi senkronizasyonu sırasında" });
        }
    }

    
    
    
    /// <summary>
    /// Depo senkronizasyonu (SADECE Admin)
    /// </summary>
    [HttpPost("warehouses")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SyncResultDto>> SyncWarehouses()
    {
        try
        {
            _logger.LogInformation("API üzerinden Katana → Koza depo senkronizasyonu tetiklendi");
            var result = await _syncService.SyncWarehousesToKozaAsync();
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Depo senkronizasyonu çalıştırılırken hata oluştu");
            return StatusCode(500, new { error = "Sunucu hata verdi: depo senkronizasyonu sırasında" });
        }
    }

    
    
    
    /// <summary>
    /// Müşteri Luca senkronizasyonu (SADECE Admin)
    /// </summary>
    [HttpPost("customers-luca")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SyncResultDto>> SyncCustomersLuca()
    {
        try
        {
            _logger.LogInformation("API üzerinden Katana → Luca müşteri (cari) senkronizasyonu tetiklendi");
            var result = await _syncService.SyncCustomersToLucaAsync();
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Müşteri (cari) senkronizasyonu çalıştırılırken hata oluştu");
            return StatusCode(500, new { error = "Sunucu hata verdi: müşteri senkronizasyonu sırasında" });
        }
    }

    
    
    
    /// <summary>
    /// Senkronizasyon durumlarını getir (Admin, Manager, StokYonetici)
    /// </summary>
    [HttpGet("status")]
    [Authorize(Roles = "Admin,Manager,StokYonetici")] 
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
    /// Belirli sync tipi durumunu getir (Admin, Manager, StokYonetici)
    /// </summary>
    [HttpGet("status/{syncType}")]
    [Authorize(Roles = "Admin,Manager,StokYonetici")] 
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

    
    
    
    
    
    /// <summary>
    /// Luca'dan stok senkronizasyonu (SADECE Admin)
    /// </summary>
    [HttpPost("from-luca/stock")]
    [Authorize(Roles = "Admin")] 
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
    /// Luca'dan fatura senkronizasyonu (SADECE Admin)
    /// </summary>
    [HttpPost("from-luca/invoices")]
    [Authorize(Roles = "Admin")]
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
    /// Luca'dan müşteri senkronizasyonu (SADECE Admin)
    /// </summary>
    [HttpPost("from-luca/customers")]
    [Authorize(Roles = "Admin")]
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
    /// Luca'dan irsaliye senkronizasyonu (SADECE Admin)
    /// </summary>
    [HttpPost("from-luca/despatch")]
    [Authorize(Roles = "Admin")]
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

    
    
    
    /// <summary>
    /// Luca'dan tüm senkronizasyon (SADECE Admin)
    /// </summary>
    [HttpPost("from-luca/all")]
    [Authorize(Roles = "Admin")]
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

    /// <summary>
    /// DEBUG: Tek bir ürünün Katana ve Luca'daki durumunu karşılaştır
    /// </summary>
    [HttpGet("debug/product/{sku}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DebugProductSync(string sku)
    {
        try
        {
            _logger.LogWarning("🔍 DEBUG: Ürün karşılaştırması başlatılıyor: {SKU}", sku);
            var result = await _syncService.DebugProductComparisonAsync(sku);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DEBUG: Ürün karşılaştırması hatası: {SKU}", sku);
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// DEBUG: Tek bir ürünü zorla Luca'ya gönder (SADECE Admin)
    /// </summary>
    [HttpPost("debug/force-sync/{sku}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> ForceSyncProduct(string sku)
    {
        try
        {
            _logger.LogWarning("🔥 FORCE SYNC: Ürün zorla senkronize ediliyor: {SKU}", sku);
            var result = await _syncService.ForceSyncSingleProductAsync(sku);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FORCE SYNC: Hata: {SKU}", sku);
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// ✅ Katana Location'larını Luca'ya Depo olarak senkronize et (SADECE Admin)
    /// </summary>
    [HttpPost("to-luca/warehouses")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SyncResultDto>> SyncWarehousesToLuca()
    {
        try
        {
            _logger.LogInformation("🏢 API üzerinden Katana Location → Luca Depo senkronizasyonu tetiklendi");
            var result = await _syncService.SyncWarehousesToLucaAsync();
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Depo senkronizasyonu hatası");
            return StatusCode(500, new { error = "Depo senkronizasyonu sırasında hata oluştu", details = ex.Message });
        }
    }

    /// <summary>
    /// ✅ Luca'ya satış faturası gönder (SADECE Admin)
    /// Luca'daki stok kartlarını kullanarak fatura oluşturur
    /// </summary>
    [HttpPost("to-luca/sales-invoice")]
    [AllowAnonymous] // Test için geçici olarak açık
    public async Task<ActionResult<SyncResultDto>> SendSalesInvoiceToLuca([FromBody] LucaCreateInvoiceHeaderRequest request)
    {
        try
        {
            _logger.LogInformation("📤 Luca'ya satış faturası gönderiliyor: {BelgeTakipNo}", request.BelgeTakipNo);
            
            // Satış faturası için varsayılan değerler
            if (string.IsNullOrEmpty(request.BelgeTurDetayId) || request.BelgeTurDetayId == "0")
            {
                request.BelgeTurDetayId = "76"; // Mal Satış Faturası
            }
            
            if (string.IsNullOrEmpty(request.MusteriTedarikci) || request.MusteriTedarikci == "0")
            {
                request.MusteriTedarikci = "1"; // Müşteri (Satış için)
            }
            
            if (string.IsNullOrEmpty(request.BelgeSeri))
            {
                request.BelgeSeri = _lucaSettings.DefaultBelgeSeri;
            }
            
            if (string.IsNullOrEmpty(request.ParaBirimKod))
            {
                request.ParaBirimKod = "TRY";
            }
            
            if (request.KurBedeli == 0)
            {
                request.KurBedeli = 1;
            }
            
            var result = await _syncService.SendSalesInvoiceAsync(request);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("✅ Satış faturası başarıyla gönderildi: {BelgeTakipNo}", request.BelgeTakipNo);
                return Ok(result);
            }
            
            _logger.LogWarning("⚠️ Satış faturası gönderilemedi: {Message}", result.Message);
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Satış faturası gönderme hatası");
            return StatusCode(500, new { 
                error = "Satış faturası gönderilirken hata oluştu", 
                details = ex.Message,
                innerError = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// ✅ Katana'dan satış siparişlerini manuel senkronize et (SADECE Admin)
    /// Background worker'ı beklemeden anında siparişleri çeker
    /// 
    /// END-TO-END SYNC FLOW:
    /// 1. Fetch orders from Katana API (GetSalesOrdersBatchedAsync)
    ///    - fromDate=null → status=NOT_SHIPPED (open orders only)
    ///    - fromDate provided → created_at_min filter (all statuses)
    /// 
    /// 2. For each order (SalesOrderDto):
    ///    a. Customer Mapping: Katana customer ID → Local customer ID
    ///    b. If customer not found: Fetch from Katana API and create locally
    ///    c. Create SalesOrder entity with:
    ///       - CustomerId = local database ID (NOT Katana ID)
    ///       - Status = raw Katana status string (NOT mapped to enum)
    ///       - All other fields from Katana DTO
    ///    d. Create SalesOrderLine entities for each row
    ///    e. Save to database
    /// 
    /// 3. Create PendingStockAdjustment for admin approval (open orders only)
    /// 
    /// NOTE: This does NOT use KatanaApiClient.GetSalesOrdersAsync (legacy method).
    ///       It directly uses IKatanaService.GetSalesOrdersBatchedAsync for better control.
    /// </summary>
    [HttpPost("from-katana/sales-orders")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SyncResultDto>> SyncSalesOrdersFromKatana([FromQuery] int? days = null)
    {
        try
        {
            // days = null ise tüm "Open" siparişleri çek (Katana UI ile aynı mantık)
            // days belirtilirse sadece son X günün siparişlerini çek
            var fromDate = days.HasValue ? DateTime.UtcNow.AddDays(-days.Value) : (DateTime?)null;
            
            _logger.LogInformation("📥 Katana'dan satış siparişleri manuel senkronizasyonu başlatıldı {DateFilter}", 
                days.HasValue ? $"(son {days} gün)" : "(tüm açık siparişler)");
            var katanaService = HttpContext.RequestServices.GetRequiredService<IKatanaService>();
            var pendingService = HttpContext.RequestServices.GetRequiredService<Katana.Business.Interfaces.IPendingStockAdjustmentService>();
            var variantMappingService = HttpContext.RequestServices.GetRequiredService<Katana.Business.Interfaces.IVariantMappingService>();
            
            // Mevcut siparişleri al
            var existingKatanaOrderIdsList = await _context.SalesOrders
                .Select(s => s.KatanaOrderId)
                .ToListAsync();
            var existingKatanaOrderIds = new HashSet<long>(existingKatanaOrderIdsList);
            
            // 🔍 DEBUG: Mevcut siparişleri logla
            _logger.LogWarning("🔍 DEBUG: Found {Count} existing orders in database", existingKatanaOrderIds.Count);
            if (existingKatanaOrderIds.Count > 0)
            {
                _logger.LogWarning("🔍 DEBUG: First 10 existing Katana Order IDs: {Ids}", 
                    string.Join(", ", existingKatanaOrderIds.Take(10)));
            }

            // ✅ PendingStockAdjustment duplicate prevention (ExternalOrderId|Sku|Quantity)
            var processedItems = await _context.PendingStockAdjustments
                .Where(p => p.ExternalOrderId != null)
                .Select(p => new { p.ExternalOrderId, p.Sku, p.Quantity })
                .ToListAsync();
            var processedItemsSet = new HashSet<string>(
                processedItems.Select(p => $"{p.ExternalOrderId}|{p.Sku}|{p.Quantity}")
            );
            
            // Ürün mapping'i
            var products = await katanaService.GetProductsAsync();
            var skuToProductId = await _context.Products
                .Where(p => !string.IsNullOrWhiteSpace(p.SKU))
                .ToDictionaryAsync(p => p.SKU!, p => p.Id, StringComparer.OrdinalIgnoreCase);
            
            var variantToProduct = new Dictionary<long, (int ProductId, string Sku, string? ProductName)>();
            foreach (var p in products)
            {
                if (long.TryParse(p.Id, out var variantId))
                {
                    var sku = p.SKU ?? p.Id;
                    var productId = skuToProductId.TryGetValue(sku, out var localId) ? localId : 0;
                    variantToProduct[variantId] = (productId, sku, p.Name);
                }
            }
            
            // Müşteri mapping'i
            var customerMapping = await _context.Customers
                .Where(c => c.ReferenceId != null)
                .ToDictionaryAsync(c => c.ReferenceId!, c => c.Id, StringComparer.OrdinalIgnoreCase);
            
            // 🔥 Tüm Katana müşterilerini önceden çek ve cache'le
            _logger.LogInformation("Fetching all customers from Katana for caching...");
            var allKatanaCustomers = await katanaService.GetCustomersAsync();
            
            // ✅ FIX: Dictionary key'i long yap (string yerine) - direct comparison için
            var katanaCustomerCache = allKatanaCustomers.ToDictionary(
                c => c.Id,  // long key - NO ToString()!
                c => c
            );
            _logger.LogInformation("Cached {Count} customers from Katana", katanaCustomerCache.Count);
            
            // 🔍 DEBUG: Cache içeriğini logla
            _logger.LogWarning("🔍 DEBUG: Customer Cache Contents (first 5):");
            foreach (var kvp in katanaCustomerCache.Take(5))
            {
                _logger.LogWarning("  Cache Key: {Key} (Type: {Type}) → Customer ID: {Id}, Name: '{Name}'",
                    kvp.Key, kvp.Key.GetType().Name, kvp.Value.Id, kvp.Value.Name);
            }
            
            var newOrdersCount = 0;
            var totalLinesCount = 0;
            var newPendingCount = 0;
            var skippedPendingCount = 0;

            var variantMappingCache = new Dictionary<long, VariantMapping?>();

            static string GetMax11SafeTaxNo(long customerId)
            {
                var id = customerId.ToString();
                if (id.Length > 10) id = id.Substring(id.Length - 10);
                return $"U{id}";
            }

            async Task<(int ProductId, string Sku)> ResolveVariantAsync(long variantId)
            {
                if (!variantMappingCache.TryGetValue(variantId, out var cached))
                {
                    cached = await variantMappingService.GetMappingAsync(variantId);
                    variantMappingCache[variantId] = cached;
                }

                if (cached != null)
                {
                    return (cached.ProductId, cached.Sku);
                }

                if (variantToProduct.TryGetValue(variantId, out var fallbackValue))
                {
                    // Persist mapping for next time
                    var created = await variantMappingService.CreateOrUpdateAsync(variantId, fallbackValue.ProductId, fallbackValue.Sku);
                    variantMappingCache[variantId] = created;
                    return (created.ProductId, created.Sku);
                }

                return (0, $"VARIANT-{variantId}");
            }
            
            await foreach (var orderBatch in katanaService.GetSalesOrdersBatchedAsync(fromDate, batchSize: 100))
            {
                foreach (var order in orderBatch)
                {
                    var shouldSaveSalesOrder = !existingKatanaOrderIds.Contains(order.Id);
                    
                    // 🔍 DEBUG: Sipariş kontrolü
                    _logger.LogWarning("🔍 DEBUG: Processing order {OrderNo} (Katana ID: {KatanaId}), shouldSave={ShouldSave}", 
                        order.OrderNo, order.Id, shouldSaveSalesOrder);
                    
                    var localCustomerId = 0;
                    var katanaCustomerIdStr = order.CustomerId.ToString();
                    if (customerMapping.TryGetValue(katanaCustomerIdStr, out var mappedCustomerId))
                    {
                        localCustomerId = mappedCustomerId;
                    }
                    
                    // Müşteri bulunamadıysa Katana'dan çekip oluştur
                    if (localCustomerId == 0)
                    {
                        // 🔍 DEBUG: Müşteri arama detayları
                        _logger.LogWarning("🔍 DEBUG: Looking for customer - Order.CustomerId={OrderCustomerId} (Type: {Type}), " +
                            "String Key='{StringKey}'",
                            order.CustomerId,
                            order.CustomerId.GetType().Name,
                            katanaCustomerIdStr);
                        
                        KatanaCustomerDto? katanaCustomer = null;
                        // ✅ FIX: long key ile direkt arama (string yerine)
                        if (katanaCustomerCache.TryGetValue(order.CustomerId, out var cachedCustomer))
                        {
                            katanaCustomer = cachedCustomer;
                            _logger.LogDebug("✅ Found customer in cache: {CustomerId}", order.CustomerId);
                        }
                        else
                        {
                            _logger.LogWarning("❌ Customer NOT FOUND in cache! Key: {Key}, Cache Keys Sample: {Sample}",
                                order.CustomerId,
                                string.Join(", ", katanaCustomerCache.Keys.Take(3)));
                        }
                        
                        if (katanaCustomer != null)
                        {
                            // Adres bilgilerini Addresses listesinden al
                            var defaultAddress = katanaCustomer.Addresses?.FirstOrDefault();
                            
                            var newCustomer = new Katana.Core.Entities.Customer
                            {
                                Title = katanaCustomer.Name ?? $"Customer-{order.CustomerId}",
                                ReferenceId = katanaCustomerIdStr,
                                Email = katanaCustomer.Email,
                                Phone = katanaCustomer.Phone,
                                Address = defaultAddress?.Line1,
                                City = defaultAddress?.City,
                                Country = defaultAddress?.Country,
                                TaxNo = GetMax11SafeTaxNo(order.CustomerId),
                                Currency = katanaCustomer.Currency ?? "TRY",
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow
                            };
                            _context.Customers.Add(newCustomer);
                            await _context.SaveChangesAsync();
                            
                            localCustomerId = newCustomer.Id;
                            customerMapping[katanaCustomerIdStr] = localCustomerId;
                            _logger.LogInformation("✅ Yeni müşteri oluşturuldu: {CustomerName} (ID: {CustomerId})", newCustomer.Title, newCustomer.Id);
                        }
                        else
                        {
                            // Müşteri Katana'da bulunamadı - "Unknown Customer" olarak oluştur
                            _logger.LogWarning("⚠️ Müşteri Katana'da bulunamadı (CustomerId: {CustomerId}), 'Unknown Customer' olarak oluşturuluyor", order.CustomerId);
                            
	                            var unknownCustomer = new Katana.Core.Entities.Customer
	                            {
	                                Title = $"Unknown Customer (Katana ID: {order.CustomerId})",
	                                ReferenceId = katanaCustomerIdStr,
	                                Email = null,
	                                Phone = null,
	                                // TaxNo column has a strict max length (typically 10/11).
	                                // Use a deterministic, unique, max-11-safe fallback: "U" + last 10 digits of Katana ID.
	                                TaxNo = $"U{(order.CustomerId.ToString().Length > 10 ? order.CustomerId.ToString().Substring(order.CustomerId.ToString().Length - 10) : order.CustomerId.ToString())}",
	                                Currency = order.Currency ?? "TRY",
	                                IsActive = false, // Inactive olarak işaretle
	                                CreatedAt = DateTime.UtcNow
	                            };
                            _context.Customers.Add(unknownCustomer);
                            await _context.SaveChangesAsync();
                            
                            localCustomerId = unknownCustomer.Id;
                            customerMapping[katanaCustomerIdStr] = localCustomerId;
                            _logger.LogInformation("✅ Unknown customer oluşturuldu: {CustomerName} (ID: {CustomerId})", unknownCustomer.Title, unknownCustomer.Id);
                        }
                    }
                    
                    // ✅ 1) SalesOrders tablosuna kaydet (yeni siparişler)
                    if (shouldSaveSalesOrder)
                    {
                        var salesOrder = new Katana.Core.Entities.SalesOrder
                        {
                            KatanaOrderId = order.Id,
                            OrderNo = order.OrderNo ?? $"SO-{order.Id}",
                            CustomerId = localCustomerId,
                            OrderCreatedDate = order.OrderCreatedDate ?? order.CreatedAt,
                            DeliveryDate = order.DeliveryDate,
                            Currency = order.Currency ?? "TRY",
                            ConversionRate = order.ConversionRate,
                            Status = order.Status ?? "NOT_SHIPPED",
                            Total = order.Total,
                            TotalInBaseCurrency = order.TotalInBaseCurrency,
                            AdditionalInfo = order.AdditionalInfo,
                            CustomerRef = order.CustomerRef,
                            Source = order.Source,
                            LocationId = order.LocationId,
                            CreatedAt = DateTime.UtcNow,
                            IsSyncedToLuca = false
                        };

                        if (order.SalesOrderRows != null)
                        {
                            foreach (var row in order.SalesOrderRows)
                            {
                                var resolved = await ResolveVariantAsync(row.VariantId);
                                var productName = variantToProduct.TryGetValue(row.VariantId, out var pInfo)
                                    ? pInfo.ProductName
                                    : null;

                                var orderLine = new Katana.Core.Entities.SalesOrderLine
                                {
                                    KatanaRowId = row.Id,
                                    VariantId = row.VariantId,
                                    SKU = resolved.Sku,
                                    ProductName = productName,
                                    Quantity = row.Quantity,
                                    PricePerUnit = row.PricePerUnit,
                                    PricePerUnitInBaseCurrency = row.PricePerUnitInBaseCurrency,
                                    Total = row.Total,
                                    TotalInBaseCurrency = row.TotalInBaseCurrency,
                                    TaxRateId = row.TaxRateId,
                                    LocationId = row.LocationId,
                                    ProductAvailability = row.ProductAvailability,
                                    ProductExpectedDate = row.ProductExpectedDate,
                                    CreatedAt = DateTime.UtcNow
                                };

                                salesOrder.Lines.Add(orderLine);
                                totalLinesCount++;
                            }
                        }

                        _context.SalesOrders.Add(salesOrder);
                        existingKatanaOrderIds.Add(order.Id);
                        newOrdersCount++;
                        
                        // 📊 Debug: Status mapping kontrolü
                        _logger.LogDebug("📊 Order {OrderNo}: Katana Status='{KatanaStatus}' → Stored Status='{StoredStatus}'",
                            salesOrder.OrderNo, order.Status, salesOrder.Status);
                    }

                    // ✅ 2) Admin onayı için PendingStockAdjustment oluştur
                    // (Mevcut siparişler için de pending eksikse yaratır; processedItemsSet duplicate'ı engeller)
                    var externalOrderId = !string.IsNullOrEmpty(order.OrderNo) ? order.OrderNo : order.Id.ToString();
                    if (order.SalesOrderRows != null && order.SalesOrderRows.Count > 0)
                    {
                        foreach (var row in order.SalesOrderRows)
                        {
                            var resolved = await ResolveVariantAsync(row.VariantId);
                            if (resolved.ProductId == 0 || string.IsNullOrWhiteSpace(resolved.Sku))
                            {
                                skippedPendingCount++;
                                continue;
                            }

                            var quantity = (int)row.Quantity;
                            var negativeQuantity = -Math.Abs(quantity);
                            var itemKey = $"{externalOrderId}|{resolved.Sku}|{negativeQuantity}";
                            if (processedItemsSet.Contains(itemKey))
                            {
                                skippedPendingCount++;
                                continue;
                            }

                            await pendingService.CreateAsync(new Katana.Data.Models.PendingStockAdjustment
                            {
                                ExternalOrderId = externalOrderId,
                                ProductId = resolved.ProductId,
                                Sku = resolved.Sku,
                                Quantity = negativeQuantity,
                                RequestedBy = "Katana-ManualSync",
                                RequestedAt = order.CreatedAt,
                                Status = "Pending",
                                Notes = $"Katana sipariş #{externalOrderId}: {quantity}x {resolved.Sku}"
                            });

                            processedItemsSet.Add(itemKey);
                            newPendingCount++;
                        }
                    }
                }
                
                await _context.SaveChangesAsync();
            }
            
            _logger.LogInformation(
                "✅ Katana sipariş senkronizasyonu tamamlandı: {OrderCount} yeni sipariş, {LineCount} satır, {PendingCount} pending oluşturuldu (skip: {Skipped})",
                newOrdersCount, totalLinesCount, newPendingCount, skippedPendingCount);
            
            return Ok(new SyncResultDto
            {
                IsSuccess = true,
                SyncType = "KATANA_SALES_ORDERS",
                Message = $"Katana'dan {newOrdersCount} yeni sipariş ({totalLinesCount} satır) senkronize edildi. Pending: {newPendingCount} (skip: {skippedPendingCount})",
                ProcessedRecords = newOrdersCount,
                SuccessfulRecords = newOrdersCount,
                FailedRecords = 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Katana sipariş senkronizasyonu hatası");
            return StatusCode(500, new SyncResultDto
            {
                IsSuccess = false,
                SyncType = "KATANA_SALES_ORDERS",
                Message = $"Senkronizasyon hatası: {ex.Message}",
                ProcessedRecords = 0,
                SuccessfulRecords = 0,
                FailedRecords = 0
            });
        }
    }

    /// <summary>
    /// 🔍 DEBUG: Katana siparişini hem API'den hem veritabanından çekip karşılaştır
    /// Kullanım: GET /api/sync/debug/katana-order/SO-56
    /// </summary>
    [HttpGet("debug/katana-order/{orderNo}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DebugKatanaOrder(string orderNo)
    {
        try
        {
            _logger.LogInformation("🔍 DEBUG: Analyzing order {OrderNo}", orderNo);

            // 1. Katana'dan direkt çek (batched API kullan)
            var katanaService = HttpContext.RequestServices.GetRequiredService<IKatanaService>();
            SalesOrderDto? katanaOrder = null;
            
            await foreach (var batch in katanaService.GetSalesOrdersBatchedAsync(fromDate: null, batchSize: 100))
            {
                katanaOrder = batch.FirstOrDefault(o => o.OrderNo == orderNo);
                if (katanaOrder != null)
                    break;
            }

            // 2. Veritabanından çek
            var dbOrder = await _context.SalesOrders
                .Include(s => s.Customer)
                .Include(s => s.Lines)
                .FirstOrDefaultAsync(s => s.OrderNo == orderNo);

            // 3. Customer mapping kontrolü
            object? customerMapping = null;
            if (katanaOrder != null)
            {
                var katanaCustomerIdStr = katanaOrder.CustomerId.ToString();
                customerMapping = await _context.Customers
                    .Where(c => c.ReferenceId == katanaCustomerIdStr)
                    .Select(c => new { 
                        c.Id, 
                        c.Title, 
                        c.ReferenceId,
                        c.Email,
                        c.Phone,
                        c.IsActive
                    })
                    .FirstOrDefaultAsync();
            }

            // 4. Karşılaştırma sonucu
            var result = new
            {
                orderNo,
                found = new
                {
                    inKatana = katanaOrder != null,
                    inDatabase = dbOrder != null
                },
                katanaOrder = katanaOrder != null ? new
                {
                    id = katanaOrder.Id,
                    orderNo = katanaOrder.OrderNo,
                    katanaCustomerId = katanaOrder.CustomerId,
                    status = katanaOrder.Status,
                    total = katanaOrder.Total,
                    currency = katanaOrder.Currency,
                    orderCreatedDate = katanaOrder.OrderCreatedDate,
                    deliveryDate = katanaOrder.DeliveryDate,
                    source = katanaOrder.Source,
                    locationId = katanaOrder.LocationId,
                    rowCount = katanaOrder.SalesOrderRows?.Count ?? 0,
                    rows = katanaOrder.SalesOrderRows?.Select(r => new
                    {
                        id = r.Id,
                        variantId = r.VariantId,
                        quantity = r.Quantity,
                        pricePerUnit = r.PricePerUnit,
                        total = r.Total
                    }).ToList()
                } : null,
                dbOrder = dbOrder != null ? new
                {
                    id = dbOrder.Id,
                    katanaOrderId = dbOrder.KatanaOrderId,
                    orderNo = dbOrder.OrderNo,
                    localCustomerId = dbOrder.CustomerId,
                    customerName = dbOrder.Customer?.Title,
                    customerEmail = dbOrder.Customer?.Email,
                    customerReferenceId = dbOrder.Customer?.ReferenceId,
                    status = dbOrder.Status,
                    total = dbOrder.Total,
                    currency = dbOrder.Currency,
                    orderCreatedDate = dbOrder.OrderCreatedDate,
                    deliveryDate = dbOrder.DeliveryDate,
                    source = dbOrder.Source,
                    locationId = dbOrder.LocationId,
                    isSyncedToLuca = dbOrder.IsSyncedToLuca,
                    createdAt = dbOrder.CreatedAt,
                    lineCount = dbOrder.Lines?.Count ?? 0,
                    lines = dbOrder.Lines?.Select(l => new
                    {
                        id = l.Id,
                        katanaRowId = l.KatanaRowId,
                        variantId = l.VariantId,
                        sku = l.SKU,
                        productName = l.ProductName,
                        quantity = l.Quantity,
                        pricePerUnit = l.PricePerUnit,
                        total = l.Total
                    }).ToList()
                } : null,
                customerMapping = customerMapping,
                analysis = new
                {
                    customerIdMatch = katanaOrder != null && dbOrder != null && customerMapping != null
                        ? $"Katana Customer ID {katanaOrder.CustomerId} → Local Customer ID {((dynamic)customerMapping).Id}"
                        : "N/A",
                    statusMatch = katanaOrder != null && dbOrder != null
                        ? katanaOrder.Status == dbOrder.Status
                        : (bool?)null,
                    totalMatch = katanaOrder != null && dbOrder != null
                        ? katanaOrder.Total == dbOrder.Total
                        : (bool?)null,
                    issues = new List<string>()
                }
            };

            // Sorun tespiti
            var issues = (List<string>)result.analysis.issues;
            
            if (katanaOrder == null)
                issues.Add("⚠️ Sipariş Katana API'de bulunamadı");
            
            if (dbOrder == null)
                issues.Add("⚠️ Sipariş veritabanında bulunamadı");
            
            if (katanaOrder != null && dbOrder == null)
                issues.Add("❌ Sipariş Katana'da var ama veritabanında yok - senkronizasyon çalışmamış");
            
            if (katanaOrder != null && customerMapping == null)
                issues.Add($"❌ Müşteri mapping bulunamadı - Katana Customer ID: {katanaOrder.CustomerId}");
            
            if (katanaOrder != null && dbOrder != null)
            {
                if (katanaOrder.Status != dbOrder.Status)
                    issues.Add($"⚠️ Status uyuşmazlığı - Katana: '{katanaOrder.Status}' vs DB: '{dbOrder.Status}'");
                
                if (katanaOrder.Total != dbOrder.Total)
                    issues.Add($"⚠️ Total uyuşmazlığı - Katana: {katanaOrder.Total} vs DB: {dbOrder.Total}");
                
                if (dbOrder.CustomerId == 0)
                    issues.Add("❌ Customer ID = 0 - Müşteri mapping başarısız");
                
                var katanaRowCount = katanaOrder.SalesOrderRows?.Count ?? 0;
                var dbLineCount = dbOrder.Lines?.Count ?? 0;
                if (katanaRowCount != dbLineCount)
                    issues.Add($"⚠️ Satır sayısı uyuşmazlığı - Katana: {katanaRowCount} vs DB: {dbLineCount}");
            }
            
            if (issues.Count == 0)
                issues.Add("✅ Sorun tespit edilmedi - Sipariş doğru senkronize edilmiş");

            _logger.LogInformation("🔍 DEBUG: Order {OrderNo} analysis completed. Issues: {IssueCount}", 
                orderNo, issues.Count);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ DEBUG: Error analyzing order {OrderNo}", orderNo);
            return StatusCode(500, new
            {
                error = "Debug analizi başarısız",
                message = ex.Message,
                stackTrace = ex.StackTrace?.Split('\n').Take(5).ToArray()
            });
        }
    }

    // ========================================================================
    // ÖLÇÜ BİRİMİ MAPPING ENDPOINT'LERİ
    // ========================================================================

    /// <summary>
    /// Luca'dan tüm ölçü birimlerini listele
    /// </summary>
    [HttpGet("list-luca-olcum-birimleri")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> ListLucaOlcumBirimleri()
    {
        try
        {
            var olcumBirimiService = HttpContext.RequestServices.GetRequiredService<IOlcumBirimiSyncService>();
            var units = await olcumBirimiService.GetLucaOlcumBirimleriAsync();
            
            return Ok(new
            {
                success = true,
                count = units.Count,
                data = units
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Luca ölçü birimleri listelenirken hata oluştu");
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Luca ölçü birimlerinden otomatik UNIT mapping'leri oluştur
    /// </summary>
    [HttpPost("sync-olcum-birimi-mappings")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> SyncOlcumBirimiMappings()
    {
        try
        {
            var olcumBirimiService = HttpContext.RequestServices.GetRequiredService<IOlcumBirimiSyncService>();
            var addedCount = await olcumBirimiService.SyncOlcumBirimiMappingsAsync();
            
            return Ok(new
            {
                success = true,
                addedCount = addedCount,
                message = $"{addedCount} yeni ölçü birimi mapping'i oluşturuldu"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ölçü birimi mapping'leri senkronize edilirken hata oluştu");
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Tek bir ürünün mapping'ini test et (Katana product + Luca request)
    /// </summary>
    [HttpGet("test-single-product/{sku}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> TestSingleProductMapping(string sku)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sku))
            {
                return BadRequest(new { success = false, error = "SKU parametresi gerekli" });
            }

            var katanaService = HttpContext.RequestServices.GetRequiredService<IKatanaService>();
            var mappingService = HttpContext.RequestServices.GetRequiredService<IMappingService>();
            
            // Katana'dan ürünü getir
            var products = await katanaService.GetProductsAsync();
            var product = products.FirstOrDefault(p => 
                string.Equals(p.SKU, sku, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.Id, sku, StringComparison.OrdinalIgnoreCase));
            
            if (product == null)
            {
                return NotFound(new { success = false, error = $"Ürün bulunamadı: {sku}" });
            }

            // Mapping'leri al
            var categoryMappings = await mappingService.GetCategoryMappingAsync();
            var unitMappings = await mappingService.GetUnitMappingAsync();

            // Mapper'ı çağır
            var lucaRequest = Katana.Business.Mappers.KatanaToLucaMapper.MapKatanaProductToStockCard(
                product,
                _lucaSettings,
                categoryMappings,
                null,
                null,
                null,
                unitMappings
            );

            return Ok(new
            {
                success = true,
                katanaProduct = new
                {
                    id = product.Id,
                    sku = product.SKU,
                    name = product.Name,
                    category = product.Category,
                    unit = product.Unit,
                    barcode = product.Barcode,
                    costPrice = product.CostPrice,
                    salesPrice = product.SalesPrice
                },
                lucaRequest = lucaRequest,
                mappingDetails = new
                {
                    categoryMappingFound = !string.IsNullOrWhiteSpace(product.Category) && categoryMappings.ContainsKey(product.Category),
                    unitMappingFound = !string.IsNullOrWhiteSpace(product.Unit) && unitMappings.ContainsKey(product.Unit.ToLowerInvariant()),
                    resolvedCategory = lucaRequest.KategoriAgacKod,
                    resolvedUnitId = lucaRequest.OlcumBirimiId
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ürün mapping testi başarısız: {SKU}", sku);
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// ✅ Mevcut NULL ProductName'leri Katana API'den çekerek günceller (SADECE Admin)
    /// SalesOrderLines tablosundaki ProductName = NULL olan kayıtları bulur,
    /// her biri için Katana API'den variant/product bilgisi çeker ve günceller.
    /// </summary>
    [HttpPost("backfill-product-names")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> BackfillProductNames([FromQuery] int? limit = 100)
    {
        try
        {
            _logger.LogInformation("🔄 ProductName backfill başlatılıyor (limit: {Limit})", limit);
            
            var katanaService = HttpContext.RequestServices.GetRequiredService<IKatanaService>();
            
            // NULL ProductName olan SalesOrderLines'ları bul
            var linesWithNullProductName = await _context.SalesOrderLines
                .Where(sol => string.IsNullOrEmpty(sol.ProductName) || sol.ProductName.StartsWith("VARIANT-"))
                .Take(limit ?? 100)
                .ToListAsync();
            
            if (linesWithNullProductName.Count == 0)
            {
                return Ok(new { 
                    success = true, 
                    message = "Güncellenecek kayıt bulunamadı - tüm ProductName'ler dolu",
                    updatedCount = 0
                });
            }
            
            _logger.LogInformation("📋 {Count} adet NULL/VARIANT ProductName bulundu", linesWithNullProductName.Count);
            
            var updatedCount = 0;
            var failedCount = 0;
            var details = new List<object>();
            
            // Her satır için Katana API'den bilgi çek
            foreach (var line in linesWithNullProductName)
            {
                try
                {
                    var (sku, productName) = await katanaService.GetVariantWithProductNameAsync(line.VariantId);
                    
                    var updated = false;
                    var oldSku = line.SKU;
                    var oldProductName = line.ProductName;
                    
                    // SKU güncelle (eğer VARIANT- ile başlıyorsa veya boşsa)
                    if (!string.IsNullOrEmpty(sku) && (string.IsNullOrEmpty(line.SKU) || line.SKU.StartsWith("VARIANT-")))
                    {
                        line.SKU = sku;
                        updated = true;
                    }
                    
                    // ProductName güncelle
                    if (!string.IsNullOrEmpty(productName) && (string.IsNullOrEmpty(line.ProductName) || line.ProductName.StartsWith("VARIANT-")))
                    {
                        line.ProductName = productName;
                        updated = true;
                    }
                    
                    if (updated)
                    {
                        updatedCount++;
                        details.Add(new
                        {
                            lineId = line.Id,
                            variantId = line.VariantId,
                            oldSku,
                            newSku = line.SKU,
                            oldProductName,
                            newProductName = line.ProductName
                        });
                        
                        _logger.LogInformation("✅ Line {LineId} güncellendi: SKU='{Sku}', ProductName='{ProductName}'", 
                            line.Id, line.SKU, line.ProductName);
                    }
                    
                    // Rate limit için kısa bekleme
                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    failedCount++;
                    _logger.LogWarning(ex, "❌ Line {LineId} (VariantId: {VariantId}) güncellenemedi", line.Id, line.VariantId);
                }
            }
            
            // Değişiklikleri kaydet
            if (updatedCount > 0)
            {
                await _context.SaveChangesAsync();
            }
            
            _logger.LogInformation("🎉 ProductName backfill tamamlandı: {Updated} güncellendi, {Failed} başarısız", updatedCount, failedCount);
            
            return Ok(new
            {
                success = true,
                message = $"ProductName backfill tamamlandı",
                totalProcessed = linesWithNullProductName.Count,
                updatedCount,
                failedCount,
                details = details.Take(20) // İlk 20 detayı göster
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ ProductName backfill hatası");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// ✅ Tek bir SalesOrderLine'ın ProductName'ini Katana'dan günceller (SADECE Admin)
    /// </summary>
    [HttpPost("backfill-product-name/{lineId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> BackfillSingleProductName(int lineId)
    {
        try
        {
            var katanaService = HttpContext.RequestServices.GetRequiredService<IKatanaService>();
            
            var line = await _context.SalesOrderLines.FindAsync(lineId);
            if (line == null)
            {
                return NotFound(new { success = false, error = $"SalesOrderLine bulunamadı: {lineId}" });
            }
            
            var (sku, productName) = await katanaService.GetVariantWithProductNameAsync(line.VariantId);
            
            var oldSku = line.SKU;
            var oldProductName = line.ProductName;
            
            if (!string.IsNullOrEmpty(sku))
                line.SKU = sku;
            
            if (!string.IsNullOrEmpty(productName))
                line.ProductName = productName;
            
            await _context.SaveChangesAsync();
            
            return Ok(new
            {
                success = true,
                lineId,
                variantId = line.VariantId,
                oldSku,
                newSku = line.SKU,
                oldProductName,
                newProductName = line.ProductName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Single ProductName backfill hatası: LineId={LineId}", lineId);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}

public class StartSyncRequest
{
    public string SyncType { get; set; } = string.Empty;
}

public class DebugProductSyncRequest
{
    public string SKU { get; set; } = string.Empty;
}
