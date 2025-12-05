using Katana.Core.DTOs;

namespace Katana.Business.Interfaces;

public interface ISyncService
{
    Task<SyncResultDto> SyncStockAsync(DateTime? fromDate = null);
    Task<SyncResultDto> SyncProductsAsync(DateTime? fromDate = null);
    Task<SyncResultDto> SyncInvoicesAsync(DateTime? fromDate = null);
    Task<SyncResultDto> SyncCustomersAsync(DateTime? fromDate = null);
    Task<SyncResultDto> SyncProductsToLucaAsync(string? sessionId = null, SyncOptionsDto? options = null);
    // Backwards-compatible overload: accept options only
    Task<SyncResultDto> SyncProductsToLucaAsync(SyncOptionsDto options);
    Task<List<StockComparisonDto>> CompareStockCardsAsync();
    Task<BatchSyncResultDto> SyncAllAsync(DateTime? fromDate = null);
    
    
    Task<SyncResultDto> SyncStockFromLucaAsync(DateTime? fromDate = null);
    Task<SyncResultDto> SyncInvoicesFromLucaAsync(DateTime? fromDate = null);
    Task<SyncResultDto> SyncCustomersFromLucaAsync(DateTime? fromDate = null);
    Task<SyncResultDto> SyncDespatchFromLucaAsync(DateTime? fromDate = null);
    Task<BatchSyncResultDto> SyncAllFromLucaAsync(DateTime? fromDate = null);
    Task<List<SyncStatusDto>> GetSyncStatusAsync();
    Task<bool> IsSyncRunningAsync(string syncType);
    
    // Koza Cari Sync
    Task<SyncResultDto> SyncSuppliersToKozaAsync(CancellationToken ct = default);
    Task<SyncResultDto> SyncWarehousesToKozaAsync(CancellationToken ct = default);
    Task<SyncResultDto> SyncCustomersToLucaAsync(CancellationToken ct = default);
    
    // Debug metodları
    Task<object> DebugProductComparisonAsync(string sku);
    Task<object> ForceSyncSingleProductAsync(string sku);
}

