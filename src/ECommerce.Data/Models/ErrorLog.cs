using System.ComponentModel.DataAnnotations;

namespace ECommerce.Data.Models;

public class ErrorLog
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string IntegrationName { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
