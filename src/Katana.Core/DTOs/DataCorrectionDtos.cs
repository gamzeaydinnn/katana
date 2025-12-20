namespace Katana.Core.DTOs;

public class DataCorrectionDto
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
    public DateTime CreatedAt { get; set; }
}

public class CreateCorrectionDto
{
    public string SourceSystem { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string? OriginalValue { get; set; }
    public string? CorrectedValue { get; set; }
    public string CorrectionReason { get; set; } = string.Empty;
}

public class ComparisonProductDto
{
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    
    
    public KatanaProductData? KatanaData { get; set; }
    
    
    public LucaProductData? LucaData { get; set; }
    
    
    public List<DataIssue> Issues { get; set; } = new();
}

public class KatanaProductData
{
    public string Id { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? SalesPrice { get; set; }
    public decimal? CostPrice { get; set; }
    public int? OnHand { get; set; }
    public int? Available { get; set; }
    public int? Committed { get; set; }
    public string? Category { get; set; }
    public bool IsActive { get; set; }
}

public class LucaProductData
{
    public int Id { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string? CategoryName { get; set; }
    public bool IsActive { get; set; }
}

public class DataIssue
{
    public string Field { get; set; } = string.Empty;
    public string Issue { get; set; } = string.Empty;
    public string? KatanaValue { get; set; }
    public string? LucaValue { get; set; }
    public string Severity { get; set; } = "Warning"; 
}
