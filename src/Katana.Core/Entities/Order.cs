using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Katana.Core.Enums;

namespace Katana.Core.Entities;

public class Order
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string OrderNo { get; set; } = string.Empty;

    [Required]
    public int CustomerId { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    [Required]
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; } = "TRY";

    public bool IsSynced { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    
    public virtual Customer? Customer { get; set; }
    public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
