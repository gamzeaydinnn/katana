using System.Text;
using System.Text.Json;
using Katana.Business.DTOs.Koza;
using Katana.Core.DTOs;
using Microsoft.Extensions.Logging;

namespace Katana.Infrastructure.APIClients;

/// <summary>
/// Koza Cari (Müşteri/Tedarikçi) işlemleri
/// Endpoint'ler: ListeleWSGnlSsAdres.do, GetirFinCalismaKosul.do, 
///               ListeleFinFinansalNesneYetkili.do, EkleFinCariHareketBaslikWS.do,
///               ListeleFinTedarikci.do, EkleFinTedarikciWS.do
/// </summary>
public partial class LucaService
{
    /// <summary>
    /// Cari Adres Listesi (ListeleWSGnlSsAdres.do)
    /// </summary>
    public async Task<JsonElement> ListCariAddressesAsync(long finansalNesneId, CancellationToken ct = default)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            var payload = new KozaCariAdresListRequest { FinansalNesneId = finansalNesneId };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            
            var req = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.CustomerAddresses)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            req.Headers.TryAddWithoutValidation("No-Paging", "true");

            ApplySessionCookie(req);
            ApplyManualSessionCookie(req);

            var client = _cookieHttpClient ?? _httpClient;
            var res = await client.SendAsync(req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);

            _logger.LogDebug("ListCariAddressesAsync response: {Body}", 
                body.Length > 500 ? body.Substring(0, 500) : body);

            // HTML döndü mü kontrol et (NO_JSON)
            if (body.TrimStart().StartsWith("<"))
            {
                _logger.LogError("Koza NO_JSON (HTML döndü) - ListCariAddressesAsync. Body preview: {Preview}", 
                    body.Length > 300 ? body.Substring(0, 300) : body);
                return JsonDocument.Parse("{\"error\":true,\"message\":\"NO_JSON - HTML döndü\"}").RootElement;
            }

            return JsonSerializer.Deserialize<JsonElement>(body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListCariAddressesAsync failed for finansalNesneId: {Id}", finansalNesneId);
            throw;
        }
    }

    /// <summary>
    /// Cari Çalışma Koşulları (GetirFinCalismaKosul.do)
    /// </summary>
    public async Task<JsonElement> GetCariCalismaKosulAsync(long calismaKosulId, CancellationToken ct = default)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            var payload = new KozaCalismaKosulRequest { CalismaKosulId = calismaKosulId };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            
            var req = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.CustomerWorkingConditions)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            ApplySessionCookie(req);
            ApplyManualSessionCookie(req);

            var client = _cookieHttpClient ?? _httpClient;
            var res = await client.SendAsync(req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);

            _logger.LogDebug("GetCariCalismaKosulAsync response: {Body}", 
                body.Length > 500 ? body.Substring(0, 500) : body);

            if (body.TrimStart().StartsWith("<"))
            {
                _logger.LogError("Koza NO_JSON (HTML döndü) - GetCariCalismaKosulAsync");
                return JsonDocument.Parse("{\"error\":true,\"message\":\"NO_JSON - HTML döndü\"}").RootElement;
            }

            return JsonSerializer.Deserialize<JsonElement>(body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetCariCalismaKosulAsync failed for calismaKosulId: {Id}", calismaKosulId);
            throw;
        }
    }

    /// <summary>
    /// Cari Yetkili Kişiler (ListeleFinFinansalNesneYetkili.do)
    /// Body: { "gnlFinansalNesne": { "finansalNesneId": X } }
    /// </summary>
    public async Task<JsonElement> ListCariYetkililerAsync(long finansalNesneId, CancellationToken ct = default)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            var payload = new KozaCariYetkiliListRequest
            {
                GnlFinansalNesne = new KozaFinansalNesneFilter { FinansalNesneId = finansalNesneId }
            };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            
            var req = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.CustomerAuthorizedPersons)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            req.Headers.TryAddWithoutValidation("No-Paging", "true");

            ApplySessionCookie(req);
            ApplyManualSessionCookie(req);

            var client = _cookieHttpClient ?? _httpClient;
            var res = await client.SendAsync(req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);

            _logger.LogDebug("ListCariYetkililerAsync response: {Body}", 
                body.Length > 500 ? body.Substring(0, 500) : body);

            if (body.TrimStart().StartsWith("<"))
            {
                _logger.LogError("Koza NO_JSON (HTML döndü) - ListCariYetkililerAsync");
                return JsonDocument.Parse("{\"error\":true,\"message\":\"NO_JSON - HTML döndü\"}").RootElement;
            }

            return JsonSerializer.Deserialize<JsonElement>(body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListCariYetkililerAsync failed for finansalNesneId: {Id}", finansalNesneId);
            throw;
        }
    }

    /// <summary>
    /// Cari Hareket Ekleme (EkleFinCariHareketBaslikWS.do)
    /// </summary>
    public async Task<KozaResult> CreateCariHareketAsync(KozaCariHareketRequest req, CancellationToken ct = default)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            var json = JsonSerializer.Serialize(req, _jsonOptions);
            
            _logger.LogDebug("CreateCariHareketAsync request: {Json}", json);

            var httpReq = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.CustomerTransaction)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            ApplySessionCookie(httpReq);
            ApplyManualSessionCookie(httpReq);

            var client = _cookieHttpClient ?? _httpClient;
            var res = await client.SendAsync(httpReq, ct);
            var body = await res.Content.ReadAsStringAsync(ct);

            _logger.LogDebug("CreateCariHareketAsync response status: {Status}, body: {Body}", 
                res.StatusCode, body.Length > 500 ? body.Substring(0, 500) : body);

            if (body.TrimStart().StartsWith("<"))
            {
                _logger.LogError("Koza NO_JSON (HTML döndü) - CreateCariHareketAsync");
                return new KozaResult 
                { 
                    Success = false, 
                    Message = "Koza NO_JSON (HTML döndü). Auth/şube/cookie kırık olabilir." 
                };
            }

            if (!res.IsSuccessStatusCode)
            {
                return new KozaResult 
                { 
                    Success = false, 
                    Message = $"HTTP {res.StatusCode}: {body}" 
                };
            }

            _logger.LogInformation("Cari hareket başarıyla oluşturuldu: CariKodu={CariKodu}, Tür={Tur}", 
                req.CariKodu, req.CariTuru == 1 ? "Müşteri" : "Tedarikçi");
            
            return new KozaResult { Success = true, Message = "OK" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateCariHareketAsync failed for cariKodu: {CariKodu}", req.CariKodu);
            return new KozaResult { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// Koza Müşteri Carilerini Listele (ListeleFinMusteri.do)
    /// </summary>
    public async Task<IReadOnlyList<KozaCariDto>> ListMusteriCarilerAsync(CancellationToken ct = default)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            var req = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.CustomerList ?? "/api/finMusteri/listele")
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
            req.Headers.TryAddWithoutValidation("No-Paging", "true");

            ApplySessionCookie(req);
            ApplyManualSessionCookie(req);

            var client = _cookieHttpClient ?? _httpClient;
            var res = await client.SendAsync(req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);

            if (body.TrimStart().StartsWith("<"))
            {
                _logger.LogError("Koza NO_JSON (HTML döndü) - ListMusteriCarilerAsync");
                throw new InvalidOperationException("Koza NO_JSON (HTML döndü). Auth/şube/cookie kırık olabilir.");
            }

            var dto = JsonSerializer.Deserialize<KozaCariListResponse>(body, _jsonOptions);
            
            if (dto?.Error == true)
            {
                _logger.LogError("Koza müşteri listeleme hatası: {Message}", dto.Message ?? "Unknown error");
                throw new InvalidOperationException(dto.Message ?? "Koza müşteri listeleme hatası");
            }

            var müsteriler = dto?.FinMusteriListesi ?? new List<KozaCariDto>();
            
            _logger.LogInformation("Koza'dan {Count} müşteri cari listelendi", müsteriler.Count);
            return müsteriler;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListMusteriCarilerAsync failed");
            throw;
        }
    }

    /// <summary>
    /// Katana Location'u Koza Depo olarak senkronize eder
    /// Yoksa oluşturur, varsa atlar
    /// </summary>
    public async Task<KozaResult> EnsureDepotAsync(KatanaLocationToDepoDto depot, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("EnsureDepotAsync: Processing depot {Code} - {Name}", depot.Code, depot.Name);
            
            // Mevcut depoları listele ve kontrol et
            var existingDepots = await ListDepotsAsync(ct);
            var existing = existingDepots.FirstOrDefault(d => 
                string.Equals(d.Kod, depot.Code, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                _logger.LogInformation("Depo zaten mevcut: {Code} (Id: {Id})", depot.Code, existing.DepoId);
                return new KozaResult 
                { 
                    Success = true, 
                    Message = "ALREADY_EXISTS",
                    Data = new { DepoKodu = existing.Kod, DepoId = existing.DepoId }
                };
            }

            // Yeni depo oluştur
            var payload = new
            {
                depo = new
                {
                    kod = depot.Code,
                    tanim = depot.Name,
                    adres = depot.Address ?? "",
                    aktif = true
                }
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            _logger.LogDebug("EnsureDepotAsync creating: {Json}", json);

            var response = await _httpClient.PostAsync(
                "/api/depo/kaydet",
                new StringContent(json, Encoding.UTF8, "application/json"),
                ct);

            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogDebug("EnsureDepotAsync response: {Status}, {Body}", response.StatusCode, body?.Substring(0, Math.Min(500, body?.Length ?? 0)));

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Depo oluşturulamadı: {Code} - Status: {Status}, Body: {Body}", 
                    depot.Code, response.StatusCode, body);
                return new KozaResult { Success = false, Message = $"HTTP {response.StatusCode}: {body}" };
            }

            long? depoId = null;
            try
            {
                var respJson = JsonSerializer.Deserialize<JsonElement>(body);
                if (respJson.TryGetProperty("depoId", out var did))
                {
                    depoId = did.GetInt64();
                }
                else if (respJson.TryGetProperty("depo", out var d) && 
                         d.TryGetProperty("depoId", out var dId))
                {
                    depoId = dId.GetInt64();
                }
            }
            catch { /* Ignore parse errors */ }

            _logger.LogInformation("Depo başarıyla oluşturuldu: {Code} - {Name} (DepoId: {Id})", 
                depot.Code, depot.Name, depoId);
            
            return new KozaResult 
            { 
                Success = true, 
                Message = "OK",
                Data = new { DepoKodu = depot.Code, DepoId = depoId }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EnsureDepotAsync failed for depot: {Code} - {Name}", depot.Code, depot.Name);
            return new KozaResult { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// Katana Customer'ı Luca Müşteri Cari olarak senkronize eder
    /// Yoksa oluşturur, varsa atlar
    /// </summary>
    public async Task<KozaResult> EnsureCustomerCariAsync(KatanaCustomerToCariDto customer, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("EnsureCustomerCariAsync: Processing customer {Code} - {Name}", customer.Code, customer.Name);

            // Mevcut müşteri carilerini listele ve kontrol et
            var existingCustomers = await ListMusteriCarilerAsync(ct);
            var existing = existingCustomers.FirstOrDefault(c => 
                string.Equals(c.Kod, customer.Code, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                _logger.LogInformation("Müşteri cari zaten mevcut: {Code} (FinansalNesneId: {Id})", 
                    customer.Code, existing.FinansalNesneId);
                return new KozaResult 
                { 
                    Success = true, 
                    Message = "ALREADY_EXISTS",
                    Data = new { CariKodu = existing.Kod, FinansalNesneId = existing.FinansalNesneId }
                };
            }

            // Koza'da cari kodu oluştur (TED_ prefix yerine MUS_ prefix)
            var cariKodu = $"MUS_{customer.Code}";

            // Yeni müşteri cari oluştur
            var payload = new
            {
                finMusteri = new
                {
                    kod = cariKodu,
                    tanim = customer.Name,
                    kisaAd = customer.Name?.Length > 30 ? customer.Name.Substring(0, 30) : customer.Name,
                    yasalUnvan = customer.Name,
                    vergiNo = customer.TaxNumber ?? "",
                    vergiDairesi = customer.TaxOffice ?? "",
                    email = customer.Email ?? "",
                    telefon = customer.Phone ?? "",
                    adres = customer.Address ?? "",
                    aktif = true,
                    paraBirimKod = "TRY"
                }
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            _logger.LogDebug("EnsureCustomerCariAsync creating: {Json}", json);

            var response = await _httpClient.PostAsync(
                "/api/finMusteri/kaydet",
                new StringContent(json, Encoding.UTF8, "application/json"),
                ct);

            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogDebug("EnsureCustomerCariAsync response: {Status}, {Body}", 
                response.StatusCode, body?.Substring(0, Math.Min(500, body?.Length ?? 0)));

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Müşteri cari oluşturulamadı: {Code} - Status: {Status}, Body: {Body}", 
                    customer.Code, response.StatusCode, body);
                return new KozaResult { Success = false, Message = $"HTTP {response.StatusCode}: {body}" };
            }

            long? finansalNesneId = null;
            try
            {
                var respJson = JsonSerializer.Deserialize<JsonElement>(body);
                if (respJson.TryGetProperty("finansalNesneId", out var fnId))
                {
                    finansalNesneId = fnId.GetInt64();
                }
                else if (respJson.TryGetProperty("finMusteri", out var fm) && 
                         fm.TryGetProperty("finansalNesneId", out var fmId))
                {
                    finansalNesneId = fmId.GetInt64();
                }
            }
            catch { /* Ignore parse errors */ }

            _logger.LogInformation("Müşteri cari başarıyla oluşturuldu: {CariKodu} - {Name} (FinansalNesneId: {Id})", 
                cariKodu, customer.Name, finansalNesneId);
            
            return new KozaResult 
            { 
                Success = true, 
                Message = "OK",
                Data = new { CariKodu = cariKodu, FinansalNesneId = finansalNesneId }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EnsureCustomerCariAsync failed for customer: {Code} - {Name}", 
                customer.Code, customer.Name);
            return new KozaResult { Success = false, Message = ex.Message };
        }
    }
}
