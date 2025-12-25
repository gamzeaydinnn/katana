using Katana.Core.DTOs;
using Katana.Core.DTOs.Koza;
using Katana.Business.Interfaces;
using Katana.Core.Enums;
using Katana.Data.Context;
using Katana.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Katana.Infrastructure.APIClients;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Katana.API.Controllers;

[ApiController]
[Route("api/adminpanel")]
[Authorize(Roles = "Admin,Manager,StokYonetici")]
public class AdminController : ControllerBase
{
    private readonly IKatanaService _katanaService;
    private readonly IntegrationDbContext _context;
    private readonly Katana.Business.Interfaces.IAdminService _adminService;
    private readonly ILogger<AdminController> _logger;
    private readonly ILoggingService _loggingService;
    private readonly Katana.Business.Interfaces.IPendingStockAdjustmentService _pendingService;

    public AdminController(
        IKatanaService katanaService,
        IntegrationDbContext context,
        Katana.Business.Services.AdminService adminService,
        ILogger<AdminController> logger,
        ILoggingService loggingService,
        Katana.Business.Interfaces.IPendingStockAdjustmentService pendingService)
    {
        _katanaService = katanaService;
        _context = context;
        _adminService = adminService;
        _logger = logger;
        _loggingService = loggingService;
        _pendingService = pendingService;
    }

    
    [HttpGet("debug/roles")]
    public IActionResult GetCurrentUserRoles()
    {
        var username = User?.Identity?.Name;
        var roles = User?.Claims
            .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();
        return Ok(new { username, roles, isAuthenticated = User?.Identity?.IsAuthenticated });
    }

    
    [HttpGet("pending-adjustments")]
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
    [Authorize(Roles = "Admin,StokYonetici")] 
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
    [Authorize(Roles = "Admin,StokYonetici")] 
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
            
            // Local veritabanından ürün sayısını al (Dashboard ile tutarlı olması için)
            var totalProducts = await _context.Products.CountAsync();
            var activeProducts = await _context.Products.CountAsync(p => p.IsActive);

            // Stok hareketlerinden toplam stok hesapla
            var activeProductIds = await _context.Products.Where(p => p.IsActive).Select(p => p.Id).ToListAsync();
            var totalStock = 0;
            if (activeProductIds.Any())
            {
                totalStock = await _context.StockMovements
                    .Where(sm => activeProductIds.Contains(sm.ProductId))
                    .SumAsync(sm => (int?)sm.ChangeQuantity) ?? 0;
            }

            // Kritik ürünler (stok <= 5)
            var productBalances = await _context.StockMovements
                .GroupBy(sm => sm.ProductId)
                .Select(g => new { ProductId = g.Key, Balance = g.Sum(x => x.ChangeQuantity) })
                .ToListAsync();
            var balancesById = productBalances.ToDictionary(x => x.ProductId, x => x.Balance);
            var activeProductsList = await _context.Products.Where(p => p.IsActive).ToListAsync();
            var criticalProducts = activeProductsList.Count(p => (balancesById.TryGetValue(p.Id, out var b) ? b : p.StockSnapshot) <= 5);

            // Toplam değer hesaplama
            var totalValue = activeProductsList.Sum(p => (balancesById.TryGetValue(p.Id, out var b) ? b : p.StockSnapshot) * p.Price);

            // Son 24 saatteki sync loglarını al
            var last24Hours = DateTime.UtcNow.AddHours(-24);
            var recentSyncs = await _context.SyncOperationLogs
                .Where(l => l.StartTime >= last24Hours)
                .ToListAsync();

            var successfulSyncs = recentSyncs.Count(s => s.Status == "SUCCESS" || s.Status == "COMPLETED");
            var failedSyncs = recentSyncs.Count(s => s.Status != "SUCCESS" && s.Status != "COMPLETED" && !string.IsNullOrEmpty(s.Status));

            return Ok(new
            {
                totalProducts,
                totalStock,
                criticalProducts,
                totalValue,
                successfulSyncs,
                failedSyncs
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
    [Authorize(Roles = "Admin,Manager,StokYonetici")] 
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
                    l.Details 
                })
                .ToList();

            return Ok(new { logs, total });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Senkronizasyon logları alınırken hata oluştu");
            return StatusCode(500, new { error = "Senkronizasyon logları alınamadı" });
        }
    }

    /// <summary>
    /// Son eklenen stok hareketlerini döner (Admin Dashboard için)
    /// </summary>
    [HttpGet("recent-stock-movements")]
    [Authorize(Roles = "Admin,Manager,StokYonetici")]
    public async Task<IActionResult> GetRecentStockMovements([FromQuery] int take = 10)
    {
        try
        {
            var movements = await _context.StockMovements
                .Include(sm => sm.Product)
                .OrderByDescending(sm => sm.MovementDate)
                .Take(take)
                .Select(sm => new {
                    id = sm.Id,
                    productName = sm.Product != null ? sm.Product.Name : "Bilinmeyen Ürün",
                    sku = sm.Product != null ? sm.Product.SKU : sm.SKU,
                    quantity = sm.ChangeQuantity,
                    movementType = sm.MovementType,
                    movementDate = sm.MovementDate,
                    reason = sm.Reason,
                    createdAt = sm.CreatedAt
                })
                .ToListAsync();

            return Ok(new { movements, total = movements.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Son stok hareketleri alınırken hata oluştu");
            return StatusCode(500, new { error = "Stok hareketleri alınamadı" });
        }
    }

    
    [HttpGet("sync-logs-anon")]
    [AllowAnonymous]
    public IActionResult GetSyncLogsAnonymous([FromQuery] int take = 20)
    {
        try
        {
            var logs = _context.SyncOperationLogs
                .OrderByDescending(l => l.StartTime)
                .Take(take)
                .Select(l => new { l.Id, l.SyncType, l.Status, l.StartTime, l.EndTime, l.Details })
                .ToList();

            return Ok(new { total = logs.Count, logs });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get anonymous sync logs");
            return StatusCode(500, new { error = "Failed to get sync logs", detail = ex.Message });
        }
    }

    
    [HttpGet("mapping-table-anon")]
    [AllowAnonymous]
    public IActionResult GetMappingTableAnonymous([FromQuery] string mappingType = "LOCATION_WAREHOUSE")
    {
        try
        {
            var entries = _context.MappingTables
                .Where(m => m.MappingType == mappingType)
                .OrderBy(m => m.SourceValue)
                .Select(m => new { m.Id, m.SourceValue, m.TargetValue, m.Description, m.IsActive })
                .ToList();

            return Ok(new { total = entries.Count, entries });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get anonymous mapping table");
            return StatusCode(500, new { error = "Failed to get mapping table", detail = ex.Message });
        }
    }

    
    [HttpPost("mapping-table-anon/set-default")]
    [AllowAnonymous]
    public async Task<IActionResult> SetMappingDefaultTo001()
    {
        try
        {
            var mapping = await _context.MappingTables
                .FirstOrDefaultAsync(m => m.MappingType == "LOCATION_WAREHOUSE" && m.SourceValue == "DEFAULT");

            if (mapping == null)
            {
                mapping = new Katana.Data.Models.MappingTable
                {
                    MappingType = "LOCATION_WAREHOUSE",
                    SourceValue = "DEFAULT",
                    TargetValue = "001",
                    Description = "Default warehouse code for unmapped locations (test)",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.MappingTables.Add(mapping);
            }
            else
            {
                mapping.TargetValue = "001";
                mapping.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new { ok = true, mappingId = mapping.Id, targetValue = mapping.TargetValue });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set mapping default");
            return StatusCode(500, new { error = "Failed to set mapping default", detail = ex.Message });
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
    [Authorize(Roles = "Admin")] 
    public async Task<IActionResult> CreateTestPendingAdjustment()
    {
        try
        {
            
            var sku = "TEST-SKU-ADMIN-001";
            var product = await _context.Products.FirstOrDefaultAsync(p => p.SKU == sku);
            if (product == null)
            {
                
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

            
            var created = await _pendingService.CreateAsync(pending);

            return Ok(new { ok = true, pendingId = created.Id, productId = product.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create test pending adjustment");
            return StatusCode(500, new { error = "Failed to create test pending adjustment", detail = ex.Message });
        }
    }

    
    [HttpPost("pending-adjustments/test-create-anon")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateTestPendingAdjustmentAnonymous()
    {
        try
        {
            
            var sku = "TEST-SKU-ANON-001";
            var product = await _context.Products.FirstOrDefaultAsync(p => p.SKU == sku);
            if (product == null)
            {
                
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
                    Name = "Test Product (Anon)",
                    SKU = sku,
                    Price = 1.0m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Stock = 10,
                    CategoryId = category.Id
                };
                _context.Products.Add(product);
                await _context.SaveChangesAsync();
            }

            var pending = new Katana.Data.Models.PendingStockAdjustment
            {
                ExternalOrderId = $"TEST-ANON-{Guid.NewGuid():N}",
                ProductId = product.Id,
                Sku = product.SKU,
                Quantity = 2,
                RequestedBy = "anon-test",
                RequestedAt = DateTimeOffset.UtcNow,
                Status = "Pending"
            };

            var created = await _pendingService.CreateAsync(pending);

            return Ok(new { ok = true, pendingId = created.Id, productId = product.Id, sku = product.SKU });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create anonymous test pending adjustment");
            return StatusCode(500, new { error = "Failed to create test pending adjustment", detail = ex.Message });
        }
    }

    [HttpGet("product-stock/{id:int}")]
    public async Task<IActionResult> GetProductStock(int id)
    {
        try
        {
            
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

    [HttpPost("manual-sync")]
    [AllowAnonymous]
    public async Task<IActionResult> ManualSync([FromBody] ManualSyncRequest request)
    {
        try
        {
            var ok = await _adminService.RunManualSyncAsync(request);
            return Ok(new { ok });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual sync failed for {Integration}", request?.IntegrationName);
            return StatusCode(500, new { error = "Manual sync failed", detail = ex.Message });
        }
    }

    

    [HttpGet("failed-records")]
    public async Task<IActionResult> GetFailedSyncRecords(
        [FromQuery] string? status = null,
        [FromQuery] string? recordType = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var query = _context.FailedSyncRecords
                .Include(f => f.IntegrationLog)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(f => f.Status == status);

            if (!string.IsNullOrEmpty(recordType))
                query = query.Where(f => f.RecordType == recordType);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(f => f.FailedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new
                {
                    f.Id,
                    f.RecordType,
                    f.RecordId,
                    f.ErrorMessage,
                    f.ErrorCode,
                    f.FailedAt,
                    f.RetryCount,
                    f.LastRetryAt,
                    f.Status,
                    f.ResolvedAt,
                    f.ResolvedBy,
                    IntegrationLogId = f.IntegrationLog != null ? f.IntegrationLog.Id : 0,
                    SourceSystem = f.IntegrationLog != null ? f.IntegrationLog.SyncType : "Unknown"
                })
                .ToListAsync();

            return Ok(new { total, page, pageSize, items });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get failed sync records");
            return StatusCode(500, new { error = "Failed to get failed sync records" });
        }
    }

    
    [HttpGet("failed-records-anon")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFailedSyncRecordsAnonymous([FromQuery] int take = 50)
    {
        try
        {
            var items = await _context.FailedSyncRecords
                .OrderByDescending(f => f.FailedAt)
                .Take(take)
                .Select(f => new { f.Id, f.RecordType, f.RecordId, f.ErrorMessage, f.FailedAt, f.Status })
                .ToListAsync();

            return Ok(new { total = items.Count, items });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get anonymous failed sync records");
            return StatusCode(500, new { error = "Failed to get failed records", detail = ex.Message });
        }
    }

    
    [HttpPost("test-push-products-anon")]
    [AllowAnonymous]
    public async Task<IActionResult> TestPushProductsAnonymous([FromQuery] int take = 10)
    {
        try
        {
            var products = await _context.Products
                .OrderByDescending(p => p.UpdatedAt)
                .Take(take)
                .ToListAsync();

            var lucaService = HttpContext.RequestServices.GetRequiredService<Katana.Business.Interfaces.ILucaService>();

            
            var lucaStockCards = products
                .Select(p => Katana.Core.Helpers.MappingHelper.MapToLucaStockCard(p))
                .ToList();

            var result = await lucaService.SendStockCardsAsync(lucaStockCards);

            return Ok(new { ok = true, processed = result.ProcessedRecords, success = result.SuccessfulRecords, failed = result.FailedRecords, message = result.Message, errors = result.Errors });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to push products to Luca in test endpoint");
            return StatusCode(500, new { error = "Failed to push products", detail = ex.Message });
        }
    }

    [HttpGet("failed-records/{id:int}")]
    public async Task<IActionResult> GetFailedSyncRecord(int id)
    {
        try
        {
            var record = await _context.FailedSyncRecords
                .Include(f => f.IntegrationLog)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (record == null)
                return NotFound(new { error = "Record not found" });

            return Ok(new
            {
                record.Id,
                record.RecordType,
                record.RecordId,
                record.OriginalData,
                record.ErrorMessage,
                record.ErrorCode,
                record.FailedAt,
                record.RetryCount,
                record.LastRetryAt,
                record.NextRetryAt,
                record.Status,
                record.Resolution,
                record.ResolvedAt,
                record.ResolvedBy,
                IntegrationLog = record.IntegrationLog != null ? new
                {
                    record.IntegrationLog.Id,
                    SyncType = record.IntegrationLog.SyncType,
                    record.IntegrationLog.Status,
                    StartTime = record.IntegrationLog.StartTime
                } : null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get failed sync record {Id}", id);
            return StatusCode(500, new { error = "Failed to get record" });
        }
    }

    [HttpPut("failed-records/{id:int}/resolve")]
    public async Task<IActionResult> ResolveFailedRecord(
        int id,
        [FromBody] ResolveFailedRecordDto dto)
    {
        try
        {
            var record = await _context.FailedSyncRecords.FindAsync(id);
            if (record == null)
                return NotFound(new { error = "Record not found" });

            record.Status = "RESOLVED";
            record.Resolution = dto.Resolution;
            record.ResolvedAt = DateTime.UtcNow;
            record.ResolvedBy = User.Identity?.Name ?? "admin";

            
            if (!string.IsNullOrEmpty(dto.CorrectedData))
                record.OriginalData = dto.CorrectedData;

            await _context.SaveChangesAsync();

            await _loggingService.LogAuditAsync(
                User.Identity?.Name ?? "admin",
                $"Resolved failed sync record {id}",
                "FailedSyncRecord",
                id.ToString(),
                record.OriginalData,
                dto.CorrectedData ?? record.OriginalData
            );

            
            if (dto.Resend && !string.IsNullOrEmpty(dto.CorrectedData))
            {
                try
                {
                    _logger.LogInformation("Attempting to resend corrected data for record {Id}, type {Type}", id, record.RecordType);
                    
                    switch (record.RecordType?.ToUpper())
                    {
                        case "STOCK":
                            
                            var stockData = JsonSerializer.Deserialize<Dictionary<string, object>>(dto.CorrectedData);
                            if (stockData != null)
                            {
                                
                                var targetSystem = record.IntegrationLog?.SyncType ?? "UNKNOWN";
                                if (targetSystem.Contains("KATANA", StringComparison.OrdinalIgnoreCase))
                                {
                                    
                                    _logger.LogInformation("Would send stock update to Katana: {Data}", dto.CorrectedData);
                                }
                                else if (targetSystem.Contains("LUCA", StringComparison.OrdinalIgnoreCase))
                                {
                                    
                                    _logger.LogInformation("Would send stock update to Luca: {Data}", dto.CorrectedData);
                                }
                            }
                            break;

                        case "ORDER":
                            var orderData = JsonSerializer.Deserialize<Dictionary<string, object>>(dto.CorrectedData);
                            if (orderData != null)
                            {
                                _logger.LogInformation("Would resend order data: {Data}", dto.CorrectedData);
                            }
                            break;

                        case "INVOICE":
                            var invoiceData = JsonSerializer.Deserialize<Dictionary<string, object>>(dto.CorrectedData);
                            if (invoiceData != null)
                            {
                                _logger.LogInformation("Would resend invoice data: {Data}", dto.CorrectedData);
                            }
                            break;

                        case "CUSTOMER":
                            var customerData = JsonSerializer.Deserialize<Dictionary<string, object>>(dto.CorrectedData);
                            if (customerData != null)
                            {
                                _logger.LogInformation("Would resend customer data: {Data}", dto.CorrectedData);
                            }
                            break;

                        default:
                            _logger.LogWarning("Unknown record type {Type} for resend", record.RecordType);
                            break;
                    }
                    
                    
                    await _loggingService.LogAuditAsync(
                        User.Identity?.Name ?? "admin",
                        "RESEND_ATTEMPT",
                        "FailedSyncRecord",
                        id.ToString(),
                        null,
                        $"Resend attempted for {record.RecordType}"
                    );
                }
                catch (Exception resendEx)
                {
                    _logger.LogError(resendEx, "Failed to resend corrected data for record {Id}", id);
                    
                }
            }

            return Ok(new { success = true, message = "Record resolved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve record {Id}", id);
            return StatusCode(500, new { error = "Failed to resolve record" });
        }
    }

    [HttpPut("failed-records/{id:int}/ignore")]
    public async Task<IActionResult> IgnoreFailedRecord(int id, [FromBody] IgnoreFailedRecordDto dto)
    {
        try
        {
            var record = await _context.FailedSyncRecords.FindAsync(id);
            if (record == null)
                return NotFound(new { error = "Record not found" });

            record.Status = "IGNORED";
            record.Resolution = dto.Reason ?? "Ignored by admin";
            record.ResolvedAt = DateTime.UtcNow;
            record.ResolvedBy = User.Identity?.Name ?? "admin";

            await _context.SaveChangesAsync();

            await _loggingService.LogAuditAsync(
                User.Identity?.Name ?? "admin",
                $"Ignored failed sync record {id}",
                "FailedSyncRecord",
                id.ToString(),
                null,
                dto.Reason
            );

            return Ok(new { success = true, message = "Record ignored successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ignore record {Id}", id);
            return StatusCode(500, new { error = "Failed to ignore record" });
        }
    }

    [HttpPost("failed-records/{id:int}/retry")]
    public async Task<IActionResult> RetryFailedRecord(int id)
    {
        try
        {
            var record = await _context.FailedSyncRecords
                .Include(f => f.IntegrationLog)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (record == null)
                return NotFound(new { error = "Record not found" });

            record.RetryCount++;
            record.LastRetryAt = DateTime.UtcNow;
            record.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, record.RetryCount)); 
            record.Status = "RETRYING";

            await _context.SaveChangesAsync();

            
            
            _logger.LogInformation("Retry initiated for record {Id}, attempt {Count}", id, record.RetryCount);

            return Ok(new { success = true, message = "Retry initiated", retryCount = record.RetryCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retry record {Id}", id);
            return StatusCode(500, new { error = "Failed to retry record" });
        }
    }

    // ====================================================================
    // STOK KARTI TEST ENDPOİNTLERİ (Update & Delete)
    // ====================================================================

    /// <summary>
    /// Stok kartı güncelleme - SKU ile Luca'dan gerçek ID bulunup güncellenir
    /// </summary>
    [HttpPost("test-update-product")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> TestUpdateProduct([FromBody] LucaUpdateStokKartiRequest request)
    {
        try
        {
            var lucaService = HttpContext.RequestServices.GetRequiredService<Katana.Business.Interfaces.ILucaService>();
            
            // 1. SKU ile Luca'dan gerçek ID'yi bul
            var lucaCards = await lucaService.ListStockCardsSimpleAsync();
            var targetSku = (request.KartKodu ?? "").Trim().ToUpperInvariant().Replace(" ", "");
            
            var existingCard = lucaCards.FirstOrDefault(x => 
                (x.KartKodu ?? "").Trim().ToUpperInvariant().Replace(" ", "") == targetSku);

            if (existingCard?.StokKartId == null)
            {
                return BadRequest(new { 
                    success = false, 
                    message = $"SKU '{request.KartKodu}' Luca'da bulunamadı. Önce ürünü oluşturun." 
                });
            }

            // 2. Gerçek Luca ID'yi set et
            var realLucaId = existingCard.StokKartId.Value;
            request.SkartId = realLucaId;
            
            _logger.LogInformation("✅ SKU eşleşti: {Sku} -> Luca ID: {LucaId}", request.KartKodu, realLucaId);

            // 3. Güncelle
            var result = await lucaService.UpdateStockCardAsync(request);
            
            return result 
                ? Ok(new { success = true, message = "Güncelleme başarılı", lucaId = realLucaId })
                : BadRequest(new { success = false, message = "Luca güncelleme reddetti" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update hatası");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Stok kartı silme - SKU ile Luca'dan gerçek ID bulunup silinir
    /// Önce local DB'den LucaId kontrol edilir (hızlı), yoksa Luca'dan çekilir (yavaş)
    /// </summary>
    [HttpPost("test-delete-product")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> TestDeleteProduct([FromQuery] string sku)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            if (string.IsNullOrWhiteSpace(sku))
                return BadRequest(new { success = false, message = "SKU gerekli" });

            var lucaService = HttpContext.RequestServices.GetRequiredService<Katana.Business.Interfaces.ILucaService>();
            var targetSku = sku.Trim().ToUpperInvariant().Replace(" ", "");
            long? realLucaId = null;
            string lookupMethod = "";

            // 1. ÖNCE LOCAL DB'DEN BAK (HIZLI - 0.1 saniye)
            var localProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.SKU.ToUpper().Replace(" ", "") == targetSku);

            if (localProduct?.LucaId != null && localProduct.LucaId > 0)
            {
                realLucaId = localProduct.LucaId;
                lookupMethod = "LocalDB (Hızlı)";
                _logger.LogInformation("⚡ Hızlı Lookup: {Sku} -> LucaId: {LucaId} (Local DB)", sku, realLucaId);
            }
            else
            {
                // 2. LOCAL'DA YOKSA LUCA'DAN ÇEK (YAVAŞ - 90 saniye)
                _logger.LogWarning("⏳ Yavaş Lookup başlatılıyor: {Sku} - Local DB'de LucaId yok", sku);
                var lucaCards = await lucaService.ListStockCardsSimpleAsync();
                
                var existingCard = lucaCards.FirstOrDefault(x => 
                    (x.KartKodu ?? "").Trim().ToUpperInvariant().Replace(" ", "") == targetSku);

                if (existingCard?.StokKartId != null)
                {
                    realLucaId = existingCard.StokKartId.Value;
                    lookupMethod = "Luca API (Yavaş)";
                    
                    // Gelecek seferler için local DB'ye kaydet
                    if (localProduct != null)
                    {
                        localProduct.LucaId = realLucaId;
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("💾 LucaId local DB'ye kaydedildi: {Sku} -> {LucaId}", sku, realLucaId);
                    }
                }
            }

            if (realLucaId == null)
            {
                return BadRequest(new { 
                    success = false, 
                    message = $"SKU '{sku}' Luca'da bulunamadı.",
                    elapsed = sw.ElapsedMilliseconds + "ms"
                });
            }

            _logger.LogInformation("🗑️ Silme: {Sku} -> Luca ID: {LucaId} ({Method})", sku, realLucaId, lookupMethod);

            // 3. SİL
            var result = await lucaService.DeleteStockCardAsync(realLucaId.Value);
            sw.Stop();
            
            if (result)
            {
                // Local DB'den de sil (opsiyonel)
                if (localProduct != null)
                {
                    _context.Products.Remove(localProduct);
                    await _context.SaveChangesAsync();
                }
                
                return Ok(new { 
                    success = true, 
                    message = "Kart silindi (Hard Delete)", 
                    sku = sku, 
                    lucaId = realLucaId,
                    lookupMethod = lookupMethod,
                    elapsed = sw.ElapsedMilliseconds + "ms"
                });
            }
            else
            {
                // 400 dönerken detay verelim
                return BadRequest(new { 
                    error = true,
                    success = false, 
                    message = "Silme/Pasife çekme başarısız oldu. Logları kontrol edin.",
                    sku = sku,
                    lucaId = realLucaId,
                    lookupMethod = lookupMethod,
                    elapsed = sw.ElapsedMilliseconds + "ms"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete hatası");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    #region Katana Webhook Yönetimi

    /// <summary>
    /// Mevcut Katana webhook'larını listele
    /// </summary>
    [HttpGet("webhooks")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetWebhooks([FromServices] IKatanaService katanaService)
    {
        try
        {
            var webhooks = await katanaService.GetWebhooksAsync();
            return Ok(new { 
                success = true, 
                count = webhooks.Count, 
                webhooks = webhooks.Select(w => new {
                    id = w.Id,
                    url = w.Url,
                    subscribedEvents = w.SubscribedEvents,
                    description = w.Description,
                    isActive = w.IsActive,
                    createdAt = w.CreatedAt
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook listesi alınırken hata");
            return StatusCode(500, new { error = "Webhook listesi alınamadı", details = ex.Message });
        }
    }

    /// <summary>
    /// Yeni Katana webhook oluştur
    /// </summary>
    [HttpPost("webhooks")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateWebhook(
        [FromServices] IKatanaService katanaService,
        [FromBody] CreateWebhookRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Url))
                return BadRequest(new { error = "URL gerekli" });
            
            if (request.Events == null || request.Events.Count == 0)
                return BadRequest(new { error = "En az bir event gerekli" });

            var webhook = await katanaService.CreateWebhookAsync(request.Url, request.Events, request.Description);
            
            if (webhook == null)
                return BadRequest(new { error = "Webhook oluşturulamadı" });

            return Ok(new { 
                success = true, 
                message = "Webhook başarıyla oluşturuldu",
                webhook = new {
                    id = webhook.Id,
                    url = webhook.Url,
                    subscribedEvents = webhook.SubscribedEvents,
                    token = webhook.Token, // İmza doğrulaması için gerekli - sakla!
                    description = webhook.Description
                },
                important = "⚠️ Token'ı kaydedin! Webhook imza doğrulaması için gerekli."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook oluşturulurken hata");
            return StatusCode(500, new { error = "Webhook oluşturulamadı", details = ex.Message });
        }
    }

    /// <summary>
    /// Katana webhook sil
    /// </summary>
    [HttpDelete("webhooks/{webhookId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteWebhook(
        [FromServices] IKatanaService katanaService,
        int webhookId)
    {
        try
        {
            var result = await katanaService.DeleteWebhookAsync(webhookId);
            
            if (!result)
                return BadRequest(new { error = "Webhook silinemedi" });

            return Ok(new { success = true, message = $"Webhook {webhookId} silindi" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook silinirken hata. ID: {WebhookId}", webhookId);
            return StatusCode(500, new { error = "Webhook silinemedi", details = ex.Message });
        }
    }

    /// <summary>
    /// Hızlı webhook kurulumu - Product event'leri için webhook oluştur
    /// </summary>
    [HttpPost("webhooks/setup-product-events")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetupProductWebhook(
        [FromServices] IKatanaService katanaService,
        [FromServices] IConfiguration configuration)
    {
        try
        {
            // API base URL'i al
            var baseUrl = configuration["AppSettings:PublicApiUrl"] 
                ?? configuration["Kestrel:Endpoints:Http:Url"]
                ?? "http://localhost:5055";
            
            var webhookUrl = $"{baseUrl.TrimEnd('/')}/api/webhook/katana/product";
            
            var events = new List<string>
            {
                "product.created",
                "product.updated",
                "product.deleted",
                "variant.created",
                "variant.updated",
                "variant.deleted",
                "material.created",
                "material.updated",
                "material.deleted"
            };

            _logger.LogInformation("Setting up product webhook. URL: {Url}, Events: {Events}", 
                webhookUrl, string.Join(", ", events));

            var webhook = await katanaService.CreateWebhookAsync(
                webhookUrl, 
                events, 
                "Product/Variant/Material Events - Auto Setup");
            
            if (webhook == null)
                return BadRequest(new { error = "Webhook oluşturulamadı" });

            // Token'ı appsettings'e kaydetmek için bilgi ver
            return Ok(new { 
                success = true, 
                message = "Product webhook'u başarıyla oluşturuldu!",
                webhook = new {
                    id = webhook.Id,
                    url = webhookUrl,
                    subscribedEvents = events,
                    token = webhook.Token
                },
                nextSteps = new[] {
                    "1. Token'ı appsettings.json'a kaydedin:",
                    $"   \"KatanaApi\": {{ \"WebhookSecret\": \"{webhook.Token}\" }}",
                    "2. API'yi yeniden başlatın",
                    "3. Katana'da ürün ekleyin ve webhook'un çalıştığını doğrulayın"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Product webhook kurulumu hatası");
            return StatusCode(500, new { error = "Webhook kurulumu başarısız", details = ex.Message });
        }
    }

    /// <summary>
    /// Hızlı webhook kurulumu - Sales Order event'leri için webhook oluştur
    /// </summary>
    [HttpPost("webhooks/setup-sales-order-events")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetupSalesOrderWebhook(
        [FromServices] IKatanaService katanaService,
        [FromServices] IConfiguration configuration)
    {
        try
        {
            var baseUrl = configuration["AppSettings:PublicApiUrl"] 
                ?? configuration["Kestrel:Endpoints:Http:Url"]
                ?? "http://localhost:5055";
            
            var webhookUrl = $"{baseUrl.TrimEnd('/')}/api/webhook/katana/sales-order";
            
            var events = new List<string>
            {
                "sales_order.created",
                "sales_order.updated",
                "sales_order.deleted",
                "sales_order.packed",
                "sales_order.delivered"
            };

            var webhook = await katanaService.CreateWebhookAsync(
                webhookUrl, 
                events, 
                "Sales Order Events - Auto Setup");
            
            if (webhook == null)
                return BadRequest(new { error = "Webhook oluşturulamadı" });

            return Ok(new { 
                success = true, 
                message = "Sales Order webhook'u başarıyla oluşturuldu!",
                webhook = new {
                    id = webhook.Id,
                    url = webhookUrl,
                    subscribedEvents = events,
                    token = webhook.Token
                },
                nextSteps = new[] {
                    "1. Token'ı appsettings.json'a kaydedin:",
                    $"   \"KatanaApi\": {{ \"WebhookSecret\": \"{webhook.Token}\" }}",
                    "2. API'yi yeniden başlatın"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sales Order webhook kurulumu hatası");
            return StatusCode(500, new { error = "Webhook kurulumu başarısız", details = ex.Message });
        }
    }

    #endregion
}

public class CreateWebhookRequest
{
    public string Url { get; set; } = string.Empty;
    public List<string> Events { get; set; } = new();
    public string? Description { get; set; }
}
