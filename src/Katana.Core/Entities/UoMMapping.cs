using System;

namespace Katana.Core.Entities;

/// <summary>
/// Maps Katana Unit of Measure string to Koza olcumBirimiId
/// Examples: "pcs" → 5 (Adet), "kg" → 1 (Kilogram), "m" → 3 (Metre)
/// </summary>
public class UoMMapping
{
    public int Id { get; set; }
    
    /// <summary>
    /// Katana UoM string (e.g., "pcs", "kg", "m", "l", "box", "unit")
    /// Stored in uppercase for case-insensitive matching
    /// </summary>
    public string KatanaUoMString { get; set; } = string.Empty;
    
    /// <summary>
    /// Koza ölçüm birimi ID
    /// Common values: 1=Kilogram, 2=Gram, 3=Metre, 4=Litre, 5=Adet, 6=Kutu, etc.
    /// </summary>
    public long KozaOlcumBirimiId { get; set; }
    
    /// <summary>
    /// Human-readable description (e.g., "Pieces", "Kilograms", "Meters")
    /// </summary>
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Sync tracking fields
    public string? LastSyncHash { get; set; }
    public string SyncStatus { get; set; } = "PENDING"; // PENDING, SYNCED, FAILED
    public DateTime? LastSyncAt { get; set; }
    public string? LastSyncError { get; set; }
}
