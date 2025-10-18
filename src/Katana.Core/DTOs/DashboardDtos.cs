/*Arayüzün ihtiyaç duyacağı özet verileri taşımak için kullanılacak DTO'lar (örn: SyncSummaryDto, StatsDto).*/
namespace Katana.Core.DTOs;

public class DashboardStatsDto
{
    public decimal TotalSales { get; set; }
    public decimal TotalRevenue { get; set; }
    public int ProductCount { get; set; }
    public int CustomerCount { get; set; }
    public int LowStockCount { get; set; }
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
    public string Severity { get; set; } = "Info"; // örn: Info, Warning, Error
}
