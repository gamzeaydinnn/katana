namespace Katana.Core.Entities;
public class DataCorrectionLog
{
    public int Id { get; set; }
    public string SourceSystem { get; set; } = string.Empty; 
    public string EntityType { get; set; } = string.Empty; 
    public string EntityId { get; set; } = string.Empty; 
    public string FieldName { get; set; } = string.Empty; 
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
