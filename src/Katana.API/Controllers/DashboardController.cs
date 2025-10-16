using Katana.Infrastructure.Services;
using Katana.Core.Interfaces;
using Katana.Data.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Katana.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IKatanaStockService _katanaStockService;
    private readonly IntegrationDbContext _context;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IKatanaStockService katanaStockService,
        IntegrationDbContext context,
        ILogger<DashboardController> logger)
    {
        _katanaStockService = katanaStockService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard statistics
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            // Check Katana API health
            var isHealthy = await _katanaStockService.IsKatanaApiHealthyAsync();
            
            if (!isHealthy)
            {
                _logger.LogWarning("Katana API is not healthy");
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

            // Fetch products from Katana API
            var products = await _katanaStockService.GetAllProductsAsync(limit: 1000);
            
            // Calculate statistics based on model structure
            var totalProducts = products.Count;
            var totalStock = products.Sum(p => (decimal)p.Stock);
            var outOfStockItems = products.Count(p => p.Stock == 0);
            var lowStockItems = products.Count(p => p.Stock > 0 && p.Stock < 10);
            
            // Get sync logs from database
            var pendingSync = await _context.SyncLogs
                .CountAsync(l => !l.IsSuccess);
            
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
                lastSyncDate = lastSync?.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss") ?? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching dashboard stats");
            return StatusCode(500, new { error = "Failed to fetch dashboard stats", details = ex.Message });
        }
    }

    /// <summary>
    /// Get recent activities
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
                message = log.IsSuccess ? $"{log.IntegrationName} - Başarılı" : $"{log.IntegrationName} - Başarısız",
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


