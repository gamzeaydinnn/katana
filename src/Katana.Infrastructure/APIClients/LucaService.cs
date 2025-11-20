using Katana.Business.DTOs;
using Katana.Data.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using Katana.Business.Interfaces;
using Katana.Core.DTOs;
/*LucaService.cs (Genişletilecek): Luca Koza API'sına veri yazma operasyonlarını içerecek.
Amacı: Sadece Luca'ya veri yazmaktan sorumlu olmak.
Sorumlulukları (Yeni):
Dönüştürülmüş fatura verisini muhasebe kaydı olarak işleme metodu.
Stok hareketlerini işleme metodu.*/

namespace Katana.Infrastructure.APIClients;
//Luca-specific payload, fallback to CSV file export if API not available.
public class LucaService : ILucaService
{
    private readonly HttpClient _httpClient;
    private readonly LucaApiSettings _settings;
    private readonly ILogger<LucaService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private string? _authToken;
    private DateTime? _tokenExpiry;
    // Cookie-based auth support for Koza
    private System.Net.CookieContainer? _cookieContainer;
    private HttpClientHandler? _cookieHandler;
    private HttpClient? _cookieHttpClient;
    private bool _isCookieAuthenticated = false;

    public LucaService(HttpClient httpClient, IOptions<LucaApiSettings> settings, ILogger<LucaService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    private async Task EnsureAuthenticatedAsync()
    {
        if (_settings.UseTokenAuth)
        {
            if (_authToken == null || _tokenExpiry == null || DateTime.UtcNow >= _tokenExpiry)
            {
                await AuthenticateAsync();
            }
            return;
        }

        // Use cookie-based auth (Koza) when UseTokenAuth == false
        if (!_isCookieAuthenticated)
        {
            await AuthenticateWithCookieAsync();
        }
    }

    private async Task AuthenticateWithCookieAsync()
    {
        try
        {
            _logger.LogInformation("Authenticating (cookie) with Koza Luca API");

            _cookieContainer ??= new System.Net.CookieContainer();
            _cookieHandler ??= new HttpClientHandler
            {
                CookieContainer = _cookieContainer,
                UseCookies = true,
                AllowAutoRedirect = true
            };
            _cookieHttpClient ??= new HttpClient(_cookieHandler);
            _cookieHttpClient.BaseAddress = new Uri($"{_settings.BaseUrl.TrimEnd('/')}/");
            _cookieHttpClient.DefaultRequestHeaders.Accept.Clear();
            _cookieHttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var authRequest = new
            {
                orgCode = _settings.MemberNumber,
                userName = _settings.Username,
                userPassword = _settings.Password
            };

            var json = JsonSerializer.Serialize(authRequest, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _cookieHttpClient.PostAsync(_settings.Endpoints.Auth, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Koza auth failed (status): {Status}", response.StatusCode);
                throw new UnauthorizedAccessException("Koza auth failed");
            }

            var respStr = await response.Content.ReadAsStringAsync();
            try
            {
                var doc = JsonSerializer.Deserialize<JsonElement>(respStr);
                if (doc.ValueKind == JsonValueKind.Object && doc.TryGetProperty("code", out var codeProp))
                {
                    var code = codeProp.GetInt32();
                    if (code != 0)
                    {
                        var msg = doc.TryGetProperty("message", out var m) ? m.GetString() : respStr;
                        _logger.LogError("Koza auth response code !=0: {Code} {Message}", code, msg);
                        throw new UnauthorizedAccessException("Koza auth response unsuccessful: " + msg);
                    }
                }
            }
            catch (Exception)
            {
                // ignore parsing error and assume success if status code was success
            }

            // Now select branch: call branches endpoint (empty JSON body)
            var branchesResp = await _cookieHttpClient.PostAsync(_settings.Endpoints.Branches, new StringContent("{}", Encoding.UTF8, "application/json"));
            if (branchesResp.IsSuccessStatusCode)
            {
                var branchesJson = await branchesResp.Content.ReadAsStringAsync();
                try
                {
                    using var doc = JsonDocument.Parse(branchesJson);

                    JsonElement? listEl = null;

                    // Common wrappers: root array, or { list: [...] }, or { branches: [...] }, or { data: [...] }
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        listEl = doc.RootElement;
                    }
                    else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        if (doc.RootElement.TryGetProperty("list", out var l) && l.ValueKind == JsonValueKind.Array)
                            listEl = l;
                        else if (doc.RootElement.TryGetProperty("branches", out var b) && b.ValueKind == JsonValueKind.Array)
                            listEl = b;
                        else if (doc.RootElement.TryGetProperty("data", out var d) && d.ValueKind == JsonValueKind.Array)
                            listEl = d;
                        else if (doc.RootElement.TryGetProperty("list", out var alt) && alt.ValueKind == JsonValueKind.Object)
                        {
                            // sometimes list may be wrapped differently; try to locate first array child
                            foreach (var prop in alt.EnumerateObject())
                            {
                                if (prop.Value.ValueKind == JsonValueKind.Array)
                                {
                                    listEl = prop.Value;
                                    break;
                                }
                            }
                        }
                    }

                    if (listEl.HasValue && listEl.Value.ValueKind == JsonValueKind.Array && listEl.Value.GetArrayLength() > 0)
                    {
                        var array = listEl.Value;

                        // Save branches JSON to repo ./logs for debugging / reproducible tests
                        try
                        {
                            var repoLogDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
                            Directory.CreateDirectory(repoLogDir);
                            var repoFile = Path.Combine(repoLogDir, "luca-branches.json");
                            await File.WriteAllTextAsync(repoFile, array.GetRawText());
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to write luca-branches.json to repo logs");
                        }

                        JsonElement? chosen = null;
                        long? chosenId = null;

                        // Prefer an element with an explicit 'isDefault' boolean flag
                        foreach (var el in array.EnumerateArray())
                        {
                            if (el.ValueKind != JsonValueKind.Object) continue;
                            if (el.TryGetProperty("isDefault", out var isDef) && isDef.ValueKind == JsonValueKind.True)
                            {
                                chosen = el;
                                break;
                            }
                            // Some providers may use localized or different flag names
                            if (el.TryGetProperty("varsayilan", out var vs) && vs.ValueKind == JsonValueKind.True)
                            {
                                chosen = el;
                                break;
                            }
                        }

                        // If no explicit default found, pick the first element
                        if (!chosen.HasValue)
                        {
                            var enumFirst = array.EnumerateArray().FirstOrDefault();
                            if (enumFirst.ValueKind != JsonValueKind.Undefined)
                                chosen = enumFirst;
                        }

                        // Extract possible id fields from chosen element
                        if (chosen.HasValue)
                        {
                            var el = chosen.Value;
                            long parsedId = 0;
                            bool got = false;
                            string[] idNames = new[] { "orgSirketSubeId", "orgSirketSubeID", "id", "subeId", "sirketSubeId", "orgSubeId" };
                            foreach (var name in idNames)
                            {
                                if (el.TryGetProperty(name, out var p))
                                {
                                    if (p.ValueKind == JsonValueKind.Number && p.TryGetInt64(out parsedId))
                                    {
                                        got = true; break;
                                    }
                                    if (p.ValueKind == JsonValueKind.String && long.TryParse(p.GetString(), out parsedId))
                                    {
                                        got = true; break;
                                    }
                                }
                            }

                            if (!got)
                            {
                                // As a final fallback, check common 'id' as number or string
                                if (el.TryGetProperty("id", out var anyid))
                                {
                                    if (anyid.ValueKind == JsonValueKind.Number && anyid.TryGetInt64(out parsedId)) got = true;
                                    else if (anyid.ValueKind == JsonValueKind.String && long.TryParse(anyid.GetString(), out parsedId)) got = true;
                                }
                            }

                            if (got)
                            {
                                chosenId = parsedId;
                            }
                        }

                        if (chosenId.HasValue)
                        {
                            try
                            {
                                _logger.LogInformation("Selected Koza branch id {BranchId} and attempting ChangeBranch", chosenId.Value);
                                var changeBody = new { orgSirketSubeId = chosenId.Value };
                                var changeJson = JsonSerializer.Serialize(changeBody, _jsonOptions);
                                var changeResp = await _cookieHttpClient.PostAsync(_settings.Endpoints.ChangeBranch, new StringContent(changeJson, Encoding.UTF8, "application/json"));
                                if (!changeResp.IsSuccessStatusCode)
                                {
                                    _logger.LogWarning("Failed to change branch in Koza (status {Status})", changeResp.StatusCode);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "ChangeBranch call failed");
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Could not determine a branch id from Koza branches response; no ChangeBranch attempt made");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse branches response");
                }
            }

            _isCookieAuthenticated = true;
            _logger.LogInformation("Authenticated with Koza (cookie) and branch selected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating with Koza (cookie)");
            throw;
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
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_settings.Endpoints.Auth, content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var authResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                _authToken = authResponse.GetProperty("token").GetString();
                var expiresIn = authResponse.GetProperty("expiresIn").GetInt32();
                _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60); // Refresh 1 minute before expiry

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

    public async Task<SyncResultDto> SendInvoicesAsync(List<LucaInvoiceDto> invoices)
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

            _logger.LogInformation("Sending {Count} invoices to Luca", invoices.Count);

            var json = JsonSerializer.Serialize(invoices, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
            var response = await client.PostAsync(_settings.Endpoints.Invoices, content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var lucaResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

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

                _logger.LogError("Failed to send invoices to Luca. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);
            }
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.FailedRecords = invoices.Count;
            result.Message = ex.Message;
            result.Errors.Add(ex.ToString());

            _logger.LogError(ex, "Error sending invoices to Luca");
        }

        result.Duration = DateTime.UtcNow - startTime;
        return result;
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

            _logger.LogInformation("Sending {Count} stock movements to Luca", stockMovements.Count);

            var json = JsonSerializer.Serialize(stockMovements, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
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

    public async Task<SyncResultDto> SendCustomersAsync(List<LucaCustomerDto> customers)
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

            _logger.LogInformation("Sending {Count} customers to Luca", customers.Count);

            var json = JsonSerializer.Serialize(customers, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
            var response = await client.PostAsync(_settings.Endpoints.CustomerCreate, content);

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

                _logger.LogError("Failed to send customers to Luca. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);
            }
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.FailedRecords = customers.Count;
            result.Message = ex.Message;
            result.Errors.Add(ex.ToString());

            _logger.LogError(ex, "Error sending customers to Luca");
        }

        result.Duration = DateTime.UtcNow - startTime;
        return result;
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
            await EnsureAuthenticatedAsync();

            _logger.LogInformation("Sending {Count} products to Luca", products.Count);

            var json = JsonSerializer.Serialize(products, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
            var response = await client.PostAsync(_settings.Endpoints.Products, content);

            if (response.IsSuccessStatusCode)
            {
                result.IsSuccess = true;
                result.SuccessfulRecords = products.Count;
                result.Message = "Products sent successfully to Luca";

                _logger.LogInformation("Successfully sent {Count} products to Luca", products.Count);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                result.IsSuccess = false;
                result.FailedRecords = products.Count;
                result.Message = $"Failed to send products to Luca: {response.StatusCode}";
                result.Errors.Add(errorContent);

                _logger.LogError("Failed to send products to Luca. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);
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

    public async Task<JsonElement> ListTaxOfficesAsync(LucaListTaxOfficesRequest? request = null)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request ?? new LucaListTaxOfficesRequest(), _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.TaxOffices)
        {
            Content = content
        };
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
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.MeasurementUnits)
        {
            Content = content
        };
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
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.CustomerList)
        {
            Content = content
        };
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
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.SupplierList)
        {
            Content = content
        };
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
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.Warehouses)
        {
            Content = content
        };
        httpRequest.Headers.Add("No-Paging", "true");

        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> ListStockCardsAsync(LucaListStockCardsRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request ?? new LucaListStockCardsRequest(), _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.StockCards)
        {
            Content = content
        };
        httpRequest.Headers.Add("No-Paging", "true");

        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> ListStockCardPriceListsAsync(LucaListStockCardPriceListsRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.StockCardPriceLists)
        {
            Content = content
        };
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
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.StockCardAltUnits)
        {
            Content = content
        };
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
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.StockCardAltStocks)
        {
            Content = content
        };
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
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.StockCardCosts)
        {
            Content = content
        };
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
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.StockCardSuppliers)
        {
            Content = content
        };
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
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.StockCardPurchaseTerms)
        {
            Content = content
        };
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
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.CustomerContacts)
        {
            Content = content
        };
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
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.BankList)
        {
            Content = content
        };
        httpRequest.Headers.Add("No-Paging", "true");

        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> GetWarehouseStockQuantityAsync(long depoId)
    {
        await EnsureAuthenticatedAsync();

        var url = $"{_settings.Endpoints.WarehouseStockQuantity}?cagirilanKart=depo&stkDepo.depoId={depoId}";
        var content = new StringContent("{}", Encoding.UTF8, "application/json");

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };
        httpRequest.Headers.Add("No-Paging", "true");

        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> ListSalesOrdersAsync(bool detayliListe = false)
    {
        await EnsureAuthenticatedAsync();

        var url = _settings.Endpoints.SalesOrderList + (detayliListe ? "?detayliListe=true" : string.Empty);
        var content = new StringContent("{}", Encoding.UTF8, "application/json");

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };
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
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.StockCategories)
        {
            Content = content
        };
        httpRequest.Headers.Add("No-Paging", "true");

        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
        public async Task<SyncResultDto> SendStockCardsAsync(List<LucaCreateStokKartiRequest> stockCards)
        {
            var result = new SyncResultDto
            {
                SyncType = "PRODUCT_STOCK_CARD",
            ProcessedRecords = stockCards.Count
        };

        var startTime = DateTime.UtcNow;

            try
            {
                await EnsureAuthenticatedAsync();

                _logger.LogInformation("Sending {Count} stock cards to Luca (Koza)", stockCards.Count);

                var json = JsonSerializer.Serialize(stockCards, _jsonOptions);
                // Log a trimmed preview to help debug Koza tarafı
                var preview = json.Length > 1200 ? json.Substring(0, 1200) + "...(truncated)" : json;
                _logger.LogDebug("Koza stock card request payload (preview): {Preview}", preview);

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
                // Prefer SendAsync with explicit request so we can capture URL easily
                var endpoint = _settings.Endpoints.Products;
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = content
                };

                var response = await client.SendAsync(httpRequest);
                // Read raw bytes so we can attempt multiple decodings (Koza may return Windows-1254 encoded body)
                var responseBytes = await response.Content.ReadAsByteArrayAsync();
                string responseContentUtf8 = string.Empty;
                string responseContent1254 = string.Empty;
                try
                {
                    responseContentUtf8 = Encoding.UTF8.GetString(responseBytes);
                }
                catch { responseContentUtf8 = string.Empty; }
                try
                {
                    responseContent1254 = Encoding.GetEncoding(1254).GetString(responseBytes);
                }
                catch { responseContent1254 = responseContentUtf8; }

                // Choose which decoded content to log/parse. Prefer cp1254 if it contains a JSON 'code' field.
                var responseContent = responseContentUtf8;
                try
                {
                    if (!string.IsNullOrEmpty(responseContent1254))
                    {
                        var tmp = JsonSerializer.Deserialize<JsonElement>(responseContent1254);
                        if (tmp.ValueKind == JsonValueKind.Object && tmp.TryGetProperty("code", out _))
                        {
                            responseContent = responseContent1254;
                        }
                    }
                }
                catch
                {
                    // ignore parsing error and stick with UTF8
                }

                _logger.LogDebug("Koza stock card response. Status: {Status}; Body (utf8): {BodyUtf8}", response.StatusCode, responseContentUtf8);
                _logger.LogDebug("Koza stock card response. Body (cp1254): {Body1254}", responseContent1254);

                // Append raw request/response to local diagnostic log for inspection
                try
                {
                    var baseUrl = client.BaseAddress?.ToString()?.TrimEnd('/') ?? _settings.BaseUrl?.TrimEnd('/') ?? string.Empty;
                    var fullUrl = string.IsNullOrWhiteSpace(baseUrl) ? endpoint : (endpoint.StartsWith("http") ? endpoint : baseUrl + "/" + endpoint.TrimStart('/'));
                    await AppendRawLogAsync("SEND_STOCK_CARDS", fullUrl, json, response.StatusCode, responseContent);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to write raw Luca log entry");
                }

                // Koza sometimes returns HTTP 200 but includes an API-level "code" in the JSON body.
                // Treat non-zero code values as failures.
                try
                {
                    if (response.IsSuccessStatusCode)
                    {
                        try
                        {
                            var parsed = JsonSerializer.Deserialize<JsonElement>(responseContent);
                            if (parsed.ValueKind == JsonValueKind.Object && parsed.TryGetProperty("code", out var codeProp) && codeProp.ValueKind == JsonValueKind.Number)
                            {
                                var code = codeProp.GetInt32();
                                if (code != 0)
                                {
                                    result.IsSuccess = false;
                                    result.FailedRecords = stockCards.Count;
                                    result.Message = $"Luca API returned code {code}";
                                    result.Errors.Add(responseContent);
                                    _logger.LogError("Luca returned code {Code} when sending stock cards: {Body}", code, responseContent);
                                }
                                else
                                {
                                    result.IsSuccess = true;
                                    result.SuccessfulRecords = stockCards.Count;
                                    result.Message = "Stock cards sent successfully to Luca (Koza)";
                                    _logger.LogInformation("Successfully sent {Count} stock cards to Luca (Koza)", stockCards.Count);
                                }
                            }
                            else
                            {
                                // No code field — fallback to HTTP status
                                result.IsSuccess = true;
                                result.SuccessfulRecords = stockCards.Count;
                                result.Message = "Stock cards sent (no code returned)";
                                _logger.LogInformation("Sent {Count} stock cards to Luca (Koza) — no code returned", stockCards.Count);
                            }
                        }
                        catch (Exception ex)
                        {
                            // Parsing failed — fallback to HTTP status
                            _logger.LogWarning(ex, "Failed to parse Luca response JSON; using HTTP status to determine success");
                            result.IsSuccess = true;
                            result.SuccessfulRecords = stockCards.Count;
                            result.Message = "Stock cards sent (response parsing failed)";
                        }
                    }
                    else
                    {
                        result.IsSuccess = false;
                        result.FailedRecords = stockCards.Count;
                        result.Message = $"Failed to send stock cards to Luca (Koza): {response.StatusCode}";
                        result.Errors.Add(responseContent);
                        _logger.LogError("Failed to send stock cards to Luca (Koza). Status: {StatusCode}, Error: {Error}", response.StatusCode, responseContent);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error while handling Luca response");
                    result.IsSuccess = false;
                    result.FailedRecords = stockCards.Count;
                    result.Message = ex.Message;
                    result.Errors.Add(ex.ToString());
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
            result.FailedRecords = stockCards.Count;
            result.Message = ex.Message;
            result.Errors.Add(ex.ToString());

            _logger.LogError(ex, "Error sending stock cards to Luca (Koza)");
        }

        result.Duration = DateTime.UtcNow - startTime;
        return result;
    }

    public async Task<JsonElement> ListInvoicesAsync(LucaListInvoicesRequest request, bool detayliListe = false)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request ?? new LucaListInvoicesRequest(), _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var url = _settings.Endpoints.InvoiceList + (detayliListe ? "?detayliListe=true" : string.Empty);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };
        httpRequest.Headers.Add("No-Paging", "true");

        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> CreateInvoiceRawAsync(LucaCreateInvoiceHeaderRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.Invoices, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> CloseInvoiceAsync(LucaCloseInvoiceRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.InvoiceClose, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> DeleteInvoiceAsync(LucaDeleteInvoiceRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.InvoiceDelete, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> ListCustomerAddressesAsync(LucaListCustomerAddressesRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.CustomerAddresses)
        {
            Content = content
        };
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
        var content = new StringContent(json, Encoding.UTF8, "application/json");

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
        var content = new StringContent(json, Encoding.UTF8, "application/json");

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

    public async Task<JsonElement> GetCustomerRiskAsync(long finansalNesneId)
    {
        await EnsureAuthenticatedAsync();

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var url = $"{_settings.Endpoints.CustomerRisk}?gnlFinansalNesne.finansalNesneId={finansalNesneId}";
        var response = await client.GetAsync(url);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> CreateCustomerTransactionAsync(LucaCreateCariHareketRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.CustomerTransaction, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> ListDeliveryNotesAsync(bool detayliListe = false)
    {
        await EnsureAuthenticatedAsync();

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var url = _settings.Endpoints.IrsaliyeList + (detayliListe ? "?detayliListe=true" : string.Empty);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Add("No-Paging", "true");

        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> CreateDeliveryNoteAsync(LucaCreateIrsaliyeBaslikRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.IrsaliyeCreate, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> DeleteDeliveryNoteAsync(LucaDeleteIrsaliyeRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

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
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.CustomerCreate, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> CreateSupplierAsync(LucaCreateSupplierRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.SupplierCreate, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> CreateStockCardAsync(LucaCreateStokKartiRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.Products, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> CreateOtherStockMovementAsync(LucaCreateDshBaslikRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

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
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.SalesOrder, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> DeleteSalesOrderAsync(LucaDeleteSalesOrderRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

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
        var content = new StringContent(json, Encoding.UTF8, "application/json");

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
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.PurchaseOrder, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> DeletePurchaseOrderAsync(LucaDeletePurchaseOrderRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

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
        var content = new StringContent(json, Encoding.UTF8, "application/json");

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
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.WarehouseTransfer, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> CreateStockCountResultAsync(LucaCreateStockCountRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

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
        var content = new StringContent(json, Encoding.UTF8, "application/json");

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
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.CreditCardEntry, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
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

    // Luca → Katana (Pull) implementations
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
            await EnsureAuthenticatedAsync();

            var queryDate = fromDate?.ToString("yyyy-MM-dd") ?? DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
            var endpoint = $"{_settings.Endpoints.Customers}?fromDate={queryDate}";

            _logger.LogInformation("Fetching customers from Luca since {Date}", queryDate);

            var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
            var response = await client.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var customers = JsonSerializer.Deserialize<List<LucaCustomerDto>>(content, _jsonOptions) ?? new List<LucaCustomerDto>();

                _logger.LogInformation("Successfully fetched {Count} customers from Luca", customers.Count);
                return customers;
            }
            else
            {
                _logger.LogError("Failed to fetch customers from Luca. Status: {StatusCode}", response.StatusCode);
                return new List<LucaCustomerDto>();
            }
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

            // Koza / Luca stok kartları listeleme endpoint'ine POST ile boş filtre gönderebiliriz.
            var json = JsonSerializer.Serialize(new { }, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.StockCards)
            {
                Content = content
            };
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

                // Olası kapsayıcı alan adlarını kontrol et
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

                // Eğer root array döndüyse direkt deserialize et
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    return JsonSerializer.Deserialize<List<LucaProductDto>>(responseContent, _jsonOptions) ?? new List<LucaProductDto>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse products response from Luca; attempting generic deserialize");
            }

            // Fallback: doğrudan array olarak deserialize etmeye çalış
            return JsonSerializer.Deserialize<List<LucaProductDto>>(responseContent, _jsonOptions) ?? new List<LucaProductDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching products from Luca");
            return new List<LucaProductDto>();
        }
    }

    public async Task<List<LucaDespatchDto>> FetchDeliveryNotesAsync(DateTime? fromDate = null)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            _logger.LogInformation("Fetching delivery notes (irsaliye) from Luca");
            var element = await ListDeliveryNotesAsync(true);

            var results = new List<LucaDespatchDto>();

            // Try to locate array payload in common wrapper fields
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

                    // Lines
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
                            catch (Exception) { /* ignore line parse errors */ }
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

    private async Task AppendRawLogAsync(string tag, string url, string requestBody, System.Net.HttpStatusCode? status, string responseBody)
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

            // Also try to write to repository root ./logs when running the API from source (dotnet run),
            // so tests/scripts that expect ./logs/luca-raw.log can find it.
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
                // ignore
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to append raw Luca log");
        }
    }


}
