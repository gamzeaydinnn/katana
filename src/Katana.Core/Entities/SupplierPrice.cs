using System.ComponentModel.DataAnnotations.Schema;

namespace Katana.Core.Entities;

public class SupplierPrice
{
    public int Id { get; set; }

    public int SupplierId { get; set; }
    public int ProductId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual Supplier? Supplier { get; set; }
    public virtual Product? Product { get; set; }
}
