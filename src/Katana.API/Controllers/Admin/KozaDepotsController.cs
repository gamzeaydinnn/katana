using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Katana.Business.Interfaces;
using Katana.Business.DTOs.Koza;

namespace Katana.API.Controllers.Admin;

/// <summary>
/// Koza Depo yönetimi endpoint'leri
/// Frontend bu endpoint'ler üzerinden Koza depo işlemlerini yapar
/// </summary>
[ApiController]
[Route("api/admin/koza/depots")]
[AllowAnonymous]  // TODO: Restore [Authorize(Roles = "Admin")] after CORS testing
public sealed class KozaDepotsController : ControllerBase
{
    private readonly ILucaService _lucaService;
    private readonly ILogger<KozaDepotsController> _logger;

    public KozaDepotsController(
        ILucaService lucaService,
        ILogger<KozaDepotsController> logger)
    {
        _lucaService = lucaService;
        _logger = logger;
    }

    /// <summary>
    /// Koza'daki tüm depoları listele
    /// GET /api/admin/koza/depots
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<KozaDepoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Listing Koza depots");
            var depots = await _lucaService.ListDepotsAsync(ct);
            
            _logger.LogInformation("Retrieved {Count} depots from Koza", depots.Count);
            return Ok(depots);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list Koza depots");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Koza'da yeni depo oluştur
    /// POST /api/admin/koza/depots/create
    /// </summary>
    [HttpPost("create")]
    [ProducesResponseType(typeof(KozaResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] KozaCreateDepotRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.StkDepo?.Kod))
            {
                return BadRequest(new { error = "Depo kodu (kod) zorunludur" });
            }

            if (string.IsNullOrWhiteSpace(request.StkDepo?.Tanim))
            {
                return BadRequest(new { error = "Depo adı (tanim) zorunludur" });
            }

            if (string.IsNullOrWhiteSpace(request.StkDepo?.KategoriKod))
            {
                return BadRequest(new { error = "Depo kategori kodu (kategoriKod) zorunludur" });
            }

            _logger.LogInformation("Creating Koza depot: {Kod} - {Tanim}", 
                request.StkDepo.Kod, request.StkDepo.Tanim);

            var result = await _lucaService.CreateDepotAsync(request, ct);
            
            if (result.Success)
            {
                _logger.LogInformation("Depot created successfully: {Kod}", request.StkDepo.Kod);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("Depot creation failed: {Message}", result.Message);
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Koza depot");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
