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
