namespace Katana.Core.Entities;

/// <summary>
/// Data correction/override tracking for Katana ↔ Luca integration
/// Admin can manually fix incorrect data before sync
/// </summary>
public class DataCorrectionLog
{
    public int Id { get; set; }
    public string SourceSystem { get; set; } = string.Empty; // "Katana" or "Luca"
    public string EntityType { get; set; } = string.Empty; // "Product", "Invoice", "Customer"
    public string EntityId { get; set; } = string.Empty; // External ID (SKU, InvoiceNo, etc.)
    public string FieldName { get; set; } = string.Empty; // "Price", "Stock", "TaxRate"
    public string? OriginalValue { get; set; }
    public string? CorrectedValue { get; set; }
    public string ValidationError { get; set; } = string.Empty;
    public string CorrectionReason { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public bool IsSynced { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}
