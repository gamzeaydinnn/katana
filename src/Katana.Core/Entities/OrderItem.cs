using System.ComponentModel.DataAnnotations.Schema;

namespace Katana.Core.Entities;

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    // Navigation
    public virtual Order? Order { get; set; }
    public virtual Product? Product { get; set; }
}
