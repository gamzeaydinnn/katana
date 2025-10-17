using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Katana.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IKatanaService _katanaService;
    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IKatanaService katanaService,
        IProductService productService,
        ILogger<ProductsController> logger)
    {
        _katanaService = katanaService;
        _productService = productService;
        _logger = logger;
    }

    // ==========================================
    // KATANA API ENDPOINTS (External)
    // ==========================================

    /// <summary>
    /// Get all products from Katana API
    /// </summary>
    [HttpGet("katana")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetKatanaProducts([FromQuery] int? page = null, [FromQuery] int? limit = null)
    {
        try
        {
            var products = await _katanaService.GetProductsAsync();
            return Ok(new { data = products, count = products.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching products from Katana API");
            return StatusCode(500, new { error = "Failed to fetch products from Katana API", details = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific product by SKU from Katana API
    /// </summary>
    [HttpGet("katana/{sku}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetKatanaProduct(string sku)
    {
        try
        {
            var product = await _katanaService.GetProductBySkuAsync(sku);
            
            if (product == null)
            {
                return NotFound(new { error = $"Product with SKU {sku} not found" });
            }
            
            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching product {ProductSku} from Katana API", sku);
            return StatusCode(500, new { error = "Failed to fetch product from Katana API", details = ex.Message });
        }
    }

    // ==========================================
    // LOCAL DATABASE ENDPOINTS
    // ==========================================

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll()
    {
        var products = await _productService.GetAllProductsAsync();
        return Ok(products);
    }

    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<ProductSummaryDto>>> GetActive()
    {
        var products = await _productService.GetActiveProductsAsync();
        return Ok(products);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetById(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null)
            return NotFound($"Ürün bulunamadı: {id}");

        return Ok(product);
    }

    [HttpGet("by-sku/{sku}")]
    public async Task<ActionResult<ProductDto>> GetBySku(string sku)
    {
        var product = await _productService.GetProductBySkuAsync(sku);
        if (product == null)
            return NotFound($"SKU bulunamadı: {sku}");

        return Ok(product);
    }

    [HttpGet("category/{categoryId}")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetByCategory(int categoryId)
    {
        var products = await _productService.GetProductsByCategoryAsync(categoryId);
        return Ok(products);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest("Arama terimi boş olamaz");

        var products = await _productService.SearchProductsAsync(q);
        return Ok(products);
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetLowStock([FromQuery] int threshold = 10)
    {
        var products = await _productService.GetLowStockProductsAsync(threshold);
        return Ok(products);
    }

    [HttpGet("out-of-stock")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetOutOfStock()
    {
        var products = await _productService.GetOutOfStockProductsAsync();
        return Ok(products);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto dto)
    {
        var validationErrors = Katana.Business.Validators.ProductValidator.ValidateCreate(dto);
        if (validationErrors.Any())
            return BadRequest(new { errors = validationErrors });

        try
        {
            var product = await _productService.CreateProductAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ProductDto>> Update(int id, [FromBody] UpdateProductDto dto)
    {
        var validationErrors = Katana.Business.Validators.ProductValidator.ValidateUpdate(dto);
        if (validationErrors.Any())
            return BadRequest(new { errors = validationErrors });

        try
        {
            var product = await _productService.UpdateProductAsync(id, dto);
            return Ok(product);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPatch("{id}/stock")]
    public async Task<ActionResult> UpdateStock(int id, [FromBody] int quantity)
    {
        var validationErrors = Katana.Business.Validators.ProductValidator.ValidateStock(quantity);
        if (validationErrors.Any())
            return BadRequest(new { errors = validationErrors });

        var result = await _productService.UpdateStockAsync(id, quantity);
        if (!result)
            return NotFound($"Ürün bulunamadı: {id}");

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            var result = await _productService.DeleteProductAsync(id);
            if (!result)
                return NotFound($"Ürün bulunamadı: {id}");

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPut("{id}/activate")]
    public async Task<ActionResult> Activate(int id)
    {
        var result = await _productService.ActivateProductAsync(id);
        if (!result)
            return NotFound($"Ürün bulunamadı: {id}");

        return NoContent();
    }

    [HttpPut("{id}/deactivate")]
    public async Task<ActionResult> Deactivate(int id)
    {
        var result = await _productService.DeactivateProductAsync(id);
        if (!result)
            return NotFound($"Ürün bulunamadı: {id}");

        return NoContent();
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<ProductStatisticsDto>> GetStatistics()
    {
        var stats = await _productService.GetProductStatisticsAsync();
        return Ok(stats);
    }
}