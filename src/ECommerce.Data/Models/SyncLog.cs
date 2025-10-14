using System.ComponentModel.DataAnnotations;

namespace Katana.Data.Models;

public class SyncLog
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string IntegrationName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsSuccess { get; set; } = true;

    [MaxLength(2000)]
    public string? Details { get; set; }
}


