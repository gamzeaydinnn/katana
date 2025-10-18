/*DashboardService için kontrat.

Diğer interfaceler (ISyncService, IMappingService vb.) yeni metotlar için güncellenecek.*/

using Katana.Core.DTOs;

namespace Katana.Business.Interfaces;

public interface IDashboardService
{
    /// <summary>
    /// Genel istatistikleri döner (ürün sayısı, müşteri sayısı, gelir, stok durumu vb.)
    /// </summary>
    /// <returns>DashboardStatsDto</returns>
    Task<DashboardStatsDto> GetDashboardStatsAsync();

    /// <summary>
    /// En son sistem aktivitelerini döner (örneğin senkronizasyonlar, hatalar, güncellemeler)
    /// </summary>
    /// <returns>Aktivite listesi (örnek: log veya SyncLog tablosundan)</returns>
    Task<IEnumerable<ActivityLogDto>> GetRecentActivitiesAsync();

    /// <summary>
    /// Düşük stok uyarısı, başarısız senkronizasyon veya sistem hataları gibi uyarıları döner.
    /// </summary>
    /// <returns>Bildirim/uyarı listesi</returns>
    Task<IEnumerable<NotificationDto>> GetSystemAlertsAsync();
}
