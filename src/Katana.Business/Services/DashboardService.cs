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
        var totalSales = await _context.Invoices.SumAsync(i => i.TotalAmount);
        var totalRevenue = await _context.AccountingRecords.SumAsync(a => a.Amount);
        var totalProducts = await _context.Products.CountAsync();
        var totalCustomers = await _context.Customers.CountAsync();
        var lowStock = await _context.Products.CountAsync(p => p.Stock < 10);

        return new DashboardStatsDto
        {
            TotalSales = totalSales,
            TotalRevenue = totalRevenue,
            ProductCount = totalProducts,
            CustomerCount = totalCustomers,
            LowStockCount = lowStock
        };
    }
}
