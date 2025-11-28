using System.ComponentModel.DataAnnotations;
using Katana.Core.Enums;

namespace Katana.Data.Models;
public class IntegrationLog
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string SyncType { get; set; } = string.Empty;

    [Required]
    public SyncStatus Status { get; set; } = SyncStatus.Pending;

    
    public DataSource Source { get; set; } = DataSource.Katana;

    
    
    
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    
    
    
    public DateTime? EndTime { get; set; }

    
    
    
    public TimeSpan? Duration => EndTime?.Subtract(StartTime);

    
    
    
    public int ProcessedRecords { get; set; }

    
    
    
    public int SuccessfulRecords { get; set; }

    
    
    
    public int FailedRecordsCount { get; set; }

    
    
    
    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    
    
    
    [MaxLength(100)]
    public string? TriggeredBy { get; set; }

    
    
    
    public string? Details { get; set; }

    
    
    
    public virtual ICollection<FailedSyncRecord> FailedRecords { get; set; } = new List<FailedSyncRecord>();
}
