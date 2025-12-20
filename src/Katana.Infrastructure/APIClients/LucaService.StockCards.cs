using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Linq;
using Katana.Core.DTOs;
using Katana.Core.DTOs.Koza;
using KozaDtos = Katana.Core.DTOs.Koza;
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
        // 🔥 MİMARİ RAPOR UYUMLU: Session kontrolü + Branch seçimi
        await EnsureAuthenticatedAsync();
        
        // 🔥 KRİTİK: Branch seçimi ZORUNLU - Mimari rapor bölüm 2.4.1
        if (!_settings.UseTokenAuth)
        {
            await EnsureBranchSelectedAsync();
        }

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        // Ensure single-stock-card creation always targets the SKART endpoint.
        var endpoint = _settings.Endpoints.StockCardCreate;
        var jsonOptionsOriginal = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
        };
        
        // 🔍 DEBUG: Log DTO values before serialization
        _logger.LogInformation("🔍 DEBUG DTO VALUES: MaliyetHesaplanacakFlag={Flag} (Type={Type}), AlisTevkifatOran={ATO}, SatisTevkifatOran={STO}, AlisTevkifatTipId={ATI}, SatisTevkifatTipId={STI}",
            request.MaliyetHesaplanacakFlag, 
            request.MaliyetHesaplanacakFlag.GetType().Name,
            request.AlisTevkifatOran ?? "NULL",
            request.SatisTevkifatOran ?? "NULL",
            request.AlisTevkifatTipId,
            request.SatisTevkifatTipId);
        
	        try
	        {
	            _logger.LogInformation("ATTEMPT 1: JSON with original property names");
	            var json1 = JsonSerializer.Serialize(request, jsonOptionsOriginal);
	            var content1 = CreateKozaContent(json1);
	            using var req1 = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = content1 };
	            ApplyManualSessionCookie(req1);
	            var resp1 = await SendWithAuthRetryAsync(req1, "CREATE_STOCK_ATTEMPT1_HTTP", 2);
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
	            var resp2 = await SendWithAuthRetryAsync(req2, "CREATE_STOCK_ATTEMPT2_HTTP", 2);
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
	            var resp3 = await SendWithAuthRetryAsync(req3, "CREATE_STOCK_ATTEMPT3_HTTP", 2);
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
    /// Postman "Stok Kartı Listesi" request format'ını kullanır
    /// </summary>
    public async Task<IReadOnlyList<KozaDtos.KozaStokKartiDto>> ListStockCardsSimpleAsync(
        DateTime? eklemeBas = null,
        DateTime? eklemeBit = null,
        CancellationToken ct = default)
    {
        try {
            await EnsureAuthenticatedAsync();
            await EnsureBranchSelectedAsync();

            // Build request body matching Postman "Stok Kartı Listesi" format
            object requestBody;
            if (eklemeBas.HasValue && eklemeBit.HasValue)
            {
                requestBody = new
                {
                    stkSkart = new
                    {
                        eklemeTarihiBas = eklemeBas.Value.ToString("dd/MM/yyyy"),
                        eklemeTarihiBit = eklemeBit.Value.ToString("dd/MM/yyyy"),
                        eklemeTarihiOp = "between"
                    }
                };
            }
            else
            {
                // No filter - get all stock cards
                requestBody = new
                {
                    stkSkart = new { }
                };
            }
            
            var json = JsonSerializer.Serialize(requestBody, _jsonOptions);
            var content = CreateKozaContent(json);

            var req = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.StockCards)
            {
                Content = content
            };
            req.Headers.TryAddWithoutValidation("No-Paging", "true");

            ApplySessionCookie(req);
            ApplyManualSessionCookie(req);

            _logger.LogDebug("📤 ListStockCards REQUEST: Endpoint={Endpoint}, Method=POST, Body={Body}",
                _settings.Endpoints.StockCards, json);

            var client = _cookieHttpClient ?? _httpClient;
            var res = await client.SendAsync(req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);

            var logPreview = body.Length > 500 ? body.Substring(0, 500) + "... (truncated)" : body;
            _logger.LogDebug("📥 ListStockCards RESPONSE ({Length} bytes): {Preview}",
                body.Length, logPreview);

#if DEBUG
            // DEBUG ONLY: capture first few raw items from Koza response to inspect field names
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                JsonElement arrayEl = root;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    foreach (var key in new[] { "list", "stokKartlari", "stkKartListesi", "stkSkart", "data", "items" })
                    {
                        if (root.TryGetProperty(key, out var candidate) && candidate.ValueKind == JsonValueKind.Array)
                        {
                            arrayEl = candidate;
                            break;
                        }
                    }
                }

                if (arrayEl.ValueKind == JsonValueKind.Array)
                {
                    var samples = arrayEl.EnumerateArray()
                        .Take(5)
                        .Select(el => el.GetRawText())
                        .ToList();

                    if (samples.Count > 0)
                    {
                        var sampleJson = $"[{string.Join(",", samples)}]";
                        _logger.LogDebug("🧪 DEBUG Koza stock sample (first {Count}): {Sample}", samples.Count, sampleJson);
                        await AppendRawLogAsync("LIST_STOCK_SAMPLE_DEBUG", _settings.Endpoints.StockCards, json, res.StatusCode, sampleJson);
                    }
                }
            }
            catch (Exception sampleEx)
            {
                _logger.LogDebug(sampleEx, "DEBUG sample logging for Koza stock cards failed");
            }
#endif

            // Check for non-JSON responses (HTML or plain text errors)
            var trimmedBody = body.TrimStart();
            
            // HTML response indicates session expired
            if (trimmedBody.StartsWith("<"))
            {
                _logger.LogError("❌ Luca returned HTML (session expired). Response: {Snippet}", logPreview);
                await AppendRawLogAsync("LIST_STOCK_HTML_ERROR", _settings.Endpoints.StockCards, json, res.StatusCode, body);
                return new List<KozaDtos.KozaStokKartiDto>();
            }
            
            // Plain text error (not JSON)
            if (trimmedBody.Length > 0 && 
                char.IsLetter(trimmedBody[0]) && 
                !trimmedBody.StartsWith("{") && 
                !trimmedBody.StartsWith("["))
            {
                _logger.LogError("❌ Luca returned plain text error: '{Text}'", trimmedBody);
                await AppendRawLogAsync("LIST_STOCK_TEXT_ERROR", _settings.Endpoints.StockCards, json, res.StatusCode, body);
                return new List<KozaDtos.KozaStokKartiDto>();
            }

            // Parse JSON response - Koza bazen KDV oranlarını string olarak döndürür ("%20" gibi)
            // Bu yüzden manuel parse yapıyoruz
            var stoklar = new List<KozaDtos.KozaStokKartiDto>();
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                JsonElement arrayEl = default;

                // Find the array in response
                if (root.ValueKind == JsonValueKind.Array)
                {
                    arrayEl = root;
                }
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    foreach (var key in new[] { "list", "stokKartlari", "stkKartListesi", "stkSkart", "data", "items" })
                    {
                        if (root.TryGetProperty(key, out var candidate) && candidate.ValueKind == JsonValueKind.Array)
                        {
                            arrayEl = candidate;
                            break;
                        }
                    }
                    
                    // Check for error response
                    if (root.TryGetProperty("error", out var errorProp) && errorProp.GetBoolean())
                    {
                        var errorMsg = root.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : "Unknown error";
                        _logger.LogError("❌ Koza API returned error: {Message}", errorMsg);
                        return new List<KozaDtos.KozaStokKartiDto>();
                    }
                }

                if (arrayEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in arrayEl.EnumerateArray())
                    {
                        try
                        {
                            var dto = ParseStockCardFromJson(item);
                            if (dto != null)
                            {
                                stoklar.Add(dto);
                            }
                        }
                        catch (Exception itemEx)
                        {
                            _logger.LogWarning(itemEx, "Failed to parse stock card item");
                        }
                    }
                }
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "❌ JSON parse error. Path: {Path}, Line: {Line}, Position: {Pos}", 
                    jsonEx.Path, jsonEx.LineNumber, jsonEx.BytePositionInLine);
                _logger.LogError("Raw response preview: {Preview}", 
                    body.Length > 500 ? body.Substring(0, 500) : body);
                
                await AppendRawLogAsync("LIST_STOCK_JSON_ERROR", _settings.Endpoints.StockCards, json, res.StatusCode, body);
                return new List<KozaDtos.KozaStokKartiDto>();
            }
            
            _logger.LogInformation("✅ Retrieved {Count} stock cards from Koza", stoklar.Count);
            return stoklar;
            

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ ListStockCardsSimpleAsync failed");
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
    private LucaCreateStokKartiRequest MapToFullStokKartiRequest(KozaDtos.KozaStokKartiDto simple)
    {
        // ✅ Tarih formatını düzelt - Koza dd/MM/yyyy formatı bekliyor
        var baslangicTarihi = !string.IsNullOrWhiteSpace(simple.BaslangicTarihi) 
            ? simple.BaslangicTarihi 
            : DateTime.UtcNow.ToString("dd/MM/yyyy");

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
            BaslangicTarihi = baslangicTarihi,  // ✅ Düzeltilmiş format (dd/MM/yyyy)
            MinStokKontrol = simple.MinStokKontrol,
            MinStokMiktari = simple.MinStokMiktari,
            MaxStokKontrol = simple.MaxStokKontrol,
            MaxStokMiktari = simple.MaxStokMiktari,
            SatilabilirFlag = simple.SatilabilirFlag,
            SatinAlinabilirFlag = simple.SatinAlinabilirFlag,
            MaliyetHesaplanacakFlag = simple.MaliyetHesaplanacakFlag != 0,  // int → bool dönüşümü
            
            // Varsayılan değerler
            KartToptanAlisKdvOran = simple.KartAlisKdvOran,
            KartToptanSatisKdvOran = simple.KartSatisKdvOran,
            AlisIskontoOran1 = 0,
            SatisIskontoOran1 = 0,
            RafOmru = 0,
            GarantiSuresi = 0,
            GtipKodu = string.Empty,
            AlisTevkifatOran = null,
            AlisTevkifatTipId = null,
            SatisTevkifatOran = null,
            SatisTevkifatTipId = null,
            IhracatKategoriNo = string.Empty,
            UtsVeriAktarimiFlag = 0,
            BagDerecesi = 0,
            LotNoFlag = simple.LotNoFlag  // ✅ Postman'de var
        };
    }

    /// <summary>
    /// Luca/Koza'ya stok kartı oluşturur - V2 (Yeni API formatı)
    /// Endpoint: EkleStkWsKart.do
    /// </summary>
    public async Task<LucaCreateStockCardResponse> CreateStockCardV2Async(
        LucaCreateStockCardRequestV2 request, 
        CancellationToken ct = default)
    {
        // Validasyon
        var validationErrors = ValidateStockCardRequest(request);
        if (validationErrors.Count > 0)
        {
            return new LucaCreateStockCardResponse
            {
                Error = true,
                Message = string.Join("; ", validationErrors)
            };
        }

        try
        {
            await EnsureAuthenticatedAsync();
            
            if (!_settings.UseTokenAuth)
            {
                await EnsureBranchSelectedAsync();
            }

            var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
            var endpoint = _settings.Endpoints.StockCardCreate;

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
            };

            var json = JsonSerializer.Serialize(request, jsonOptions);
            _logger.LogInformation("CreateStockCardV2Async: Sending request for {KartKodu} - {KartAdi}", 
                request.KartKodu, request.KartAdi);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = CreateKozaContent(json)
            };
            ApplyManualSessionCookie(httpRequest);

            var response = await SendWithAuthRetryAsync(httpRequest, "CREATE_STOCK_V2", 2);
            var responseBody = await ReadResponseContentAsync(response);
            
            await AppendRawLogAsync("CREATE_STOCK_V2", endpoint, json, response.StatusCode, responseBody);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("CreateStockCardV2Async failed HTTP {Status}: {Body}", 
                    response.StatusCode, responseBody);
                return new LucaCreateStockCardResponse
                {
                    Error = true,
                    Message = $"HTTP {response.StatusCode}: {responseBody}"
                };
            }

            // Response parse
            return ParseStockCardResponse(responseBody, request.KartKodu, request.KartAdi);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateStockCardV2Async failed for: {KartKodu}", request.KartKodu);
            return new LucaCreateStockCardResponse
            {
                Error = true,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// Stok kartı request validasyonu
    /// </summary>
    private List<string> ValidateStockCardRequest(LucaCreateStockCardRequestV2 request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.KartKodu))
            errors.Add("kartKodu is required");

        if (string.IsNullOrWhiteSpace(request.KartAdi))
            errors.Add("kartAdi is required");

        if (request.KartTuru != 1 && request.KartTuru != 2)
            errors.Add("kartTuru must be 1 (Stok) or 2 (Hizmet)");

        if (string.IsNullOrWhiteSpace(request.BaslangicTarihi))
            errors.Add("baslangicTarihi is required");
        else if (!IsValidDateFormat(request.BaslangicTarihi))
            errors.Add("baslangicTarihi must be in dd/MM/yyyy format");

        return errors;
    }

    /// <summary>
    /// Tarih formatı kontrolü (dd/MM/yyyy)
    /// </summary>
    private bool IsValidDateFormat(string dateStr)
    {
        return DateTime.TryParseExact(
            dateStr, 
            "dd/MM/yyyy", 
            CultureInfo.InvariantCulture, 
            DateTimeStyles.None, 
            out _);
    }

    /// <summary>
    /// JSON element'ten stok kartı parse et
    /// Koza bazen KDV oranlarını string olarak döndürür ("%20" gibi)
    /// </summary>
    private KozaDtos.KozaStokKartiDto? ParseStockCardFromJson(JsonElement item)
    {
        var dto = new KozaDtos.KozaStokKartiDto();

        // ID alanları
        if (item.TryGetProperty("skartId", out var skartId))
            dto.StokKartId = skartId.ValueKind == JsonValueKind.Number ? skartId.GetInt64() : null;
        else if (item.TryGetProperty("stokKartId", out var stokKartId))
            dto.StokKartId = stokKartId.ValueKind == JsonValueKind.Number ? stokKartId.GetInt64() : null;

        // Kod alanları - TÜM OLASI FIELD İSİMLERİNİ KONTROL ET
        // Luca API farklı endpoint'lerde farklı field isimleri dönebiliyor
        dto.KartKodu = TryGetProperty(item, "kod", "kartKodu", "code", "skartKod", "stokKartKodu", "stokKodu") ?? "";

        // Ad alanları - TÜM OLASI FIELD İSİMLERİNİ KONTROL ET
        dto.KartAdi = TryGetProperty(item, "adi", "kartAdi", "tanim", "name", "stokKartAdi", "stokAdi") ?? "";

        // Barkod
        if (item.TryGetProperty("barkod", out var barkod))
            dto.Barkod = barkod.GetString();

        // Kategori
        if (item.TryGetProperty("kategoriKodu", out var kategoriKodu))
            dto.KategoriAgacKod = kategoriKodu.GetString() ?? "";
        else if (item.TryGetProperty("hiyerarsikKod", out var hiyerarsikKod))
            dto.KategoriAgacKod = hiyerarsikKod.GetString() ?? "";
        else if (item.TryGetProperty("kategoriAgacKod", out var kategoriAgacKod))
            dto.KategoriAgacKod = kategoriAgacKod.GetString() ?? "";

        // KDV oranları - string olarak gelebilir ("%20" gibi)
        dto.KartSatisKdvOran = ParseKdvOran(item, "satisKdvOran", "kartSatisKdvOran");
        dto.KartAlisKdvOran = ParseKdvOran(item, "alisKdvOran", "kartAlisKdvOran");

        // Ölçüm birimi
        if (item.TryGetProperty("temelOlcuBirimi", out var temelOlcuBirimi))
            dto.UzunAdi = temelOlcuBirimi.GetString(); // Geçici olarak UzunAdi'ye koyuyoruz

        return dto;
    }

    /// <summary>
    /// KDV oranını parse et - string ("%20") veya number olabilir
    /// </summary>
    private double ParseKdvOran(JsonElement item, params string[] propertyNames)
    {
        foreach (var propName in propertyNames)
        {
            if (item.TryGetProperty(propName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number)
                {
                    return prop.GetDouble();
                }
                else if (prop.ValueKind == JsonValueKind.String)
                {
                    var strVal = prop.GetString();
                    if (!string.IsNullOrEmpty(strVal))
                    {
                        // "%20" -> 0.20, "20" -> 0.20
                        strVal = strVal.Replace("%", "").Trim();
                        if (double.TryParse(strVal, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                        {
                            // Eğer 1'den büyükse yüzde olarak gelmiş demektir
                            return parsed > 1 ? parsed / 100.0 : parsed;
                        }
                    }
                }
            }
        }
        return 0.18; // Varsayılan KDV
    }

    /// <summary>
    /// Stok kartı response parse
    /// </summary>
    private LucaCreateStockCardResponse ParseStockCardResponse(string? responseBody, string kartKodu, string kartAdi)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return new LucaCreateStockCardResponse
            {
                Error = true,
                Message = "Empty response from Luca"
            };
        }

        try
        {
            var result = JsonSerializer.Deserialize<LucaCreateStockCardResponse>(responseBody, _jsonOptions);
            if (result != null)
            {
                // Başarılı ise log
                if (!result.Error && result.SkartId.HasValue)
                {
                    _logger.LogInformation("✅ Stok kartı oluşturuldu: {KartKodu} - {KartAdi}, SkartId: {SkartId}", 
                        kartKodu, kartAdi, result.SkartId);
                }
                return result;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse stock card response as JSON");
        }

        // Fallback: "Başar" içeriyorsa başarılı say
        if (responseBody.Contains("Başar", StringComparison.OrdinalIgnoreCase) ||
            responseBody.Contains("başarı", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("✅ Stok kartı oluşturuldu (text match): {KartKodu} - {KartAdi}", kartKodu, kartAdi);
            return new LucaCreateStockCardResponse
            {
                Error = false,
                Message = responseBody
            };
        }

        return new LucaCreateStockCardResponse
        {
            Error = true,
            Message = responseBody
        };
    }

    // ====================================================================
    // STOK KARTI GÜNCELLEME VE SİLME İŞLEMLERİ
    // ====================================================================

    /// <summary>
    /// Mevcut bir stok kartını günceller.
    /// Endpoint: /GuncelleStkWsSkart.do
    /// </summary>
    public async Task<bool> UpdateStockCardAsync(LucaUpdateStokKartiRequest request)
    {
        await EnsureAuthenticatedAsync();
        await EnsureBranchSelectedAsync();

        // Endpoint - appsettings'den al veya sabit kullan
        var endpoint = _settings.Endpoints.StockCardUpdate ?? "GuncelleStkWsSkart.do";
        
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        // ATTEMPT 1: Direct JSON serialization
        try
        {
            _logger.LogInformation("📝 Stok Kartı Güncelleme ATTEMPT 1: Direct JSON. ID={Id}, SKU={Sku}", 
                request.SkartId, request.KartKodu);
            
            var json = JsonSerializer.Serialize(request, jsonOptions);
            var content = CreateKozaContent(json);
            
            using var req = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = content };
            ApplyManualSessionCookie(req);
            
            var response = await SendWithAuthRetryAsync(req, "UPDATE_STOCK_ATTEMPT1", 2);
            var body = await ReadResponseContentAsync(response);
            
            await AppendRawLogAsync("UPDATE_STOCK_ATTEMPT1", endpoint, json, response.StatusCode, body);

            if (response.IsSuccessStatusCode)
            {
                var parsed = ParseKozaOperationResponse(body);
                if (parsed.IsSuccess)
                {
                    _logger.LogInformation("✅ Stok kartı güncellendi: ID={Id}, SKU={Sku}", request.SkartId, request.KartKodu);
                    return true;
                }
            }
            
            _logger.LogWarning("Stok kartı güncelleme ATTEMPT 1 başarısız: {Response}", body?.Substring(0, Math.Min(300, body?.Length ?? 0)));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "UpdateStockCard ATTEMPT 1 exception for ID: {Id}", request.SkartId);
        }

        // ATTEMPT 2: Wrapped object (stkSkart)
        try
        {
            _logger.LogInformation("📝 Stok Kartı Güncelleme ATTEMPT 2: Wrapped object. ID={Id}", request.SkartId);
            
            var wrapped = new { stkSkart = request };
            var json = JsonSerializer.Serialize(wrapped, jsonOptions);
            var content = CreateKozaContent(json);
            
            using var req = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = content };
            ApplyManualSessionCookie(req);
            
            var response = await SendWithAuthRetryAsync(req, "UPDATE_STOCK_ATTEMPT2", 2);
            var body = await ReadResponseContentAsync(response);
            
            await AppendRawLogAsync("UPDATE_STOCK_ATTEMPT2", endpoint, json, response.StatusCode, body);

            if (response.IsSuccessStatusCode)
            {
                var parsed = ParseKozaOperationResponse(body);
                if (parsed.IsSuccess)
                {
                    _logger.LogInformation("✅ Stok kartı güncellendi (ATTEMPT 2): ID={Id}, SKU={Sku}", request.SkartId, request.KartKodu);
                    return true;
                }
            }
            
            _logger.LogWarning("Stok kartı güncelleme ATTEMPT 2 başarısız: {Response}", body?.Substring(0, Math.Min(300, body?.Length ?? 0)));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "UpdateStockCard ATTEMPT 2 exception for ID: {Id}", request.SkartId);
        }

        _logger.LogError("❌ Stok kartı güncelleme başarısız - tüm denemeler tükendi. ID={Id}, SKU={Sku}", 
            request.SkartId, request.KartKodu);
        return false;
    }

    /// <summary>
    /// Stok kartını siler (HARD DELETE - JSON Formatı ile)
    /// Luca'ya { "skartId": ... } formatında silme isteği gönderir
    /// 🔥 MİMARİ RAPOR UYUMLU: Session + Branch + Cookie yönetimi
    /// </summary>
    public async Task<bool> DeleteStockCardAsync(long skartId)
    {
        _logger.LogInformation("🔥 HARD DELETE (JSON): Kart silme isteği hazırlanıyor... ID: {Id}", skartId);

        try
        {
            // 🔥 MİMARİ RAPOR UYUMLU: Session kontrolü (Bölüm 4.1)
            await EnsureAuthenticatedAsync();
            
            // 🔥 KRİTİK: Branch seçimi ZORUNLU - Mimari rapor bölüm 2.4.3
            if (!_settings.UseTokenAuth)
            {
                await EnsureBranchSelectedAsync();
            }

            // 🔥 MİMARİ RAPOR UYUMLU: Doğru client seçimi
            var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
            
            // Endpoint - settings'ten al
            string endpoint = _settings.Endpoints.StockCardDelete;

            // Request Body (İstenen Format: {"skartId": 79909})
            var requestData = new { skartId = skartId };
            string jsonContent = JsonSerializer.Serialize(requestData);

            _logger.LogInformation("📤 Gönderilen JSON: {Json}", jsonContent);

            // 🔥 MİMARİ RAPOR UYUMLU: CreateKozaContent kullan (ISO-8859-9 encoding)
            var content = CreateKozaContent(jsonContent);

            // 🔥 MİMARİ RAPOR UYUMLU: HttpRequestMessage ile cookie ekle
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = content
            };
            
            // 🔥 KRİTİK: Session cookie'yi manuel ekle
            ApplyManualSessionCookie(request);

            // 🔥 MİMARİ RAPOR UYUMLU: SendWithAuthRetryAsync kullan (retry mekanizması)
            var response = await SendWithAuthRetryAsync(request, "DELETE_STOCK_CARD", 2);
            var responseString = await ReadResponseContentAsync(response);

            _logger.LogInformation("📥 Luca Yanıtı ({Code}): {Response}", response.StatusCode, responseString);

            // Raw log kaydet
            await AppendRawLogAsync("DELETE_STOCK_CARD", endpoint, jsonContent, response.StatusCode, responseString);

            // Başarı Kontrolü
            if (response.IsSuccessStatusCode)
            {
                // HTML response = session expired
                if (responseString.TrimStart().StartsWith("<"))
                {
                    _logger.LogError("❌ Luca HTML döndü (session expired). Retry gerekli.");
                    return false;
                }
                
                // JSON parse et
                try
                {
                    using var doc = JsonDocument.Parse(responseString);
                    var root = doc.RootElement;
                    
                    // error: true kontrolü
                    if (root.TryGetProperty("error", out var errorProp) && errorProp.GetBoolean())
                    {
                        var errorMsg = root.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : "Unknown error";
                        _logger.LogError("❌ Luca silme hatası: {Message}", errorMsg);
                        return false;
                    }
                    
                    // Başarılı - error: false veya error yok
                    _logger.LogInformation("✅ İŞLEM TAMAM: Kart Luca'dan silindi. ID: {Id}", skartId);
                    return true;
                }
                catch (JsonException)
                {
                    // JSON değilse text kontrol et
                    if (!responseString.Contains("hata", StringComparison.OrdinalIgnoreCase) && 
                        !responseString.Contains("error", StringComparison.OrdinalIgnoreCase) &&
                        !responseString.Contains("Beklenmedik", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("✅ İŞLEM TAMAM: Kart Luca'dan silindi (text response). ID: {Id}", skartId);
                        return true;
                    }
                }
            }
            
            // 🔥 FALLBACK: Hard delete başarısız oldu, Zombie operasyonu dene
            _logger.LogWarning("⚠️ Hard delete başarısız. Zombie operasyonu deneniyor... ID: {Id}", skartId);
            
            try
            {
                var zombieResult = await DeleteStockCardZombieAsync(skartId);
                if (zombieResult)
                {
                    _logger.LogInformation("✅ Zombie operasyonu başarılı! Kart pasife çekildi. ID: {Id}", skartId);
                    return true;
                }
            }
            catch (Exception zombieEx)
            {
                _logger.LogError(zombieEx, "❌ Zombie operasyonu da başarısız. ID: {Id}", skartId);
            }
            
            _logger.LogError("❌ Luca silme işlemine izin vermedi ve zombie operasyonu da başarısız oldu.");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Silme isteği sırasında teknik hata oluştu. ID: {Id}", skartId);
            return false;
        }
    }
}
