using Katana.Core.DTOs;

namespace Katana.Business.Interfaces;

/// <summary>
/// Service for managing product merge operations
/// </summary>
public interface IProductMergeService
{
    /// <summary>
    /// Previews the impact of a merge operation
    /// </summary>
    Task<MergePreview> PreviewMergeAsync(MergeRequest request);

    /// <summary>
    /// Executes a merge operation
    /// </summary>
    Task<MergeResult> ExecuteMergeAsync(MergeRequest request, string adminUserId);

    /// <summary>
    /// Rolls back a merge operation
    /// </summary>
    Task<MergeResult> RollbackMergeAsync(int mergeHistoryId, string adminUserId);

    /// <summary>
    /// Marks a group as keep separate
    /// </summary>
    Task MarkGroupAsKeepSeparateAsync(string productName, string reason, string adminUserId);

    /// <summary>
    /// Removes keep separate flag
    /// </summary>
    Task RemoveKeepSeparateFlagAsync(string productName, string adminUserId);
}
