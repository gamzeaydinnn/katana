using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Katana.Core.Entities;
public class StockTransfer
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string FromWarehouse { get; set; } = string.Empty;

    [MaxLength(50)]
    public string ToWarehouse { get; set; } = string.Empty;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Quantity { get; set; }

    public DateTime TransferDate { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    public string Status { get; set; } = "Pending";
}
