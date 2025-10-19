using Katana.Business.Interfaces;
using Katana.Data.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Katana.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AnalyticsController : ControllerBase
{
    private readonly IKatanaService _katanaService;
    private readonly IntegrationDbContext _context;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(IKatanaService katanaService, IntegrationDbContext context, ILogger<AnalyticsController> logger)
    {
        _katanaService = katanaService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/Analytics/stock - Stok raporu
    /// </summary>
    [HttpGet("stock")]
    public async Task<IActionResult> GetStockReport()
    {
        try
        {
            var products = await _katanaService.GetProductsAsync();
            
            var report = products.Select(p => new
            {
                sku = p.SKU,
                name = p.Name,
                price = p.Price,
                isActive = p.IsActive,
                status = p.IsActive ? "Aktif" : "Pasif"
            }).ToList();

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating stock report");
            return StatusCode(500, new { message = "Stok raporu oluşturulamadı" });
        }
    }

    /// <summary>
    /// GET /api/Analytics/sync - Senkronizasyon raporu
    /// </summary>
    [HttpGet("sync")]
    public async Task<IActionResult> GetSyncReport()
    {
        try
        {
            var logs = await _context.SyncOperationLogs
                .OrderByDescending(l => l.StartTime)
                .Take(100)
                .Select(l => new
                {
                    syncType = l.SyncType,
                    status = l.Status,
                    startTime = l.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    duration = l.EndTime.HasValue 
                        ? $"{(l.EndTime.Value - l.StartTime).TotalSeconds:F1}s" 
                        : "N/A",
                    successCount = l.SuccessfulRecords,
                    failCount = l.FailedRecords
                })
                .ToListAsync();

            return Ok(logs);
        }
        catch (Microsoft.Data.Sqlite.SqliteException)
        {
            // Tablo henüz oluşturulmamış, boş liste döndür
            return Ok(new List<object>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating sync report");
            return StatusCode(500, new { message = "Sync raporu oluşturulamadı" });
        }
    }

    /// <summary>
    /// GET /api/Analytics/summary - Özet istatistikler
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummaryReport()
    {
        try
        {
            var products = await _katanaService.GetProductsAsync();
            
            List<Katana.Core.Entities.SyncOperationLog> recentSyncs;
            try
            {
                recentSyncs = await _context.SyncOperationLogs
                    .OrderByDescending(l => l.StartTime)
                    .Take(10)
                    .ToListAsync();
            }
            catch (Microsoft.Data.Sqlite.SqliteException)
            {
                recentSyncs = new List<Katana.Core.Entities.SyncOperationLog>();
            }

            var summary = new
            {
                totalProducts = products.Count,
                activeProducts = products.Count(p => p.IsActive),
                inactiveProducts = products.Count(p => !p.IsActive),
                totalSyncs = recentSyncs.Count,
                successfulSyncs = recentSyncs.Count(s => s.Status == "SUCCESS"),
                failedSyncs = recentSyncs.Count(s => s.Status != "SUCCESS"),
                lastSyncDate = recentSyncs.FirstOrDefault()?.StartTime.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating summary report");
            return StatusCode(500, new { message = "Özet rapor oluşturulamadı" });
        }
    }
}
