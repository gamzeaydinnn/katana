using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Katana.Business.Interfaces;
using Microsoft.Extensions.Options;
using Katana.Data.Configuration;

namespace Katana.API.Controllers;

/// <summary>
/// Diagnostic endpoints to verify configuration and Katana API connectivity. Admin-only.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class TestController : ControllerBase
{
    private readonly IKatanaService _katanaService;
    private readonly IOptions<KatanaApiSettings> _settings;
    private readonly ILogger<TestController> _logger;

    public TestController(IKatanaService katanaService, IOptions<KatanaApiSettings> settings, ILogger<TestController> logger)
    {
        _katanaService = katanaService;
        _settings = settings;
        _logger = logger;
    }

    [HttpGet("katana-config")]
    public IActionResult GetKatanaConfig()
    {
        var config = _settings.Value;
        return Ok(new
        {
            baseUrl = config.BaseUrl,
            productsEndpoint = config.Endpoints.Products,
            fullUrl = $"{config.BaseUrl.TrimEnd('/')}/{config.Endpoints.Products}",
            apiKeyLength = config.ApiKey?.Length ?? 0,
            apiKeyFirst10 = config.ApiKey?.Substring(0, Math.Min(10, config.ApiKey.Length)),
            hasApiKey = !string.IsNullOrEmpty(config.ApiKey)
        });
    }

    [HttpGet("katana-direct")]
    public async Task<IActionResult> TestKatanaDirect()
    {
        try
        {
            _logger.LogWarning("=== DIRECT KATANA API TEST START ===");
            
            var products = await _katanaService.GetProductsAsync();
            
            _logger.LogWarning("=== DIRECT KATANA API TEST END - Products Count: {Count} ===", products.Count);
            
            return Ok(new
            {
                success = true,
                count = products.Count,
                firstProduct = products.FirstOrDefault()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test failed");
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }
}
