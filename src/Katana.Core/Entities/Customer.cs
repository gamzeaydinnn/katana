using System.ComponentModel.DataAnnotations;

namespace Katana.Core.Entities;

public class Customer
{
    public int Id { get; set; }
    
    /// <summary>
    /// Müşteri tipi: 1=Şirket (VKN), 2=Şahıs (TCKN)
    /// </summary>
    public int Type { get; set; } = 1;
    
    [Required]
    [MaxLength(11)]
    public string TaxNo { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? TaxOffice { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? ContactPerson { get; set; }
    
    [MaxLength(20)]
    public string? Phone { get; set; }
    
    [MaxLength(100)]
    public string? Email { get; set; }
    
    [MaxLength(500)]
    public string? Address { get; set; }
    
    [MaxLength(100)]
    public string? City { get; set; }
    
    [MaxLength(100)]
    public string? District { get; set; }
    
    [MaxLength(50)]
    public string? Country { get; set; } = "Turkey";
    
    /// <summary>
    /// Luca'daki cari kart kodu (CK-{Id} formatında)
    /// </summary>
    [MaxLength(50)]
    public string? LucaCode { get; set; }
    
    /// <summary>
    /// Luca'daki finansalNesneId (cari kart ID)
    /// </summary>
    public long? LucaFinansalNesneId { get; set; }
    
    /// <summary>
    /// Katana referans ID'si
    /// </summary>
    [MaxLength(100)]
    public string? ReferenceId { get; set; }
    
    /// <summary>
    /// Müşteri grup kodu
    /// </summary>
    [MaxLength(50)]
    public string? GroupCode { get; set; }
    
    /// <summary>
    /// Varsayılan iskonto oranı (%)
    /// </summary>
    public decimal? DefaultDiscountRate { get; set; }
    
    /// <summary>
    /// Müşteri para birimi (Katana'dan, Luca'ya gönderilmez)
    /// </summary>
    [MaxLength(10)]
    public string? Currency { get; set; } = "TRY";
    
    public bool IsActive { get; set; } = true;
    
    public bool IsSynced { get; set; } = false;
    
    public DateTime? SyncedAt { get; set; }
    
    /// <summary>
    /// Son sync hatası
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
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    
    /// <summary>
    /// Luca cari kart kodunu oluşturur (CK-{Id})
    /// </summary>
    public string GenerateLucaCode() => $"CK-{Id}";
}

