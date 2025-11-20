namespace Katana.Core.DTOs;
/*Arayüzden eşleştirme verilerini almak ve göndermek için CreateMappingDto, UpdateMappingDto gibi sınıflar eklenecek.

*/
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

/// <summary>
/// Mapping doğrulama sonucu (özet).
/// </summary>
public class MappingValidationDto
{
    public bool IsValid { get; set; }
    public int TotalMappings { get; set; }
    public int ActiveMappings { get; set; }
    public DateTime ValidationDate { get; set; } = DateTime.UtcNow;
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Mapping hatası bilgisi.
/// </summary>
public class MappingErrorDto
{
    public string RecordType { get; set; } = string.Empty;
    public string RecordId { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Mapping istatistikleri.
/// </summary>
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


