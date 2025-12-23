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
}
