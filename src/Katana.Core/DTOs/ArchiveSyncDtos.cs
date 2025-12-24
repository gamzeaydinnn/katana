namespace Katana.Core.DTOs;

/// <summary>
/// Result of the archive sync operation
/// </summary>
public class ArchiveSyncResult
{
    public int TotalKatanaProducts { get; set; }
    public int TotalLocalProducts { get; set; }
    public int ProductsToArchive { get; set; }
    public int ArchivedSuccessfully { get; set; }
    public int ArchiveFailed { get; set; }
    public List<ProductArchivePreview> ArchivedProducts { get; set; } = new();
    public List<ArchiveError> Errors { get; set; } = new();
    public DateTime SyncStartedAt { get; set; }
    public DateTime SyncCompletedAt { get; set; }
    public bool IsPreviewOnly { get; set; }
    
    /// <summary>
    /// Duration of the sync operation in seconds
    /// </summary>
    public double DurationSeconds => (SyncCompletedAt - SyncStartedAt).TotalSeconds;
}

/// <summary>
/// Preview information for a product that will be archived
/// </summary>
public class ProductArchivePreview
{
    public int KatanaProductId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsAlreadyArchived { get; set; }
}

/// <summary>
/// Error information when archiving a product fails
/// </summary>
public class ArchiveError
{
    public int KatanaProductId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
