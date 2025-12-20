



using Katana.Core.DTOs;

namespace Katana.Business.Interfaces;

public interface IDashboardService
{
    
    
    
    
    Task<DashboardStatsDto> GetDashboardStatsAsync();

    
    
    
    
    Task<IEnumerable<ActivityLogDto>> GetRecentActivitiesAsync();

    
    
    
    
    Task<IEnumerable<NotificationDto>> GetSystemAlertsAsync();
}
