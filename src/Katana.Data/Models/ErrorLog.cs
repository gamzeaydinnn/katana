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

    [MaxLength(4000)]
    public string? StackTrace { get; set; }

    [MaxLength(200)]
    public string? Operation { get; set; }

    /// <summary>
    /// Hata türü (örneğin Validation, API, Database, Sync)
    /// </summary>
    public ErrorType ErrorType { get; set; } = ErrorType.Unknown;

    /// <summary>
    /// Log seviyesi: Info, Warning, Error
    /// </summary>
    [MaxLength(20)]
    public string Level { get; set; } = "Error";
    
    /// <summary>
    /// Log kategorisi: Authentication, Sync, ExternalAPI, vb.
    /// </summary>
    [MaxLength(50)]
    public string? Category { get; set; }
    
    /// <summary>
    /// İşlemi yapan kullanıcı
    /// </summary>
    [MaxLength(100)]
    public string? User { get; set; }
    
    /// <summary>
    /// Ek context bilgisi
    /// </summary>
    [MaxLength(500)]
    public string? ContextData { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
