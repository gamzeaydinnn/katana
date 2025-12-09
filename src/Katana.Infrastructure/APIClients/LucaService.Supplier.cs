using System;
using System.Text;
using System.Text.Json;
using Katana.Core.DTOs.Koza;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Microsoft.Extensions.Logging;
using System.Linq;

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
        return await ListTedarikciCarilerAsync(null, null, "between", ct);
    }

    /// <summary>
    /// Luca Tedarikçi Carilerini Listele (ListeleFinTedarikci.do) - Filtreleme ile
    /// </summary>
    public async Task<IReadOnlyList<KozaCariDto>> ListTedarikciCarilerAsync(
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
                    finTedarikci = new
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
                // Luca dokümantasyonuna göre apiFilter=true gönderilmeli
                payload = new
                {
                    apiFilter = true
                };
            }

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var req = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.SupplierList)
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
                var snippet = body.Length > 200 ? body.Substring(0, 200) : body;
                _logger.LogError("Luca NO_JSON (HTML döndü) - ListTedarikciCarilerAsync. Body snippet: {Snippet}", snippet);
                // Exception yerine boş liste dön
                return new List<KozaCariDto>();
            }

            // Debug için raw body'yi logla (ilk 500 karakter)
            _logger.LogDebug("ListTedarikciCarilerAsync raw body (first 500): {Body}",
                body.Length > 500 ? body[..500] : body);
            _logger.LogInformation("DEBUG ListTedarikciCarilerAsync FULL BODY SAMPLE (first 1000 chars):{NewLine}{Body}",
                Environment.NewLine,
                body.Length > 1000 ? body[..1000] : body);

            var dto = JsonSerializer.Deserialize<KozaCariListResponse>(body, _jsonOptions);
            
            if (dto?.Error == true)
            {
                _logger.LogError("Luca tedarikçi listeleme hatası: {Message}", dto.Message ?? "Unknown error");
                throw new InvalidOperationException(dto.Message ?? "Luca tedarikçi listeleme hatası");
            }

            // Önce finTedarikciListesi, yoksa list'e bak
            var tedarikçiler = dto?.FinTedarikciListesi
                             ?? dto?.List
                             ?? new List<KozaCariDto>();
            
            _logger.LogInformation("Luca'dan {Count} tedarikçi cari listelendi (Filtre: {KodBas}-{KodBit})", 
                tedarikçiler.Count, kodBas ?? "Tümü", kodBit ?? "Tümü");
            return tedarikçiler;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListTedarikciCarilerAsync failed");
            throw;
        }
    }

    /// <summary>
    /// Luca tedarikçi carilerini sade liste DTO'suna map ederek döndürür
    /// </summary>
    public async Task<IReadOnlyList<KozaSupplierListItemDto>> ListTedarikciSupplierItemsAsync(CancellationToken ct = default)
    {
        return await ListTedarikciSupplierItemsAsync(null, null, "between", ct);
    }

    /// <summary>
    /// Luca tedarikçi carilerini sade liste DTO'suna map ederek döndürür (filtrelenmiş)
    /// </summary>
    public async Task<IReadOnlyList<KozaSupplierListItemDto>> ListTedarikciSupplierItemsAsync(
        string? kodBas,
        string? kodBit,
        string kodOp = "between",
        CancellationToken ct = default)
    {
        var cariler = await ListTedarikciCarilerAsync(kodBas, kodBit, kodOp, ct);
        if (cariler == null || cariler.Count == 0)
        {
            return new List<KozaSupplierListItemDto>();
        }

        return cariler
            .Select(KozaSupplierListItemDto.FromKozaCari)
            .ToList();
    }

    /// <summary>
    /// Koza Tedarikçi Kartı Ekleme (EkleFinTedarikciWS.do) - Postman ile birebir uyumlu
    /// </summary>
    public async Task<KozaResult> CreateTedarikciCariAsync(KozaTedarikciEkleRequest request, CancellationToken ct = default)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            // FIXED: Postman ile birebir uyumlu - düz obje, finTedarikci sarmalayıcısı YOK
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            
            _logger.LogDebug("CreateTedarikciCariAsync request: {Json}", json);

            var httpReq = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.SupplierCreate)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            ApplySessionCookie(httpReq);
            ApplyManualSessionCookie(httpReq);

            var client = _cookieHttpClient ?? _httpClient;
            var res = await client.SendAsync(httpReq, ct);
            var body = await res.Content.ReadAsStringAsync(ct);

            _logger.LogDebug("CreateTedarikciCariAsync response status: {Status}, body: {Body}", 
                res.StatusCode, body.Length > 500 ? body.Substring(0, 500) : body);

            if (body.TrimStart().StartsWith("<"))
            {
                _logger.LogError("Koza NO_JSON (HTML döndü) - CreateTedarikciCariAsync. Body snippet: {Snippet}",
                    body.Length > 300 ? body[..300] : body);
                return new KozaResult 
                { 
                    Success = false, 
                    Message = "Koza HTML hata sayfası döndürdü (muhtemelen JSON formatı uyuşmuyor)." 
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
                else if (respJson.TryGetProperty("finTedarikci", out var ft))
                {
                    if (ft.TryGetProperty("finansalNesneId", out var ftId))
                        finansalNesneId = ftId.GetInt64();
                    if (ft.TryGetProperty("kod", out var kod))
                        cariKodu = kod.GetString();
                }
                
                if (respJson.TryGetProperty("kod", out var kod2))
                    cariKodu = kod2.GetString();
            }
            catch { /* Ignore parse errors */ }

            _logger.LogInformation("Tedarikçi cari başarıyla oluşturuldu: {CariKodu} (FinansalNesneId: {Id})", 
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
            _logger.LogError(ex, "CreateTedarikciCariAsync failed for kartKod: {KartKod}", request.KartKod);
            return new KozaResult { Success = false, Message = ex.Message };
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
                CariTipId = 1, // Tedarikçi (Postman'da 1)
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

            // Yeni tedarikçi oluştur - Postman örneğine uygun tüm zorunlu alanlarla
            var kozaRequest = new KozaTedarikciEkleRequest
            {
                Tip = "1", // "1": Şirket
                CariTipId = 1, // Tedarikçi (Postman'da 1)
                TakipNoFlag = true, // Postman'da true
                EfaturaTuru = 1, // 1: Temel Fatura
                KategoriKod = "", // Postman'da boş string
                KartKod = cariKodu,
                Tanim = supplier.Name,
                KisaAd = supplier.Name.Length > 50 ? supplier.Name.Substring(0, 50) : supplier.Name,
                YasalUnvan = supplier.Name,
                VergiNo = supplier.TaxNo,
                ParaBirimKod = "TRY",
                TcUyruklu = true, // Postman'da true
                AdresTipId = 9, // 9: Fatura adresi (Postman'da 9)
                Ulke = "TÜRKİYE",
                Il = "İSTANBUL",
                Ilce = "Merkez", // Postman'da var
                AdresSerbest = supplier.Address ?? "",
                IletisimTipId = supplier.Phone != null ? 3 : 5, // 3=Cep, 5=E-Posta (Postman'da 3)
                IletisimTanim = supplier.Phone ?? supplier.Email ?? ""
            };

            _logger.LogWarning("=== SUPPLIER CREATE - Using CreateTedarikciCariAsync ===");
            _logger.LogWarning("CariKodu: {CariKodu}", cariKodu);
            
            // CreateTedarikciCariAsync kullan - bu metod zaten doğru formatta gönderir
            var result = await CreateTedarikciCariAsync(kozaRequest, ct);

            if (result.Success)
            {
                string? kozaCariKodu = cariKodu;
                long? finansalNesneId = null;

                try
                {
                    if (result.Data != null)
                    {
                        var dataJson = JsonSerializer.Serialize(result.Data, _jsonOptions);
                        var dataElement = JsonSerializer.Deserialize<JsonElement>(dataJson, _jsonOptions);

                        if (dataElement.TryGetProperty("CariKodu", out var ck))
                            kozaCariKodu = ck.GetString() ?? kozaCariKodu;
                        if (dataElement.TryGetProperty("FinansalNesneId", out var fn) && fn.ValueKind == JsonValueKind.Number)
                            finansalNesneId = fn.GetInt64();
                    }
                }
                catch
                {
                    // ignore parse errors
                }

                _logger.LogInformation(
                    "Supplier sync ok: KatanaSupplierId={Id}, KatanaName={Name}, KozaCariKodu={Kod}, KozaFinansalNesneId={FinId}",
                    supplier.KatanaSupplierId,
                    supplier.Name,
                    kozaCariKodu,
                    finansalNesneId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EnsureSupplierCariAsync failed for supplier: {Id} - {Name}", 
                supplier.KatanaSupplierId, supplier.Name);
            return new KozaResult { Success = false, Message = ex.Message };
        }
    }
}
