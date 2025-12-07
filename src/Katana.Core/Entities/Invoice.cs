using System.ComponentModel.DataAnnotations;
using Katana.Core.Enums;

namespace Katana.Core.Entities;
public class Invoice
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string InvoiceNo { get; set; } = string.Empty;
    
    public int CustomerId { get; set; }
    
    public decimal Amount { get; set; }
    
    public decimal TaxAmount { get; set; }
    
    public decimal TotalAmount { get; set; }
    
    // ✅ FIX: Güvenli enum kullanımı (string yerine)
    // Database'de string olarak saklanıyor ama code'da type-safe enum
    [Required]
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft; 
    
    public DateTime InvoiceDate { get; set; }
    
    public DateTime? DueDate { get; set; }
    
    [MaxLength(10)]
    public string Currency { get; set; } = "TRY";
    
    [MaxLength(1000)]
    public string? Notes { get; set; }
    
    public bool IsSynced { get; set; } = false;
    
    public DateTime? SyncedAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    
    public virtual Customer Customer { get; set; } = null!;
    public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
}

