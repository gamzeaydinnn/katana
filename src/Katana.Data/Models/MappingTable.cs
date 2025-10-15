using System.ComponentModel.DataAnnotations;

namespace Katana.Data.Models;
//Eşleştirmenin hangi türe ait olduğunu belirten bir alan (MappingType - örn: "Product", "Customer") eklenmeli.


public class MappingTable
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string MappingType { get; set; } = string.Empty; // SKU_ACCOUNT, LOCATION_WAREHOUSE
    
    [Required]
    [MaxLength(100)]
    public string SourceValue { get; set; } = string.Empty; // Katana value
    
    [Required]
    [MaxLength(100)]
    public string TargetValue { get; set; } = string.Empty; // Luca value
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(100)]
    public string? CreatedBy { get; set; }
    
    [MaxLength(100)]
    public string? UpdatedBy { get; set; }
}

