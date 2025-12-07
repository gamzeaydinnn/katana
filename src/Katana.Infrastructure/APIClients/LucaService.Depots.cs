using System.Text;
using System.Text.Json;
using Katana.Core.DTOs.Koza;
using Microsoft.Extensions.Logging;

namespace Katana.Infrastructure.APIClients;

/// <summary>
/// Koza Depo (Depot) işlemleri
/// Endpoint'ler: ListeleStkDepo.do, EkleStkWsDepo.do
/// </summary>
public partial class LucaService
{
    /// <summary>
    /// Koza depolarını listele (ListeleStkDepo.do)
    /// Boş body {} ile tüm depoları getirir
    /// UseNoPagingHeader: appsettings'den kontrol edilir (performans için)
    /// </summary>
    public async Task<IReadOnlyList<KozaDepoDto>> ListDepotsAsync(CancellationToken ct = default)
    {
        try
        {
            // Cookie/session auth sağla
            await EnsureAuthenticatedAsync();

            var req = new HttpRequestMessage(HttpMethod.Post, "ListeleStkDepo.do")
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
            
            // No-Paging header'ı sadece konfig'de aktifse ekle
            if (_settings.UseNoPagingHeader)
            {
                req.Headers.TryAddWithoutValidation("No-Paging", "true");
                _logger.LogDebug("No-Paging header eklendi (appsettings: UseNoPagingHeader=true)");
            }

            // Session cookie'yi uygula
            ApplySessionCookie(req);
            ApplyManualSessionCookie(req);

            var client = _cookieHttpClient ?? _httpClient;
            var res = await client.SendAsync(req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);

            // Koza bazen HTML döndürebilir (NO_JSON hatası)
            if (body.TrimStart().StartsWith("<"))
            {
                var snippet = body.Length > 200 ? body.Substring(0, 200) : body;
                _logger.LogError("Koza NO_JSON (HTML döndü) - ListeleStkDepo.do. Auth/şube/cookie kırık olabilir. Body snippet: {Snippet}", snippet);
                // Exception yerine boş liste dön - controller 502 ile handle edebilir
                return new List<KozaDepoDto>();
            }

            // Response'u parse et
            var dto = JsonSerializer.Deserialize<KozaDepoListResponse>(body, _jsonOptions);
            
            if (dto?.Error == true)
            {
                _logger.LogError("Koza depo listeleme hatası: {Message}", dto.Message ?? "Unknown error");
                throw new InvalidOperationException(dto.Message ?? "Koza depo listeleme hatası");
            }

            // Koza response alanı değişken olabiliyor: depolar veya stkDepoListesi
            var depolar = dto?.Depolar ?? dto?.StkDepoListesi ?? new List<KozaDepoDto>();
            
            _logger.LogInformation("Koza'dan {Count} depo listelendi", depolar.Count);
            return depolar;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListDepotsAsync failed");
            throw;
        }
    }

    /// <summary>
    /// Koza'da yeni depo oluştur (EkleStkWsDepo.do)
    /// Content-Type: application/json
    /// </summary>
    public async Task<KozaResult> CreateDepotAsync(KozaCreateDepotRequest req, CancellationToken ct = default)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            // Düz JSON - depoKategoriAgacId dahil
            var payload = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                { "kod", req.StkDepo.Kod },
                { "tanim", req.StkDepo.Tanim },
                { "kategoriKod", req.StkDepo.KategoriKod }
            };
            
            // Depo kategori ağacı ID (Luca'daki MERKEZ DEPO için 11356)
            if (req.StkDepo.DepoKategoriAgacId.HasValue)
                payload["depoKategoriAgacId"] = req.StkDepo.DepoKategoriAgacId.Value;
            if (!string.IsNullOrWhiteSpace(req.StkDepo.SisDepoKategoriAgacKodu))
                payload["sisDepoKategoriAgacKodu"] = req.StkDepo.SisDepoKategoriAgacKodu;
            
            if (!string.IsNullOrWhiteSpace(req.StkDepo.Ulke))
                payload["ulke"] = req.StkDepo.Ulke;
            if (!string.IsNullOrWhiteSpace(req.StkDepo.Il))
                payload["il"] = req.StkDepo.Il;
            if (!string.IsNullOrWhiteSpace(req.StkDepo.Ilce))
                payload["ilce"] = req.StkDepo.Ilce;
            if (!string.IsNullOrWhiteSpace(req.StkDepo.AdresSerbest))
                payload["adresSerbest"] = req.StkDepo.AdresSerbest;

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(payload, _jsonOptions),
                Encoding.UTF8,
                "application/json");

            _logger.LogInformation("CreateDepotAsync - JSON payload: {Json}", 
                JsonSerializer.Serialize(payload, _jsonOptions));

            var httpReq = new HttpRequestMessage(HttpMethod.Post, "EkleStkWsDepo.do")
            {
                Content = jsonContent
            };

            ApplySessionCookie(httpReq);
            ApplyManualSessionCookie(httpReq);

            var client = _cookieHttpClient ?? _httpClient;
            var res = await client.SendAsync(httpReq, ct);
            var body = await res.Content.ReadAsStringAsync(ct);

            // Response body'yi logla (ilk 500 karakter)
            var bodySnippet = body.Length > 500 ? body.Substring(0, 500) : body;
            _logger.LogInformation("CreateDepotAsync response - Status: {Status}, Body: {Body}", 
                res.StatusCode, bodySnippet);

            // HTML döndü mü kontrol et (NO_JSON)
            if (body.TrimStart().StartsWith("<"))
            {
                _logger.LogError("Koza NO_JSON (HTML döndü) - EkleStkWsDepo.do. Auth/şube/cookie kırık olabilir.");
                return new KozaResult 
                { 
                    Success = false, 
                    Message = "Koza NO_JSON (HTML döndü). Auth/şube/cookie kırık olabilir." 
                };
            }

            // Response parse et
            try
            {
                var parsed = JsonSerializer.Deserialize<KozaDepoListResponse>(body, _jsonOptions);
                if (parsed?.Error == true)
                {
                    _logger.LogError("Koza depo oluşturma hatası: {Message}", parsed.Message);
                    return new KozaResult 
                    { 
                        Success = false, 
                        Message = parsed.Message ?? "Koza depo oluşturma hatası" 
                    };
                }
            }
            catch (JsonException ex)
            {
                _logger.LogDebug(ex, "CreateDepotAsync response parse failed (non-fatal), assuming success on HTTP OK");
            }

            if (!res.IsSuccessStatusCode)
            {
                // Log body for debugging 400 errors
                _logger.LogError("Koza CreateDepot Error ({StatusCode}): {Body}", res.StatusCode, bodySnippet);
                
                return new KozaResult 
                { 
                    Success = false, 
                    Message = $"HTTP {res.StatusCode}: {bodySnippet}" 
                };
            }

            _logger.LogInformation("Depo başarıyla oluşturuldu: {Kod} - {Tanim}", req.StkDepo.Kod, req.StkDepo.Tanim);
            
            return new KozaResult 
            { 
                Success = true, 
                Message = "OK" 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateDepotAsync failed for depot: {Kod}", req.StkDepo?.Kod ?? "unknown");
            return new KozaResult 
            { 
                Success = false, 
                Message = ex.Message 
            };
        }
    }
}
