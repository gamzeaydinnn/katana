using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Katana.Core.Entities;

/// <summary>
/// Katana Customer → Koza Cari (Müşteri) mapping tablosu
/// Katana müşterileri Koza'da Cari kartı olarak tutulur (cariTipId=1)
/// </summary>
[Table("CustomerKozaCariMappings")]
public class CustomerKozaCariMapping
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// Katana Customer ID (string to handle long IDs from Katana API)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string KatanaCustomerId { get; set; } = string.Empty;
    
    /// <summary>
    /// Koza cari kodu (CK-{KatanaCustomerId} formatında veya Koza'dan dönen kod)
    /// ÖNEMLI: String olarak sakla - leading zero kaybı olmasın!
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
    /// Katana Customer adı (log/debug için)
    /// </summary>
    [MaxLength(200)]
    public string? KatanaCustomerName { get; set; }
    
    /// <summary>
    /// Koza cari tanımı (log/debug için)
    /// </summary>
    [MaxLength(200)]
    public string? KozaCariTanim { get; set; }
    
    /// <summary>
    /// Katana Customer vergi numarası (duplicate kontrolü için)
    /// </summary>
    [MaxLength(11)]
    public string? KatanaCustomerTaxNo { get; set; }
    
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
}
