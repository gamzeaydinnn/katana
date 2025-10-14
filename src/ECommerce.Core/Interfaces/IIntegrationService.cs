using ECommerce.Core.DTOs;
/*tüm senkronizasyonu birleştiren “üst seviye” bir interface olacak. 
Yani Katana ve Luca servislerini çağırıp senkronizasyonu koordine edecek bir interface.*/
namespace ECommerce.Core.Interfaces
{
    /// <summary>
    /// Integration service combines Katana and Luca services for synchronization
    /// </summary>
    public interface IIntegrationService
    {
        Task<SyncResultDto> SyncStockAsync(DateTime? fromDate = null);
        Task<SyncResultDto> SyncInvoicesAsync(DateTime? fromDate = null);
        Task<SyncResultDto> SyncCustomersAsync(DateTime? fromDate = null);
        Task<BatchSyncResultDto> SyncAllAsync(DateTime? fromDate = null);

        Task<List<SyncStatusDto>> GetSyncStatusAsync();
        Task<bool> IsSyncRunningAsync(string syncType);
    }
}
