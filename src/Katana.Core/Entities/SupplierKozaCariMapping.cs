using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Katana.Core.Entities;

/// <summary>
/// Katana Supplier → Koza Cari (Tedarikçi) mapping tablosu
/// Katana tedarikçileri Koza'da Cari kartı olarak tutulur (cariTipId=2)
/// </summary>
[Table("SupplierKozaCariMappings")]
public class SupplierKozaCariMapping
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// Katana Supplier ID (string)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string KatanaSupplierId { get; set; } = string.Empty;
    
    /// <summary>
    /// Koza cari kodu (TED-{KatanaSupplierId} formatında)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string KozaCariKodu { get; set; } = string.Empty;
    
    /// <summary>
    /// Koza finansal nesne ID
    /// Cari işlemleri (adres, yetkili, hareket) için gerekli
    /// </summary>
    public long? KozaFinansalNesneId { get; set; }
    
    /// <summary>
    /// Katana Supplier adı (log/debug için)
    /// </summary>
    [MaxLength(200)]
    public string? KatanaSupplierName { get; set; }
    
    /// <summary>
    /// Koza cari tanımı (log/debug için)
    /// </summary>
    [MaxLength(200)]
    public string? KozaCariTanim { get; set; }
    
    /// <summary>
    /// Son güncelleme zamanı
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Oluşturma zamanı
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
