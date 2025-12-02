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
    /// Koza Tedarikçi Carilerini Listele (ListeleFinTedarikci.do)
    /// </summary>
    public async Task<IReadOnlyList<KozaCariDto>> ListTedarikciCarilerAsync(CancellationToken ct = default)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            var req = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.SupplierList)
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
                _logger.LogError("Koza NO_JSON (HTML döndü) - ListTedarikciCarilerAsync");
                throw new InvalidOperationException("Koza NO_JSON (HTML döndü). Auth/şube/cookie kırık olabilir.");
            }

            var dto = JsonSerializer.Deserialize<KozaCariListResponse>(body, _jsonOptions);
            
            if (dto?.Error == true)
            {
                _logger.LogError("Koza tedarikçi listeleme hatası: {Message}", dto.Message ?? "Unknown error");
                throw new InvalidOperationException(dto.Message ?? "Koza tedarikçi listeleme hatası");
            }

            var tedarikçiler = dto?.FinTedarikciListesi ?? new List<KozaCariDto>();
            
            _logger.LogInformation("Koza'dan {Count} tedarikçi cari listelendi", tedarikçiler.Count);
            return tedarikçiler;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListTedarikciCarilerAsync failed");
            throw;
        }
    }

    /// <summary>
    /// Katana Supplier → Koza Tedarikçi Cari
    /// Varsa güncelle, yoksa oluştur (EkleFinTedarikciWS.do)
    /// </summary>
    public async Task<KozaResult> EnsureSupplierCariAsync(KatanaSupplierToCariDto supplier, CancellationToken ct = default)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            // Cari kodu oluştur: TED-{KatanaSupplierId}
            var cariKodu = $"TED-{supplier.KatanaSupplierId}";

            // Önce mevcut tedarikçileri kontrol et (aynı vergi no veya kod var mı)
            var mevcutlar = await ListTedarikciCarilerAsync(ct);
            var mevcut = mevcutlar.FirstOrDefault(t => 
                t.Kod == cariKodu || 
                (!string.IsNullOrEmpty(supplier.TaxNo) && t.VergiNo == supplier.TaxNo));

            if (mevcut != null)
            {
                _logger.LogInformation("Tedarikçi zaten mevcut: {CariKodu} (FinansalNesneId: {Id})", 
                    mevcut.Kod, mevcut.FinansalNesneId);
                return new KozaResult 
                { 
                    Success = true, 
                    Message = "Mevcut tedarikçi bulundu",
                    Data = new { CariKodu = mevcut.Kod, FinansalNesneId = mevcut.FinansalNesneId }
                };
            }

            // Yeni tedarikçi oluştur
            var request = new LucaCreateSupplierRequest
            {
                Tip = 1, // Tüzel kişi
                CariTipId = 2, // Tedarikçi
                KartKod = cariKodu,
                Tanim = supplier.Name,
                KisaAd = supplier.Name.Length > 50 ? supplier.Name.Substring(0, 50) : supplier.Name,
                YasalUnvan = supplier.Name,
                VergiNo = supplier.TaxNo,
                ParaBirimKod = "TRY",
                Ulke = "TÜRKİYE",
                Il = "İSTANBUL",
                AdresSerbest = supplier.Address,
                IletisimTanim = supplier.Phone ?? supplier.Email,
                AdresTipId = 1, // Merkez
                IletisimTipId = supplier.Phone != null ? 1L : 2L // 1=Telefon, 2=Email
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            
            _logger.LogDebug("EnsureSupplierCariAsync creating: {Json}", json);

            var httpReq = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.SupplierCreate)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            ApplySessionCookie(httpReq);
            ApplyManualSessionCookie(httpReq);

            var client = _cookieHttpClient ?? _httpClient;
            var res = await client.SendAsync(httpReq, ct);
            var body = await res.Content.ReadAsStringAsync(ct);

            _logger.LogDebug("EnsureSupplierCariAsync response: {Status}, {Body}", 
                res.StatusCode, body.Length > 500 ? body.Substring(0, 500) : body);

            if (body.TrimStart().StartsWith("<"))
            {
                _logger.LogError("Koza NO_JSON (HTML döndü) - EnsureSupplierCariAsync");
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

            // Response'dan finansalNesneId almaya çalış
            long? finansalNesneId = null;
            try
            {
                var respJson = JsonSerializer.Deserialize<JsonElement>(body);
                if (respJson.TryGetProperty("finansalNesneId", out var fnId))
                {
                    finansalNesneId = fnId.GetInt64();
                }
                else if (respJson.TryGetProperty("finTedarikci", out var ft) && 
                         ft.TryGetProperty("finansalNesneId", out var ftId))
                {
                    finansalNesneId = ftId.GetInt64();
                }
            }
            catch { /* Ignore parse errors */ }

            _logger.LogInformation("Tedarikçi başarıyla oluşturuldu: {CariKodu} - {Name} (FinansalNesneId: {Id})", 
                cariKodu, supplier.Name, finansalNesneId);
            
            return new KozaResult 
            { 
                Success = true, 
                Message = "OK",
                Data = new { CariKodu = cariKodu, FinansalNesneId = finansalNesneId }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EnsureSupplierCariAsync failed for supplier: {Id} - {Name}", 
                supplier.KatanaSupplierId, supplier.Name);
            return new KozaResult { Success = false, Message = ex.Message };
        }
    }
}
