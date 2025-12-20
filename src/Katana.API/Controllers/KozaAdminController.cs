using Katana.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Katana.API.Controllers;

[ApiController]
[Route("api/admin/koza")]
[Authorize]
public class KozaAdminController : ControllerBase
{
    private readonly ILucaService _lucaService;
    private readonly ILogger<KozaAdminController> _logger;

    public KozaAdminController(ILucaService lucaService, ILogger<KozaAdminController> logger)
    {
        _lucaService = lucaService;
        _logger = logger;
    }

    [HttpGet("ping")]
    public async Task<IActionResult> PingAsync()
    {
        var result = new ExternalServiceHealthResult { Service = "Luca API" };

        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var isHealthy = await _lucaService.TestConnectionAsync();
            sw.Stop();

            result.IsHealthy = isHealthy;
            result.ResponseTimeMs = sw.ElapsedMilliseconds;
            result.Message = isHealthy ? "Connection successful" : "Connection failed";
            result.CheckedAt = DateTime.UtcNow;

            if (!isHealthy)
            {
                _logger.LogWarning("Koza ping failed.");
                return StatusCode(503, result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Koza ping hatası");
            result.IsHealthy = false;
            result.Message = $"Error: {ex.Message}";
            result.CheckedAt = DateTime.UtcNow;
            return StatusCode(503, result);
        }
    }
}
