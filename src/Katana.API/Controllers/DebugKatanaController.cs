using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Katana.API.Controllers;

[ApiController]
[Route("api/debug/katana")]
[AllowAnonymous]
public class DebugKatanaController : ControllerBase
{
    private readonly IKatanaService _katanaService;
    private readonly ILogger<DebugKatanaController> _logger;

    public DebugKatanaController(IKatanaService katanaService, ILogger<DebugKatanaController> logger)
    {
        _katanaService = katanaService;
        _logger = logger;
    }

    /// <summary>
    /// Calls KatanaService.TestConnectionAsync and returns whether the configured Katana API is reachable.
    /// GET /api/debug/katana/test-connection
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
    /// Fetch products from Katana (calls GetProductsAsync) and returns a count and a small sample.
    /// GET /api/debug/katana/products?limit=20
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
}
