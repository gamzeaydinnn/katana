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

        // Compute total stock from StockMovements (authoritative) for active products.
        var activeProductIds = await _context.Products.Where(p => p.IsActive).Select(p => p.Id).ToListAsync();
        var totalStock = 0;
        if (activeProductIds.Any())
        {
            totalStock = await _context.StockMovements
                .Where(sm => activeProductIds.Contains(sm.ProductId))
                .SumAsync(sm => (int?)sm.ChangeQuantity) ?? 0;
        }

        // Compute number of products with balance <= 5 using movement sums + fallback to snapshot
        var productBalances = await _context.StockMovements
            .GroupBy(sm => sm.ProductId)
            .Select(g => new { ProductId = g.Key, Balance = g.Sum(x => x.ChangeQuantity) })
            .ToListAsync();

        var balancesById = productBalances.ToDictionary(x => x.ProductId, x => x.Balance);
        var activeProducts = await _context.Products.Where(p => p.IsActive).ToListAsync();
        var criticalStock = activeProducts.Count(p => (balancesById.TryGetValue(p.Id, out var b) ? b : p.StockSnapshot) <= 5);
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
