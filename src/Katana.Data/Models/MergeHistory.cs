using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Katana.Core.DTOs;
using Katana.Core.Entities;

namespace Katana.Data.Models;

/// <summary>
/// Represents a product merge operation history entry
/// </summary>
[Table("merge_history")]
public class MergeHistory
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("canonical_product_id")]
    public int CanonicalProductId { get; set; }

    [Required]
    [Column("canonical_product_name")]
    [MaxLength(500)]
    public string CanonicalProductName { get; set; } = string.Empty;

    [Required]
    [Column("canonical_product_sku")]
    [MaxLength(200)]
    public string CanonicalProductSKU { get; set; } = string.Empty;

    [Required]
    [Column("merged_product_ids")]
    public List<int> MergedProductIds { get; set; } = new();

    [Column("sales_orders_updated")]
    public int SalesOrdersUpdated { get; set; }

    [Column("boms_updated")]
    public int BOMsUpdated { get; set; }

    [Column("stock_movements_updated")]
    public int StockMovementsUpdated { get; set; }

    [Required]
    [Column("admin_user_id")]
    [MaxLength(100)]
    public string AdminUserId { get; set; } = string.Empty;

    [Required]
    [Column("admin_user_name")]
    [MaxLength(200)]
    public string AdminUserName { get; set; } = string.Empty;

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("status")]
    [MaxLength(50)]
    public MergeStatus Status { get; set; }

    [Column("reason")]
    public string? Reason { get; set; }

    [ForeignKey(nameof(CanonicalProductId))]
    public Product? CanonicalProduct { get; set; }
}
