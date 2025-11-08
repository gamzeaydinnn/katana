using Katana.Business.DTOs;
using Katana.Business.Interfaces;
using Katana.Core.Enums;
using Katana.Data.Context;
using Katana.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Katana.Infrastructure.APIClients;
using Microsoft.Extensions.Logging;

namespace Katana.API.Controllers;

[ApiController]
[Route("api/adminpanel")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IKatanaService _katanaService;
    private readonly IntegrationDbContext _context;
    private readonly ILogger<AdminController> _logger;
    private readonly ILoggingService _loggingService;
    private readonly Katana.Business.Interfaces.IPendingStockAdjustmentService _pendingService;

    public AdminController(
        IKatanaService katanaService,
        IntegrationDbContext context,
        ILogger<AdminController> logger,
        ILoggingService loggingService,
        Katana.Business.Interfaces.IPendingStockAdjustmentService pendingService)
    {
        _katanaService = katanaService;
        _context = context;
        _logger = logger;
        _loggingService = loggingService;
        _pendingService = pendingService;
    }

    // Pending adjustments service will be resolved from DI when needed
    [HttpGet("pending-adjustments")]
    [Authorize(Roles = "Admin,StockManager")]
    public async Task<IActionResult> GetPendingAdjustments()
    {
        try
        {
            var items = await _context.PendingStockAdjustments
                .OrderByDescending(p => p.RequestedAt)
                .Take(200)
                .Select(p => new {
                    p.Id,
                    p.ExternalOrderId,
                    p.ProductId,
                    p.Sku,
                    p.Quantity,
                    p.RequestedBy,
                    p.RequestedAt,
                    p.Status,
                    p.ApprovedBy,
                    p.ApprovedAt,
                    p.RejectionReason,
                    p.Notes
                })
                .ToListAsync();

            return Ok(new { items, total = items.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending adjustments");
            return StatusCode(500, new { error = "Failed to get pending adjustments" });
        }
    }

    [HttpPost("pending-adjustments/{id}/approve")]
    [Authorize(Roles = "Admin,StockManager")]
    public async Task<IActionResult> ApprovePendingAdjustment(long id, [FromQuery] string approvedBy = "admin")
    {
        try
        {
            var ok = await _pendingService.ApproveAsync(id, approvedBy);
            if (!ok)
            {
                var item = await _pendingService.GetByIdAsync(id);
                if (item == null)
                {
                    return NotFound(new { error = "Pending adjustment not found" });
                }

                var reason = string.IsNullOrWhiteSpace(item.RejectionReason)
                    ? $"Adjustment already {item.Status}"
                    : item.RejectionReason;

                return BadRequest(new { error = reason });
            }
            return Ok(new { ok = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving pending adjustment {Id}", id);
            return StatusCode(500, new { error = "Failed to approve pending adjustment" });
        }
    }

    [HttpPost("pending-adjustments/{id}/reject")]
    [Authorize(Roles = "Admin,StockManager")]
    public async Task<IActionResult> RejectPendingAdjustment(long id, [FromBody] RejectDto dto)
    {
        try
        {
            var ok = await _pendingService.RejectAsync(id, dto.RejectedBy ?? "admin", dto.Reason);
            if (!ok)
            {
                var item = await _pendingService.GetByIdAsync(id);
                if (item == null)
                {
                    return NotFound(new { error = "Pending adjustment not found" });
                }

                return BadRequest(new { error = $"Adjustment already {item.Status}" });
            }
            return Ok(new { ok = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting pending adjustment {Id}", id);
            return StatusCode(500, new { error = "Failed to reject pending adjustment" });
        }
    }

    public class RejectDto
    {
        public string? RejectedBy { get; set; }
        public string? Reason { get; set; }
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        try
        {
            _loggingService.LogInfo("Admin statistics requested", User?.Identity?.Name, "GetStatistics", LogCategory.UserAction);
            var products = await _katanaService.GetProductsAsync();
            var totalProducts = products.Count;
            var activeProducts = products.Count(p => p.IsActive);

            return Ok(new
            {
                totalProducts,
                totalStock = activeProducts,
                successfulSyncs = 0,
                failedSyncs = 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin statistics");
            _loggingService.LogError("Failed to get admin statistics", ex, User?.Identity?.Name, null, LogCategory.System);
            return StatusCode(500, new { error = "Failed to get statistics" });
        }
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            _loggingService.LogInfo($"Products requested (Page: {page}, Size: {pageSize})", User?.Identity?.Name, "GetProducts", LogCategory.UserAction);
            var allProducts = await _katanaService.GetProductsAsync();

            var startIndex = (page - 1) * pageSize;
            var products = allProducts
                .Skip(startIndex)
                .Take(pageSize)
                .Select(p => new
                {
                    id = p.SKU,
                    sku = p.SKU,
                    name = p.Name,
                    stock = 0,
                    isActive = p.IsActive
                }).ToList();

            return Ok(new { products, total = allProducts.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products");
            _loggingService.LogError("Failed to get products from Katana API", ex, User?.Identity?.Name, $"Page: {page}, Size: {pageSize}", LogCategory.ExternalAPI);
            return StatusCode(500, new { error = "Failed to get products" });
        }
    }

    [HttpGet("products/{id}")]
    public async Task<IActionResult> GetProductById(string id)
    {
        try
        {
            var product = await _katanaService.GetProductBySkuAsync(id);
            
            if (product == null)
                return NotFound();

            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting product {id}");
            return StatusCode(500, new { error = "Failed to get product" });
        }
    }

    [HttpGet("sync-logs")]
    public IActionResult GetSyncLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var logsQuery = _context.SyncOperationLogs
                .OrderByDescending(l => l.StartTime);

            var total = logsQuery.Count();

            var logs = logsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new {
                    l.Id,
                    l.SyncType,
                    l.Status,
                    l.StartTime,
                    l.EndTime,
                    l.Details // varsa
                })
                .ToList();

            return Ok(new { logs, total });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sync logs");
            return StatusCode(500, new { error = "Failed to get sync logs" });
        }
    }

    [HttpGet("db-check")]
    public async Task<IActionResult> GetDatabaseCheck()
    {
        try
        {
            var provider = _context.Database.ProviderName ?? "unknown";
            var conn = _context.Database.GetDbConnection();
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT OBJECT_ID('dbo.PendingStockAdjustments')";
            var val = await cmd.ExecuteScalarAsync();
            await conn.CloseAsync();
            var exists = val != null && val != DBNull.Value;
            return Ok(new { provider, pendingStockAdjustmentsTableExists = exists });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DB check failed");
            return StatusCode(500, new { error = "DB check failed", detail = ex.Message });
        }
    }

    [HttpGet("migrations")]
    public async Task<IActionResult> GetAppliedMigrations()
    {
        try
        {
            var conn = _context.Database.GetDbConnection();
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT [MigrationId], [ProductVersion] FROM [__EFMigrationsHistory] ORDER BY [MigrationId]";
            var list = new List<object>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new { migrationId = reader.GetString(0), productVersion = reader.GetString(1) });
            }
            await conn.CloseAsync();
            return Ok(new { applied = list });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read __EFMigrationsHistory");
            return StatusCode(500, new { error = "Failed to read migrations history", detail = ex.Message });
        }
    }

    [HttpGet("db-table-info")]
    public async Task<IActionResult> GetPendingTableInfo()
    {
        try
        {
            var conn = _context.Database.GetDbConnection();
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PendingStockAdjustments'";
            var list = new List<object>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new { schema = reader.GetString(0), name = reader.GetString(1) });
            }
            await conn.CloseAsync();
            return Ok(new { tables = list });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query INFORMATION_SCHEMA for PendingStockAdjustments");
            return StatusCode(500, new { error = "Failed to query table info", detail = ex.Message });
        }
    }

    [HttpPost("pending-adjustments/test-create")]
    [Authorize(Roles = "Admin,StockManager")]
    public async Task<IActionResult> CreateTestPendingAdjustment()
    {
        try
        {
            // Create or find a test product
            var sku = "TEST-SKU-ADMIN-001";
            var product = await _context.Products.FirstOrDefaultAsync(p => p.SKU == sku);
            if (product == null)
            {
                // Ensure there's at least one category to satisfy FK
                var category = await _context.Categories.FirstOrDefaultAsync();
                if (category == null)
                {
                    category = new Katana.Core.Entities.Category {
                        Name = "Uncategorized",
                        Description = "Auto-created test category",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Categories.Add(category);
                    await _context.SaveChangesAsync();
                }

                product = new Katana.Core.Entities.Product
                {
                    Name = "Test Product (Admin)",
                    SKU = sku,
                    Price = 0m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Stock = 100,
                    CategoryId = category.Id
                };
                _context.Products.Add(product);
                await _context.SaveChangesAsync();
            }

            var pending = new Katana.Data.Models.PendingStockAdjustment
            {
                ExternalOrderId = $"TEST-{Guid.NewGuid():N}",
                ProductId = product.Id,
                Sku = product.SKU,
                Quantity = 5,
                RequestedBy = "test",
                RequestedAt = DateTimeOffset.UtcNow,
                Status = "Pending"
            };

            // Use the centralized pending service so notifications (if configured) are published
            var created = await _pendingService.CreateAsync(pending);

            return Ok(new { ok = true, pendingId = created.Id, productId = product.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create test pending adjustment");
            return StatusCode(500, new { error = "Failed to create test pending adjustment", detail = ex.Message });
        }
    }

    [HttpGet("product-stock/{id:int}")]
    public async Task<IActionResult> GetProductStock(int id)
    {
        try
        {
            // Product.Id is an int in the EF model - ensure we use the correct type here
            var p = await _context.Products.FindAsync(id);
            if (p == null) return NotFound();
            return Ok(new { id = p.Id, sku = p.SKU, stock = p.Stock });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get product stock");
            return StatusCode(500, new { error = "Failed to get product stock", detail = ex.Message });
        }
    }

    [HttpGet("katana-health")]
    public async Task<IActionResult> CheckKatanaHealth()
    {
        try
        {
            var isHealthy = await _katanaService.TestConnectionAsync();
            return Ok(new { isHealthy });
        }
        catch (Exception)
        {
            return Ok(new { isHealthy = false });
        }
    }
}
