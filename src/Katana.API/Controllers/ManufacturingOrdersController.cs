using Katana.Business.Interfaces;
using Katana.Core.Entities;
using Katana.Core.Enums;
using Katana.Core.Interfaces;
using Katana.Data.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Katana.API.Controllers;

[Authorize]
[ApiController]
[Route("api/manufacturing-orders")]
public class ManufacturingOrdersController : ControllerBase
{
    private readonly IntegrationDbContext _context;
    private readonly ILoggingService _loggingService;
    private readonly IAuditService _auditService;

    public ManufacturingOrdersController(
        IntegrationDbContext context,
        ILoggingService loggingService,
        IAuditService auditService)
    {
        _context = context;
        _loggingService = loggingService;
        _auditService = auditService;
    }

    /// <summary>
    /// Tüm üretim emirlerini listele
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ManufacturingOrderDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? status = null)
    {
        try
        {
            var query = _context.ManufacturingOrders
                .Include(m => m.Product)
                .AsQueryable();

            // Filter by status
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(m => m.Status == status);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var orders = await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new ManufacturingOrderDto
                {
                    Id = m.Id,
                    OrderNo = m.OrderNo,
                    ProductId = m.ProductId,
                    ProductName = m.Product != null ? m.Product.Name : null,
                    ProductSku = m.Product != null ? m.Product.SKU : null,
                    Quantity = m.Quantity,
                    Status = m.Status,
                    DueDate = m.DueDate,
                    IsSynced = m.IsSynced,
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                items = orders,
                pagination = new
                {
                    currentPage = page,
                    pageSize,
                    totalCount,
                    totalPages
                }
            });
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"ManufacturingOrders GetAll failed: {ex.Message}", ex);
            return StatusCode(500, new { message = "Üretim emirleri yüklenirken hata oluştu", error = ex.Message });
        }
    }

    /// <summary>
    /// Üretim emri detayını getir
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ManufacturingOrderDto>> GetById(int id)
    {
        var order = await _context.ManufacturingOrders
            .Include(m => m.Product)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (order == null)
        {
            return NotFound(new { message = $"Üretim emri bulunamadı: {id}" });
        }

        var dto = new ManufacturingOrderDto
        {
            Id = order.Id,
            OrderNo = order.OrderNo,
            ProductId = order.ProductId,
            ProductName = order.Product?.Name,
            ProductSku = order.Product?.SKU,
            Quantity = order.Quantity,
            Status = order.Status,
            DueDate = order.DueDate,
            IsSynced = order.IsSynced,
            CreatedAt = order.CreatedAt
        };

        return Ok(dto);
    }

    /// <summary>
    /// Yeni üretim emri oluştur
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ManufacturingOrderDto>> Create([FromBody] CreateManufacturingOrderRequest request)
    {
        // Validate product
        var product = await _context.Products.FindAsync(request.ProductId);
        if (product == null)
        {
            return BadRequest(new { message = $"Ürün bulunamadı: {request.ProductId}" });
        }

        // Generate order number
        var orderNo = $"MO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

        var order = new ManufacturingOrder
        {
            OrderNo = orderNo,
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            Status = request.Status ?? "NotStarted",
            DueDate = request.DueDate ?? DateTime.UtcNow.AddDays(7),
            IsSynced = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.ManufacturingOrders.Add(order);
        await _context.SaveChangesAsync();

        _auditService.LogCreate(
            "ManufacturingOrder",
            order.Id.ToString(),
            User.Identity?.Name ?? "System",
            $"Yeni üretim emri oluşturuldu: {orderNo}");

        _loggingService.LogInfo($"ManufacturingOrder created: {orderNo}", User.Identity?.Name, null, LogCategory.Business);

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, new ManufacturingOrderDto
        {
            Id = order.Id,
            OrderNo = order.OrderNo,
            ProductId = order.ProductId,
            ProductName = product.Name,
            ProductSku = product.SKU,
            Quantity = order.Quantity,
            Status = order.Status,
            DueDate = order.DueDate,
            IsSynced = order.IsSynced,
            CreatedAt = order.CreatedAt
        });
    }

    /// <summary>
    /// Üretim emrini güncelle
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ManufacturingOrderDto>> Update(int id, [FromBody] UpdateManufacturingOrderRequest request)
    {
        var order = await _context.ManufacturingOrders
            .Include(m => m.Product)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (order == null)
        {
            return NotFound(new { message = $"Üretim emri bulunamadı: {id}" });
        }

        // Update fields
        if (request.Quantity.HasValue) order.Quantity = request.Quantity.Value;
        if (request.Status != null) order.Status = request.Status;
        if (request.DueDate.HasValue) order.DueDate = request.DueDate.Value;

        await _context.SaveChangesAsync();

        _auditService.LogUpdate(
            "ManufacturingOrder",
            id.ToString(),
            User.Identity?.Name ?? "System",
            null,
            "Üretim emri güncellendi");

        _loggingService.LogInfo($"ManufacturingOrder updated: {order.OrderNo}", User.Identity?.Name, null, LogCategory.Business);

        return Ok(new ManufacturingOrderDto
        {
            Id = order.Id,
            OrderNo = order.OrderNo,
            ProductId = order.ProductId,
            ProductName = order.Product?.Name,
            ProductSku = order.Product?.SKU,
            Quantity = order.Quantity,
            Status = order.Status,
            DueDate = order.DueDate,
            IsSynced = order.IsSynced,
            CreatedAt = order.CreatedAt
        });
    }

    /// <summary>
    /// Üretim emrini sil
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var order = await _context.ManufacturingOrders.FindAsync(id);

        if (order == null)
        {
            return NotFound(new { message = $"Üretim emri bulunamadı: {id}" });
        }

        if (order.IsSynced)
        {
            return BadRequest(new { message = "Senkronize edilmiş üretim emirleri silinemez" });
        }

        _context.ManufacturingOrders.Remove(order);
        await _context.SaveChangesAsync();

        _auditService.LogDelete(
            "ManufacturingOrder",
            id.ToString(),
            User.Identity?.Name ?? "System",
            $"Üretim emri silindi: {order.OrderNo}");

        _loggingService.LogInfo($"ManufacturingOrder deleted: {order.OrderNo}", User.Identity?.Name, null, LogCategory.Business);

        return Ok(new { message = "Üretim emri silindi" });
    }

    /// <summary>
    /// Üretim emri istatistikleri
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult> GetStats()
    {
        try
        {
            var stats = await _context.ManufacturingOrders
                .GroupBy(m => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    NotStarted = g.Count(m => m.Status == "NotStarted"),
                    InProgress = g.Count(m => m.Status == "InProgress"),
                    Completed = g.Count(m => m.Status == "Completed"),
                    Cancelled = g.Count(m => m.Status == "Cancelled"),
                    Synced = g.Count(m => m.IsSynced),
                    NotSynced = g.Count(m => !m.IsSynced),
                    TotalQuantity = g.Sum(m => m.Quantity)
                })
                .FirstOrDefaultAsync();

            return Ok(stats ?? new
            {
                Total = 0,
                NotStarted = 0,
                InProgress = 0,
                Completed = 0,
                Cancelled = 0,
                Synced = 0,
                NotSynced = 0,
                TotalQuantity = 0m
            });
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"ManufacturingOrders GetStats failed: {ex.Message}", ex);
            return StatusCode(500, new { message = "İstatistikler yüklenirken hata oluştu" });
        }
    }
}

// ===== DTO'LAR =====

public class ManufacturingOrderDto
{
    public int Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? ProductSku { get; set; }
    public decimal Quantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public bool IsSynced { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateManufacturingOrderRequest
{
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public string? Status { get; set; }
    public DateTime? DueDate { get; set; }
}

public class UpdateManufacturingOrderRequest
{
    public decimal? Quantity { get; set; }
    public string? Status { get; set; }
    public DateTime? DueDate { get; set; }
}
