using Katana.Business.DTOs;
using Katana.Business.Interfaces;
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
[AllowAnonymous]
public class AdminController : ControllerBase
{
    private readonly IKatanaService _katanaService;
    private readonly IntegrationDbContext _context;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IKatanaService katanaService,
        IntegrationDbContext context,
        ILogger<AdminController> logger)
    {
        _katanaService = katanaService;
        _context = context;
        _logger = logger;
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        try
        {
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
            return StatusCode(500, new { error = "Failed to get statistics" });
        }
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
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
            var isHealthy = await _katanaService.TestConnectionAsync();
            return Ok(new { isHealthy });
        }
        catch (Exception)
        {
            return Ok(new { isHealthy = false });
        }
    }
}


