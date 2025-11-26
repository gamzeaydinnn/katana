using Katana.Core.DTOs;

namespace Katana.Business.Interfaces;

public interface ISyncService
{
    Task<SyncResultDto> SyncStockAsync(DateTime? fromDate = null);
    Task<SyncResultDto> SyncProductsAsync(DateTime? fromDate = null);
    Task<SyncResultDto> SyncInvoicesAsync(DateTime? fromDate = null);
    Task<SyncResultDto> SyncCustomersAsync(DateTime? fromDate = null);
    Task<BatchSyncResultDto> SyncAllAsync(DateTime? fromDate = null);
    
    // Luca → Katana 
    Task<SyncResultDto> SyncStockFromLucaAsync(DateTime? fromDate = null);
    Task<SyncResultDto> SyncInvoicesFromLucaAsync(DateTime? fromDate = null);
    Task<SyncResultDto> SyncCustomersFromLucaAsync(DateTime? fromDate = null);
    Task<SyncResultDto> SyncDespatchFromLucaAsync(DateTime? fromDate = null);
    Task<BatchSyncResultDto> SyncAllFromLucaAsync(DateTime? fromDate = null);
    Task<List<SyncStatusDto>> GetSyncStatusAsync();
    Task<bool> IsSyncRunningAsync(string syncType);
}

