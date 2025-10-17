using System.ComponentModel.DataAnnotations;

namespace Katana.Data.Models;

public class ErrorLog
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string IntegrationName { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? StackTrace { get; set; }

    [MaxLength(200)]
    public string? Operation { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
