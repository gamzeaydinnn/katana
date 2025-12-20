
namespace Katana.Core.DTOs;

public class DashboardStatsDto
{
    public int TotalProducts { get; set; }
    public int TotalStock { get; set; }
    public int CriticalStock { get; set; }
    public int PendingSync { get; set; }
    public decimal TotalSales { get; set; }
}
public class ActivityLogDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Status { get; set; } = "Info";
}

public class NotificationDto
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Severity { get; set; } = "Info"; 
}
