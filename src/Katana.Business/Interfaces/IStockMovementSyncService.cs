using Katana.Core.DTOs;

namespace Katana.Business.Interfaces;

/// <summary>
/// Stok Transfer ve Adjustment senkronizasyon servisi
/// Katana → Luca (Depo Transfer / DSH Fiş)
/// </summary>
public interface IStockMovementSyncService
{
    /// <summary>
    /// Tüm stok hareketlerini (transfer + adjustment) listeler
    /// </summary>
    Task<List<StockMovementSyncDto>> GetAllMovementsAsync(StockMovementFilterDto? filter = null);

    /// <summary>
    /// Bekleyen stok transferlerini listeler
    /// </summary>
    Task<List<StockMovementSyncDto>> GetPendingTransfersAsync();

    /// <summary>
    /// Bekleyen stok düzeltmelerini listeler
    /// </summary>
    Task<List<StockMovementSyncDto>> GetPendingAdjustmentsAsync();

    /// <summary>
    /// Tek bir stok transferini Luca'ya senkronize eder
    /// </summary>
    Task<MovementSyncResultDto> SyncTransferToLucaAsync(int transferId);

    /// <summary>
    /// Tek bir stok düzeltmesini Luca'ya senkronize eder
    /// </summary>
    Task<MovementSyncResultDto> SyncAdjustmentToLucaAsync(int adjustmentId);

    /// <summary>
    /// Toplu stok hareketi senkronizasyonu
    /// </summary>
    Task<MovementBatchSyncResultDto> SyncBatchAsync(List<int> transferIds, List<int> adjustmentIds);

    /// <summary>
    /// Bekleyen tüm hareketleri senkronize eder
    /// </summary>
    Task<MovementBatchSyncResultDto> SyncAllPendingAsync();

    /// <summary>
    /// Dashboard istatistiklerini döner
    /// </summary>
    Task<MovementDashboardStatsDto> GetDashboardStatsAsync();
}

#region DTOs

/// <summary>
/// Stok hareketi DTO - Transfer ve Adjustment için ortak
/// </summary>
public class StockMovementSyncDto
{
    public int Id { get; set; }
    public string DocumentNo { get; set; } = string.Empty;
    
    /// <summary>
    /// "TRANSFER" veya "ADJUSTMENT"
    /// </summary>
    public string MovementType { get; set; } = string.Empty;
    
    /// <summary>
    /// Transfer için "Kaynak → Hedef", Adjustment için "Depo"
    /// </summary>
    public string LocationInfo { get; set; } = string.Empty;
    
    public DateTime MovementDate { get; set; }
    public decimal TotalQuantity { get; set; }
    
    /// <summary>
    /// "PENDING", "SYNCED", "ERROR"
    /// </summary>
    public string SyncStatus { get; set; } = "PENDING";
    
    public long? LucaDocumentId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? SyncedAt { get; set; }
    
    /// <summary>
    /// Adjustment için: Pozitif/Negatif bilgisi
    /// </summary>
    public string? AdjustmentReason { get; set; }
    
    public List<StockMovementRowDto> Rows { get; set; } = new();
}

public class StockMovementRowDto
{
    public int Id { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal? UnitCost { get; set; }
}

public class StockMovementFilterDto
{
    public string? MovementType { get; set; } // "TRANSFER", "ADJUSTMENT", null = hepsi
    public string? SyncStatus { get; set; } // "PENDING", "SYNCED", "ERROR", null = hepsi
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? LocationId { get; set; }
}

public class MovementSyncResultDto
{
    public bool Success { get; set; }
    public int MovementId { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public long? LucaDocumentId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
}

public class MovementBatchSyncResultDto
{
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<MovementSyncResultDto> Results { get; set; } = new();
}

public class MovementDashboardStatsDto
{
    public int TotalTransfers { get; set; }
    public int PendingTransfers { get; set; }
    public int SyncedTransfers { get; set; }
    public int FailedTransfers { get; set; }
    
    public int TotalAdjustments { get; set; }
    public int PendingAdjustments { get; set; }
    public int SyncedAdjustments { get; set; }
    public int FailedAdjustments { get; set; }
    
    public DateTime? LastSyncDate { get; set; }
}

#endregion
