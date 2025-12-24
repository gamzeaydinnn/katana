using Katana.Core.DTOs;

namespace Katana.Business.Interfaces;

public interface IKatanaCleanupService
{
    Task<OrderProductAnalysisResult> AnalyzeOrderProductsAsync();
    Task<KatanaCleanupResult> DeleteFromKatanaAsync(List<string> skus, bool dryRun = true);
    Task<ResetResult> ResetOrderApprovalsAsync(bool dryRun = true);
    Task<CleanupReport> GenerateCleanupReportAsync();
    Task<BackupResult> CreateBackupAsync();
    Task<RollbackResult> RollbackAsync(string backupId);
    
    /// <summary>
    /// Cancels duplicate orders in Katana based on provided Katana Order IDs.
    /// </summary>
    Task<KatanaOrderCleanupResult> CancelDuplicateOrdersAsync(List<long> katanaOrderIds, bool dryRun = true);
    
    /// <summary>
    /// Finds duplicate orders in Katana based on order number patterns.
    /// </summary>
    Task<Dictionary<string, List<long>>> FindDuplicateOrdersAsync(DateTime? fromDate = null);
}
