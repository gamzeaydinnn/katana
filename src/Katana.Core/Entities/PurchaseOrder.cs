using System.ComponentModel.DataAnnotations.Schema;
using Katana.Core.Enums;

namespace Katana.Core.Entities;

public class PurchaseOrder
{
    public int Id { get; set; }

    public string OrderNo { get; set; } = string.Empty;

    public int SupplierId { get; set; }

    public string? SupplierCode { get; set; }

    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Pending;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    public DateTime? ExpectedDate { get; set; }

    public bool IsSynced { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    
    public virtual Supplier? Supplier { get; set; }
    public virtual ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
}
