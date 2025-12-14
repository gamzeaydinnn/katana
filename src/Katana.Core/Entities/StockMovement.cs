
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Katana.Core.Enums;

namespace Katana.Core.Entities;

public class StockMovement
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProductId { get; set; }

    [MaxLength(50)]
    public string ProductSku { get; set; } = string.Empty;

    [MaxLength(50)]
    public string SKU { get; set; } = string.Empty;

    public int ChangeQuantity { get; set; }

    [Required]
    public MovementType MovementType { get; set; }

    [MaxLength(200)]
    public string Reason { get; set; } = string.Empty;

    [MaxLength(100)]
    public string SourceDocument { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public DateTime MovementDate { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string WarehouseCode { get; set; } = "MAIN";

    public bool IsSynced { get; set; } = false;

    public DateTime? SyncedAt { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;
}
