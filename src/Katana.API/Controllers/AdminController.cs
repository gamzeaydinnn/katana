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
[AllowAnonymous]
public class AdminController : ControllerBase
{
    private readonly IKatanaService _katanaService;
    private readonly IntegrationDbContext _context;
    private readonly ILogger<AdminController> _logger;
    private readonly ILoggingService _loggingService;

    public AdminController(
        IKatanaService katanaService,
        IntegrationDbContext context,
        ILogger<AdminController> logger,
        ILoggingService loggingService)
    {
        _katanaService = katanaService;
        _context = context;
        _logger = logger;
        _loggingService = loggingService;
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


