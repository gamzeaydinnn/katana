using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Katana.Core.Entities;

/// <summary>
/// Katana Sales Order entity with Luca synchronization fields
/// </summary>
public class SalesOrder
{
    public int Id { get; set; }

    /// <summary>
    /// Katana API'den gelen orijinal sipariş ID
    /// </summary>
    public long KatanaOrderId { get; set; }

    [Required]
    [MaxLength(100)]
    public string OrderNo { get; set; } = string.Empty;

    public int CustomerId { get; set; }

    public DateTime? OrderCreatedDate { get; set; }

    public DateTime? DeliveryDate { get; set; }

    [MaxLength(3)]
    public string? Currency { get; set; } = "TRY";

    /// <summary>
    /// Katana API status (raw string): NOT_SHIPPED, OPEN, SHIPPED, DELIVERED, CANCELLED, etc.
    /// This is stored as-is from Katana API, not mapped to OrderStatus enum.
    /// </summary>
    [MaxLength(50)]
    public string Status { get; set; } = "NOT_SHIPPED";

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Total { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? TotalInBaseCurrency { get; set; }

    /// <summary>
    /// Döviz kuru (Katana API'den gelen conversion_rate)
    /// EUR, USD gibi dövizli siparişler için kullanılır
    /// </summary>
    [Column(TypeName = "decimal(18,6)")]
    public decimal? ConversionRate { get; set; }

    [MaxLength(500)]
    public string? AdditionalInfo { get; set; }

    [MaxLength(100)]
    public string? CustomerRef { get; set; }

    [MaxLength(50)]
    public string? Source { get; set; }

    public long? LocationId { get; set; }

    // ==================== Luca Integration Fields ====================

    /// <summary>
    /// Luca'da oluşturulan sipariş ID (EkleStsWsSiparisBaslik yanıtındaki siparisId)
    /// </summary>
    public int? LucaOrderId { get; set; }

    /// <summary>
    /// Luca belge serisi (örn: "SAT")
    /// </summary>
    [MaxLength(10)]
    public string? BelgeSeri { get; set; }

    /// <summary>
    /// Luca belge numarası
    /// </summary>
    [MaxLength(50)]
    public string? BelgeNo { get; set; }

    /// <summary>
    /// Belge düzenleme saati (HH:mm formatında)
    /// </summary>
    [MaxLength(5)]
    public string? DuzenlemeSaati { get; set; }

    /// <summary>
    /// Belge türü detay ID (varsayılan: 17 - Satış Siparişi)
    /// </summary>
    public int? BelgeTurDetayId { get; set; } = 17;

    /// <summary>
    /// Nakliye bedeli türü (0: Net, 1: Brüt)
    /// </summary>
    public int? NakliyeBedeliTuru { get; set; } = 0;

    /// <summary>
    /// Teklif/Sipariş türü (0: Standart, 1: Teklif, 2: Proforma)
    /// </summary>
    public int? TeklifSiparisTur { get; set; } = 0;

    /// <summary>
    /// Onay durumu (true: onaylı, false: onaysız)
    /// </summary>
    public bool OnayFlag { get; set; } = true;

    /// <summary>
    /// Luca'ya en son senkronizasyon zamanı
    /// </summary>
    public DateTime? LastSyncAt { get; set; }

    /// <summary>
    /// Son senkronizasyon hatası (varsa)
    /// </summary>
    [MaxLength(1000)]
    public string? LastSyncError { get; set; }

    /// <summary>
    /// Luca'ya senkronize edildi mi?
    /// </summary>
    public bool IsSyncedToLuca { get; set; } = false;

    // ==================== Admin Approval Fields ====================

    /// <summary>
    /// Admin tarafından onaylanma tarihi
    /// </summary>
    public DateTime? ApprovedDate { get; set; }

    /// <summary>
    /// Onaylayan admin kullanıcı adı
    /// </summary>
    [MaxLength(100)]
    public string? ApprovedBy { get; set; }

    /// <summary>
    /// Senkronizasyon durumu (Pending, InProgress, Completed, Failed)
    /// </summary>
    [MaxLength(50)]
    public string? SyncStatus { get; set; }

    // ==================== Navigation Properties ====================

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<SalesOrderLine> Lines { get; set; } = new List<SalesOrderLine>();

    // ==================== Timestamps ====================

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
