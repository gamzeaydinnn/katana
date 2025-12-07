using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Katana.Core.Entities;

/// <summary>
/// Katana Location → Koza Depo mapping tablosu
/// Eldeki miktar endpoint'i depoId istiyor, depo transferi depoKodu istiyor
/// </summary>
[Table("LocationKozaDepotMappings")]
public class LocationKozaDepotMapping
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// Katana Location ID (string veya numeric olabilir)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string KatanaLocationId { get; set; } = string.Empty;
    
    /// <summary>
    /// Koza depo kodu (kod alanı)
    /// Depo transferi için gerekli (girisDepoKodu/cikisDepoKodu)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string KozaDepoKodu { get; set; } = string.Empty;
    
    /// <summary>
    /// Koza depo ID (depoId)
    /// Eldeki miktar endpoint'i için gerekli
    /// </summary>
    public long? KozaDepoId { get; set; }
    
    /// <summary>
    /// Senkronizasyon durumu: PENDING, SYNCED, FAILED
    /// </summary>
    [MaxLength(20)]
    public string SyncStatus { get; set; } = "PENDING";
    
    /// <summary>
    /// Son senkronize edilen verinin hash'i (değişiklik tespiti için)
    /// </summary>
    [MaxLength(64)]
    public string? LastSyncHash { get; set; }
    
    /// <summary>
    /// Son senkronizasyon hatası (varsa)
    /// </summary>
    [MaxLength(500)]
    public string? LastSyncError { get; set; }
    
    /// <summary>
    /// Son senkronize edilen tarih
    /// </summary>
    public DateTime? LastSyncAt { get; set; }
    
    /// <summary>
    /// Son güncelleme zamanı
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Oluşturma zamanı
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Katana Location adı (log/debug için)
    /// </summary>
    [MaxLength(200)]
    public string? KatanaLocationName { get; set; }
    
    /// <summary>
    /// Koza depo adı (log/debug için)
    /// </summary>
    [MaxLength(200)]
    public string? KozaDepoTanim { get; set; }
}
