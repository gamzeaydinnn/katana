using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Katana.Business.Services;

public class DashboardService : IDashboardService
{
    private readonly IntegrationDbContext _context;

    public DashboardService(IntegrationDbContext context)
    {
        _context = context;
    }

    
    
    
    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        
        var totalProducts = await _context.Products.CountAsync();

        
        var activeProductIds = await _context.Products.Where(p => p.IsActive).Select(p => p.Id).ToListAsync();
        var totalStock = 0;
        if (activeProductIds.Any())
        {
            totalStock = await _context.StockMovements
                .Where(sm => activeProductIds.Contains(sm.ProductId))
                .SumAsync(sm => (int?)sm.ChangeQuantity) ?? 0;
        }

        
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

    /// <summary>
    /// Son aktiviteleri getir
    /// </summary>
    public async Task<IEnumerable<ActivityLogDto>> GetRecentActivitiesAsync()
    {
        // AuditLogs'dan son aktiviteleri getir
        var activities = await _context.AuditLogs
            .OrderByDescending(a => a.Timestamp)
            .Take(20)
            .Select(a => new ActivityLogDto
            {
                Id = a.Id,
                Type = a.ActionType,
                Message = a.Details ?? $"{a.EntityName} {a.ActionType}",
                Timestamp = a.Timestamp,
                Status = "Info"
            })
            .ToListAsync();

        return activities;
    }

    /// <summary>
    /// Sistem uyarılarını getir
    /// </summary>
    public async Task<IEnumerable<NotificationDto>> GetSystemAlertsAsync()
    {
        // Okunmamış bildirimleri getir
        var alerts = await _context.Notifications
            .Where(n => !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Take(10)
            .Select(n => new NotificationDto
            {
                Title = n.Title ?? n.Type,
                Message = n.Payload ?? "No details available",
                CreatedAt = n.CreatedAt,
                Severity = "Info"
            })
            .ToListAsync();

        return alerts;
    }
}
