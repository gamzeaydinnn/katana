namespace Katana.Data.Models;

public class DashboardMetric
{
    public int Id { get; set; }
    public DateTime Hour { get; set; } 
    public int ErrorCount { get; set; }
    public int AuditCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

