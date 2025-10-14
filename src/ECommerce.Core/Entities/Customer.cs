using System.ComponentModel.DataAnnotations;

namespace Katana.Core.Entities;

public class Customer
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(11)]
    public string TaxNo { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? ContactPerson { get; set; }
    
    [MaxLength(20)]
    public string? Phone { get; set; }
    
    [MaxLength(100)]
    public string? Email { get; set; }
    
    [MaxLength(500)]
    public string? Address { get; set; }
    
    [MaxLength(100)]
    public string? City { get; set; }
    
    [MaxLength(50)]
    public string? Country { get; set; } = "Turkey";
    
    public bool IsActive { get; set; } = true;
    
    public bool IsSynced { get; set; } = false;
    
    public DateTime? SyncedAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}

