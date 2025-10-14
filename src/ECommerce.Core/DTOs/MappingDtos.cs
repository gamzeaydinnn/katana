namespace ECommerce.Core.DTOs;

/// <summary>
/// DTO for SKU to Account mapping
/// </summary>
public class SkuAccountMappingDto
{
    public string Sku { get; set; } = string.Empty;
    public string AccountCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for Location to Warehouse mapping
/// </summary>
public class LocationWarehouseMappingDto
{
    public string Location { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Generic mapping DTO, if needed for multiple mapping types
/// </summary>
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
