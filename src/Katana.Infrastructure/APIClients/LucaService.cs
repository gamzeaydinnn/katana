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
            var baseUri = new Uri($"{_settings.BaseUrl.TrimEnd('/')}/");
            _cookieHttpClient.BaseAddress = baseUri;
            _cookieHttpClient.DefaultRequestHeaders.Accept.Clear();
            _cookieHttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrWhiteSpace(_settings.ManualSessionCookie))
            {
                try
                {
                    _logger.LogInformation("Using manual Koza session cookie (bypassing login + branch selection)");

                    var cookieValue = _settings.ManualSessionCookie.Trim();
                    if (cookieValue.StartsWith("JSESSIONID=", StringComparison.OrdinalIgnoreCase))
                    {
                        cookieValue = cookieValue.Substring("JSESSIONID=".Length);
                    }
                    var cookie = new System.Net.Cookie("JSESSIONID", cookieValue)
                    {
                        Domain = baseUri.Host,
                        Path = "/"
                    };
                    _cookieContainer.Add(baseUri, cookie);

                    _logger.LogInformation("Added manual cookie JSESSIONID={Preview}", cookieValue.Substring(0, Math.Min(20, cookieValue.Length)) + "...");

                    // If a specific branch is required, attempt to switch before verification
                    if (_settings.ForcedBranchId.HasValue)
                    {
                        try
                        {
                            await ChangeBranchAsync(_settings.ForcedBranchId.Value);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "ForcedBranchId {BranchId} could not be applied with manual cookie", _settings.ForcedBranchId.Value);
                        }
                    }

                    var isValid = await VerifyManualSessionAsync();
                    if (!isValid)
                    {
                        _logger.LogError("Manual session cookie is expired/invalid; fetch a new JSESSIONID after logging in and selecting branch in Koza");
                        throw new UnauthorizedAccessException("Manual Koza session cookie invalid");
                    }

                    _isCookieAuthenticated = true;
                    _logger.LogInformation("Manual session verified; Koza requests will use browser cookie");
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to apply/verify manual Koza session cookie; falling back to login flow");
                }
            }

            // Koza login: try real HTML field names first, then alternates, then JSON
            var loginAttempts = new List<(string desc, HttpContent content)>
            {
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
                })),
                ("JSON:orgCode_userName_userPassword", new StringContent(
                    JsonSerializer.Serialize(new
                    {
                        orgCode = _settings.MemberNumber,
                        userName = _settings.Username,
                        userPassword = _settings.Password
                    }, _jsonOptions), Encoding.UTF8, "application/json"))
            };

            HttpResponseMessage? response = null;
            string authBody = string.Empty;
            bool loginOk = false;

            foreach (var (desc, payload) in loginAttempts)
            {
                try
                {
                    response = await _cookieHttpClient.PostAsync(_settings.Endpoints.Auth, payload);
                    authBody = await ReadResponseContentAsync(response);
                    var payloadText = payload switch
                    {
                        StringContent sc => await sc.ReadAsStringAsync(),
                        FormUrlEncodedContent fc => await fc.ReadAsStringAsync(),
                        _ => string.Empty
                    };
                    await AppendRawLogAsync($"AUTH_LOGIN:{desc}", _settings.Endpoints.Auth, payloadText, response.StatusCode, authBody);

                    if (response.IsSuccessStatusCode && IsKozaLoginSuccess(authBody))
                    {
                        loginOk = true;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Login attempt {Desc} failed", desc);
                }
            }

            if (!loginOk || response == null)
            {
                _logger.LogError("Koza login failed; last response: {Body}", authBody);
                throw new UnauthorizedAccessException("Koza login failed");
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Koza auth failed (status): {Status}", response.StatusCode);
                throw new UnauthorizedAccessException("Koza auth failed");
            }

            var branchId = await SelectDefaultBranchAsync(authBody);
            if (branchId.HasValue)
            {
                await ChangeBranchAsync(branchId.Value);
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

    private async Task<bool> VerifyManualSessionAsync()
    {
        try
        {
            _logger.LogDebug("Verifying manual Koza session cookie...");

            var testContent = new StringContent("{}", Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.MeasurementUnits)
            {
                Content = testContent
            };
            request.Headers.Add("No-Paging", "true");

            var response = await _cookieHttpClient!.SendAsync(request);
            var responseContent = await ReadResponseContentAsync(response);

            if (responseContent.Contains("Şirket Şube seçimi Yapılmalı", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("Manual cookie present but branch not selected in Koza session");
                return false;
            }

            if (responseContent.Contains("Giriş", StringComparison.OrdinalIgnoreCase) ||
                responseContent.Contains("<form", StringComparison.OrdinalIgnoreCase) ||
                responseContent.TrimStart().StartsWith("<html>", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("Manual cookie invalid/expired - login page returned");
                return false;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Manual cookie verification returned HTTP {Status}", response.StatusCode);
                return false;
            }

            try
            {
                using var doc = JsonDocument.Parse(responseContent);
                _logger.LogDebug("Manual cookie verification succeeded with JSON response");
                return true;
            }
            catch
            {
                _logger.LogWarning("Manual cookie verification HTTP OK but response not JSON; assuming valid");
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual session verification failed");
            return false;
        }
    }

    // Koza login responses vary (HTML or JSON). Detect success with best-effort heuristics.
    private bool IsKozaLoginSuccess(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return false;

        // Try JSON pattern: {"code":0} or {"success":true}
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("code", out var codeProp) && codeProp.ValueKind == JsonValueKind.Number)
                {
                    if (codeProp.GetInt32() == 0) return true;
                    // Explicit non-zero code means failure
                    return false;
                }
                if (root.TryGetProperty("success", out var successProp) && successProp.ValueKind == JsonValueKind.True)
                    return true;
            }
        }
        catch
        {
            // Not JSON, fall back to string heuristics
        }

        var text = body.ToLowerInvariant();
        // Known failure markers from Koza HTML responses
        string[] failTokens = { "hatal", "yanl", "gecersiz", "captcha", "deneme hakk", "beklenmedik", "error", "exception" };
        if (failTokens.Any(text.Contains))
            return false;

        // Success markers
        string[] successTokens = { "anasayfa", "menu", "redirect", "yetki", "hosgeldiniz", "giri\u015f ba\u015far\u0131l", "loginok" };
        if (successTokens.Any(text.Contains))
            return true;

        // If HTTP status was OK and body is non-empty but ambiguous, optimistically treat as success
        return true;
    }

    private async Task<long?> SelectDefaultBranchAsync(string? lastAuthBody = null)
    {
        try
        {
            if (_settings.ForcedBranchId.HasValue)
            {
                _logger.LogInformation("ForcedBranchId configured; using branch {BranchId}", _settings.ForcedBranchId.Value);
                return _settings.ForcedBranchId.Value;
            }

            var branchesResp = await _cookieHttpClient!.PostAsync(_settings.Endpoints.Branches, new StringContent("{}", Encoding.UTF8, "application/json"));
            if (!branchesResp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Branches request failed with status {Status}", branchesResp.StatusCode);
                return null;
            }

            var branchesJson = await ReadResponseContentAsync(branchesResp);
            await AppendRawLogAsync("AUTH_BRANCHES", _settings.Endpoints.Branches, "{}", branchesResp.StatusCode, branchesJson);
            using var doc = JsonDocument.Parse(branchesJson);
            var root = doc.RootElement;

            JsonElement? arrayEl = null;
            if (root.ValueKind == JsonValueKind.Array)
                arrayEl = root;
            else if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("list", out var l) && l.ValueKind == JsonValueKind.Array) arrayEl = l;
                else if (root.TryGetProperty("branches", out var b) && b.ValueKind == JsonValueKind.Array) arrayEl = b;
                else if (root.TryGetProperty("data", out var d) && d.ValueKind == JsonValueKind.Array) arrayEl = d;
            }

            if (!arrayEl.HasValue || arrayEl.Value.GetArrayLength() == 0) return null;

            // pick default if flagged, otherwise first
            var chosen = arrayEl.Value.EnumerateArray()
                .FirstOrDefault(el =>
                    (el.TryGetProperty("isDefault", out var isDef) && isDef.ValueKind == JsonValueKind.True) ||
                    (el.TryGetProperty("varsayilan", out var vs) && vs.ValueKind == JsonValueKind.True));
            if (chosen.ValueKind == JsonValueKind.Undefined)
            {
                chosen = arrayEl.Value.EnumerateArray().FirstOrDefault();
            }

            if (TryGetBranchId(chosen, out var id))
            {
                try
                {
                    Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "logs"));
                    await File.WriteAllTextAsync(Path.Combine("logs", "luca-branches.json"), arrayEl.Value.GetRawText());
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to write luca-branches.json to logs");
                }
                return id;
            }

            _logger.LogWarning("Could not determine branch id from Koza response");
            if (!string.IsNullOrWhiteSpace(branchesJson))
            {
                _logger.LogWarning("Branches raw (preview): {Branches}", branchesJson.Substring(0, Math.Min(500, branchesJson.Length)));
            }
            if (!string.IsNullOrWhiteSpace(lastAuthBody))
            {
                _logger.LogWarning("Last auth body (preview): {AuthBody}", lastAuthBody.Substring(0, Math.Min(300, lastAuthBody.Length)));
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to select Koza branch");
            return null;
        }
    }

    private bool TryGetBranchId(JsonElement element, out long id)
    {
        id = 0;
        if (element.ValueKind != JsonValueKind.Object) return false;

        string[] idFields = { "orgSirketSubeId", "orgSirketSubeID", "id", "subeId", "sirketSubeId", "orgSubeId" };
        foreach (var field in idFields)
        {
            if (element.TryGetProperty(field, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt64(out id)) return true;
                if (prop.ValueKind == JsonValueKind.String && long.TryParse(prop.GetString(), out id)) return true;
            }
        }
        return false;
    }

    private async Task ChangeBranchAsync(long branchId)
    {
        try
        {
            // Try several ChangeBranch payload formats and log responses for diagnosis.
            var attempts = new List<(string desc, HttpContent content)>();

            var jsonPayload = JsonSerializer.Serialize(new { orgSirketSubeId = branchId }, _jsonOptions);
            attempts.Add(("JSON:orgSirketSubeId", new StringContent(jsonPayload, Encoding.UTF8, "application/json")));

            // form-urlencoded fallback
            attempts.Add(("FORM:orgSirketSubeId", new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("orgSirketSubeId", branchId.ToString()) })));

            // try alternate field naming
            var jsonPayloadAlt = JsonSerializer.Serialize(new { orgSirketSubeID = branchId }, _jsonOptions);
            attempts.Add(("JSON:orgSirketSubeID", new StringContent(jsonPayloadAlt, Encoding.UTF8, "application/json")));
            attempts.Add(("FORM:orgSirketSubeID", new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("orgSirketSubeID", branchId.ToString()) })));

            attempts.Add(("JSON:id", new StringContent(JsonSerializer.Serialize(new { id = branchId }, _jsonOptions), Encoding.UTF8, "application/json")));

            foreach (var (desc, content) in attempts)
            {
                try
                {
                    var resp = await _cookieHttpClient!.PostAsync(_settings.Endpoints.ChangeBranch, content);
                    var body = await ReadResponseContentAsync(resp);
                    var payloadText = content switch
                    {
                        StringContent sc => await sc.ReadAsStringAsync(),
                        FormUrlEncodedContent fc => await fc.ReadAsStringAsync(),
                        _ => string.Empty
                    };
                    await AppendRawLogAsync("CHANGE_BRANCH:" + desc, _settings.Endpoints.ChangeBranch, payloadText, resp.StatusCode, body);

                    if (resp.IsSuccessStatusCode)
                    {
                        // If Koza returns a JSON 'code' field, ensure it's 0 (success)
                        try
                        {
                            using var doc = JsonDocument.Parse(body);
                            if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("code", out var codeProp) && codeProp.ValueKind == JsonValueKind.Number)
                            {
                                if (codeProp.GetInt32() == 0)
                                {
                                    _logger.LogInformation("ChangeBranch succeeded using {Desc}", desc);
                                    return;
                                }
                                else
                                {
                                    _logger.LogWarning("ChangeBranch attempt {Desc} returned code {Code}", desc, codeProp.GetInt32());
                                }
                            }
                            else
                            {
                                _logger.LogInformation("ChangeBranch succeeded (HTTP OK) using {Desc}", desc);
                                return;
                            }
                        }
                        catch (Exception)
                        {
                            _logger.LogInformation("ChangeBranch HTTP OK with unparseable body using {Desc}", desc);
                            return;
                        }
                    }
                    else
                    {
                        _logger.LogWarning("ChangeBranch attempt {Desc} failed with status {Status}", desc, resp.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "ChangeBranch attempt {Desc} threw", desc);
                }
            }

            _logger.LogWarning("All ChangeBranch attempts finished without success");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ChangeBranch call failed");
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

            var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
            // Koza (UseTokenAuth=false) tarafında EkleStkWsHareket.do Action'ı yok; diğer stok hareketi endpoint'ini kullan.
            if (_settings.UseTokenAuth)
            {
                var json = JsonSerializer.Serialize(stockMovements, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
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
                        : movement.ProductCode;
                    try
                    {
                        var payload = JsonSerializer.Serialize(movement, _jsonOptions);
                        var response = await client.PostAsync(_settings.Endpoints.OtherStockMovement, new StringContent(payload, Encoding.UTF8, "application/json"));
                        var body = await ReadResponseContentAsync(response);
                        await AppendRawLogAsync("SEND_STOCK_MOVEMENT", _settings.Endpoints.OtherStockMovement, payload, response.StatusCode, body);

                        if (!response.IsSuccessStatusCode)
                        {
                            failed++;
                            result.Errors.Add($"{movementLabel}: HTTP {response.StatusCode} - {body}");
                            _logger.LogError("Stock movement {Doc} failed HTTP {Status}: {Body}", movementLabel, response.StatusCode, body);
                            continue;
                        }

                        // Koza genelde code=0 dönüyor
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

                    await Task.Delay(100);
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

    public async Task<long> CreateIrsaliyeAsync(LucaIrsaliyeDto dto)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(dto, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;

        var response = await client.PostAsync(_settings.Endpoints.IrsaliyeCreate, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("CreateIrsaliyeAsync failed with status {Status}: {Body}", response.StatusCode, responseContent);
        }

        return TryParseId(responseContent);
    }

    public async Task DeleteIrsaliyeAsync(long irsaliyeId)
    {
        await EnsureAuthenticatedAsync();
        var payload = new LucaDeleteIrsaliyeRequest { SsIrsaliyeBaslikId = irsaliyeId };
        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        await client.PostAsync(_settings.Endpoints.IrsaliyeDelete, new StringContent(json, Encoding.UTF8, "application/json"));
    }

    public async Task<long> CreateSatinalmaSiparisAsync(LucaSatinalmaSiparisDto dto)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(dto, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
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
        await client.PostAsync(_settings.Endpoints.PurchaseOrderDelete, new StringContent(json, Encoding.UTF8, "application/json"));
    }

    public async Task<long> CreateDepoTransferAsync(LucaDepoTransferDto dto)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(dto, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
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
        var content = new StringContent(json, Encoding.UTF8, "application/json");
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
        var response = await client.PostAsync(_settings.Endpoints.CustomerTransaction, new StringContent(json, Encoding.UTF8, "application/json"));
        var responseContent = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("CreateCariHareketAsync failed with status {Status}: {Body}", response.StatusCode, responseContent);
        }
        return TryParseId(responseContent);
    }

    public async Task<long> CreateFaturaKapamaAsync(LucaFaturaKapamaDto dto)
    {
        await EnsureAuthenticatedAsync();
        var json = JsonSerializer.Serialize(dto, _jsonOptions);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.InvoiceClose, new StringContent(json, Encoding.UTF8, "application/json"));
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
        var successCount = 0;
        var failedCount = 0;

        try
        {
            await EnsureAuthenticatedAsync();
            _logger.LogInformation("Sending {Count} stock cards to Luca (Koza) one by one (Koza does not accept arrays)", stockCards.Count);

            var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
            var endpoint = _settings.UseTokenAuth ? _settings.Endpoints.Products : _settings.Endpoints.StockCardCreate;
            var enc1254 = Encoding.GetEncoding(1254);

            foreach (var card in stockCards)
            {
                try
                {
                    var payload = JsonSerializer.Serialize(card, _jsonOptions);
                    var content = new ByteArrayContent(enc1254.GetBytes(payload));
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
                    {
                        CharSet = "windows-1254"
                    };

                    using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
                    {
                        Content = content
                    };

                    var response = await client.SendAsync(httpRequest);
                    var responseBytes = await response.Content.ReadAsByteArrayAsync();
                    string responseContent;
                    try { responseContent = enc1254.GetString(responseBytes); } catch { responseContent = Encoding.UTF8.GetString(responseBytes); }

                    var baseUrl = client.BaseAddress?.ToString()?.TrimEnd('/') ?? _settings.BaseUrl?.TrimEnd('/') ?? string.Empty;
                    var fullUrl = string.IsNullOrWhiteSpace(baseUrl) ? endpoint : (endpoint.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? endpoint : baseUrl + "/" + endpoint.TrimStart('/'));
                    await AppendRawLogAsync("SEND_STOCK_CARD", fullUrl, payload, response.StatusCode, responseContent);

                    if (responseContent.TrimStart().StartsWith("<", StringComparison.OrdinalIgnoreCase))
                    {
                        failedCount++;
                        var htmlPreview = responseContent.Length > 200 ? responseContent.Substring(0, 200) : responseContent;
                        result.Errors.Add($"{card.KartKodu}: HTML response (likely session/captcha/branch issue): {htmlPreview}");
                        _logger.LogError("Stock card {Card} returned HTML instead of JSON. Session likely expired/branch not selected.", card.KartKodu);
                        continue;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        failedCount++;
                        var previewError = responseContent.Length > 300 ? responseContent.Substring(0, 300) + "...(truncated)" : responseContent;
                        result.Errors.Add($"{card.KartKodu}: HTTP {response.StatusCode} - {previewError}");
                        _logger.LogError("Stock card {Card} failed HTTP {Status}: {Body}", card.KartKodu, response.StatusCode, previewError);
                        continue;
                    }

                    try
                    {
                        var parsed = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        if (parsed.ValueKind == JsonValueKind.Object && parsed.TryGetProperty("code", out var codeProp))
                        {
                            var code = codeProp.GetInt32();
                            if (code == 0)
                            {
                                if (parsed.TryGetProperty("stkSkart", out var skartEl) && skartEl.ValueKind == JsonValueKind.Object && skartEl.TryGetProperty("skartId", out var idEl))
                                {
                                    _logger.LogInformation("Stock card {Card} created with ID {Id}", card.KartKodu, idEl.ToString());
                                }
                            }
                            else if (code == 1003)
                            {
                                _logger.LogError("Stock card {Card} failed with code 1003 (branch selection required / session expired). Stopping.", card.KartKodu);
                                throw new UnauthorizedAccessException("Session expired or branch not selected (code 1003). Renew manual session cookie.");
                            }
                            else
                            {
                                failedCount++;
                                var msg = parsed.TryGetProperty("message", out var m) ? m.GetString() : "Unknown error";
                                result.Errors.Add($"{card.KartKodu}: code={code} message={msg}");
                                _logger.LogError("Stock card {Card} failed with code {Code} message {Message}", card.KartKodu, code, msg);
                                continue;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Stock card {Card} response could not be parsed; assuming success on HTTP OK", card.KartKodu);
                    }

                    successCount++;
                    _logger.LogInformation("Stock card created: {Card}", card.KartKodu);
                }
                catch (Exception ex)
                {
                    failedCount++;
                    result.Errors.Add($"{card.KartKodu}: {ex.Message}");
                    _logger.LogError(ex, "Error sending stock card {Card}", card.KartKodu);
                }

                await Task.Delay(200);
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
        result.IsSuccess = successCount > 0 && failedCount == 0;
        result.Message = $"{successCount} success, {failedCount} failed";
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
        var endpoint = _settings.UseTokenAuth ? _settings.Endpoints.Products : _settings.Endpoints.StockCardCreate;
        var response = await client.PostAsync(endpoint, content);
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

    private async Task<string> ReadResponseContentAsync(HttpResponseMessage response)
    {
        var charset = response.Content.Headers.ContentType?.CharSet?.Trim().ToLowerInvariant();
        var bytes = await response.Content.ReadAsByteArrayAsync();

        // Prefer explicit charset if provided; Koza sometimes uses windows-1254/iso-8859-9
        if (!string.IsNullOrWhiteSpace(charset))
        {
            if (charset.Contains("1254") || charset.Contains("iso-8859-9"))
            {
                try { return Encoding.GetEncoding(1254).GetString(bytes); } catch { /* fall back below */ }
            }
            if (charset.Contains("utf-8"))
            {
                try { return Encoding.UTF8.GetString(bytes); } catch { /* fall back */ }
            }
        }

        // Fallback: try UTF-8, then cp1254
        try { return Encoding.UTF8.GetString(bytes); } catch { /* ignore */ }
        try { return Encoding.GetEncoding(1254).GetString(bytes); } catch { /* ignore */ }
        return string.Empty;
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
            // ignore parse errors
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

}
