using System;
using System.Threading.Tasks;
using Katana.Business.Interfaces;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Katana.Infrastructure.Mappers;
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
    private readonly IKatanaService _katanaService;
    private readonly IntegrationDbContext _dbContext;

    public KozaDebugController(
        ILucaService lucaService,
        IOptions<LucaApiSettings> lucaOptions,
        ILogger<KozaDebugController> logger,
        IKatanaService katanaService,
        IntegrationDbContext dbContext)
    {
        _lucaService = lucaService;
        _lucaSettings = lucaOptions.Value;
        _logger = logger;
        _katanaService = katanaService;
        _dbContext = dbContext;
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
            // Allow explicit category code for testing; if provided, use it as KartKodu (matches LoaderService behaviour)
            KartKodu = string.IsNullOrWhiteSpace(request.CategoryCode) ? sku : request.CategoryCode,
            KartTuru = 1,
            BaslangicTarihi = DateTime.UtcNow.Date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            OlcumBirimiId = request.OlcumBirimiId ?? _lucaSettings.DefaultOlcumBirimiId,
            PerakendeSatisBirimFiyat = (double)(request.SalesPrice ?? 100m),
            PerakendeAlisBirimFiyat = (double)(request.PurchasePrice ?? 80m),
            KartAlisKdvOran = request.VatRate ?? _lucaSettings.DefaultKdvOran,
            KartSatisKdvOran = request.VatRate ?? _lucaSettings.DefaultKdvOran,
            UzunAdi = name
        };

        // If a category tree code was supplied explicitly, populate KategoriAgacKod as well
        if (!string.IsNullOrWhiteSpace(request.CategoryCode))
        {
            stockCard.KategoriAgacKod = request.CategoryCode.Trim();
        }

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

    [HttpGet("dry-payload")]
    public async Task<IActionResult> GetDryPayload([FromQuery] int limit = 50)
    {
        try
        {
            var products = await _katanaService.GetProductsAsync();
            var selected = products.Take(limit).ToList();

            var entries = await _dbContext.MappingTables
                .Where(m => m.IsActive && m.MappingType != null && m.MappingType.ToUpper() == "PRODUCT_CATEGORY")
                .Select(m => new { m.SourceValue, m.TargetValue })
                .ToListAsync();

            var mappings = entries
                .Where(e => !string.IsNullOrWhiteSpace(e.SourceValue))
                .ToDictionary(e => NormalizeMappingKey(e.SourceValue), e => e.TargetValue ?? string.Empty, StringComparer.OrdinalIgnoreCase);

            var output = new System.Collections.Generic.List<object>();
            foreach (var p in selected)
            {
                var dto = KatanaToLucaMapper.MapKatanaProductToStockCard(p, _lucaSettings, mappings);
                output.Add(new { Sku = p.SKU, KartKodu = dto.KartKodu, KategoriAgacKod = dto.KategoriAgacKod, KartAdi = dto.KartAdi, Barkod = dto.Barkod });
            }

            return Ok(output);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "KozaDebugController => Error while building dry payload");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("luca-categories")]
    public async Task<IActionResult> GetLucaCategories()
    {
        try
        {
            var resp = await _lucaService.ListStockCategoriesAsync(new Katana.Core.DTOs.LucaListStockCategoriesRequest());
            // Return raw JSON from Luca as-is so callers can inspect exact structure
            return Content(resp.GetRawText(), "application/json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "KozaDebugController => Failed to query Luca categories");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // Helper functions copied from LoaderService to normalize mapping keys for debug endpoint
    private static string NormalizeMappingKey(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var s = input.Trim().ToUpperInvariant();
        s = s.Replace('/', ' ').Replace('\\', ' ').Replace('-', ' ');
        s = RemoveDiacritics(s);
        while (s.Contains("  ")) s = s.Replace("  ", " ");
        return s;
    }

    private static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;
        var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (var ch in normalized)
        {
            var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                sb.Append(ch);
            }
        }
        return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }
}

public class KozaDebugRequest
{
    public string? SKU { get; set; }
    public string? Name { get; set; }
    public string? CategoryCode { get; set; }
    public long? OlcumBirimiId { get; set; }
    public decimal? SalesPrice { get; set; }
    public decimal? PurchasePrice { get; set; }
    public double? VatRate { get; set; }
}

 
