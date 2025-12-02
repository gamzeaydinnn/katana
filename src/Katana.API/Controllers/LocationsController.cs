using Microsoft.AspNetCore.Mvc;
using Katana.Business.Interfaces;

namespace Katana.API.Controllers;

/// <summary>
/// Katana Location endpoint'leri
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LocationsController : ControllerBase
{
    private readonly IKatanaService _katanaService;
    private readonly ILogger<LocationsController> _logger;

    public LocationsController(
        IKatanaService katanaService,
        ILogger<LocationsController> logger)
    {
        _katanaService = katanaService;
        _logger = logger;
    }

    /// <summary>
    /// Katana'dan tüm location'ları getir
    /// GET /api/Locations
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetLocations()
    {
        try
        {
            _logger.LogInformation("Fetching locations from Katana API");
            var locations = await _katanaService.GetLocationsAsync();
            
            _logger.LogInformation("Retrieved {Count} locations from Katana", locations.Count);
            return Ok(locations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch locations from Katana");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
