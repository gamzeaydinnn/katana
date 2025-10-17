namespace Katana.Data.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string ActionType { get; set; } = string.Empty; // örn: CREATE, UPDATE, DELETE
        public string EntityName { get; set; } = string.Empty;
        public int? EntityId { get; set; }
        public string? Details { get; set; }
        public string? PerformedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? Changes { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
