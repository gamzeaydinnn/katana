
using Katana.Core.Enums;

namespace Katana.Core.Entities;





public class AuditRecord
{
    
    
    
    public int Id { get; set; }

    
    
    
    public AuditActionType ActionType { get; set; }

    
    
    
    public string EntityName { get; set; } = string.Empty;

    
    
    
    public int? EntityId { get; set; }

    
    
    
    public string? OldValues { get; set; }

    
    
    
    public string? NewValues { get; set; }

    
    
    
    public string PerformedBy { get; set; } = "System";

    
    
    
    public string? Description { get; set; }

    
    
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    
    
    
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
}
