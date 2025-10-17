using System.ComponentModel.DataAnnotations;

namespace Katana.Core.Entities;

public class AccountingRecord
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string TransactionNo { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    public string Type { get; set; } = string.Empty; // INCOME, EXPENSE
    
    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty; // SALE, PURCHASE, SALARY, RENT, etc.
    
    public decimal Amount { get; set; }
    
    [Required]
    [MaxLength(10)]
    public string Currency { get; set; } = "TRY";
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public int? InvoiceId { get; set; }
    
    public int? CustomerId { get; set; }
    
    [MaxLength(100)]
    public string? PaymentMethod { get; set; } // CASH, CREDIT_CARD, BANK_TRANSFER, etc.
    
    public DateTime TransactionDate { get; set; }
    
    public bool IsSynced { get; set; } = false;
    
    public DateTime? SyncedAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Invoice? Invoice { get; set; }
    public virtual Customer? Customer { get; set; }
}
