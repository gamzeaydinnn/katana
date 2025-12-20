using Katana.Core.Enums;
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

    public string? StackTrace { get; set; }

    [MaxLength(200)]
    public string? Operation { get; set; }

    
    
    
    public ErrorType ErrorType { get; set; } = ErrorType.Unknown;

    
    
    
    [MaxLength(20)]
    public string Level { get; set; } = "Error";
    
    
    
    
    [MaxLength(50)]
    public string? Category { get; set; }
    
    
    
    
    [MaxLength(100)]
    public string? User { get; set; }
    
    
    
    
    [MaxLength(1000)]
    public string? ContextData { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
