using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Katana.Core.Enums;

namespace Katana.Core.Entities;

public class PurchaseOrder
{
    public int Id { get; set; }

    public string OrderNo { get; set; } = string.Empty;

    public int SupplierId { get; set; }

    public string? SupplierCode { get; set; }

    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Pending;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    public DateTime? ExpectedDate { get; set; }

    public bool IsSynced { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // ===== LUCA ENTEGRASYON ALANLARI =====
    
    /// <summary>
    /// Luca'dan dönen ssSatinalmaSiparisBaslikId
    /// </summary>
    public long? LucaPurchaseOrderId { get; set; }
    
    /// <summary>
    /// Luca'da oluşan belge numarası
    /// </summary>
    [StringLength(20)]
    public string? LucaDocumentNo { get; set; }
    
    /// <summary>
    /// Belge serisi (örn: "A", "B")
    /// </summary>
    [StringLength(10)]
    public string? DocumentSeries { get; set; }
    
    /// <summary>
    /// Belge türü detay ID - varsayılan 2 (Satınalma Siparişi)
    /// </summary>
    public int DocumentTypeDetailId { get; set; } = 2;
    
    /// <summary>
    /// KDV dahil mi? (kdvFlag)
    /// </summary>
    public bool VatIncluded { get; set; } = true;
    
    /// <summary>
    /// Özel kod - idempotency için kullanılır (Katana sipariş numarası)
    /// </summary>
    [StringLength(100)]
    public string? ReferenceCode { get; set; }
    
    /// <summary>
    /// Proje kodu (opsiyonel)
    /// </summary>
    [StringLength(50)]
    public string? ProjectCode { get; set; }
    
    /// <summary>
    /// Açıklama - Luca'ya gönderilecek not
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Sevk adresi ID (opsiyonel)
    /// </summary>
    public long? ShippingAddressId { get; set; }
    
    /// <summary>
    /// Luca ile senkronize edildi mi?
    /// </summary>
    public bool IsSyncedToLuca { get; set; }
    
    /// <summary>
    /// Son senkronizasyon tarihi
    /// </summary>
    public DateTime? LastSyncAt { get; set; }
    
    /// <summary>
    /// Son senkronizasyon hatası
    /// </summary>
    [StringLength(2000)]
    public string? LastSyncError { get; set; }
    
    /// <summary>
    /// Sync retry sayısı
    /// </summary>
    public int SyncRetryCount { get; set; }
    
    // ===== NAVIGATION PROPERTIES =====
    
    public virtual Supplier? Supplier { get; set; }
    public virtual ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
}
