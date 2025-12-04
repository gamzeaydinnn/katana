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
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Katana API bağlantı hatası - HTTP isteği başarısız");
            return StatusCode(503, new { 
                error = "Katana API'ye bağlanılamadı. Lütfen Katana API ayarlarını kontrol edin.",
                details = ex.Message,
                suggestion = "Katana API bağlantısı olmadan depo kartı oluşturmak için manuel giriş yapabilirsiniz."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch locations from Katana");
            return StatusCode(500, new { 
                error = "Katana'dan location bilgileri alınamadı",
                details = ex.Message 
            });
        }
    }
    
    /// <summary>
    /// Varsayılan/manuel depo listesi döndür (Katana bağlantısı olmadan)
    /// GET /api/Locations/defaults
    /// </summary>
    [HttpGet("defaults")]
    public IActionResult GetDefaultLocations()
    {
        _logger.LogInformation("Returning default locations (Katana API bypass)");
        
        var defaultLocations = new[]
        {
            new {
                id = 1,
                name = "MERKEZ DEPO",
                legal_name = "Ana Depo",
                address = new {
                    line_1 = "",
                    city = "",
                    country = "Türkiye"
                },
                is_primary = true
            }
        };
        
        return Ok(defaultLocations);
    }
}
