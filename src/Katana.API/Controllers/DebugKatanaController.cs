using System.Linq;
using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Katana.API.Controllers;

[ApiController]
[Route("api/debug/katana")]
[Authorize]
public class DebugKatanaController : ControllerBase
{
    private readonly IKatanaService _katanaService;
    private readonly ILogger<DebugKatanaController> _logger;

    public DebugKatanaController(IKatanaService katanaService, ILogger<DebugKatanaController> logger)
    {
        _katanaService = katanaService;
        _logger = logger;
    }

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

    [HttpGet("katana-invoices")]
    public async Task<IActionResult> GetKatanaInvoices([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var start = from ?? DateTime.UtcNow.AddDays(-30);
        var end = to ?? DateTime.UtcNow;
        _logger.LogInformation("Debug: fetching Katana invoices from {Start} to {End}", start, end);
        var invoices = await _katanaService.GetInvoicesAsync(start, end);
        return Ok(new { data = invoices, count = invoices.Count });
    }
}
