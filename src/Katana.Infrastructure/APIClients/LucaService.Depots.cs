using System.Text;
using System.Text.Json;
using Katana.Business.DTOs.Koza;
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
            req.Headers.TryAddWithoutValidation("No-Paging", "true");

            // Session cookie'yi uygula
            ApplySessionCookie(req);
            ApplyManualSessionCookie(req);

            var client = _cookieHttpClient ?? _httpClient;
            var res = await client.SendAsync(req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);

            // Koza bazen HTML döndürebilir (NO_JSON hatası)
            if (body.TrimStart().StartsWith("<"))
            {
                _logger.LogError("Koza NO_JSON (HTML döndü) - ListeleStkDepo.do. Auth/şube/cookie kırık olabilir. Body: {Body}", 
                    body.Length > 500 ? body.Substring(0, 500) : body);
                throw new InvalidOperationException("Koza NO_JSON (HTML döndü). Auth/şube/cookie kırık olabilir.");
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
    /// Koza'nın beklediği payload: { stkDepo: { kod, tanim, kategoriKod, ulke, il, ilce, adresSerbest } }
    /// </summary>
    public async Task<KozaResult> CreateDepotAsync(KozaCreateDepotRequest req, CancellationToken ct = default)
    {
        try
        {
            // Cookie/session auth sağla
            await EnsureAuthenticatedAsync();

            var json = JsonSerializer.Serialize(req, _jsonOptions);
            
            _logger.LogDebug("CreateDepotAsync request: {Json}", json);

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var httpReq = new HttpRequestMessage(HttpMethod.Post, "EkleStkWsDepo.do")
            {
                Content = content
            };

            // Session cookie'yi uygula
            ApplySessionCookie(httpReq);
            ApplyManualSessionCookie(httpReq);

            var client = _cookieHttpClient ?? _httpClient;
            var res = await client.SendAsync(httpReq, ct);
            var body = await res.Content.ReadAsStringAsync(ct);

            _logger.LogDebug("CreateDepotAsync response status: {Status}, body: {Body}", 
                res.StatusCode, body.Length > 500 ? body.Substring(0, 500) : body);

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
                return new KozaResult 
                { 
                    Success = false, 
                    Message = $"HTTP {res.StatusCode}: {body}" 
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
