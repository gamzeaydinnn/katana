using System.Text;
using System.Text.Json;
using System.Linq;
using Katana.Core.DTOs.Koza;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Data.Configuration;
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
        return await ListMusteriCarilerAsync(null, null, "between", ct);
    }

    /// <summary>
    /// Koza Müşteri Carilerini Listele (ListeleFinMusteri.do) - Filtreleme ile
    /// </summary>
    public async Task<IReadOnlyList<KozaCariDto>> ListMusteriCarilerAsync(
        string? kodBas, 
        string? kodBit, 
        string kodOp = "between",
        CancellationToken ct = default)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            object payload;
            if (!string.IsNullOrEmpty(kodBas) && !string.IsNullOrEmpty(kodBit))
            {
                // Filtreleme ile
                payload = new
                {
                    finMusteri = new
                    {
                        gnlFinansalNesne = new
                        {
                            kodBas = kodBas,
                            kodBit = kodBit,
                            kodOp = kodOp
                        }
                    }
                };
            }
            else
            {
                // Filtreleme olmadan (tümü)
                payload = new { };
            }

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var req = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.CustomerList ?? "/api/finMusteri/listele")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
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

            // FIX: finMusteriListesi alanını kontrol et, yoksa list alanına bak
            var müsteriler = dto?.FinMusteriListesi ?? dto?.List ?? new List<KozaCariDto>();
            
            _logger.LogInformation("Koza'dan {Count} müşteri cari listelendi (Filtre: {KodBas}-{KodBit}), Kaynak: {Source}", 
                müsteriler.Count, kodBas ?? "Tümü", kodBit ?? "Tümü", 
                dto?.FinMusteriListesi != null ? "finMusteriListesi" : (dto?.List != null ? "list" : "empty"));
            return müsteriler;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListMusteriCarilerAsync failed");
            throw;
        }
    }

    /// <summary>
    /// Luca müşteri carilerini sade liste DTO'suna map ederek döndürür
    /// </summary>
    public async Task<IReadOnlyList<KozaCustomerListItemDto>> ListMusteriCustomerItemsAsync(CancellationToken ct = default)
    {
        return await ListMusteriCustomerItemsAsync(null, null, "between", ct);
    }

    /// <summary>
    /// Luca müşteri carilerini sade liste DTO'suna map ederek döndürür (filtrelenmiş)
    /// </summary>
    public async Task<IReadOnlyList<KozaCustomerListItemDto>> ListMusteriCustomerItemsAsync(
        string? kodBas,
        string? kodBit,
        string kodOp = "between",
        CancellationToken ct = default)
    {
        var cariler = await ListMusteriCarilerAsync(kodBas, kodBit, kodOp, ct);
        if (cariler == null || cariler.Count == 0)
        {
            return new List<KozaCustomerListItemDto>();
        }

        return cariler
            .Select(KozaCustomerListItemDto.FromKozaCari)
            .ToList();
    }

    public KozaMusteriEkleRequest BuildKozaMusteriEkleRequest(Customer customer)
    {
        return KozaCustomerRequestFactory.Build(customer, _settings);
    }

    /// <summary>
    /// Koza Müşteri Kartı Ekleme (EkleFinMusteriWS.do) - Dokümantasyona tam uyumlu
    /// </summary>
    public async Task<KozaResult> CreateMusteriCariAsync(KozaMusteriEkleRequest request, CancellationToken ct = default)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            // Luca Koza RPC tarzı bekliyor: düz body (finMusteri sarmalamasız)
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            _logger.LogInformation("SEND_CUSTOMER payload={Json}", json);

            var response = await SendKozaCustomerRequestAsync(() =>
            {
                var endpoint = ResolveCustomerCreateEndpoint();
                var httpReq = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                ApplySessionCookie(httpReq);
                ApplyManualSessionCookie(httpReq);
                return httpReq;
            }, ct);
            var body = response.Body;

            _logger.LogDebug("CreateMusteriCariAsync response status: {Status}, body: {Body}", 
                response.StatusCode, body.Length > 500 ? body.Substring(0, 500) : body);

            var trimmed = body.TrimStart();
            if (trimmed.StartsWith("<") || (!trimmed.StartsWith("{") && !trimmed.StartsWith("[")))
            {
                _logger.LogError("Koza NO_JSON (HTML/non-JSON döndü) - CreateMusteriCariAsync");
                return new KozaResult 
                { 
                    Success = false, 
                    Message = "Koza NO_JSON (HTML veya non-JSON döndü). Auth/şube/cookie kırık olabilir." 
                };
            }

            if (!response.IsSuccessStatusCode)
            {
                return new KozaResult 
                { 
                    Success = false, 
                    Message = $"HTTP {response.StatusCode}: {body}" 
                };
            }

            // Response'dan finansalNesneId ve cariKodu almaya çalış
            long? finansalNesneId = null;
            string? cariKodu = null;
            try
            {
                var respJson = JsonSerializer.Deserialize<JsonElement>(body);
                if (respJson.TryGetProperty("finansalNesneId", out var fnId))
                {
                    finansalNesneId = fnId.GetInt64();
                }
                else if (respJson.TryGetProperty("finMusteri", out var fm))
                {
                    if (fm.TryGetProperty("finansalNesneId", out var fmId))
                        finansalNesneId = fmId.GetInt64();
                    if (fm.TryGetProperty("kod", out var kod))
                        cariKodu = kod.GetString();
                }
                
                if (respJson.TryGetProperty("kod", out var kod2))
                    cariKodu = kod2.GetString();
            }
            catch { /* Ignore parse errors */ }

            _logger.LogInformation("Müşteri cari başarıyla oluşturuldu: {CariKodu} (FinansalNesneId: {Id})", 
                cariKodu ?? request.KartKod ?? "N/A", finansalNesneId);
            
            return new KozaResult 
            { 
                Success = true, 
                Message = "OK",
                Data = new { CariKodu = cariKodu ?? request.KartKod, FinansalNesneId = finansalNesneId }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateMusteriCariAsync failed for kartKod: {KartKod}", request.KartKod);
            return new KozaResult { Success = false, Message = ex.Message };
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

            if (_settings.UsePostmanCustomerFormat)
            {
                var mapped = new Customer
                {
                    LucaCode = customer.Code,
                    Title = customer.Name ?? customer.Code,
                    Address = customer.Address,
                    Email = customer.Email,
                    Phone = customer.Phone,
                    GroupCode = customer.KatanaCustomerId,
                    Type = 1
                };

                var postmanKozaRequest = BuildKozaMusteriEkleRequest(mapped);
                var result = await CreateMusteriCariAsync(postmanKozaRequest, ct);
                return result;
            }

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
            var cariKodu = customer.Code;

            // FIX: Postman collection'daki format kullan - düz obje, finMusteri sarmalayıcısı YOK
            var kozaRequest = new KozaMusteriEkleRequest
            {
                Tip = 1,
                CariTipId = 5,
                KartKod = cariKodu,
                Tanim = customer.Name,
                KisaAd = customer.Name?.Length > 30 ? customer.Name.Substring(0, 30) : customer.Name,
                YasalUnvan = customer.Name,
                ParaBirimKod = "TRY",
                AdresTipId = !string.IsNullOrWhiteSpace(customer.Address) ? 9 : null,
                AdresSerbest = customer.Address,
                Il = null,
                Ilce = null,
                Ulke = null,
                IletisimTipId = !string.IsNullOrWhiteSpace(customer.Email) ? 5 : null,
                IletisimTanim = customer.Email ?? customer.Phone
            };

            _logger.LogDebug("EnsureCustomerCariAsync delegating create for {Code} via Koza RPC endpoint", cariKodu);
            return await CreateMusteriCariAsync(kozaRequest, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EnsureCustomerCariAsync failed for customer: {Code} - {Name}", 
                customer.Code, customer.Name);
            return new KozaResult { Success = false, Message = ex.Message };
        }
    }

    private string ResolveCustomerCreateEndpoint()
    {
        var configured = _settings.Endpoints.CustomerCreate;
        if (!string.IsNullOrWhiteSpace(configured) &&
            !configured.Contains("/api/finMusteri/kaydet", StringComparison.OrdinalIgnoreCase))
        {
            return configured;
        }

        const string kozaEndpoint = "EkleFinMusteriWS.do";
        if (!string.Equals(configured, kozaEndpoint, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("CustomerCreate endpoint misconfigured as {Configured}; forcing Koza RPC endpoint {Endpoint}", 
                configured ?? "(null)", kozaEndpoint);
        }

        _settings.Endpoints.CustomerCreate = kozaEndpoint;
        return kozaEndpoint;
    }
}

public static class KozaCustomerRequestFactory
{
    public static KozaMusteriEkleRequest Build(Customer customer, LucaApiSettings settings)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(settings);

        var name = string.IsNullOrWhiteSpace(customer.Title)
            ? customer.LucaCode ?? customer.GenerateLucaCode()
            : customer.Title.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            name = "Musteri";
        }

        var kartKod = string.IsNullOrWhiteSpace(customer.LucaCode)
            ? customer.GenerateLucaCode()
            : customer.LucaCode.Trim();

        var adresSerbest = string.IsNullOrWhiteSpace(customer.Address) ? null : customer.Address.Trim();
        var il = string.IsNullOrWhiteSpace(customer.City) ? null : customer.City.Trim();
        var ilce = string.IsNullOrWhiteSpace(customer.District) ? null : customer.District.Trim();
        var ulke = string.IsNullOrWhiteSpace(customer.Country) ? null : customer.Country.Trim();

        var iletisim = !string.IsNullOrWhiteSpace(customer.Email)
            ? customer.Email.Trim()
            : (!string.IsNullOrWhiteSpace(customer.Phone) ? customer.Phone.Trim() : null);

        var kategoriKod = ResolveCustomerKategoriKod(customer, settings);

        return new KozaMusteriEkleRequest
        {
            Tip = customer.Type > 0 ? customer.Type : 1,
            CariTipId = 5,
            KartKod = kartKod,
            Tanim = name,
            KisaAd = SafeTruncate(name, 30),
            YasalUnvan = name,
            ParaBirimKod = "TRY",
            KategoriKod = kategoriKod,
            AdresTipId = string.IsNullOrWhiteSpace(adresSerbest) ? null : 9,
            AdresSerbest = adresSerbest,
            Il = il,
            Ilce = ilce,
            Ulke = ulke,
            IletisimTipId = string.IsNullOrWhiteSpace(iletisim) ? null : 5,
            IletisimTanim = iletisim
        };
    }

    private static string? ResolveCustomerKategoriKod(Customer customer, LucaApiSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(customer.GroupCode) && settings.CategoryMapping != null)
        {
            var key = customer.GroupCode.Trim();
            if (settings.CategoryMapping.TryGetValue(key, out var mapped) && !string.IsNullOrWhiteSpace(mapped))
            {
                return mapped;
            }

            var altMatch = settings.CategoryMapping
                .FirstOrDefault(kv => string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(altMatch.Value))
            {
                return altMatch.Value;
            }
        }

        if (!string.IsNullOrWhiteSpace(settings.DefaultKategoriKodu))
        {
            return settings.DefaultKategoriKodu;
        }

        return null;
    }

    private static string? SafeTruncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }
}
