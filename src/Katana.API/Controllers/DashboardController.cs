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

            // Basit istatistikler hesapla
            var totalProducts = products.Count;
            var totalStock = products.Count(p => p.IsActive);
            var outOfStockItems = products.Count(p => !p.IsActive);
            var lowStockItems = 0;

            // Senkronizasyon loglarını DB'den çek
            var pendingSync = await _context.SyncLogs.CountAsync(l => !l.IsSuccess);
            var lastSync = await _context.SyncLogs
                .OrderByDescending(l => l.CreatedAt)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                totalProducts,
                totalStock,
                lowStockItems,
                outOfStockItems,
                pendingSync,
                lastSyncDate = lastSync?.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss") 
                               ?? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sync stats.");
            return StatusCode(500, new { error = "Failed to fetch sync stats", details = ex.Message });
        }
    }

    /// <summary>
    /// Uygulama veritabanından dashboard istatistiklerini döner (ürün, stok, müşteri, gelir vb.)
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
    /// En son sistem aktivitelerini döner (örnek: senkronizasyon, hata logları)
    /// </summary>
    [HttpGet("activities")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecentActivities()
    {
        try
        {
            var recentLogs = await _context.SyncLogs
                .OrderByDescending(l => l.CreatedAt)
                .Take(20)
                .ToListAsync();

            var activities = recentLogs.Select(log => new
            {
                id = log.Id,
                type = log.IntegrationName,
                message = log.IsSuccess
                    ? $"{log.IntegrationName} - Başarılı"
                    : $"{log.IntegrationName} - Başarısız",
                timestamp = log.CreatedAt,
                status = log.IsSuccess ? "Success" : "Failed"
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
