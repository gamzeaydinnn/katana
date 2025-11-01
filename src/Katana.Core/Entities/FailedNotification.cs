using Katana.Core.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Katana.Core.Entities;

public class FailedNotification : IEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    public string EventName { get; set; } = null!;

    [Required]
    public string Payload { get; set; } = null!;

    public int RetryCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastRetryAt { get; set; }

    public DateTime? NextRetryAt { get; set; }

    public string? LastError { get; set; }
}
