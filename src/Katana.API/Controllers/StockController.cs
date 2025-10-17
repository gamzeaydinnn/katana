using Katana.Core.DTOs;
using Katana.Core.Interfaces;
using Katana.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Katana.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockController : ControllerBase
{
    private readonly IStockService _stockService;
    private readonly IKatanaService _katanaService;
    private readonly ILogger<StockController> _logger;

    public StockController(
        IStockService stockService,
        IKatanaService katanaService,
        ILogger<StockController> logger)
    {
        _stockService = stockService;
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

    /// <summary>
    /// Get all stock movements from local database
    /// </summary>
    [HttpGet("local/movements")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLocalStockMovements()
    {
        try
        {
            var movements = await _stockService.GetAllStockMovementsAsync();
            return Ok(new { data = movements, count = movements.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting local stock movements");
            return StatusCode(500, new { error = "Failed to get stock movements", details = ex.Message });
        }
    }

    /// <summary>
    /// Get stock movements by product ID
    /// </summary>
    [HttpGet("local/movements/product/{productId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStockMovementsByProduct(int productId)
    {
        try
        {
            var movements = await _stockService.GetStockMovementsByProductIdAsync(productId);
            return Ok(new { data = movements, count = movements.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock movements for product {ProductId}", productId);
            return StatusCode(500, new { error = "Failed to get stock movements", details = ex.Message });
        }
    }

    /// <summary>
    /// Get stock movements by location
    /// </summary>
    [HttpGet("local/movements/location/{location}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStockMovementsByLocation(string location)
    {
        try
        {
            var movements = await _stockService.GetStockMovementsByLocationAsync(location);
            return Ok(new { data = movements, count = movements.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock movements for location {Location}", location);
            return StatusCode(500, new { error = "Failed to get stock movements", details = ex.Message });
        }
    }

    /// <summary>
    /// Get stock movements by date range
    /// </summary>
    [HttpGet("local/movements/range")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStockMovementsByDateRange(
        [FromQuery] DateTime startDate, 
        [FromQuery] DateTime endDate)
    {
        try
        {
            var movements = await _stockService.GetStockMovementsByDateRangeAsync(startDate, endDate);
            return Ok(new { data = movements, count = movements.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock movements for date range");
            return StatusCode(500, new { error = "Failed to get stock movements", details = ex.Message });
        }
    }

    /// <summary>
    /// Create a new stock movement
    /// </summary>
    [HttpPost("local/movements")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateStockMovement([FromBody] CreateStockMovementDto dto)
    {
        try
        {
            var movement = await _stockService.CreateStockMovementAsync(dto);
            return CreatedAtAction(nameof(GetLocalStockMovements), new { id = movement.Id }, movement);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid stock movement data");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating stock movement");
            return StatusCode(500, new { error = "Failed to create stock movement", details = ex.Message });
        }
    }

    /// <summary>
    /// Delete a stock movement
    /// </summary>
    [HttpDelete("local/movements/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStockMovement(int id)
    {
        try
        {
            var result = await _stockService.DeleteStockMovementAsync(id);
            if (!result)
            {
                return NotFound(new { error = $"Stock movement with ID {id} not found" });
            }
            return Ok(new { message = "Stock movement deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting stock movement {Id}", id);
            return StatusCode(500, new { error = "Failed to delete stock movement", details = ex.Message });
        }
    }

    /// <summary>
    /// Get stock summary for all products
    /// </summary>
    [HttpGet("local/summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStockSummary()
    {
        try
        {
            var summary = await _stockService.GetStockSummaryAsync();
            return Ok(new { data = summary, count = summary.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock summary");
            return StatusCode(500, new { error = "Failed to get stock summary", details = ex.Message });
        }
    }

    /// <summary>
    /// Get stock summary for a specific product
    /// </summary>
    [HttpGet("local/summary/{productId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStockSummaryByProduct(int productId)
    {
        try
        {
            var summary = await _stockService.GetStockSummaryByProductIdAsync(productId);
            if (summary == null)
            {
                return NotFound(new { error = $"Product with ID {productId} not found" });
            }
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock summary for product {ProductId}", productId);
            return StatusCode(500, new { error = "Failed to get stock summary", details = ex.Message });
        }
    }

    /// <summary>
    /// Get unsynced stock movements
    /// </summary>
    [HttpGet("local/movements/unsynced")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnsyncedStockMovements()
    {
        try
        {
            var movements = await _stockService.GetUnsyncedStockMovementsAsync();
            return Ok(new { data = movements, count = movements.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unsynced stock movements");
            return StatusCode(500, new { error = "Failed to get unsynced stock movements", details = ex.Message });
        }
    }

    /// <summary>
    /// Mark stock movement as synced
    /// </summary>
    [HttpPut("local/movements/{id}/sync")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsSynced(int id)
    {
        try
        {
            var result = await _stockService.MarkAsSyncedAsync(id);
            if (!result)
            {
                return NotFound(new { error = $"Stock movement with ID {id} not found" });
            }
            return Ok(new { message = "Stock movement marked as synced" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking stock movement {Id} as synced", id);
            return StatusCode(500, new { error = "Failed to mark as synced", details = ex.Message });
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