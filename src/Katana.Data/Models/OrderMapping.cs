using System.ComponentModel.DataAnnotations;

namespace Katana.Data.Models;

/// <summary>
/// Katana Order ID'lerini Luca Invoice ID'lerine map eden tablo
/// İdempotency için kullanılır - aynı sipariş 2 kez Luca'ya gönderilmemesi için
/// </summary>
public class OrderMapping
{
    public int Id { get; set; }
    
    /// <summary>
    /// Katana Order ID (Backend database'deki Order.Id)
    /// </summary>
    public int OrderId { get; set; }
    
    /// <summary>
    /// Luca'dan dönen fatura/sipariş ID
    /// </summary>
    public long LucaInvoiceId { get; set; }
    
    /// <summary>
    /// Entity tipi: "SalesOrder", "PurchaseOrder", "Invoice" vb.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// Katana API'den gelen orijinal sipariş ID veya referans kodu
    /// (Örn: PendingStockAdjustment.ExternalOrderId, SalesOrder.OrderNo)
    /// </summary>
    [MaxLength(200)]
    public string? ExternalOrderId { get; set; }
    
    /// <summary>
    /// Koza'da kullanılan belge serisi
    /// </summary>
    [MaxLength(10)]
    public string? BelgeSeri { get; set; }

    /// <summary>
    /// Koza'da kullanılan belge numarası
    /// </summary>
    [MaxLength(50)]
    public string? BelgeNo { get; set; }

    /// <summary>
    /// Belge takip numarası (Katana order_no veya externalOrderId)
    /// </summary>
    [MaxLength(100)]
    public string? BelgeTakipNo { get; set; }
    
    /// <summary>
    /// Senkronizasyon durumu: PENDING, SYNCED, FAILED
    /// </summary>
    [MaxLength(20)]
    public string SyncStatus { get; set; } = "SYNCED";
    
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
    /// Mapping oluşturulma tarihi
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Mapping güncelleme tarihi (Luca ID değiştirildiğinde)
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Son senkronize edilen tarih
    /// </summary>
    public DateTime? LastSyncAt { get; set; }
}
