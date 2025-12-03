using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Katana.Core.Entities;

/// <summary>
/// Koza Depo Kartı (cache tablosu)
/// Koza'daki depoları local DB'de cache'ler
/// </summary>
[Table("KozaDepots")]
public class KozaDepot
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// Koza depo ID
    /// </summary>
    public long? DepoId { get; set; }
    
    /// <summary>
    /// Depo kodu (zorunlu)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Kod { get; set; } = string.Empty;
    
    /// <summary>
    /// Depo adı
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Tanim { get; set; } = string.Empty;
    
    /// <summary>
    /// Kategori kodu
    /// </summary>
    [MaxLength(50)]
    public string? KategoriKod { get; set; }
    
    /// <summary>
    /// Ülke
    /// </summary>
    [MaxLength(50)]
    public string? Ulke { get; set; }
    
    /// <summary>
    /// İl
    /// </summary>
    [MaxLength(50)]
    public string? Il { get; set; }
    
    /// <summary>
    /// İlçe
    /// </summary>
    [MaxLength(50)]
    public string? Ilce { get; set; }
    
    /// <summary>
    /// Adres serbest metin
    /// </summary>
    [MaxLength(500)]
    public string? AdresSerbest { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
