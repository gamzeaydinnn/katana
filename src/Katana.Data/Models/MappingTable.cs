using System.ComponentModel.DataAnnotations;

namespace Katana.Data.Models;
public class MappingTable
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string MappingType { get; set; } = string.Empty; 
    
    [Required]
    [MaxLength(100)]
    public string SourceValue { get; set; } = string.Empty; 
    
    [Required]
    [MaxLength(100)]
    public string TargetValue { get; set; } = string.Empty; 
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Son senkronize edilen verinin hash'i (değişiklik tespiti için)
    /// </summary>
    [MaxLength(64)]
    public string? LastSyncHash { get; set; }
    
    /// <summary>
    /// Son senkronize edilen tarih
    /// </summary>
    public DateTime? LastSyncAt { get; set; }
    
    /// <summary>
    /// Senkronizasyon durumu: PENDING, SYNCED, FAILED
    /// </summary>
    [MaxLength(20)]
    public string SyncStatus { get; set; } = "PENDING";
    
    /// <summary>
    /// Son senkronizasyon hatası (varsa)
    /// </summary>
    [MaxLength(500)]
    public string? LastSyncError { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(100)]
    public string? CreatedBy { get; set; }
    
    [MaxLength(100)]
    public string? UpdatedBy { get; set; }
}

