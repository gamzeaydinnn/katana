namespace Katana.Core.Entities;

/// <summary>
/// Katana ürünleri ile Luca stok kartları arasındaki eşleştirmeyi tutar.
/// Luca'da güncelleme endpoint'i olmadığından her değişiklikte yeni versiyon oluşturulur.
/// </summary>
public class ProductLucaMapping
{
    public int Id { get; set; }
    
    // Katana bilgileri
    public string KatanaProductId { get; set; } = string.Empty;
    public string KatanaSku { get; set; } = string.Empty;
    
    // Luca bilgileri
    /// <summary>
    /// Versiyonlu SKU: SKU-V2, SKU-V3...
    /// </summary>
    public string LucaStockCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Luca'dan dönen ID
    /// </summary>
    public long? LucaStockId { get; set; }
    
    // Versiyon yönetimi
    public int Version { get; set; } = 1;
    
    /// <summary>
    /// Her Katana ürünü için sadece 1 aktif kayıt olur
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    // Sync durumu: PENDING, SYNCED, FAILED
    public string SyncStatus { get; set; } = "PENDING";
    
    // Değişiklik kontrolü için son senkronize edilen veriler
    public string? SyncedProductName { get; set; }
    public decimal? SyncedPrice { get; set; }
    public int? SyncedVatRate { get; set; }
    public string? SyncedBarcode { get; set; }
    
    // Hata yönetimi
    public string? LastSyncError { get; set; }
    public DateTime? SyncedAt { get; set; }
    
    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
}
