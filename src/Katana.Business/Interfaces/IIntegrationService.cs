using Katana.Business.DTOs;
using Katana.Core.DTOs;
/*tÃ¼m senkronizasyonu birleÅŸtiren â€œÃ¼st seviyeâ€ bir interface olacak. 
Yani Katana ve Luca servislerini Ã§aÄŸÄ±rÄ±p senkronizasyonu koordine edecek bir interface.*/
namespace Katana.Business.Interfaces;

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



