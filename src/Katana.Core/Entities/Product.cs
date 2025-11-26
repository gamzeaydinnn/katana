using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Katana.Core.Enums;

namespace Katana.Core.Entities;

public class Product
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string SKU { get; set; } = string.Empty;
    
    public decimal Price { get; set; }
    
    
    [Column("Stock")]
    public int StockSnapshot { get; set; }

    
    
    [NotMapped]
    public int Stock
    {
        get
        {
            return (StockMovements != null && StockMovements.Any())
                ? StockMovements.Sum(x => x.ChangeQuantity)
                : StockSnapshot;
        }
        set
        {
            
            var current = (StockMovements != null && StockMovements.Any()) ? StockMovements.Sum(x => x.ChangeQuantity) : StockSnapshot;

            
            var delta = value - current;
            if (delta == 0) return;

            
            
            if (Id == 0 && (StockMovements == null || !StockMovements.Any()))
            {
                StockSnapshot = value;
                return;
            }

            
            
            var movement = new StockMovement
            {
                ProductId = this.Id,
                ProductSku = this.SKU,
                ChangeQuantity = delta,
                MovementType = delta > 0 ? MovementType.In : MovementType.Out,
                SourceDocument = "AutoSet",
                Timestamp = DateTime.UtcNow,
                WarehouseCode = "MAIN",
                IsSynced = false
            };

            StockMovements ??= new List<StockMovement>();
            StockMovements.Add(movement);
        }
    }
    
    public int CategoryId { get; set; }
    
    [MaxLength(500)]
    public string? MainImageUrl { get; set; }
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    
    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    
    
    public virtual ICollection<Stock> Stocks { get; set; } = new List<Stock>();
    public virtual ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public virtual ICollection<Batch> Batches { get; set; } = new List<Batch>();
    public virtual ICollection<BillOfMaterials> BillOfMaterials { get; set; } = new List<BillOfMaterials>();
}
