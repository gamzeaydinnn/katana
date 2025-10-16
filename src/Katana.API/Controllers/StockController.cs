using Katana.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Katana.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockController : ControllerBase
{
    private readonly IKatanaService _katanaService;
    private readonly ILogger<StockController> _logger;

    public StockController(
        IKatanaService katanaService,
        ILogger<StockController> logger)
    {
        _katanaService = katanaService;
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
            var from = fromDate ?? DateTime.UtcNow.AddMonths(-1);
            var to = DateTime.UtcNow;
            var movements = await _katanaService.GetStockChangesAsync(from, to);
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
            var products = await _katanaService.GetProductsAsync();
            
            // Transform to stock status format using correct Product properties
            var stockStatus = products.Select(p => new
            {
                id = p.SKU,
                name = p.Name,
                sku = p.SKU,
                quantity = 0,
                unit = "pcs",
                minStock = 10,
                maxStock = (int?)null,
                status = p.IsActive ? "Normal" : "Out",
                lastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"),
                category = ""
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