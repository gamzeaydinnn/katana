
using System;

namespace Katana.Core.Entities;

public class AuditLog
{
    public int Id { get; set; }

    public string ActionType { get; set; } = string.Empty;

    public string EntityName { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public string PerformedBy { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string? Details { get; set; }

    public string? Changes { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }
}
