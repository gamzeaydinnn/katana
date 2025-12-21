using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Katana.Core.DTOs;
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
using Katana.Core.Helpers;

namespace Katana.Infrastructure.APIClients;

/// <summary>
/// LucaService - PART 3: Queries (List/Fetch methods, Helpers, Upsert methods)
/// </summary>
public partial class LucaService
{
    /// <summary>
    /// Fatura Listesi - Filtreleme ile (ListeleFtrSsFaturaBaslik.do)
    /// </summary>
    public async Task<JsonElement> ListInvoicesAsync(
        int? parUstHareketTuru = null,
        int? parAltHareketTuru = null,
        long? belgeNoBas = null,
        long? belgeNoBit = null,
        string? belgeTarihiBas = null,
        string? belgeTarihiBit = null,
        bool detayliListe = false,
        CancellationToken ct = default)
    {
        var request = new LucaListInvoicesRequest
        {
            ParUstHareketTuru = parUstHareketTuru,
            ParAltHareketTuru = parAltHareketTuru
        };

        if (belgeNoBas.HasValue || belgeTarihiBas != null)
        {
            request.FtrSsFaturaBaslik = new LucaInvoiceOrgBelgeFilter
            {
                GnlOrgSsBelge = new LucaInvoiceBelgeFilter
                {
                    BelgeNoBas = belgeNoBas,
                    BelgeNoBit = belgeNoBit,
                    BelgeNoOp = belgeNoBas.HasValue && belgeNoBit.HasValue ? "between" : null,
                    BelgeTarihiBas = belgeTarihiBas,
                    BelgeTarihiBit = belgeTarihiBit,
                    BelgeTarihiOp = belgeTarihiBas != null && belgeTarihiBit != null ? "between" : null
                }
            };
        }

        return await ListInvoicesAsync(request, detayliListe, ct);
    }

    /// <summary>
    /// Fatura Listesi - Request ile (ListeleFtrSsFaturaBaslik.do)
    /// </summary>
    public async Task<JsonElement> ListInvoicesAsync(LucaListInvoicesRequest request, bool detayliListe = false, CancellationToken ct = default)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request ?? new LucaListInvoicesRequest(), _jsonOptions);
        var content = CreateKozaContent(json);

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var url = _settings.Endpoints.InvoiceList + (detayliListe ? "?detayliListe=true" : string.Empty);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };
        ApplyManualSessionCookie(httpRequest);
        httpRequest.Headers.Add("No-Paging", "true");

        var response = await client.SendAsync(httpRequest, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> CreateInvoiceRawAsync(LucaCreateInvoiceHeaderRequest request)
    {
        await EnsureAuthenticatedAsync();

        NormalizeInvoiceCreateRequest(request);

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        _logger.LogInformation("📄 CreateInvoice - Sending JSON: {Json}", json);

        if (_settings.UseTokenAuth)
        {
            var content = CreateKozaContent(json);
            var tokenResponse = await _httpClient.PostAsync(_settings.Endpoints.Invoices, content);
            var tokenResponseContent = await tokenResponse.Content.ReadAsStringAsync();

            _logger.LogInformation("📄 CreateInvoice (token) - Response Status: {Status}, Body: {Body}",
                tokenResponse.StatusCode, tokenResponseContent);

            if (IsHtmlResponse(tokenResponseContent))
            {
                LogHtmlResponse("CREATE_INVOICE_RAW_TOKEN", tokenResponse, tokenResponseContent, attempt: 1, maxAttempts: 1);
                throw new InvalidOperationException($"Luca API returned HTML content for invoice create (token mode). Status={(int)tokenResponse.StatusCode}");
            }

            if (!tokenResponse.IsSuccessStatusCode)
            {
                _logger.LogError("❌ CreateInvoice (token) FAILED - Status: {Status}, Body: {Body}", tokenResponse.StatusCode, tokenResponseContent);
                throw new HttpRequestException($"Luca API Error ({tokenResponse.StatusCode}): {tokenResponseContent}");
            }

            return JsonSerializer.Deserialize<JsonElement>(tokenResponseContent);
        }

        await EnsureBranchSelectedAsync();
        await VerifyBranchSelectionAsync();

        // 🔥 FATURA ENDPOINT WARMUP: Struts Action'ını uyandır
        // StockCards warmup'ı farklı bir Action class'ı uyandırıyor, fatura için ayrı warmup gerekli
        await WarmupInvoiceEndpointAsync();

        var endpoint = _settings.Endpoints.InvoiceCreate;
        var encoder = _encoding;
        var contentBytes = new ByteArrayContent(encoder.GetBytes(json));
        contentBytes.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = _encoding.WebName };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = contentBytes
        };
        ApplyManualSessionCookie(httpRequest);

        var response = await SendWithAuthRetryAsync(httpRequest, "CREATE_INVOICE_RAW", 3);
        var responseContent = await ReadResponseContentAsync(response);
        await AppendRawLogAsync("CREATE_INVOICE_RAW", endpoint, json, response.StatusCode, responseContent);

        _logger.LogInformation("📄 CreateInvoice - Response Status: {Status}, Body: {Body}", 
            response.StatusCode, responseContent);

        if (IsHtmlResponse(responseContent))
        {
            LogHtmlResponse("CREATE_INVOICE_RAW", response, responseContent, attempt: 1, maxAttempts: 1);
            throw new InvalidOperationException($"Luca API returned HTML content for invoice create. Status={(int)response.StatusCode}");
        }
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("❌ CreateInvoice FAILED - Status: {Status}, Body: {Body}", response.StatusCode, responseContent);
            throw new HttpRequestException($"Luca API Error ({response.StatusCode}): {responseContent}");
        }
        
        // 🔥 Luca bazen HTTP 200 döner ama body'de hata kodu olur (code: 1001, 1002 = Login gerekli)
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        if (result.TryGetProperty("code", out var codeProp) && codeProp.ValueKind == JsonValueKind.Number)
        {
            var code = codeProp.GetInt32();
            if (code == 1001 || code == 1002)
            {
                var msg = result.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : "Login olunmalı";
                _logger.LogError("❌ CreateInvoice FAILED - Luca returned login required error. Code: {Code}, Message: {Message}", code, msg);
                
                // Session'ı yenile ve tekrar dene
                _logger.LogInformation("🔄 Session yenileniyor ve fatura tekrar gönderilecek...");
                await ForceSessionRefreshAsync();
                await EnsureBranchSelectedAsync();
                
                // Retry once
                using var retryRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new ByteArrayContent(encoder.GetBytes(json))
                };
                retryRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = _encoding.WebName };
                ApplyManualSessionCookie(retryRequest);
                
                var retryResponse = await (_cookieHttpClient ?? _httpClient).SendAsync(retryRequest);
                var retryContent = await ReadResponseContentAsync(retryResponse);
                await AppendRawLogAsync("CREATE_INVOICE_RAW_RETRY", endpoint, json, retryResponse.StatusCode, retryContent);
                
                _logger.LogInformation("📄 CreateInvoice RETRY - Response Status: {Status}, Body: {Body}", 
                    retryResponse.StatusCode, retryContent);
                
                if (IsHtmlResponse(retryContent))
                {
                    throw new InvalidOperationException($"Luca API returned HTML after retry. Status={(int)retryResponse.StatusCode}");
                }
                
                var retryResult = JsonSerializer.Deserialize<JsonElement>(retryContent);
                if (retryResult.TryGetProperty("code", out var retryCodeProp) && retryCodeProp.ValueKind == JsonValueKind.Number)
                {
                    var retryCode = retryCodeProp.GetInt32();
                    if (retryCode == 1001 || retryCode == 1002)
                    {
                        throw new UnauthorizedAccessException($"Luca API login required after retry. Code: {retryCode}");
                    }
                }
                
                return retryResult;
            }
        }
        
        return result;
    }

    public async Task<JsonElement> CreateInvoiceRawJsonAsync(string rawJson)
    {
        await EnsureAuthenticatedAsync();

        var json = rawJson ?? string.Empty;
        _logger.LogInformation("📄 CreateInvoice (passthrough) - Sending JSON: {Json}", json);

        if (_settings.UseTokenAuth)
        {
            var content = CreateKozaContent(json);
            var tokenResponse = await _httpClient.PostAsync(_settings.Endpoints.Invoices, content);
            var tokenResponseContent = await tokenResponse.Content.ReadAsStringAsync();

            _logger.LogInformation("📄 CreateInvoice (passthrough/token) - Response Status: {Status}, Body: {Body}",
                tokenResponse.StatusCode, tokenResponseContent);

            if (IsHtmlResponse(tokenResponseContent))
            {
                LogHtmlResponse("CREATE_INVOICE_RAW_PASSTHROUGH_TOKEN", tokenResponse, tokenResponseContent, attempt: 1, maxAttempts: 1);
                throw new InvalidOperationException($"Luca API returned HTML content for invoice create (token mode). Status={(int)tokenResponse.StatusCode}");
            }

            if (!tokenResponse.IsSuccessStatusCode)
            {
                _logger.LogError("❌ CreateInvoice (passthrough/token) FAILED - Status: {Status}, Body: {Body}", tokenResponse.StatusCode, tokenResponseContent);
                throw new HttpRequestException($"Luca API Error ({tokenResponse.StatusCode}): {tokenResponseContent}");
            }

            return JsonSerializer.Deserialize<JsonElement>(tokenResponseContent);
        }

        await EnsureBranchSelectedAsync();
        await VerifyBranchSelectionAsync();
        await WarmupInvoiceEndpointAsync();

        var endpoint = _settings.Endpoints.InvoiceCreate;
        var contentBytes = new ByteArrayContent(_encoding.GetBytes(json));
        contentBytes.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = _encoding.WebName };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = contentBytes
        };
        ApplyManualSessionCookie(httpRequest);

        var response = await SendWithAuthRetryAsync(httpRequest, "CREATE_INVOICE_RAW_PASSTHROUGH", 3);
        var responseContent = await ReadResponseContentAsync(response);
        await AppendRawLogAsync("CREATE_INVOICE_RAW_PASSTHROUGH", endpoint, json, response.StatusCode, responseContent);

        _logger.LogInformation("📄 CreateInvoice (passthrough) - Response Status: {Status}, Body: {Body}",
            response.StatusCode, responseContent);

        if (IsHtmlResponse(responseContent))
        {
            LogHtmlResponse("CREATE_INVOICE_RAW_PASSTHROUGH", response, responseContent, attempt: 1, maxAttempts: 1);
            throw new InvalidOperationException($"Luca API returned HTML content for invoice create. Status={(int)response.StatusCode}");
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("❌ CreateInvoice (passthrough) FAILED - Status: {Status}, Body: {Body}", response.StatusCode, responseContent);
            throw new HttpRequestException($"Luca API Error ({response.StatusCode}): {responseContent}");
        }

        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> CloseInvoiceAsync(LucaCloseInvoiceRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.InvoiceClose)
        {
            Content = content
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);
        
        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> DeleteInvoiceAsync(LucaDeleteInvoiceRequest request)
    {
        await EnsureAuthenticatedAsync();
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.InvoiceDelete)
        {
            Content = content
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);
        
        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> ListCustomerAddressesAsync(LucaListCustomerAddressesRequest request)
    {
        await EnsureAuthenticatedAsync();
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.CustomerAddresses)
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
    public async Task<JsonElement> GetCustomerWorkingConditionsAsync(LucaGetCustomerWorkingConditionsRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.CustomerWorkingConditions, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> ListCustomerAuthorizedPersonsAsync(LucaListCustomerAuthorizedPersonsRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.CustomerAuthorizedPersons)
        {
            Content = content
        };
        httpRequest.Headers.Add("No-Paging", "true");

        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> GetCustomerRiskAsync(LucaGetCustomerRiskRequest request)
    {
        await EnsureAuthenticatedAsync();

        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var url = $"{_settings.Endpoints.CustomerRisk}?gnlFinansalNesne.finansalNesneId={request.GnlFinansalNesne.FinansalNesneId}";
        
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
    public async Task<JsonElement> CreateCustomerTransactionAsync(LucaCreateCariHareketRequest request)
    {
        await EnsureAuthenticatedAsync();
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.CustomerTransaction, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> CreateCustomerTransactionAsync(
        Payment payment,
        Customer customer,
        long belgeTurDetayId,
        int cariTuru,
        string belgeSeri,
        bool avansFlag,
        string? aciklama = null)
    {
        var request = MappingHelper.MapToLucaCariHareketCreate(payment, customer, belgeTurDetayId, cariTuru, belgeSeri, avansFlag, aciklama);
        return await CreateCustomerTransactionAsync(request);
    }
    public async Task<JsonElement> ListDeliveryNotesAsync(LucaListIrsaliyeRequest? request = null, bool detayliListe = false)
    {
        await EnsureAuthenticatedAsync();

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var url = _settings.Endpoints.IrsaliyeList + (detayliListe ? "?detayliListe=true" : string.Empty);

        var json = JsonSerializer.Serialize(request ?? new LucaListIrsaliyeRequest(), _jsonOptions);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = CreateKozaContent(json)
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);
        httpRequest.Headers.Add("No-Paging", "true");

        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<string> GetEirsaliyeXmlAsync(LucaGetEirsaliyeXmlRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        await EnsureAuthenticatedAsync();

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.EirsaliyeXml)
        {
            Content = CreateKozaContent(json)
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);

        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return responseContent;
    }
    public async Task<JsonElement> CreateDeliveryNoteAsync(LucaCreateIrsaliyeBaslikRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.IrsaliyeCreate, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    
    public async Task<JsonElement> GetInvoicePdfLinkAsync(LucaInvoicePdfLinkRequest request)
    {
        await EnsureAuthenticatedAsync();
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.InvoicePdfLink)
        {
            Content = content
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);
        httpRequest.Headers.TryAddWithoutValidation("No-Paging", "true");

        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> ListCurrencyInvoicesAsync(LucaListCurrencyInvoicesRequest request)
    {
        await EnsureAuthenticatedAsync();
        var effectiveRequest = request ?? new LucaListCurrencyInvoicesRequest();
        if (!effectiveRequest.DovizGetir.HasValue)
        {
            effectiveRequest.DovizGetir = 1;
        }
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var url = _settings.Endpoints.CurrencyInvoiceList;
        var json = JsonSerializer.Serialize(effectiveRequest, _jsonOptions);

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
    
    public async Task<JsonElement> DeleteDeliveryNoteAsync(LucaDeleteIrsaliyeRequest request)
    {
        await EnsureAuthenticatedAsync();
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.IrsaliyeDelete, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> CreateCustomerAsync(LucaCreateCustomerRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var endpoint = ResolveCustomerCreateEndpoint();
        var response = await client.PostAsync(endpoint, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> CreateOtherStockMovementAsync(LucaCreateDshBaslikRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.OtherStockMovement, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> CreateSalesOrderAsync(LucaCreateSalesOrderRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.SalesOrder, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> CreateSalesOrderHeaderAsync(LucaCreateOrderHeaderRequest request)
    {
        await EnsureAuthenticatedAsync();
        await EnsureBranchSelectedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);

        static bool LooksLikeLoginRequired(string? body)
        {
            if (string.IsNullOrWhiteSpace(body)) return false;
            return body.Contains("Login olunmalı", StringComparison.OrdinalIgnoreCase)
                   || body.Contains("login olunmali", StringComparison.OrdinalIgnoreCase)
                   || body.Contains("\"code\":1001", StringComparison.OrdinalIgnoreCase)
                   || body.Contains("\"code\":1002", StringComparison.OrdinalIgnoreCase)
                   || body.Contains("Giris.do", StringComparison.OrdinalIgnoreCase)
                   || body.TrimStart().StartsWith("<", StringComparison.Ordinal); // HTML login page
        }

        static bool LooksLikeJson(string? body)
        {
            if (string.IsNullOrWhiteSpace(body)) return false;
            var trimmed = body.TrimStart();
            return trimmed.StartsWith("{", StringComparison.Ordinal) || trimmed.StartsWith("[", StringComparison.Ordinal);
        }

        async Task<(HttpStatusCode Status, string Body)> SendOnceAsync()
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.SalesOrder)
            {
                Content = CreateKozaContent(json)
            };

            ApplySessionCookie(httpRequest);
            ApplyManualSessionCookie(httpRequest);

            var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
            var res = await client.SendAsync(httpRequest);
            var body = await res.Content.ReadAsStringAsync();
            try { await AppendRawLogAsync("SALES_ORDER_HEADER", _settings.Endpoints.SalesOrder, json, res.StatusCode, body); } catch { }
            try { await SaveHttpTrafficAsync("SALES_ORDER_HEADER", httpRequest, res); } catch { }
            return (res.StatusCode, body);
        }

        var first = await SendOnceAsync();
        if (LooksLikeLoginRequired(first.Body))
        {
            _logger.LogWarning("CreateSalesOrderHeaderAsync: response indicates not-authenticated; forcing session refresh and retrying once. Preview={Preview}",
                first.Body.Length > 300 ? first.Body.Substring(0, 300) : first.Body);

            await ForceSessionRefreshAsync();
            await EnsureBranchSelectedAsync();

            first = await SendOnceAsync();
        }

        if ((int)first.Status >= 400)
        {
            throw new HttpRequestException($"Luca SalesOrder API failed with status {(int)first.Status}");
        }

        // Luca sometimes returns 200 with an HTML login page or other non-JSON content.
        // Never attempt to parse such responses as JSON.
        if (LooksLikeLoginRequired(first.Body))
        {
            var preview = first.Body.Length > 300 ? first.Body.Substring(0, 300) : first.Body;
            throw new UnauthorizedAccessException($"Login olunmalı. ResponsePreview={preview}");
        }

        if (!LooksLikeJson(first.Body))
        {
            var preview = first.Body.Length > 300 ? first.Body.Substring(0, 300) : first.Body;
            throw new InvalidOperationException($"Unexpected non-JSON response from Luca SalesOrder endpoint. ResponsePreview={preview}");
        }

        return JsonSerializer.Deserialize<JsonElement>(first.Body);
    }
    public async Task<JsonElement> CreateSalesOrderHeaderAsync(
        Order order,
        Customer customer,
        List<OrderItem> items,
        long belgeTurDetayId,
        string belgeSeri)
    {
        var request = MappingHelper.MapToLucaSalesOrderHeader(order, customer, items, belgeTurDetayId, belgeSeri);
        return await CreateSalesOrderHeaderAsync(request);
    }
    public async Task<JsonElement> DeleteSalesOrderAsync(LucaDeleteSalesOrderRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.SalesOrderDelete, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> DeleteSalesOrderDetailAsync(LucaDeleteSalesOrderDetailRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.SalesOrderDetailDelete, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> CreatePurchaseOrderAsync(LucaCreatePurchaseOrderRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.PurchaseOrder, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> CreatePurchaseOrderHeaderAsync(LucaCreateOrderHeaderRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.PurchaseOrder, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> CreatePurchaseOrderHeaderAsync(
        PurchaseOrder purchaseOrder,
        Supplier supplier,
        List<PurchaseOrderItem> items,
        long belgeTurDetayId,
        string belgeSeri)
    {
        var request = MappingHelper.MapToLucaPurchaseOrderHeader(purchaseOrder, supplier, items, belgeTurDetayId, belgeSeri);
        return await CreatePurchaseOrderHeaderAsync(request);
    }
    public async Task<JsonElement> DeletePurchaseOrderAsync(LucaDeletePurchaseOrderRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.PurchaseOrderDelete, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> DeletePurchaseOrderDetailAsync(LucaDeletePurchaseOrderDetailRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.PurchaseOrderDetailDelete, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    
    public async Task<JsonElement> CreateWarehouseTransferAsync(LucaCreateWarehouseTransferRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.WarehouseTransfer, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    
    /// <summary>
    /// Luca Depo Transferi - LucaStockTransferRequest wrapper
    /// </summary>
    public async Task<long> CreateWarehouseTransferAsync(LucaStockTransferRequest request)
    {
        try
        {
            await EnsureAuthenticatedAsync();
            
            // LucaStockTransferRequest → LucaCreateWarehouseTransferRequest dönüşümü
            var transferRequest = new LucaCreateWarehouseTransferRequest
            {
                BelgeTurDetayId = request.StkDepoTransferBaslik.BelgeTurDetayId,
                BelgeSeri = request.StkDepoTransferBaslik.BelgeSeri,
                BelgeNo = request.StkDepoTransferBaslik.BelgeNo,
                BelgeTarihi = request.StkDepoTransferBaslik.BelgeTarihi,
                BelgeAciklama = request.StkDepoTransferBaslik.BelgeAciklama,
                GirisDepoKodu = request.StkDepoTransferBaslik.GirisDepoKodu,
                CikisDepoKodu = request.StkDepoTransferBaslik.CikisDepoKodu,
                DetayList = request.StkDepoTransferBaslik.DetayList
                    .Select(r => new LucaWarehouseTransferDetailRequest
                    {
                        KartKodu = r.KartKodu,
                        Miktar = (decimal)r.Miktar,
                        OlcuBirimi = r.OlcuBirimi,
                        Aciklama = r.Aciklama
                    }).ToList()
            };
            
            var result = await CreateWarehouseTransferAsync(transferRequest);
            
            // Response'u logla
            var responseText = result.GetRawText();
            _logger.LogInformation("🔍 Depo Transfer Response: {Response}", responseText);
            
            // Response'dan ID çıkar
            if (result.TryGetProperty("id", out var idProp) || result.TryGetProperty("ssBelgeId", out idProp))
            {
                var id = idProp.GetInt64();
                _logger.LogInformation("✅ Depo Transfer ID bulundu: {Id}", id);
                return id;
            }
            
            // Alternatif: data.id
            if (result.TryGetProperty("data", out var dataProp) && dataProp.TryGetProperty("id", out idProp))
            {
                var id = idProp.GetInt64();
                _logger.LogInformation("✅ Depo Transfer ID (data.id) bulundu: {Id}", id);
                return id;
            }
            
            // success ve message kontrol et
            if (result.TryGetProperty("success", out var successProp))
            {
                var success = successProp.GetBoolean();
                _logger.LogWarning("⚠️ Depo Transfer success: {Success}", success);
                
                if (result.TryGetProperty("message", out var messageProp))
                {
                    _logger.LogWarning("⚠️ Depo Transfer message: {Message}", messageProp.GetString());
                }
            }
            
            _logger.LogWarning("❌ Depo transfer response'dan ID çıkarılamadı: {Response}", responseText);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Depo transfer oluşturma hatası");
            throw;
        }
    }
    
    /// <summary>
    /// Luca DSH Stok Hareketi Fişi (Fire, Sarf, Sayım Fazlası vb.)
    /// </summary>
    public async Task<long> CreateStockVoucherAsync(LucaStockVoucherRequest request)
    {
        try
        {
            await EnsureAuthenticatedAsync();
            
            // LucaStockVoucherRequest → LucaCreateDshBaslikRequest dönüşümü
            var dshRequest = new LucaCreateDshBaslikRequest
            {
                BelgeSeri = request.StkDshBaslik.BelgeSeri,
                BelgeNo = request.StkDshBaslik.BelgeNo,
                BelgeTarihi = request.StkDshBaslik.BelgeTarihi,
                BelgeAciklama = request.StkDshBaslik.BelgeAciklama,
                BelgeTurDetayId = request.StkDshBaslik.BelgeTurDetayId,
                DepoKodu = request.StkDshBaslik.DepoKodu,
                ParaBirimKod = request.StkDshBaslik.ParaBirimKod,
                DetayList = request.StkDshBaslik.DetayList
                    .Select(r => new LucaCreateDshDetayRequest
                    {
                        KartTuru = r.KartTuru,
                        KartKodu = r.KartKodu,
                        KartAdi = r.KartAdi,
                        Miktar = r.Miktar,
                        OlcuBirimi = r.OlcuBirimi,
                        BirimFiyat = r.BirimFiyat,
                        Aciklama = r.Aciklama,
                        LotNo = r.LotNo,
                        SeriNo = r.SeriNo
                    }).ToList()
            };
            
            var result = await CreateOtherStockMovementAsync(dshRequest);
            
            // Response'u logla
            var responseText = result.GetRawText();
            _logger.LogInformation("🔍 DSH Stok Fişi Response: {Response}", responseText);
            
            // Response'dan ID çıkar
            if (result.TryGetProperty("id", out var idProp) || result.TryGetProperty("ssDshBaslikId", out idProp))
            {
                var id = idProp.GetInt64();
                _logger.LogInformation("✅ DSH Stok Fişi ID bulundu: {Id}", id);
                return id;
            }
            
            // Alternatif: data.id
            if (result.TryGetProperty("data", out var dataProp) && dataProp.TryGetProperty("id", out idProp))
            {
                var id = idProp.GetInt64();
                _logger.LogInformation("✅ DSH Stok Fişi ID (data.id) bulundu: {Id}", id);
                return id;
            }
            
            // success ve message kontrol et
            if (result.TryGetProperty("success", out var successProp))
            {
                var success = successProp.GetBoolean();
                _logger.LogWarning("⚠️ DSH Stok Fişi success: {Success}", success);
                
                if (result.TryGetProperty("message", out var messageProp))
                {
                    _logger.LogWarning("⚠️ DSH Stok Fişi message: {Message}", messageProp.GetString());
                }
            }
            
            _logger.LogWarning("❌ DSH stok fişi response'dan ID çıkarılamadı: {Response}", responseText);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DSH stok fişi oluşturma hatası");
            throw;
        }
    }
    
    public async Task<JsonElement> CreateStockCountResultAsync(LucaCreateStockCountRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.StockCountResult, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> CreateWarehouseAsync(LucaCreateWarehouseRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.Warehouse, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> CreateCreditCardEntryAsync(LucaCreateCreditCardEntryRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.CreditCardEntry, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> CreateCreditCardEntryAsync(
        Payment payment,
        Customer customer,
        string belgeSeri,
        string kasaCariKodu,
        DateTime? vadeTarihi = null,
        bool? avansFlag = null)
    {
        var request = MappingHelper.MapToLucaKrediKartiGiris(payment, customer, belgeSeri, kasaCariKodu, vadeTarihi, avansFlag);
        return await CreateCreditCardEntryAsync(request);
    }
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            _logger.LogInformation("Testing connection to Luca API");

            var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
            var response = await client.GetAsync(_settings.Endpoints.Health);
            var isConnected = response.IsSuccessStatusCode;

            _logger.LogInformation("Luca API connection test result: {IsConnected}", isConnected);
            return isConnected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing connection to Luca API");
            return false;
        }
    }
    private void EnsureInvoiceDefaults(IEnumerable<LucaInvoiceDto> invoices)
    {
        if (invoices == null)
        {
            return;
        }

        foreach (var invoice in invoices)
        {
            EnsureInvoiceDefaults(invoice);
        }
    }
    private void EnsureInvoiceDefaults(LucaInvoiceDto? invoice)
    {
        if (invoice == null)
        {
            return;
        }

        invoice.GnlOrgSsBelge ??= new LucaBelgeDto();
        var belge = invoice.GnlOrgSsBelge;

        if (string.IsNullOrWhiteSpace(belge.BelgeSeri))
        {
            belge.BelgeSeri = string.IsNullOrWhiteSpace(_settings.DefaultBelgeSeri)
                ? "EFA2025"
                : _settings.DefaultBelgeSeri;
        }

        if (belge.BelgeTurDetayId <= 0)
        {
            var defaultBelgeTurDetayId = TryGetDefaultBelgeTurDetayId("SalesInvoice");
            if (defaultBelgeTurDetayId.HasValue)
            {
                belge.BelgeTurDetayId = defaultBelgeTurDetayId.Value;
            }
        }

        if (belge.BelgeTarihi == default)
        {
            belge.BelgeTarihi = invoice.DocumentDate;
        }

        if (!belge.VadeTarihi.HasValue)
        {
            belge.VadeTarihi = invoice.DueDate;
        }

        if (!belge.BelgeNo.HasValue && int.TryParse(invoice.DocumentNo, out var parsedNo))
        {
            belge.BelgeNo = parsedNo;
        }

        if (string.IsNullOrWhiteSpace(belge.BelgeAciklama))
        {
            belge.BelgeAciklama = Truncate($"Invoice {invoice.DocumentNo}", 250);
        }

        if (!invoice.FaturaTur.HasValue || invoice.FaturaTur.Value <= 0)
        {
            invoice.FaturaTur = 1;
        }

        if (!invoice.MusteriTedarikci.HasValue || invoice.MusteriTedarikci.Value <= 0)
        {
            invoice.MusteriTedarikci = 1;
        }

        if (string.IsNullOrWhiteSpace(invoice.ParaBirimKod))
        {
            invoice.ParaBirimKod = "TRY";
        }

        
        if (!invoice.KdvFlag.HasValue)
        {
            invoice.KdvFlag = true;
        }

        if (string.IsNullOrWhiteSpace(invoice.CariKodu))
        {
            throw new InvalidOperationException("CariKodu (müşteri kodu) zorunludur");
        }

        if (invoice.Lines == null || !invoice.Lines.Any())
        {
            throw new InvalidOperationException("Fatura detayları (Lines) zorunludur");
        }

        foreach (var line in invoice.Lines)
        {
            EnsureLineDefaults(line);
        }

        void EnsureLineDefaults(LucaInvoiceItemDto line)
        {
            if (line == null)
            {
                throw new InvalidOperationException("Fatura detay satırı boş olamaz");
            }

            SetNumericLineProperty(line, "KartTuru", 1);

            if (string.IsNullOrWhiteSpace(line.Unit))
            {
                line.Unit = "ADET";
            }

            var measurementProperty = line.GetType().GetProperty("OlcuBirimi");
            if (measurementProperty != null &&
                measurementProperty.PropertyType == typeof(string))
            {
                var measurementValue = measurementProperty.GetValue(line) as string;
                if (string.IsNullOrWhiteSpace(measurementValue))
                {
                    measurementProperty.SetValue(line, "ADET");
                }
            }
            else if (!line.OlcuBirimi.HasValue || line.OlcuBirimi <= 0)
            {
                if (_settings.DefaultOlcumBirimiId > 0)
                {
                    line.OlcuBirimi = _settings.DefaultOlcumBirimiId;
                }
            }

            var unitPrice = ReadDecimalProperty(line, "BirimFiyat", line.UnitPrice);
            var quantity = ReadDecimalProperty(line, "Miktar", line.Quantity);

            if (unitPrice <= 0 || quantity <= 0)
            {
                var code = ReadStringProperty(line, "KartKodu");
                if (string.IsNullOrWhiteSpace(code))
                {
                    code = line.ProductCode;
                }

                throw new InvalidOperationException($"Satır için birim fiyat ve miktar zorunludur: {code}");
            }
        }

        void SetNumericLineProperty(object lineItem, string propertyName, int defaultValue)
        {
            var property = lineItem.GetType().GetProperty(propertyName);
            if (property == null || !property.CanRead || !property.CanWrite)
            {
                return;
            }

            var raw = property.GetValue(lineItem);
            var numeric = ConvertToNullableLong(raw);
            if (!numeric.HasValue || numeric.Value <= 0)
            {
                var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                var converted = Convert.ChangeType(defaultValue, targetType, CultureInfo.InvariantCulture);
                property.SetValue(lineItem, converted);
            }
        }

        decimal ReadDecimalProperty(object lineItem, string propertyName, decimal fallback)
        {
            var property = lineItem.GetType().GetProperty(propertyName);
            if (property == null || !property.CanRead)
            {
                return fallback;
            }

            var raw = property.GetValue(lineItem);
            if (raw == null)
            {
                return fallback;
            }

            try
            {
                return Convert.ToDecimal(raw, CultureInfo.InvariantCulture);
            }
            catch
            {
                return fallback;
            }
        }

        string? ReadStringProperty(object lineItem, string propertyName)
        {
            var property = lineItem.GetType().GetProperty(propertyName);
            if (property == null || !property.CanRead)
            {
                return null;
            }

            var raw = property.GetValue(lineItem);
            return raw?.ToString();
        }

        long? ConvertToNullableLong(object? value)
        {
            if (value == null)
            {
                return null;
            }

            try
            {
                return Convert.ToInt64(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
        }
    }

    private long? TryGetDefaultBelgeTurDetayId(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var defaultsProperty = _settings.GetType().GetProperty("DefaultBelgeTurDetayId");
        if (defaultsProperty == null)
        {
            return null;
        }

        var defaults = defaultsProperty.GetValue(_settings);
        if (defaults == null)
        {
            return null;
        }

        if (defaults is IDictionary<string, long> typedDict && typedDict.TryGetValue(key, out var typedValue))
        {
            return typedValue;
        }

        if (defaults is IDictionary dictionary)
        {
            if (dictionary.Contains(key))
            {
                return Convert.ToInt64(dictionary[key]);
            }

            var lowered = key.ToLowerInvariant();
            if (dictionary.Contains(lowered))
            {
                return Convert.ToInt64(dictionary[lowered]);
            }
        }

        var matchingProperty = defaults.GetType().GetProperty(key);
        if (matchingProperty != null)
        {
            var propertyValue = matchingProperty.GetValue(defaults);
            if (propertyValue != null && long.TryParse(propertyValue.ToString(), out var result))
            {
                return result;
            }
        }

        return null;
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed.Substring(0, maxLength);
    }

        private ByteArrayContent CreateFormContentCp1254(string payloadJson)
        {
            var pairs = new List<string>();
            try
            {
                using var doc = JsonDocument.Parse(payloadJson);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        string valueStr;
                        switch (prop.Value.ValueKind)
                        {
                            case JsonValueKind.String:
                                valueStr = prop.Value.GetString() ?? string.Empty;
                                break;
                            case JsonValueKind.Number:
                                valueStr = prop.Value.GetRawText();
                                break;
                            case JsonValueKind.True:
                                valueStr = "true";
                                break;
                            case JsonValueKind.False:
                                valueStr = "false";
                                break;
                            case JsonValueKind.Null:
                                valueStr = string.Empty;
                                break;
                            default:
                                valueStr = prop.Value.GetRawText();
                                break;
                        }

                        var k = UrlEncodeCp1254(prop.Name ?? string.Empty);
                        var v = UrlEncodeCp1254(valueStr ?? string.Empty);
                        pairs.Add(k + "=" + v);
                    }
                }
            }
            catch
            {
                // fallback: send raw JSON as single field 'payload'
                var k = UrlEncodeCp1254("payload");
                var v = UrlEncodeCp1254(payloadJson ?? string.Empty);
                pairs.Add(k + "=" + v);
            }

            var form = string.Join("&", pairs);
            var bytes = _encoding.GetBytes(form);
            var content = new ByteArrayContent(bytes);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded") { CharSet = "windows-1254" };
            return content;
        }

    private HttpContent CreateKozaContent(string json)
    {
        var payload = json ?? string.Empty;
        var content = new ByteArrayContent(_encoding.GetBytes(payload));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
        {
            CharSet = _encoding.WebName
        };
        return content;
    }

    private void ApplyManualSessionCookie(HttpRequestMessage? request)
    {
        try
        {
            if (request == null) return;

            // If the CookieContainer already has cookies for this host, prefer sending the full cookie set.
            // Some Koza flows rely on multiple cookies; sending only JSESSIONID can lead to "Login olunmalı.".
            try
            {
                if (_cookieContainer != null && !string.IsNullOrWhiteSpace(_settings.BaseUrl))
                {
                    var baseUri = new Uri(_settings.BaseUrl.TrimEnd('/') + "/");
                    var cookies = _cookieContainer.GetCookies(baseUri).Cast<System.Net.Cookie>().ToList();
                    if (cookies.Count > 0)
                    {
                        var cookieHeader = string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}"));
                        request.Headers.Remove("Cookie");
                        request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
                        _logger.LogDebug("🍪 ApplyManualSessionCookie: Applied CookieContainer cookies (count={Count}, names={Names})",
                            cookies.Count, string.Join(",", cookies.Select(c => c.Name).Distinct()));
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "🍪 ApplyManualSessionCookie: Failed to apply CookieContainer cookie set; falling back to single-cookie mode");
            }
            
            // 🔥 DEBUG: Cookie durumunu logla
            var cookieSource = "none";
            string? cookieToApply = null;
            
            // Öncelik sırası: 1) _manualJSessionId (login'den gelen), 2) _sessionCookie, 3) CookieContainer, 4) ManualSessionCookie (config)
            if (!string.IsNullOrWhiteSpace(_manualJSessionId))
            {
                cookieToApply = _manualJSessionId;
                cookieSource = "_manualJSessionId";
            }
            else if (!string.IsNullOrWhiteSpace(_sessionCookie))
            {
                cookieToApply = _sessionCookie;
                cookieSource = "_sessionCookie";
            }
            else
            {
                // CookieContainer'dan almayı dene
                var containerCookie = TryGetJSessionFromContainer();
                if (!string.IsNullOrWhiteSpace(containerCookie))
                {
                    cookieToApply = containerCookie.StartsWith("JSESSIONID=", StringComparison.OrdinalIgnoreCase) 
                        ? containerCookie 
                        : "JSESSIONID=" + containerCookie;
                    cookieSource = "CookieContainer";
                }
                else if (!string.IsNullOrWhiteSpace(_settings?.ManualSessionCookie))
                {
                    cookieToApply = _settings.ManualSessionCookie;
                    cookieSource = "ManualSessionCookie(config)";
                }
            }
            
            if (string.IsNullOrWhiteSpace(cookieToApply)) 
            {
                _logger.LogDebug("🍪 ApplyManualSessionCookie: No cookie available to apply");
                return;
            }

            var trimmed = cookieToApply.Trim();
            if (trimmed.IndexOf("FILL_ME", StringComparison.OrdinalIgnoreCase) >= 0) 
            {
                _logger.LogDebug("🍪 ApplyManualSessionCookie: Cookie contains FILL_ME placeholder, skipping");
                return;
            }

            // Cookie formatını normalize et
            if (!trimmed.StartsWith("JSESSIONID=", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = "JSESSIONID=" + trimmed;
            }

            // Always set/replace cookie header; a stale Cookie header causes "Login olunmalı" even after a successful login.
            request.Headers.Remove("Cookie");
            request.Headers.TryAddWithoutValidation("Cookie", trimmed);
            _logger.LogDebug("🍪 ApplyManualSessionCookie: Applied cookie from {Source} (preview: {Preview})",
                cookieSource,
                trimmed.Length > 50 ? trimmed.Substring(0, 50) + "..." : trimmed);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to apply manual session cookie to outgoing request");
        }
    }
    private void ValidateFaturaKapama(LucaFaturaKapamaDto dto, long belgeTurDetayId)
    {
        if (dto == null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        if (FaturaKapamaCariRules.TryGetValue(belgeTurDetayId, out var rule) && dto.CariTur != rule.ExpectedCariTur)
        {
            throw new InvalidOperationException(rule.ErrorMessage);
        }
    }
    private static async Task<string> ReadContentPreviewAsync(HttpContent content)
    {
        if (content == null)
        {
            return string.Empty;
        }

        try
        {
            return await content.ReadAsStringAsync();
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
        public async Task<List<LucaInvoiceDto>> FetchInvoicesAsync(DateTime? fromDate = null)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            var queryDate = fromDate?.ToString("yyyy-MM-dd") ?? DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
            var endpoint = $"{_settings.Endpoints.Invoices}?fromDate={queryDate}";

            _logger.LogInformation("Fetching invoices from Luca since {Date}", queryDate);

            var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
            var response = await client.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var invoices = JsonSerializer.Deserialize<List<LucaInvoiceDto>>(content, _jsonOptions) ?? new List<LucaInvoiceDto>();

                _logger.LogInformation("Successfully fetched {Count} invoices from Luca", invoices.Count);
                return invoices;
            }
            else
            {
                _logger.LogError("Failed to fetch invoices from Luca. Status: {StatusCode}", response.StatusCode);
                return new List<LucaInvoiceDto>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching invoices from Luca");
            return new List<LucaInvoiceDto>();
        }
    }
    public async Task<List<LucaStockDto>> FetchStockMovementsAsync(DateTime? fromDate = null)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            var queryDate = fromDate?.ToString("yyyy-MM-dd") ?? DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
            var endpoint = $"{_settings.Endpoints.Stock}?fromDate={queryDate}";

            _logger.LogInformation("Fetching stock movements from Luca since {Date}", queryDate);

            var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
            var response = await client.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var stockMovements = JsonSerializer.Deserialize<List<LucaStockDto>>(content, _jsonOptions) ?? new List<LucaStockDto>();

                _logger.LogInformation("Successfully fetched {Count} stock movements from Luca", stockMovements.Count);
                return stockMovements;
            }
            else
            {
                _logger.LogError("Failed to fetch stock movements from Luca. Status: {StatusCode}", response.StatusCode);
                return new List<LucaStockDto>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching stock movements from Luca");
            return new List<LucaStockDto>();
        }
    }
    public async Task<List<LucaCustomerDto>> FetchCustomersAsync(DateTime? fromDate = null)
    {
        try
        {
            _logger.LogInformation("Fetching customers from Luca (fromDate={FromDate})", fromDate);
            var element = await ListCustomersAsync();
            var customers = new List<LucaCustomerDto>();

            JsonElement arrayEl = default;
            if (element.ValueKind == JsonValueKind.Array)
            {
                arrayEl = element;
            }
            else if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                {
                    arrayEl = data;
                }
                else if (element.TryGetProperty("finMusteriListesi", out var finMusteriListesi) && finMusteriListesi.ValueKind == JsonValueKind.Array)
                {
                    arrayEl = finMusteriListesi;
                }
                else if (element.TryGetProperty("list", out var list) && list.ValueKind == JsonValueKind.Array)
                {
                    arrayEl = list;
                }
            }

            if (arrayEl.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("Customer list response from Luca did not contain an array; returning empty list");
                return customers;
            }

            foreach (var item in arrayEl.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    continue;

                var dto = new LucaCustomerDto
                {
                    CustomerCode = TryGetProperty(item, "kod", "cariKodu") ?? string.Empty,
                    Title = TryGetProperty(item, "tanim", "cariTanim") ?? string.Empty,
                    TaxNo = TryGetProperty(item, "vergiNo", "vkn", "tcKimlikNo") ?? string.Empty,
                    ContactPerson = TryGetProperty(item, "yetkili", "yetkiliKisi"),
                    Phone = TryGetProperty(item, "telefon"),
                    Email = TryGetProperty(item, "email"),
                    Address = TryGetProperty(item, "adresSerbest", "adres"),
                    City = TryGetProperty(item, "il"),
                    Country = TryGetProperty(item, "ulke", "country")
                };

                customers.Add(dto);
            }

            _logger.LogInformation("Successfully fetched {Count} customers from Luca", customers.Count);
            return customers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching customers from Luca");
            return new List<LucaCustomerDto>();
        }
    }
    public async Task<List<LucaProductDto>> FetchProductsAsync(DateTime? fromDate = null)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            _logger.LogInformation("Fetching products (stock cards) from Luca (Koza)...");

            
            var json = JsonSerializer.Serialize(new { }, _jsonOptions);
            var content = CreateKozaContent(json);

            var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.StockCards)
            {
                Content = content
            };
            ApplyManualSessionCookie(httpRequest);
            httpRequest.Headers.Add("No-Paging", "true");

            var response = await client.SendAsync(httpRequest);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch products from Luca. Status: {Status}", response.StatusCode);
                return new List<LucaProductDto>();
            }

            var responseContent = await response.Content.ReadAsStringAsync();

            try
            {
                using var doc = JsonDocument.Parse(responseContent);

                
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (doc.RootElement.TryGetProperty("list", out var listEl) && listEl.ValueKind == JsonValueKind.Array)
                    {
                        return JsonSerializer.Deserialize<List<LucaProductDto>>(listEl.GetRawText(), _jsonOptions) ?? new List<LucaProductDto>();
                    }

                    if (doc.RootElement.TryGetProperty("stkSkartList", out var skartList) && skartList.ValueKind == JsonValueKind.Array)
                    {
                        return JsonSerializer.Deserialize<List<LucaProductDto>>(skartList.GetRawText(), _jsonOptions) ?? new List<LucaProductDto>();
                    }

                    if (doc.RootElement.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Array)
                    {
                        return JsonSerializer.Deserialize<List<LucaProductDto>>(dataEl.GetRawText(), _jsonOptions) ?? new List<LucaProductDto>();
                    }
                }

                
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    return JsonSerializer.Deserialize<List<LucaProductDto>>(responseContent, _jsonOptions) ?? new List<LucaProductDto>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse products response from Luca; attempting generic deserialize");
            }

            
            return JsonSerializer.Deserialize<List<LucaProductDto>>(responseContent, _jsonOptions) ?? new List<LucaProductDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching products from Luca");
            return new List<LucaProductDto>();
        }
    }

    public async Task<List<LucaProductDto>> FetchProductsAsync(System.Threading.CancellationToken cancellationToken = default)
    {
        var result = new List<LucaProductDto>();

        var cookieContainer = new CookieContainer();
        var handler = new HttpClientHandler
        {
            CookieContainer = cookieContainer,
            UseCookies = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            AllowAutoRedirect = true
        };

        var baseAddr = !string.IsNullOrWhiteSpace(_settings.BaseUrl) ? new Uri(_settings.BaseUrl.TrimEnd('/') + "/") : null;

        using var client = new HttpClient(handler)
        {
            BaseAddress = baseAddr
        };

        try
        {
            var loggedIn = await PerformLoginOnClientAsync(client, cookieContainer, cancellationToken);
            if (!loggedIn)
            {
                _logger.LogError("[Luca] FetchProductsAsync: Login/branch selection failed.");
                return result;
            }

            var url = !string.IsNullOrWhiteSpace(_settings.Endpoints?.StockCards) ? _settings.Endpoints.StockCards : "ListeleStkSkart.do";

            var formPairs = new List<KeyValuePair<string, string>>();

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new FormUrlEncodedContent(formPairs)
            };

            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Luca] FetchProductsAsync: HTTP request failed.");
                return result;
            }

            var statusCode = (int)response.StatusCode;
            var rawBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

            var encoding1254 = Encoding.GetEncoding(1254);
            var bodyText = encoding1254.GetString(rawBytes);

            try { await AppendRawLogAsync("FetchProducts", (client.BaseAddress?.ToString() ?? string.Empty) + url, 
                $"FORM:{string.Join("&", formPairs.Select(p => $"{p.Key}={p.Value}"))}",
                response.StatusCode, bodyText); } catch (Exception) { }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[Luca] FetchProductsAsync: Response not successful. Status: {Status}", statusCode);
                return result;
            }

            if (IsJson(bodyText))
            {
                result = ParseKozaProductJson(bodyText);
            }
            else
            {
                result = ParseKozaProductHtml(bodyText);
            }

            _logger.LogInformation("[Luca] FetchProductsAsync: Parsed {Count} products from Koza.", result.Count);
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[Luca] FetchProductsAsync: cancelled");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Luca] FetchProductsAsync: unexpected error");
            return result;
        }
    }

    private bool IsJson(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        text = text.Trim();
        return (text.StartsWith("{") && text.EndsWith("}")) ||
               (text.StartsWith("[") && text.EndsWith("]"));
    }

    private List<Katana.Core.DTOs.LucaProductDto> ParseKozaProductJson(string json)
    {
        var list = new List<Katana.Core.DTOs.LucaProductDto>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            JsonElement dataEl = default;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("data", out var d) && d.ValueKind == JsonValueKind.Array) dataEl = d;
                else if (root.TryGetProperty("list", out var l) && l.ValueKind == JsonValueKind.Array) dataEl = l;
                else if (root.TryGetProperty("stkSkartList", out var s) && s.ValueKind == JsonValueKind.Array) dataEl = s;
            }
            else if (root.ValueKind == JsonValueKind.Array)
            {
                dataEl = root;
            }

            if (dataEl.ValueKind != JsonValueKind.Array) return list;

            foreach (var item in dataEl.EnumerateArray())
            {
                var code = item.TryGetProperty("kartKodu", out var codeEl) ? codeEl.GetString() ?? string.Empty : string.Empty;
                var name = item.TryGetProperty("kartAdi", out var nameEl) ? nameEl.GetString() ?? string.Empty : string.Empty;
                var category = item.TryGetProperty("kategoriAgacKod", out var catEl) ? catEl.GetString() : (item.TryGetProperty("kategori", out var cat2) ? cat2.GetString() : null);

                if (string.IsNullOrWhiteSpace(code)) continue;

                var dto = new Katana.Core.DTOs.LucaProductDto
                {
                    ProductCode = code,
                    ProductName = name,
                    Unit = item.TryGetProperty("olcumBirimi", out var u) ? u.GetString() : null
                };
                list.Add(dto);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ParseKozaProductJson failed");
        }

        return list;
    }

    private List<Katana.Core.DTOs.LucaProductDto> ParseKozaProductHtml(string html)
    {
        var list = new List<Katana.Core.DTOs.LucaProductDto>();
        try
        {
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            var rows = doc.DocumentNode.SelectNodes("//table[@id='grid']//tr[position()>1]")
                       ?? doc.DocumentNode.SelectNodes("//table//tr[position()>1]");
            if (rows == null) return list;

            foreach (var row in rows)
            {
                var cells = row.SelectNodes("./td");
                if (cells == null || cells.Count < 2) continue;

                var code = cells[0].InnerText.Trim();
                var name = cells.Count > 1 ? cells[1].InnerText.Trim() : string.Empty;
                var category = cells.Count > 2 ? cells[2].InnerText.Trim() : null;

                if (string.IsNullOrWhiteSpace(code)) continue;

                list.Add(new Katana.Core.DTOs.LucaProductDto
                {
                    ProductCode = WebUtility.HtmlDecode(code),
                    ProductName = WebUtility.HtmlDecode(name),
                    Unit = cells.Count > 3 ? WebUtility.HtmlDecode(cells[3].InnerText.Trim()) : null
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ParseKozaProductHtml failed");
        }
        return list;
    }

    private async Task<bool> PerformLoginOnClientAsync(HttpClient client, CookieContainer cookieContainer, System.Threading.CancellationToken cancellationToken = default)
    {
        try
        {
            var baseUri = client.BaseAddress ?? new Uri(_settings.BaseUrl?.TrimEnd('/') + "/");

            try
            {
                var getResp = await client.GetAsync(_settings.Endpoints.Auth ?? "Giris.do", cancellationToken);
                var getBody = await ReadResponseContentAsync(getResp);
                await AppendRawLogAsync("AUTH_LOGIN_GET_ONCLIENT", _settings.Endpoints.Auth, string.Empty, getResp.StatusCode, getBody);
            }
            catch (Exception)
            {
            }

            var loginAttempts = new List<(string desc, HttpContent content)>
            {
                ("JSON:orgCode_userName_userPassword", CreateKozaContent(
                    JsonSerializer.Serialize(new
                    {
                        orgCode = _settings.MemberNumber,
                        userName = _settings.Username,
                        userPassword = _settings.Password
                    }, _jsonOptions))),
                ("FORM:orgCode_user_girisForm.userPassword", new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "orgCode", _settings.MemberNumber },
                    { "user", _settings.Username },
                    { "girisForm.userPassword", _settings.Password },
                    { "girisForm.captchaInput", string.Empty }
                })),
                ("FORM:orgCode_userName_userPassword", new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "orgCode", _settings.MemberNumber },
                    { "userName", _settings.Username },
                    { "userPassword", _settings.Password }
                }))
            };

            foreach (var (desc, payload) in loginAttempts)
            {
                try
                {
                    var payloadText = await ReadContentPreviewAsync(payload);
                    var resp = await client.PostAsync(_settings.Endpoints.Auth, payload, cancellationToken);
                    var body = await ReadResponseContentAsync(resp);
                    await AppendRawLogAsync($"AUTH_LOGIN_ONCLIENT:{desc}", _settings.Endpoints.Auth, payloadText, resp.StatusCode, body);

                    try
                    {
                        if (cookieContainer != null)
                        {
                            var cookies = cookieContainer.GetCookies(baseUri);
                            var c = cookies.Cast<System.Net.Cookie>().FirstOrDefault(x => string.Equals(x.Name, "JSESSIONID", StringComparison.OrdinalIgnoreCase));
                            if (c != null && !string.IsNullOrWhiteSpace(c.Value))
                            {
                                return true;
                            }
                        }
                    }
                    catch { }

                    if (resp.IsSuccessStatusCode && IsKozaLoginSuccess(body))
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Login attempt on client failed: {Desc}", desc);
                }
            }

            _logger.LogWarning("PerformLoginOnClientAsync: login attempts failed");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PerformLoginOnClientAsync threw");
            return false;
        }
    }
    
    public async Task<List<LucaDespatchDto>> FetchDeliveryNotesAsync(DateTime? fromDate = null)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            _logger.LogInformation("Fetching delivery notes (irsaliye) from Luca");
            var element = await ListDeliveryNotesAsync(null, true);

            var results = new List<LucaDespatchDto>();

            
            JsonElement arrayEl = default;
            if (element.ValueKind == JsonValueKind.Array)
            {
                arrayEl = element;
            }
            else if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty("list", out var list) && list.ValueKind == JsonValueKind.Array)
                    arrayEl = list;
                else if (element.TryGetProperty("irsaliyeList", out var il) && il.ValueKind == JsonValueKind.Array)
                    arrayEl = il;
                else if (element.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                    arrayEl = data;
            }

            if (arrayEl.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("Delivery notes response did not contain an array; returning empty list");
                return results;
            }

            foreach (var item in arrayEl.EnumerateArray())
            {
                try
                {
                    var dto = new LucaDespatchDto();

                    if (item.TryGetProperty("belgeNo", out var bno))
                        dto.DocumentNo = bno.GetString() ?? string.Empty;

                    if (item.TryGetProperty("belgeTarihi", out var bdt))
                    {
                        if (bdt.ValueKind == JsonValueKind.String && DateTime.TryParse(bdt.GetString(), out var dt))
                            dto.DocumentDate = dt;
                        else if (bdt.ValueKind == JsonValueKind.Number && bdt.TryGetInt64(out var unix))
                            dto.DocumentDate = DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;
                    }

                    if (item.TryGetProperty("cariKodu", out var ck))
                        dto.CustomerCode = ck.GetString();

                    if (item.TryGetProperty("cariTanim", out var ct))
                        dto.CustomerTitle = ct.GetString();

                    
                    if (item.TryGetProperty("detayList", out var detay) && detay.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var line in detay.EnumerateArray())
                        {
                            try
                            {
                                var li = new LucaDespatchItemDto();
                                if (line.TryGetProperty("kartKodu", out var pk))
                                    li.ProductCode = pk.GetString() ?? string.Empty;
                                if (line.TryGetProperty("kartAdi", out var pn))
                                    li.ProductName = pn.GetString();
                                if (line.TryGetProperty("miktar", out var mq) && mq.ValueKind == JsonValueKind.Number)
                                    li.Quantity = mq.GetDecimal();
                                if (line.TryGetProperty("birimFiyat", out var up) && up.ValueKind == JsonValueKind.Number)
                                    li.UnitPrice = up.GetDecimal();
                                if (line.TryGetProperty("kdvOran", out var tr) && tr.ValueKind == JsonValueKind.Number)
                                    li.TaxRate = tr.GetDouble();

                                dto.Lines.Add(li);
                            }
                            catch (Exception) {  }
                        }
                    }

                    results.Add(dto);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse one delivery note item");
                }
            }

            _logger.LogInformation("Parsed {Count} delivery notes from Luca", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching delivery notes from Luca");
            return new List<LucaDespatchDto>();
        }
    }
    private bool NeedsBranchSelection(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return false;

        var lower = body.ToLowerInvariant();
        if (lower.Contains("şirket şube seçimi") || lower.Contains("sirket sube secimi") || lower.Contains("sube secimi yapilmali"))
            return true;

        if (lower.Contains("\"code\":1003") || lower.Contains("code\":1003") || lower.Contains("code\": 1003"))
            return true;

        return false;
    }

    /// <summary>
    /// Response'un HTML olup olmadığını kontrol eder (session timeout/login sayfası)
    /// </summary>
    private bool IsHtmlResponse(string? responseContent)
    {
        if (string.IsNullOrWhiteSpace(responseContent))
            return false;

        var trimmed = responseContent.TrimStart();
        
        // HTML başlangıç tag'leri
        if (trimmed.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("<html", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("<HTML", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("<!doctype", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Login sayfası veya error sayfası göstergeleri
        var lower = trimmed.ToLowerInvariant();
        if (lower.Contains("<title>") && lower.Contains("</title>") &&
            (lower.Contains("login") || lower.Contains("giriş") || lower.Contains("oturum") || lower.Contains("error")))
        {
            return true;
        }

        // HTML body tag'i varsa
        if (lower.Contains("<body") || lower.Contains("<head"))
        {
            return true;
        }

        return false;
    }

    private async Task AppendRawLogAsync(string tag, string? url, string requestBody, System.Net.HttpStatusCode? status, string responseBody)
    {
        try
        {
            var baseDir = AppContext.BaseDirectory ?? Directory.GetCurrentDirectory();
            var logDir = Path.Combine(baseDir, "logs");
            Directory.CreateDirectory(logDir);
            var file = Path.Combine(logDir, "luca-raw.log");

            var sb = new StringBuilder();
            sb.AppendLine("----");
            sb.AppendLine(DateTime.UtcNow.ToString("o") + " " + tag);
            sb.AppendLine("URL: " + (url ?? string.Empty));
            sb.AppendLine("Request:");
            sb.AppendLine(requestBody ?? string.Empty);
            sb.AppendLine("ResponseStatus: " + (status?.ToString() ?? "(null)"));
            sb.AppendLine("Response:");
            sb.AppendLine(responseBody ?? string.Empty);
            sb.AppendLine("----");

            await File.AppendAllTextAsync(file, sb.ToString());

            
            
            try
            {
                var cwd = Directory.GetCurrentDirectory();
                var repoLogDir = Path.Combine(cwd, "logs");
                if (!string.Equals(repoLogDir, logDir, StringComparison.OrdinalIgnoreCase))
                {
                    Directory.CreateDirectory(repoLogDir);
                    var repoFile = Path.Combine(repoLogDir, "luca-raw.log");
                    await File.AppendAllTextAsync(repoFile, sb.ToString());
                }
            }
            catch (Exception)
            {
                
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to append raw Luca log");
        }
    }
    private async Task SaveHttpTrafficAsync(string tag, HttpRequestMessage? request, HttpResponseMessage? response)
    {
        try
        {
            var baseDir = AppContext.BaseDirectory ?? Directory.GetCurrentDirectory();
            var logDir = Path.Combine(baseDir, "logs");
            Directory.CreateDirectory(logDir);

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var safeTag = SanitizeFileName(tag ?? "traffic");
            var filePath = Path.Combine(logDir, $"{safeTag}-http-{timestamp}.txt");

            var sb = new StringBuilder();
            sb.AppendLine("----");
            sb.AppendLine(DateTime.UtcNow.ToString("o") + " " + tag);

            var reqMsg = request ?? response?.RequestMessage;
            if (reqMsg != null)
            {
                sb.AppendLine("RequestUri: " + (reqMsg.RequestUri?.ToString() ?? string.Empty));
                sb.AppendLine("RequestMethod: " + reqMsg.Method.Method);
                sb.AppendLine("Request Headers:");
                foreach (var h in reqMsg.Headers)
                {
                    sb.AppendLine($"{h.Key}: {string.Join(",", h.Value)}");
                }
                if (reqMsg.Content != null)
                {
                    foreach (var h in reqMsg.Content.Headers)
                    {
                        sb.AppendLine($"{h.Key}: {string.Join(",", h.Value)}");
                    }
                }
            }
            else
            {
                sb.AppendLine("Request: (null)");
            }

            if (response != null)
            {
                sb.AppendLine("Response Status: " + response.StatusCode);
                sb.AppendLine("Response Headers:");
                foreach (var h in response.Headers)
                {
                    sb.AppendLine($"{h.Key}: {string.Join(",", h.Value)}");
                }
                if (response.Content != null)
                {
                    foreach (var h in response.Content.Headers)
                    {
                        sb.AppendLine($"{h.Key}: {string.Join(",", h.Value)}");
                    }
                }

                
                if (response.Headers.TryGetValues("Set-Cookie", out var scs))
                {
                    sb.AppendLine("Set-Cookie:");
                    foreach (var s in scs) sb.AppendLine(s);
                }
            }

            
            try
            {
                var cookieContainerLocal = _cookieContainer;
                if (cookieContainerLocal != null && !string.IsNullOrWhiteSpace(_settings.BaseUrl))
                {
                    var uri = new Uri(_settings.BaseUrl);
                    var cookieCol = cookieContainerLocal.GetCookies(uri);
                    var list = new List<object>();
                    foreach (System.Net.Cookie ck in cookieCol)
                    {
                        list.Add(new
                        {
                            ck.Name,
                            ck.Value,
                            ck.Domain,
                            ck.Path,
                            Expires = ck.Expires == DateTime.MinValue ? (DateTime?)null : ck.Expires,
                            ck.Secure,
                            ck.HttpOnly
                        });
                    }
                    var cookieFile = Path.Combine(logDir, $"{safeTag}-cookies-{timestamp}.json");
                    await File.WriteAllTextAsync(cookieFile, JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }));
                    sb.AppendLine("CookiesFile: " + cookieFile);
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("Cookie dump failed: " + ex.Message);
            }

            sb.AppendLine("----");

            try
            {
                await File.WriteAllTextAsync(filePath, sb.ToString());
            }
            catch (Exception ex)
            {
                try
                {
                    _logger.LogWarning(ex, "Failed to write http traffic file '{FilePath}', falling back to safe filename.", filePath);
                }
                catch { }

                var fallback = Path.Combine(logDir, $"http-traffic-{timestamp}-{Guid.NewGuid().ToString("N").Substring(0,8)}.txt");
                await File.WriteAllTextAsync(fallback, sb.ToString());
                filePath = fallback;
            }

            try
            {
                var cwd = Directory.GetCurrentDirectory();
                var repoLogDir = Path.Combine(cwd, "logs");
                if (!string.Equals(repoLogDir, logDir, StringComparison.OrdinalIgnoreCase))
                {
                    Directory.CreateDirectory(repoLogDir);
                    var repoFile = Path.Combine(repoLogDir, Path.GetFileName(filePath));
                    try
                    {
                        await File.WriteAllTextAsync(repoFile, sb.ToString());
                    }
                    catch (Exception ex)
                    {
                        try { _logger.LogWarning(ex, "Failed to write repo-copy of http traffic file '{RepoFile}'", repoFile); } catch { }
                    }
                }
            }
            catch (Exception)
            {
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save HTTP traffic diagnostics");
        }
    }
    private async Task<string?> SaveHttpTrafficAndGetFilePathAsync(string tag, HttpRequestMessage? request, HttpResponseMessage? response)
    {
        try
        {
            var baseDir = AppContext.BaseDirectory ?? Directory.GetCurrentDirectory();
            var logDir = Path.Combine(baseDir, "logs");
            Directory.CreateDirectory(logDir);

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var safeTag = SanitizeFileName(tag ?? "traffic");
            var filePath = Path.Combine(logDir, $"{safeTag}-http-{timestamp}.txt");

            var sb = new StringBuilder();
            sb.AppendLine("----");
            sb.AppendLine(DateTime.UtcNow.ToString("o") + " " + tag);

            var reqMsg = request ?? response?.RequestMessage;
            if (reqMsg != null)
            {
                sb.AppendLine("RequestUri: " + (reqMsg.RequestUri?.ToString() ?? string.Empty));
                sb.AppendLine("RequestMethod: " + reqMsg.Method.Method);
                sb.AppendLine("Request Headers:");
                foreach (var h in reqMsg.Headers)
                {
                    sb.AppendLine($"{h.Key}: {string.Join(",", h.Value)}");
                }
                if (reqMsg.Content != null)
                {
                    foreach (var h in reqMsg.Content.Headers)
                    {
                        sb.AppendLine($"{h.Key}: {string.Join(",", h.Value)}");
                    }
                }
            }
            else
            {
                sb.AppendLine("Request: (null)");
            }

            if (response != null)
            {
                sb.AppendLine("Response Status: " + response.StatusCode);
                sb.AppendLine("Response Headers:");
                foreach (var h in response.Headers)
                {
                    sb.AppendLine($"{h.Key}: {string.Join(",", h.Value)}");
                }
                if (response.Content != null)
                {
                    foreach (var h in response.Content.Headers)
                    {
                        sb.AppendLine($"{h.Key}: {string.Join(",", h.Value)}");
                    }
                }

                if (response.Headers.TryGetValues("Set-Cookie", out var scs))
                {
                    sb.AppendLine("Set-Cookie:");
                    foreach (var s in scs) sb.AppendLine(s);
                }
            }
            try
            {
                var cookieContainerLocal = _cookieContainer;
                if (cookieContainerLocal != null && !string.IsNullOrWhiteSpace(_settings.BaseUrl))
                {
                    var uri = new Uri(_settings.BaseUrl);
                    var cookieCol = cookieContainerLocal.GetCookies(uri);
                    var list = new List<object>();
                    foreach (System.Net.Cookie ck in cookieCol)
                    {
                        list.Add(new
                        {
                            ck.Name,
                            ck.Value,
                            ck.Domain,
                            ck.Path,
                            Expires = ck.Expires == DateTime.MinValue ? (DateTime?)null : ck.Expires,
                            ck.Secure,
                            ck.HttpOnly
                        });
                    }
                    var cookieFile = Path.Combine(logDir, $"{safeTag}-cookies-{timestamp}.json");
                    await File.WriteAllTextAsync(cookieFile, JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }));
                    sb.AppendLine("CookiesFile: " + cookieFile);
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("Cookie dump failed: " + ex.Message);
            }

            sb.AppendLine("----");
            await File.WriteAllTextAsync(filePath, sb.ToString());

            try
            {
                var cwd = Directory.GetCurrentDirectory();
                var repoLogDir = Path.Combine(cwd, "logs");
                if (!string.Equals(repoLogDir, logDir, StringComparison.OrdinalIgnoreCase))
                {
                    Directory.CreateDirectory(repoLogDir);
                    var repoFile = Path.Combine(repoLogDir, Path.GetFileName(filePath));
                    await File.WriteAllTextAsync(repoFile, sb.ToString());
                    return repoFile;
                }
            }
            catch (Exception)
            {
            }

            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save HTTP traffic diagnostics (and return file path)");
            return null;
        }
    }

    private async Task<string> ReadResponseContentAsync(HttpResponseMessage response)
    {
        var charset = response.Content.Headers.ContentType?.CharSet?.Trim().ToLowerInvariant();
        var bytes = await response.Content.ReadAsByteArrayAsync();

        
        if (!string.IsNullOrWhiteSpace(charset))
        {
            if (charset.Contains("1254") || charset.Contains("iso-8859-9"))
            {
                try { return _encoding.GetString(bytes); } catch {  }
            }
            if (charset.Contains("utf-8"))
            {
                try { return Encoding.UTF8.GetString(bytes); } catch {  }
            }
        }

        
        try { return Encoding.UTF8.GetString(bytes); } catch {  }
        try { return _encoding.GetString(bytes); } catch {  }
        return string.Empty;
    }

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "file";
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '.' || c == '_' || c == '-')
            {
                sb.Append(c);
            }
            else
            {
                sb.Append('_');
            }
        }

        var s = sb.ToString();
        while (s.Contains("__")) s = s.Replace("__", "_");
        if (s.Length > 120) s = s.Substring(0, 120);
        s = s.TrimEnd('.', ' ');
        if (string.IsNullOrWhiteSpace(s)) return "file";
        return s;
    }
    private static long TryParseId(string responseContent)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Number && root.TryGetInt64(out var num))
            {
                return num;
            }
            string[] idKeys = { "id", "faturaId", "irsaliyeId", "ssIrsaliyeBaslikId", "ssSiparisBaslikId", "belgeId", "entityId" };
            foreach (var key in idKeys)
            {
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(key, out var prop))
                {
                    if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt64(out var parsed))
                        return parsed;
                    if (prop.ValueKind == JsonValueKind.String && long.TryParse(prop.GetString(), out var parsedStr))
                        return parsedStr;
                }
            }
        }
        catch
        {
            
        }
        return 0;
    }
    private List<T> DeserializeList<T>(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            return JsonSerializer.Deserialize<List<T>>(element.GetRawText(), _jsonOptions) ?? new List<T>();
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
            {
                return JsonSerializer.Deserialize<List<T>>(data.GetRawText(), _jsonOptions) ?? new List<T>();
            }
            if (element.TryGetProperty("list", out var list) && list.ValueKind == JsonValueKind.Array)
            {
                return JsonSerializer.Deserialize<List<T>>(list.GetRawText(), _jsonOptions) ?? new List<T>();
            }
        }
        return new List<T>();
    }

    /// <summary>
    /// Search for a stock card by SKU/KartKodu in Luca.
    /// Returns the skartId if found, null if not found.
    /// </summary>
    public async Task<long?> FindStockCardBySkuAsync(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            return null;

        // 🔥 CACHE KONTROLÜ: Aynı session'da tekrar sorgulamayı önle
        await _stockCardCacheLock.WaitAsync();
        bool cacheWasEmpty = _stockCardCache.Count == 0;
        try
        {
            if (_stockCardCache.TryGetValue(sku, out var cachedId))
            {
                _logger.LogDebug("🔄 Cache HIT: {SKU} → {Id}", sku, cachedId);
                return cachedId;
            }
        }
        finally
        {
            _stockCardCacheLock.Release();
        }

        // 🚨 FAILOVER: Cache boşsa UYAR!
        if (cacheWasEmpty)
        {
            _logger.LogWarning("⚠️ CACHE BOŞ! Cache warming başarısız olmuş olabilir.");
            _logger.LogWarning("   → SKU: {SKU} için CANLI API sorgusu yapılacak (yavaş!)", sku);
        }

        try
        {
            _logger.LogDebug("🔍 Luca'da stok kartı aranıyor (FUZZY SEARCH): {SKU}", sku);
            
            await EnsureAuthenticatedAsync();
            await EnsureBranchSelectedAsync();

            // 🎯 FUZZY SEARCH: SKU ile BAŞLAYAN tüm kayıtları getir
            // Bu sayede "81.06301-8211", "81.06301-8211-V2", "81.06301-8211-V3" hepsini bulabiliriz
            var request = new LucaListStockCardsRequest
            {
                StkSkart = new LucaStockCardCodeFilter
                {
                    KodBas = sku,
                    KodBit = sku + "ZZZZ",  // Alfabetik range için üst limit
                    KodOp = "between"       // SKU ile başlayan tüm kayıtlar
                }
            };

            var result = await ListStockCardsAsync(request);

            // 🔥 DEFENSIVE PROGRAMMING: BOŞ/GEÇERSİZ RESPONSE KONTROLÜ + RETRY
            if (result.ValueKind == JsonValueKind.Undefined || result.ValueKind == JsonValueKind.Null)
            {
                _logger.LogWarning("⚠️ [RETRY] Luca'dan geçersiz response geldi (Undefined/Null) - SKU: {SKU}", sku);
                _logger.LogWarning("   Session yenileniyor ve TEKRAR DENENİYOR...");
                
                try
                {
                    await ForceSessionRefreshAsync();
                    _logger.LogInformation("✅ Session yenilendi, SKU: {SKU} için tekrar sorgulanıyor...", sku);
                    
                    result = await ListStockCardsAsync(request);
                    
                    // Retry sonrası hala boş/geçersiz mi?
                    if (result.ValueKind == JsonValueKind.Undefined || result.ValueKind == JsonValueKind.Null)
                    {
                        _logger.LogError("❌ [RETRY FAILED] Session yenileme sonrası hala geçersiz response - SKU: {SKU}", sku);
                        return null;
                    }
                }
                catch (Exception retryEx)
                {
                    _logger.LogError(retryEx, "❌ [RETRY EXCEPTION] Session refresh başarısız - SKU: {SKU}", sku);
                    return null;
                }
            }

            // Boş array kontrolü (retry sonrası tekrar kontrol et)
            if (result.ValueKind == JsonValueKind.Array && result.GetArrayLength() == 0)
            {
                _logger.LogInformation("ℹ️ Stok kartı bulunamadı (boş liste): {SKU}", sku);
                return null;
            }
            
            // 🔍 DEBUG: Response tipini logla
            _logger.LogDebug("📋 Response tipi: {Kind}, SKU: {SKU}", result.ValueKind, sku);

            // Response'dan array'i çıkar - doğrudan array veya object içinde olabilir
            JsonElement arrayToProcess = default;
            
            if (result.ValueKind == JsonValueKind.Array && result.GetArrayLength() > 0)
            {
                _logger.LogDebug("📋 Response doğrudan Array olarak geldi, {Count} kayıt", result.GetArrayLength());
                arrayToProcess = result;
            }
            else if (result.ValueKind == JsonValueKind.Object)
            {
                // 🔍 DEBUG: Response yapısını logla
                var propNames = new List<string>();
                foreach (var prop in result.EnumerateObject())
                {
                    propNames.Add($"{prop.Name}({prop.Value.ValueKind})");
                }
                _logger.LogDebug("📋 Response Object yapısı - Property'ler: {Props}", string.Join(", ", propNames));
                
                // Check for array in various property names (Luca API farklı isimler dönebiliyor)
                foreach (var key in new[] { "list", "stkSkart", "data", "items" })
                {
                    if (result.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.Array)
                    {
                        arrayToProcess = prop;
                        _logger.LogDebug("📋 Array bulundu: '{Key}' property'sinde {Count} kayıt", key, prop.GetArrayLength());
                        break;
                    }
                }
                
                // 🔍 DEBUG: Eğer hiçbir array bulunamadıysa logla
                if (arrayToProcess.ValueKind != JsonValueKind.Array)
                {
                    _logger.LogWarning("⚠️ Response'da array bulunamadı! SKU: {SKU}, Response preview: {Preview}", 
                        sku, result.GetRawText().Length > 500 ? result.GetRawText().Substring(0, 500) : result.GetRawText());
                }
            }
            
            // Array'i işle
            if (arrayToProcess.ValueKind == JsonValueKind.Array)
            {
                if (arrayToProcess.GetArrayLength() == 0)
                {
                    // 🔥 BOŞ LİSTE ama bu gerçekten "yok" mu yoksa API hatası mı?
                    // Cache boşsa bu şüpheli bir durum (session/branch problemi olabilir)
                    if (cacheWasEmpty)
                    {
                        _logger.LogWarning("⚠️ [SUSPICIOUS] Liste boş ANCAK cache de boştu - Session/Branch problemi olabilir!");
                        _logger.LogWarning("   SKU: {SKU} - Bu gerçekten 'yok' mu yoksa API başarısız mı? DIKKATLI DAVRAN!", sku);
                    }
                    else
                    {
                        _logger.LogInformation("ℹ️ Stok kartı bulunamadı (list boş): {SKU}", sku);
                    }
                    return null;
                }

                // 🎯 AKILLI EŞLEŞME: Öncelik sırası
                // 1. TAM EŞLEŞME (SKU = "81.06301-8211")
                // 2. VERSİYONLU EŞLEŞME (SKU-V2, SKU-V3, ..., SKU-V99)
                // 3. AUTO- PREFIX (AUTO-6d876996)
                
                long? exactMatchId = null;
                long? versionedMatchId = null;
                long? autoMatchId = null;
                string? exactMatchCode = null;
                string? versionedMatchCode = null;
                string? autoMatchCode = null;
                
                var candidates = new List<(string code, long id, string type)>();

                foreach (var item in arrayToProcess.EnumerateArray())
                {
                    // 🔍 DEBUG: İlk item'ın yapısını logla
                    if (candidates.Count == 0)
                    {
                        var itemProps = new List<string>();
                        foreach (var prop in item.EnumerateObject())
                        {
                            itemProps.Add($"{prop.Name}={prop.Value.ToString().Substring(0, Math.Min(50, prop.Value.ToString().Length))}");
                        }
                        _logger.LogDebug("🔍 İlk item yapısı: {Props}", string.Join(", ", itemProps.Take(10)));
                    }
                    
                    // KartKodu eşleşmesi kontrol et - TÜM OLASI FIELD İSİMLERİNİ KONTROL ET
                    // Luca API farklı endpoint'lerde farklı field isimleri dönebiliyor:
                    // kod, kartKodu, code, skartKod, stokKartKodu, stokKodu
                    var kartKodu = TryGetProperty(item, "kod", "kartKodu", "code", "skartKod", "stokKartKodu", "stokKodu");
                    
                    // 🔍 DEBUG: Bulunan kartKodu'yu logla
                    if (candidates.Count < 3)
                    {
                        _logger.LogDebug("🔍 Item kartKodu: '{KartKodu}', Aranan SKU: '{SKU}'", kartKodu ?? "(null)", sku);
                    }

                    if (string.IsNullOrWhiteSpace(kartKodu))
                        continue;

                    var trimmedKod = kartKodu.Trim();
                    
                    // SkartId al
                    if (!item.TryGetProperty("skartId", out var skartIdProp))
                        continue;
                        
                    long? skartId = null;
                    if (skartIdProp.ValueKind == JsonValueKind.Number)
                        skartId = skartIdProp.GetInt64();
                    else if (skartIdProp.ValueKind == JsonValueKind.String && long.TryParse(skartIdProp.GetString(), out var parsed))
                        skartId = parsed;
                        
                    if (!skartId.HasValue || skartId.Value == 0)
                        continue;

                    // 1️⃣ TAM EŞLEŞME kontrolü
                    if (trimmedKod.Equals(sku.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        exactMatchId = skartId.Value;
                        exactMatchCode = trimmedKod;
                        candidates.Add((trimmedKod, skartId.Value, "EXACT"));
                        _logger.LogDebug("  ✅ Tam eşleşme bulundu: {Code} → {Id}", trimmedKod, skartId.Value);
                    }
                    // 2️⃣ VERSİYONLU EŞLEŞME (-V2, -V3, -V99, timestamp sonekleri)
                    else if (trimmedKod.StartsWith(sku.Trim(), StringComparison.OrdinalIgnoreCase) &&
                             (System.Text.RegularExpressions.Regex.IsMatch(trimmedKod, @"-V\d+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase) ||
                              System.Text.RegularExpressions.Regex.IsMatch(trimmedKod, @"-\d{12}$"))) // Timestamp soneki (örn: -202512052307)
                    {
                        versionedMatchId ??= skartId.Value; // İlk bulduğunu al
                        versionedMatchCode ??= trimmedKod;
                        candidates.Add((trimmedKod, skartId.Value, "VERSIONED"));
                        _logger.LogDebug("  📦 Versiyonlu eşleşme bulundu: {Code} → {Id}", trimmedKod, skartId.Value);
                    }
                    // 3️⃣ AUTO- PREFIX (AUTO-6d876996 gibi)
                    else if (trimmedKod.StartsWith("AUTO-", StringComparison.OrdinalIgnoreCase))
                    {
                        // Stok Adı (tanim/kartAdi) alanında orijinal SKU olabilir mi kontrol et
                        var kartAdi = item.TryGetProperty("tanim", out var tanimProp) ? tanimProp.GetString() :
                                      item.TryGetProperty("kartAdi", out var kartAdiProp) ? kartAdiProp.GetString() : null;
                        
                        if (!string.IsNullOrWhiteSpace(kartAdi) && 
                            kartAdi.Trim().Contains(sku.Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            autoMatchId ??= skartId.Value;
                            autoMatchCode ??= trimmedKod;
                            candidates.Add((trimmedKod, skartId.Value, "AUTO"));
                            _logger.LogDebug("  🔧 AUTO- prefix eşleşme bulundu: {Code} (Stok Adı: {Name}) → {Id}", 
                                trimmedKod, kartAdi, skartId.Value);
                        }
                    }
                }
                
                // 🎯 SONUÇ: Öncelik sırasına göre dön
                if (exactMatchId.HasValue)
                {
                    _logger.LogInformation("✅ [EXACT MATCH] Stok kartı bulundu: {SKU} → {Code} (skartId: {Id})", 
                        sku, exactMatchCode, exactMatchId.Value);
                    
                    await _stockCardCacheLock.WaitAsync();
                    try
                    {
                        _stockCardCache[sku] = exactMatchId;
                    }
                    finally
                    {
                        _stockCardCacheLock.Release();
                    }
                    
                    return exactMatchId;
                }
                else if (versionedMatchId.HasValue)
                {
                    _logger.LogWarning("⚠️ [VERSIONED MATCH] SKU: {SKU} Luca'da versiyonlanmış olarak bulundu: {Code} (skartId: {Id})", 
                        sku, versionedMatchCode, versionedMatchId.Value);
                    _logger.LogWarning("   ⚠️ DİKKAT: Bu ürün zaten var! Yeni kart açılmamalı.");
                    
                    if (candidates.Count > 1)
                    {
                        _logger.LogWarning("   📋 Bulunan {Count} varyasyon:", candidates.Count);
                        foreach (var (code, id, type) in candidates)
                        {
                            _logger.LogWarning("      - {Code} ({Type}) → ID: {Id}", code, type, id);
                        }
                    }
                    
                    await _stockCardCacheLock.WaitAsync();
                    try
                    {
                        _stockCardCache[sku] = versionedMatchId;
                    }
                    finally
                    {
                        _stockCardCacheLock.Release();
                    }
                    
                    return versionedMatchId;
                }
                else if (autoMatchId.HasValue)
                {
                    _logger.LogWarning("⚠️ [AUTO-PREFIX MATCH] SKU: {SKU} Luca'da AUTO- prefix ile bulundu: {Code} (skartId: {Id})", 
                        sku, autoMatchCode, autoMatchId.Value);
                    _logger.LogWarning("   ⚠️ DİKKAT: Bu ürün zaten var! Yeni kart açılmamalı.");
                    
                    await _stockCardCacheLock.WaitAsync();
                    try
                    {
                        _stockCardCache[sku] = autoMatchId;
                    }
                    finally
                    {
                        _stockCardCacheLock.Release();
                    }
                    
                    return autoMatchId;
                }
            }

            _logger.LogInformation("ℹ️ Stok kartı bulunamadı (FUZZY SEARCH sonucu): {SKU}", sku);
            
            // ✅ Bulunamayan kartları da cache'e ekle (tekrar sorgulamayı önle)
            await _stockCardCacheLock.WaitAsync();
            try
            {
                _stockCardCache[sku] = null;
            }
            finally
            {
                _stockCardCacheLock.Release();
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ FindStockCardBySkuAsync error for '{SKU}': {Message}", sku, ex.Message);
            return null; // ✅ HATA DURUMUNDA NULL DÖN
        }
    }

    /// <summary>
    /// Luca'daki stok kartı detaylarını getir (karşılaştırma için)
    /// </summary>
    public async Task<LucaStockCardDetails?> GetStockCardDetailsBySkuAsync(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            return null;

        try
        {
            _logger.LogDebug("🔍 Luca'da stok kartı detayları getiriliyor: {SKU}", sku);
            
            await EnsureAuthenticatedAsync();
            await EnsureBranchSelectedAsync();

            var request = new LucaListStockCardsRequest
            {
                StkSkart = new LucaStockCardCodeFilter
                {
                    KodBas = sku,
                    KodBit = sku,
                    KodOp = "between"
                }
            };

            var result = await ListStockCardsAsync(request);

            // 🔥 CRITICAL: Raw JSON'u logla (debugging için)
            var rawJson = result.GetRawText();
            _logger.LogInformation("📊 LUCA RAW RESPONSE for SKU '{SKU}': {RawJsonPreview}", 
                sku, rawJson.Length > 500 ? rawJson.Substring(0, 500) + "..." : rawJson);

            // 🔥 BOŞ/GEÇERSİZ RESPONSE KONTROLÜ
            if (result.ValueKind == JsonValueKind.Undefined || result.ValueKind == JsonValueKind.Null)
            {
                _logger.LogWarning("⚠️ GetStockCardDetailsBySkuAsync: Geçersiz response (Undefined/Null) - SKU: {SKU}", sku);
                return null;
            }

            if (result.ValueKind == JsonValueKind.Object &&
                result.TryGetProperty("list", out var listProp) && 
                listProp.ValueKind == JsonValueKind.Array)
            {
                if (listProp.GetArrayLength() == 0)
                {
                    _logger.LogInformation("ℹ️ Stok kartı detayları bulunamadı (list boş): {SKU}", sku);
                    return null;
                }

                foreach (var item in listProp.EnumerateArray())
                {
                    // Kod eşleşmesi kontrol et
                    var kartKodu = item.TryGetProperty("kod", out var kodProp) ? kodProp.GetString() : 
                                   item.TryGetProperty("kartKodu", out var kartKoduProp) ? kartKoduProp.GetString() : null;
                    
                    if (!string.Equals(kartKodu?.Trim(), sku.Trim(), StringComparison.OrdinalIgnoreCase))
                        continue;

                    // 🔥 CRITICAL: Available fields'ı logla
                    var availableFields = string.Join(", ", item.EnumerateObject().Select(p => p.Name));
                    _logger.LogInformation("📦 Available fields for SKU '{SKU}': {Fields}", sku, availableFields);

                    // ✅ Çoklu field kontrolü - hangi field dolu ise onu kullan
                    var kartAdi = item.TryGetProperty("KartAdi", out var kartAdiProp) ? kartAdiProp.GetString() :
                                  item.TryGetProperty("kartAdi", out var kartAdi2Prop) ? kartAdi2Prop.GetString() :
                                  item.TryGetProperty("tanim", out var tanimProp) ? tanimProp.GetString() :
                                  item.TryGetProperty("stokKartAdi", out var stokAdiProp) ? stokAdiProp.GetString() :
                                  item.TryGetProperty("adi", out var adiProp) ? adiProp.GetString() :
                                  item.TryGetProperty("name", out var nameProp) ? nameProp.GetString() :
                                  sku; // Son çare: SKU'yu kullan

                    _logger.LogInformation("✅ KartAdi extracted: '{KartAdi}' for SKU: {SKU}", kartAdi, sku);

                    var details = new LucaStockCardDetails
                    {
                        SkartId = item.TryGetProperty("skartId", out var idProp) && idProp.ValueKind == JsonValueKind.Number 
                            ? idProp.GetInt64() : 0,
                        KartKodu = kartKodu ?? sku,
                        KartAdi = kartAdi, // Artık asla null olmaz
                        KartTuru = item.TryGetProperty("kartTuru", out var turuProp) && turuProp.ValueKind == JsonValueKind.Number 
                            ? turuProp.GetInt32() : 1,
                        OlcumBirimiId = item.TryGetProperty("olcumBirimiId", out var obProp) && obProp.ValueKind == JsonValueKind.Number 
                            ? obProp.GetInt64() : 1,
                        KartAlisKdvOran = item.TryGetProperty("kartAlisKdvOran", out var akdvProp) && akdvProp.ValueKind == JsonValueKind.Number 
                            ? akdvProp.GetDouble() : 0,
                        KartSatisKdvOran = item.TryGetProperty("kartSatisKdvOran", out var skdvProp) && skdvProp.ValueKind == JsonValueKind.Number 
                            ? skdvProp.GetDouble() : 0,
                        KartTipi = item.TryGetProperty("kartTipi", out var tipiProp) && tipiProp.ValueKind == JsonValueKind.Number 
                            ? tipiProp.GetInt32() : 1,
                        KategoriAgacKod = item.TryGetProperty("kategoriAgacKod", out var katProp) ? katProp.GetString() : null,
                        Barkod = item.TryGetProperty("barkod", out var barkodProp) ? barkodProp.GetString() : null,
                        // Fiyat alanları - karşılaştırma için
                        SatisFiyat = TryGetDoubleProperty(item, "perakendeSatisBirimFiyat", "satisFiyat", "salesPrice", "fiyat"),
                        AlisFiyat = TryGetDoubleProperty(item, "perakendeAlisBirimFiyat", "alisFiyat", "purchasePrice")
                    };

                    _logger.LogInformation("✅ Stok kartı detayları bulundu: {SKU} → KartAdi: {KartAdi}, SkartId: {SkartId}", 
                        sku, details.KartAdi ?? "(boş)", details.SkartId);
                    return details;
                }
            }

            _logger.LogInformation("ℹ️ Stok kartı detayları bulunamadı: {SKU}", sku);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ GetStockCardDetailsBySkuAsync error for '{SKU}': {Message}", sku, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Yeni gelen stok kartını Luca'daki mevcut kartla karşılaştır
    /// Farklılık varsa true döner (yeni kart açılmalı)
    /// NOT: Luca API'si bazı alanları boş döndürüyor, bu yüzden sadece güvenilir alanları karşılaştırıyoruz
    /// </summary>
    public bool HasStockCardChanges(LucaCreateStokKartiRequest newCard, LucaStockCardDetails? existingCard)
    {
        // CRITICAL: NULL kontrolü - Luca'dan veri gelmezse yeni kayıt olarak işle
        if (existingCard == null)
        {
            _logger.LogWarning("Stok kartı bulunamadı: {KartKodu}, yeni kayıt olarak işlenecek", newCard.KartKodu);
            return true; // Yeni kayıt olarak oluştur
        }

        // Parse hatasını yakala - KartKodu boşsa veri güvenilir değil
        if (string.IsNullOrEmpty(existingCard.KartKodu))
        {
            _logger.LogError("❌ Luca'dan dönen data eksik (KartKodu boş): {KartKodu}", newCard.KartKodu);
            _logger.LogDebug("Existing data: KartKodu={ExistingKartKodu}, KartAdi={KartAdi}, SkartId={SkartId}", 
                existingCard.KartKodu ?? "(null)", existingCard.KartAdi ?? "(null)", existingCard.SkartId);
            return false; // Atlama yap, hata logla - güvenli taraf
        }

        // 🔥 KRİTİK: Luca'dan gelen data güvenilir mi kontrol et
        // KartAdi boşsa fallback kullan
        if (string.IsNullOrWhiteSpace(existingCard.KartAdi))
        {
            _logger.LogWarning("⚠️ Luca'dan KartAdi boş geldi, SKU fallback kullanılıyor: {KartKodu}", newCard.KartKodu);
            existingCard.KartAdi = existingCard.KartKodu ?? newCard.KartKodu; // SKU'yu kullan
            _logger.LogDebug("Fallback applied: KartAdi set to '{FallbackKartAdi}' for SKU: {SKU}", 
                existingCard.KartAdi, existingCard.KartKodu);
        }

        // 🔥 BOŞ OBJECT KONTROLÜ - HTML parse hatası sonucu boş object oluşmuş olabilir
        // Tüm önemli alanlar boşsa bu güvenilir değil
        if (existingCard.SkartId == 0 &&
            !existingCard.SatisFiyat.HasValue &&
            string.IsNullOrWhiteSpace(existingCard.KategoriAgacKod))
        {
            _logger.LogError("❌ Luca'dan dönen data boş object (HTML parse hatası olabilir): {KartKodu}. Güvenli taraf: değişiklik yok sayılıyor.", newCard.KartKodu);
            return false; // Güvenli taraf: Atlama yap
        }

        try
        {
            // Sadece güvenilir alanları karşılaştır
            // Luca API'si kartAdi, kdvOran gibi alanları bazen boş/0 döndürüyor
            bool hasChanges = false;
            var changeReasons = new List<string>();

            // 🔥 BAĞIMSIZ KONTROL MANTIĞI (Independent Check Logic)
            // Her değişiklik ayrı ayrı hesaplanır ve sonra OR (||) ile birleştirilir
            // Böylece "Fiyat 0" olsa bile İsim değişirse yeni versiyon oluşur!

            // 1️⃣ İSİM KONTROLÜ - SADECE LOGLARken İÇİN (VERSİYONLAMAYA ETKİ ETMEZ!)
            // 🔥 KRİTİK: İsim değişikliği ASLA yeni versiyon oluşturmaz
            // Sebep: Katana DB'de isim corruption var (SKU yazılmış), Luca'daki orijinal isim korunmalı
            if (!string.IsNullOrWhiteSpace(newCard.KartAdi) && !string.IsNullOrWhiteSpace(existingCard.KartAdi))
            {
                // 🔥 ULTRA TOLERANSLI KARŞILAŞTIRMA: Her türlü encoding sorununu tolere et
                var normalizedNew = NormalizeForUltraLooseComparison(newCard.KartAdi);
                var normalizedExisting = NormalizeForUltraLooseComparison(existingCard.KartAdi);
                
                // Karşılaştırma: Normalize edilmiş versiyonlar eşit mi?
                var isNameEqual = normalizedNew.Equals(normalizedExisting, StringComparison.OrdinalIgnoreCase);
                
                // 🔥 EK KONTROL: Eğer normalize hala farklıysa, "benzerlik oranı" kontrolü yap
                // Örnek: "Ø35*1,5 PIPE" vs "O35*1,5 PIPE" vs "??35*1,5 PIPE" → %90+ benzer
                if (!isNameEqual)
                {
                    var similarity = CalculateStringSimilarity(normalizedNew, normalizedExisting);
                    if (similarity >= 0.85) // %85 ve üzeri benzer ise "aynı" say
                    {
                        isNameEqual = true;
                        _logger.LogDebug("⚠️ İsimler normalize sonrası farklı AMA %{Similarity:N0} benzer, aynı kabul ediliyor: '{Name1}' ≈ '{Name2}'",
                            similarity * 100, normalizedNew, normalizedExisting);
                    }
                }
                
                // 🚫 İSİM DEĞİŞİKLİĞİ KONTROLÜ DEVRE DIŞI!
                // Sebep: Katana DB'de bazı ürünlerin ismi yerine SKU yazılmış (örn: "81.06301-8212")
                // Bu durum Luca'daki gerçek isimle ("COOLING WATER PIPE") çakışıyor ve gereksiz -V2 üretiyor.
                // Çözüm: İsim farkını LOG'la ama değişiklik sayma (isNameChanged = false).
                var actualNameDifference = !isNameEqual;
                
                if (actualNameDifference)
                {
                    // Bilgilendirici log - ama "değişiklik" olarak işaretlemiyoruz
                    _logger.LogInformation("ℹ️ İsim farkı algılandı ama SYNC POLICY gereği GÖRMEZDEN GELİNİYOR:");
                    _logger.LogInformation("   Luca: '{LucaName}'", existingCard.KartAdi);
                    _logger.LogInformation("   Katana: '{KatanaName}'", newCard.KartAdi);
                    _logger.LogInformation("   Sebep: Katana DB'de isim corruption var, Luca'daki orijinal ismi koruyoruz");
                    
                    // changeReasons'a EKLEME (isim farkını logla ama "değişiklik" sayma)
                    // changeReasons.Add($"📝 İsim farkı var (ignored): Luca='{existingCard.KartAdi}' vs Katana='{newCard.KartAdi}'");
                }
                else
                {
                    _logger.LogDebug("✅ İsim AYNI kabul edildi (tolerance ile): '{Name1}' ≈ '{Name2}'", 
                        normalizedNew, normalizedExisting);
                }
            }

            // 2️⃣ FİYAT KONTROLÜ - Luca fiyatı 0 ise ATLA!
            // 🔥 KRİTİK FİX: Sonsuz versiyon döngüsünü önlemek için
            bool isPriceChanged = false;
            var existingPrice = existingCard.SatisFiyat ?? 0;
            
            if (existingPrice == 0 || existingPrice < 0.01)
            {
                _logger.LogInformation("⚠️ Luca fiyatı 0 olduğu için fiyat kontrolü atlandı: {KartKodu} (Luca: {LucaPrice}, Katana: {KatanaPrice})", 
                    newCard.KartKodu, existingPrice, newCard.PerakendeSatisBirimFiyat);
                isPriceChanged = false; // Fiyat kontrolü devre dışı
            }
            else if (newCard.PerakendeSatisBirimFiyat > 0)
            {
                if (Math.Abs(newCard.PerakendeSatisBirimFiyat - existingPrice) > 0.01)
                {
                    isPriceChanged = true;
                    changeReasons.Add($"💰 Fiyat DEĞİŞTİ: {existingPrice:N2} TL -> {newCard.PerakendeSatisBirimFiyat:N2} TL");
                }
            }

            // 3️⃣ KDV ORANI KONTROLÜ (Kritik Alan!)
            bool isVatChanged = false;
            // Sadece HER İKİSİ de geçerli değere sahipse karşılaştır (0 veya çok küçük değilse)
            if (newCard.KartAlisKdvOran > 0.01 && existingCard.KartAlisKdvOran > 0.01)
            {
                if (Math.Abs(newCard.KartAlisKdvOran - existingCard.KartAlisKdvOran) > 0.01)
                {
                    isVatChanged = true;
                    changeReasons.Add($"📊 KDV Oranı DEĞİŞTİ: {existingCard.KartAlisKdvOran:N2} -> {newCard.KartAlisKdvOran:N2}");
                }
            }
            else
            {
                _logger.LogInformation("⚠️ KDV kontrolü atlandı (0/geçersiz değer): {KartKodu} (Luca: {LucaKdv}, Katana: {KatanaKdv})",
                    newCard.KartKodu, existingCard.KartAlisKdvOran, newCard.KartAlisKdvOran);
            }

            // 4️⃣ ÖLÇÜ BİRİMİ KONTROLÜ (Kritik Alan!)
            bool isUnitChanged = false;
            // Sadece HER İKİSİ de geçerli ve default değil ise karşılaştır (ID > 1)
            if (newCard.OlcumBirimiId > 1 && existingCard.OlcumBirimiId > 1)
            {
                if (newCard.OlcumBirimiId != existingCard.OlcumBirimiId)
                {
                    isUnitChanged = true;
                    changeReasons.Add($"📏 Ölçü Birimi DEĞİŞTİ: ID {existingCard.OlcumBirimiId} -> {newCard.OlcumBirimiId}");
                }
            }

            // 5️⃣ KATEGORİ KONTROLÜ (İsteğe bağlı)
            bool isCategoryChanged = false;
            if (!string.IsNullOrWhiteSpace(newCard.KategoriAgacKod) && !string.IsNullOrWhiteSpace(existingCard.KategoriAgacKod))
            {
                if (!string.Equals(newCard.KategoriAgacKod.Trim(), existingCard.KategoriAgacKod.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    isCategoryChanged = true;
                    changeReasons.Add($"📂 Kategori DEĞİŞTİ: '{existingCard.KategoriAgacKod}' -> '{newCard.KategoriAgacKod}'");
                }
            }

            // 🎯 SONUÇ: KRİTİK ALANLARDAN HERHANGİ BİRİ DEĞİŞTİYSE TRUE (OR mantığı)
            // İsim değişikliği ASLA versiyonlamaya sebep olmaz!
            hasChanges = isPriceChanged || isVatChanged || isUnitChanged || isCategoryChanged;

            if (hasChanges)
            {
                _logger.LogWarning("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                _logger.LogWarning("🔄 ÜRÜN DEĞİŞİKLİĞİ TESPİT EDİLDİ: {KartKodu}", newCard.KartKodu);
                _logger.LogWarning("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                
                foreach (var reason in changeReasons)
                {
                    _logger.LogWarning("   📝 {Reason}", reason);
                }
                
                _logger.LogWarning("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                _logger.LogWarning("⚡ AKSIYON: Luca API güncelleme desteklemiyor");
                _logger.LogWarning("   → Yeni versiyonlu SKU ile stok kartı oluşturulacak");
                _logger.LogWarning("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            }
            else
            {
                _logger.LogInformation("✅ Stok kartı '{KartKodu}' - Değişiklik yok, atlanıyor", newCard.KartKodu);
            }

            return hasChanges;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HasStockCardChanges hatası: {KartKodu}", newCard.KartKodu);
            return false; // Güvenli taraf: Değişiklik yok say
        }
    }

    /// <summary>
    /// Stok kartı için versiyon numarası oluştur (ör: SKU-V2, SKU-V3)
    /// </summary>
    public async Task<string> GenerateVersionedSkuAsync(string baseSku)
    {
        _logger.LogInformation("🔢 Versiyonlu SKU oluşturuluyor: {BaseSku}", baseSku);
        
        // Önce base SKU ile başlayan tüm kartları bul
        var version = 2;
        var maxVersion = 10; // Makul bir üst limit

        while (version <= maxVersion)
        {
            var versionedSku = $"{baseSku}-V{version}";
            _logger.LogDebug("   Kontrol ediliyor: {VersionedSku}", versionedSku);
            
            var exists = await FindStockCardBySkuAsync(versionedSku);
            
            if (!exists.HasValue)
            {
                _logger.LogInformation("✅ Uygun versiyon bulundu: {VersionedSku}", versionedSku);
                return versionedSku;
            }
            
            _logger.LogDebug("   ❌ {VersionedSku} zaten mevcut, sonraki versiyon deneniyor...", versionedSku);
            version++;
        }

        // 🔥 FALLBACK FİX: Timestamp çok uzun, bunun yerine V99 kullan
        // Maksimum versiyona ulaşıldıysa, güvenli fallback: baseSku-V99
        var fallbackSku = $"{baseSku}-V99";
        _logger.LogError("❌ Maksimum versiyon sayısına ulaşıldı (V{MaxVersion})! Fallback kullanılıyor: {Sku}", maxVersion, fallbackSku);
        _logger.LogError("⚠️ DİKKAT: Bu ürün için çok fazla versiyon var, veritabanını temizlemeyi düşünün!");
        return fallbackSku;
    }

    /// <summary>
    /// UPSERT: If stock card exists in Luca, mark as duplicate (API doesn't support update).
    /// If not exists, create new card.
    /// </summary>
    public async Task<SyncResultDto> UpsertStockCardAsync(LucaCreateStokKartiRequest stockCard)
    {
        var result = new SyncResultDto
        {
            SyncType = "STOCK_CARD_UPSERT",
            ProcessedRecords = 1,
            SyncTime = DateTime.UtcNow
        };

        try
        {
            var sku = stockCard.KartKodu;
            
            // First, check if the card already exists
            var existingSkartId = await FindStockCardBySkuAsync(sku);
            
            if (existingSkartId.HasValue)
            {
                // Card already exists in Luca
                // NOTE: Luca Koza API does NOT support stock card updates!
                // The card already exists, so we mark it as "duplicate" (already synced)
                result.DuplicateRecords = 1;
                result.IsSuccess = true;
                result.Message = $"Stok kartı '{sku}' zaten Luca'da mevcut (skartId: {existingSkartId.Value}). Luca API stok kartı güncellemesini desteklemiyor.";
                _logger.LogInformation("Stock card {SKU} already exists in Luca with skartId {SkartId}. Luca API does not support updates.", sku, existingSkartId.Value);
                return result;
            }

            // Card doesn't exist, create new
            var sendResult = await SendStockCardsAsync(new List<LucaCreateStokKartiRequest> { stockCard });
            
            result.IsSuccess = sendResult.IsSuccess || sendResult.DuplicateRecords > 0;
            result.SuccessfulRecords = sendResult.SuccessfulRecords;
            result.FailedRecords = sendResult.FailedRecords;
            result.DuplicateRecords = sendResult.DuplicateRecords;
            result.Errors = sendResult.Errors;
            result.Message = sendResult.IsSuccess 
                ? $"Stok kartı '{sku}' Luca'ya başarıyla eklendi."
                : $"Stok kartı '{sku}' Luca'ya eklenemedi: {string.Join(", ", sendResult.Errors)}";

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting stock card {SKU} to Luca", stockCard.KartKodu);
            result.IsSuccess = false;
            result.FailedRecords = 1;
            result.Errors.Add($"{stockCard.KartKodu}: {ex.Message}");
            result.Message = $"Stok kartı işlenirken hata: {ex.Message}";
            return result;
        }
    }

    #region Cari Kart (Customer) Methods

    /// <summary>
    /// Luca'da cari kart arar (kartKodu bazlı)
    /// </summary>
    public async Task<long?> FindCariCardByCodeAsync(string kartKodu)
    {
        if (string.IsNullOrWhiteSpace(kartKodu))
            return null;

        try
        {
            await EnsureAuthenticatedAsync();
            await EnsureBranchSelectedAsync();

            // ListeleFinMusteri.do ile ara
            var request = new LucaListCariKartRequest
            {
                FinMusteri = new LucaCariKartListFilter
                {
                    GnlFinansalNesne = new LucaCariKartFilter
                    {
                        KodBas = kartKodu,
                        KodBit = kartKodu,
                        KodOp = "between"
                    }
                }
            };

            var result = await ListCustomersAsync(new LucaListCustomersRequest());
            
            if (result.ValueKind == JsonValueKind.Object)
            {
                if (result.TryGetProperty("list", out var listProp) && listProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in listProp.EnumerateArray())
                    {
                        // kartKodu kontrolü
                        if (item.TryGetProperty("kod", out var kodProp) && 
                            kodProp.ValueKind == JsonValueKind.String &&
                            string.Equals(kodProp.GetString(), kartKodu, StringComparison.OrdinalIgnoreCase))
                        {
                            // finansalNesneId al
                            if (item.TryGetProperty("finansalNesneId", out var idProp))
                            {
                                if (idProp.ValueKind == JsonValueKind.Number)
                                    return idProp.GetInt64();
                                if (idProp.ValueKind == JsonValueKind.String && long.TryParse(idProp.GetString(), out var parsed))
                                    return parsed;
                            }
                        }
                    }
                }
            }

            _logger.LogInformation("Cari kart with code {KartKodu} not found in Luca", kartKodu);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for cari kart by code {KartKodu} in Luca", kartKodu);
            return null;
        }
    }

    /// <summary>
    /// Luca'da cari kart günceller
    /// NOT: Luca Koza API'de cari kart güncelleme endpoint'i sınırlı olabilir
    /// </summary>
    public async Task<SyncResultDto> UpdateCariCardAsync(LucaUpdateCustomerFullRequest request)
    {
        var result = new SyncResultDto
        {
            SyncType = "CARI_CARD_UPDATE",
            ProcessedRecords = 1,
            SyncTime = DateTime.UtcNow
        };

        try
        {
            await EnsureAuthenticatedAsync();
            await EnsureBranchSelectedAsync();

            // NOT: Luca Koza API'de GuncelleFinMusteriWS.do yoksa bu çalışmaz
            // Şu an için sadece log bırakıyoruz
            _logger.LogWarning("Cari kart güncelleme henüz desteklenmiyor. KartKod: {KartKod}", request.KartKod);
            
            result.IsSuccess = false;
            result.Message = "Luca API cari kart güncelleme desteklemiyor. Manuel güncelleme gerekli.";
            result.Errors.Add($"{request.KartKod}: API does not support customer updates");
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cari kart {KartKod} in Luca", request.KartKod);
            result.IsSuccess = false;
            result.FailedRecords = 1;
            result.Errors.Add($"{request.KartKod}: {ex.Message}");
            return result;
        }
    }

    /// <summary>
    /// UPSERT: Cari kart varsa duplicate olarak işaretle (güncelleme yok), yoksa oluştur
    /// </summary>
    public async Task<SyncResultDto> UpsertCariCardAsync(Customer customer)
    {
        var result = new SyncResultDto
        {
            SyncType = "CARI_CARD_UPSERT",
            ProcessedRecords = 1,
            SyncTime = DateTime.UtcNow
        };

        try
        {
            // ÖNEMLİ: Branch seçimi zorunlu (1003 hatası önleme)
            await EnsureAuthenticatedAsync();
            await EnsureBranchSelectedAsync();

            if (_settings.UsePostmanCustomerFormat)
            {
                var kozaRequest = BuildKozaMusteriEkleRequest(customer);
                var kozaResult = await CreateMusteriCariAsync(kozaRequest);

                if (!kozaResult.Success)
                {
                    result.IsSuccess = false;
                    result.FailedRecords = 1;
                    result.Errors.Add($"{customer.Id}: {kozaResult.Message}");
                    return result;
                }

                result.IsSuccess = true;
                result.SuccessfulRecords = 1;
                result.Message = $"Cari kart '{kozaRequest.KartKod}' Luca'ya gönderildi (Postman format).";
                return result;
            }
            
            var kartKodu = customer.LucaCode ?? customer.GenerateLucaCode();
            
            // Önce Luca'da ara
            var existingId = await FindCariCardByCodeAsync(kartKodu);
            
            if (existingId.HasValue)
            {
                // Zaten var - Luca API güncelleme desteklemediği için sadece log
                result.DuplicateRecords = 1;
                result.IsSuccess = true;
                result.Message = $"Cari kart '{kartKodu}' zaten Luca'da mevcut (finansalNesneId: {existingId.Value}). Luca API güncelleme desteklemiyor.";
                _logger.LogInformation("Cari kart {KartKodu} already exists in Luca with finansalNesneId {Id}. API does not support updates.", 
                    kartKodu, existingId.Value);
                return result;
            }

            // Yeni kart oluştur
            var createRequest = MappingHelper.MapToLucaCustomerCreate(customer);
            var createResult = await CreateCustomerAsync(createRequest);
            
            // Sonucu kontrol et
            if (createResult.ValueKind == JsonValueKind.Object)
            {
                if (createResult.TryGetProperty("error", out var errorProp) && errorProp.ValueKind == JsonValueKind.True)
                {
                    var msg = createResult.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : "Unknown error";
                    
                    // Duplicate kontrolü
                    if (msg?.Contains("daha önce kullanılmış", StringComparison.OrdinalIgnoreCase) == true ||
                        msg?.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        result.DuplicateRecords = 1;
                        result.IsSuccess = true;
                        result.Message = $"Cari kart '{kartKodu}' zaten Luca'da mevcut.";
                        return result;
                    }
                    
                    result.IsSuccess = false;
                    result.FailedRecords = 1;
                    result.Errors.Add($"{kartKodu}: {msg}");
                    return result;
                }
                
                // Başarılı - finansalNesneId al
                if (createResult.TryGetProperty("finansalNesneId", out var idProp))
                {
                    long newId = 0;
                    if (idProp.ValueKind == JsonValueKind.Number)
                        newId = idProp.GetInt64();
                    else if (idProp.ValueKind == JsonValueKind.String)
                        long.TryParse(idProp.GetString(), out newId);
                    
                    result.IsSuccess = true;
                    result.SuccessfulRecords = 1;
                    result.Message = $"Cari kart '{kartKodu}' Luca'ya başarıyla eklendi (finansalNesneId: {newId}).";
                    result.Details.Add($"finansalNesneId={newId}");
                    return result;
                }
            }
            
            result.IsSuccess = true;
            result.SuccessfulRecords = 1;
            result.Message = $"Cari kart '{kartKodu}' Luca'ya gönderildi.";
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting cari kart for customer {CustomerId} to Luca", customer.Id);
            result.IsSuccess = false;
            result.FailedRecords = 1;
            result.Errors.Add($"Customer {customer.Id}: {ex.Message}");
            result.Message = $"Cari kart işlenirken hata: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// Müşteri adresini Luca'ya gönderir
    /// </summary>
    public async Task<SyncResultDto> SendCustomerAddressAsync(long finansalNesneId, string address, string? city, string? district, bool isDefault = true)
    {
        var result = new SyncResultDto
        {
            SyncType = "CUSTOMER_ADDRESS",
            ProcessedRecords = 1,
            SyncTime = DateTime.UtcNow
        };

        try
        {
            await EnsureAuthenticatedAsync();
            await EnsureBranchSelectedAsync();

            // EkleWSGnlSsAdres.do endpoint'i
            var endpoint = "EkleWSGnlSsAdres.do";
            var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;

            var payload = new
            {
                finansalNesneId = finansalNesneId,
                adresTipId = 1, // 1=Fatura adresi
                ulke = "TURKIYE",
                il = city,
                ilce = district,
                adresSerbest = address,
                varsayilanFlag = isDefault ? 1 : 0
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = content };
            ApplyManualSessionCookie(request);

            var response = await client.SendAsync(request);
            var responseContent = await ReadResponseContentAsync(response);

            _logger.LogInformation("SendCustomerAddress response: {Response}", responseContent);

            if (response.IsSuccessStatusCode)
            {
                result.IsSuccess = true;
                result.SuccessfulRecords = 1;
                result.Message = "Adres başarıyla eklendi.";
            }
            else
            {
                result.IsSuccess = false;
                result.FailedRecords = 1;
                result.Errors.Add($"HTTP {response.StatusCode}: {responseContent}");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending customer address to Luca");
            result.IsSuccess = false;
            result.FailedRecords = 1;
            result.Errors.Add(ex.Message);
            return result;
        }
    }

    #endregion

    #region Turkish Character Normalization Helper

    /// <summary>
    /// Türkçe karakterleri normalize eder ve karşılaştırma için temizler.
    /// ? karakterlerini SİLER - strict karşılaştırma için kullanılır.
    /// Luca API'si Türkçe karakterleri bazen ? olarak döndürüyor.
    /// Örn: BÜKÜMLÜ -> B?K?ML? olarak geliyor, TALAŞ -> TALA? oluyor.
    /// 
    /// Örnek: "TALAŞ BFM-01" ve "TALA? BFM 01" -> "TALASBFM01" (aynı)
    /// </summary>
    private static string NormalizeTurkishCharsForComparison(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // 1. BÜYÜK harfe çevir (Invariant - kültürden bağımsız)
        var result = input.ToUpperInvariant();

        // 2. Türkçe karakterleri ASCII eşdeğerlerine çevir
        result = result
            .Replace("Ü", "U").Replace("Ö", "O")
            .Replace("Ş", "S").Replace("Ç", "C")
            .Replace("Ğ", "G").Replace("İ", "I")
            .Replace("Ø", "O")  // Çap sembolü (diameter symbol)
            .Replace("I", "I"); // Türkçe büyük I, İngilizce I ile aynı

        // 3. ? karakterlerini sil (Luca encoding hatası)
        result = result.Replace("?", "");

        // 4. Tüm boşlukları, tireleri, alt çizgileri ve noktalama işaretlerini sil
        // Sadece harf ve rakamları bırak
        result = new string(result.Where(c => char.IsLetterOrDigit(c)).ToArray());

        // 5. Son trim (gereksiz ama garanti için)
        return result.Trim();
    }

    /// <summary>
    /// Türkçe karakterleri normalize eder ama ? karakterini KORUR (wildcard için).
    /// Wildcard karşılaştırma için kullanılır - ? herhangi bir karakterle eşleşir.
    /// Örnek: "DEM?R TALA?" -> "DEMRTALA?" (? korunur)
    /// </summary>
    private static string NormalizePreservingWildcard(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // 1. BÜYÜK harfe çevir
        var result = input.ToUpperInvariant();

        // 2. Türkçe karakterleri ASCII eşdeğerlerine çevir (? karakterini KORUYORUZ!)
        result = result
            .Replace("Ü", "U").Replace("Ö", "O")
            .Replace("Ş", "S").Replace("Ç", "C")
            .Replace("Ğ", "G").Replace("İ", "I")
            .Replace("Ø", "O")
            .Replace("I", "I");

        // 3. Boşlukları ve noktalama işaretlerini sil, ama ? karakterini KORUYORUZ!
        result = new string(result.Where(c => char.IsLetterOrDigit(c) || c == '?').ToArray());

        return result.Trim();
    }

    /// <summary>
    /// İki string'i Türkçe karakter toleranslı karşılaştırır.
    /// Luca API'sinin Türkçe karakter encoding sorunu nedeniyle kullanılır.
    /// ? karakterleri wildcard olarak değerlendirilir (herhangi bir karakterle eşleşir).
    /// 
    /// 🔥 KRİTİK: ? karakterini KORUYARAK normalize ediyoruz (NormalizePreservingWildcard)
    /// Örnek: "DEMİR TALAŞ" vs "DEM?R TALA?" -> "DEMIRTALAS" vs "DEMRTALA?" -> MATCH!
    /// </summary>
    private static bool AreEqualIgnoringTurkishChars(string? str1, string? str2)
    {
        // Boş kontrolleri
        if (string.IsNullOrWhiteSpace(str1) && string.IsNullOrWhiteSpace(str2))
            return true;
        if (string.IsNullOrWhiteSpace(str1) || string.IsNullOrWhiteSpace(str2))
            return false;

        // Wildcard-preserving normalization (? karakterini KORUR)
        var normalized1 = NormalizePreservingWildcard(str1);
        var normalized2 = NormalizePreservingWildcard(str2);
        
        // Uzunluk farkı toleransı: %10'dan fazla fark varsa eşit değildir
        // (Bazen ? karakteri encoding yüzünden yer kaplayabilir)
        int maxLength = Math.Max(normalized1.Length, normalized2.Length);
        int minLength = Math.Min(normalized1.Length, normalized2.Length);
        if (maxLength > 0 && (double)(maxLength - minLength) / maxLength > 0.1)
        {
            return false; // %10'dan fazla uzunluk farkı
        }
        
        // Karakter karakter karşılaştır - WILDCARD mantığı
        int matchCount = 0;
        int compareLength = Math.Min(normalized1.Length, normalized2.Length);
        
        for (int i = 0; i < compareLength; i++)
        {
            char c1 = normalized1[i];
            char c2 = normalized2[i];
            
            // ? karakteri herhangi bir karakterle eşleşir (JOKER)
            if (c1 == '?' || c2 == '?')
            {
                matchCount++;
                continue;
            }
            
            // Karakterler aynıysa eşleşme sayısını artır
            if (c1 == c2)
            {
                matchCount++;
            }
        }
        
        // %90 veya daha fazla eşleşme varsa KABUL ET
        double matchRate = (double)matchCount / compareLength;
        return matchRate >= 0.90;
    }

    /// <summary>
    /// ULTRA TOLERANSLI NORMALİZASYON: Tüm encoding sorunlarını, boşlukları, noktalama işaretlerini temizler
    /// Ø, ø, ?? karakterlerini O'ya çevirir. Sadece harf ve rakamları bırakır.
    /// Örn: "Ø35*1,5 PIPE" → "O3515PIPE", "??35*1,5 PIPE" → "O3515PIPE"
    /// </summary>
    private static string NormalizeForUltraLooseComparison(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // 1. BÜYÜK harfe çevir
        var result = input.ToUpperInvariant();

        // 2. ENCODING SORUNLARINI ÇÖZME - Tüm bilinen varyantları temizle
        result = result
            // Türkçe karakterler
            .Replace("Ü", "U").Replace("Ö", "O")
            .Replace("Ş", "S").Replace("Ç", "C")
            .Replace("Ğ", "G").Replace("İ", "I")
            // Çap sembolü (diameter) varyantları
            .Replace("Ø", "O").Replace("ø", "O")
            .Replace("Φ", "O").Replace("φ", "O")  // Greek Phi (bazen Ø yerine kullanılır)
            // Encoding hatası karakterleri
            .Replace("?", "")  // Tüm ? karakterlerini sil (encoding bozukluğu)
            .Replace("�", "")  // Replacement character (Unicode U+FFFD)
            // Diğer yaygın encoding sorunları
            .Replace("Â", "A").Replace("â", "a")
            .Replace("Î", "I").Replace("î", "i")
            .Replace("Û", "U").Replace("û", "u")
            // Windows-1254 <-> UTF-8 encoding sorunları
            .Replace("Ã‡", "C")  // Ç encoding hatası
            .Replace("Ã–", "O")  // Ö encoding hatası
            .Replace("Å�", "I");  // İ encoding hatası

        // 3. Noktalama işaretlerini, boşlukları, özel karakterleri SİL
        // Sadece harf ve rakamları bırak
        result = new string(result.Where(c => char.IsLetterOrDigit(c)).ToArray());

        // 4. Trim
        return result.Trim();
    }

    /// <summary>
    /// İki string arasındaki benzerlik oranını hesaplar (Levenshtein Distance tabanlı)
    /// 0.0 = Tamamen farklı, 1.0 = Tamamen aynı
    /// Örn: "O3515PIPE" vs "O35151PIPE" → 0.91 (benzer)
    /// </summary>
    private static double CalculateStringSimilarity(string str1, string str2)
    {
        if (string.IsNullOrEmpty(str1) && string.IsNullOrEmpty(str2))
            return 1.0;
        if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
            return 0.0;
        if (str1 == str2)
            return 1.0;

        // Levenshtein Distance hesapla
        int distance = LevenshteinDistance(str1, str2);
        int maxLength = Math.Max(str1.Length, str2.Length);
        
        // Similarity = 1 - (distance / maxLength)
        double similarity = 1.0 - ((double)distance / maxLength);
        return similarity;
    }

    /// <summary>
    /// Levenshtein Distance (Edit Distance) hesaplar
    /// İki string arasındaki minimum düzenleme sayısını (insertion, deletion, substitution) bulur
    /// </summary>
    private static int LevenshteinDistance(string str1, string str2)
    {
        int n = str1.Length;
        int m = str2.Length;

        // Boş string kontrolü
        if (n == 0) return m;
        if (m == 0) return n;

        // DP matrisi
        int[,] d = new int[n + 1, m + 1];

        // İlk satır ve sütunu doldur
        for (int i = 0; i <= n; i++)
            d[i, 0] = i;
        for (int j = 0; j <= m; j++)
            d[0, j] = j;

        // DP ile distance hesapla
        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (str1[i - 1] == str2[j - 1]) ? 0 : 1;

                d[i, j] = Math.Min(
                    Math.Min(
                        d[i - 1, j] + 1,      // Deletion
                        d[i, j - 1] + 1),     // Insertion
                    d[i - 1, j - 1] + cost);  // Substitution
            }
        }

        return d[n, m];
    }

    #endregion

    #region Fatura Gönderme (E-Fatura/E-Arşiv)

    /// <summary>
    /// E-Fatura veya E-Arşiv olarak fatura gönderir.
    /// Not: Koleksiyonda endpoint tam belirtilmemiş, muhtemelen entegrasyon tetikleyicisidir.
    /// </summary>
    public async Task<JsonElement> SendInvoiceAsync(LucaSendInvoiceRequest request)
    {
        const string endpoint = "GonderFtrWsFaturaBaslik.do"; // Varsayılan endpoint ismi
        
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = content
        };
        ApplyManualSessionCookie(httpRequest);
        ApplySessionCookie(httpRequest);
        
        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    #endregion
}
