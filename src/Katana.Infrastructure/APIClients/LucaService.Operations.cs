using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Katana.Core.DTOs;
using Katana.Core.DTOs.Koza;
using Katana.Core.Helpers;
using Katana.Data.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text;
using System.IO;
using System.Linq;
using HtmlAgilityPack;
using System.Net.Http.Headers;
using System.Net;
using System.Globalization;
using Katana.Business.Interfaces;
using Katana.Business.Mappers;
using Katana.Core.Entities;
using KozaDtos = Katana.Core.DTOs.Koza;
using System.Diagnostics;

namespace Katana.Infrastructure.APIClients;

/// <summary>
/// LucaService - PART 2: Operations (Send/Create methods - Invoices, Customers, Stock, Products)
/// </summary>
public partial class LucaService
{
    /// <summary>
    /// ZOMBİ OPERASYONU: Kartı pasife çeker ve ismini/kodunu değiştirir
    /// Hareket görmüş kartları silmek yerine görünmez yapar
    /// 🔥 MİMARİ RAPOR UYUMLU: Session + Branch + Cookie yönetimi
    /// </summary>
    public async Task<bool> DeleteStockCardZombieAsync(long skartId)
    {
        _logger.LogInformation("🧟 ZOMBİ OPERASYONU BAŞLATILDI: Kart Pasife Çekiliyor... ID: {Id}", skartId);

        try
        {
            // 🔥 MİMARİ RAPOR UYUMLU: Session kontrolü
            await EnsureAuthenticatedAsync();
            
            // 🔥 KRİTİK: Branch seçimi ZORUNLU
            if (!_settings.UseTokenAuth)
            {
                await EnsureBranchSelectedAsync();
            }

            // 1. Kartın mevcut verilerini çek (İsim ve Kod bozulmasın diye)
            var existingCards = await ListStockCardsSimpleAsync(); 
            var targetCard = existingCards.FirstOrDefault(x => x.StokKartId == skartId);

            if (targetCard == null)
            {
                _logger.LogError("❌ Hedef kart bulunamadı! ID: {Id}", skartId);
                return false;
            }

            // 2. Yeni İsim ve Kod Belirle (Zombileştirme)
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmm");
            string newCode = $"SIL_{targetCard.KartKodu}_{timestamp}"; 
            if (newCode.Length > 30) newCode = newCode.Substring(0, 30);

            string newName = $"!!! SİLİNDİ !!! - {targetCard.KartAdi}";
            if (newName.Length > 50) newName = newName.Substring(0, 50);

            // 3. MANUEL JSON OLUŞTURMA
            var payload = new Dictionary<string, object>
            {
                { "skartId", targetCard.StokKartId ?? 0 },
                { "kartKodu", newCode },
                { "kartAdi", newName },
                { "aktif", 0 },           // <--- KRİTİK NOKTA: 0 (Pasif)
                { "kartTipi", 1 },
                { "kartTuru", 1 },
                { "anaBirimId", 1 },
                { "olcumBirimiId", 1 },
                { "kdvOrani", 0 }
            };

            var json = JsonSerializer.Serialize(payload);
            _logger.LogInformation("📤 Zombi Payload: {Json}", json);

            // 🔥 MİMARİ RAPOR UYUMLU: CreateKozaContent + HttpRequestMessage + Cookie
            string endpoint = _settings.Endpoints.StockCardUpdate;
            var content = CreateKozaContent(json);
            
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = content
            };
            
            // 🔥 KRİTİK: Session cookie'yi manuel ekle
            ApplyManualSessionCookie(request);

            // 🔥 MİMARİ RAPOR UYUMLU: SendWithAuthRetryAsync kullan
            var response = await SendWithAuthRetryAsync(request, "ZOMBIE_UPDATE", 2);
            var responseContent = await ReadResponseContentAsync(response);

            _logger.LogInformation("📥 Luca Yanıtı: {Content}", responseContent);
            
            // Raw log kaydet
            await AppendRawLogAsync("ZOMBIE_UPDATE", endpoint, json, response.StatusCode, responseContent);

            // Başarı kontrolü
            if (response.IsSuccessStatusCode)
            {
                // HTML response = session expired
                if (responseContent.TrimStart().StartsWith("<"))
                {
                    _logger.LogError("❌ Luca HTML döndü (session expired)");
                    return false;
                }
                
                // JSON parse et
                try
                {
                    using var doc = JsonDocument.Parse(responseContent);
                    var root = doc.RootElement;
                    
                    if (root.TryGetProperty("error", out var errorProp) && errorProp.GetBoolean())
                    {
                        var errorMsg = root.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : "Unknown error";
                        _logger.LogError("❌ Luca hata mesajı döndü: {Msg}", errorMsg);
                        return false;
                    }
                }
                catch (JsonException)
                {
                    // JSON değilse text kontrol et
                    if (responseContent.Contains("hata", StringComparison.OrdinalIgnoreCase) ||
                        responseContent.Contains("error", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogError("❌ Luca hata mesajı döndü: {Msg}", responseContent);
                        return false;
                    }
                }

                _logger.LogInformation("✅ ZOMBİ OPERASYONU BAŞARILI! Kart pasife çekildi. Yeni Kod: {Code}", newCode);
                return true;
            }
            else
            {
                _logger.LogError("❌ HTTP Hatası alındı: {Code}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Zombi operasyonu patladı. ID: {Id}", skartId);
        }

        return false;
    }

        public async Task<SalesOrderSyncResultDto> CreateSalesOrderInvoiceAsync(SalesOrder order, string? depoKodu = null, CancellationToken ct = default)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));

            // 🚨 IDEMPOTENCY CHECK: Same OrderNo must NEVER create a second invoice
            if (order.IsSyncedToLuca && order.LucaOrderId.HasValue && order.LucaOrderId.Value > 0)
            {
                _logger.LogWarning("🚨 IDEMPOTENCY: Invoice already exists for OrderNo={OrderNo}, LucaOrderId={LucaOrderId}. Returning existing.", 
                    order.OrderNo, order.LucaOrderId);
                return new SalesOrderSyncResultDto
                {
                    IsSuccess = true,
                    Message = "Fatura zaten mevcut (idempotent)",
                    LucaOrderId = (int)order.LucaOrderId.Value,
                    SyncedAt = order.LastSyncAt ?? DateTime.UtcNow
                };
            }

            if (order.Customer == null)
            {
                return new SalesOrderSyncResultDto
                {
                    IsSuccess = false,
                    Message = "Müşteri bilgisi eksik",
                    ErrorDetails = "Customer is null"
                };
            }

        if (order.Lines == null || order.Lines.Count == 0)
        {
            return new SalesOrderSyncResultDto
            {
                IsSuccess = false,
                Message = "Sipariş satırları bulunamadı",
                ErrorDetails = "No order lines"
            };
        }

            // 🔍 DETAYLI LOGLAMA: Sipariş bilgileri
            _logger.LogInformation(
                "📤 Luca fatura oluşturma başlatıldı. OrderId={OrderId}, OrderNo={OrderNo}, Currency={Currency}, ConversionRate={ConversionRate}, LineCount={LineCount}",
                order.Id, order.OrderNo, order.Currency, order.ConversionRate, order.Lines?.Count ?? 0);

        static string? TryGetMessage(JsonElement el)
        {
            if (el.TryGetProperty("mesaj", out var mesaj) && mesaj.ValueKind == JsonValueKind.String)
                return mesaj.GetString();
            if (el.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String)
                return msg.GetString();
            if (el.TryGetProperty("error", out var err) && err.ValueKind == JsonValueKind.String)
                return err.GetString();
            return null;
        }

        static int? TryGetInvoiceId(JsonElement el)
        {
            if (el.TryGetProperty("faturaId", out var faturaIdProp) && faturaIdProp.ValueKind == JsonValueKind.Number)
                return faturaIdProp.GetInt32();
            if (el.TryGetProperty("invoiceId", out var invIdProp) && invIdProp.ValueKind == JsonValueKind.Number)
                return invIdProp.GetInt32();
            if (el.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.Number)
                return idProp.GetInt32();
            return null;
        }

        static bool? TryGetSuccess(JsonElement el)
        {
            if (el.TryGetProperty("basarili", out var basarili)
                && (basarili.ValueKind == JsonValueKind.True || basarili.ValueKind == JsonValueKind.False))
                return basarili.GetBoolean();
            if (el.TryGetProperty("success", out var success)
                && (success.ValueKind == JsonValueKind.True || success.ValueKind == JsonValueKind.False))
                return success.GetBoolean();
            return null;
        }

        try
        {
            // 🔍 DETAYLI LOGLAMA: Mapping öncesi
            _logger.LogInformation(
                "🔄 Mapping başlatılıyor. CustomerId={CustomerId}, CustomerTitle={CustomerTitle}, LucaCode={LucaCode}, TaxNo={TaxNo}, Lines={LineCount}",
                order.Customer.Id, order.Customer.Title, order.Customer.LucaCode, order.Customer.TaxNo, order.Lines?.Count ?? 0);

            var request = MappingHelper.MapToLucaInvoiceFromSalesOrder(order, order.Customer, depoKodu);
            
            // 🔍 DETAYLI LOGLAMA: Mapping sonrası
            _logger.LogInformation(
                "✅ Mapping tamamlandı. CariKodu={CariKodu}, ParaBirimKod={ParaBirimKod}, KurBedeli={KurBedeli}, DetayCount={DetayCount}",
                request.CariKodu, request.ParaBirimKod, request.KurBedeli, request.DetayList?.Count ?? 0);

            var response = await CreateInvoiceRawAsync(request);

            var success = TryGetSuccess(response);
            var invoiceId = TryGetInvoiceId(response);
            var message = TryGetMessage(response);

            var isOk = success ?? invoiceId.HasValue;
            
                if (isOk)
                {
                    _logger.LogInformation(
                        "✅ Luca fatura başarıyla oluşturuldu. OrderId={OrderId}, OrderNo={OrderNo}, LucaInvoiceId={LucaInvoiceId}, DetayCount={DetayCount}",
                        order.Id, order.OrderNo, invoiceId, request.DetayList?.Count ?? 0);
                }
                else
                {
                    _logger.LogError(
                        "❌ Luca fatura oluşturma başarısız. OrderId={OrderId}, OrderNo={OrderNo}, Error={Error}, DetayCount={DetayCount}",
                        order.Id, order.OrderNo, message ?? "Bilinmeyen hata", request.DetayList?.Count ?? 0);
            }

            return new SalesOrderSyncResultDto
            {
                IsSuccess = isOk,
                Message = isOk ? "Luca satış faturası oluşturuldu" : "Luca satış faturası oluşturma başarısız",
                LucaOrderId = invoiceId,
                SyncedAt = DateTime.UtcNow,
                ErrorDetails = isOk ? null : (message ?? "Bilinmeyen Luca hatası")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "❌ CreateSalesOrderInvoiceAsync exception. OrderId={OrderId}, OrderNo={OrderNo}, Currency={Currency}, ConversionRate={ConversionRate}",
                order.Id, order.OrderNo, order.Currency, order.ConversionRate);
            return new SalesOrderSyncResultDto
            {
                IsSuccess = false,
                Message = "Luca satış faturası oluşturma başarısız",
                ErrorDetails = ex.Message,
                SyncedAt = DateTime.UtcNow
            };
        }
    }

    private async Task AuthenticateAsync()
    {
        try
        {
            _logger.LogInformation("Authenticating with Luca API");

            var authRequest = new
            {
                username = _settings.Username,
                password = _settings.Password
            };

            var json = JsonSerializer.Serialize(authRequest, _jsonOptions);
            var content = CreateKozaContent(json);

            var response = await _httpClient.PostAsync(_settings.Endpoints.Auth, content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var authResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                _authToken = authResponse.GetProperty("token").GetString();
                var expiresIn = authResponse.GetProperty("expiresIn").GetInt32();
                _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60); 

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);

                _logger.LogInformation("Successfully authenticated with Luca API");
            }
            else
            {
                _logger.LogError("Failed to authenticate with Luca API. Status: {StatusCode}", response.StatusCode);
                throw new UnauthorizedAccessException("Failed to authenticate with Luca API");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating with Luca API");
            throw;
        }
    }
    public async Task<SyncResultDto> SendInvoicesAsync(List<LucaCreateInvoiceHeaderRequest> invoices)
    {
        var result = new SyncResultDto
        {
            SyncType = "INVOICE",
            ProcessedRecords = invoices.Count
        };

        var startTime = DateTime.UtcNow;
        try
        {
            await EnsureAuthenticatedAsync();

            if (_settings.UseTokenAuth)
            {
                return await SendInvoicesWithTokenAsync(invoices, result, startTime);
            }

            await EnsureBranchSelectedAsync();
            await VerifyBranchSelectionAsync();

            return await SendInvoicesViaKozaAsync(invoices, result, startTime);
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.FailedRecords = invoices.Count;
            result.Message = ex.Message;
            result.Errors.Add(ex.ToString());
            result.Duration = DateTime.UtcNow - startTime;

            _logger.LogError(ex, "Error sending invoices to Luca");
            return result;
        }
    }
    public Task<SyncResultDto> SendInvoiceAsync(LucaCreateInvoiceHeaderRequest invoice) =>
        SendInvoicesAsync(new List<LucaCreateInvoiceHeaderRequest> { invoice });

    private async Task<SyncResultDto> SendInvoicesWithTokenAsync(
        List<LucaCreateInvoiceHeaderRequest> invoices,
        SyncResultDto result,
        DateTime startTime)
    {
        _logger.LogInformation("Sending {Count} invoices to Luca (token mode)", invoices.Count);

        var legacyInvoices = ConvertToLegacyInvoices(invoices);
        EnsureInvoiceDefaults(legacyInvoices);

        var json = JsonSerializer.Serialize(legacyInvoices, _jsonOptions);
        var content = CreateKozaContent(json);

        var response = await _httpClient.PostAsync(_settings.Endpoints.Invoices, content);
        if (response.IsSuccessStatusCode)
        {
            result.IsSuccess = true;
            result.SuccessfulRecords = invoices.Count;
            result.Message = "Invoices sent successfully to Luca";
            _logger.LogInformation("Successfully sent {Count} invoices to Luca", invoices.Count);
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            result.IsSuccess = false;
            result.FailedRecords = invoices.Count;
            result.Message = $"Failed to send invoices to Luca: {response.StatusCode}";
            result.Errors.Add(errorContent);
            _logger.LogError("Failed to send invoices to Luca. Status: {StatusCode}, Error: {Error}", response.StatusCode, errorContent);
        }

        result.Duration = DateTime.UtcNow - startTime;
        return result;
    }
    private async Task<SyncResultDto> SendInvoicesViaKozaAsync(
        List<LucaCreateInvoiceHeaderRequest> invoices,
        SyncResultDto result,
        DateTime startTime)
    {
        _logger.LogInformation("Sending {Count} invoices to Luca (Koza)", invoices.Count);
        var client = _cookieHttpClient ?? _httpClient;
        var endpoint = _settings.Endpoints.InvoiceCreate;
        var encoder = _encoding;
        var success = 0;
        var failed = 0;

        foreach (var invoice in invoices)
        {
            var label = ResolveInvoiceLabel(invoice);
            try
            {
                // 🚨 HARD GUARD: Kritik alanları Luca'ya göndermeden ÖNCE kontrol et
                if (string.IsNullOrWhiteSpace(invoice.CariAd))
                {
                    _logger.LogError(
                        "🚨 BLOCKED invoice send: CariAd is EMPTY. BelgeTakipNo={BelgeTakipNo}",
                        invoice.BelgeTakipNo);
                    throw new InvalidOperationException("CariAd zorunlu alan ve boş olamaz");
                }
                if (string.IsNullOrWhiteSpace(invoice.CariKodu))
                {
                    _logger.LogError(
                        "🚨 BLOCKED invoice send: CariKodu is EMPTY. BelgeTakipNo={BelgeTakipNo}",
                        invoice.BelgeTakipNo);
                    throw new InvalidOperationException("CariKodu zorunlu alan ve boş olamaz");
                }
                if (invoice.DetayList == null || invoice.DetayList.Count == 0)
                {
                    _logger.LogError(
                        "🚨 BLOCKED invoice send: DetayList is EMPTY. BelgeTakipNo={BelgeTakipNo}",
                        invoice.BelgeTakipNo);
                    throw new InvalidOperationException("DetayList zorunlu alan ve boş olamaz");
                }

                NormalizeInvoiceCreateRequest(invoice);
                var payload = JsonSerializer.Serialize(invoice, _jsonOptions);
                var content = new ByteArrayContent(encoder.GetBytes(payload));
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
                {
                    CharSet = _encoding.WebName
                };

                    using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
                    {
                        Content = content
                    };
                        ApplyManualSessionCookie(httpRequest);

                        var response = await SendWithAuthRetryAsync(httpRequest, "SEND_INVOICE", 2);
                var responseBody = await ReadResponseContentAsync(response);
                await AppendRawLogAsync("SEND_INVOICE", endpoint, payload, response.StatusCode, responseBody);

                if (NeedsBranchSelection(responseBody))
                {
                    _logger.LogWarning("Invoice {InvoiceLabel} failed due to missing branch selection. Re-authenticating and retrying once.", label);
                    MarkSessionUnauthenticated();
                    await EnsureAuthenticatedAsync();
                    await EnsureBranchSelectedAsync();

                    using var retryRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
                    {
                        Content = new ByteArrayContent(encoder.GetBytes(payload))
                        {
                            Headers =
                            {
                                ContentType = new MediaTypeHeaderValue("application/json")
                                {
                                    CharSet = _encoding.WebName
                                }
                            }
                        }
                    };
                    ApplyManualSessionCookie(retryRequest);

                    response = await (_cookieHttpClient ?? _httpClient).SendAsync(retryRequest);
                    responseBody = await ReadResponseContentAsync(response);
                    await AppendRawLogAsync("SEND_INVOICE_RETRY", endpoint, payload, response.StatusCode, responseBody);
                }

                // 🔥 HTML response kontrolü (session timeout) - SendWithAuthRetryAsync içinde zaten var
                // Burada tekrar kontrol etmeye gerek yok, SendWithAuthRetryAsync zaten HTML'i yakalıyor

                if (!response.IsSuccessStatusCode)
                {
                    failed++;
                    result.Errors.Add($"{label}: HTTP {response.StatusCode} - {responseBody}");
                    _logger.LogError("Invoice {InvoiceLabel} failed HTTP {Status}: {Body}", label, response.StatusCode, responseBody);
                    continue;
                }

                var (isSuccess, message) = ParseKozaOperationResponse(responseBody);
                if (!isSuccess)
                {
                    failed++;
                    result.Errors.Add($"{label}: {message}");
                    _logger.LogError("Invoice {InvoiceLabel} failed: {Message}", label, message);
                    continue;
                }

                success++;
                _logger.LogInformation("Invoice {InvoiceLabel} sent successfully", label);
            }
            catch (Exception ex)
            {
                failed++;
                result.Errors.Add($"{label}: {ex.Message}");
                _logger.LogError(ex, "Error sending invoice {InvoiceLabel}", label);
            }
            // 🚀 Minimal delay - hızlandırıldı
            await Task.Delay(15);
        }
        result.SuccessfulRecords = success;
        result.FailedRecords = failed;
        result.IsSuccess = failed == 0;
        result.Message = failed == 0
            ? "Invoices sent successfully to Luca"
            : $"{success} succeeded, {failed} failed";
        result.Duration = DateTime.UtcNow - startTime;
        return result;
    }
    private List<LucaInvoiceDto> ConvertToLegacyInvoices(IEnumerable<LucaCreateInvoiceHeaderRequest> invoices)
    {
        var list = new List<LucaInvoiceDto>();
        foreach (var invoice in invoices)
            {
                var dto = new LucaInvoiceDto
                {
                    GnlOrgSsBelge = new LucaBelgeDto
                    {
                        BelgeSeri = invoice.BelgeSeri ?? _settings.DefaultBelgeSeri ?? "EFA2025",
                        BelgeNo = int.TryParse(invoice.BelgeNo, out var belgeNoInt) ? belgeNoInt : (int?)null,
                        BelgeTarihi = ParseDateOrDefault(invoice.BelgeTarihi),
                        VadeTarihi = ParseDateOrDefault(invoice.VadeTarihi),
                        BelgeTurDetayId = ParseLong(invoice.BelgeTurDetayId)
                    },
                    FaturaTur = ParseInt(invoice.FaturaTur),
                    ParaBirimKod = invoice.ParaBirimKod ?? "TRY",
                    KurBedeli = invoice.KurBedeli,
                    MusteriTedarikci = ParseInt(invoice.MusteriTedarikci),
                    CariKodu = invoice.CariKodu,
                    CariTanim = invoice.CariTanim,
                    CariTip = invoice.CariTip,
                    CariKisaAd = invoice.CariKisaAd,
                    CariYasalUnvan = invoice.CariYasalUnvan,
                    VergiNo = invoice.VergiNo,
                    AdresSerbest = invoice.AdresSerbest,
                    KdvFlag = invoice.KdvFlag,
                    ReferansNo = invoice.ReferansNo
                };

                dto.DocumentNo = invoice.BelgeTakipNo ?? invoice.BelgeNo?.ToString() ?? string.Empty;
                var belgeDate = dto.GnlOrgSsBelge?.BelgeTarihi ?? ParseDateOrDefault(invoice.BelgeTarihi);
                dto.DocumentDate = belgeDate == default ? DateTime.UtcNow : belgeDate;
                var dueDate = ParseDateOrDefault(invoice.VadeTarihi);
                dto.DueDate = dueDate == default ? DateTime.UtcNow : dueDate;
                dto.CustomerTitle = invoice.CariAd ?? invoice.CariTanim ?? invoice.CariYasalUnvan ?? string.Empty;
                dto.CustomerCode = invoice.CariKodu ?? string.Empty;
                dto.CustomerTaxNo = invoice.VergiNo ?? string.Empty;
                dto.Lines = invoice.DetayList?.Select(ConvertToLegacyInvoiceLine).ToList() ?? new List<LucaInvoiceItemDto>();
                dto.NetAmount = dto.Lines.Sum(l => l.NetAmount);
                dto.TaxAmount = dto.Lines.Sum(l => l.TaxAmount);
                dto.GrossAmount = dto.Lines.Sum(l => l.GrossAmount);

            list.Add(dto);
        }

        return list;
    }

    private DateTime ParseDateOrDefault(string? value)
    {
        if (DateTime.TryParse(value, out var dt)) return dt;
        return DateTime.UtcNow;
    }

    private DateTime? ParseDateOrDefault(DateTime? value)
    {
        return value == default ? DateTime.UtcNow : value;
    }

    private long ParseLong(string? value)
    {
        return long.TryParse(value, out var num) ? num : 0;
    }

    private int ParseInt(string? value)
    {
        return int.TryParse(value, out var num) ? num : 0;
    }
    private LucaInvoiceItemDto ConvertToLegacyInvoiceLine(LucaCreateInvoiceDetailRequest detail)
    {
        var netAmount = detail.Tutar.HasValue
            ? Convert.ToDecimal(detail.Tutar.Value)
            : Convert.ToDecimal(detail.BirimFiyat * detail.Miktar);
        var taxAmount = netAmount * Convert.ToDecimal(detail.KdvOran);

        return new LucaInvoiceItemDto
        {
            ProductCode = detail.KartKodu,
            Description = detail.KartAdi ?? detail.KartKodu,
            Quantity = Convert.ToDecimal(detail.Miktar),
            Unit = "ADET",
            UnitPrice = Convert.ToDecimal(detail.BirimFiyat),
            NetAmount = netAmount,
            TaxRate = Convert.ToDecimal(detail.KdvOran),
            TaxAmount = taxAmount,
            GrossAmount = netAmount + taxAmount,
            AccountCode = detail.HesapKod ?? string.Empty
        };
    }
    private static string ResolveInvoiceLabel(LucaCreateInvoiceHeaderRequest invoice)
    {
        if (!string.IsNullOrWhiteSpace(invoice.BelgeTakipNo))
        {
            return invoice.BelgeTakipNo;
        }

        if (!string.IsNullOrWhiteSpace(invoice.BelgeNo))
        {
            return $"{invoice.BelgeSeri ?? "EFA2025"}-{invoice.BelgeNo}";
        }

        return "INVOICE";
    }
    private static (bool IsSuccess, string? Message) ParseKozaOperationResponse(string? responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return (false, "Empty response from Luca");
        }
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("code", out var codeElement))
            {
                var code = codeElement.GetInt32();
                if (code == 0)
                {
                    return (true, null);
                }

                var message = root.TryGetProperty("message", out var messageElement)
                    ? messageElement.GetString()
                    : "Unknown error";
                return (false, $"code={code} message={message}");
            }
        }
        catch (JsonException)
        {
            
        }

        return responseBody.Contains("Başar", StringComparison.OrdinalIgnoreCase)
            ? (true, null)
            : (false, responseBody);
    }
    private async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage req)
    {
        var clone = new HttpRequestMessage(req.Method, req.RequestUri);
        foreach (var header in req.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (req.Content != null)
        {
            using var ms = new MemoryStream();
            await req.Content.CopyToAsync(ms);
            var bytes = ms.ToArray();
            var content = new ByteArrayContent(bytes);
            foreach (var header in req.Content.Headers)
            {
                content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
            clone.Content = content;
        }

        return clone;
    }

    private void LogHtmlResponse(string logTag, HttpResponseMessage response, string? responseContent, int attempt, int maxAttempts)
    {
        try
        {
            var content = responseContent ?? string.Empty;
            var trimmed = content.TrimStart();
            if (trimmed.Length == 0 || !trimmed.StartsWith("<", StringComparison.Ordinal))
            {
                return;
            }

            const int maxLogChars = 20000;
            var logBody = trimmed.Length > maxLogChars ? trimmed.Substring(0, maxLogChars) + " ...(truncated)" : trimmed;

            _logger.LogError(
                "Luca API HTML Yanıtı Döndü! Tag={Tag} Url={Url} Status={Status} Attempt={Attempt}/{MaxAttempts} ContentType={ContentType} İçerik: {Content}",
                logTag,
                response.RequestMessage?.RequestUri?.ToString() ?? "(unknown)",
                (int)response.StatusCode,
                attempt,
                maxAttempts,
                response.Content?.Headers?.ContentType?.ToString() ?? "(unknown)",
                logBody);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to log HTML response content for {Tag}", logTag);
        }
    }
    private async Task<HttpResponseMessage> SendWithAuthRetryAsync(HttpRequestMessage request, string logTag, int maxAttempts = 2)
    {
        var attempt = 0;
        while (true)
        {
            attempt++;
            var client = _cookieHttpClient ?? _httpClient;

            // Ensure session cookie is attached even when CookieContainer fails to apply it automatically.
            // ApplySessionCookie uses CookieContainer, ApplyManualSessionCookie can fall back to in-memory/manual cookie values.
            ApplySessionCookie(request);
            ApplyManualSessionCookie(request);

            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "HTTP send failed on attempt {Attempt} for {Tag}", attempt, logTag);
                if (attempt >= maxAttempts) throw;
                MarkSessionUnauthenticated();
                await EnsureAuthenticatedAsync();
                await EnsureBranchSelectedAsync();
                request = await CloneHttpRequestMessageAsync(request);
                continue;
            }

            var body = await ReadResponseContentAsync(response);
            var preview = request.Content != null ? await ReadContentPreviewAsync(request.Content) : string.Empty;
            await AppendRawLogAsync(logTag + (attempt > 1 ? $"_RETRY{attempt}" : string.Empty), request.RequestUri?.ToString() ?? string.Empty, preview, response.StatusCode, body);

            var needsBranch = !_settings.UseTokenAuth && NeedsBranchSelection(body);
            var bodyLower = (body ?? string.Empty).ToLowerInvariant();
            var bodyIndicatesLogin = false;
            var actionInstantiateError = false;
            if (IsHtmlResponse(body))
            {
                _logger.LogWarning("⚠️ {Tag}: HTML response alındı (session timeout). Attempt {Attempt}/{MaxAttempts}", logTag, attempt, maxAttempts);
                LogHtmlResponse(logTag, response, body, attempt, maxAttempts);

                if (attempt >= maxAttempts)
                {
                    _logger.LogError("❌ {Tag}: {MaxAttempts} denemeden sonra hala HTML response alınıyor!", logTag, maxAttempts);
                    throw new InvalidOperationException($"{logTag}: HTML response received after {attempt} attempts (session/login failure). Luca session süresi dolmuş olabilir.");
                }

                _logger.LogInformation("🔄 Session yenileniyor (attempt {Attempt})...", attempt);
                
                try
                {
                    // ForceSessionRefreshAsync tüm state'i temizler ve yeniden login yapar
                    await ForceSessionRefreshAsync();
                    _logger.LogInformation("✅ Session yenilendi");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ Session refresh hatası, tekrar denenecek");
                }

                await Task.Delay(500 * attempt); // Kısa bekleme
                request = await CloneHttpRequestMessageAsync(request);
                continue;
            }
            try
            {
                if (!string.IsNullOrWhiteSpace(bodyLower) && !_settings.UseTokenAuth)
                {
                    actionInstantiateError = bodyLower.Contains("unable to instantiate action") || bodyLower.Contains("stkwshareketaction");
                    if (bodyLower.Contains("login olunmalı") || bodyLower.Contains("login olunmali") || bodyLower.Contains("\"code\":1001") || bodyLower.Contains("\"code\":1002") || bodyLower.Contains("1001") || bodyLower.Contains("1002") || actionInstantiateError)
                    {
                        bodyIndicatesLogin = true;
                    }
                }
            }
            catch (Exception) { }

            if (response.IsSuccessStatusCode && !needsBranch && !bodyIndicatesLogin)
            {
                return response;
            }
            if ((response.StatusCode == HttpStatusCode.Unauthorized || needsBranch || bodyIndicatesLogin) && attempt < maxAttempts)
            {
                string? trafficFile = null;
                try
                {
                    trafficFile = await SaveHttpTrafficAndGetFilePathAsync(logTag + (attempt > 1 ? $"_RETRY{attempt}" : string.Empty), request, response);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to persist traffic before retry");
                }

                _logger.LogWarning("{Tag}: attempt {Attempt} failed due to authentication/branch or Koza login-needed marker; re-authenticating and retrying. Preview: {Preview} TrafficFile: {TrafficFile}", logTag, attempt, (body ?? string.Empty).Length > 300 ? (body ?? string.Empty).Substring(0, 300) : (body ?? string.Empty), trafficFile ?? "(none)");
                MarkSessionUnauthenticated();
                await EnsureAuthenticatedAsync();
                await EnsureBranchSelectedAsync();
                request = await CloneHttpRequestMessageAsync(request);
                continue;
            }
            return response;
        }
    }
    private async Task<SyncResultDto> SendCustomersWithTokenAsync(
        List<LucaCreateCustomerRequest> customers,
        SyncResultDto result,
        DateTime startTime)
    {
        _logger.LogInformation("Sending {Count} customers to Luca (token mode)", customers.Count);

        var legacyCustomers = ConvertToLegacyCustomers(customers);
        var json = JsonSerializer.Serialize(legacyCustomers, _jsonOptions);
        var content = CreateKozaContent(json);

        var response = await _httpClient.PostAsync(_settings.Endpoints.Customers, content);
        if (response.IsSuccessStatusCode)
        {
            result.IsSuccess = true;
            result.SuccessfulRecords = customers.Count;
            result.Message = "Customers sent successfully to Luca";
            _logger.LogInformation("Successfully sent {Count} customers to Luca", customers.Count);
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            result.IsSuccess = false;
            result.FailedRecords = customers.Count;
            result.Message = $"Failed to send customers to Luca: {response.StatusCode}";
            result.Errors.Add(errorContent);
            _logger.LogError("Failed to send customers to Luca. Status: {StatusCode}, Error: {Error}", response.StatusCode, errorContent);
        }

        result.Duration = DateTime.UtcNow - startTime;
        return result;
    }
    private async Task<SyncResultDto> SendCustomersViaKozaAsync(
        List<LucaCreateCustomerRequest> customers,
        SyncResultDto result,
        DateTime startTime)
    {
        _logger.LogInformation("Sending {Count} customers to Luca (Koza)", customers.Count);
        var client = (_cookieHttpClient ?? _httpClient);
        var endpoint = ResolveCustomerCreateEndpoint();
        var success = 0;
        var failed = 0;
        foreach (var customer in customers)
        {
            var label = ResolveCustomerLabel(customer);
            try
            {
                var payload = JsonSerializer.Serialize(customer, _jsonOptions);
                var content = CreateKozaContent(payload);
                var response = await client.PostAsync(endpoint, content);
                var body = await ReadResponseContentAsync(response);
                await AppendRawLogAsync("SEND_CUSTOMER", endpoint, payload, response.StatusCode, body);
                if (!response.IsSuccessStatusCode)
                {
                    failed++;
                    result.Errors.Add($"{label}: HTTP {response.StatusCode} - {body}");
                    _logger.LogError("Customer {Label} failed HTTP {Status}: {Body}", label, response.StatusCode, body);
                    continue;
                }

                var (isSuccess, message) = ParseKozaOperationResponse(body);
                if (!isSuccess)
                {
                    failed++;
                    result.Errors.Add($"{label}: {message}");
                    _logger.LogError("Customer {Label} failed: {Message}", label, message);
                    continue;
                }

                success++;
                _logger.LogInformation("Customer {Label} sent successfully", label);
            }
            catch (Exception ex)
            {
                failed++;
                result.Errors.Add($"{label}: {ex.Message}");
                _logger.LogError(ex, "Error sending customer {Label}", label);
            }
            // 🚀 Minimal delay - hızlandırıldı
            await Task.Delay(10);
        }
        result.SuccessfulRecords = success;
        result.FailedRecords = failed;
        result.IsSuccess = failed == 0;
        result.Message = failed == 0
            ? "Customers sent successfully to Luca"
            : $"{success} succeeded, {failed} failed";
        result.Duration = DateTime.UtcNow - startTime;
        return result;
    }
    private List<LucaCustomerDto> ConvertToLegacyCustomers(IEnumerable<LucaCreateCustomerRequest> customers)
    {
        return customers.Select(c => new LucaCustomerDto
        {
            CustomerCode = string.IsNullOrWhiteSpace(c.KartKod) ? c.Tanim ?? Guid.NewGuid().ToString("N") : c.KartKod,
            Title = c.Tanim ?? c.KartKod ?? string.Empty,
            TaxNo = c.VergiNo ?? c.TcKimlikNo ?? string.Empty,
            ContactPerson = null,
            Phone = c.IletisimTanim,
            Email = null,
            Address = c.AdresSerbest,
            City = c.Il,
            Country = c.Ulke
        }).ToList();
    }
    private static string ResolveCustomerLabel(LucaCreateCustomerRequest customer)
    {
        if (!string.IsNullOrWhiteSpace(customer.KartKod))
        {
            return customer.KartKod;
        }
        if (!string.IsNullOrWhiteSpace(customer.Tanim))
        {
            return customer.Tanim;
        }
        return "CUSTOMER";
    }
    public async Task<SyncResultDto> SendStockMovementsAsync(List<LucaStockDto> stockMovements)
    {
        var result = new SyncResultDto
        {
            SyncType = "STOCK",
            ProcessedRecords = stockMovements.Count
        };
        var startTime = DateTime.UtcNow;
        try
        {
            await EnsureAuthenticatedAsync();
            if (!_settings.UseTokenAuth)
            {
                await EnsureBranchSelectedAsync();
            }
            _logger.LogInformation("Sending {Count} stock movements to Luca", stockMovements.Count);
            var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
            if (_settings.UseTokenAuth)
            {
                var json = JsonSerializer.Serialize(stockMovements, _jsonOptions);
                var content = CreateKozaContent(json);
                var response = await client.PostAsync(_settings.Endpoints.Stock, content);

                if (response.IsSuccessStatusCode)
                {
                    result.IsSuccess = true;
                    result.SuccessfulRecords = stockMovements.Count;
                    result.Message = "Stock movements sent successfully to Luca";
                    _logger.LogInformation("Successfully sent {Count} stock movements to Luca", stockMovements.Count);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    result.IsSuccess = false;
                    result.FailedRecords = stockMovements.Count;
                    result.Message = $"Failed to send stock movements to Luca: {response.StatusCode}";
                    result.Errors.Add(errorContent);
                    _logger.LogError("Failed to send stock movements to Luca. Status: {StatusCode}, Error: {Error}",
                        response.StatusCode, errorContent);
                }
            }
            else
            {
                var succeeded = 0;
                var failed = 0;
                foreach (var movement in stockMovements)
                {
                    var movementLabel = !string.IsNullOrWhiteSpace(movement.Reference)
                        ? movement.Reference
                        : movement.ProductCode ?? "STOCK";
                    try
                    {
                        var payload = JsonSerializer.Serialize(movement, _jsonOptions);

                        // If the serialized movement JSON does not include 'belgeSeri', inject a default
                        try
                        {
                            if (!payload.Contains("\"belgeSeri\"", StringComparison.OrdinalIgnoreCase))
                            {
                                var defaultSeri = string.IsNullOrWhiteSpace(_settings.DefaultBelgeSeri) ? "EFA2025" : _settings.DefaultBelgeSeri.Trim();
                                var quoted = JsonSerializer.Serialize(defaultSeri);
                                // Build new JSON by inserting belgeSeri after opening brace
                                var bodyWithoutOpen = payload.TrimStart();
                                if (bodyWithoutOpen.StartsWith("{"))
                                {
                                    payload = "{" + $"\"belgeSeri\":{quoted}," + bodyWithoutOpen.Substring(1);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Failed to inject default belgeSeri into stock movement payload; proceeding with original payload");
                        }
                        using var req = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.OtherStockMovement)
                        {
                            Content = CreateKozaContent(payload)
                        };
                        var response = await SendWithAuthRetryAsync(req, "SEND_STOCK_MOVEMENT", 2);
                        var body = await ReadResponseContentAsync(response);
                        if (!response.IsSuccessStatusCode)
                        {
                            failed++;
                            result.Errors.Add($"{movementLabel}: HTTP {response.StatusCode} - {body}");
                            _logger.LogError("Stock movement {Doc} failed HTTP {Status}: {Body}", movementLabel, response.StatusCode, body);
                            continue;
                        }
                        
                        try
                        {
                           using var doc = JsonDocument.Parse(body);
                            if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("code", out var codeProp))
                            {
                                var code = codeProp.GetInt32();
                                if (code != 0)
                                {
                                    failed++;
                                    var msg = doc.RootElement.TryGetProperty("message", out var m) ? m.GetString() : "Unknown error";
                                    result.Errors.Add($"{movementLabel}: code={code} message={msg}");
                                    _logger.LogError("Stock movement {Doc} failed with code {Code} message {Message}", movementLabel, code, msg);
                                    continue;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Could not parse stock movement response; assuming success on HTTP OK");
                        }
                        succeeded++;
                        _logger.LogInformation("Stock movement sent: {Doc}", movementLabel);
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        result.Errors.Add($"{movementLabel}: {ex.Message}");
                        _logger.LogError(ex, "Error sending stock movement {Doc}", movementLabel);
                    }
                    // Minimal delay to avoid rate limiting
                    await Task.Delay(25);
                }
                result.SuccessfulRecords = succeeded;
                result.FailedRecords = failed;
                result.IsSuccess = failed == 0;
                result.Message = failed == 0 ? "All stock movements sent successfully (one by one)." : $"{succeeded} succeeded, {failed} failed.";
            }
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.FailedRecords = stockMovements.Count;
            result.Message = ex.Message;
            result.Errors.Add(ex.ToString());
            _logger.LogError(ex, "Error sending stock movements to Luca");
        }

        result.Duration = DateTime.UtcNow - startTime;
        return result;
    }
    public async Task<SyncResultDto> SendCustomersAsync(List<LucaCreateCustomerRequest> customers)
    {
        var result = new SyncResultDto
        {
            SyncType = "CUSTOMER",
            ProcessedRecords = customers.Count
        };

        var startTime = DateTime.UtcNow;

        try
        {
            await EnsureAuthenticatedAsync();

            if (_settings.UseTokenAuth)
            {
                return await SendCustomersWithTokenAsync(customers, result, startTime);
            }
            await EnsureBranchSelectedAsync();
            return await SendCustomersViaKozaAsync(customers, result, startTime);
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.FailedRecords = customers.Count;
            result.Message = ex.Message;
            result.Errors.Add(ex.ToString());
            result.Duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "Error sending customers to Luca");
            return result;
        }
    }
    public async Task<SyncResultDto> SendProductsAsync(List<LucaProductUpdateDto> products)
    {
        var result = new SyncResultDto
        {
            SyncType = "PRODUCT",
            ProcessedRecords = products.Count
        };
        var startTime = DateTime.UtcNow;
        try
        {
            _logger.LogInformation("Sending {Count} products to Luca", products.Count);

            var json = JsonSerializer.Serialize(products, _jsonOptions);
            var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
            var endpoint = _settings.Endpoints.Products;
            var baseUrl = client.BaseAddress?.ToString()?.TrimEnd('/') ?? _settings.BaseUrl?.TrimEnd('/') ?? string.Empty;
            var fullUrl = string.IsNullOrWhiteSpace(baseUrl) ? endpoint : (endpoint.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? endpoint : baseUrl + "/" + endpoint.TrimStart('/'));

            HttpResponseMessage? response = null;
            string responseContent = string.Empty;
            await EnsureAuthenticatedAsync();
            await EnsureBranchSelectedAsync();
            using var prodReq = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = CreateKozaContent(json)
            };
            var prodResp = await SendWithAuthRetryAsync(prodReq, "SEND_PRODUCTS_ATTEMPT", 3);
            response = prodResp;
            responseContent = await ReadResponseContentAsync(response);

            if (response != null && response.IsSuccessStatusCode)
            {
                result.IsSuccess = true;
                result.SuccessfulRecords = products.Count;
                result.Message = "Products sent successfully to Luca";
                _logger.LogInformation("Successfully sent {Count} products to Luca", products.Count);
            }
            else
            {
                result.IsSuccess = false;
                result.FailedRecords = products.Count;
                result.Message = $"Failed to send products to Luca: {response?.StatusCode}";
                result.Errors.Add(responseContent);
                _logger.LogError("Failed to send products to Luca. Status: {StatusCode}, Error: {Error}",
                    response?.StatusCode, responseContent);
            }
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.FailedRecords = products.Count;
            result.Message = ex.Message;
            result.Errors.Add(ex.ToString());
            _logger.LogError(ex, "Error sending products to Luca");
        }

        result.Duration = DateTime.UtcNow - startTime;
        return result;
    }
    public async Task<long> CreateIrsaliyeAsync(LucaIrsaliyeDto dto)
    {
        await EnsureAuthenticatedAsync();
        var json = JsonSerializer.Serialize(dto, _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.IrsaliyeCreate, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("CreateIrsaliyeAsync failed with status {Status}: {Body}", response.StatusCode, responseContent);
        }

        return TryParseId(responseContent);
    }
    public async Task<SyncResultDto> SendStockCardsAsync(string sessionId, List<LucaCreateStokKartiRequest> stockCards)
    {
        // Backwards-compat shim: some callers in older deployments may still invoke
        // the legacy session-based overload. Forward to the per-product implementation
        // to preserve compatibility and avoid runtime NotSupportedExceptions.
        _logger?.LogWarning("Legacy SendStockCardsAsync(sessionId, ...) called. Forwarding to per-product SendStockCardsAsync(List<...>). SessionId will be ignored.");
        return await SendStockCardsAsync(stockCards);
    }
    public async Task DeleteIrsaliyeAsync(long irsaliyeId)
    {
        await EnsureAuthenticatedAsync();
        var payload = new LucaDeleteIrsaliyeRequest { SsIrsaliyeBaslikId = irsaliyeId };
        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        await client.PostAsync(_settings.Endpoints.IrsaliyeDelete, CreateKozaContent(json));
    }
    public async Task<long> CreateSatinalmaSiparisAsync(LucaSatinalmaSiparisDto dto)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(dto, _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.PurchaseOrder, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("CreateSatinalmaSiparisAsync failed with status {Status}: {Body}", response.StatusCode, responseContent);
        }

        return TryParseId(responseContent);
    }
    public async Task DeleteSatinalmaSiparisAsync(long siparisId)
    {
        await EnsureAuthenticatedAsync();
        var payload = new LucaDeletePurchaseOrderRequest { SsSiparisBaslikId = siparisId };
        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        await client.PostAsync(_settings.Endpoints.PurchaseOrderDelete, CreateKozaContent(json));
    }
    public async Task<long> CreateDepoTransferAsync(LucaDepoTransferDto dto)
    {
        await EnsureAuthenticatedAsync();
        var json = JsonSerializer.Serialize(dto, _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.WarehouseTransfer, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("CreateDepoTransferAsync failed with status {Status}: {Body}", response.StatusCode, responseContent);
        }
        return TryParseId(responseContent);
    }
    public async Task<List<LucaTedarikciDto>> GetTedarikciListAsync()
    {
        var result = new List<LucaTedarikciDto>();
        var jsonElement = await ListSuppliersAsync();
        if (jsonElement.ValueKind == JsonValueKind.Array)
        {
            result = JsonSerializer.Deserialize<List<LucaTedarikciDto>>(jsonElement.GetRawText(), _jsonOptions) ?? new List<LucaTedarikciDto>();
        }
        else if (jsonElement.ValueKind == JsonValueKind.Object)
        {
            if (jsonElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
            {
                result = JsonSerializer.Deserialize<List<LucaTedarikciDto>>(data.GetRawText(), _jsonOptions) ?? new List<LucaTedarikciDto>();
            }
            else if (jsonElement.TryGetProperty("finTedarikciListesi", out var finTedarikciListesi) && finTedarikciListesi.ValueKind == JsonValueKind.Array)
            {
                result = JsonSerializer.Deserialize<List<LucaTedarikciDto>>(finTedarikciListesi.GetRawText(), _jsonOptions) ?? new List<LucaTedarikciDto>();
            }
            else if (jsonElement.TryGetProperty("list", out var list) && list.ValueKind == JsonValueKind.Array)
            {
                result = JsonSerializer.Deserialize<List<LucaTedarikciDto>>(list.GetRawText(), _jsonOptions) ?? new List<LucaTedarikciDto>();
            }
        }
        return result;
    }
    public async Task<long> CreateTedarikciAsync(LucaCreateSupplierRequest dto)
    {
        await EnsureAuthenticatedAsync();
        var json = JsonSerializer.Serialize(dto, _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;

        var response = await client.PostAsync(_settings.Endpoints.SupplierCreate, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("CreateTedarikciAsync failed with status {Status}: {Body}", response.StatusCode, responseContent);
        }
        return TryParseId(responseContent);
    }
    public async Task<long> CreateCariHareketAsync(LucaCariHareketDto dto)
    {
        await EnsureAuthenticatedAsync();
        var json = JsonSerializer.Serialize(dto, _jsonOptions);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.CustomerTransaction, CreateKozaContent(json));
        var responseContent = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("CreateCariHareketAsync failed with status {Status}: {Body}", response.StatusCode, responseContent);
        }
        return TryParseId(responseContent);
    }
    public async Task<long> CreateFaturaKapamaAsync(LucaFaturaKapamaDto dto, long belgeTurDetayId)
    {
        await EnsureAuthenticatedAsync();
        ValidateFaturaKapama(dto, belgeTurDetayId);

        var json = JsonSerializer.Serialize(dto, _jsonOptions);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.InvoiceClose, CreateKozaContent(json));
        var responseContent = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("CreateFaturaKapamaAsync failed with status {Status}: {Body}", response.StatusCode, responseContent);
        }
        return TryParseId(responseContent);
    }
    // GetDepoListAsync metodu LucaService.Depots.cs dosyasına taşındı
    public async Task<List<LucaVergiDairesiDto>> GetVergiDairesiListAsync()
    {
        var element = await ListTaxOfficesAsync();
        return DeserializeList<LucaVergiDairesiDto>(element);
    }
    public async Task<List<LucaOlcumBirimiDto>> GetOlcumBirimiListAsync()
    {
        var element = await ListMeasurementUnitsAsync();
        return DeserializeList<LucaOlcumBirimiDto>(element);
    }
    public async Task<List<LucaMeasurementUnitDto>> GetMeasurementUnitsAsync()
    {
        var element = await ListMeasurementUnitsAsync();
        return DeserializeList<LucaMeasurementUnitDto>(element);
    }
    public async Task<List<LucaWarehouseDto>> GetWarehousesAsync()
    {
        var element = await ListWarehousesAsync();
        return DeserializeList<LucaWarehouseDto>(element);
    }
    public async Task<List<LucaBranchDto>> GetBranchesAsync()
    {
        await EnsureAuthenticatedAsync();
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var req = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.Branches)
        {
            Content = CreateKozaContent("{}")
        };
        ApplySessionCookie(req);
        ApplyManualSessionCookie(req);
        var response = await client.SendAsync(req);
        var body = await ReadResponseContentAsync(response);
        await AppendRawLogAsync("LIST_BRANCHES", _settings.Endpoints.Branches, "{}", response.StatusCode, body);
        response.EnsureSuccessStatusCode();
        var branches = new List<LucaBranchDto>();
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            JsonElement arrayEl = default;
            if (root.ValueKind == JsonValueKind.Array)
            {
                arrayEl = root;
            }
            else
            {
                foreach (var wrapper in new[] { "list", "data", "branches", "items", "sirketSubeList" })
                {
                    if (root.TryGetProperty(wrapper, out var prop) && prop.ValueKind == JsonValueKind.Array)
                    {
                        arrayEl = prop;
                        break;
                    }
                }
            }

            if (arrayEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in arrayEl.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object) continue;
                    if (TryExtractBranchId(item, out var id))
                    {
                        branches.Add(new LucaBranchDto
                        {
                            Id = id,
                            Ack = TryGetProperty(item, "ack"),
                            Tanim = TryGetProperty(item, "tanim", "name", "ad")
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse branch list; returning empty list");
        }
        return branches;
    }
    public async Task<JsonElement> ListTaxOfficesAsync(LucaListTaxOfficesRequest? request = null)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request ?? new LucaListTaxOfficesRequest(), _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.TaxOffices)
        {
            Content = content
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);
        httpRequest.Headers.Add("No-Paging", "true");
        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> ListMeasurementUnitsAsync(LucaListMeasurementUnitsRequest? request = null)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request ?? new LucaListMeasurementUnitsRequest(), _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.MeasurementUnits)
        {
            Content = content
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);
        httpRequest.Headers.Add("No-Paging", "true");
        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> ListDocumentTypeDetailsAsync(LucaListDocumentTypeDetailsRequest? request = null)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request ?? new LucaListDocumentTypeDetailsRequest(), _jsonOptions);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.DocumentTypeDetails)
        {
            Content = CreateKozaContent(json)
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);
        httpRequest.Headers.TryAddWithoutValidation("No-Paging", "true");
        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> ListDocumentSeriesAsync(LucaListDocumentSeriesRequest request)
    {
        await EnsureAuthenticatedAsync();

        var effectiveRequest = request ?? new LucaListDocumentSeriesRequest();
        var json = JsonSerializer.Serialize(effectiveRequest, _jsonOptions);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.DocumentSeries)
        {
            Content = CreateKozaContent(json)
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);
        httpRequest.Headers.TryAddWithoutValidation("No-Paging", "true");
        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> ListBranchCurrenciesAsync(LucaListBranchCurrenciesRequest request)
    {
        await EnsureAuthenticatedAsync();

        var effectiveRequest = request ?? new LucaListBranchCurrenciesRequest();
        var json = JsonSerializer.Serialize(effectiveRequest, _jsonOptions);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.BranchCurrencies)
        {
            Content = CreateKozaContent(json)
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);
        httpRequest.Headers.TryAddWithoutValidation("No-Paging", "true");
        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> GetDocumentSeriesMaxAsync(LucaGetDocumentSeriesMaxRequest request)
    {
        await EnsureAuthenticatedAsync();

        var effectiveRequest = request ?? new LucaGetDocumentSeriesMaxRequest();
        var json = JsonSerializer.Serialize(effectiveRequest, _jsonOptions);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.DocumentSeriesMax)
        {
            Content = CreateKozaContent(json)
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);
        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> ListDynamicLovValuesAsync(LucaListDynamicLovValuesRequest request)
    {
        await EnsureAuthenticatedAsync();
        var effectiveRequest = request ?? new LucaListDynamicLovValuesRequest();
        var json = JsonSerializer.Serialize(effectiveRequest, _jsonOptions);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.DynamicLovValueList)
        {
            Content = CreateKozaContent(json)
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);
        httpRequest.Headers.TryAddWithoutValidation("No-Paging", "true");
        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> UpdateDynamicLovValueAsync(LucaUpdateDynamicLovValueRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        await EnsureAuthenticatedAsync();
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.DynamicLovValueUpdate)
        {
            Content = CreateKozaContent(json)
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);
        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> CreateDynamicLovValueAsync(LucaCreateDynamicLovRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        await EnsureAuthenticatedAsync();
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.DynamicLovValueCreate)
        {
            Content = CreateKozaContent(json)
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);
        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> UpdateAttributeAsync(LucaUpdateAttributeRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        await EnsureAuthenticatedAsync();
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.AttributeUpdate)
        {
            Content = CreateKozaContent(json)
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);
        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> ListCustomerTransactionsAsync(LucaListCariHareketBaslikRequest request, bool detayliListe = false)
    {
        await EnsureAuthenticatedAsync();
        var effectiveRequest = request ?? new LucaListCariHareketBaslikRequest();
        var json = JsonSerializer.Serialize(effectiveRequest, _jsonOptions);
        var url = _settings.Endpoints.CustomerTransactionList;
        if (detayliListe)
        {
            url = url.Contains('?', StringComparison.Ordinal)
                ? url + "&detayliListe=true"
                : url + "?detayliListe=true";
        }

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = CreateKozaContent(json)
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);
        httpRequest.Headers.TryAddWithoutValidation("No-Paging", "true");
        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> ListSpecialCustomerTransactionsAsync(LucaListOzelCariHareketBaslikRequest? request = null)
    {
        await EnsureAuthenticatedAsync();
        var effectiveRequest = request ?? new LucaListOzelCariHareketBaslikRequest();
        var json = JsonSerializer.Serialize(effectiveRequest, _jsonOptions);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.SpecialCustomerTransactionList)
        {
            Content = CreateKozaContent(json)
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);
        httpRequest.Headers.TryAddWithoutValidation("No-Paging", "true");
        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> CreateCustomerContractAsync(LucaCreateCustomerContractRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        await EnsureAuthenticatedAsync();
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.CustomerContractCreate)
        {
            Content = CreateKozaContent(json)
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);
        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> ListStockCardsAutoCompleteAsync(LucaStockCardAutoCompleteRequest request)
    {
        await EnsureAuthenticatedAsync();
        var effectiveRequest = request ?? new LucaStockCardAutoCompleteRequest();
        var queryParams = new List<string>();

        void AppendParam(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                queryParams.Add($"{key}={WebUtility.UrlEncode(value)}");
            }
        }

        void AppendParamInt(string key, int? value)
        {
            if (value.HasValue)
            {
                queryParams.Add($"{key}={value.Value}");
            }
        }

        AppendParamInt("kartTuru", effectiveRequest.KartTuru);
        AppendParam("q", effectiveRequest.Query);
        AppendParamInt("pageNo", effectiveRequest.PageNo);
        AppendParamInt("pageSize", effectiveRequest.PageSize);
        AppendParamInt("autoComplete", effectiveRequest.AutoComplete);
        AppendParamInt("displayTagSize", effectiveRequest.DisplayTagSize);

        var url = _settings.Endpoints.StockCardAutoComplete;
        if (queryParams.Count > 0)
        {
            url += url.Contains("?", StringComparison.Ordinal) ? "&" : "?";
            url += string.Join("&", queryParams);
        }

        var json = JsonSerializer.Serialize(effectiveRequest, _jsonOptions);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = CreateKozaContent(json)
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);
        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> NotifyUtsAsync(LucaUtsTransmitRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        await EnsureAuthenticatedAsync();
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.UtsTransmit)
        {
            Content = CreateKozaContent(json)
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);
        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<byte[]> GenerateStockServiceReportAsync(LucaDynamicStockServiceReportRequest request)
    {
        await EnsureAuthenticatedAsync();
        var effective = request ?? new LucaDynamicStockServiceReportRequest();
        var queryParams = new List<string>();

        void Add(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                queryParams.Add($"{key}={Uri.EscapeDataString(value)}");
            }
        }

        void AddInt(string key, int? value)
        {
            if (value.HasValue)
            {
                queryParams.Add($"{key}={value.Value}");
            }
        }

        Add("parSiralamaKriteri", effective.ParSiralamaKriteri);
        Add("parStokKartTuru", effective.ParStokKartTuru);
        Add("basStokKodAd_comp", effective.BasStokKodAdComp);
        Add("basStokKodAd_comp_ack", effective.BasStokKodAdCompAck);
        Add("bitStokKodAd_comp", effective.BitStokKodAdComp);
        Add("bitStokKodAd_comp_ack", effective.BitStokKodAdCompAck);
        Add("parBaslangicStokKodu", effective.ParBaslangicStokKodu);
        Add("parBitisStokKodu", effective.ParBitisStokKodu);
        Add("raporFormat", effective.RaporFormat);
        Add("request_locale", effective.RequestLocale);
        Add("menuItemIslemKod", effective.MenuItemIslemKod);
        AddInt("dovizGetir", effective.DovizGetir);

        var url = _settings.Endpoints.StockServiceReport;
        if (queryParams.Count > 0)
        {
            url += url.Contains("?", StringComparison.Ordinal) ? "&" : "?";
            url += string.Join("&", queryParams);
        }

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = CreateKozaContent("{}")
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);
        var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task<JsonElement> ListCustomersAsync(LucaListCustomersRequest? request = null)
    {
        await EnsureAuthenticatedAsync();
        var json = JsonSerializer.Serialize(request ?? new LucaListCustomersRequest(), _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.CustomerList)
        {
            Content = content
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);
        httpRequest.Headers.Add("No-Paging", "true");
        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> ListSuppliersAsync(LucaListSuppliersRequest? request = null)
    {
        await EnsureAuthenticatedAsync();
        var json = JsonSerializer.Serialize(request ?? new LucaListSuppliersRequest(), _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.SupplierList)
        {
            Content = content
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);
        httpRequest.Headers.Add("No-Paging", "true");
        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> ListWarehousesAsync(LucaListWarehousesRequest? request = null)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request ?? new LucaListWarehousesRequest(), _jsonOptions);
        var content = CreateKozaContent(json);

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.Warehouses)
        {
            Content = content
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);
        httpRequest.Headers.Add("No-Paging", "true");

        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    /// <summary>
    /// Stok Kartları Listeleme - Filtreleme ile (ListeleStkSkart.do)
    /// </summary>
    public async Task<JsonElement> ListStockCardsAsync(
        string? kodBas = null,
        string? kodBit = null,
        string kodOp = "between",
        CancellationToken ct = default)
    {
        var request = new LucaListStockCardsRequest();
        
        if (!string.IsNullOrEmpty(kodBas) && !string.IsNullOrEmpty(kodBit))
        {
            request.StkSkart = new LucaStockCardCodeFilter
            {
                KodBas = kodBas,
                KodBit = kodBit,
                KodOp = kodOp
            };
        }
        else
        {
            request.StkSkart = new LucaStockCardCodeFilter();
        }
        
        return await ListStockCardsAsync(request, ct);
    }

    /// <summary>
    /// Stok Kartları Listeleme - Request ile (ListeleStkSkart.do)
    /// </summary>
    public async Task<JsonElement> ListStockCardsAsync(
        LucaListStockCardsRequest request,
        CancellationToken ct = default,
        int pageNo = 1,
        int pageSize = 100,
        bool skipEnsure = false)
    {
        try
        {
            var effectiveRequest = request ?? new LucaListStockCardsRequest();
            if (effectiveRequest.StkSkart == null)
            {
                 effectiveRequest.StkSkart = new LucaStockCardCodeFilter();
            }

            var json = JsonSerializer.Serialize(effectiveRequest, _jsonOptions);
            var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
            var endpoint = AppendPagingParameters(_settings.Endpoints.StockCards, pageNo, pageSize);
            
            // 🔥 DEBUG: Session durumunu logla
            _logger.LogDebug("📋 ListStockCardsAsync başlıyor - Session durumu: Authenticated={IsAuth}, SessionCookie={HasSession}, ManualJSession={HasManual}, CookieExpiry={Expiry}",
                _isCookieAuthenticated,
                !string.IsNullOrWhiteSpace(_sessionCookie),
                !string.IsNullOrWhiteSpace(_manualJSessionId),
                _cookieExpiresAt?.ToString("HH:mm:ss") ?? "N/A");
            _logger.LogDebug("🔍 Stok kartları endpoint'i: {Endpoint}", endpoint);
            _logger.LogDebug("📄 Paged request: pageNo={PageNo}, pageSize={PageSize}", pageNo, pageSize);
            if (!skipEnsure)
            {
                LogCookieState("ListStockCardsAsync");
            }
            
            for (var attempt = 1; attempt <= 3; attempt++)
            {
                if (!skipEnsure)
                {
                    await EnsureAuthenticatedAsync();
                    await EnsureBranchSelectedAsync();
                }
                
                // 🔥 DEBUG: Her attempt öncesi cookie durumunu logla
                _logger.LogDebug("📋 ListStockCardsAsync Attempt {Attempt}/3 - Cookie: {Cookie}", 
                    attempt,
                    !string.IsNullOrWhiteSpace(_manualJSessionId) ? _manualJSessionId.Substring(0, Math.Min(30, _manualJSessionId.Length)) + "..." : 
                    !string.IsNullOrWhiteSpace(_sessionCookie) ? _sessionCookie.Substring(0, Math.Min(30, _sessionCookie.Length)) + "..." : "NONE");

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = CreateKozaContent(json)
                };
                ApplyManualSessionCookie(httpRequest);
                _logger.LogDebug("📨 Request headers: {Headers}", string.Join("; ", httpRequest.Headers.Select(h => h.Key + "=" + string.Join(',', h.Value))));

                HttpResponseMessage? response = null;
                string responseContent = "[]";
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                    cts.Token.Register(() => ct.ThrowIfCancellationRequested());
                    response = await client.SendAsync(httpRequest, cts.Token);
                    responseContent = await ReadResponseContentAsync(response);
                }
                catch (OperationCanceledException)
                {
                    if (skipEnsure)
                    {
                        throw new TimeoutException($"ListStockCardsAsync timed out (attempt {attempt}).");
                    }
                    _logger.LogWarning("ListStockCardsAsync timed out (attempt {Attempt}); returning empty list to proceed with sync.", attempt);
                    return JsonDocument.Parse("[]").RootElement.Clone();
                }

                try
                {
                    await AppendRawLogAsync($"LIST_STOCK_CARDS_{attempt}", endpoint, json, response.StatusCode, responseContent);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to append LIST_STOCK_CARDS log (attempt {Attempt})", attempt);
                }

                _logger.LogDebug("📥 ListStockCards Response: Status={Status}, Headers={Headers}", 
                    response?.StatusCode, response != null ? string.Join("; ", response.Headers.Select(h => h.Key)) : "N/A");
                _logger.LogDebug("📄 Response snippet (first 500 chars): {Body}", 
                    responseContent.Length > 500 ? responseContent.Substring(0, 500) : responseContent);

                if (response == null)
                {
                    _logger.LogWarning("ListStockCardsAsync received null response (attempt {Attempt}); retrying.", attempt);
                    await Task.Delay(200 * attempt, ct);
                    continue;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && attempt < 3)
                {
                    _logger.LogWarning("Stock card list returned 401; re-authenticating (attempt {Attempt})", attempt);
                    MarkSessionUnauthenticated();
                    await Task.Delay(200 * attempt);
                    continue;
                }

                if (!_settings.UseTokenAuth && NeedsBranchSelection(responseContent) && attempt < 3)
                {
                    _logger.LogWarning("Branch selection required while listing stock cards (attempt {Attempt}); retrying.", attempt);
                    MarkSessionUnauthenticated();
                    await Task.Delay(200 * attempt);
                    continue;
                }

                // 🔥 HTML RESPONSE KONTROLÜ - Session timeout veya login sayfası
                if (IsHtmlResponse(responseContent))
                {
                    _logger.LogError("❌ ListStockCardsAsync HTML response aldı (session timeout/login gerekli). Attempt: {Attempt}", attempt);
                    
                    // 🔍 HTML response'u detaylı logla
                    var htmlPreview = responseContent.Length > 500 ? responseContent.Substring(0, 500) + "...(truncated)" : responseContent;
                    _logger.LogError("📄 HTML Response Preview:\n{Preview}", htmlPreview);
                    
                    // 🔍 Hangi URL'e redirect ediyor?
                    if (responseContent.Contains("login", StringComparison.OrdinalIgnoreCase) || 
                        responseContent.Contains("giris", StringComparison.OrdinalIgnoreCase) ||
                        responseContent.Contains("oturum", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogError("🚨 Login sayfasına redirect ediliyor! Cookie problemi var. Session expire olmuş olabilir.");
                    }
                    
                    // 🔍 Response headers'ı logla
                    _logger.LogDebug("📊 Response Headers: Status={Status}, ContentType={ContentType}, SetCookie={SetCookie}",
                        response.StatusCode,
                        response.Content.Headers.ContentType?.MediaType ?? "N/A",
                        response.Headers.Contains("Set-Cookie") ? "YES" : "NO");
                    
                    if (attempt < 3)
                    {
                        // 🔥 AGRESİF SESSION YENİLEME - Tüm session state'i temizle
                        _logger.LogWarning("🔄 Session tamamen yenileniyor (attempt {Attempt})...", attempt);
                        await ForceSessionRefreshAsync();
                        await Task.Delay(1000 * attempt); // Daha uzun bekleme
                        continue;
                    }
                    else
                    {
                        // Son denemede de HTML geldi, boş liste dön
                        _logger.LogError("❌ ListStockCardsAsync 3 denemede de HTML döndü. Session sorunu çözülemedi. Boş liste döndürülüyor.");
                        if (skipEnsure)
                        {
                            throw new InvalidOperationException("Koza returned HTML instead of JSON (session/branch issue).");
                        }
                        return JsonDocument.Parse("[]").RootElement.Clone();
                    }
                }

                response.EnsureSuccessStatusCode();
                
                // JSON parse denemesi - hata varsa yakalayıp boş liste dön
                try
                {
                    return JsonSerializer.Deserialize<JsonElement>(responseContent);
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "ListStockCardsAsync JSON parse hatası. Response: {Response}", 
                        responseContent.Length > 500 ? responseContent.Substring(0, 500) + "..." : responseContent);
                    
                    if (attempt < 3)
                    {
                        _logger.LogWarning("JSON parse hatası, tekrar deneniyor (attempt {Attempt})", attempt);
                        MarkSessionUnauthenticated();
                        await Task.Delay(500 * attempt);
                        continue;
                    }

                    if (skipEnsure)
                    {
                        throw;
                    }
                    return JsonDocument.Parse("[]").RootElement.Clone();
                }
            }

            return JsonDocument.Parse("[]").RootElement.Clone();
        }
        catch (Exception ex)
        {
            if (skipEnsure) throw;
            _logger.LogWarning(ex, "ListStockCardsAsync failed; returning empty list to allow sync to proceed.");
            return JsonDocument.Parse("[]").RootElement.Clone();
        }
    }

    private void LogCookieState(string context)
    {
        try
        {
            if (_settings == null || string.IsNullOrWhiteSpace(_settings.BaseUrl))
            {
                _logger.LogDebug("{Context}: BaseUrl boş, cookie durumu atlandı", context);
                return;
            }

            var baseUri = new Uri(_settings.BaseUrl.TrimEnd('/') + "/");
            var cookies = _cookieContainer?.GetCookies(baseUri);
            var count = cookies?.Count ?? 0;
            _logger.LogDebug("{Context}: Cookie sayısı = {Count}", context, count);
            if (cookies != null)
            {
                foreach (Cookie cookie in cookies)
                {
                    var preview = cookie.Value.Length > 20 ? cookie.Value[..20] + "..." : cookie.Value;
                    _logger.LogDebug("{Context}: Cookie {Name}={Value}", context, cookie.Name, preview);
                }
            }

            if (!string.IsNullOrWhiteSpace(_manualJSessionId))
            {
                var preview = _manualJSessionId.Length > 20 ? _manualJSessionId[..20] + "..." : _manualJSessionId;
                _logger.LogDebug("{Context}: Manual JSESSIONID present (preview): {Preview}", context, preview);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "{Context}: Cookie durumunu loglarken hata oluştu", context);
        }
    }

    private static string AppendPagingParameters(string endpoint, int pageNo, int pageSize)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return $"?pageNo={Math.Max(1, pageNo)}&pageSize={Math.Max(1, pageSize)}";
        }

        var separator = endpoint.Contains("?", StringComparison.Ordinal) ? "&" : "?";
        var safePageNo = Math.Max(1, pageNo);
        var safePageSize = Math.Max(1, pageSize);
        return $"{endpoint}{separator}pageNo={safePageNo}&pageSize={safePageSize}";
    }

    /// <summary>
    /// SKU/KartKodu normalizasyonu - KartKoduHelper.CanonicalizeKartKodu'ya yönlendirir.
    /// Cache lookup, payload oluşturma ve duplicate kontrolü için tek bir canonical form sağlar.
    /// </summary>
    private static string NormalizeSku(string? sku)
    {
        return KartKoduHelper.CanonicalizeKartKodu(sku);
    }

    public async Task<List<LucaStockCardSummaryDto>> ListStockCardsAsync(CancellationToken cancellationToken = default)
    {
        // Cache warmup: use the JSON endpoint with a wide date range to avoid HTML responses and paging issues.
        var request = new LucaListStockCardsRequest
        {
            StkSkart = new LucaStockCardCodeFilter
            {
                EklemeTarihiBas = "01/01/2020",
                EklemeTarihiBit = DateTime.Now.ToString("dd/MM/yyyy"),
                EklemeTarihiOp = "between"
            }
        };

        var json = await ListStockCardsAsync(request, cancellationToken);
        var result = new List<LucaStockCardSummaryDto>();

        try
        {
            JsonElement arrayEl = default;
            if (json.ValueKind == JsonValueKind.Array)
            {
                arrayEl = json;
            }
            else if (json.ValueKind == JsonValueKind.Object)
            {
                foreach (var key in new[] { "stkSkart", "data", "list", "items" })
                {
                    if (json.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.Array)
                    {
                        arrayEl = prop;
                        break;
                    }
                }
            }

            if (arrayEl.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("ListStockCardsAsync (cache warmup) unexpected JSON shape: {Kind}", json.ValueKind);
                return result;
            }

            foreach (var item in arrayEl.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) continue;

                var code = TryGetProperty(item, "kod", "kartKodu", "code", "skartKod", "stokKartKodu", "stokKodu");
                var barcode = TryGetProperty(item, "barkod", "barcode");
                var name = TryGetProperty(item, "kartAdi", "tanim", "name", "stokKartAdi", "stokAdi") ?? code ?? string.Empty;
                var unit = TryGetProperty(item, "anaBirimAdi", "olcumBirimi", "birim");
                var qtyText = TryGetProperty(item, "stokMiktari", "miktar", "quantity");

                if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(barcode))
                {
                    continue;
                }

                double qty = 0;
                if (!string.IsNullOrWhiteSpace(qtyText))
                {
                    double.TryParse(qtyText.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out qty);
                }

                result.Add(new LucaStockCardSummaryDto
                {
                    StokKodu = code ?? string.Empty,
                    StokAdi = name ?? string.Empty,
                    Birim = unit ?? string.Empty,
                    Miktar = qty,
                    Barcode = barcode,
                    // Değişiklik tespiti için ek alanlar
                    AlisFiyat = TryGetDecimalProperty(item, "alisFiyat", "purchasePrice"),
                    SatisFiyat = TryGetDecimalProperty(item, "satisFiyat", "salesPrice", "fiyat"),
                    AlisKdvOran = TryGetDoubleProperty(item, "kartAlisKdvOran", "alisKdvOran"),
                    SatisKdvOran = TryGetDoubleProperty(item, "kartSatisKdvOran", "satisKdvOran"),
                    KategoriKodu = TryGetProperty(item, "kategoriAgacKod", "kategoriKodu", "category")
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ListStockCardsAsync (cache warmup) failed; returning empty list.");
        }

        return result;
    }

    public async Task<JsonElement> ListStockCardPriceListsAsync(LucaListStockCardPriceListsRequest request)
    {
        await EnsureAuthenticatedAsync();
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.StockCardPriceLists)
        {
            Content = content
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);
        httpRequest.Headers.Add("No-Paging", "true");
        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> ListStockCardAltUnitsAsync(LucaStockCardByIdRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.StockCardAltUnits)
        {
            Content = content
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);
        httpRequest.Headers.Add("No-Paging", "true");

        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> ListStockCardAltStocksAsync(LucaStockCardByIdRequest request)
    {
        await EnsureAuthenticatedAsync();
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.StockCardAltStocks)
        {
            Content = content
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);
        httpRequest.Headers.Add("No-Paging", "true");
        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> ListStockCardCostsAsync(LucaStockCardByIdRequest request)
    {
        await EnsureAuthenticatedAsync();
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.StockCardCosts)
        {
            Content = content
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);
        httpRequest.Headers.Add("No-Paging", "true");
        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    /// <summary>
    /// Stok Kartı Temin Yerleri Listesi - skartId ile (ListeleStkSkartTeminYeri.do)
    /// </summary>
    public async Task<JsonElement> ListStockCardSuppliersAsync(long skartId, CancellationToken ct = default)
    {
        var request = new LucaStockCardByIdRequest
        {
            StkSkart = new LucaStockCardKey { SkartId = skartId }
        };
        return await ListStockCardSuppliersAsync(request, ct);
    }

    /// <summary>
    /// Stok Kartı Temin Yerleri Listesi - Request ile (ListeleStkSkartTeminYeri.do)
    /// </summary>
    public async Task<JsonElement> ListStockCardSuppliersAsync(LucaStockCardByIdRequest request, CancellationToken ct = default)
    {
        await EnsureAuthenticatedAsync();
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.StockCardSuppliers)
        {
            Content = content
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);
        httpRequest.Headers.Add("No-Paging", "true");
        var response = await client.SendAsync(httpRequest, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> ListStockCardPurchaseTermsAsync(LucaStockCardByIdRequest request)
    {
        await EnsureAuthenticatedAsync();
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.StockCardPurchaseTerms)
        {
            Content = content
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);
        httpRequest.Headers.Add("No-Paging", "true");
        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> ListStockCardPurchasePricesAsync(LucaStockCardByIdRequest request)
    {
        await EnsureAuthenticatedAsync();
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.StockCardPurchasePrices)
        {
            Content = content
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);
        httpRequest.Headers.Add("No-Paging", "true");
        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> ListStockCardSalesPricesAsync(LucaStockCardByIdRequest request)
    {
        await EnsureAuthenticatedAsync();
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.StockCardSalesPrices)
        {
            Content = content
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);
        httpRequest.Headers.Add("No-Paging", "true");
        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> ListCustomerContactsAsync(LucaListCustomerContactsRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.CustomerContacts)
        {
            Content = content
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);
        httpRequest.Headers.Add("No-Paging", "true");
        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> ListBanksAsync(LucaListBanksRequest? request = null)
    {
        await EnsureAuthenticatedAsync();
        var json = JsonSerializer.Serialize(request ?? new LucaListBanksRequest(), _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.BankList)
        {
            Content = content
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);
        httpRequest.Headers.Add("No-Paging", "true");

        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> ListCashAccountsAsync(LucaListCashAccountsRequest? request = null)
    {
        await EnsureAuthenticatedAsync();
        var json = JsonSerializer.Serialize(request ?? new LucaListCashAccountsRequest(), _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.CashList)
        {
            Content = content
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);
        httpRequest.Headers.Add("No-Paging", "true");
        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> GetWarehouseStockQuantityAsync(LucaGetWarehouseStockRequest request)
    {
        await EnsureAuthenticatedAsync();
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);
        var url = _settings.Endpoints.WarehouseStockQuantity;
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };
        ApplyManualSessionCookie(httpRequest);
        httpRequest.Headers.Add("No-Paging", "true");
        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> ListSalesOrdersAsync(LucaListSalesOrdersRequest? request = null, bool detayliListe = false)
    {
        await EnsureAuthenticatedAsync();
        var url = _settings.Endpoints.SalesOrderList + (detayliListe ? "?detayliListe=true" : string.Empty);
        var json = JsonSerializer.Serialize(request ?? new LucaListSalesOrdersRequest(), _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };
        ApplyManualSessionCookie(httpRequest);
        httpRequest.Headers.Add("No-Paging", "true");
        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> ListPurchaseOrdersAsync(LucaListPurchaseOrdersRequest? request = null, bool detayliListe = false)
    {
        await EnsureAuthenticatedAsync();
        var url = _settings.Endpoints.PurchaseOrderList + (detayliListe ? "?detayliListe=true" : string.Empty);
        var json = JsonSerializer.Serialize(request ?? new LucaListPurchaseOrdersRequest(), _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);
        httpRequest.Headers.Add("No-Paging", "true");
        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> ListStockCategoriesAsync(LucaListStockCategoriesRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.StockCategories)
        {
            Content = content
        };
        ApplyManualSessionCookie(httpRequest);
        httpRequest.Headers.Add("No-Paging", "true");
        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<SyncResultDto> SendProductsFromExcelAsync(List<ExcelProductDto> products, CancellationToken cancellationToken = default)
    {
        if (products == null || products.Count == 0)
        {
            return new SyncResultDto
            {
                SyncType = "PRODUCT_STOCK_CARD",
                ProcessedRecords = 0,
                SuccessfulRecords = 0,
                FailedRecords = 0
            };
        }
        var mapped = new List<LucaCreateStokKartiRequest>();
        foreach (var p in products)
        {
            cancellationToken.ThrowIfCancellationRequested();
            mapped.Add(KatanaToLucaMapper.MapFromExcelRow(
                p,
                _settings.DefaultKdvOran,
                _settings.DefaultOlcumBirimiId,
                _settings.DefaultKartTipi,
                _settings.DefaultKategoriKodu));
        }
        return await SendStockCardsAsync(mapped);
    }
    private async Task<List<KozaDtos.KozaStokKartiDto>> ListStockCardsLightAsync(string q, int pageNo, int pageSize, bool skipEnsure = false)
    {
        var safePageNo = Math.Max(1, pageNo);
        var safePageSize = Math.Max(1, pageSize);
        var queryText = q ?? string.Empty;

        if (!skipEnsure)
        {
            await EnsureAuthenticatedAsync();
            await EnsureBranchSelectedAsync();
        }

        var endpoint = _settings?.Endpoints?.StockCardAutoComplete;
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            endpoint = "SdlSkart.do";
        }

        var separator = endpoint.Contains("?", StringComparison.Ordinal) ? "&" : "?";
        var url = $"{endpoint}{separator}kartTuru=1&q={Uri.EscapeDataString(queryText)}&pageNo={safePageNo}&autoComplete=1&pageSize={safePageSize}";

        var payload = new
        {
            displayTagSize = safePageSize
        };

        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = CreateKozaContent(json)
        };
        if (httpRequest.Content?.Headers != null && httpRequest.Content.Headers.ContentType == null)
        {
            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        }
        httpRequest.Headers.TryAddWithoutValidation("displayTagSize", safePageSize.ToString(CultureInfo.InvariantCulture));
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);

        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        _logger.LogDebug("Autocomplete response => Status={Status}, ContentType={ContentType}, Url={Url}, Length={Length}",
            response.StatusCode,
            response.Content.Headers.ContentType?.MediaType ?? "unknown",
            url,
            responseContent?.Length ?? 0);

        var trimmed = responseContent?.TrimStart() ?? "";
        if (trimmed.StartsWith("<", StringComparison.Ordinal))
        {
            var preview = (responseContent?.Length ?? 0) > 200
                ? responseContent!.Substring(0, 200) + "..."
                : responseContent ?? "";
            _logger.LogError("ListStockCardsLightAsync HTML response. Status={Status}; BodyPreview={Preview}",
                response.StatusCode, preview);
            _logger.LogWarning("Koza HTML returned (likely invalid/missing q or session redirect). URL={Url}", url);
            throw new InvalidOperationException("Koza returned HTML instead of JSON (session/endpoint/q issue)");
        }

        var cards = new List<KozaDtos.KozaStokKartiDto>();
        try
        {
            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;
            JsonElement arrayEl = default;

            if (root.ValueKind == JsonValueKind.Array)
            {
                arrayEl = root;
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var key in new[] { "items", "list", "data" })
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
                foreach (var item in arrayEl.EnumerateArray())
                {
                    string? code = null;
                    long? stokKartId = null;

                    if (item.ValueKind == JsonValueKind.Object)
                    {
                        if (item.TryGetProperty("label", out var labelProp))
                        {
                            code = labelProp.GetString();
                        }

                        if (item.TryGetProperty("value", out var valueProp))
                        {
                            if (valueProp.ValueKind == JsonValueKind.Number && valueProp.TryGetInt64(out var longVal))
                            {
                                stokKartId = longVal;
                            }
                            else if (valueProp.ValueKind == JsonValueKind.String)
                            {
                                var valueText = valueProp.GetString();
                                if (long.TryParse(valueText, out var parsed))
                                {
                                    stokKartId = parsed;
                                }
                            }
                        }
                    }

                    if (string.IsNullOrWhiteSpace(code))
                    {
                        code = stokKartId?.ToString() ?? string.Empty;
                    }

                    if (!string.IsNullOrWhiteSpace(code))
                    {
                        cards.Add(new KozaDtos.KozaStokKartiDto
                        {
                            KartKodu = code,
                            StokKartId = stokKartId
                        });
                    }
                }
            }
        }
        catch (JsonException jsonEx)
        {
            _logger.LogWarning(jsonEx, "ListStockCardsLightAsync JSON parse error. Body preview: {Preview}",
                responseContent.Length > 200 ? responseContent.Substring(0, 200) + "..." : responseContent);
        }

        return cards;
    }

    private readonly record struct KozaStockCacheResult(
        bool IsReady,
        Dictionary<string, long> Cache,
        int PagesFetched,
        int DuplicateSkuCount,
        int? TotalReported,
        string? ErrorMessage);

    private async Task<KozaStockCacheResult> BuildKozaStockCacheAsync(CancellationToken ct)
    {
        var cache = new Dictionary<string, long>(StringComparer.Ordinal);
        var duplicateSkuCount = 0;
        int? totalReported = null;
        var pagesFetched = 0;

        var pageNo = 1;
        const int pageSize = 100;
        const int duplicateLogLimit = 10;
        var heartbeatSw = Stopwatch.StartNew();

        try
        {
            var request = new LucaListStockCardsRequest
            {
                StkSkart = new LucaStockCardCodeFilter()
            };

            while (true)
            {
                var page = await ListStockCardsAsync(request, ct, pageNo, pageSize, skipEnsure: true);
                if (page.ValueKind == JsonValueKind.Null || page.ValueKind == JsonValueKind.Undefined)
                {
                    break;
                }

                JsonElement arrayEl = default;
                if (page.ValueKind == JsonValueKind.Array)
                {
                    arrayEl = page;
                }
                else if (page.ValueKind == JsonValueKind.Object)
                {
                    foreach (var key in new[] { "list", "stokKartlari", "stkKartListesi", "stkSkart", "data", "items" })
                    {
                        if (page.TryGetProperty(key, out var candidate) && candidate.ValueKind == JsonValueKind.Array)
                        {
                            arrayEl = candidate;
                            break;
                        }
                    }
                }

                if (arrayEl.ValueKind != JsonValueKind.Array)
                {
                    _logger.LogWarning("Cache build: unexpected JSON shape on page {Page}", pageNo);
                    break;
                }

                pagesFetched++;
                var itemCount = arrayEl.GetArrayLength();
                totalReported ??= TryGetInt32Property(page, "total", "totalCount", "recordsTotal", "kayitSayisi", "count");
                var addedOnPage = 0;

                foreach (var item in arrayEl.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object) continue;
                    var code = TryGetProperty(item, "kod", "kartKodu", "skartKod", "stokKartKodu", "stokKodu");
                    long? id = null;
                    if (item.TryGetProperty("skartId", out var skartId) && skartId.ValueKind == JsonValueKind.Number)
                    {
                        id = skartId.GetInt64();
                    }
                    else if (item.TryGetProperty("stokKartId", out var stokKartId) && stokKartId.ValueKind == JsonValueKind.Number)
                    {
                        id = stokKartId.GetInt64();
                    }

                    if (!string.IsNullOrWhiteSpace(code) && id.HasValue)
                    {
                        var normalized = NormalizeSku(code);
                        if (cache.ContainsKey(normalized))
                        {
                            duplicateSkuCount++;
                            if (duplicateSkuCount <= duplicateLogLimit)
                            {
                                _logger.LogWarning("Cache build: duplicate SKU ignored (keeping first). sku='{Sku}' newId={NewId}", normalized, id.Value);
                            }
                            continue;
                        }

                        cache[normalized] = id.Value;
                        addedOnPage++;
                    }
                }

                _logger.LogInformation("Cache build: page {Page} items={ItemCount} added={Added} totalDistinct={TotalDistinct}",
                    pageNo, itemCount, addedOnPage, cache.Count);

                if (heartbeatSw.Elapsed >= TimeSpan.FromSeconds(3))
                {
                    await _syncProgressReporter.ReportAsync(new SyncProgressDto(
                        SyncType: "PRODUCT_STOCK_CARD",
                        Stage: "CACHE",
                        Total: totalReported ?? 0,
                        Processed: cache.Count,
                        Success: 0,
                        Failed: 0,
                        CurrentSku: null,
                        TimestampUtc: DateTime.UtcNow), ct);
                    heartbeatSw.Restart();
                }

                if (itemCount == 0)
                {
                    break;
                }

                if (itemCount < pageSize)
                {
                    break;
                }

                if (totalReported.HasValue)
                {
                    var expectedPages = (int)Math.Ceiling(totalReported.Value / (double)pageSize);
                    if (pageNo >= expectedPages)
                    {
                        break;
                    }
                }

                pageNo++;
                if (ct.IsCancellationRequested) break;
            }

            if (duplicateSkuCount > duplicateLogLimit)
            {
                _logger.LogWarning("Cache build: {Count} duplicate SKU ignored (only first {Limit} logged)", duplicateSkuCount, duplicateLogLimit);
            }

            return new KozaStockCacheResult(true, cache, pagesFetched, duplicateSkuCount, totalReported, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache build failed");
            return new KozaStockCacheResult(false, cache, pagesFetched, duplicateSkuCount, totalReported, ex.Message);
        }
    }

    private static int? TryGetInt32Property(JsonElement root, params string[] names)
    {
        if (root.ValueKind != JsonValueKind.Object) return null;
        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var prop)) continue;
            try
            {
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var n)) return n;
                if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out var s)) return s;
            }
            catch
            {
            }
        }
        return null;
    }

    public async Task<SyncResultDto> SendStockCardsAsync(List<LucaCreateStokKartiRequest> stockCards)
    {
        static void WriteStockDebug(string cardCode, string stage, string payload, string response)
        {
            try
            {
                var repoDir = Directory.GetCurrentDirectory();
                var logDir = Path.Combine(repoDir, "logs");
                Directory.CreateDirectory(logDir);
                var file = Path.Combine(logDir, "stock-debug.log");
                File.AppendAllText(file,
                    $"--- {DateTime.UtcNow:o} [{stage}] {cardCode}{Environment.NewLine}PAYLOAD:{Environment.NewLine}{payload}{Environment.NewLine}RESPONSE:{Environment.NewLine}{response}{Environment.NewLine}---{Environment.NewLine}");
            }
            catch { }
        }
        // 🔥 DE-DUPLICATION: Aynı KartKodu'dan birden fazla varsa temizle
        var uniqueCards = stockCards
            .GroupBy(c => NormalizeSku(c.KartKodu))
            .Select(g => g.First())
            .ToList();

        if (uniqueCards.Count < stockCards.Count)
        {
            _logger.LogWarning("⚠️ Duplicate KartKodu temizlendi: {Before} → {After}", 
                stockCards.Count, uniqueCards.Count);
        }
        
        var result = new SyncResultDto
        {
            SyncType = "PRODUCT_STOCK_CARD",
            ProcessedRecords = uniqueCards.Count
        };

        var startTime = DateTime.UtcNow;
        var successCount = 0;
        var failedCount = 0;
        var duplicateCount = 0;
        var skippedCount = 0;
        var sentCount = 0;
        var heartbeatSw = Stopwatch.StartNew();
        
        // 🚀 PERFORMANS OPTİMİZASYONU - Batch işleme ayarları
        const int batchSize = 50; // 🔥 Artırıldı: Daha hızlı işleme (20 → 50)
        const int rateLimitDelayMs = 25; // 🔥 Azaltıldı: Minimum delay (300ms → 25ms)
        
        async Task MaybeReportProgressAsync(string stage, string? currentSku, bool force = false)
        {
            if (!force && heartbeatSw.Elapsed < TimeSpan.FromSeconds(3)) return;
            heartbeatSw.Restart();

            try
            {
                await _syncProgressReporter.ReportAsync(new SyncProgressDto(
                    SyncType: "PRODUCT_STOCK_CARD",
                    Stage: stage,
                    Total: uniqueCards.Count,
                    Processed: successCount + failedCount + skippedCount,
                    Success: successCount,
                    Failed: failedCount,
                    CurrentSku: currentSku,
                    TimestampUtc: DateTime.UtcNow));
            }
            catch
            {
            }
        }
        
        try
        {
            // 🔥 STEP 1: Authentication ve Branch Selection
            _logger.LogInformation("🔐 Step 1/3: Authentication ve Branch Selection...");
            await EnsureAuthenticatedAsync();
            await EnsureBranchSelectedAsync();
            
            // 🔥 STEP 2: Session Warmup (Struts framework'ünü uyandır)
            _logger.LogInformation("🔥 Step 2/3: Session Warmup başlatılıyor...");
            try
            {
                var warmupOk = await WarmupSessionAsync();
                if (!warmupOk)
                {
                    _logger.LogWarning("⚠️ Session warmup başarısız, ancak devam ediliyor");
                }
            }
            catch (Exception warmupEx)
            {
                _logger.LogWarning(warmupEx, "⚠️ Session warmup hatası, ancak devam ediliyor");
            }
            
            // 🔥 STEP 3: CACHE WARMING (Tek seferlik - tüm Luca stok kartlarını çek)
            _logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            _logger.LogInformation("📥 Step 3/3: CACHE WARMING - Koza stok kartları tek seferde çekiliyor...");
            _logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            var cacheAlreadyLoaded = false;
            var existingCache = new Dictionary<string, long>(StringComparer.Ordinal);

            await _stockCardCacheLock.WaitAsync();
            try
            {
                cacheAlreadyLoaded = _stockCardCache.Count > 0;
                if (cacheAlreadyLoaded)
                {
                    foreach (var kv in _stockCardCache)
                    {
                        if (kv.Value.HasValue)
                        {
                            existingCache[NormalizeSku(kv.Key)] = kv.Value.Value;
                        }
                    }
                }
            }
            finally
            {
                _stockCardCacheLock.Release();
            }

            var kozaCache = cacheAlreadyLoaded && existingCache.Count > 0
                ? new KozaStockCacheResult(true, existingCache, 0, 0, existingCache.Count, null)
                : await BuildKozaStockCacheAsync(CancellationToken.None);

            if (cacheAlreadyLoaded && existingCache.Count > 0)
            {
                _logger.LogInformation("🚀 Cache zaten dolu ({Count} kart) - Luca'dan tekrar çekilmiyor!", existingCache.Count);
            }

            if (!kozaCache.IsReady || kozaCache.Cache.Count == 0)
            {
                result.IsSuccess = false;
                result.FailedRecords = uniqueCards.Count;
                var msg = kozaCache.ErrorMessage ?? "Koza cache oluşturulamadı.";
                if (kozaCache.IsReady && kozaCache.Cache.Count == 0)
                {
                    msg = "Koza cache 0 ürün döndürdü; duplicate riskini önlemek için işlem durduruldu.";
                }
                result.Message = $"❌ Cache build başarısız: {msg}";
                result.Errors.Add(result.Message);
                _logger.LogError("❌ Koza cache oluşturulamadı. Sync durduruldu. Reason: {Reason}", msg);
                return result;
            }

            if (!cacheAlreadyLoaded)
            {
                await _stockCardCacheLock.WaitAsync();
                try
                {
                    _stockCardCache.Clear();
                    foreach (var kv in kozaCache.Cache)
                    {
                        _stockCardCache[kv.Key] = kv.Value;
                    }
                }
                finally
                {
                    _stockCardCacheLock.Release();
                }

                _logger.LogInformation("✅ Cache loaded: {Count} stock cards, pages={Pages}, duplicatesIgnored={Duplicates}",
                    kozaCache.Cache.Count, kozaCache.PagesFetched, kozaCache.DuplicateSkuCount);
            }

            await MaybeReportProgressAsync("CACHE", null, force: true);

            var toCreate = new List<LucaCreateStokKartiRequest>();
            foreach (var card in uniqueCards)
            {
                var originalKartKodu = card.KartKodu ?? string.Empty;
                var normalizedSku = NormalizeSku(card.KartKodu);
                if (!string.IsNullOrWhiteSpace(normalizedSku) && kozaCache.Cache.ContainsKey(normalizedSku))
                {
                    skippedCount++;
                    duplicateCount++;
                    _logger.LogInformation("⏭️ SKIP: {Original} zaten Luca'da var (cache). Canonical: {Canonical}", originalKartKodu, normalizedSku);
                    await MaybeReportProgressAsync("CACHE", card.KartKodu);
                    continue;
                }
                
                _logger.LogDebug("📝 Cache MISS: {Original} → {Canonical} - Oluşturulacak", originalKartKodu, normalizedSku);
                toCreate.Add(card);
            }

            if (toCreate.Count == 0)
            {
                _logger.LogInformation("Tüm stok kartları Luca'da mevcut. Yeni kart oluşturulmadı.");
                await MaybeReportProgressAsync("DONE", null, force: true);

                result.SuccessfulRecords = 0;
                result.FailedRecords = 0;
                result.DuplicateRecords = duplicateCount;
                result.SkippedRecords = skippedCount;
                result.IsSuccess = true;
                result.Message = "Tüm kartlar Luca'da bulundu; yeni kart oluşturulmadı.";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            _logger.LogInformation("Sending {Count} stock cards to Luca (Koza) with batch size {BatchSize}", toCreate.Count, batchSize);

            // NOT: client'ı her seferinde güncel al, ForceSessionRefresh _cookieHttpClient'ı yenileyebilir
            var endpoint = _settings.Endpoints.StockCardCreate;
            // 🔥 RAPOR ZORUNLULUĞU: ISO-8859-9 encoding kullanılmalı (1254 değil!)
            // Luca API Türkçe karakterler için ISO-8859-9 bekliyor
            var encoding = Encoding.GetEncoding("ISO-8859-9");
            
            // Batch işleme (create edilecek kartlar için)
            var batches = toCreate
                .Select((card, index) => new { card, index })
                .GroupBy(x => x.index / batchSize)
                .Select(g => g.Select(x => x.card).ToList())
                .ToList();
            
            var batchNumber = 0;
            foreach (var batch in batches)
            {
                batchNumber++;
                _logger.LogInformation("Processing batch {BatchNumber}/{TotalBatches} ({BatchCount} cards)", 
                    batchNumber, batches.Count, batch.Count);
                
                var batchPosted = false;

                foreach (var card in batch)
                {
                    var cardHadPost = false;
                    try
                    {
                        // 🔥 CANONICAL KEY - Cache ve payload için aynı normalizasyon
                        var canonicalKey = KartKoduHelper.CanonicalizeKartKodu(card.KartKodu);
                        var originalKartKodu = card.KartKodu ?? string.Empty;
                        
                        // 🔥 SKU DEBUG LOG - Hata ayıklama için (original ve canonical)
                        _logger.LogDebug("SKU Check → Original={Original}, Canonical={Canonical}", originalKartKodu, canonicalKey);

                        _logger.LogInformation("✨ Yeni stok kartı: {SKU} (canonical: {Canonical})", originalKartKodu, canonicalKey);
                        
                        // Yeni kayıt oluştur
                        _logger.LogInformation("➕ [3/3] Yeni stok kartı POST ediliyor: {KartKodu} → {Canonical}", originalKartKodu, canonicalKey);
                        
                    // 🔥 Postman örneğine göre JSON formatında request oluştur
                    var baslangic = string.IsNullOrWhiteSpace(card.BaslangicTarihi)
                        ? DateTime.Now.ToString("dd'/'MM'/'yyyy", System.Globalization.CultureInfo.InvariantCulture)
                        : card.BaslangicTarihi;
                    
                    // 🔥 CANONICAL NORMALIZATION - Payload için de aynı canonical form kullan
                    var safeCode = canonicalKey; // Cache key ile aynı
                    var safeName = KartKoduHelper.CanonicalizeKartKodu(card.KartAdi); // İsim için de normalize et
                    
                    // ✅ KartAdi boşsa SKU kullan (fallback)
                    if (string.IsNullOrWhiteSpace(safeName))
                    {
                        _logger.LogWarning("⚠️ KartAdi boş, SKU kullanılıyor: {KartKodu}", originalKartKodu);
                        safeName = safeCode;
                    }
                    if (string.IsNullOrWhiteSpace(safeName))
                    {
                        // Son güvenlik ağı: isim yine boşsa NONAME-<SKU>
                        safeName = $"NONAME-{safeCode}";
                        _logger.LogWarning("⚠️ KartAdi hala boş, NONAME fallback kullanıldı: {KartAd}", safeName);
                    }
                    
                    // 🔥 RAPOR ZORUNLULUĞU: LUCA DOKÜMANTASYONUNA %100 UYGUN - SADECE BU ALANLAR!
                    // ❌ EKSTRA ALAN EKLENMEMELİ: kartSatisKdvOran, uzunAdi, stokKategoriId vs. YASAK!
                    // ✅ ÇALIŞAN ÖRNEK (RAPOR): 
                    // {"kartAdi":"Test Ürünü","kartKodu":"00013225","kartTipi":1,
                    //  "kartAlisKdvOran":1,"olcumBirimiId":1,"baslangicTarihi":"06/04/2022",
                    //  "kartTuru":1,"kategoriAgacKod":null,"barkod":"8888888",
                    //  "alisTevkifatOran":"7/10","satisTevkifatOran":"2/10",
                    //  "alisTevkifatTipId":1,"satisTevkifatTipId":1,
                    //  "satilabilirFlag":1,"satinAlinabilirFlag":1,"lotNoFlag":1,
                    //  "minStokKontrol":0,"maliyetHesaplanacakFlag":true}
                    var jsonRequest = new Dictionary<string, object?>
                    {
                        // ✅ ZORUNLU ALANLAR (3 tane)
                        ["kartAdi"] = safeName,                    // required - Ürün adı
                        ["kartKodu"] = safeCode,                   // required - SKU/Stok kodu
                        ["baslangicTarihi"] = baslangic,           // required - dd/MM/yyyy formatında
                        
                        // ✅ TİP VE KATEGORİ
                        ["kartTipi"] = 1,                          // Sabit: 1
                        ["kartTuru"] = 1,                          // 1=Stok, 2=Hizmet
                        ["kategoriAgacKod"] = null,                // Kategori (şimdilik null)
                        
                        // ✅ KDV VE ÖLÇÜ BİRİMİ
                        ["kartAlisKdvOran"] = 1,                   // KDV oranı (1 = %18)
                        ["olcumBirimiId"] = 1,                     // 1=ADET, 2=KG, 3=LT, 4=M, 5=MT, 6=M2, 7=M3
                        
                        // ✅ BARKOD
                        ["barkod"] = safeCode,                     // Barkod (SKU ile aynı olabilir)
                        
                        // ✅ TEVKİFAT BİLGİLERİ (null veya "7/10" formatında)
                        ["alisTevkifatOran"] = null,               // Alış tevkifat oranı (örn: "7/10")
                        ["satisTevkifatOran"] = null,              // Satış tevkifat oranı (örn: "2/10")
                        ["alisTevkifatTipId"] = null,              // Alış tevkifat tip ID (1,2,3...)
                        ["satisTevkifatTipId"] = null,             // Satış tevkifat tip ID (1,2,3...)
                        
                        // ✅ FLAGLER (integer: 0 veya 1)
                        ["satilabilirFlag"] = 1,                   // Satılabilir mi? 1=Evet
                        ["satinAlinabilirFlag"] = 1,               // Satın alınabilir mi? 1=Evet
                        ["lotNoFlag"] = 1,                         // Lot takibi? 1=Evet
                        ["minStokKontrol"] = 0,                    // Min stok kontrolü? 0=Hayır
                        
                        // ⚠️ DİKKAT: maliyetHesaplanacakFlag BOOLEAN (diğerleri integer!)
                        ["maliyetHesaplanacakFlag"] = true         // Maliyet hesaplansın mı? boolean!
                    };
                    
                    // Null değerleri de serialize et
                    var serializeOptions = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = null,
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
                        WriteIndented = false
                    };
                    var payload = JsonSerializer.Serialize(jsonRequest, serializeOptions);
                    _logger.LogInformation(">>> LUCA JSON REQUEST ({Card}): {Payload}", card.KartKodu, payload);

                    // JSON content olarak gönder (ISO-8859-9 encoding ile)
                    var byteContent = new ByteArrayContent(encoding.GetBytes(payload));
                    byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    byteContent.Headers.ContentType.CharSet = encoding.WebName;

                        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
                        {
                            Content = byteContent
                        };
                        ApplyManualSessionCookie(httpRequest);

                        try { await SaveHttpTrafficAsync($"SEND_STOCK_CARD_REQUEST:{card.KartKodu}", httpRequest, null); } catch (Exception) { }

                        sentCount++;
                        // Her request'te güncel client'ı al (ForceSessionRefresh sonrası değişmiş olabilir)
                        var currentClient = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
                        cardHadPost = true;
                        var response = await currentClient.SendAsync(httpRequest);
                    var responseBytes = await response.Content.ReadAsByteArrayAsync();
                    string responseContent;
                    try { responseContent = encoding.GetString(responseBytes); } catch { responseContent = Encoding.UTF8.GetString(responseBytes); }
                    try
                    {
                        var preview = responseContent.Length > 500 ? responseContent.Substring(0, 500) + "...(truncated)" : responseContent;
                        _logger.LogInformation("Luca stock card response for {Card} => HTTP {StatusCode}, BODY={Body}", card.KartKodu, response.StatusCode, preview);
                        Console.WriteLine($">>> LUCA STOCK CARD RESPONSE {card.KartKodu}: HTTP {(int)response.StatusCode} {response.StatusCode} BODY={preview}");
                    }
                    catch { }
                    var baseUrl = currentClient.BaseAddress?.ToString()?.TrimEnd('/') ?? _settings.BaseUrl?.TrimEnd('/') ?? string.Empty;
                        var fullUrl = string.IsNullOrWhiteSpace(baseUrl) ? endpoint : (endpoint.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? endpoint : baseUrl + "/" + endpoint.TrimStart('/'));
                        await AppendRawLogAsync("SEND_STOCK_CARD", fullUrl, payload, response.StatusCode, responseContent);
                        try { await SaveHttpTrafficAsync($"SEND_STOCK_CARD_RESPONSE:{card.KartKodu}", httpRequest, response); } catch (Exception) { }
                        WriteStockDebug(card.KartKodu ?? string.Empty, "JSON", payload, responseContent);

                    if (NeedsBranchSelection(responseContent))
                    {
                        _logger.LogWarning("Stock card {Card} failed due to branch not selected; re-authenticating + branch change, then retrying once", card.KartKodu);
                        MarkSessionUnauthenticated();
                        await EnsureAuthenticatedAsync();
                        await EnsureBranchSelectedAsync();
                        await VerifyBranchSelectionAsync();
                        var retryReq = new HttpRequestMessage(HttpMethod.Post, endpoint)
                        {
                            Content = new ByteArrayContent(encoding.GetBytes(payload))
                            {
                                Headers = { ContentType = new MediaTypeHeaderValue("application/json") { CharSet = _encoding.WebName } }
                            }
                        };
                        ApplyManualSessionCookie(retryReq);
                        sentCount++;
                        cardHadPost = true;
                        response = await (_cookieHttpClient ?? _httpClient).SendAsync(retryReq);
                        responseBytes = await response.Content.ReadAsByteArrayAsync();
                        try { responseContent = encoding.GetString(responseBytes); } catch { responseContent = Encoding.UTF8.GetString(responseBytes); }
                        await AppendRawLogAsync("SEND_STOCK_CARD_RETRY", fullUrl, payload, response.StatusCode, responseContent);
                    }

                    if (responseContent.TrimStart().StartsWith("<", StringComparison.OrdinalIgnoreCase))
                    {
                        
                        _logger.LogError("Stock card {Card} returned HTML (session expired?). Snippet: {Snippet}", card.KartKodu, responseContent.Length > 200 ? responseContent.Substring(0, 200) : responseContent);
                        _logger.LogWarning("🔄 Stock card {Card} returned HTML. Session timeout olabilir - ForceSessionRefresh yapılıyor...", card.KartKodu);
                        await AppendRawLogAsync($"SEND_STOCK_CARD_HTML:{card.KartKodu}", fullUrl, payload, response.StatusCode, responseContent);
                        try { await SaveHttpTrafficAsync($"SEND_STOCK_CARD_HTML:{card.KartKodu}", null, response); } catch (Exception) {  }
                        
                        // HTML genellikle session timeout demek - önce session'ı yenile
                        try
                        {
                            await ForceSessionRefreshAsync();
                            _logger.LogInformation("✅ Session yenilendi, stok kartı tekrar gönderiliyor: {Card}", card.KartKodu);
                            
                            // Session yenilendikten sonra tekrar dene
                            var retryAfterRefresh = new HttpRequestMessage(HttpMethod.Post, endpoint)
                            {
                                Content = new ByteArrayContent(encoding.GetBytes(payload))
                                {
                                    Headers = { ContentType = new MediaTypeHeaderValue("application/json") { CharSet = _encoding.WebName } }
                                }
                            };
                            ApplyManualSessionCookie(retryAfterRefresh);
                            sentCount++;
                            cardHadPost = true;
                            var retryResp = await (_cookieHttpClient ?? _httpClient).SendAsync(retryAfterRefresh);
                            var retryBytes = await retryResp.Content.ReadAsByteArrayAsync();
                            string retryContent;
                            try { retryContent = encoding.GetString(retryBytes); } catch { retryContent = Encoding.UTF8.GetString(retryBytes); }
                            await AppendRawLogAsync($"SEND_STOCK_CARD_SESSION_RETRY:{card.KartKodu}", fullUrl, payload, retryResp.StatusCode, retryContent);
                            
                            if (!retryContent.TrimStart().StartsWith("<", StringComparison.OrdinalIgnoreCase))
                            {
                                responseContent = retryContent;
                                response = retryResp;
                                _logger.LogInformation("✅ Session yenileme sonrası başarılı: {Card}", card.KartKodu);
                            }
                            else
                            {
                                _logger.LogWarning("⚠️ Session yenileme sonrası hala HTML döndü: {Card}", card.KartKodu);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "ForceSessionRefresh hatası, devam ediliyor: {Card}", card.KartKodu);
                        }
                        
                        // Hala HTML ise diğer retry metodlarını dene
                        if (responseContent.TrimStart().StartsWith("<", StringComparison.OrdinalIgnoreCase))
                        {
                        _logger.LogWarning("Stock card {Card} hala HTML döndürüyor. UTF-8 JSON ve form-encoded retry deneniyor...", card.KartKodu);
                        
                        try
                        {
                            var utf8Bytes = Encoding.UTF8.GetBytes(payload);
                            var utf8Content = new ByteArrayContent(utf8Bytes);
                            utf8Content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
                            using var utf8Req = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = utf8Content };
                            ApplyManualSessionCookie(utf8Req);
                            sentCount++;
                            cardHadPost = true;
                            var utf8Resp = await (_cookieHttpClient ?? _httpClient).SendAsync(utf8Req);
                            var utf8BytesResp = await utf8Resp.Content.ReadAsByteArrayAsync();
                            string utf8RespContent;
                            try { utf8RespContent = Encoding.UTF8.GetString(utf8BytesResp); } catch { utf8RespContent = _encoding.GetString(utf8BytesResp); }
                            await AppendRawLogAsync($"SEND_STOCK_CARD_UTF8_RETRY:{card.KartKodu}", fullUrl, payload, utf8Resp.StatusCode, utf8RespContent);
                            try { await SaveHttpTrafficAsync($"SEND_STOCK_CARD_UTF8_RETRY:{card.KartKodu}", utf8Req, utf8Resp); } catch (Exception) {  }

                            if (!utf8RespContent.TrimStart().StartsWith("<", StringComparison.OrdinalIgnoreCase))
                            {
                                responseContent = utf8RespContent;
                                response = utf8Resp;
                                
                            }
                            else
                            {
                                _logger.LogWarning("UTF-8 retry for {Card} still returned HTML", card.KartKodu);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "UTF-8 retry failed for stock card {Card}", card.KartKodu);
                        }
                        
                        if (responseContent.TrimStart().StartsWith("<", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                
                                var formPairs = new List<KeyValuePair<string, string>>();
                                try
                                {
                                    using var doc = JsonDocument.Parse(payload);
                                    if (doc.RootElement.ValueKind == JsonValueKind.Object)
                                    {
                                        foreach (var prop in doc.RootElement.EnumerateObject())
                                        {
                                            if (prop.Value.ValueKind == JsonValueKind.String)
                                                formPairs.Add(new KeyValuePair<string, string>(prop.Name, prop.Value.GetString() ?? string.Empty));
                                            else if (prop.Value.ValueKind == JsonValueKind.Number)
                                                formPairs.Add(new KeyValuePair<string, string>(prop.Name, prop.Value.ToString() ?? string.Empty));
                                            else if (prop.Value.ValueKind == JsonValueKind.True || prop.Value.ValueKind == JsonValueKind.False)
                                                formPairs.Add(new KeyValuePair<string, string>(prop.Name, prop.Value.GetBoolean() ? "true" : "false"));
                                            else if (prop.Value.ValueKind == JsonValueKind.Null)
                                                formPairs.Add(new KeyValuePair<string, string>(prop.Name, string.Empty));
                                            
                                        }
                                    }
                                }
                                catch (Exception) {  }
                                var form = new FormUrlEncodedContent(formPairs);
                                using var formReq = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = form };
                                ApplyManualSessionCookie(formReq);
                                sentCount++;
                                cardHadPost = true;
                                var formResp = await (_cookieHttpClient ?? _httpClient).SendAsync(formReq);
                                var formRespBody = await ReadResponseContentAsync(formResp);
                                await AppendRawLogAsync($"SEND_STOCK_CARD_FORM_RETRY:{card.KartKodu}", fullUrl, payload, formResp.StatusCode, formRespBody);
                                try { await SaveHttpTrafficAsync($"SEND_STOCK_CARD_FORM_RETRY:{card.KartKodu}", formReq, formResp); } catch (Exception) {  }
                                if (!formRespBody.TrimStart().StartsWith("<", StringComparison.OrdinalIgnoreCase))
                                {
                                    responseContent = formRespBody;
                                    response = formResp;
                                }
                                else
                                {
                                    _logger.LogWarning("Form-encoded retry for {Card} returned HTML", card.KartKodu);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Form-encoded retry failed for stock card {Card}", card.KartKodu);
                            }
                        }
                        
                        if (responseContent.TrimStart().StartsWith("<", StringComparison.OrdinalIgnoreCase))
                        {
                            failedCount++;
                            var htmlPreview = responseContent.Length > 300 ? responseContent.Substring(0, 300) : responseContent;
                            result.Errors.Add($"{card.KartKodu}: HTML response after retries: {htmlPreview}");
                            _logger.LogError("Stock card {Card} returned HTML after retries. Session/format issue.", card.KartKodu);
                            continue;
                        }
                        } // Session refresh sonrası HTML kontrol if bloğu sonu
                    }

                    // Eğer Koza {"error":true} dönüyor ve mesaj yoksa, stkSkart wrapper ile tekrar dene
                    try
                    {
                        using var doc = JsonDocument.Parse(responseContent);
                        if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                            doc.RootElement.TryGetProperty("error", out var errProp) &&
                            errProp.ValueKind == JsonValueKind.True)
                        {
                            var msg = doc.RootElement.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.String
                                ? msgProp.GetString() ?? string.Empty
                                : string.Empty;
                            if (string.IsNullOrWhiteSpace(msg))
                            {
                                _logger.LogWarning("ℹ️ Koza 'error:true' döndürdü ve mesaj yok. stkSkart wrapper ile tekrar deneniyor: {Card}", card.KartKodu);
                                var wrappedPayload = JsonSerializer.Serialize(new { stkSkart = jsonRequest }, serializeOptions);
                                var wrappedContent = new ByteArrayContent(encoding.GetBytes(wrappedPayload));
                                wrappedContent.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
                                using var wrappedReq = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = wrappedContent };
                                ApplyManualSessionCookie(wrappedReq);
                                sentCount++;
                                cardHadPost = true;
                                var wrappedResp = await (_cookieHttpClient ?? _httpClient).SendAsync(wrappedReq);
                                var wrappedBytes = await wrappedResp.Content.ReadAsByteArrayAsync();
                                string wrappedResponseContent;
                                try { wrappedResponseContent = encoding.GetString(wrappedBytes); } catch { wrappedResponseContent = Encoding.UTF8.GetString(wrappedBytes); }
                                await AppendRawLogAsync($"SEND_STOCK_CARD_WRAPPED:{card.KartKodu}", fullUrl, wrappedPayload, wrappedResp.StatusCode, wrappedResponseContent);
                                try { await SaveHttpTrafficAsync($"SEND_STOCK_CARD_WRAPPED:{card.KartKodu}", wrappedReq, wrappedResp); } catch (Exception) {  }
                                WriteStockDebug(card.KartKodu ?? string.Empty, "WRAPPED", wrappedPayload, wrappedResponseContent);
                                if (!wrappedResponseContent.TrimStart().StartsWith("<", StringComparison.OrdinalIgnoreCase))
                                {
                                    responseContent = wrappedResponseContent;
                                    response = wrappedResp;
                                }
                                else
                                {
                                    _logger.LogWarning("stkSkart wrapper denemesi HTML döndürdü: {Card}", card.KartKodu);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Wrapper retry kontrolü başarısız: {Card}", card.KartKodu);
                    }

                    // Eğer Koza error:true dönüyor ve mesaj boşsa, form-encoded fallback dene
                    try
                    {
                        using var doc = JsonDocument.Parse(responseContent);
                        if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                            doc.RootElement.TryGetProperty("error", out var errProp2) &&
                            errProp2.ValueKind == JsonValueKind.True)
                        {
                            var msg2 = doc.RootElement.TryGetProperty("message", out var msgProp2) && msgProp2.ValueKind == JsonValueKind.String
                                ? msgProp2.GetString() ?? string.Empty
                                : string.Empty;
                            if (string.IsNullOrWhiteSpace(msg2))
                            {
                                _logger.LogWarning("ℹ️ Koza 'error:true' + boş mesaj. Form-encoded fallback deneniyor: {Card}", card.KartKodu);
                                var formPairs = jsonRequest
                                    .Where(kv => kv.Value != null)
                                    .Select(kv => new KeyValuePair<string, string>(kv.Key, kv.Value?.ToString() ?? string.Empty))
                                    .ToList();
                                var formContent = new FormUrlEncodedContent(formPairs);
                                using var formReq = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = formContent };
                                ApplyManualSessionCookie(formReq);
                                sentCount++;
                                cardHadPost = true;
                                var formResp = await (_cookieHttpClient ?? _httpClient).SendAsync(formReq);
                                var formRespBody = await ReadResponseContentAsync(formResp);
                                await AppendRawLogAsync($"SEND_STOCK_CARD_FORM_EMPTYERROR:{card.KartKodu}", fullUrl, string.Join("&", formPairs.Select(p => $"{p.Key}={p.Value}")), formResp.StatusCode, formRespBody);
                                try { await SaveHttpTrafficAsync($"SEND_STOCK_CARD_FORM_EMPTYERROR:{card.KartKodu}", formReq, formResp); } catch (Exception) { }
                                WriteStockDebug(card.KartKodu ?? string.Empty, "FORM_EMPTYERROR", string.Join("&", formPairs.Select(p => $"{p.Key}={p.Value}")), formRespBody);
                                if (!formRespBody.TrimStart().StartsWith("<", StringComparison.OrdinalIgnoreCase))
                                {
                                    responseContent = formRespBody;
                                    response = formResp;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Form fallback kontrolü başarısız: {Card}", card.KartKodu);
                    }
                    JsonElement parsedResponse = default;
                    var parsedSuccessfully = false;
                    try
                    {
                        parsedResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        parsedSuccessfully = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Stock card {Card} response could not be parsed; assuming success on HTTP OK", card.KartKodu);
                    }

                    // Check for Luca error responses (error=true or code!=0)
                    if (parsedSuccessfully && parsedResponse.ValueKind == JsonValueKind.Object)
                    {
                        // Handle {"error":true,"message":"..."} format
                        if (parsedResponse.TryGetProperty("error", out var errorProp) && 
                            errorProp.ValueKind == JsonValueKind.True)
                        {
                            var msg = parsedResponse.TryGetProperty("message", out var messageProp) && 
                                      messageProp.ValueKind == JsonValueKind.String
                                ? messageProp.GetString() ?? "Unknown error"
                                : "Unknown error";

                            // SKIP duplicates as warnings, not failures
                            // Check for duplicate message (handle both correct UTF-8 and broken encoding)
                            var isDuplicate = msg.Contains("daha önce kullanılmış", StringComparison.OrdinalIgnoreCase) || 
                                              msg.Contains("daha once kullanilmis", StringComparison.OrdinalIgnoreCase) ||
                                              msg.Contains("nce kullan", StringComparison.OrdinalIgnoreCase) || // partial match for broken encoding
                                              msg.Contains("already exists", StringComparison.OrdinalIgnoreCase) || 
                                              msg.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
                                              msg.Contains("Kart kodu daha", StringComparison.OrdinalIgnoreCase); // Turkish prefix
                            
                            if (isDuplicate)
                            {
                                // Kart kodu Luca'da zaten mevcut
                                // Luca'da stok kartı güncelleme olmadığı için duplicate kayıtları uyarı olarak logluyoruz
                                _logger.LogWarning("⚠️ Stok kartı '{Card}' Luca'da zaten mevcut (duplicate). Güncelleme yapılmayacak. " +
                                    "Katana'da ürün güncellemesi yapmanız gerekirse, Luca'da manuel olarak aynı kartı düzenleyiniz.", 
                                    card.KartKodu);
                                
                                // 🔥 DUPLICATE CACHE UPDATE - Koza duplicate dedi, cache'e ekle ki tekrar denemeyelim
                                await _stockCardCacheLock.WaitAsync();
                                try
                                {
                                    var canonicalForCache = KartKoduHelper.CanonicalizeKartKodu(card.KartKodu);
                                    if (!_stockCardCache.ContainsKey(canonicalForCache))
                                    {
                                        // ID'yi bilmiyoruz ama -1 ile işaretle (exists but unknown ID)
                                        _stockCardCache[canonicalForCache] = -1;
                                        _logger.LogInformation("🔄 Duplicate tespit edildi, cache'e eklendi: {Canonical} (ID unknown)", canonicalForCache);
                                    }
                                    if (kozaCache.IsReady && !kozaCache.Cache.ContainsKey(canonicalForCache))
                                    {
                                        kozaCache.Cache[canonicalForCache] = -1;
                                    }
                                }
                                finally
                                {
                                    _stockCardCacheLock.Release();
                                }
                                
                                // Duplicate'ı success olarak işaretle (atlanacak)
                                duplicateCount++;
                                continue;
                            }

                            // Other errors are failures
                            failedCount++;
                            result.Errors.Add($"{card.KartKodu}: {msg}");
                            _logger.LogError("Stock card {Card} failed: {Message}", card.KartKodu, msg);
                            continue;
                        }

                        // Handle {"code":1003,...} format
                        if (parsedResponse.TryGetProperty("code", out var codeProp) && 
                            codeProp.ValueKind == JsonValueKind.Number)
                        {
                            var code = codeProp.GetInt32();
                            if (code == 1003)
                            {
                                _logger.LogError("Stock card {Card} failed with code 1003 (session expired). Stopping.", card.KartKodu);
                                throw new UnauthorizedAccessException("Session expired or branch not selected (code 1003).");
                            }

                            if (code != 0)
                            {
                                failedCount++;
                                var msg = parsedResponse.TryGetProperty("message", out var messageProp) && 
                                          messageProp.ValueKind == JsonValueKind.String
                                    ? messageProp.GetString()
                                    : "Unknown error";
                                result.Errors.Add($"{card.KartKodu}: code={code} message={msg}");
                                _logger.LogError("Stock card {Card} failed with code {Code}: {Message}", card.KartKodu, code, msg);
                                continue;
                            }
                        }
                    }
                    if (!response.IsSuccessStatusCode)
                    {
                        failedCount++;
                        var previewError = responseContent.Length > 300 ? responseContent.Substring(0, 300) + "...(truncated)" : responseContent;
                        result.Errors.Add($"{card.KartKodu}: HTTP {response.StatusCode} - {previewError}");
                        _logger.LogError("Stock card {Card} failed HTTP {Status}: {Body}", card.KartKodu, response.StatusCode, previewError);
                        continue;
                    }
                    // 🔥 Postman örneğine göre başarılı response: {"skartId": 79409, "error": false, "message": "..."}
                    long? newSkartId = null;
                    if (parsedSuccessfully && parsedResponse.ValueKind == JsonValueKind.Object)
                    {
                        // Format 1: {"skartId": 79409, "error": false, "message": "..."}
                        if (parsedResponse.TryGetProperty("skartId", out var skartIdProp) && 
                            skartIdProp.ValueKind == JsonValueKind.Number)
                        {
                            newSkartId = skartIdProp.GetInt64();
                            var message = parsedResponse.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.String
                                ? msgProp.GetString() : "Başarılı";
                            
                            _logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                            _logger.LogInformation("✅ STOK KARTI BAŞARIYLA OLUŞTURULDU");
                            _logger.LogInformation("   SKU: {Card}", card.KartKodu);
                            _logger.LogInformation("   Luca ID (skartId): {SkartId}", newSkartId);
                            _logger.LogInformation("   Mesaj: {Message}", message);
                            _logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                        }
                        // Format 2: {"stkSkart": {"skartId": ...}}
                        else if (parsedResponse.TryGetProperty("stkSkart", out var skartEl) &&
                            skartEl.ValueKind == JsonValueKind.Object &&
                            skartEl.TryGetProperty("skartId", out var idEl))
                        {
                            if (idEl.ValueKind == JsonValueKind.Number)
                            {
                                newSkartId = idEl.GetInt64();
                            }
                            _logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                            _logger.LogInformation("✅ STOK KARTI BAŞARIYLA OLUŞTURULDU");
                            _logger.LogInformation("   SKU: {Card}", card.KartKodu);
                            _logger.LogInformation("   Luca ID: {Id}", idEl.ToString());
                            _logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                        }
                        else
                        {
                            _logger.LogInformation("✅ Stock card {Card} created (response format unknown)", card.KartKodu);
                        }
                    }

                    // ✅ Gönderilen kartı cache'e ekle (tekrar sorgulamayı önle)
                    if (newSkartId.HasValue)
                    {
                        await _stockCardCacheLock.WaitAsync();
                        try
                        {
                            var normalized = NormalizeSku(card.KartKodu);
                            _stockCardCache[normalized] = newSkartId.Value;
                            if (kozaCache.IsReady)
                            {
                                kozaCache.Cache[normalized] = newSkartId.Value;
                            }
                            _logger.LogDebug("🔄 Cache'e eklendi: {SKU} → {Id}", normalized, newSkartId.Value);
                        }
                        finally
                        {
                            _stockCardCacheLock.Release();
                        }
                    }

                    successCount++;
                    _logger.LogInformation("✅ Stock card created: {Card}", card.KartKodu);
                }
                catch (Exception ex)
                {
                    var errorMsg = ex.Message.ToLowerInvariant();
                    
                    // 🔥 DUPLICATE HATA KONTROLÜ - Luca'nın döndürdüğü hata mesajlarını yakala
                    if (errorMsg.Contains("daha önce kullanılmış") ||
                        errorMsg.Contains("already exists") ||
                        errorMsg.Contains("duplicate") ||
                        errorMsg.Contains("zaten mevcut") ||
                        errorMsg.Contains("kayıt var") ||
                        errorMsg.Contains("kart kodu var"))
                    {
                        _logger.LogWarning("⚠️ Duplicate tespit edildi (API hatası): {KartKodu} - {Message}", card.KartKodu, ex.Message);
                        
                        // 🔥 DUPLICATE CACHE UPDATE - Exception'dan gelen duplicate'ı da cache'e ekle
                        try
                        {
                            await _stockCardCacheLock.WaitAsync();
                            try
                            {
                                var canonicalForCache = KartKoduHelper.CanonicalizeKartKodu(card.KartKodu);
                                if (!_stockCardCache.ContainsKey(canonicalForCache))
                                {
                                    _stockCardCache[canonicalForCache] = -1; // ID unknown
                                    _logger.LogInformation("🔄 Duplicate (exception), cache'e eklendi: {Canonical}", canonicalForCache);
                                }
                                if (kozaCache.IsReady && !kozaCache.Cache.ContainsKey(canonicalForCache))
                                {
                                    kozaCache.Cache[canonicalForCache] = -1;
                                }
                            }
                            finally
                            {
                                _stockCardCacheLock.Release();
                            }
                        }
                        catch { /* Cache update failure shouldn't break the flow */ }
                        
                        duplicateCount++;
                        skippedCount++;
                        // Duplicate hata olarak sayma, başarısız olarak sayma
                    }
                    else
                    {
                        failedCount++;
                        result.Errors.Add($"{card.KartKodu}: {ex.Message}");
                        _logger.LogError(ex, "❌ Error sending stock card {Card}", card.KartKodu);
                    }
                }
                finally
                {
                    await MaybeReportProgressAsync("SENDING", card.KartKodu);
                    if (cardHadPost)
                    {
                        batchPosted = true;
                        await Task.Delay(rateLimitDelayMs);
                    }
                }
            }
            
            // Batch arası bekleme - API'yi yormamak ve session timeout önlemek için
            if (batchPosted && batchNumber < batches.Count)
            {
                _logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                _logger.LogInformation("✅ Batch {BatchNumber}/{TotalBatches} tamamlandı", batchNumber, batches.Count);
                _logger.LogInformation("   Başarılı: {Success}, Başarısız: {Failed}, Duplicate: {Duplicate}", 
                    successCount, failedCount, duplicateCount);
                _logger.LogInformation("⏳ Sonraki batch için 2 saniye bekleniyor...");
                _logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                await Task.Delay(2000);
            }
        }
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.FailedRecords = stockCards.Count;
            result.Message = ex.Message;
            result.Errors.Add(ex.ToString());
            _logger.LogError(ex, "Error sending stock cards to Luca");
        }
        result.SuccessfulRecords = successCount;
        result.FailedRecords = failedCount;
        result.DuplicateRecords = duplicateCount;
        result.SentRecords = sentCount;
        result.SkippedRecords = skippedCount;
        // IsSuccess should be true if no real failures occurred (duplicates are not failures)
        result.IsSuccess = failedCount == 0;
        string summary;
        if (skippedCount > 0 || duplicateCount > 0)
        {
            summary = $"✅ {successCount} yeni oluşturuldu, ⏭️ {skippedCount} atlandı (zaten mevcut/değişiklik yok), ❌ {failedCount} başarısız. Toplam: {stockCards.Count}";
        }
        else
        {
            summary = $"✅ {successCount} başarılı, ❌ {failedCount} başarısız. Toplam: {stockCards.Count}";
        }

        result.Message = summary;
        result.Duration = DateTime.UtcNow - startTime;
        await MaybeReportProgressAsync("DONE", null, force: true);
        return result;
    }
    public async Task<SyncResultDto> SendStockCardAsync(LucaStockCardDto stockCard)
    {
        var createDto = new LucaCreateStokKartiRequest
        {
            KartAdi = stockCard.KartAdi,
            KartTuru = stockCard.KartTuru,
            KartKodu = stockCard.KartKodu ?? string.Empty,
            OlcumBirimiId = stockCard.OlcumBirimiId,
            BaslangicTarihi = stockCard.BaslangicTarihi == default ? null : stockCard.BaslangicTarihi.ToString("dd/MM/yyyy"),
            KartAlisKdvOran = stockCard.KartAlisKdvOran,
            KartSatisKdvOran = stockCard.KartSatisKdvOran,
            KartToptanAlisKdvOran = stockCard.KartToptanAlisKdvOran ?? 0,
            KartToptanSatisKdvOran = stockCard.KartToptanSatisKdvOran ?? 0,
            KategoriAgacKod = stockCard.KategoriAgacKod ?? _settings.DefaultKategoriKodu,
            KartTipi = stockCard.KartTipi ?? _settings.DefaultKartTipi,
            Barkod = stockCard.Barkod ?? stockCard.KartKodu ?? string.Empty,
            UzunAdi = stockCard.UzunAdi ?? stockCard.KartAdi,
            BitisTarihi = stockCard.BitisTarihi,
            MaliyetHesaplanacakFlag = stockCard.MaliyetHesaplanacakFlag,
            GtipKodu = stockCard.GtipKodu ?? string.Empty,
            GarantiSuresi = stockCard.GarantiSuresi ?? 0,
            RafOmru = stockCard.RafOmru ?? 0,
            AlisTevkifatOran = stockCard.AlisTevkifatOran,
            AlisTevkifatTipId = stockCard.AlisTevkifatKod,  // Luca doc: alisTevkifatTipId
            SatisTevkifatOran = stockCard.SatisTevkifatOran,
            SatisTevkifatTipId = stockCard.SatisTevkifatKod,  // Luca doc: satisTevkifatTipId
            MinStokKontrol = stockCard.MinStokKontrol ?? 0,
            MinStokMiktari = stockCard.MinStokMiktari ?? 0,
            MaxStokKontrol = stockCard.MaxStokKontrol ?? 0,
            MaxStokMiktari = stockCard.MaxStokMiktari ?? 0,
            AlisIskontoOran1 = stockCard.AlisIskontoOran1 ?? 0,
            SatisIskontoOran1 = stockCard.SatisIskontoOran1 ?? 0,
            SatilabilirFlag = stockCard.SatilabilirFlag ? 1 : 0,
            SatinAlinabilirFlag = stockCard.SatinAlinabilirFlag ? 1 : 0,
            SeriNoFlag = stockCard.SeriNoFlag ? 1 : 0,
            LotNoFlag = stockCard.LotNoFlag ? 1 : 0,
            DetayAciklama = stockCard.DetayAciklama ?? string.Empty,
            PerakendeAlisBirimFiyat = 0,
            PerakendeSatisBirimFiyat = 0
        };

        return await SendStockCardsAsync(new List<LucaCreateStokKartiRequest> { createDto });
    }
}
