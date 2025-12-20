using Katana.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Katana.API.Controllers;

/// <summary>
/// Stok Hareketi Luca Senkronizasyon Controller'ı
/// Transfer ve Adjustment → Luca Depo Transferi / DSH Fişi
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StockMovementSyncController : ControllerBase
{
    private readonly IStockMovementSyncService _syncService;
    private readonly ILogger<StockMovementSyncController> _logger;

    public StockMovementSyncController(
        IStockMovementSyncService syncService,
        ILogger<StockMovementSyncController> logger)
    {
        _syncService = syncService;
        _logger = logger;
    }

    /// <summary>
    /// Tüm stok hareketlerini listeler (Transfer + Adjustment)
    /// </summary>
    [HttpGet("movements")]
    public async Task<ActionResult<List<StockMovementSyncDto>>> GetAllMovements(
        [FromQuery] string? movementType = null,
        [FromQuery] string? syncStatus = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var filter = new StockMovementFilterDto
        {
            MovementType = movementType,
            SyncStatus = syncStatus,
            StartDate = startDate,
            EndDate = endDate
        };

        var movements = await _syncService.GetAllMovementsAsync(filter);
        return Ok(movements);
    }

    /// <summary>
    /// Bekleyen stok transferlerini listeler
    /// </summary>
    [HttpGet("transfers/pending")]
    public async Task<ActionResult<List<StockMovementSyncDto>>> GetPendingTransfers()
    {
        var transfers = await _syncService.GetPendingTransfersAsync();
        return Ok(transfers);
    }

    /// <summary>
    /// Bekleyen stok düzeltmelerini listeler
    /// </summary>
    [HttpGet("adjustments/pending")]
    public async Task<ActionResult<List<StockMovementSyncDto>>> GetPendingAdjustments()
    {
        var adjustments = await _syncService.GetPendingAdjustmentsAsync();
        return Ok(adjustments);
    }

    /// <summary>
    /// Tek bir stok transferini Luca'ya senkronize eder
    /// </summary>
    [HttpPost("sync/transfer/{transferId}")]
    public async Task<ActionResult<MovementSyncResultDto>> SyncTransfer(int transferId)
    {
        _logger.LogInformation("🔄 Transfer {TransferId} senkronizasyonu başlatılıyor", transferId);
        
        var result = await _syncService.SyncTransferToLucaAsync(transferId);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Tek bir stok düzeltmesini Luca'ya senkronize eder
    /// </summary>
    [HttpPost("sync/adjustment/{adjustmentId}")]
    public async Task<ActionResult<MovementSyncResultDto>> SyncAdjustment(int adjustmentId)
    {
        _logger.LogInformation("🔄 Adjustment {AdjustmentId} senkronizasyonu başlatılıyor", adjustmentId);
        
        var result = await _syncService.SyncAdjustmentToLucaAsync(adjustmentId);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Genel senkronizasyon endpoint'i - type ve id parametreleriyle
    /// </summary>
    [HttpPost("sync-movement/{type}/{id}")]
    public async Task<ActionResult<MovementSyncResultDto>> SyncMovement(string type, int id)
    {
        _logger.LogInformation("🔄 {Type} #{Id} senkronizasyonu başlatılıyor", type, id);
        
        MovementSyncResultDto result;
        
        if (type.Equals("TRANSFER", StringComparison.OrdinalIgnoreCase))
        {
            result = await _syncService.SyncTransferToLucaAsync(id);
        }
        else if (type.Equals("ADJUSTMENT", StringComparison.OrdinalIgnoreCase))
        {
            result = await _syncService.SyncAdjustmentToLucaAsync(id);
        }
        else
        {
            return BadRequest(new { success = false, message = $"Geçersiz hareket tipi: {type}" });
        }
        
        if (result.Success)
        {
            return Ok(new { success = true, message = $"{type} başarıyla Luca'ya işlendi.", lucaId = result.LucaDocumentId });
        }
        
        return BadRequest(new { success = false, message = result.ErrorMessage });
    }

    /// <summary>
    /// Toplu senkronizasyon
    /// </summary>
    [HttpPost("sync/batch")]
    public async Task<ActionResult<MovementBatchSyncResultDto>> SyncBatch([FromBody] MovementBatchSyncRequest request)
    {
        _logger.LogInformation("🔄 Toplu senkronizasyon başlatılıyor: {TransferCount} transfer, {AdjustmentCount} adjustment",
            request.TransferIds?.Count ?? 0, request.AdjustmentIds?.Count ?? 0);
        
        var result = await _syncService.SyncBatchAsync(
            request.TransferIds ?? new List<int>(),
            request.AdjustmentIds ?? new List<int>());
        
        return Ok(result);
    }

    /// <summary>
    /// Bekleyen tüm hareketleri senkronize eder
    /// </summary>
    [HttpPost("sync/all-pending")]
    public async Task<ActionResult<MovementBatchSyncResultDto>> SyncAllPending()
    {
        _logger.LogInformation("🔄 Tüm bekleyen stok hareketleri senkronize ediliyor...");
        
        var result = await _syncService.SyncAllPendingAsync();
        
        return Ok(result);
    }

    /// <summary>
    /// Dashboard istatistiklerini döner
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<MovementDashboardStatsDto>> GetDashboardStats()
    {
        var stats = await _syncService.GetDashboardStatsAsync();
        return Ok(stats);
    }
}

/// <summary>
/// Toplu stok hareketi senkronizasyon isteği
/// </summary>
public class MovementBatchSyncRequest
{
    public List<int>? TransferIds { get; set; }
    public List<int>? AdjustmentIds { get; set; }
}
