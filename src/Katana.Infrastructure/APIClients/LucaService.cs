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

public partial class LucaService : ILucaService
{
    private readonly Katana.Core.Interfaces.ILucaCookieJarStore? _externalCookieJarStore;
    private readonly HttpClient _httpClient;
    private readonly LucaApiSettings _settings;
    private readonly ILogger<LucaService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly Encoding _encoding;
    private string? _sessionCookie;
    private string? _manualJSessionId;
    private string? _authToken;
    private DateTime? _tokenExpiry;
    private DateTime? _cookieExpiresAt;
    private static readonly SemaphoreSlim _branchSemaphore = new SemaphoreSlim(1, 1);
    private System.Net.CookieContainer? _cookieContainer;
    private HttpClientHandler? _cookieHandler;
    private HttpClient? _cookieHttpClient;
    private bool _isCookieAuthenticated = false;
    private DateTime? _lastSuccessfulAuthAt = null;
    private static readonly System.Threading.SemaphoreSlim _loginSemaphore = new System.Threading.SemaphoreSlim(1, 1);
    private static readonly Dictionary<long, (int ExpectedCariTur, string ErrorMessage)> FaturaKapamaCariRules =
        new()
        {
            
            { 49,  (5, "Tahsilat makbuzu için sadece Kasa Kartı kullanılabilir (cariTur=5)") },
            { 63,  (5, "Tediye makbuzu için sadece Kasa Kartı kullanılabilir (cariTur=5)") },
            { 64,  (3, "Gelen havale için sadece Banka Kartı kullanılabilir (cariTur=3)") },
            { 65,  (3, "Gönderilen havale için Banka Kartı kullanılabilir (cariTur=3)") },
            { 68,  (3, "Virman işlemleri için Banka Kartı kullanılabilir (cariTur=3)") },
            { 66,  (1, "Alacak dekontu için Cari Kart kullanılmalıdır (cariTur=1)") },
            { 67,  (1, "Borç dekontu için Cari Kart kullanılmalıdır (cariTur=1)") },

            
            { 127, (5, "Kredi kartı girişi için sadece Kasa Kartı kullanılabilir (cariTur=5)") }
        };
    public LucaService(HttpClient httpClient, IOptions<LucaApiSettings> settings, ILogger<LucaService> logger, Katana.Core.Interfaces.ILucaCookieJarStore? cookieJarStore = null)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
        _externalCookieJarStore = cookieJarStore;
        _encoding = InitializeEncoding(_settings.Encoding);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
        try
        {
            _cookieRefreshCts = new CancellationTokenSource();
            var cts = _cookieRefreshCts;
            Task.Run(async () =>
            {
                var token = cts.Token;
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        _logger.LogDebug("Cookie refresh: ensuring authentication and branch selection");
                        await EnsureAuthenticatedAsync();
                        await EnsureBranchSelectedAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Cookie refresh encountered an error");
                    }

                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(25), token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }, cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to start cookie refresh loop");
        }
    }
    private string UrlEncodeCp1254(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        var encoding = Encoding.GetEncoding(1254);
        var bytes = encoding.GetBytes(value);
        var sb = new StringBuilder();

        foreach (var b in bytes)
        {
            if ((b >= 48 && b <= 57) || (b >= 65 && b <= 90) || (b >= 97 && b <= 122) || b == 45 || b == 46 || b == 95 || b == 126)
            {
                sb.Append((char)b);
            }
            else if (b == 32)
            {
                sb.Append('+');
            }
            else
            {
                sb.Append('%');
                sb.Append(b.ToString("X2"));
            }
        }

        return sb.ToString();
    }
    private System.Threading.CancellationTokenSource? _cookieRefreshCts;
    private static Encoding InitializeEncoding(string? encodingName)
    {
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
        catch (Exception)
        {
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(encodingName))
            {
                return Encoding.GetEncoding(encodingName);
            }
        }
        catch (Exception)
        {
        }

        return Encoding.GetEncoding(1254);
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

        await EnsureSessionAsync();
    }
    private string? TryGetJSessionFromContainer()
    {
        try
        {
            if (_cookieContainer != null && !string.IsNullOrWhiteSpace(_settings.BaseUrl))
            {
                var baseUri = new Uri(_settings.BaseUrl.TrimEnd('/') + "/");
                var cookies = _cookieContainer.GetCookies(baseUri);
                var c = cookies.Cast<System.Net.Cookie>().FirstOrDefault(x => string.Equals(x.Name, "JSESSIONID", StringComparison.OrdinalIgnoreCase));
                if (c != null && !string.IsNullOrWhiteSpace(c.Value)) return c.Value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to read JSESSIONID from CookieContainer");
        }
        return null;
    }
    private void ApplySessionCookie(HttpRequestMessage req)
    {
        try
        {
            if (!req.Headers.Contains("Cookie"))
            {
                var js = TryGetJSessionFromContainer();
                if (!string.IsNullOrWhiteSpace(js))
                {
                    var full = js.StartsWith("JSESSIONID=", StringComparison.OrdinalIgnoreCase) ? js : "JSESSIONID=" + js;
                    req.Headers.TryAddWithoutValidation("Cookie", full);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to apply session cookie to request");
        }
    }
    private async Task VerifyBranchSelectionAsync()
    {
        try
        {
            var resp = await (_cookieHttpClient ?? _httpClient).PostAsync(_settings.Endpoints.Branches, CreateKozaContent("{}"));
            var content = await resp.Content.ReadAsStringAsync();
            await AppendRawLogAsync("BRANCH_VERIFY", _settings.Endpoints.Branches, "{}", resp.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Branch verification call failed; proceeding with current session.");
        }
    }
    private async Task AuthenticateWithCookieAsync()
    {
        if (!string.IsNullOrWhiteSpace(_settings.ManualSessionCookie))
        {
            try
            {
                _logger.LogInformation("Using MANUAL session cookie for authentication (configured). Normalizing and applying to HttpClient.");

                var cookieValue = _settings.ManualSessionCookie.Trim();
                if (cookieValue.StartsWith("JSESSIONID=", StringComparison.OrdinalIgnoreCase))
                {
                    cookieValue = cookieValue.Substring("JSESSIONID=".Length);
                }

                try
                {
                    _cookieHttpClient?.Dispose();
                }
                catch { }

                _cookieContainer = null;
                _cookieHandler = null;

                var baseUri = new Uri(_settings.BaseUrl.TrimEnd('/') + "/");
                var handler = new HttpClientHandler
                {
                    UseCookies = false,
                    AllowAutoRedirect = true,
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };

                _cookieHttpClient = new HttpClient(handler)
                {
                    BaseAddress = baseUri
                };

                _cookieHttpClient.DefaultRequestHeaders.Accept.Clear();
                _cookieHttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var fullCookie = cookieValue.StartsWith("JSESSIONID=", StringComparison.OrdinalIgnoreCase)
                    ? cookieValue
                    : "JSESSIONID=" + cookieValue;

                _cookieHttpClient.DefaultRequestHeaders.Remove("Cookie");
                _cookieHttpClient.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", fullCookie);

                _logger.LogInformation("Manual session cookie applied to HttpClient (masked): {CookiePreview}", fullCookie.Length > 40 ? fullCookie.Substring(0, 40) + "..." : fullCookie);

                // Try selecting branch using the manual cookie
                var manualOk = await SelectBranchWithManualCookieAsync();
                if (manualOk)
                {
                    _isCookieAuthenticated = true;
                    _lastSuccessfulAuthAt = DateTime.UtcNow;
                    _logger.LogInformation("=== Koza Authentication Complete (ManualCookie) ===");
                    return;
                }

                _logger.LogWarning("Manual-cookie branch selection did not succeed; falling back to perform full login flow");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exception while applying ManualSessionCookie; falling back to login flow");
            }
        }
        await LoginWithServiceAsync();
    }

    private async Task EnsureSessionAsync()
    {
        const int maxRetries = 3;
        var delays = new[] { 2000, 4000, 6000 }; // Exponential backoff: 2s, 4s, 6s
        
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                if (_isCookieAuthenticated && !string.IsNullOrWhiteSpace(_sessionCookie))
                {
                    if (!_cookieExpiresAt.HasValue || DateTime.UtcNow < _cookieExpiresAt.Value)
                    {
                        return;
                    }
                    _logger.LogWarning("Cookie expired or about to expire, re-authenticating (Attempt {Attempt}/{MaxRetries})", attempt + 1, maxRetries);
                }

                await LoginWithServiceAsync();
                
                // Başarılı login sonrası kontrol
                if (_isCookieAuthenticated && !string.IsNullOrWhiteSpace(_sessionCookie))
                {
                    _logger.LogInformation("Session başarıyla oluşturuldu (Attempt {Attempt})", attempt + 1);
                    return;
                }
                
                _logger.LogWarning("Login başarısız - session oluşturulamadı (Attempt {Attempt}/{MaxRetries})", attempt + 1, maxRetries);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Session oluşturma hatası (Attempt {Attempt}/{MaxRetries})", attempt + 1, maxRetries);
            }
            
            // Son deneme değilse bekle
            if (attempt < maxRetries - 1)
            {
                var delay = delays[attempt];
                _logger.LogInformation("Yeniden deneme öncesi {Delay}ms bekleniyor...", delay);
                await Task.Delay(delay);
            }
        }
        
        _logger.LogError("Session oluşturulamadı - {MaxRetries} deneme başarısız", maxRetries);
        throw new InvalidOperationException($"Koza session oluşturulamadı - {maxRetries} deneme başarısız oldu");
    }

    private async Task LoginWithServiceAsync()
    {
        // Fast, non-blocking guard to avoid queuing many callers when a valid session exists
        if (_isCookieAuthenticated && !string.IsNullOrWhiteSpace(_sessionCookie) && (!_cookieExpiresAt.HasValue || DateTime.UtcNow < _cookieExpiresAt.Value))
        {
            _logger.LogDebug("Existing Koza session is valid, skipping login (fast-path)");
            return;
        }

        // Short cooldown to prevent rapid repeated auth attempts from multiple concurrent callers
        if (_lastSuccessfulAuthAt.HasValue && (DateTime.UtcNow - _lastSuccessfulAuthAt.Value) < TimeSpan.FromSeconds(10))
        {
            _logger.LogDebug("Recent Koza authentication succeeded at {Time}; skipping redundant login", _lastSuccessfulAuthAt.Value);
            return;
        }

        await _loginSemaphore.WaitAsync();
        try
        {
            // Re-check after acquiring semaphore to avoid races
            if (_isCookieAuthenticated && !string.IsNullOrWhiteSpace(_sessionCookie) && (!_cookieExpiresAt.HasValue || DateTime.UtcNow < _cookieExpiresAt.Value))
            {
                _logger.LogDebug("Existing Koza session is valid, skipping login");
                return;
            }
            _logger.LogInformation("=== Starting Koza Authentication (guarded) ===");
            var baseUri = new Uri($"{_settings.BaseUrl.TrimEnd('/')}/");

            if (_settings.UseHeadlessAuth)
            {
                try
                {
                    var sessionManager = new LucaSessionManager(_settings, _logger);
                    var jsession = await sessionManager.GetFreshCookieAsync();
                    if (!string.IsNullOrWhiteSpace(jsession))
                    {
                        _sessionCookie = "JSESSIONID=" + jsession;
                        _cookieExpiresAt = DateTime.UtcNow.AddMinutes(20);

                        
                        if (_cookieHttpClient == null)
                        {
                            _cookieContainer = new System.Net.CookieContainer();
                            _cookieHandler = new HttpClientHandler
                            {
                                CookieContainer = _cookieContainer,
                                UseCookies = true,
                                AllowAutoRedirect = true
                            };
                            _cookieHttpClient = new HttpClient(_cookieHandler)
                            {
                                BaseAddress = baseUri
                            };
                        }
                        _cookieHttpClient.DefaultRequestHeaders.Accept.Clear();
                        _cookieHttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                        try
                        {
                            if (_cookieContainer != null)
                            {
                                var cookie = new System.Net.Cookie("JSESSIONID", jsession, "/", baseUri.Host);
                                _cookieContainer.Add(baseUri, cookie);
                                _logger.LogDebug("Headless auth: JSESSIONID inserted into CookieContainer for host {Host}", baseUri.Host);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Headless auth: failed to insert JSESSIONID into CookieContainer");
                        }
                        _isCookieAuthenticated = true;
                        await EnsureBranchSelectedAsync();
                        _lastSuccessfulAuthAt = DateTime.UtcNow;
                        _logger.LogInformation("=== Koza Authentication Complete (Headless) ===");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Headless login failed; falling back to WS login");
                }
            }
            _logger.LogInformation("Falling back to login flow via PerformLoginAsync (script-compatible)");

            if (_cookieHttpClient == null)
            {
                _cookieContainer = new System.Net.CookieContainer();
                _cookieHandler = new HttpClientHandler
                {
                    CookieContainer = _cookieContainer,
                    UseCookies = true,
                    AllowAutoRedirect = true
                };
                _cookieHttpClient = new HttpClient(_cookieHandler)
                {
                    BaseAddress = baseUri
                };
            }

            _cookieHttpClient.DefaultRequestHeaders.Accept.Clear();
            _cookieHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
            );
            var loginOk = await PerformLoginAsync();
            if (!loginOk)
            {
                _logger.LogError("PerformLoginAsync reports login failure (no usable session established)");
                throw new UnauthorizedAccessException("Login did not succeed via PerformLoginAsync");
            }

            string? jsessionFromContainer = null;
            try
            {
                if (_cookieContainer != null)
                {
                    var cookies = _cookieContainer.GetCookies(baseUri);
                    var c = cookies.Cast<System.Net.Cookie>().FirstOrDefault(x => string.Equals(x.Name, "JSESSIONID", StringComparison.OrdinalIgnoreCase));
                    if (c != null)
                    {
                        jsessionFromContainer = c.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read JSESSIONID from CookieContainer after PerformLoginAsync");
            }

            if (!string.IsNullOrWhiteSpace(jsessionFromContainer))
            {
                _sessionCookie = "JSESSIONID=" + jsessionFromContainer;
                _cookieExpiresAt = DateTime.UtcNow.AddMinutes(20);
                _isCookieAuthenticated = true;
                _logger.LogInformation("Session cookie acquired from CookieContainer (PerformLoginAsync)");
                try
                {
                    if (_cookieContainer != null && !string.IsNullOrWhiteSpace(_settings.BaseUrl))
                    {
                        try
                        {
                                var baseUriCookie = new Uri(_settings.BaseUrl.TrimEnd('/') + "/");
                                var host = baseUriCookie.Host;
                                var cookieVal = jsessionFromContainer;
                                var cookie = new System.Net.Cookie("JSESSIONID", cookieVal, "/", host);
                                try
                                {
                                    _cookieContainer.Add(baseUriCookie, cookie);
                                }
                                catch
                                {
                                    _cookieContainer.Add(new Uri(baseUriCookie.GetLeftPart(UriPartial.Authority)), cookie);
                                }
                            _logger.LogDebug("Inserted JSESSIONID into CookieContainer for host {Host}", host);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Could not add JSESSIONID to CookieContainer with explicit domain/path");
                        }
                    }
                }
                catch (Exception) { }
            }
            else
            {
                _logger.LogWarning("PerformLoginAsync succeeded but no JSESSIONID found in CookieContainer; proceeding but session may not be valid");
            }

            await EnsureBranchSelectedAsync();
            _lastSuccessfulAuthAt = DateTime.UtcNow;
            _logger.LogInformation("=== Koza Authentication Complete (WS/PerformLogin) ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "!!! Koza authentication failed !!!");
            _isCookieAuthenticated = false;
            throw;
        }
        finally
        {
            _loginSemaphore.Release();
        }
    }
    private async Task<bool> SelectBranchWithManualCookieAsync()
    {
        try
        {
            _logger.LogInformation("Calling YdlUserResponsibilityOrgSs.do to get branch list...");

            var branchesUrl = _settings.Endpoints.Branches;
            var emptyBody = CreateKozaContent("{}");

            try
            {
                var headersPreview = string.Empty;
                if (_cookieHttpClient != null)
                {
                    headersPreview = string.Join("; ", _cookieHttpClient.DefaultRequestHeaders.Select(h => h.Key + "=" + string.Join(',', h.Value)));
                }
                _logger.LogDebug("_cookieHttpClient.DefaultRequestHeaders: {Headers}", headersPreview);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to read DefaultRequestHeaders for debug");
            }

            using var branchesRequest = new HttpRequestMessage(HttpMethod.Post, branchesUrl)
            {
                Content = emptyBody
            };
            ApplySessionCookie(branchesRequest);
            ApplyManualSessionCookie(branchesRequest);

            var branchesResponse = await _cookieHttpClient!.SendAsync(branchesRequest);
            var branchesContent = await branchesResponse.Content.ReadAsStringAsync();

            _logger.LogDebug("Branches response status: {Status}", branchesResponse.StatusCode);
            _logger.LogDebug("Branches response body length: {Len}", branchesContent?.Length ?? 0);
            _logger.LogTrace("Branches response body: {Body}", branchesContent);
                try { await SaveHttpTrafficAsync("BRANCHES", null, branchesResponse); } catch (Exception) {  }

            if (!branchesResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get branches. Status: {Status}", branchesResponse.StatusCode);
                return false;
            }

            using var doc = JsonDocument.Parse(branchesContent);

            JsonElement arrayEl = default;

            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                arrayEl = doc.RootElement;
            }
            else if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                string[] wrappers = { "list", "data", "branches", "items", "sirketSubeList" };

                foreach (var wrapper in wrappers)
                {
                    if (doc.RootElement.TryGetProperty(wrapper, out var prop) &&
                        prop.ValueKind == JsonValueKind.Array)
                    {
                        arrayEl = prop;
                        _logger.LogDebug("Found branches array in '{Wrapper}' property", wrapper);
                        break;
                    }
                }
            }
            if (arrayEl.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("Could not find branches array in response");
                return false;
            }

            var branchCount = arrayEl.GetArrayLength();
            _logger.LogInformation("Found {Count} branches", branchCount);

            if (branchCount == 0)
            {
                _logger.LogWarning("No branches available");
                return false;
            }

            await SaveBranchesDebugInfoAsync(arrayEl);

            long? selectedBranchId = null;
            var preferredBranch = _settings.ForcedBranchId ?? _settings.DefaultBranchId;

            foreach (var branch in arrayEl.EnumerateArray())
            {
                if (branch.ValueKind != JsonValueKind.Object) continue;

                if (TryExtractBranchId(branch, out var branchId))
                {
                    var branchName = TryGetProperty(branch, "tanim", "name", "ad");
                    _logger.LogDebug("Branch: id={Id}, name={Name}", branchId, branchName ?? "(unnamed)");
                    
                    if (preferredBranch.HasValue && preferredBranch.Value == branchId)
                    {
                        selectedBranchId = branchId;
                        _logger.LogInformation("Selected forced branch from config: {Id} - {Name}", branchId, branchName ?? "(unnamed)");
                        break; 
                    }

                    if (!selectedBranchId.HasValue)
                    {
                        selectedBranchId = branchId;
                        _logger.LogInformation("Selected first branch: {Id} - {Name}", branchId, branchName ?? "(unnamed)");
                    }
                }
            }
            if (!selectedBranchId.HasValue)
            {
                _logger.LogError("Could not extract orgSirketSubeId from any branch");
                return false;
            }

            _logger.LogInformation("Calling GuncelleYtkSirketSubeDegistir.do with orgSirketSubeId={BranchId}", selectedBranchId.Value);

            var changeBranchUrl = _settings.Endpoints.ChangeBranch;
            var changeBranchPayload = new { orgSirketSubeId = selectedBranchId.Value };
            var changeBranchJson = JsonSerializer.Serialize(changeBranchPayload, _jsonOptions);
            var changeBranchContent = CreateKozaContent(changeBranchJson);
            _logger.LogDebug("ChangeBranch request: {Payload}", changeBranchJson);
            var changeBranchResponse = await _cookieHttpClient.PostAsync(changeBranchUrl, changeBranchContent);
            var changeBranchBody = await changeBranchResponse.Content.ReadAsStringAsync();
            _logger.LogDebug("ChangeBranch response status: {Status}", changeBranchResponse.StatusCode);
            _logger.LogDebug("ChangeBranch response body: {Body}", changeBranchBody);

                    if (!changeBranchResponse.IsSuccessStatusCode)
                    {
                        _logger.LogError("Failed to change branch. Status: {Status}", changeBranchResponse.StatusCode);
                        return false;
                    }

                    var lowerBody = (changeBranchBody ?? string.Empty).ToLowerInvariant();
                    var indicatesLogin = lowerBody.Contains("login olunmalı") || lowerBody.Contains("login olunmali") || lowerBody.Contains("1001") || lowerBody.Contains("1002") || lowerBody.Contains("1003");
                    if (indicatesLogin)
                    {
                        _logger.LogWarning("ChangeBranch response indicates not-authenticated/branch issue (detected in body). Trying manual-cookie retry before failing.");

                        try
                        {
                            string? jsession = null;
                            try
                            {
                                if (_cookieContainer != null && !string.IsNullOrWhiteSpace(_settings.BaseUrl))
                                {
                                    var baseUri = new Uri(_settings.BaseUrl.TrimEnd('/') + "/");
                                    var cookies = _cookieContainer.GetCookies(baseUri);
                                    var c = cookies.Cast<System.Net.Cookie>().FirstOrDefault(x => string.Equals(x.Name, "JSESSIONID", StringComparison.OrdinalIgnoreCase));
                                    if (c != null && !string.IsNullOrWhiteSpace(c.Value)) jsession = c.Value;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogDebug(ex, "Failed to read JSESSIONID from CookieContainer for manual retry");
                            }
                            if (jsession == null)
                            {
                                try
                                {
                                    if (changeBranchResponse.Headers.TryGetValues("Set-Cookie", out var scs))
                                    {
                                        foreach (var sc in scs)
                                        {
                                            if (sc != null && sc.IndexOf("JSESSIONID=", StringComparison.OrdinalIgnoreCase) >= 0)
                                            {
                                                var parts = sc.Split(';', StringSplitOptions.RemoveEmptyEntries);
                                                var kv = parts.Select(p => p.Trim()).FirstOrDefault(p => p.StartsWith("JSESSIONID=", StringComparison.OrdinalIgnoreCase));
                                                if (!string.IsNullOrWhiteSpace(kv))
                                                {
                                                    jsession = kv.Substring("JSESSIONID=".Length);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogDebug(ex, "Failed to read Set-Cookie headers for JSESSIONID");
                                }
                            }
                            if (!string.IsNullOrWhiteSpace(jsession))
                            {
                                var manualCookieValue = jsession.StartsWith("JSESSIONID=", StringComparison.OrdinalIgnoreCase) ? jsession : "JSESSIONID=" + jsession;
                                _logger.LogInformation("Attempting manual-cookie ChangeBranch retry using JSESSIONID (masked): {Preview}", manualCookieValue.Length > 40 ? manualCookieValue.Substring(0, 40) + "..." : manualCookieValue);

                                try { _cookieHttpClient?.Dispose(); } catch { }

                                var handler = new HttpClientHandler
                                {
                                    UseCookies = false,
                                    AllowAutoRedirect = true,
                                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                                };
                                var manualClient = new HttpClient(handler)
                                {
                                    BaseAddress = new Uri(_settings.BaseUrl.TrimEnd('/') + "/")
                                };
                                manualClient.DefaultRequestHeaders.Accept.Clear();
                                manualClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                                manualClient.DefaultRequestHeaders.Remove("Cookie");
                                manualClient.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", manualCookieValue);
                                try
                                {
                                    manualClient.DefaultRequestHeaders.Remove("Referer");
                                    manualClient.DefaultRequestHeaders.TryAddWithoutValidation("Referer", _settings.BaseUrl?.TrimEnd('/') + "/");
                                    manualClient.DefaultRequestHeaders.Remove("Origin");
                                    manualClient.DefaultRequestHeaders.TryAddWithoutValidation("Origin", _settings.BaseUrl?.TrimEnd('/') + "/");
                                    manualClient.DefaultRequestHeaders.Remove("X-Requested-With");
                                    manualClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
                                    manualClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; KatanaClient/1.0)");
                                }
                                catch (Exception) { }
                                using var retryReq = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.ChangeBranch)
                                {
                                    Content = CreateKozaContent(changeBranchJson)
                                };
                                try
                                {
                                    retryReq.Headers.Remove("Referer");
                                    retryReq.Headers.TryAddWithoutValidation("Referer", _settings.BaseUrl?.TrimEnd('/') + "/");
                                    retryReq.Headers.Remove("Origin");
                                    retryReq.Headers.TryAddWithoutValidation("Origin", _settings.BaseUrl?.TrimEnd('/') + "/");
                                    retryReq.Headers.Remove("X-Requested-With");
                                    retryReq.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
                                }
                                catch (Exception) { }
                                var retryResp = await manualClient.SendAsync(retryReq);
                                var retryBody = await retryResp.Content.ReadAsStringAsync();
                                await AppendRawLogAsync("CHANGE_BRANCH:MANUAL_RETRY", _settings.Endpoints.ChangeBranch, changeBranchJson, retryResp.StatusCode, retryBody);
                                try { await SaveHttpTrafficAsync("CHANGE_BRANCH:MANUAL_RETRY", retryReq, retryResp); } catch { }

                                if (retryResp.IsSuccessStatusCode)
                                {
                                    try
                                    {
                                        using var rdoc = JsonDocument.Parse(retryBody);
                                        if (rdoc.RootElement.TryGetProperty("code", out var codeProp2) && codeProp2.ValueKind == JsonValueKind.Number && codeProp2.GetInt32() == 0)
                                        {
                                            _logger.LogInformation("Manual-cookie ChangeBranch retry succeeded");
                                            _cookieContainer = null;
                                            _isCookieAuthenticated = true;
                                            _sessionCookie = manualCookieValue;
                                            return true;
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        _logger.LogInformation("Manual-cookie ChangeBranch HTTP OK but response unparseable; treating as success");
                                        _cookieHttpClient = manualClient;
                                        _cookieContainer = null;
                                        _isCookieAuthenticated = true;
                                        _sessionCookie = manualCookieValue;
                                        return true;
                                    }
                                }
                                try { manualClient.Dispose(); } catch { }
                            }
                            else
                            {
                                _logger.LogDebug("No JSESSIONID found to attempt manual-cookie retry");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Manual-cookie retry attempt failed");
                        }
                        _logger.LogError("ChangeBranch returned login/branch error and manual retry did not succeed");
                        _isCookieAuthenticated = false;
                        return false;
                    }

                    try
                    {
                        using var changeDoc = JsonDocument.Parse(changeBranchBody);

                        if (changeDoc.RootElement.TryGetProperty("code", out var codeProp))
                        {
                            var code = codeProp.GetInt32();
                            if (code != 0)
                            {
                                var message = changeDoc.RootElement.TryGetProperty("message", out var msgProp)
                                    ? msgProp.GetString()
                                    : "Unknown error";
                                _logger.LogError("ChangeBranch returned error code {Code}: {Message}", code, message);
                                return false;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse ChangeBranch response, assuming success");
                    }

                    _logger.LogInformation("✓ Branch selection completed successfully");
                    return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during branch selection");
            return false;
        }
    }
    private bool TryExtractBranchId(JsonElement branch, out long branchId)
    {
        branchId = 0;
        string[] idFields = {
            "orgSirketSubeId",
            "orgSirketSubeID",
            "orgSubeId",
            "id",
            "branchId",
            "branchID",
            "subeId",
            "sirketSubeId",
            "companyId"
        };
        foreach (var field in idFields)
        {
            if (branch.TryGetProperty(field, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt64(out branchId))
                    return true;

                if (prop.ValueKind == JsonValueKind.String && long.TryParse(prop.GetString(), out branchId))
                    return true;
            }
        }
        return false;
    }
    private string? TryGetProperty(JsonElement element, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (element.TryGetProperty(name, out var prop) &&
                prop.ValueKind == JsonValueKind.String)
            {
                return prop.GetString();
            }
        }
        return null;
    }
    
    private decimal? TryGetDecimalProperty(JsonElement element, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (element.TryGetProperty(name, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number)
                {
                    return prop.GetDecimal();
                }
                if (prop.ValueKind == JsonValueKind.String)
                {
                    var str = prop.GetString()?.Replace(',', '.');
                    if (decimal.TryParse(str, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var val))
                        return val;
                }
            }
        }
        return null;
    }
    
    private double? TryGetDoubleProperty(JsonElement element, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (element.TryGetProperty(name, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number)
                {
                    return prop.GetDouble();
                }
                if (prop.ValueKind == JsonValueKind.String)
                {
                    var str = prop.GetString()?.Replace(',', '.');
                    if (double.TryParse(str, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var val))
                        return val;
                }
            }
        }
        return null;
    }
    
    private async Task SaveBranchesDebugInfoAsync(JsonElement branchesArray)
    {
        try
        {
            var logsDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            Directory.CreateDirectory(logsDir);

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var filePath = Path.Combine(logsDir, $"koza-branches-{timestamp}.json");

            var formattedJson = JsonSerializer.Serialize(
                branchesArray,
                new JsonSerializerOptions { WriteIndented = true }
            );

            await File.WriteAllTextAsync(filePath, formattedJson);
            _logger.LogDebug("Saved branches debug info to: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save branches debug info");
        }
    }
    private async Task<bool> PerformLoginAsync()
    {
        if (_cookieHttpClient == null)
        {
            var baseUri = new Uri(_settings.BaseUrl.TrimEnd('/') + "/");
            _cookieContainer = new System.Net.CookieContainer();
            _cookieHandler = new HttpClientHandler
            {
                CookieContainer = _cookieContainer,
                UseCookies = true,
                AllowAutoRedirect = true
            };
            _cookieHttpClient = new HttpClient(_cookieHandler)
            {
                BaseAddress = baseUri
            };
        }
        try
        {
            var authPath = _settings.Endpoints.Auth ?? "Giris.do";
            try
            {
                var getResp = await _cookieHttpClient.GetAsync(authPath);
                var getBody = await getResp.Content.ReadAsStringAsync();
                await AppendRawLogAsync("AUTH_LOGIN_GET", authPath, string.Empty, getResp.StatusCode, getBody);
            }
            catch (Exception getEx)
            {
                _logger.LogDebug(getEx, "Initial GET to login page failed (non-fatal)");
            }
        }
        catch (Exception) { }

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

        HttpResponseMessage? response = null;
        string authBody = string.Empty;

        foreach (var (desc, payload) in loginAttempts)
        {
            try
            {
                var payloadText = await ReadContentPreviewAsync(payload);
                response = await _cookieHttpClient!.PostAsync(_settings.Endpoints.Auth, payload);
                authBody = await ReadResponseContentAsync(response);
                await AppendRawLogAsync($"AUTH_LOGIN:{desc}", _settings.Endpoints.Auth, payloadText, response.StatusCode, authBody);
                try { await SaveHttpTrafficAsync($"AUTH_LOGIN:{desc}", null, response); } catch (Exception) {  }
                try
                {
                        var cookieContainerLocal = _cookieContainer;
                        if (cookieContainerLocal != null)
                        {
                            var baseUri = _cookieHttpClient?.BaseAddress ?? new Uri(_settings.BaseUrl.TrimEnd('/') + "/");
                            var cookies = cookieContainerLocal.GetCookies(baseUri);
                            var c = cookies.Cast<System.Net.Cookie>().FirstOrDefault(x => string.Equals(x.Name, "JSESSIONID", StringComparison.OrdinalIgnoreCase));
                            if (c != null && !string.IsNullOrWhiteSpace(c.Value))
                            {
                                _sessionCookie = "JSESSIONID=" + c.Value;
                                _manualJSessionId = "JSESSIONID=" + c.Value;
                                _cookieExpiresAt = DateTime.UtcNow.AddMinutes(20);
                                _logger.LogInformation("PerformLoginAsync: JSESSIONID acquired from CookieContainer (manual cache updated)");
                                return true;
                            }
                        }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error while checking CookieContainer after login attempt");
                }

                if (response.IsSuccessStatusCode && IsKozaLoginSuccess(authBody))
                {
                    _logger.LogInformation("PerformLoginAsync: login response indicates success (body) for {Desc}", desc);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Login attempt {Desc} failed", desc);
            }
        }

        _logger.LogError("Koza login failed; last response: {Body}", authBody);
        return false;
    }
    private async Task<long?> GetDefaultBranchIdAsync()
    {
        return await SelectDefaultBranchAsync();
    }
    private bool IsKozaLoginSuccess(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return false;
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("code", out var codeProp) && codeProp.ValueKind == JsonValueKind.Number)
                {
                    if (codeProp.GetInt32() == 0) return true;
                    
                    return false;
                }
                if (root.TryGetProperty("success", out var successProp) && successProp.ValueKind == JsonValueKind.True)
                    return true;
            }
        }
        catch
        {
            
        }
        var text = body.ToLowerInvariant();
        string[] failTokens = { "hatal", "yanl", "gecersiz", "captcha", "deneme hakk", "beklenmedik", "error", "exception" };
        if (failTokens.Any(text.Contains))
            return false;
        string[] successTokens = { "anasayfa", "menu", "redirect", "yetki", "hosgeldiniz", "giri\u015f ba\u015far\u0131l", "loginok" };
        if (successTokens.Any(text.Contains))
            return true;
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
            if (_settings.DefaultBranchId.HasValue)
            {
                _logger.LogInformation("DefaultBranchId configured; using branch {BranchId}", _settings.DefaultBranchId.Value);
                return _settings.DefaultBranchId.Value;
            }

            var branchesResp = await _cookieHttpClient!.PostAsync(_settings.Endpoints.Branches, CreateKozaContent("{}"));
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

        string[] idFields = { "orgSirketSubeId", "orgSirketSubeID", "orgSubeId", "id", "branchId", "branchID", "subeId", "sirketSubeId", "companyId" };
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
    private async Task EnsureBranchSelectedAsync()
    {
        if (_settings.UseTokenAuth) return;
        if (_cookieHttpClient == null) return;

        await _branchSemaphore.WaitAsync();
        try
        {
            List<LucaBranchDto> branches = new List<LucaBranchDto>();
            try
            {
                branches = (await GetBranchesAsync()) ?? new List<LucaBranchDto>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read branches list before selection");
            }

            if (branches.Count > 0)
            {
                _logger.LogInformation("Available branches: {Count} -> {Ids}", branches.Count, string.Join(", ", branches.Select(b => b.Id.ToString())));
            }
            if (branches.Count == 0)
            {
                _logger.LogWarning("Branch list is empty; attempting manual-cookie branch selection fallback");
                try
                {
                    var manualOk = await SelectBranchWithManualCookieAsync();
                    if (manualOk)
                    {
                        _logger.LogInformation("Manual-cookie branch selection succeeded");
                        return;
                    }
                    _logger.LogWarning("Manual-cookie branch selection did not find/apply a branch");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Manual-cookie branch selection attempt threw an exception");
                }
                var preferredBranchConfigured = _settings.ForcedBranchId ?? _settings.DefaultBranchId;
                if (preferredBranchConfigured.HasValue)
                {
                    _logger.LogWarning("Branch list empty; attempting direct ChangeBranch to configured preferred branch {BranchId}", preferredBranchConfigured.Value);
                    try
                    {
                        var changedDirect = await ChangeBranchAsync(preferredBranchConfigured.Value);
                        if (changedDirect)
                        {
                            _logger.LogInformation("Direct ChangeBranch to {BranchId} succeeded", preferredBranchConfigured.Value);
                            return;
                        }
                        _logger.LogWarning("Direct ChangeBranch to {BranchId} failed", preferredBranchConfigured.Value);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Direct ChangeBranch attempt threw an exception");
                    }
                }
                return;
            }

            var preferredBranch = _settings.ForcedBranchId ?? _settings.DefaultBranchId;

            if (preferredBranch.HasValue)
            {
                if (branches.Any(b => b.Id == preferredBranch.Value))
                {
                    _logger.LogInformation("Preferred branch {BranchId} is present in branch list, attempting to apply it", preferredBranch.Value);
                    var changed = await ChangeBranchAsync(preferredBranch.Value);
                    if (!changed)
                    {
                        _logger.LogWarning("Preferred branch {BranchId} could not be applied despite being present", preferredBranch.Value);
                    }
                    return;
                }

                _logger.LogWarning("Preferred branch {BranchId} not found in branch list; attempting to apply it anyway (will fallback to first available branch on failure)", preferredBranch.Value);
                var attempted = await ChangeBranchAsync(preferredBranch.Value);
                if (attempted)
                {
                    return;
                }
                var first = branches.FirstOrDefault();
                if (first != null && first.Id.HasValue)
                {
                    _logger.LogInformation("Falling back to first available branch {BranchId}", first.Id.Value);
                    var changed = await ChangeBranchAsync(first.Id.Value);
                    if (!changed)
                    {
                        _logger.LogWarning("Fallback branch change to {BranchId} failed", first.Id.Value);
                    }
                    return;
                }
                _logger.LogWarning("No branches available to fallback to after preferred branch attempt");
                return;
            }
            var branchId = await SelectDefaultBranchAsync();
            if (branchId.HasValue)
            {
                var changed = await ChangeBranchAsync(branchId.Value);
                if (!changed)
                {
                    _logger.LogWarning("Branch change to {BranchId} was not confirmed", branchId.Value);
                }
                return;
            }
            _logger.LogWarning("SelectDefaultBranchAsync did not return a branch; attempting manual-cookie fallback");
            try
            {
                var manualOk2 = await SelectBranchWithManualCookieAsync();
                if (manualOk2)
                {
                    _logger.LogInformation("Manual-cookie branch selection succeeded (fallback)");
                    return;
                }
                _logger.LogWarning("Manual-cookie fallback did not find/apply a branch");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Manual-cookie fallback attempt threw an exception");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "EnsureBranchSelectedAsync failed; proceeding with existing session");
        }
        finally
        {
            _branchSemaphore.Release();
        }
    }
    private async Task<bool> ChangeBranchAsync(long branchId)
    {
        try
        {
            
            var attempts = new List<(string desc, HttpContent content)>();

            var jsonPayload = JsonSerializer.Serialize(new { orgSirketSubeId = branchId }, _jsonOptions);
            attempts.Add(("JSON:orgSirketSubeId", CreateKozaContent(jsonPayload)));
           attempts.Add(("FORM:orgSirketSubeId", new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("orgSirketSubeId", branchId.ToString()) })));
            var jsonPayloadAlt = JsonSerializer.Serialize(new { orgSirketSubeID = branchId }, _jsonOptions);
            attempts.Add(("JSON:orgSirketSubeID", CreateKozaContent(jsonPayloadAlt)));
            attempts.Add(("FORM:orgSirketSubeID", new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("orgSirketSubeID", branchId.ToString()) })));
            attempts.Add(("JSON:id", CreateKozaContent(JsonSerializer.Serialize(new { id = branchId }, _jsonOptions))));
            foreach (var attempt in attempts)
            {
                var reAuthed = false;
                var desc = attempt.desc;
                var content = attempt.content;
                try
                {
retryChangeBranch:
                    var payloadText = await ReadContentPreviewAsync(content);

                    using (var req = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.ChangeBranch)
                    {
                        Content = content
                    })
                    {
                        ApplySessionCookie(req);
                        ApplyManualSessionCookie(req);
                        try
                        {
                            var cookieContainerLocal = _cookieContainer;
                            if (cookieContainerLocal != null && !string.IsNullOrWhiteSpace(_settings.BaseUrl))
                            {
                                try
                                {
                                    var uri = new Uri(_settings.BaseUrl.TrimEnd('/') + "/");
                                    var cookieCol = cookieContainerLocal.GetCookies(uri);
                                    var cookieList = cookieCol.Cast<System.Net.Cookie>().Select(c => $"{c.Name}={c.Value}").ToArray();
                                    _logger.LogDebug("ChangeBranch: CookieContainer contents before request: {Cookies}", string.Join(";", cookieList));
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogDebug(ex, "Failed to read CookieContainer for ChangeBranch debug");
                                }
                            }
                        }
                        catch (Exception) { }

                        var resp = await (_cookieHttpClient ?? _httpClient).SendAsync(req);
                        var body = await ReadResponseContentAsync(resp);
                        await AppendRawLogAsync("CHANGE_BRANCH:" + desc, _settings.Endpoints.ChangeBranch, payloadText, resp.StatusCode, body);
                        try { await SaveHttpTrafficAsync("CHANGE_BRANCH:" + desc, req, resp); } catch (Exception) { }

                        if (!string.IsNullOrWhiteSpace(body) && (body.Contains("Login olunmalı", StringComparison.OrdinalIgnoreCase) || body.Contains("login olunmali", StringComparison.OrdinalIgnoreCase) || body.Contains("1001") || body.Contains("1002")))
                        {
                            _logger.LogWarning("ChangeBranch response indicates not-authenticated or invalid session: {Preview}", body.Length > 300 ? body.Substring(0, 300) : body);
                            _isCookieAuthenticated = false;

                            if (!reAuthed)
                            {
                                reAuthed = true;
                                try
                                {
                                    await EnsureAuthenticatedAsync();
                                    content = CreateKozaContent(payloadText);
                                    _logger.LogInformation("Re-authenticated after ChangeBranch 1001; retrying {Desc}", desc);
                                    goto retryChangeBranch;
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Re-auth after ChangeBranch 1001 failed");
                                }
                            }
                            return false;
                        }
                        if (resp.IsSuccessStatusCode)
                        {
                            try
                            {
                                using var doc = JsonDocument.Parse(body);
                                if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("code", out var codeProp) && codeProp.ValueKind == JsonValueKind.Number)
                                {
                                    if (codeProp.GetInt32() == 0)
                                    {
                                        _logger.LogInformation("ChangeBranch succeeded using {Desc}", desc);
                                        return true;
                                    }
                                    else
                                    {
                                        _logger.LogWarning("ChangeBranch attempt {Desc} returned code {Code}", desc, codeProp.GetInt32());
                                    }
                                }
                                else
                                {
                                    _logger.LogInformation("ChangeBranch succeeded (HTTP OK) using {Desc}", desc);
                                    return true;
                                }
                            }
                            catch (Exception)
                            {
                                _logger.LogInformation("ChangeBranch HTTP OK with unparseable body using {Desc}", desc);
                                return true;
                            }
                        }
                        else
                        {
                            _logger.LogWarning("ChangeBranch attempt {Desc} failed with status {Status}", desc, resp.StatusCode);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "ChangeBranch attempt {Desc} threw", desc);
                }
            }
            _logger.LogWarning("All ChangeBranch attempts finished without success");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ChangeBranch call failed");
            return false;
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

                    response = await (_cookieHttpClient ?? client).SendAsync(retryRequest);
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
            for (var attempt = 1; attempt <= 3; attempt++)
            {
                await EnsureAuthenticatedAsync();
                await EnsureBranchSelectedAsync();

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

                response.EnsureSuccessStatusCode();
                return JsonSerializer.Deserialize<JsonElement>(responseContent);
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
                _logger.LogWarning("ListStockCardsAsync: Koza returned HTML (session expired?). Re-authenticating and retrying once...");
                
                // Session expired olabilir, yeniden login dene
                try
                {
                    await PerformLoginAsync();
                    await EnsureBranchSelectedAsync();
                    
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
        var result = new SyncResultDto
        {
            SyncType = "PRODUCT_STOCK_CARD",
            ProcessedRecords = stockCards.Count
        };

        var startTime = DateTime.UtcNow;
        var successCount = 0;
        var failedCount = 0;
        var duplicateCount = 0;
        var sentCount = 0;
        try
        {
            await EnsureAuthenticatedAsync();
            await EnsureBranchSelectedAsync();
            await VerifyBranchSelectionAsync();
            _logger.LogWarning(">>> USING SAFE PER-PRODUCT FLOW <<<");
            _logger.LogInformation("Sending {Count} stock cards to Luca (Koza) one by one (Koza does not accept arrays)", stockCards.Count);

            var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
            var endpoint = _settings.Endpoints.StockCardCreate;
            var enc1254 = Encoding.GetEncoding(1254);

            foreach (var card in stockCards)
            {
                try
                {
                    var payload = JsonSerializer.Serialize(card, _jsonOptions);

                    // Build form data string using the same rules as the working PowerShell script
                    var sb = new StringBuilder();
                    var baslangic = DateTime.Now.ToString("dd'/'MM'/'yyyy", System.Globalization.CultureInfo.InvariantCulture);
                    sb.Append($"baslangicTarihi={UrlEncodeCp1254(baslangic)}");

                    sb.Append($"&kartKodu={UrlEncodeCp1254(card.KartKodu ?? string.Empty)}");

                    var safeName = (card.KartAdi ?? string.Empty)
                        .Replace("Ø", "O")
                        .Replace("ø", "o");
                    sb.Append($"&kartAdi={UrlEncodeCp1254(safeName)}");

                    sb.Append("&kartTuru=1");
                    sb.Append("&olcumBirimiId=1");
                    sb.Append("&kartAlisKdvOran=1");
                    sb.Append("&kartSatisKdvOran=1");
                    sb.Append("&kartTipi=1");

                    if (!string.IsNullOrEmpty(card.KategoriAgacKod))
                    {
                        var kAgac = card.KategoriAgacKod;
                        sb.Append($"&kategoriAgacKod={UrlEncodeCp1254(kAgac)}");
                    }

                    sb.Append("&satilabilirFlag=1");
                    sb.Append("&satinAlinabilirFlag=1");
                    sb.Append($"&lotNoFlag={card.LotNoFlag}");
                    sb.Append("&maliyetHesaplanacakFlag=1");
                    var formDataString = sb.ToString();

                    _logger.LogInformation(">>> LUCA FORM DATA ({Card}): {Form}", card.KartKodu, formDataString);

                    var encoding = enc1254;
                    var byteContent = new ByteArrayContent(encoding.GetBytes(formDataString));
                    byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                    byteContent.Headers.ContentType.CharSet = "windows-1254";

                    using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
                    {
                        Content = byteContent
                    };
                    ApplyManualSessionCookie(httpRequest);

                    try { await SaveHttpTrafficAsync($"SEND_STOCK_CARD_REQUEST:{card.KartKodu}", httpRequest, null); } catch (Exception) { }

                    sentCount++;
                    var response = await client.SendAsync(httpRequest);
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
                    var baseUrl = client.BaseAddress?.ToString()?.TrimEnd('/') ?? _settings.BaseUrl?.TrimEnd('/') ?? string.Empty;
                    var fullUrl = string.IsNullOrWhiteSpace(baseUrl) ? endpoint : (endpoint.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? endpoint : baseUrl + "/" + endpoint.TrimStart('/'));
                    await AppendRawLogAsync("SEND_STOCK_CARD", fullUrl, formDataString, response.StatusCode, responseContent);
                    try { await SaveHttpTrafficAsync($"SEND_STOCK_CARD_RESPONSE:{card.KartKodu}", httpRequest, response); } catch (Exception) { }

                    if (NeedsBranchSelection(responseContent))
                    {
                        _logger.LogWarning("Stock card {Card} failed due to branch not selected; re-authenticating + branch change, then retrying once", card.KartKodu);
                        _isCookieAuthenticated = false;
                        await EnsureAuthenticatedAsync();
                        await EnsureBranchSelectedAsync();
                        await VerifyBranchSelectionAsync();
                        var retryReq = new HttpRequestMessage(HttpMethod.Post, endpoint)
                        {
                            Content = new ByteArrayContent(enc1254.GetBytes(payload))
                            {
                                Headers = { ContentType = new MediaTypeHeaderValue("application/json") { CharSet = _encoding.WebName } }
                            }
                        };
                        ApplyManualSessionCookie(retryReq);
                        sentCount++;
                        response = await (_cookieHttpClient ?? client).SendAsync(retryReq);
                        responseBytes = await response.Content.ReadAsByteArrayAsync();
                        try { responseContent = enc1254.GetString(responseBytes); } catch { responseContent = Encoding.UTF8.GetString(responseBytes); }
                        await AppendRawLogAsync("SEND_STOCK_CARD_RETRY", fullUrl, payload, response.StatusCode, responseContent);
                    }

                    if (responseContent.TrimStart().StartsWith("<", StringComparison.OrdinalIgnoreCase))
                    {
                        
                        _logger.LogError("Stock card {Card} returned HTML. Snippet: {Snippet}", card.KartKodu, responseContent.Length > 200 ? responseContent.Substring(0, 200) : responseContent);
                        _logger.LogWarning("Stock card {Card} returned HTML. Will attempt UTF-8 JSON retry then form-encoded retry.", card.KartKodu);
                        await AppendRawLogAsync($"SEND_STOCK_CARD_HTML:{card.KartKodu}", fullUrl, payload, response.StatusCode, responseContent);
                        try { await SaveHttpTrafficAsync($"SEND_STOCK_CARD_HTML:{card.KartKodu}", null, response); } catch (Exception) {  }
                        
                        try
                        {
                            var utf8Bytes = Encoding.UTF8.GetBytes(payload);
                            var utf8Content = new ByteArrayContent(utf8Bytes);
                            utf8Content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
                            using var utf8Req = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = utf8Content };
                            ApplyManualSessionCookie(utf8Req);
                            sentCount++;
                            var utf8Resp = await (_cookieHttpClient ?? client).SendAsync(utf8Req);
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
                                var formResp = await (_cookieHttpClient ?? client).SendAsync(formReq);
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
                                _logger.LogWarning("Stock card {Card} already exists in Luca (skipped): {Message}", card.KartKodu, msg);
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
                    if (parsedSuccessfully &&
                        parsedResponse.ValueKind == JsonValueKind.Object &&
                        parsedResponse.TryGetProperty("stkSkart", out var skartEl) &&
                        skartEl.ValueKind == JsonValueKind.Object &&
                        skartEl.TryGetProperty("skartId", out var idEl))
                    {
                        _logger.LogInformation("Stock card {Card} created with ID {Id}", card.KartKodu, idEl.ToString());
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
        result.DuplicateRecords = duplicateCount;
        result.SentRecords = sentCount;
        // IsSuccess should be true if no real failures occurred (duplicates are not failures)
        result.IsSuccess = failedCount == 0;
        result.Message = duplicateCount > 0 
            ? $"{successCount} yeni oluşturuldu, {duplicateCount} zaten vardı, {failedCount} başarısız" 
            : $"{successCount} success, {failedCount} failed";
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
            MaliyetHesaplanacakFlag = stockCard.MaliyetHesaplanacakFlag ? 1 : 0,
            GtipKodu = stockCard.GtipKodu ?? string.Empty,
            GarantiSuresi = stockCard.GarantiSuresi ?? 0,
            RafOmru = stockCard.RafOmru ?? 0,
            AlisTevkifatOran = stockCard.AlisTevkifatOran ?? "0",
            AlisTevkifatKod = stockCard.AlisTevkifatKod ?? 0,
            SatisTevkifatOran = stockCard.SatisTevkifatOran ?? "0",
            SatisTevkifatKod = stockCard.SatisTevkifatKod ?? 0,
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
    public async Task<JsonElement> ListInvoicesAsync(LucaListInvoicesRequest request, bool detayliListe = false)
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

        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> CreateInvoiceRawAsync(LucaCreateInvoiceHeaderRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);

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
        var content = CreateKozaContent(json);
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
        var content = CreateKozaContent(json);
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
        var response = await client.PostAsync(url, content);
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
    public async Task<JsonElement> ListDeliveryNotesAsync(bool detayliListe = false)
    {
        await EnsureAuthenticatedAsync();

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var url = _settings.Endpoints.IrsaliyeList + (detayliListe ? "?detayliListe=true" : string.Empty);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = CreateKozaContent("{}")
        };
        ApplyManualSessionCookie(httpRequest);
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
        var content = CreateKozaContent(json);

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
        var response = await client.PostAsync(_settings.Endpoints.CustomerCreate, content);
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

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.SalesOrder, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
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
            
            // Response'dan ID çıkar
            if (result.TryGetProperty("id", out var idProp) || result.TryGetProperty("ssBelgeId", out idProp))
            {
                return idProp.GetInt64();
            }
            
            // Alternatif: data.id
            if (result.TryGetProperty("data", out var dataProp) && dataProp.TryGetProperty("id", out idProp))
            {
                return idProp.GetInt64();
            }
            
            _logger.LogWarning("Depo transfer response'dan ID çıkarılamadı: {Response}", result.GetRawText());
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
            
            // Response'dan ID çıkar
            if (result.TryGetProperty("id", out var idProp) || result.TryGetProperty("ssDshBaslikId", out idProp))
            {
                return idProp.GetInt64();
            }
            
            // Alternatif: data.id
            if (result.TryGetProperty("data", out var dataProp) && dataProp.TryGetProperty("id", out idProp))
            {
                return idProp.GetInt64();
            }
            
            _logger.LogWarning("DSH stok fişi response'dan ID çıkarılamadı: {Response}", result.GetRawText());
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
                ? "A"
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
            var manual = !string.IsNullOrWhiteSpace(_manualJSessionId)
                ? _manualJSessionId
                : _settings?.ManualSessionCookie;
            if (string.IsNullOrWhiteSpace(manual)) return;

            var trimmed = manual.Trim();
            if (trimmed.IndexOf("FILL_ME", StringComparison.OrdinalIgnoreCase) >= 0) return;

            if (!request.Headers.Contains("Cookie"))
            {
                request.Headers.TryAddWithoutValidation("Cookie", trimmed);
            }
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
            var element = await ListDeliveryNotesAsync(true);

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

        try
        {
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

            if (result.ValueKind == JsonValueKind.Object)
            {
                // Check for "list" array
                if (result.TryGetProperty("list", out var listProp) && listProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in listProp.EnumerateArray())
                    {
                        if (item.TryGetProperty("skartId", out var skartIdProp))
                        {
                            if (skartIdProp.ValueKind == JsonValueKind.Number)
                                return skartIdProp.GetInt64();
                            if (skartIdProp.ValueKind == JsonValueKind.String && long.TryParse(skartIdProp.GetString(), out var parsed))
                                return parsed;
                        }
                    }
                }
            }

            _logger.LogInformation("Stock card with SKU {SKU} not found in Luca", sku);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for stock card by SKU {SKU} in Luca", sku);
            return null;
        }
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
}
