using System.ComponentModel.DataAnnotations;

namespace ECommerce.Core.Entities;

public class InvoiceItem
{
    public int Id { get; set; }
    
    public int InvoiceId { get; set; }
    
    public int ProductId { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string ProductSKU { get; set; } = string.Empty;
    
    public int Quantity { get; set; }
    
    public decimal UnitPrice { get; set; }
    
    public decimal TaxRate { get; set; } = 0.18m; // Default 18% KDV
    
    public decimal TaxAmount { get; set; }
    
    public decimal TotalAmount { get; set; }
    
    [MaxLength(20)]
    public string Unit { get; set; } = "ADET";
    
    // Navigation properties
    public virtual Invoice Invoice { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}