using System;

namespace Katana.Core.Entities;

/// <summary>
/// Maps Katana tax_rate_id to Koza KDV oranı (decimal format: 0.18 for 18%)
/// </summary>
public class TaxRateMapping
{
    public int Id { get; set; }
    
    /// <summary>
    /// Katana tax_rate_id from sales_order_rows, shipping_fees, etc.
    /// </summary>
    public long KatanaTaxRateId { get; set; }
    
    /// <summary>
    /// Koza KDV oranı (decimal: 0.18 = 18%, 0.20 = 20%, 0.01 = 1%, 0.08 = 8%)
    /// </summary>
    public decimal KozaKdvOran { get; set; }
    
    /// <summary>
    /// Human-readable description (e.g., "Standard VAT 18%", "Reduced VAT 8%")
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
