using Katana.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Katana.API.Controllers;

[ApiController]
[Route("api/cache")]
[Authorize]
public sealed class CacheController : ControllerBase
{
    private readonly ILucaService _lucaService;
    private readonly ILogger<CacheController> _logger;

    public CacheController(ILucaService lucaService, ILogger<CacheController> logger)
    {
        _lucaService = lucaService;
        _logger = logger;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var status = await _lucaService.GetCacheStatusAsync();
        return Ok(status);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        _logger.LogInformation("Manual cache refresh requested");
        var warmed = await _lucaService.WarmupCacheWithRetryAsync(2);
        var status = await _lucaService.GetCacheStatusAsync();
        return warmed
            ? Ok(new { message = "Cache refreshed", status })
            : Problem("Cache refresh failed", statusCode: 500);
    }
}
