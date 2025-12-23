using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Katana.Core.Entities;

/// <summary>
/// Sales Order line item with Luca synchronization fields
/// </summary>
public class SalesOrderLine
{
    public int Id { get; set; }

    public int SalesOrderId { get; set; }

    /// <summary>
    /// Katana'daki SalesOrderRow ID
    /// </summary>
    public long KatanaRowId { get; set; }

    /// <summary>
    /// Katana Variant ID
    /// </summary>
    public long VariantId { get; set; }

    [Required]
    [MaxLength(100)]
    public string SKU { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ProductName { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? PricePerUnit { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? PricePerUnitInBaseCurrency { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Total { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? TotalInBaseCurrency { get; set; }

    /// <summary>
    /// KDV oranı (örn: 20)
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? TaxRate { get; set; }

    public long? TaxRateId { get; set; }

    public long? LocationId { get; set; }

    [MaxLength(50)]
    public string? ProductAvailability { get; set; }

    public DateTime? ProductExpectedDate { get; set; }

    // ==================== Luca Integration Fields ====================

    /// <summary>
    /// Luca'daki sipariş detay ID
    /// </summary>
    public int? LucaDetayId { get; set; }

    /// <summary>
    /// Luca stok ID (Product.LucaStockId ile eşleşir)
    /// </summary>
    public int? LucaStokId { get; set; }

    /// <summary>
    /// Luca depo ID (Location.LucaDepoId ile eşleşir)
    /// </summary>
    public int? LucaDepoId { get; set; }

    /// <summary>
    /// Katana'ya gönderildiğinde oluşturulan order ID
    /// Her satır için ayrı order oluşturulmuşsa bu değer farklı olabilir
    /// </summary>
    public int? KatanaOrderId { get; set; }

    // ==================== Navigation Properties ====================

    public virtual SalesOrder? SalesOrder { get; set; }

    // ==================== Timestamps ====================

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
