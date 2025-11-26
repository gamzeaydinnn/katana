using System;
using System.Threading.Tasks;
using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Data.Configuration;
using Katana.Infrastructure.APIClients;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Globalization;
using Microsoft.Extensions.Options;

namespace Katana.API.Controllers;

[ApiController]
[Route("api/koza-debug")]
public class KozaDebugController : ControllerBase
{
    private readonly ILucaService _lucaService;
    private readonly LucaApiSettings _lucaSettings;
    private readonly ILogger<KozaDebugController> _logger;

    public KozaDebugController(
        ILucaService lucaService,
        IOptions<LucaApiSettings> lucaOptions,
        ILogger<KozaDebugController> logger)
    {
        _lucaService = lucaService;
        _lucaSettings = lucaOptions.Value;
        _logger = logger;
    }

    [HttpPost("stock-card")]
    public async Task<IActionResult> SendStockCard([FromBody] KozaDebugRequest request)
    {
        var sku = string.IsNullOrWhiteSpace(request.SKU)
            ? "DEBUG-" + Guid.NewGuid().ToString("N")[..8]
            : request.SKU.Trim();

        var name = string.IsNullOrWhiteSpace(request.Name) ? sku : request.Name.Trim();

        var stockCard = new LucaCreateStokKartiRequest
        {
            KartAdi = name,
            KartKodu = sku,
            KartTuru = 1,
            BaslangicTarihi = DateTime.UtcNow.Date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            OlcumBirimiId = request.OlcumBirimiId ?? _lucaSettings.DefaultOlcumBirimiId,
            PerakendeSatisBirimFiyat = (double)(request.SalesPrice ?? 100m),
            PerakendeAlisBirimFiyat = (double)(request.PurchasePrice ?? 80m),
            KartAlisKdvOran = request.VatRate ?? _lucaSettings.DefaultKdvOran,
            KartSatisKdvOran = request.VatRate ?? _lucaSettings.DefaultKdvOran,
            UzunAdi = name
        };

        _logger.LogInformation("KozaDebugController => Sending stock card SKU={SKU}, Name={Name}, BranchId={Branch}", sku, name, _lucaSettings.ForcedBranchId);

        try
        {
            var result = await _lucaService.SendStockCardsAsync(new System.Collections.Generic.List<LucaCreateStokKartiRequest> { stockCard });
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "KozaDebugController => Error while sending stock card SKU={SKU}", sku);
            return StatusCode(500, new { error = "Koza send failed", message = ex.Message });
        }
    }
}

public class KozaDebugRequest
{
    public string? SKU { get; set; }
    public string? Name { get; set; }
    public long? OlcumBirimiId { get; set; }
    public decimal? SalesPrice { get; set; }
    public decimal? PurchasePrice { get; set; }
    public double? VatRate { get; set; }
}
