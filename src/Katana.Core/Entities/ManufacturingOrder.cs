using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Katana.Core.Entities;

public class ManufacturingOrder
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string OrderNo { get; set; } = string.Empty;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Quantity { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "NotStarted";

    public DateTime DueDate { get; set; }

    public bool IsSynced { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
