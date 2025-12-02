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
    /// Son güncelleme zamanı
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
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
