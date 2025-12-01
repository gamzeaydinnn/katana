using Katana.Business.Interfaces;
using Katana.Business.Services;
using Katana.Core.Enums;
using Katana.Core.DTOs;
using Katana.Data.Context;
using Katana.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Katana.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IntegrationDbContext _context;
    private readonly ILogger<ReportsController> _logger;
    private readonly IKatanaService _katanaService;
    private readonly DashboardService _dashboardService;

    public ReportsController(
        IntegrationDbContext context,
        ILogger<ReportsController> logger,
        IKatanaService katanaService,
        DashboardService dashboardService)
    {
        _context = context;
        _logger = logger;
        _katanaService = katanaService;
        _dashboardService = dashboardService;
    }

    
    
    
    [HttpGet("logs")]
    public async Task<ActionResult<object>> GetIntegrationLogs(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 50,
        [FromQuery] string? syncType = null,
        [FromQuery] string? status = null)
    {
        try
        {
            var query = _context.IntegrationLogs.AsQueryable();

            if (!string.IsNullOrEmpty(syncType))
            {
                query = query.Where(l => l.SyncType == syncType.ToUpper());
            }

            if (!string.IsNullOrEmpty(status))
            {
                
                if (Enum.TryParse<SyncStatus>(status, true, out var syncStatus))
                {
                    query = query.Where(l => l.Status == syncStatus);
                }
            }

            var totalCount = await query.CountAsync();
            var logs = await query
                .OrderByDescending(l => l.StartTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new
                {
                    l.Id,
                    l.SyncType,
                    l.Status,
                    l.StartTime,
                    l.EndTime,
                    l.Duration,
                    l.ProcessedRecords,
                    l.SuccessfulRecords,
                    l.FailedRecords,
                    l.ErrorMessage,
                    l.TriggeredBy
                })
                .ToListAsync();

            return Ok(new
            {
                logs,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving integration logs");
            return StatusCode(500, new { error = "Internal server error retrieving logs" });
        }
    }

    
    
    
    [HttpGet("last")]
    public async Task<ActionResult<object>> GetLastSyncReports()
    {
        try
        {
            var syncTypes = new[] { "STOCK", "INVOICE", "CUSTOMER" };
            var reports = new List<object>();

            foreach (var syncType in syncTypes)
            {
                var lastLog = await _context.IntegrationLogs
                    .Where(l => l.SyncType == syncType)
                    .OrderByDescending(l => l.StartTime)
                    .FirstOrDefaultAsync();

                if (lastLog != null)
                {
                    reports.Add(new
                    {
                        syncType,
                        lastLog.Status,
                        lastLog.StartTime,
                        lastLog.EndTime,
                        lastLog.Duration,
                        lastLog.ProcessedRecords,
                        lastLog.SuccessfulRecords,
                        lastLog.FailedRecords,
                        lastLog.ErrorMessage
                    });
                }
                else
                {
                    reports.Add(new
                    {
                        syncType,
                        Status = "NEVER_RUN",
                        StartTime = (DateTime?)null,
                        EndTime = (DateTime?)null,
                        Duration = (TimeSpan?)null,
                        ProcessedRecords = 0,
                        SuccessfulRecords = 0,
                        FailedRecords = 0,
                        ErrorMessage = (string?)null
                    });
                }
            }

            return Ok(reports);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving last sync reports");
            return StatusCode(500, new { error = "Internal server error retrieving reports" });
        }
    }

    
    
    
    [HttpGet("failed")]
    public async Task<ActionResult<object>> GetFailedRecords(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? recordType = null,
        [FromQuery] string? status = null)
    {
        try
        {
            var query = _context.FailedSyncRecords
                .Include(f => f.IntegrationLog)
                .AsQueryable();

            // Status filtresi
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(f => f.Status == status.ToUpper());
            }
            else
            {
                query = query.Where(f => f.Status == "FAILED");
            }

            if (!string.IsNullOrEmpty(recordType))
            {
                query = query.Where(f => f.RecordType == recordType.ToUpper());
            }

            var totalCount = await query.CountAsync();
            var failedRecords = await query
                .OrderByDescending(f => f.FailedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new
                {
                    f.Id,
                    f.RecordType,
                    f.RecordId,
                    f.ErrorMessage,
                    // Kullanıcı dostu hata mesajı
                    UserFriendlyError = Katana.Core.Helpers.UserFriendlyMessages.TranslateError(f.ErrorMessage, f.ErrorCode),
                    f.ErrorCode,
                    f.FailedAt,
                    f.RetryCount,
                    f.LastRetryAt,
                    f.NextRetryAt,
                    f.Status,
                    SourceSystem = f.IntegrationLog != null ? 
                        (f.IntegrationLog.SyncType.Contains("Katana") ? "KATANA" : "LUCA") : "SYSTEM",
                    IntegrationLog = f.IntegrationLog != null ? new
                    {
                        f.IntegrationLog.SyncType,
                        f.IntegrationLog.StartTime
                    } : null
                })
                .ToListAsync();

            return Ok(new
            {
                failedRecords,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving failed records");
            return StatusCode(500, new { error = "Hatalı kayıtlar yüklenirken bir sorun oluştu" });
        }
    }

    
    
    
    [HttpGet("statistics")]
    public async Task<ActionResult<object>> GetSyncStatistics([FromQuery] int days = 7)
    {
        try
        {
            var fromDate = DateTime.UtcNow.AddDays(-days);
            
            var statistics = await _context.IntegrationLogs
                .Where(l => l.StartTime >= fromDate)
                .GroupBy(l => l.SyncType)
                .Select(g => new
                {
                    SyncType = g.Key,
                    TotalRuns = g.Count(),
                    SuccessfulRuns = g.Count(l => l.Status == SyncStatus.Success),
                    FailedRuns = g.Count(l => l.Status == SyncStatus.Failed),
                    TotalProcessedRecords = g.Sum(l => l.ProcessedRecords),
                    TotalSuccessfulRecords = g.Sum(l => l.SuccessfulRecords),
                    TotalFailedRecords = g.Sum(l => l.FailedRecordsCount),
                    AverageDuration = g.Where(l => l.Duration.HasValue)
                                     .Average(l => l.Duration!.Value.TotalSeconds),
                    LastRunTime = g.Max(l => l.StartTime)
                })
                .ToListAsync();

            var overallStats = new
            {
                TotalSyncRuns = await _context.IntegrationLogs.CountAsync(l => l.StartTime >= fromDate),
                TotalFailedRecords = await _context.FailedSyncRecords.CountAsync(f => f.FailedAt >= fromDate),
                ActiveFailedRecords = await _context.FailedSyncRecords.CountAsync(f => f.Status == "FAILED"),
                SyncTypeStatistics = statistics
            };

            return Ok(overallStats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sync statistics");
            return StatusCode(500, new { error = "Internal server error retrieving statistics" });
        }
    }

    
    
    
    [HttpGet("stock")]
    [HttpGet("~/api/Analytics/stock")]
    [AllowAnonymous] 
    public async Task<ActionResult<object>> GetStockReport(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        [FromQuery] string? search = null,
        [FromQuery] bool lowStockOnly = false)
    {
        try
        {
            var query = _context.Products.AsQueryable();

            
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => 
                    p.Name.Contains(search) || 
                    p.SKU.Contains(search));
            }

            
            var initialProducts = await query.Select(p => new
            {
                p.Id,
                p.Name,
                p.SKU,
                CategoryId = p.CategoryId,
                Balance = _context.StockMovements.Where(sm => sm.ProductId == p.Id).Sum(sm => (int?)sm.ChangeQuantity) ?? p.StockSnapshot,
                Price = p.Price,
                IsActive = p.IsActive,
                MainImageUrl = p.MainImageUrl,
                Description = p.Description,
                LastUpdated = p.UpdatedAt
            }).ToListAsync();

            
            if (lowStockOnly)
            {
                initialProducts = initialProducts.Where(p => p.Balance <= 10).ToList();
            }

            var totalCount = initialProducts.Count;

            var ordered = initialProducts.OrderByDescending(p => p.Balance * p.Price).ToList();

            var stockData = ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.SKU,
                    CategoryId = p.CategoryId,
                    Quantity = Math.Max(p.Balance, 0),
                    Price = p.Price,
                    StockValue = p.Balance * p.Price,
                    IsLowStock = p.Balance <= 10,
                    IsOutOfStock = p.Balance == 0,
                    IsActive = p.IsActive,
                    MainImageUrl = p.MainImageUrl,
                    Description = p.Description,
                    LastUpdated = p.LastUpdated
                })
                .ToList();

            
            var totalStockValue = stockData.Sum(s => s.StockValue);
            var lowStockCount = stockData.Count(s => s.IsLowStock);
            var outOfStockCount = stockData.Count(s => s.IsOutOfStock);
            var activeProductsCount = stockData.Count(s => s.IsActive);

            return Ok(new
            {
                stockData,
                summary = new
                {
                    totalProducts = totalCount,
                    totalStockValue,
                    averagePrice = totalCount > 0 ? stockData.Average(s => s.Price) : 0,
                    totalStock = stockData.Sum(s => s.Quantity),
                    lowStockCount,
                    outOfStockCount,
                    activeProductsCount
                },
                pagination = new
                {
                    page,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating stock report");
            return StatusCode(500, new { error = "Failed to generate stock report" });
        }
    }

    
    
    
    [HttpGet("sync")]
    [HttpGet("~/api/Analytics/sync")]
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating sync report");
            return StatusCode(500, new { message = "Sync raporu oluşturulamadı" });
        }
    }

    
    
    
    [HttpGet("summary")]
    [HttpGet("~/api/Analytics/summary")]
    public async Task<IActionResult> GetSummaryReport()
    {
        try
        {
            var products = await _katanaService.GetProductsAsync();

            var recentSyncs = await _context.SyncOperationLogs
                .OrderByDescending(l => l.StartTime)
                .Take(10)
                .ToListAsync();

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

    
    
    
    [HttpGet("dashboard")]
    [HttpGet("~/api/Dashboard")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            _logger.LogInformation("Dashboard istatistikleri getiriliyor");

            // Lokal veritabanından gerçek verileri al
            var dbProducts = await _context.Products.ToListAsync();
            var totalProducts = dbProducts.Count;
            var activeProducts = dbProducts.Count(p => p.IsActive);
            var inStock = dbProducts.Count(p => p.Stock > 0);
            var outOfStock = dbProducts.Count(p => p.Stock == 0);
            var lowStock = dbProducts.Count(p => p.Stock > 0 && p.Stock <= 10);
            var totalValue = dbProducts.Sum(p => p.Stock * p.Price);

            // Son sync bilgisi
            var lastSync = await _context.IntegrationLogs
                .OrderByDescending(l => l.EndTime ?? l.StartTime)
                .FirstOrDefaultAsync();

            var pendingSync = await _context.PendingStockAdjustments
                .CountAsync(p => p.Status == "Pending");

            var stats = new
            {
                totalProducts,
                activeProducts,
                inStock,
                outOfStock,
                lowStock,
                totalValue,
                totalStock = activeProducts,
                pendingSync,
                criticalStock = lowStock,
                lastSyncDate = lastSync?.EndTime ?? lastSync?.StartTime
            };

            _logger.LogInformation("Dashboard istatistikleri başarıyla alındı: Total={Total}, Active={Active}, InStock={InStock}, OutOfStock={OutOfStock}", 
                totalProducts, activeProducts, inStock, outOfStock);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dashboard verileri alınırken hata oluştu");
            return StatusCode(500, new { message = "Dashboard verileri alınamadı" });
        }
    }

    
    
    
    [HttpGet("dashboard/sync-stats")]
    [HttpGet("~/api/Dashboard/sync-stats")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardSyncStats()
    {
        try
        {
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

            var products = await _katanaService.GetProductsAsync();

            var totalProducts = products.Count;
            var totalStock = products.Count(p => p.IsActive);
            var outOfStockItems = products.Count(p => !p.IsActive);
            var lowStockItems = 0;

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
            _logger.LogError(ex, "Senkronizasyon istatistikleri alınırken hata oluştu.");
            return StatusCode(500, new { error = "Senkronizasyon istatistikleri alınamadı", details = ex.Message });
        }
    }

    
    
    
    [HttpGet("dashboard/stats")]
    [HttpGet("~/api/Dashboard/stats")]
    [AllowAnonymous]
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

    
    
    
    [HttpGet("dashboard/activities")]
    [HttpGet("~/api/Dashboard/activities")]
    [AllowAnonymous]
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
            _logger.LogError(ex, "Son aktiviteler alınırken hata oluştu");
            return StatusCode(500, new { error = "Son aktiviteler alınamadı", details = ex.Message });
        }
    }
}
