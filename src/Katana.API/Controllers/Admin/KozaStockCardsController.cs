using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using KozaDtos = Katana.Core.DTOs.Koza;
using KozaStockCardListDto = Katana.Core.DTOs.KozaStokKartiListDto;

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
    /// <param name="eklemeBas">Optional: Filter by creation date start (dd/MM/yyyy)</param>
    /// <param name="eklemeBit">Optional: Filter by creation date end (dd/MM/yyyy)</param>
    /// <param name="ct">Cancellation token</param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<KozaStockCardListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> List(
        [FromQuery] DateTime? eklemeBas = null,
        [FromQuery] DateTime? eklemeBit = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Listing Koza stock cards (eklemeBas={Start}, eklemeBit={End})", 
                eklemeBas?.ToString("dd/MM/yyyy") ?? "null", 
                eklemeBit?.ToString("dd/MM/yyyy") ?? "null");
            
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            var request = new LucaListStockCardsRequest
            {
                StkSkart = new LucaStockCardCodeFilter()
            };

            if (eklemeBas.HasValue || eklemeBit.HasValue)
            {
                request.StkSkart.EklemeTarihiBas = eklemeBas?.ToString("dd/MM/yyyy");
                request.StkSkart.EklemeTarihiBit = eklemeBit?.ToString("dd/MM/yyyy") ?? DateTime.Now.ToString("dd/MM/yyyy");
                request.StkSkart.EklemeTarihiOp = "between";
            }
            
            var allCards = new List<KozaStockCardListDto>();
            var pageNo = 1;
            const int pageSize = 100;
            var maxPages = 20;
            var consecutiveEmptyPages = 0;

            while (pageNo <= maxPages)
            {
                JsonElement jsonResult = default;
                var attempt = 0;
                const int maxAttempts = 2;

                while (attempt < maxAttempts)
                {
                    attempt++;
                    try
                    {
                        jsonResult = await _lucaService.ListStockCardsAsync(
                            request,
                            linkedCts.Token,
                            pageNo: pageNo,
                            pageSize: pageSize,
                            skipEnsure: false);
                        
                        var pageCards = ParseStockCards(jsonResult);
                        
                        if (pageCards.Count == 0)
                        {
                            consecutiveEmptyPages++;
                            _logger.LogInformation("Page {Page} returned 0 items (consecutive empty: {Empty})", 
                                pageNo, consecutiveEmptyPages);
                            
                            if (consecutiveEmptyPages >= 2)
                            {
                                _logger.LogInformation("Two consecutive empty pages, stopping pagination");
                                goto PaginationComplete;
                            }
                        }
                        else
                        {
                            consecutiveEmptyPages = 0;
                            allCards.AddRange(pageCards);
                            _logger.LogInformation("Page {Page}: +{Count} cards (Total: {Total})", 
                                pageNo, pageCards.Count, allCards.Count);
                            
                            if (pageCards.Count < pageSize)
                            {
                                _logger.LogInformation("Last page detected (partial page with {Count} items)", pageCards.Count);
                                goto PaginationComplete;
                            }
                        }
                        
                        break;
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("HTML", StringComparison.OrdinalIgnoreCase) && attempt < maxAttempts)
                    {
                        _logger.LogWarning("Page {Page} attempt {Attempt} returned HTML, refreshing session...", pageNo, attempt);
                        await _lucaService.ForceSessionRefreshAsync();
                        await Task.Delay(1000);
                    }
                    catch (Exception ex) when (attempt < maxAttempts)
                    {
                        _logger.LogWarning(ex, "Page {Page} attempt {Attempt} failed, retrying...", pageNo, attempt);
                        await Task.Delay(500);
                    }
                }
                
                pageNo++;
            }

            PaginationComplete:
            _logger.LogInformation("✅ Retrieved {Count} stock cards from Koza (pages: {Pages})", 
                allCards.Count, pageNo - 1);
            
            return Ok(allCards);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("Koza stock cards request timed out after 5 minutes");
            return StatusCode(504, new { error = "İstek zaman aşımına uğradı. Koza API yanıt vermedi. Lütfen daha sonra tekrar deneyin." });
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("Koza stock cards request was cancelled (timeout)");
            return StatusCode(504, new { error = "İstek zaman aşımına uğradı. Koza API yanıt vermedi. Lütfen daha sonra tekrar deneyin." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list Koza stock cards");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private static List<KozaStockCardListDto> ParseStockCards(JsonElement jsonResult)
    {
        if (jsonResult.ValueKind == JsonValueKind.Array)
        {
            return JsonSerializer.Deserialize<List<KozaStockCardListDto>>(jsonResult.GetRawText()) 
                ?? new List<KozaStockCardListDto>();
        }

        if (jsonResult.ValueKind == JsonValueKind.Object)
        {
            foreach (var key in new[] { "list", "stkSkart", "data", "items" })
            {
                if (jsonResult.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.Array)
                {
                    return JsonSerializer.Deserialize<List<KozaStockCardListDto>>(prop.GetRawText()) 
                        ?? new List<KozaStockCardListDto>();
                }
            }
        }

        return new List<KozaStockCardListDto>();
    }

    /// <summary>
    /// Koza'da yeni stok kartı oluştur
    /// POST /api/admin/koza/stocks/create
    /// </summary>
    [HttpPost("create")]
    [ProducesResponseType(typeof(KozaDtos.KozaResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] KozaDtos.KozaCreateStokKartiRequest request,
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

    /// <summary>
    /// Koza'da yeni stok kartı oluştur - V2 (Yeni API formatı)
    /// POST /api/admin/koza/stocks/v2
    /// </summary>
    /// <remarks>
    /// Örnek request:
    /// {
    ///   "kartAdi": "Test Ürünü",
    ///   "kartKodu": "00013225",
    ///   "kartTipi": 1,
    ///   "kartAlisKdvOran": 1,
    ///   "olcumBirimiId": 1,
    ///   "baslangicTarihi": "06/04/2022",
    ///   "kartTuru": 1,
    ///   "barkod": "8888888"
    /// }
    /// </remarks>
    [HttpPost("v2")]
    [ProducesResponseType(typeof(Katana.Core.DTOs.LucaCreateStockCardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateV2(
        [FromBody] Katana.Core.DTOs.LucaCreateStockCardRequestV2 request,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Creating Koza stock card V2: {Kod} - {Ad}", 
                request.KartKodu, request.KartAdi);

            var result = await _lucaService.CreateStockCardV2Async(request, ct);
            
            if (!result.Error)
            {
                _logger.LogInformation("Stock card V2 created successfully: {Kod}, SkartId: {SkartId}", 
                    request.KartKodu, result.SkartId);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("Stock card V2 creation failed: {Message}", result.Message);
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Koza stock card V2");
            return StatusCode(500, new Katana.Core.DTOs.LucaCreateStockCardResponse 
            { 
                Error = true, 
                Message = ex.Message 
            });
        }
    }
}
