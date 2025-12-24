using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

/// <summary>
/// Duplicate analysis result containing all detected duplicate groups
/// </summary>
public class DuplicateAnalysisResult
{
    public List<DuplicateGroup> DuplicateGroups { get; set; } = new();
    public DuplicateStatistics Statistics { get; set; } = new();
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// A group of stock cards that are duplicates of each other
/// </summary>
public class DuplicateGroup
{
    public string GroupId { get; set; } = string.Empty;
    public string StockName { get; set; } = string.Empty;
    public DuplicateCategory Category { get; set; }
    public List<StockCardInfo> StockCards { get; set; } = new();
}

/// <summary>
/// A group of products with duplicate names for merge operations
/// </summary>
public class ProductDuplicateGroup
{
    public string ProductName { get; set; } = string.Empty;
    public int Count { get; set; }
    public long? KatanaOrderId { get; set; }
    public List<ProductSummary> Products { get; set; } = new();
    public bool IsKeepSeparate { get; set; }
    public string? KeepSeparateReason { get; set; }
    public DateTime? KeepSeparateDate { get; set; }
    public string? KeepSeparateBy { get; set; }
}

/// <summary>
/// Summary information about a product in a duplicate group
/// </summary>
public class ProductSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int? SalesOrderId { get; set; }
    public int SalesOrderCount { get; set; }
    public int BOMCount { get; set; }
    public int StockMovementCount { get; set; }
    public bool IsActive { get; set; }
    public bool IsSuggestedCanonical { get; set; }
    public long? KatanaOrderId { get; set; }
}

/// <summary>
/// Information about a single stock card within a duplicate group
/// </summary>
public class StockCardInfo
{
    public long SkartId { get; set; }
    public string StockCode { get; set; } = string.Empty;
    public string StockName { get; set; } = string.Empty;
    public bool IsCanonical { get; set; }
    public string? IssueDescription { get; set; }
}

/// <summary>
/// Category of duplicate issue
/// </summary>
public enum DuplicateCategory
{
    Versioning,
    ConcatenationError,
    CharacterEncoding,
    Mixed
}

/// <summary>
/// Statistics about detected duplicates
/// </summary>
public class DuplicateStatistics
{
    public int TotalStockCards { get; set; }
    public int DuplicateGroups { get; set; }
    public int TotalDuplicates { get; set; }
    public int VersioningDuplicates { get; set; }
    public int ConcatenationErrors { get; set; }
    public int EncodingIssues { get; set; }
}

/// <summary>
/// Preview of deduplication actions before execution
/// </summary>
public class DeduplicationPreview
{
    public List<DeduplicationAction> Actions { get; set; } = new();
    public PreviewStatistics Statistics { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// A single deduplication action to be performed
/// </summary>
public class DeduplicationAction
{
    public string GroupId { get; set; } = string.Empty;
    public StockCardInfo CanonicalCard { get; set; } = new();
    public List<StockCardInfo> CardsToRemove { get; set; } = new();
    public string Reason { get; set; } = string.Empty;
    public ActionType Type { get; set; }
}

/// <summary>
/// Type of deduplication action
/// </summary>
public enum ActionType
{
    Remove,
    UpdateAndRemove,
    Skip
}

/// <summary>
/// Statistics about preview actions
/// </summary>
public class PreviewStatistics
{
    public int TotalActions { get; set; }
    public int CardsToKeep { get; set; }
    public int CardsToRemove { get; set; }
    public int CardsToUpdate { get; set; }
}

/// <summary>
/// Result of deduplication execution
/// </summary>
public class DeduplicationExecutionResult
{
    public int SuccessfulRemovals { get; set; }
    public int FailedRemovals { get; set; }
    public int SkippedActions { get; set; }
    public List<ExecutionError> Errors { get; set; } = new();
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Error that occurred during deduplication execution
/// </summary>
public class ExecutionError
{
    public string GroupId { get; set; } = string.Empty;
    public string StockCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Configuration for deduplication rules
/// </summary>
public class DeduplicationRules
{
    public List<CanonicalSelectionRule> Rules { get; set; } = new();
    public CanonicalSelectionRule DefaultRule { get; set; } = new();
}

/// <summary>
/// A rule for selecting the canonical stock card
/// </summary>
public class CanonicalSelectionRule
{
    public string Name { get; set; } = string.Empty;
    public int Priority { get; set; }
    public RuleType Type { get; set; }
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Type of canonical selection rule
/// </summary>
public enum RuleType
{
    PreferNoVersionSuffix,
    PreferLowerVersion,
    PreferShorterCode,
    PreferNoSpecialCharacters,
    PreferCorrectEncoding
}

/// <summary>
/// Export format for analysis results
/// </summary>
public enum ExportFormat
{
    Json,
    Csv
}

/// <summary>
/// Plan for merging duplicate products by SKU base code
/// </summary>
public class SkuBaseMergePlan
{
    public List<SkuBaseMergeGroup> Groups { get; set; } = new();
    public int TotalGroups { get; set; }
    public int TotalDuplicates { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// A group of products that share the same SKU base code
/// </summary>
public class SkuBaseMergeGroup
{
    public string BaseSku { get; set; } = string.Empty;
    public long CanonicalProductId { get; set; }
    public List<long> DuplicateProductIds { get; set; } = new();
    public List<string> ProductSkus { get; set; } = new();
}

/// <summary>
/// Request to merge duplicate products into a canonical product
/// </summary>
public class MergeRequest
{
    public int CanonicalProductId { get; set; }
    public List<int> ProductIdsToMerge { get; set; } = new();
    public bool UpdateSalesOrders { get; set; } = true;
    public bool UpdateBOMs { get; set; } = true;
    public bool UpdateStockMovements { get; set; } = true;
    public string? Reason { get; set; }
}

/// <summary>
/// Preview of the impact of a merge operation
/// </summary>
public class MergePreview
{
    public ProductSummary? CanonicalProduct { get; set; }
    public List<ProductSummary> ProductsToMerge { get; set; } = new();
    public int SalesOrdersToUpdate { get; set; }
    public int BOMsToUpdate { get; set; }
    public int StockMovementsToUpdate { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> CriticalWarnings { get; set; } = new();
    public bool CanProceed { get; set; }
}

/// <summary>
/// Result of a merge operation
/// </summary>
public class MergeResult
{
    public bool Success { get; set; }
    public int MergeHistoryId { get; set; }
    public int SalesOrdersUpdated { get; set; }
    public int BOMsUpdated { get; set; }
    public int StockMovementsUpdated { get; set; }
    public int ProductsInactivated { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Entry in the merge history audit log
/// </summary>
public class MergeHistoryEntry
{
    public int Id { get; set; }
    public int CanonicalProductId { get; set; }
    public string CanonicalProductName { get; set; } = string.Empty;
    public string CanonicalProductSKU { get; set; } = string.Empty;
    public List<int> MergedProductIds { get; set; } = new();
    public int SalesOrdersUpdated { get; set; }
    public int BOMsUpdated { get; set; }
    public int StockMovementsUpdated { get; set; }
    public string AdminUserId { get; set; } = string.Empty;
    public string AdminUserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public MergeStatus Status { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Status of a merge operation
/// </summary>
public enum MergeStatus
{
    Pending,
    Completed,
    RolledBack,
    Failed
}
