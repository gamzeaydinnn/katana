using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Katana.Business.DTOs;
using Katana.Business.Models.DTOs;
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
using Katana.Infrastructure.Mappers;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Helpers;

namespace Katana.Infrastructure.APIClients;

/// <summary>
/// LucaService - PART 2: Operations (Send/Create methods - Invoices, Customers, Stock, Products)
/// </summary>
public partial class LucaService
{
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
                    _isCookieAuthenticated = false;
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
            // Minimal delay to avoid rate limiting
            await Task.Delay(50);
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
                    BelgeSeri = invoice.BelgeSeri ?? _settings.DefaultBelgeSeri ?? "A",
                    BelgeNo = invoice.BelgeNo,
                    BelgeTarihi = invoice.BelgeTarihi == default ? DateTime.UtcNow : invoice.BelgeTarihi,
                    VadeTarihi = invoice.VadeTarihi,
                    BelgeTurDetayId = invoice.BelgeTurDetayId
                },
                FaturaTur = invoice.FaturaTur,
                ParaBirimKod = invoice.ParaBirimKod ?? "TRY",
                KurBedeli = invoice.KurBedeli,
                MusteriTedarikci = invoice.MusteriTedarikci,
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
            var belgeDate = dto.GnlOrgSsBelge?.BelgeTarihi ?? invoice.BelgeTarihi;
            dto.DocumentDate = belgeDate == default ? DateTime.UtcNow : belgeDate;
            dto.DueDate = invoice.VadeTarihi ?? DateTime.UtcNow;
            dto.CustomerTitle = invoice.CariTanim ?? string.Empty;
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

        if (invoice.BelgeNo.HasValue)
        {
            return $"{invoice.BelgeSeri ?? "A"}-{invoice.BelgeNo.Value}";
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
    private async Task<HttpResponseMessage> SendWithAuthRetryAsync(HttpRequestMessage request, string logTag, int maxAttempts = 2)
    {
        var attempt = 0;
        while (true)
        {
            attempt++;
            var client = _cookieHttpClient ?? _httpClient;

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
                _isCookieAuthenticated = false;
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
                _isCookieAuthenticated = false;
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
        var endpoint = _settings.Endpoints.CustomerCreate;
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
            // Minimal delay to avoid rate limiting
            await Task.Delay(25);
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
                                var defaultSeri = string.IsNullOrWhiteSpace(_settings.DefaultBelgeSeri) ? "A" : _settings.DefaultBelgeSeri.Trim();
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
    public async Task<List<LucaDepoDto>> GetDepoListAsync()
    {
        var element = await ListWarehousesAsync();
        return DeserializeList<LucaDepoDto>(element);
    }
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

    public async Task<JsonElement> ListStockCardsAsync(LucaListStockCardsRequest request)
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
            
            // 🔥 DEBUG: Session durumunu logla
            _logger.LogDebug("📋 ListStockCardsAsync başlıyor - Session durumu: Authenticated={IsAuth}, SessionCookie={HasSession}, ManualJSession={HasManual}, CookieExpiry={Expiry}",
                _isCookieAuthenticated,
                !string.IsNullOrWhiteSpace(_sessionCookie),
                !string.IsNullOrWhiteSpace(_manualJSessionId),
                _cookieExpiresAt?.ToString("HH:mm:ss") ?? "N/A");
            
            for (var attempt = 1; attempt <= 3; attempt++)
            {
                await EnsureAuthenticatedAsync();
                await EnsureBranchSelectedAsync();
                
                // 🔥 DEBUG: Her attempt öncesi cookie durumunu logla
                _logger.LogDebug("📋 ListStockCardsAsync Attempt {Attempt}/3 - Cookie: {Cookie}", 
                    attempt,
                    !string.IsNullOrWhiteSpace(_manualJSessionId) ? _manualJSessionId.Substring(0, Math.Min(30, _manualJSessionId.Length)) + "..." : 
                    !string.IsNullOrWhiteSpace(_sessionCookie) ? _sessionCookie.Substring(0, Math.Min(30, _sessionCookie.Length)) + "..." : "NONE");

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.StockCards)
                {
                    Content = CreateKozaContent(json)
                };
                ApplyManualSessionCookie(httpRequest);
                httpRequest.Headers.Add("No-Paging", "true");

                HttpResponseMessage? response = null;
                string responseContent = "[]";
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                    response = await client.SendAsync(httpRequest, cts.Token);
                    responseContent = await ReadResponseContentAsync(response);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("ListStockCardsAsync timed out (attempt {Attempt}); returning empty list to proceed with sync.", attempt);
                    return JsonDocument.Parse("[]").RootElement.Clone();
                }

                try
                {
                    await AppendRawLogAsync($"LIST_STOCK_CARDS_{attempt}", _settings.Endpoints.StockCards, json, response.StatusCode, responseContent);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to append LIST_STOCK_CARDS log (attempt {Attempt})", attempt);
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && attempt < 3)
                {
                    _logger.LogWarning("Stock card list returned 401; re-authenticating (attempt {Attempt})", attempt);
                    _isCookieAuthenticated = false;
                    await Task.Delay(200 * attempt);
                    continue;
                }

                if (!_settings.UseTokenAuth && NeedsBranchSelection(responseContent) && attempt < 3)
                {
                    _logger.LogWarning("Branch selection required while listing stock cards (attempt {Attempt}); retrying.", attempt);
                    _isCookieAuthenticated = false;
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
                        _isCookieAuthenticated = false;
                        await Task.Delay(500 * attempt);
                        continue;
                    }
                    
                    return JsonDocument.Parse("[]").RootElement.Clone();
                }
            }

            return JsonDocument.Parse("[]").RootElement.Clone();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ListStockCardsAsync failed; returning empty list to allow sync to proceed.");
            return JsonDocument.Parse("[]").RootElement.Clone();
        }
    }
    public async Task<List<LucaStockCardSummaryDto>> ListStockCardsAsync(CancellationToken cancellationToken = default)
    {
        var result = new List<LucaStockCardSummaryDto>();

        try
        {
            if (string.IsNullOrWhiteSpace(_manualJSessionId) && !_settings.UseTokenAuth)
            {
                _logger.LogWarning("ListStockCardsAsync: No manual session id present; results may be empty if Koza requires login cookie.");
            }

            await EnsureAuthenticatedAsync();
            await EnsureBranchSelectedAsync();

            var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
            var endpoint = _settings.Endpoints.StockCards;

            var sb = new StringBuilder();
            sb.Append("stkSkart.kodOp=like");
            sb.Append("&stkSkart.kodBas=");
            sb.Append("&start=0");
            sb.Append("&limit=10000");

            var formDataString = sb.ToString();
            var encoding = Encoding.GetEncoding(1254);
            var byteContent = new ByteArrayContent(encoding.GetBytes(formDataString));
            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            byteContent.Headers.ContentType.CharSet = "windows-1254";

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = byteContent
            };
            ApplyManualSessionCookie(httpRequest);

            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(httpRequest, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ListStockCardsAsync: HTTP call failed");
                return result;
            }

            var rawBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            string responseContent;
            try { responseContent = encoding.GetString(rawBytes); } catch { responseContent = Encoding.UTF8.GetString(rawBytes); }

            if (responseContent.TrimStart().StartsWith("<", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("ListStockCardsAsync: Koza returned HTML (session expired?). Forcing complete session refresh...");
                
                // Session expired - force complete refresh
                try
                {
                    await ForceSessionRefreshAsync();
                    
                    // Yeni content oluştur (HttpContent bir kez kullanıldıktan sonra tekrar kullanılamaz)
                    var retryByteContent = new ByteArrayContent(encoding.GetBytes(formDataString));
                    retryByteContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                    retryByteContent.Headers.ContentType.CharSet = "windows-1254";
                    
                    // Retry request
                    using var retryRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
                    {
                        Content = retryByteContent
                    };
                    ApplyManualSessionCookie(retryRequest);
                    
                    var retryResponse = await client.SendAsync(retryRequest, cancellationToken);
                    var retryBytes = await retryResponse.Content.ReadAsByteArrayAsync(cancellationToken);
                    string retryContent;
                    try { retryContent = encoding.GetString(retryBytes); } catch { retryContent = Encoding.UTF8.GetString(retryBytes); }
                    
                    if (retryContent.TrimStart().StartsWith("<", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogError("ListStockCardsAsync: Still HTML after retry. Body snippet: {Snippet}",
                            retryContent.Length > 300 ? retryContent[..300] : retryContent);
                        return result;
                    }
                    
                    responseContent = retryContent;
                }
                catch (Exception retryEx)
                {
                    _logger.LogError(retryEx, "ListStockCardsAsync: Retry failed after HTML response");
                    return result;
                }
            }

            JsonElement element;
            try
            {
                element = JsonSerializer.Deserialize<JsonElement>(responseContent);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ListStockCardsAsync: JSON parse failed for stock card list");
                return result;
            }

            JsonElement arrayEl = default;
            if (element.ValueKind == JsonValueKind.Array)
            {
                arrayEl = element;
            }
            else if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var key in new[] { "stkSkart", "data", "list", "items" })
                {
                    if (element.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.Array)
                    {
                        arrayEl = prop;
                        break;
                    }
                }
            }

            if (arrayEl.ValueKind != JsonValueKind.Array)
            {
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

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ListStockCardsAsync failed; returning empty list.");
            return result;
        }
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

    public async Task<JsonElement> ListStockCardSuppliersAsync(LucaStockCardByIdRequest request)
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
        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
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
            .GroupBy(c => c.KartKodu)
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
        
        // Batch işleme için ayarlar
        const int batchSize = 20; // 🔥 Küçültüldü: Session timeout önleme
        const int rateLimitDelayMs = 300; // Rate limit için bekleme süresi (ms)
        
        try
        {
            await EnsureAuthenticatedAsync();
            await EnsureBranchSelectedAsync();
            await VerifyBranchSelectionAsync();
            _logger.LogWarning(">>> USING SAFE PER-PRODUCT FLOW WITH UPSERT LOGIC <<<");
            _logger.LogInformation("Sending {Count} stock cards to Luca (Koza) with batch size {BatchSize}", uniqueCards.Count, batchSize);

            // NOT: client'ı her seferinde güncel al, ForceSessionRefresh _cookieHttpClient'ı yenileyebilir
            var endpoint = _settings.Endpoints.StockCardCreate;
            // 🔥 RAPOR ZORUNLULUĞU: ISO-8859-9 encoding kullanılmalı (1254 değil!)
            // Luca API Türkçe karakterler için ISO-8859-9 bekliyor
            var encoding = Encoding.GetEncoding("ISO-8859-9");
            
            // Batch işleme (uniqueCards kullan)
            var batches = uniqueCards
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

                // 🔥 PERFORMANS OPTİMİZASYONU: Her batch başında session'ı yenile VE TÜM Luca kartlarını cache'e al
                try
                {
                    _logger.LogInformation("🔄 Batch {BatchNumber}/{TotalBatches} başlıyor - Session yenileniyor...", 
                        batchNumber, batches.Count);
                    await ForceSessionRefreshAsync();
                    await EnsureBranchSelectedAsync(); // Branch selection da yap
                    _logger.LogInformation("✅ Session ve branch hazır");
                    
                    // 🚀 CACHE WARMING: Tüm Luca stok kartlarını ön-yükle (tek seferde!)
                    // Bu sayede her ürün için ayrı API çağrısı yapmayız (10x-100x hızlanma!)
                    _logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                    _logger.LogInformation("📥 CACHE WARMING BAŞLIYOR - Tüm Luca stok kartları çekiliyor...");
                    _logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                    
                    var allLucaCards = await ListStockCardsSimpleAsync(CancellationToken.None);
                    
                    if (allLucaCards.Count == 0)
                    {
                        _logger.LogError("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                        _logger.LogError("❌ KRİTİK HATA: CACHE WARMING BAŞARISIZ!");
                        _logger.LogError("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                        _logger.LogError("   ⚠️ ListStockCardsSimpleAsync() 0 ürün döndü!");
                        _logger.LogError("   ⚠️ SEBEP: JSON parse hatası, session timeout, veya yetki sorunu olabilir");
                        _logger.LogError("   ⚠️ SONUÇ: Cache boş kalacak, tüm ürünler için CANLI API sorgusu yapılacak (YAVAŞ!)");
                        _logger.LogError("   ⚠️ FİX: ListStockCardsSimpleAsync() loglarını kontrol et, 'U' hatası varsa fix yap");
                        _logger.LogError("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                    }
                    else
                    {
                        _logger.LogInformation("✅ {Count} stok kartı Luca'dan çekildi", allLucaCards.Count);
                    }
                    
                    // Cache'i doldur
                    await _stockCardCacheLock.WaitAsync();
                    try
                    {
                        _stockCardCache.Clear(); // Eski cache'i temizle
                        
                        int validCount = 0;
                        int invalidCount = 0;
                        
                        foreach (var lucaCard in allLucaCards)
                        {
                            if (!string.IsNullOrWhiteSpace(lucaCard.KartKodu) && lucaCard.StokKartId.HasValue)
                            {
                                _stockCardCache[lucaCard.KartKodu] = lucaCard.StokKartId.Value;
                                validCount++;
                            }
                            else
                            {
                                invalidCount++;
                            }
                        }
                        
                        if (validCount == 0 && allLucaCards.Count > 0)
                        {
                            _logger.LogError("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                            _logger.LogError("❌ KRİTİK HATA: DTO MAPPING HATASI!");
                            _logger.LogError("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                            _logger.LogError("   ⚠️ {TotalCards} kart çekildi AMA hiçbirinde KartKodu veya StokKartId yok!", allLucaCards.Count);
                            _logger.LogError("   ⚠️ SEBEP: KozaStokKartiDto field isimleri Luca API'si ile uyuşmuyor olabilir");
                            _logger.LogError("   ⚠️ FİX: KozaStokKartiDto sınıfını kontrol et, field adlarını Luca response'a göre düzelt");
                            _logger.LogError("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                        }
                        else if (invalidCount > 0)
                        {
                            _logger.LogWarning("⚠️ {ValidCount} geçerli, {InvalidCount} geçersiz kart (KartKodu veya ID eksik)", 
                                validCount, invalidCount);
                        }
                        
                        _logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                        _logger.LogInformation("✅ CACHE HAZIR: {Count} SKU → StokKartId mapping", _stockCardCache.Count);
                        _logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                    }
                    finally
                    {
                        _stockCardCacheLock.Release();
                    }
                }
                catch (InvalidOperationException criticalEx)
                {
                    // 🔴 FAIL FAST: Cache warming CRITICAL hatası - Sync'i durdur!
                    _logger.LogError(criticalEx, "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                    _logger.LogError("❌ FATAL ERROR: CACHE WARMING KRİTİK HATASI!");
                    _logger.LogError("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                    _logger.LogError("   ⚠️ Luca'dan stok kartları çekilemedi (JSON parse hatası veya API error)");
                    _logger.LogError("   ⚠️ Cache boş kaldı - Senkronizasyon GÜVENLİ DEĞİL!");
                    _logger.LogError("   ⚠️ SEBEP: {Message}", criticalEx.Message);
                    _logger.LogError("   ⚠️ AKSIYON: Sync DURDURULDU (veri bütünlüğü için)");
                    _logger.LogError("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                    
                    result.IsSuccess = false;
                    result.FailedRecords = uniqueCards.Count;
                    result.Errors.Add($"CRITICAL: Cache warming failed - {criticalEx.Message}");
                    result.Message = "Sync aborted due to cache warming failure (data integrity protection)";
                    
                    // SYNC'İ DURDUR - Exception fırlat
                    throw new InvalidOperationException(
                        "Sync aborted: Cache warming failed. Cannot safely proceed without knowing existing stock cards. " +
                        "This prevents duplicate creation and data corruption.", criticalEx);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ Batch başında beklenmeyen hata (cache warming)");
                    _logger.LogWarning("   → Cache boş kalabilir, performans düşük olacak ama sync devam edecek");
                }

            foreach (var card in batch)
            {
                try
                {
                    // 🔥 UPSERT LOGIC STEP 1: Cache'den kontrol yap (hızlı!)
                    _logger.LogDebug("🔍 [1/3] Cache kontrolü: {SKU}", card.KartKodu);
                    var existingSkartId = await FindStockCardBySkuAsync(card.KartKodu); // Cache'den gelecek
                    
                    if (existingSkartId.HasValue)
                    {
                        _logger.LogInformation("📦 [CACHE HIT] Stok kartı bulundu: {SKU} (skartId: {Id})", 
                            card.KartKodu, existingSkartId.Value);
                        
                        // Kayıt zaten var - Luca API güncelleme desteklemiyor
                        // Değişiklik kontrolü yap
                        var existingCard = await GetStockCardDetailsBySkuAsync(card.KartKodu);
                        bool hasChanges = HasStockCardChanges(card, existingCard);
                        
                        if (!hasChanges)
                        {
                            _logger.LogInformation("⏭️ SKIP: {SKU} zaten Luca'da var, değişiklik yok - atlanıyor", 
                                card.KartKodu);
                            skippedCount++;
                            duplicateCount++;
                            continue;
                        }
                        else
                        {
                            // 🔥 DEĞİŞİKLİK VAR - Luca güncelleme desteklemiyor, YENİ VERSİYONLU SKU İLE KART AÇ
                            _logger.LogWarning("⚠️ KATANA'DA ÜRÜN GÜNCELLENDİ: {SKU}", card.KartKodu);
                            _logger.LogWarning("🚫 Luca API güncelleme desteklemiyor - Yeni versiyonlu SKU ile stok kartı açılacak");
                            
                            var originalSku = card.KartKodu;
                            
                            // Yeni versiyonlu SKU oluştur (örn: SKU-V2, SKU-V3...)
                            var newVersionedSku = await GenerateVersionedSkuAsync(card.KartKodu);
                            
                            _logger.LogWarning("📝 YENİ STOK KARTI OLUŞTURULUYOR:");
                            _logger.LogWarning("   Orijinal SKU: {OldSKU}", originalSku);
                            _logger.LogWarning("   Yeni SKU: {NewSKU}", newVersionedSku);
                            _logger.LogWarning("   Sebep: Katana'da ürün bilgileri güncellendi, Luca'da yeni versiyon açılıyor");
                            
                            // Kartı yeni SKU ile güncelle
                            card.KartKodu = newVersionedSku;
                            
                            // 🔥 KRİTİK FİX: Barkod çakışmasını önle!
                            // Orijinal ürünün barkodu zaten kullanılıyor, yeni versiyonda boş gönder
                            card.Barkod = string.Empty;
                            _logger.LogInformation("🔧 Barkod temizlendi (duplicate barcode önleme): {SKU}", newVersionedSku);
                            
                            // Devam et ve yeni kart olarak oluştur (aşağıdaki kod bloğuna geç)
                        }
                    }
                    else
                    {
                        _logger.LogInformation("✨ [CACHE MISS] Yeni stok kartı: {SKU}", card.KartKodu);
                        
                        // 🔥 DEFENSIVE PROGRAMMING STEP 2: DOUBLE CHECK!
                        // Cache MISS demek, GERÇEKTEN YOK demek değildir!
                        // Cache warming patlamış olabilir (Struts "Unable to instantiate Action" hatası)
                        // İçerik eksik ya da null dönmüş olabilir (optimistic programming hatası)
                        // SON BİR KEZ DAHA KONTROL ET: Canlı API'den SKU'yu tekrar sorgula!
                        _logger.LogWarning("⚠️ [2/3] Cache MISS tespit edildi - SAFETY CHECK: Canlı API'den tekrar sorgulanıyor...");
                        
                        long? liveCheckSkartId = null;
                        try
                        {
                            // Tekrar dene: Fuzzy search ile SKU'yu bul (cache'i tekrar kullanır ama boşsa API'ye gider)
                            // ANCAK cache zaten boşsa, bu çağrı da boş dönebilir!
                            // Daha güvenli: Direkt ListStockCardsSimpleAsync çağır ve manuel ara!
                            _logger.LogDebug("🔍 GetStockCardBySkuFromLiveApiAsync (Fuzzy Search) çağrılıyor: {SKU}", card.KartKodu);
                            
                            // Alternatif 1: FindStockCardBySkuAsync tekrar dene (cache'den gelirse bile, güvenli)
                            liveCheckSkartId = await FindStockCardBySkuAsync(card.KartKodu);
                            
                            if (liveCheckSkartId.HasValue)
                            {
                                // 🚨 KRİTİK HATA: Cache'de YOKTU, ama canlı API'de VAR!
                                // Cache warming çökmüş veya eksik yüklenmiş!
                                _logger.LogError("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                                _logger.LogError("🚨 [CACHE INTEGRITY ERROR] SKU: {SKU}", card.KartKodu);
                                _logger.LogError("   Cache sonucu: BULUNAMADI (null)");
                                _logger.LogError("   Live API sonucu: BULUNDU (skartId: {Id})", liveCheckSkartId.Value);
                                _logger.LogError("   SONUÇ: Cache warming başarısız veya eksik!");
                                _logger.LogError("   Duplicate oluşturma ÖNLENDİ - UPDATE/SKIP mantığına devam ediliyor");
                                _logger.LogError("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                                
                                // 🔥 UPSERT LOGIC: Bulundu, o halde değişiklik kontrolüne geç
                                existingSkartId = liveCheckSkartId;
                                
                                var existingCard = await GetStockCardDetailsBySkuAsync(card.KartKodu);
                                bool hasChanges = HasStockCardChanges(card, existingCard);
                                
                                if (!hasChanges)
                                {
                                    _logger.LogInformation("⏭️ SKIP (live check): {SKU} zaten var, değişiklik yok - atlanıyor", card.KartKodu);
                                    skippedCount++;
                                    duplicateCount++;
                                    continue; // Bir sonraki karta geç
                                }
                                else
                                {
                                    // Değişiklik var - versiyonlu yeni kart oluştur
                                    _logger.LogWarning("⚠️ KATANA'DA ÜRÜN GÜNCELLENDİ (live check): {SKU}", card.KartKodu);
                                    var originalSku2 = card.KartKodu;
                                    var newVersionedSku2 = await GenerateVersionedSkuAsync(card.KartKodu);
                                    
                                    _logger.LogWarning("📝 YENİ VERSIYONLU STOK KARTI:");
                                    _logger.LogWarning("   Orijinal: {Old}", originalSku2);
                                    _logger.LogWarning("   Yeni: {New}", newVersionedSku2);
                                    
                                    card.KartKodu = newVersionedSku2;
                                    card.Barkod = string.Empty; // Duplicate barcode önle
                                    
                                    // Yeni SKU ile devam et (aşağıdaki create bloğuna git)
                                }
                            }
                            else
                            {
                                // ✅ Güvenli: Cache'de de yok, live API'de de yok - gerçekten yeni kart
                                _logger.LogInformation("✅ [SAFETY CHECK PASSED] SKU gerçekten yok: {SKU} - CREATE yapılacak", card.KartKodu);
                            }
                        }
                        catch (Exception liveCheckEx)
                        {
                            // Live check patlasa bile devam et (ama logla!)
                            _logger.LogError(liveCheckEx, "❌ Live safety check başarısız (SKU: {SKU}), CREATE'e devam ediliyor (RİSKLİ!)", card.KartKodu);
                        }
                    }
                    
                    // Yeni kayıt oluştur
                    _logger.LogInformation("➕ [3/3] Yeni stok kartı POST ediliyor: {KartKodu}", card.KartKodu);
                    
                    // 🔥 Postman örneğine göre JSON formatında request oluştur
                    var baslangic = string.IsNullOrWhiteSpace(card.BaslangicTarihi)
                        ? DateTime.Now.ToString("dd'/'MM'/'yyyy", System.Globalization.CultureInfo.InvariantCulture)
                        : card.BaslangicTarihi;
                    var safeName = (card.KartAdi ?? string.Empty)
                        .Replace("Ø", "O")
                        .Replace("ø", "o")
                        .Trim();
                    var safeCode = (card.KartKodu ?? string.Empty)
                        .Replace("Ø", "O")
                        .Replace("ø", "o")
                        .Trim();
                    
                    // ✅ KartAdi boşsa SKU kullan (fallback)
                    if (string.IsNullOrWhiteSpace(safeName))
                    {
                        _logger.LogWarning("⚠️ KartAdi boş, SKU kullanılıyor: {KartKodu}", card.KartKodu);
                        safeName = card.KartKodu ?? "UNKNOWN-PRODUCT";
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
                        _isCookieAuthenticated = false;
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
                            _stockCardCache[card.KartKodu] = newSkartId.Value;
                            _logger.LogDebug("🔄 Cache'e eklendi: {SKU} → {Id}", card.KartKodu, newSkartId.Value);
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
                // Rate limiting - her kayıt arasında kısa bekleme
                await Task.Delay(rateLimitDelayMs);
            }
            
            // Batch arası bekleme - API'yi yormamak ve session timeout önlemek için
            if (batchNumber < batches.Count)
            {
                _logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                _logger.LogInformation("✅ Batch {BatchNumber}/{TotalBatches} tamamlandı", batchNumber, batches.Count);
                _logger.LogInformation("   Başarılı: {Success}, Başarısız: {Failed}, Duplicate: {Duplicate}", 
                    successCount, failedCount, duplicateCount);
                _logger.LogInformation("⏳ Sonraki batch için 2 saniye bekleniyor...");
                _logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                await Task.Delay(2000); // 🔥 2 saniyeye çıkarıldı
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
        // Detaylı mesaj - Luca API güncelleme desteklemediğini belirt
        if (skippedCount > 0 || duplicateCount > 0)
        {
            result.Message = $"✅ {successCount} yeni oluşturuldu, ⏭️ {skippedCount} atlandı (zaten mevcut/değişiklik yok), ❌ {failedCount} başarısız. Toplam: {stockCards.Count}";
        }
        else
        {
            result.Message = $"✅ {successCount} başarılı, ❌ {failedCount} başarısız. Toplam: {stockCards.Count}";
        }
        result.Duration = DateTime.UtcNow - startTime;
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
