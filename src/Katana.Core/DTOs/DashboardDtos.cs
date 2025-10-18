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
