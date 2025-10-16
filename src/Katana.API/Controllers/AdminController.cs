using Katana.Core.DTOs;
using Katana.Core.Interfaces;
using Katana.Data.Context;
using Katana.Data.Models;
using Katana.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Katana.API.Controllers;

[ApiController]
[Route("api/adminpanel")]
public class AdminController : ControllerBase
{
    private readonly IKatanaStockService _stockService;
    private readonly IntegrationDbContext _context;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IKatanaStockService stockService,
        IntegrationDbContext context,
        ILogger<AdminController> logger)
    {
        _stockService = stockService;
        _context = context;
        _logger = logger;
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        try
        {
            var products = await _stockService.GetAllProductsAsync(1, 100);
            var totalProducts = products.Count;
            var totalStock = products.Sum(p => (int)p.Stock);

            var stats = new
            {
                totalProducts,
                totalStock,
                successfulSyncs = 0,
                failedSyncs = 0
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin statistics");
            return StatusCode(500, new { error = "Failed to get statistics" });
        }
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var allProducts = await _stockService.GetAllProductsAsync(1, 100);

            var startIndex = (page - 1) * pageSize;
            var products = allProducts
                .Skip(startIndex)
                .Take(pageSize)
                .Select(p => new
                {
                    id = p.Id.ToString(),
                    sku = p.SKU,
                    name = p.Name,
                    stock = (int)p.Stock,
                    status = p.Stock > 0 ? "Aktif" : "Stokta Yok"
                }).ToList();

            return Ok(new { products, total = allProducts.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products");
            return StatusCode(500, new { error = "Failed to get products" });
        }
    }

    [HttpGet("products/{id}")]
    public async Task<IActionResult> GetProductById(string id)
    {
        try
        {
            var product = await _stockService.GetProductByIdAsync(id);
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
            return Ok(new { logs = new List<object>(), total = 0 });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sync logs");
            return StatusCode(500, new { error = "Failed to get sync logs" });
        }
    }

    [HttpGet("katana-health")]
    public async Task<IActionResult> CheckKatanaHealth()
    {
        try
        {
            var isHealthy = await _stockService.IsKatanaApiHealthyAsync();
            return Ok(new { isHealthy });
        }
        catch (Exception)
        {
            return Ok(new { isHealthy = false });
        }
    }
}


