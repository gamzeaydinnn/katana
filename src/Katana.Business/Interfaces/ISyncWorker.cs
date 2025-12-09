using System.Threading.Tasks;
using Katana.Business.DTOs.Sync;

namespace Katana.Business.Interfaces
{
    /// <summary>
    /// Background worker interface for asynchronous sync operations
    /// Handles queued sync jobs via Hangfire
    /// </summary>
    public interface ISyncWorker
    {
        /// <summary>
        /// Process stock cards synchronization in background
        /// This method is called by Hangfire job queue
        /// </summary>
        /// <param name="limit">Maximum number of products to sync (null = all)</param>
        /// <param name="dryRun">If true, simulate sync without actual updates</param>
        /// <returns>Sync result summary with success/failure counts</returns>
        Task<SyncResultDto> ProcessStockCardsAsync(int? limit = null, bool dryRun = false);

        /// <summary>
        /// Process customer synchronization in background
        /// </summary>
        /// <param name="limit">Maximum number of customers to sync</param>
        /// <param name="dryRun">Dry run mode</param>
        /// <returns>Sync result summary</returns>
        Task<SyncResultDto> ProcessCustomersAsync(int? limit = null, bool dryRun = false);

        /// <summary>
        /// Process invoice synchronization in background
        /// </summary>
        /// <param name="limit">Maximum number of invoices to sync</param>
        /// <param name="dryRun">Dry run mode</param>
        /// <returns>Sync result summary</returns>
        Task<SyncResultDto> ProcessInvoicesAsync(int? limit = null, bool dryRun = false);

        /// <summary>
        /// Get current job progress (for monitoring)
        /// </summary>
        /// <param name="jobId">Hangfire job ID</param>
        /// <returns>Progress information</returns>
        Task<SyncProgressDto> GetJobProgressAsync(string jobId);
    }
}
