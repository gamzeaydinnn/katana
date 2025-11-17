using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

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
    
    // Persisted legacy stock snapshot (kept for compatibility with existing migrations/usages)
    [Column("Stock")]
    public int StockSnapshot { get; set; }

    // Exposed Stock property: prefer calculated value from StockMovements when present.
    // Marked NotMapped so EF won't treat it as a separate column.
    [NotMapped]
    public int Stock
    {
        get => (StockMovements != null && StockMovements.Any()) ? StockMovements.Sum(x => x.ChangeQuantity) : StockSnapshot;
        set => StockSnapshot = value;
    }
    
    public int CategoryId { get; set; }
    
    [MaxLength(500)]
    public string? MainImageUrl { get; set; }
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    
    // Local stock snapshot/history entries (kept separate from StockMovement used for integration)
    public virtual ICollection<Stock> Stocks { get; set; } = new List<Stock>();
}

