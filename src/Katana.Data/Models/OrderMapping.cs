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
    public string EntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// Katana API'den gelen orijinal sipariş ID veya referans kodu
    /// (Örn: PendingStockAdjustment.ExternalOrderId, SalesOrder.OrderNo)
    /// </summary>
    public string? ExternalOrderId { get; set; }
    
    /// <summary>
    /// Mapping oluşturulma tarihi
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Mapping güncelleme tarihi (Luca ID değiştirildiğinde)
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
