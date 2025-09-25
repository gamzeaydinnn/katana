using System.ComponentModel.DataAnnotations;

namespace ECommerce.Data.Models;

public class IntegrationLog
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string SyncType { get; set; } = string.Empty; // STOCK, INVOICE, CUSTOMER
    
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = string.Empty; // SUCCESS, FAILED, RUNNING
    
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    
    public DateTime? EndTime { get; set; }
    
    public TimeSpan? Duration => EndTime?.Subtract(StartTime);
    
    public int ProcessedRecords { get; set; }
    
    public int SuccessfulRecords { get; set; }
    
    public int FailedRecords { get; set; }
    
    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }
    
    [MaxLength(100)]
    public string? TriggeredBy { get; set; }
    
    public string? Details { get; set; } // JSON formatted details
    
    // Navigation properties
    public virtual ICollection<FailedSyncRecord> FailedRecords { get; set; } = new List<FailedSyncRecord>();
}