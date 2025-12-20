using System.ComponentModel.DataAnnotations;

namespace Katana.Data.Models;

public class FailedSyncRecord
{
    public int Id { get; set; }
    
    public int IntegrationLogId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string RecordType { get; set; } = string.Empty; 
    
    [MaxLength(100)]
    public string? RecordId { get; set; } 
    
    public string OriginalData { get; set; } = string.Empty; 
    
    [MaxLength(2000)]
    public string ErrorMessage { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? ErrorCode { get; set; }
    
    public DateTime FailedAt { get; set; } = DateTime.UtcNow;
    
    public int RetryCount { get; set; } = 0;
    
    public DateTime? LastRetryAt { get; set; }
    
    public DateTime? NextRetryAt { get; set; }
    
    [MaxLength(20)]
    public string Status { get; set; } = "FAILED"; 
    
    [MaxLength(1000)]
    public string? Resolution { get; set; }
    
    public DateTime? ResolvedAt { get; set; }
    
    [MaxLength(100)]
    public string? ResolvedBy { get; set; }
    
    
    public virtual IntegrationLog IntegrationLog { get; set; } = null!;
}

