using System.ComponentModel.DataAnnotations;

namespace Katana.Core.Entities;

public class Stock
{
    public int Id { get; set; }
    
    public int ProductId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Location { get; set; } = string.Empty;
    
    public int Quantity { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty; 
    
    [MaxLength(500)]
    public string? Reason { get; set; }
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [MaxLength(100)]
    public string? Reference { get; set; } 
    
    public bool IsSynced { get; set; } = false;
    
    public DateTime? SyncedAt { get; set; }
    
    
    public virtual Product Product { get; set; } = null!;
}

