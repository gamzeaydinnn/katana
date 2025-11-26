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

    
    public virtual PurchaseOrder? PurchaseOrder { get; set; }
    public virtual Product? Product { get; set; }
}
