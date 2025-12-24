using Katana.Core.DTOs;

namespace Katana.Business.Interfaces;

/// <summary>
/// Service for analyzing and managing product duplicates
/// </summary>
public interface IDuplicateAnalysisService
{
    /// <summary>
    /// Analyzes all products and returns duplicate groups
    /// </summary>
    Task<List<ProductDuplicateGroup>> AnalyzeDuplicatesAsync();

    /// <summary>
    /// Gets detailed information about a specific duplicate group
    /// </summary>
    Task<ProductDuplicateGroup?> GetDuplicateGroupDetailAsync(string productName);

    /// <summary>
    /// Filters duplicate groups based on criteria
    /// </summary>
    Task<List<ProductDuplicateGroup>> FilterDuplicateGroupsAsync(DuplicateFilterCriteria criteria);

    /// <summary>
    /// Exports duplicate analysis results to CSV
    /// </summary>
    Task<byte[]> ExportDuplicateAnalysisAsync(List<ProductDuplicateGroup> groups);
}

/// <summary>
/// Criteria for filtering duplicate groups
/// </summary>
public class DuplicateFilterCriteria
{
    public string? CategoryName { get; set; }
    public int? MinimumCount { get; set; }
    public string? NamePattern { get; set; }
    public string? SkuPattern { get; set; }
}
