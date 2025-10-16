using Katana.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Katana.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IKatanaService _katanaService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IKatanaService katanaService,
        ILogger<ProductsController> logger)
    {
        _katanaService = katanaService;
        _logger = logger;
    }

    /// <summary>
    /// Get all products from Katana API
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetProducts([FromQuery] int? page = null, [FromQuery] int? limit = null)
    {
        try
        {
            var products = await _katanaService.GetProductsAsync();
            return Ok(new { data = products, count = products.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching products");
            return StatusCode(500, new { error = "Failed to fetch products from Katana API", details = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific product by ID from Katana API
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetProduct(string id)
    {
        try
        {
            var product = await _katanaService.GetProductBySkuAsync(id);
            
            if (product == null)
            {
                return NotFound(new { error = $"Product with ID {id} not found" });
            }
            
            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching product {ProductId}", id);
            return StatusCode(500, new { error = "Failed to fetch product from Katana API", details = ex.Message });
        }
    }
}