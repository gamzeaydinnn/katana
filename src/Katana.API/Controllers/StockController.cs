using Katana.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Katana.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockController : ControllerBase
{
    private readonly IKatanaStockService _katanaStockService;
    private readonly ILogger<StockController> _logger;

    public StockController(
        IKatanaStockService katanaStockService,
        ILogger<StockController> logger)
    {
        _katanaStockService = katanaStockService;
        _logger = logger;
    }

    /// <summary>
    /// Get stock movements from Katana API
    /// </summary>
    [HttpGet("movements")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetStockMovements(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] int? page = null)
    {
        try
        {
            var movements = await _katanaStockService.GetStockMovementsAsync(fromDate, page);
            return Ok(new { data = movements, count = movements.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching stock movements");
            return StatusCode(500, new { error = "Failed to fetch stock movements from Katana API", details = ex.Message });
        }
    }

    /// <summary>
    /// Get stock status for all products
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetStockStatus([FromQuery] int? page = null, [FromQuery] int? limit = null)
    {
        try
        {
            var products = await _katanaStockService.GetAllProductsAsync(page, limit);
            
            // Transform to stock status format using correct Product properties
            var stockStatus = products.Select(p => new
            {
                id = p.Id.ToString(),
                name = p.Name,
                sku = p.SKU,
                quantity = p.Stock,
                unit = "pcs", // Default unit
                minStock = 10, // Default min stock
                maxStock = (int?)null, // No max stock
                status = GetStockStatus(p.Stock),
                lastUpdated = p.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                category = "" // No category in Product entity
            }).ToList();
            
            return Ok(new { data = stockStatus, count = stockStatus.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching stock status");
            return StatusCode(500, new { error = "Failed to fetch stock status from Katana API", details = ex.Message });
        }
    }

    private static string GetStockStatus(int quantity)
    {
        if (quantity == 0)
            return "Out";
        
        if (quantity < 10)
            return "Low";
        
        if (quantity > 1000)
            return "High";
        
        return "Normal";
    }
}