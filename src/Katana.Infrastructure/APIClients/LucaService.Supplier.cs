using System.Text;
using System.Text.Json;
using Katana.Business.DTOs.Koza;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Microsoft.Extensions.Logging;

namespace Katana.Infrastructure.APIClients;

/// <summary>
/// Luca Tedarikçi (Supplier) işlemleri
/// </summary>
public partial class LucaService
{
    /// <summary>
    /// Luca'ya tedarikçi oluşturur
    /// </summary>
    public async Task<JsonElement> CreateSupplierAsync(LucaCreateSupplierRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.SupplierCreate, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    /// <summary>
    /// Luca Tedarikçi Carilerini Listele (ListeleFinTedarikci.do)
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
                var snippet = body.Length > 200 ? body.Substring(0, 200) : body;
                _logger.LogError("Luca NO_JSON (HTML döndü) - ListTedarikciCarilerAsync. Body snippet: {Snippet}", snippet);
                // Exception yerine boş liste dön
                return new List<KozaCariDto>();
            }

            var dto = JsonSerializer.Deserialize<KozaCariListResponse>(body, _jsonOptions);
            
            if (dto?.Error == true)
            {
                _logger.LogError("Luca tedarikçi listeleme hatası: {Message}", dto.Message ?? "Unknown error");
                throw new InvalidOperationException(dto.Message ?? "Luca tedarikçi listeleme hatası");
            }

            var tedarikçiler = dto?.FinTedarikciListesi ?? new List<KozaCariDto>();
            
            _logger.LogInformation("Luca'dan {Count} tedarikçi cari listelendi", tedarikçiler.Count);
            return tedarikçiler;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListTedarikciCarilerAsync failed");
            throw;
        }
    }

    /// <summary>
    /// Katana Supplier'ı Luca/Koza Cari olarak ekler/günceller
    /// </summary>
    public async Task<SyncResultDto> UpsertCariCardAsync(Supplier supplier)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            // Luca cari kodu oluştur
            var lucaCode = supplier.LucaCode ?? $"TED-{supplier.Id}";

            // Önce mevcut tedarikçileri kontrol et
            var mevcutlar = await ListTedarikciCarilerAsync();
            var mevcut = mevcutlar.FirstOrDefault(t => 
                t.Kod == lucaCode || 
                (!string.IsNullOrEmpty(supplier.TaxNo) && t.VergiNo == supplier.TaxNo));

            if (mevcut != null)
            {
                _logger.LogInformation("Tedarikçi cari zaten mevcut: {CariKodu} (FinansalNesneId: {Id})", 
                    mevcut.Kod, mevcut.FinansalNesneId);
                
                return new SyncResultDto
                {
                    IsSuccess = true,
                    Message = "Tedarikçi cari zaten Luca'da mevcut",
                    SyncType = "SUPPLIER_CARI_UPSERT",
                    Details = new List<string> { $"finansalNesneId={mevcut.FinansalNesneId}" }
                };
            }

            // Yeni tedarikçi cari oluştur
            var request = new LucaCreateSupplierRequest
            {
                Tip = 1, // Tüzel kişi
                CariTipId = 2, // Tedarikçi
                KartKod = lucaCode,
                Tanim = supplier.Name,
                KisaAd = supplier.Name.Length > 50 ? supplier.Name.Substring(0, 50) : supplier.Name,
                YasalUnvan = supplier.Name,
                VergiNo = supplier.TaxNo,
                ParaBirimKod = "TRY",
                Ulke = "TÜRKİYE",
                Il = supplier.City ?? "İSTANBUL",
                AdresSerbest = supplier.Address,
                IletisimTanim = supplier.Phone ?? supplier.Email,
                AdresTipId = 1, // Merkez
                IletisimTipId = supplier.Phone != null ? 1L : 2L // 1=Telefon, 2=Email
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            
            _logger.LogDebug("UpsertCariCardAsync (Supplier) creating: {Json}", json);

            var httpReq = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.SupplierCreate)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            ApplySessionCookie(httpReq);
            ApplyManualSessionCookie(httpReq);

            var client = _cookieHttpClient ?? _httpClient;
            var res = await client.SendAsync(httpReq);
            var body = await res.Content.ReadAsStringAsync();

            _logger.LogDebug("UpsertCariCardAsync (Supplier) response: {Status}, {Body}", 
                res.StatusCode, body.Length > 500 ? body.Substring(0, 500) : body);

            if (body.TrimStart().StartsWith("<"))
            {
                _logger.LogError("Luca NO_JSON (HTML döndü) - UpsertCariCardAsync (Supplier)");
                return new SyncResultDto
                {
                    IsSuccess = false,
                    Message = "Luca NO_JSON (HTML döndü). Auth/şube/cookie kırık olabilir.",
                    SyncType = "SUPPLIER_CARI_UPSERT",
                    Errors = new List<string> { "NO_JSON_RESPONSE" }
                };
            }

            if (!res.IsSuccessStatusCode)
            {
                return new SyncResultDto
                {
                    IsSuccess = false,
                    Message = $"HTTP {res.StatusCode}: {body}",
                    SyncType = "SUPPLIER_CARI_UPSERT",
                    Errors = new List<string> { body }
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

            _logger.LogInformation("Tedarikçi cari başarıyla oluşturuldu: {CariKodu} - {Name} (FinansalNesneId: {Id})", 
                lucaCode, supplier.Name, finansalNesneId);
            
            return new SyncResultDto
            {
                IsSuccess = true,
                Message = $"Tedarikçi cari başarıyla oluşturuldu: {lucaCode}",
                SyncType = "SUPPLIER_CARI_UPSERT",
                Details = finansalNesneId.HasValue 
                    ? new List<string> { $"finansalNesneId={finansalNesneId}" }
                    : new List<string>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpsertCariCardAsync (Supplier) failed for supplier: {Id} - {Name}", 
                supplier.Id, supplier.Name);
            
            return new SyncResultDto
            {
                IsSuccess = false,
                Message = $"Tedarikçi cari senkronizasyonu başarısız: {ex.Message}",
                SyncType = "SUPPLIER_CARI_UPSERT",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    /// <summary>
    /// Katana Supplier → Luca Tedarikçi Cari (Legacy)
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
                _logger.LogError("Luca NO_JSON (HTML döndü) - EnsureSupplierCariAsync");
                return new KozaResult 
                { 
                    Success = false, 
                    Message = "Luca NO_JSON (HTML döndü). Auth/şube/cookie kırık olabilir." 
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
