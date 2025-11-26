using System.ComponentModel.DataAnnotations;

namespace Katana.Core.Entities;

public class Notification
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Type { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    
    public string? Payload { get; set; }

    public string? Link { get; set; }

    public bool IsRead { get; set; } = false;

    public long? RelatedPendingId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
