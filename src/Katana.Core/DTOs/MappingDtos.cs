namespace Katana.Core.DTOs;

// TODO: SkuAccountMappingDto removed - use MappingDto with MappingType="SKU_ACCOUNT" instead

public class LocationWarehouseMappingDto
{
    public string Location { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class MappingDto
{
    public string MappingType { get; set; } = string.Empty;
    public string SourceValue { get; set; } = string.Empty;
    public string TargetValue { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}




public class MappingValidationDto
{
    public bool IsValid { get; set; }
    public int TotalMappings { get; set; }
    public int ActiveMappings { get; set; }
    public DateTime ValidationDate { get; set; } = DateTime.UtcNow;
    public List<string> Errors { get; set; } = new();
}




public class MappingErrorDto
{
    public string RecordType { get; set; } = string.Empty;
    public string RecordId { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}




public class MappingStatsDto
{
    public int TotalMappings { get; set; }
    public int ActiveMappings { get; set; }
    public int InactiveMappings { get; set; }
    public int ProductMappings { get; set; }
    public int CustomerMappings { get; set; }
    public int LocationMappings { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}


