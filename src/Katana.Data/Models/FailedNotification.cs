using System.ComponentModel.DataAnnotations;

namespace Katana.Data.Models;

public class FailedNotification
{
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string EventName { get; set; } = string.Empty; 

    
    public string Payload { get; set; } = string.Empty;

    public int RetryCount { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastRetryAt { get; set; }

    public DateTime? NextRetryAt { get; set; }

    [MaxLength(1000)]
    public string? LastError { get; set; }
}

