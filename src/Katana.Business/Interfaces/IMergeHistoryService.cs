using Katana.Core.DTOs;

namespace Katana.Business.Interfaces;

/// <summary>
/// Service for managing merge history
/// </summary>
public interface IMergeHistoryService
{
    /// <summary>
    /// Creates a new merge history entry
    /// </summary>
    Task<int> CreateMergeHistoryAsync(MergeHistoryEntry entry);

    /// <summary>
    /// Gets merge history with filters
    /// </summary>
    Task<List<MergeHistoryEntry>> GetMergeHistoryAsync(MergeHistoryFilter? filter = null);

    /// <summary>
    /// Gets detailed information about a specific merge history entry
    /// </summary>
    Task<MergeHistoryEntry?> GetMergeHistoryDetailAsync(int mergeHistoryId);

    /// <summary>
    /// Updates merge history status
    /// </summary>
    Task UpdateMergeHistoryStatusAsync(int mergeHistoryId, MergeStatus status);
}

/// <summary>
/// Filter criteria for merge history
/// </summary>
public class MergeHistoryFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? AdminUserId { get; set; }
    public MergeStatus? Status { get; set; }
}
