using System.ComponentModel.DataAnnotations;

namespace Katana.Core.Entities;
public class Supplier
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Code { get; set; }

    [MaxLength(50)]
    public string? TaxNo { get; set; }

    [MaxLength(100)]
    public string? ContactName { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(300)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Luca'daki cari kart kodu (TED-{Id} formatında)
    /// </summary>
    [MaxLength(50)]
    public string? LucaCode { get; set; }

    /// <summary>
    /// Luca'daki finansalNesneId (cari kart ID)
    /// </summary>
    public long? LucaFinansalNesneId { get; set; }

    /// <summary>
    /// Son senkronizasyon hatası
    /// </summary>
    [MaxLength(500)]
    public string? LastSyncError { get; set; }
    
    /// <summary>
    /// Son senkronize edilen verinin hash'i (değişiklik tespiti için)
    /// </summary>
    [MaxLength(64)]
    public string? LastSyncHash { get; set; }
    
    /// <summary>
    /// Senkronizasyon durumu: PENDING, SYNCED, FAILED
    /// </summary>
    [MaxLength(20)]
    public string SyncStatus { get; set; } = "PENDING";
    
    /// <summary>
    /// Son senkronize edilen tarih
    /// </summary>
    public DateTime? LastSyncAt { get; set; }
    
    public bool IsSynced { get; set; } = false;

    public virtual ICollection<SupplierPrice> PriceList { get; set; } = new List<SupplierPrice>();

    /// <summary>
    /// Luca cari kart kodunu oluşturur (TED-{Id})
    /// </summary>
    public string GenerateLucaCode() => $"TED-{Id}";
}
