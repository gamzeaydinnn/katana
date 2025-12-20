using Katana.Business.Services;
using Katana.Core.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Katana.API.Controllers.Admin;

[ApiController]
[Route("api/admin/location-mappings")]
public class LocationMappingController : ControllerBase
{
    private readonly ILocationMappingService _locationMappingService;
    private readonly ILogger<LocationMappingController> _logger;

    public LocationMappingController(
        ILocationMappingService locationMappingService,
        ILogger<LocationMappingController> logger)
    {
        _locationMappingService = locationMappingService;
        _logger = logger;
    }

    /// <summary>
    /// Tüm location mapping'leri getir
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<LocationKozaDepotMapping>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllMappings()
    {
        try
        {
            var mappings = await _locationMappingService.GetAllMappingsAsync();
            return Ok(mappings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all location mappings");
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Location ID'ye göre mapping getir
    /// </summary>
    [HttpGet("{locationId}")]
    [ProducesResponseType(typeof(LocationKozaDepotMapping), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMappingByLocationId(string locationId)
    {
        try
        {
            var mapping = await _locationMappingService.GetMappingByLocationIdAsync(locationId);
            
            if (mapping == null)
            {
                return NotFound(new { error = "Mapping not found", locationId });
            }

            return Ok(mapping);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting mapping for Location {LocationId}", locationId);
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Location ID'ye göre depo kodu getir
    /// </summary>
    [HttpGet("{locationId}/depo-kodu")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDepoKodu(string locationId)
    {
        try
        {
            var depoKodu = await _locationMappingService.GetDepoKoduByLocationIdAsync(locationId);
            return Ok(new { locationId, depoKodu });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting depo kodu for Location {LocationId}", locationId);
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Location mapping oluştur veya güncelle
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(LocationKozaDepotMapping), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrUpdateMapping([FromBody] CreateLocationMappingRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.KatanaLocationId))
            {
                return BadRequest(new { error = "KatanaLocationId is required" });
            }

            if (string.IsNullOrWhiteSpace(request.KozaDepoKodu))
            {
                return BadRequest(new { error = "KozaDepoKodu is required" });
            }

            var mapping = await _locationMappingService.CreateOrUpdateMappingAsync(
                request.KatanaLocationId,
                request.KozaDepoKodu,
                request.KozaDepoId,
                request.KatanaLocationName,
                request.KozaDepoTanim);

            return Ok(mapping);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating location mapping");
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Depo kodunu validate et
    /// </summary>
    [HttpPost("validate-depo-kodu")]
    [ProducesResponseType(typeof(DepoKoduValidationResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateDepoKodu([FromBody] ValidateDepoKoduRequest request)
    {
        try
        {
            var isValid = await _locationMappingService.ValidateDepoKoduAsync(request.DepoKodu);
            
            return Ok(new DepoKoduValidationResponse
            {
                DepoKodu = request.DepoKodu,
                IsValid = isValid,
                Message = isValid 
                    ? "Depo kodu geçerli" 
                    : "Depo kodu KozaDepots tablosunda bulunamadı"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating depo kodu: {DepoKodu}", request.DepoKodu);
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Location → Depo Kodu dictionary'si getir
    /// </summary>
    [HttpGet("dictionary")]
    [ProducesResponseType(typeof(Dictionary<string, string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLocationToDepoKoduMap()
    {
        try
        {
            var map = await _locationMappingService.GetLocationToDepoKoduMapAsync();
            return Ok(map);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting location to depo kodu map");
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }
}

// DTOs
public class CreateLocationMappingRequest
{
    public string KatanaLocationId { get; set; } = string.Empty;
    public string KozaDepoKodu { get; set; } = string.Empty;
    public long? KozaDepoId { get; set; }
    public string? KatanaLocationName { get; set; }
    public string? KozaDepoTanim { get; set; }
}

public class ValidateDepoKoduRequest
{
    public string DepoKodu { get; set; } = string.Empty;
}

public class DepoKoduValidationResponse
{
    public string DepoKodu { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
}
