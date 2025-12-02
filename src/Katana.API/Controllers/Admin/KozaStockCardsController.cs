using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Katana.Business.Interfaces;
using Katana.Business.DTOs.Koza;

namespace Katana.API.Controllers.Admin;

/// <summary>
/// Koza Stok Kartı yönetimi endpoint'leri
/// Frontend bu endpoint'ler üzerinden Koza stok kartı işlemlerini yapar
/// </summary>
[ApiController]
[Route("api/admin/koza/stocks")]
[AllowAnonymous]  // TODO: Restore [Authorize(Roles = "Admin")] after CORS testing
public sealed class KozaStockCardsController : ControllerBase
{
    private readonly ILucaService _lucaService;
    private readonly ILogger<KozaStockCardsController> _logger;

    public KozaStockCardsController(
        ILucaService lucaService,
        ILogger<KozaStockCardsController> logger)
    {
        _lucaService = lucaService;
        _logger = logger;
    }

    /// <summary>
    /// Koza'daki tüm stok kartlarını listele
    /// GET /api/admin/koza/stocks
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<KozaStokKartiDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Listing Koza stock cards");
            var stockCards = await _lucaService.ListStockCardsSimpleAsync(ct);
            
            _logger.LogInformation("Retrieved {Count} stock cards from Koza", stockCards.Count);
            return Ok(stockCards);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list Koza stock cards");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Koza'da yeni stok kartı oluştur
    /// POST /api/admin/koza/stocks/create
    /// </summary>
    [HttpPost("create")]
    [ProducesResponseType(typeof(KozaResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] KozaCreateStokKartiRequest request,
        CancellationToken ct)
    {
        try
        {
            // Validasyon
            if (string.IsNullOrWhiteSpace(request.StkKart?.KartKodu))
            {
                return BadRequest(new { error = "Stok kodu (kartKodu) zorunludur" });
            }

            if (string.IsNullOrWhiteSpace(request.StkKart?.KartAdi))
            {
                return BadRequest(new { error = "Stok adı (kartAdi) zorunludur" });
            }

            if (request.StkKart.OlcumBirimiId <= 0)
            {
                return BadRequest(new { error = "Geçerli bir ölçüm birimi (olcumBirimiId) seçilmelidir" });
            }

            if (string.IsNullOrWhiteSpace(request.StkKart?.KategoriAgacKod))
            {
                return BadRequest(new { error = "Kategori kodu (kategoriAgacKod) zorunludur" });
            }

            _logger.LogInformation("Creating Koza stock card: {Kod} - {Ad}", 
                request.StkKart.KartKodu, request.StkKart.KartAdi);

            var result = await _lucaService.CreateStockCardSimpleAsync(request, ct);
            
            if (result.Success)
            {
                _logger.LogInformation("Stock card created successfully: {Kod}", request.StkKart.KartKodu);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("Stock card creation failed: {Message}", result.Message);
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Koza stock card");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
