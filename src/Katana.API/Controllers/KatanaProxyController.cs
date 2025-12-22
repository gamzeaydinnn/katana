using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Katana.API.Controllers;

/// <summary>
/// Proxy controller for Katana MRP API - provides direct access to Katana API endpoints
/// </summary>
[ApiController]
[Route("api/katana")]
[Authorize]
public class KatanaProxyController : ControllerBase
{
    private readonly IKatanaService _katanaService;
    private readonly ILogger<KatanaProxyController> _logger;

    public KatanaProxyController(IKatanaService katanaService, ILogger<KatanaProxyController> logger)
    {
        _katanaService = katanaService;
        _logger = logger;
    }

    /// <summary>
    /// Test connection to Katana API
    /// </summary>
    [HttpGet("test-connection")]
    public async Task<IActionResult> TestConnection()
    {
        try
        {
            var ok = await _katanaService.TestConnectionAsync();
            return Ok(new { connected = ok });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while testing Katana connection");
            return StatusCode(500, new { connected = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get sales orders from Katana API
    /// </summary>
    [HttpGet("sales-orders")]
    public async Task<IActionResult> GetSalesOrders([FromQuery] int limit = 10, [FromQuery] DateTime? fromDate = null)
    {
        try
        {
            _logger.LogInformation("Fetching sales orders from Katana API. Limit: {Limit}, FromDate: {FromDate}", limit, fromDate);
            
            var orders = await _katanaService.GetSalesOrdersAsync(fromDate);
            
            if (orders == null || !orders.Any())
            {
                return Ok(new { data = new List<object>(), count = 0 });
            }

            var limitedOrders = orders.Take(limit).ToList();
            
            return Ok(new 
            { 
                data = limitedOrders, 
                count = limitedOrders.Count,
                total = orders.Count 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while fetching sales orders from Katana");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get products from Katana API
    /// </summary>
    [HttpGet("products")]
    public async Task<IActionResult> GetProducts([FromQuery] int limit = 20)
    {
        try
        {
            var products = await _katanaService.GetProductsAsync();
            var count = products?.Count ?? 0;
            var sample = (products ?? new List<KatanaProductDto>()).Take(Math.Max(0, limit));
            return Ok(new { count, sample });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while fetching products from Katana");
            return StatusCode(500, new { count = 0, error = ex.Message });
        }
    }

    /// <summary>
    /// Get customers from Katana API
    /// </summary>
    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomers([FromQuery] int limit = 20)
    {
        try
        {
            var customers = await _katanaService.GetCustomersAsync();
            var count = customers?.Count ?? 0;
            var sample = (customers ?? new List<KatanaCustomerDto>()).Take(Math.Max(0, limit));
            return Ok(new { count, sample });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while fetching customers from Katana");
            return StatusCode(500, new { count = 0, error = ex.Message });
        }
    }
}
