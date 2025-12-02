using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Katana.Core.Entities;

public class PurchaseOrderItem
{
    public int Id { get; set; }

    public int PurchaseOrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }
    
    // ===== LUCA ENTEGRASYON ALANLARI =====
    
    /// <summary>
    /// Luca stok kart kodu - Luca'ya gönderilecek
    /// </summary>
    [StringLength(50)]
    public string? LucaStockCode { get; set; }
    
    /// <summary>
    /// Depo kodu (varsayılan: "01")
    /// </summary>
    [StringLength(10)]
    public string WarehouseCode { get; set; } = "01";
    
    /// <summary>
    /// KDV oranı (varsayılan: 20)
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal VatRate { get; set; } = 20;
    
    /// <summary>
    /// Birim kodu (varsayılan: "AD")
    /// </summary>
    [StringLength(10)]
    public string UnitCode { get; set; } = "AD";
    
    /// <summary>
    /// İndirim tutarı
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; }
    
    /// <summary>
    /// Luca'dan dönen detay satır ID'si
    /// </summary>
    public long? LucaDetailId { get; set; }
    
    // ===== NAVIGATION PROPERTIES =====
    
    public virtual PurchaseOrder? PurchaseOrder { get; set; }
    public virtual Product? Product { get; set; }
}
