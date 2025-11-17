/*DashboardController'ın ihtiyaç duyduğu özet verileri ve istatistikleri hazırlayacak olan servis.
Amacı: Veri katmanındaki log ve hata kayıtlarını analiz edip anlamlı çıktılara dönüştürmek.
Sorumlulukları:
IntegrationLogRepository ve FailedSyncRecordRepository'den verileri okuyup özet bilgiler oluşturmak.
Başarı/hata oranlarını hesaplamak.*/
using Katana.Core.DTOs;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Katana.Business.Services;

public class DashboardService
{
    private readonly IntegrationDbContext _context;

    public DashboardService(IntegrationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Yönetim paneli için özet istatistikleri döner.
    /// </summary>
    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        // Tüm ürünleri say (Canlı Stok ile tutarlılık için)
        var totalProducts = await _context.Products.CountAsync();
        var totalStock = await _context.Products.Where(p => p.IsActive).SumAsync(p => p.Stock);
        var criticalStock = await _context.Products.CountAsync(p => p.Stock <= 5 && p.IsActive);
        var pendingSync = await _context.PendingStockAdjustments.CountAsync(p => p.Status == "Pending");
        var totalSales = await _context.Invoices
            .Where(i => i.Status == "PAID")
            .SumAsync(i => (decimal?)i.TotalAmount) ?? 0m;

        return new DashboardStatsDto
        {
            TotalProducts = totalProducts,
            TotalStock = totalStock,
            CriticalStock = criticalStock,
            PendingSync = pendingSync,
            TotalSales = totalSales
        };
    }
}
