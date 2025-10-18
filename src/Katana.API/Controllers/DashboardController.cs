using Katana.Business.Interfaces;
using Katana.Business.Services;
using Katana.Core.DTOs;
using Katana.Data.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Katana.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IKatanaService _katanaService;
    private readonly IntegrationDbContext _context;
    private readonly ILogger<DashboardController> _logger;
    private readonly DashboardService _dashboardService;

    public DashboardController(
        DashboardService dashboardService,
        IKatanaService katanaService,
        IntegrationDbContext context,
        ILogger<DashboardController> logger)
    {
        _katanaService = katanaService;
        _context = context;
        _logger = logger;
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Katana API bağlantısı ve senkronizasyon istatistikleri
    /// </summary>
    [HttpGet("sync-stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSyncStats()
    {
        try
        {
            // Katana API bağlantısını kontrol et
            var isHealthy = await _katanaService.TestConnectionAsync();

            if (!isHealthy)
            {
                _logger.LogWarning("Katana API bağlantısı başarısız.");
                return Ok(new
                {
                    totalProducts = 0,
                    totalStock = 0,
                    lowStockItems = 0,
                    outOfStockItems = 0,
                    pendingSync = 0,
                    lastSyncDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"),
                    warning = "Katana API bağlantısı kurulamadı"
                });
            }

            // Ürünleri Katana API'den çek
            var products = await _katanaService.GetProductsAsync();

            var totalProducts = products.Count;
            var totalStock = products.Count(p => p.IsActive);
            var outOfStockItems = products.Count(p => !p.IsActive);
            var lowStockItems = 0;

            // Senkronizasyon loglarını DB'den çek
            var pendingSync = await _context.SyncOperationLogs
                .CountAsync(l => l.Status != "SUCCESS");

            var lastSync = await _context.SyncOperationLogs
                .OrderByDescending(l => l.EndTime ?? l.StartTime)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                totalProducts,
                totalStock,
                lowStockItems,
                outOfStockItems,
                pendingSync,
                lastSyncDate = (lastSync != null
                    ? (lastSync.EndTime ?? lastSync.StartTime).ToString("yyyy-MM-ddTHH:mm:ss")
                    : DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"))
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sync stats.");
            return StatusCode(500, new { error = "Failed to fetch sync stats", details = ex.Message });
        }
    }

    /// <summary>
    /// Dashboard istatistikleri
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats()
    {
        try
        {
            var stats = await _dashboardService.GetDashboardStatsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dashboard istatistikleri alınamadı.");
            return StatusCode(500, new { error = "İstatistikler alınamadı", details = ex.Message });
        }
    }

    /// <summary>
    /// En son sistem aktiviteleri
    /// </summary>
    [HttpGet("activities")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecentActivities()
    {
        try
        {
            var recentLogs = await _context.SyncOperationLogs
                .OrderByDescending(l => l.EndTime ?? l.StartTime)
                .Take(20)
                .ToListAsync();

            var activities = recentLogs.Select(log => new
            {
                id = log.Id,
                type = log.SyncType,
                message = log.Status == "SUCCESS"
                    ? $"{log.SyncType} - Başarılı"
                    : $"{log.SyncType} - Başarısız",
                timestamp = log.EndTime ?? log.StartTime,
                status = log.Status
            }).ToList();

            return Ok(new { data = activities, count = activities.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching recent activities");
            return StatusCode(500, new { error = "Failed to fetch recent activities", details = ex.Message });
        }
    }
}
