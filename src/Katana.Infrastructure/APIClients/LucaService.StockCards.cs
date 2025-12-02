using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Katana.Business.DTOs.Koza;
using Katana.Core.DTOs;
using Microsoft.Extensions.Logging;

namespace Katana.Infrastructure.APIClients;

/// <summary>
/// Koza Stok Kartı işlemleri
/// Endpoint'ler: ListeleStkKart.do, EkleStkWsKart.do
/// </summary>
public partial class LucaService
{
    /// <summary>
    /// Luca/Koza'ya stok kartı oluşturur (birden fazla deneme ile)
    /// </summary>
    public async Task<JsonElement> CreateStockCardAsync(LucaCreateStokKartiRequest request)
    {
        await EnsureAuthenticatedAsync();

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        // Ensure single-stock-card creation always targets the SKART endpoint.
        var endpoint = _settings.Endpoints.StockCardCreate;
        var jsonOptionsOriginal = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
        
        try
        {
            _logger.LogInformation("ATTEMPT 1: JSON with original property names");
            var json1 = JsonSerializer.Serialize(request, jsonOptionsOriginal);
            var content1 = CreateKozaContent(json1);
            using var req1 = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = content1 };
            ApplyManualSessionCookie(req1);
            var resp1 = await client.SendAsync(req1);
            var body1 = await ReadResponseContentAsync(resp1);
            await AppendRawLogAsync("CREATE_STOCK_ATTEMPT1", endpoint, json1, resp1.StatusCode, body1);
            try { await SaveHttpTrafficAsync("CREATE_STOCK_ATTEMPT1", req1, resp1); } catch { }

            if (resp1.IsSuccessStatusCode)
            {
                var parsed = ParseKozaOperationResponse(body1);
                if (parsed.IsSuccess)
                {
                    _logger.LogInformation("✅ SUCCESS with ATTEMPT 1");
                    return JsonSerializer.Deserialize<JsonElement>(body1);
                }
            }

            _logger.LogInformation("ATTEMPT 1 did not succeed: preview: {Preview}", body1?.Substring(0, Math.Min(200, body1?.Length ?? 0)));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ATTEMPT 1 exception");
        }
        try
        {
            _logger.LogInformation("ATTEMPT 2: Wrapped object (stkSkart)");
            var wrapped = new { stkSkart = request };
            var json2 = JsonSerializer.Serialize(wrapped, jsonOptionsOriginal);
            var content2 = CreateKozaContent(json2);
            using var req2 = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = content2 };
            ApplyManualSessionCookie(req2);
            var resp2 = await client.SendAsync(req2);
            var body2 = await ReadResponseContentAsync(resp2);
            await AppendRawLogAsync("CREATE_STOCK_ATTEMPT2", endpoint, json2, resp2.StatusCode, body2);
            try { await SaveHttpTrafficAsync("CREATE_STOCK_ATTEMPT2", req2, resp2); } catch { }

            if (resp2.IsSuccessStatusCode)
            {
                var parsed = ParseKozaOperationResponse(body2);
                if (parsed.IsSuccess)
                {
                    _logger.LogInformation("✅ SUCCESS with ATTEMPT 2");
                    return JsonSerializer.Deserialize<JsonElement>(body2);
                }
            }
            _logger.LogInformation("ATTEMPT 2 did not succeed: preview: {Preview}", body2?.Substring(0, Math.Min(200, body2?.Length ?? 0)));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ATTEMPT 2 exception");
        }
        try
        {
            _logger.LogInformation("ATTEMPT 3: Form-encoded key/value pairs");
            var fields = new Dictionary<string, string?>
            {
                ["kartAdi"] = request.KartAdi,
                ["kartKodu"] = request.KartKodu,
                ["kartTipi"] = request.KartTipi.ToString(),
                ["kartTuru"] = request.KartTuru.ToString(),
                ["baslangicTarihi"] = request.BaslangicTarihi,
                ["olcumBirimiId"] = request.OlcumBirimiId.ToString(),
                ["kategoriAgacKod"] = request.KategoriAgacKod,
                ["barkod"] = request.Barkod,
                ["kartAlisKdvOran"] = request.KartAlisKdvOran.ToString(CultureInfo.InvariantCulture),
                ["kartSatisKdvOran"] = request.KartSatisKdvOran.ToString(CultureInfo.InvariantCulture),
                ["satilabilirFlag"] = request.SatilabilirFlag.ToString(),
                ["satinAlinabilirFlag"] = request.SatinAlinabilirFlag.ToString(),
                ["lotNoFlag"] = request.LotNoFlag.ToString(),
                ["maliyetHesaplanacakFlag"] = request.MaliyetHesaplanacakFlag.ToString()
            };
            
            var formPairs = fields.Where(kv => kv.Value != null).Select(kv => Uri.EscapeDataString(kv.Key) + "=" + Uri.EscapeDataString(kv.Value!));
            var formBody = string.Join("&", formPairs);
            var formBytes = _encoding.GetBytes(formBody ?? string.Empty);
            var content3 = new ByteArrayContent(formBytes);
            content3.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded") { CharSet = _encoding.WebName };

            using var req3 = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = content3 };
            ApplyManualSessionCookie(req3);
            var resp3 = await client.SendAsync(req3);
            var body3 = await ReadResponseContentAsync(resp3);
            await AppendRawLogAsync("CREATE_STOCK_ATTEMPT3", endpoint, formBody ?? string.Empty, resp3.StatusCode, body3);
            try { await SaveHttpTrafficAsync("CREATE_STOCK_ATTEMPT3", req3, resp3); } catch { }
            if (resp3.IsSuccessStatusCode)
            {
                var parsed = ParseKozaOperationResponse(body3);
                if (parsed.IsSuccess)
                {
                    _logger.LogInformation("✅ SUCCESS with ATTEMPT 3");
                    return JsonSerializer.Deserialize<JsonElement>(body3);
                }
            }

            _logger.LogInformation("ATTEMPT 3 did not succeed: preview: {Preview}", body3?.Substring(0, Math.Min(200, body3?.Length ?? 0)));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ATTEMPT 3 exception");
        }
        
        throw new InvalidOperationException("All serialization attempts for CreateStockCardAsync failed. Need working example from Koza. Check logs for ATTEMPT* entries.");
    }

    /// <summary>
    /// Koza stok kartlarını listele (basitleştirilmiş)
    /// Frontend için sadece gerekli alanları döndürür
    /// </summary>
    public async Task<IReadOnlyList<KozaStokKartiDto>> ListStockCardsSimpleAsync(CancellationToken ct = default)
    {
        try {
            await EnsureAuthenticatedAsync();

            var req = new HttpRequestMessage(HttpMethod.Post, "ListeleStkKart.do")
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
            req.Headers.TryAddWithoutValidation("No-Paging", "true");

            ApplySessionCookie(req);
            ApplyManualSessionCookie(req);

            var client = _cookieHttpClient ?? _httpClient;
            var res = await client.SendAsync(req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);

            // NO_JSON hatası kontrolü
            if (body.TrimStart().StartsWith("<"))
            {
                _logger.LogError("Koza NO_JSON (HTML döndü) - ListeleStkKart.do. Body: {Body}", 
                    body.Length > 500 ? body.Substring(0, 500) : body);
                throw new InvalidOperationException("Koza NO_JSON hatası");
            }

            // Response parse
            var dto = JsonSerializer.Deserialize<KozaStokKartiListResponse>(body, _jsonOptions);
            
            if (dto?.Error == true)
            {
                _logger.LogError("Koza stok kartı listeleme hatası: {Message}", dto.Message);
                throw new InvalidOperationException(dto.Message ?? "Koza stok kartı listeleme hatası");
            }

            // Koza response alanı değişken: stokKartlari veya stkKartListesi
            var stoklar = dto?.StokKartlari ?? dto?.StkKartListesi ?? new List<KozaStokKartiDto>();
            
            _logger.LogInformation("Koza'dan {Count} stok kartı listelendi", stoklar.Count);
            return stoklar;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListStockCardsSimpleAsync failed");
            throw;
        }
    }

    /// <summary>
    /// Koza'da yeni stok kartı oluştur (basitleştirilmiş)
    /// Frontend'den gelen basit DTO'yu LucaCreateStokKartiRequest'e çevirir
    /// </summary>
    public async Task<KozaResult> CreateStockCardSimpleAsync(KozaCreateStokKartiRequest req, CancellationToken ct = default)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            // Basit DTO'yu tam DTO'ya çevir
            var fullRequest = MapToFullStokKartiRequest(req.StkKart);

            _logger.LogDebug("CreateStockCardSimpleAsync request: {Kod}", req.StkKart.KartKodu);

            // Mevcut CreateStockCardAsync metodunu kullan
            var response = await CreateStockCardAsync(fullRequest);

            // JsonElement'ten sonucu parse et
            try
            {
                if (response.ValueKind == JsonValueKind.Object)
                {
                    var errorProp = response.TryGetProperty("error", out var err) ? err : default;
                    var messageProp = response.TryGetProperty("message", out var msg) ? msg : default;

                    if (errorProp.ValueKind == JsonValueKind.True)
                    {
                        return new KozaResult
                        {
                            Success = false,
                            Message = messageProp.ValueKind == JsonValueKind.String 
                                ? messageProp.GetString() 
                                : "Koza stok kartı oluşturma hatası"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Response parse failed, assuming success");
            }

            _logger.LogInformation("Stok kartı başarıyla oluşturuldu: {Kod} - {Ad}", 
                req.StkKart.KartKodu, req.StkKart.KartAdi);
            
            return new KozaResult { Success = true, Message = "OK" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateStockCardSimpleAsync failed for: {Kod}", req.StkKart?.KartKodu ?? "unknown");
            return new KozaResult { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// Basit DTO'yu tam LucaCreateStokKartiRequest'e çevir
    /// </summary>
    private LucaCreateStokKartiRequest MapToFullStokKartiRequest(KozaStokKartiDto simple)
    {
        return new LucaCreateStokKartiRequest
        {
            KartKodu = simple.KartKodu,
            KartAdi = simple.KartAdi,
            KartTuru = simple.KartTuru,
            KartTipi = simple.KartTipi,
            OlcumBirimiId = simple.OlcumBirimiId,
            KategoriAgacKod = simple.KategoriAgacKod,
            KartAlisKdvOran = simple.KartAlisKdvOran,
            KartSatisKdvOran = simple.KartSatisKdvOran,
            UzunAdi = simple.UzunAdi ?? simple.KartAdi,
            Barkod = simple.Barkod ?? string.Empty,
            BaslangicTarihi = simple.BaslangicTarihi ?? DateTime.UtcNow.ToString("yyyy-MM-dd"),
            MinStokKontrol = simple.MinStokKontrol,
            MinStokMiktari = simple.MinStokMiktari,
            MaxStokKontrol = simple.MaxStokKontrol,
            MaxStokMiktari = simple.MaxStokMiktari,
            SatilabilirFlag = simple.SatilabilirFlag,
            SatinAlinabilirFlag = simple.SatinAlinabilirFlag,
            MaliyetHesaplanacakFlag = simple.MaliyetHesaplanacakFlag,
            
            // Varsayılan değerler
            KartToptanAlisKdvOran = simple.KartAlisKdvOran,
            KartToptanSatisKdvOran = simple.KartSatisKdvOran,
            AlisIskontoOran1 = 0,
            SatisIskontoOran1 = 0,
            RafOmru = 0,
            GarantiSuresi = 0,
            GtipKodu = string.Empty,
            AlisTevkifatOran = string.Empty,
            AlisTevkifatKod = 0,
            SatisTevkifatOran = string.Empty,
            SatisTevkifatKod = 0,
            IhracatKategoriNo = string.Empty,
            UtsVeriAktarimiFlag = 0,
            BagDerecesi = 0
        };
    }
}
