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
using System.Security.Authentication;
using System.Net.Sockets;

namespace Katana.Infrastructure.APIClients;

/// <summary>
/// LucaService - PART 1: Core (Fields, Constructor, Authentication, Session, Login, Branch Selection)
/// </summary>
public partial class LucaService : ILucaService
{
    private readonly Katana.Core.Interfaces.ILucaCookieJarStore? _externalCookieJarStore;
    private readonly Katana.Core.Interfaces.ISyncProgressReporter _syncProgressReporter;
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
    private bool _branchSelected;
    private DateTime? _lastSuccessfulAuthAt = null;
    private static readonly System.Threading.SemaphoreSlim _loginSemaphore = new System.Threading.SemaphoreSlim(1, 1);
    
    // 🔥 AUTH RATE LIMITING: Çok sık auth önleme
    private static readonly SemaphoreSlim _authLock = new SemaphoreSlim(1, 1);
    private DateTime _lastAuthTime = DateTime.MinValue;
    private static readonly TimeSpan MinAuthInterval = TimeSpan.FromSeconds(5);
    
    // 🔥 FILE LOCK: Log dosyası yazımı için
    private static readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);
    
    // 🔥 CACHE: Duplicate stok kartı sorunu için in-memory cache
    private readonly Dictionary<string, long?> _stockCardCache = new();
    private readonly SemaphoreSlim _stockCardCacheLock = new(1, 1);
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
    private readonly SemaphoreSlim _kozaCustomerRequestLock = new(1, 1);
    private static readonly TimeSpan[] KozaConnectionResetRetryDelays =
    {
        TimeSpan.FromMilliseconds(500),
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4)
    };
    private const int KozaThrottleMinDelayMs = 350;
    private const int KozaThrottleMaxDelayMs = 1000;
    private string CookieJarKey =>
        string.IsNullOrWhiteSpace(_settings.BaseUrl) ? "LucaCookieJar:default" : $"LucaCookieJar:{_settings.BaseUrl.Trim()}";

    private System.Net.CookieContainer GetOrCreateSharedCookieContainer()
    {
        if (_externalCookieJarStore != null)
        {
            return _externalCookieJarStore.GetOrCreate(CookieJarKey);
        }

        return _cookieContainer ??= new System.Net.CookieContainer();
    }
    public LucaService(
        HttpClient httpClient,
        IOptions<LucaApiSettings> settings,
        ILogger<LucaService> logger,
        Katana.Core.Interfaces.ILucaCookieJarStore? cookieJarStore = null,
        Katana.Core.Interfaces.ISyncProgressReporter? syncProgressReporter = null)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
        _externalCookieJarStore = cookieJarStore;
        _syncProgressReporter = syncProgressReporter ?? new Katana.Core.Interfaces.NoopSyncProgressReporter();
        _encoding = InitializeEncoding(_settings.Encoding);

        ApplyDefaultHttpClientHeaders(_httpClient);
        // Share session cookies across transient LucaService instances to avoid re-login storms.
        _cookieContainer = _externalCookieJarStore?.GetOrCreate(CookieJarKey);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
        try
        {
            // LucaService is created via HttpClientFactory (transient).
            // Starting a per-instance background auth refresh loop causes authentication storms.
            // Authentication is handled on-demand by EnsureAuthenticatedAsync and by explicit workers/controllers when needed.
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Cookie refresh loop not started");
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
        // 🔥 DEBUG: Authentication durumunu logla
        var manualCookieValue = _settings.ManualSessionCookie ?? "";
        var isManualCookieValid = !string.IsNullOrWhiteSpace(manualCookieValue) && 
                                  !manualCookieValue.Contains("FILL_ME", StringComparison.OrdinalIgnoreCase) &&
                                  manualCookieValue.Length > 20;
        
        _logger.LogDebug("🔐 EnsureAuthenticatedAsync: UseTokenAuth={UseToken}, IsAuthenticated={IsAuth}, HasSession={HasSession}, ManualCookieValid={ManualValid}, CookieExpiry={Expiry}",
            _settings.UseTokenAuth,
            _isCookieAuthenticated,
            !string.IsNullOrWhiteSpace(_sessionCookie) || !string.IsNullOrWhiteSpace(_manualJSessionId),
            isManualCookieValid,
            _cookieExpiresAt?.ToString("HH:mm:ss") ?? "N/A");
        
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
        if (!string.IsNullOrWhiteSpace(_settings.ManualSessionCookie) && 
            !_settings.ManualSessionCookie.Contains("FILL_ME", StringComparison.OrdinalIgnoreCase) &&
            _settings.ManualSessionCookie.Length > 20)
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
                var handler = CreateHandler(useCookies: false, container: null);

                _cookieHttpClient = new HttpClient(handler)
                {
                    BaseAddress = baseUri,
                    Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds)
                };

                ApplyDefaultHttpClientHeaders(_cookieHttpClient);

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

    /// <summary>
    /// Session'ı tamamen yeniler - tüm cookie state'i temizler ve yeniden login yapar.
    /// HTML response alındığında veya session timeout durumunda kullanılır.
    /// </summary>
    public async Task ForceSessionRefreshAsync()
    {
        _logger.LogWarning("🔄 ForceSessionRefreshAsync: Tüm session state temizleniyor...");
        
        // 1. Tüm session state'i temizle
        MarkSessionUnauthenticated();
        _sessionCookie = null;
        _manualJSessionId = null;
        _cookieExpiresAt = null;
        _lastSuccessfulAuthAt = null;
        
        // 2. Cookie container'ı temizle
        if (_cookieContainer != null)
        {
            try
            {
                var baseUri = new Uri(_settings.BaseUrl.TrimEnd('/') + "/");
                var cookies = _cookieContainer.GetCookies(baseUri);
                foreach (System.Net.Cookie cookie in cookies)
                {
                    cookie.Expired = true;
                }
                _logger.LogDebug("🍪 Cookie container temizlendi");
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Cookie container temizleme hatası");
            }
        }
        
        // 3. HttpClient'ı yeniden oluştur (DI HttpClient'ı dispose ETME)
        try
        {
            if (_cookieHttpClient != null)
            {
                _cookieHttpClient.DefaultRequestHeaders.Clear();
                _logger.LogDebug("🧹 HttpClient headers temizlendi (Client dispose edilmedi)");
            }

            _cookieContainer = GetOrCreateSharedCookieContainer();
            _cookieHandler = CreateHandler(useCookies: true, container: _cookieContainer);
            _cookieHttpClient = new HttpClient(_cookieHandler)
            {
                BaseAddress = new Uri(_settings.BaseUrl.TrimEnd('/') + "/"),
                Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds)
            };

            ApplyDefaultHttpClientHeaders(_cookieHttpClient);
            _logger.LogDebug("🔌 HttpClient yeniden oluşturuldu (yeni CookieContainer ile)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HttpClient yenileme hatası");
        }
        
        // 4. Yeniden login yap
        _logger.LogInformation("🔑 Yeniden login yapılıyor...");
        await EnsureAuthenticatedAsync();
        
        // 5. Branch seçimi
        _logger.LogDebug("🏢 Branch seçimi kontrol ediliyor...");
        await EnsureBranchSelectedAsync();
        
        // 6. Session warmup
        _logger.LogDebug("🔥 Session warmup başlatılıyor (ForceSessionRefresh sonrası)...");
        try
        {
            var forceRefreshWarmupOk = await WarmupSessionAsync();
            if (!forceRefreshWarmupOk)
            {
                _logger.LogWarning("⚠️ Session warmup başarısız oldu, ancak devam ediliyor");
            }
            else
            {
                _logger.LogDebug("✅ Session warmup başarılı");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Session warmup sırasında hata oluştu, ancak devam ediliyor");
        }
        
        _logger.LogInformation("✅ ForceSessionRefreshAsync tamamlandı. Authenticated: {IsAuth}, Cookie: {HasCookie}", 
            _isCookieAuthenticated, 
            !string.IsNullOrWhiteSpace(_sessionCookie) || !string.IsNullOrWhiteSpace(_manualJSessionId));
    }

    private void MarkSessionUnauthenticated()
    {
        _isCookieAuthenticated = false;
        _branchSelected = false;
    }

    private bool BranchSelectionCompleted()
    {
        _branchSelected = true;
        return true;
    }

    private async Task LoginWithServiceAsync()
    {
        // Fast, non-blocking guard to avoid queuing many callers when a valid session exists
        if (_isCookieAuthenticated && !string.IsNullOrWhiteSpace(_sessionCookie) && (!_cookieExpiresAt.HasValue || DateTime.UtcNow < _cookieExpiresAt.Value))
        {
            _logger.LogDebug("Existing Koza session is valid, skipping login (fast-path)");
            return;
        }

        // Another fast-path for transient instances: reuse any existing CookieContainer session.
        // This prevents each instance from running a full login flow even though a session is already present in memory.
        try
        {
            _cookieContainer ??= _externalCookieJarStore?.GetOrCreate(CookieJarKey);
            var js = TryGetJSessionFromContainer();
            if (!string.IsNullOrWhiteSpace(js))
            {
                _sessionCookie = js.StartsWith("JSESSIONID=", StringComparison.OrdinalIgnoreCase) ? js : "JSESSIONID=" + js;
                _cookieExpiresAt ??= DateTime.UtcNow.AddMinutes(20);
                _isCookieAuthenticated = true;
                _lastSuccessfulAuthAt ??= DateTime.UtcNow;

                // Ensure we have a cookie-aware HttpClient bound to the shared CookieContainer.
                // Otherwise calls that fall back to `_httpClient` may miss the session cookies and return "Login olunmalı".
                if (_cookieHttpClient == null)
                {
                    var baseUri = new Uri($"{_settings.BaseUrl.TrimEnd('/')}/");
                    _cookieContainer = GetOrCreateSharedCookieContainer();
                    _cookieHandler = CreateHandler(useCookies: true, container: _cookieContainer);
                    _cookieHttpClient = new HttpClient(_cookieHandler)
                    {
                        BaseAddress = baseUri,
                        Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds)
                    };
                    ApplyDefaultHttpClientHeaders(_cookieHttpClient);
                }
                _logger.LogDebug("Reusing existing JSESSIONID from shared CookieContainer; skipping login flow");
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to reuse JSESSIONID from shared CookieContainer");
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
	                            _cookieContainer = GetOrCreateSharedCookieContainer();
	                            _cookieHandler = CreateHandler(useCookies: true, container: _cookieContainer);
	                            _cookieHttpClient = new HttpClient(_cookieHandler)
	                            {
	                                BaseAddress = baseUri,
                                Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds)
                            };
                        }
                        ApplyDefaultHttpClientHeaders(_cookieHttpClient);

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
                        
                        // 🔥 SESSION WARMUP: Struts framework'ünü uyandır
                        _logger.LogDebug("🔥 Starting session warmup after headless auth...");
                        var headlessWarmupOk = await WarmupSessionAsync();
                        if (!headlessWarmupOk)
                        {
                            _logger.LogWarning("⚠️ Session warmup başarısız oldu, ancak devam ediliyor");
                        }
                        
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
	                _cookieContainer = GetOrCreateSharedCookieContainer();
	                _cookieHandler = CreateHandler(useCookies: true, container: _cookieContainer);
	                _cookieHttpClient = new HttpClient(_cookieHandler)
	                {
	                    BaseAddress = baseUri,
                    Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds)
                };
            }

            ApplyDefaultHttpClientHeaders(_cookieHttpClient);
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
            
            // 🔥 SESSION WARMUP: Struts framework'ünü uyandır
            _logger.LogDebug("🔥 Starting session warmup after branch selection...");
            var performLoginWarmupOk = await WarmupSessionAsync();
            if (!performLoginWarmupOk)
            {
                _logger.LogWarning("⚠️ Session warmup başarısız oldu, ancak devam ediliyor");
            }
            
            _lastSuccessfulAuthAt = DateTime.UtcNow;
            _logger.LogInformation("=== Koza Authentication Complete (WS/PerformLogin) ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "!!! Koza authentication failed !!!");
            MarkSessionUnauthenticated();
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

            // 🔥 DEFENSIVE PROGRAMMING STEP 3: STRUTS TIMING FIX
            // Şube değiştikten sonra Struts framework'ün internal state'ini senkronize etmesi için
            // KISA BİR BEKLEME süresi ekle. Yoksa ListStockCards isteği "Unable to instantiate Action" hatası alır.
            // User feedback: "Şube değiştirdikten (ChangeBranch) sonra, liste çekme isteği (ListStockCards) 
            //                 yapmadan önce araya çok kısa bir Task.Delay(500) koymayı dene"
            _logger.LogDebug("⏳ [STRUTS SYNC] Waiting 500ms after ChangeBranch for Struts framework synchronization...");
            await Task.Delay(500);
            _logger.LogDebug("✅ [STRUTS SYNC] Delay complete - ready for ListStockCards");

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

                                var handler = CreateHandler(useCookies: false, container: null);
                                var manualClient = new HttpClient(handler)
                                {
                                    BaseAddress = new Uri(_settings.BaseUrl.TrimEnd('/') + "/")
                                };
                                ApplyDefaultHttpClientHeaders(manualClient);
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
                                            return BranchSelectionCompleted();
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        _logger.LogInformation("Manual-cookie ChangeBranch HTTP OK but response unparseable; treating as success");
                                        _cookieHttpClient = manualClient;
                                        _cookieContainer = null;
                                        _isCookieAuthenticated = true;
                                        _sessionCookie = manualCookieValue;
                                        return BranchSelectionCompleted();
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
                        MarkSessionUnauthenticated();
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
                    
                    // 🔥 SESSION WARMUP: Struts framework'ünü uyandır
                    _logger.LogDebug("🔥 Starting session warmup after manual cookie branch selection...");
                    var manualCookieWarmupOk = await WarmupSessionAsync();
                    if (!manualCookieWarmupOk)
                    {
                        _logger.LogWarning("⚠️ Session warmup başarısız oldu, ancak devam ediliyor");
                    }
                    
                    return BranchSelectionCompleted();
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
	            _cookieContainer = GetOrCreateSharedCookieContainer();
	            _cookieHandler = CreateHandler(useCookies: true, container: _cookieContainer);
	            _cookieHttpClient = new HttpClient(_cookieHandler)
	            {
	                BaseAddress = baseUri,
                Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds)
            };
            ApplyDefaultHttpClientHeaders(_cookieHttpClient);
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
                
                // 🔥 DEBUG: Login response durumunu logla
                _logger.LogInformation("🔐 Login attempt '{Desc}': Status={Status}, HasCookie={HasCookie}", 
                    desc, 
                    response.StatusCode,
                    response.Headers.Contains("Set-Cookie"));
                
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
                                
                                // 🔥 DEBUG: Cookie detaylarını logla
                                _logger.LogInformation("🔐 Login SUCCESS: JSESSIONID acquired. Cookie preview: {Preview}, Expires: {Expiry}", 
                                    _sessionCookie.Substring(0, Math.Min(30, _sessionCookie.Length)) + "...",
                                    _cookieExpiresAt?.ToString("HH:mm:ss"));
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
    private async Task EnsureBranchSelectedAsync(bool force = false)
    {
        if (_settings.UseTokenAuth) return;
        if (_cookieHttpClient == null) return;
        if (!force && _branchSelected && _isCookieAuthenticated)
        {
            _logger.LogDebug("Branch already selected; skipping re-selection.");
            return;
        }

        await _branchSemaphore.WaitAsync();
        try
        {
            if (!force && _branchSelected && _isCookieAuthenticated)
            {
                _logger.LogDebug("Branch already selected (inside lock); skipping.");
                return;
            }
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
                            MarkSessionUnauthenticated();

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
                                        return BranchSelectionCompleted();
                                    }
                                    else
                                    {
                                        _logger.LogWarning("ChangeBranch attempt {Desc} returned code {Code}", desc, codeProp.GetInt32());
                                    }
                                }
                                else
                                {
                                    _logger.LogInformation("ChangeBranch succeeded (HTTP OK) using {Desc}", desc);
                                    return BranchSelectionCompleted();
                                }
                            }
                            catch (Exception)
                            {
                                _logger.LogInformation("ChangeBranch HTTP OK with unparseable body using {Desc}", desc);
                                return BranchSelectionCompleted();
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

    /// <summary>
    /// Session warmup: Struts framework'ünü uyandırmak için basit bir GET isteği atar.
    /// Login + cookie alındıktan sonra çağrılmalıdır.
    /// "Unable to instantiate Action" hatasını önlemek için kullanılır.
    /// </summary>
    private async Task<bool> WarmupSessionAsync()
    {
        try
        {
            _logger.LogInformation("🔥 Session warmup başlatılıyor...");
            
            // FIXED: Postman collection ile uyumlu POST + JSON body kullan
            // Postman'da: POST ListeleStkSkart.do + JSON body
            var warmupBody = new
            {
                stkSkart = new
                {
                    eklemeTarihiBas = "06/04/2022",
                    eklemeTarihiBit = "06/04/2022",
                    eklemeTarihiOp = "between"
                }
            };
            
            var json = JsonSerializer.Serialize(warmupBody, _jsonOptions);
            
            using var request = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.StockCards)
            {
                Content = CreateKozaContent(json)
            };
            
            // Mevcut session cookie'lerini uygula
            ApplySessionCookie(request);
            ApplyManualSessionCookie(request);
            
            var response = await (_cookieHttpClient ?? _httpClient).SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();
            
            _logger.LogDebug("Warmup response status: {Status}, body length: {Length}", 
                response.StatusCode, 
                responseBody?.Length ?? 0);
            
            // Response gövdesi JSON'a benziyorsa başarılı say
            if (!string.IsNullOrWhiteSpace(responseBody))
            {
                var trimmed = responseBody.TrimStart();
                if (trimmed.StartsWith("{") || trimmed.StartsWith("["))
                {
                    _logger.LogInformation("✅ Session warmup başarılı - JSON response alındı");
                    return true;
                }
                
                // HTML gibi görünüyorsa warning
                if (trimmed.StartsWith("<") || trimmed.Contains("<!DOCTYPE", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("⚠️ Session warmup: HTML response alındı (session timeout olabilir)");
                    _logger.LogDebug("HTML preview: {Preview}", 
                        responseBody.Substring(0, Math.Min(200, responseBody.Length)));
                    
                    await Task.Delay(1000);
                    return false;
                }
            }
            
            // Belirsiz durum - başarılı kabul et
            _logger.LogInformation("✅ Session warmup tamamlandı (response belirsiz ama hata yok)");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Session warmup sırasında hata oluştu");
            await Task.Delay(2000);
            return false;
        }
    }

    private HttpClientHandler CreateHandler(bool useCookies, CookieContainer? container)
    {
        var handler = new HttpClientHandler
        {
            UseCookies = useCookies,
            CookieContainer = useCookies ? container : null,
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            SslProtocols = SslProtocols.Tls12
        };

        try
        {
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }
        catch
        {
            // Non-fatal: fallback to default validation if setting callback fails
        }

        return handler;
    }

    private void ApplyDefaultHttpClientHeaders(HttpClient client)
    {
        client.DefaultRequestHeaders.ExpectContinue = false;
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (!client.DefaultRequestHeaders.UserAgent.Any())
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }
    }

    private async Task<KozaHttpResponse> SendKozaCustomerRequestAsync(Func<HttpRequestMessage> requestFactory, CancellationToken ct)
    {
        await _kozaCustomerRequestLock.WaitAsync(ct);
        try
        {
            var jitter = Random.Shared.Next(KozaThrottleMinDelayMs, KozaThrottleMaxDelayMs + 1);
            if (jitter > 0)
            {
                await Task.Delay(jitter, ct);
            }

            var totalAttempts = KozaConnectionResetRetryDelays.Length + 1;
            for (var attempt = 0; attempt <= KozaConnectionResetRetryDelays.Length; attempt++)
            {
                using var request = requestFactory();
                ForceHttp11(request);

                try
                {
                    var client = _cookieHttpClient ?? _httpClient;
                    using var response = await client.SendAsync(request, ct);

                    string body;
                    var statusCode = response.StatusCode;
                    var isSuccess = response.IsSuccessStatusCode;

                    try
                    {
                        body = await response.Content.ReadAsStringAsync(ct);
                    }
                    catch (Exception ex) when (IsConnectionResetException(ex) && attempt < KozaConnectionResetRetryDelays.Length)
                    {
                        var delay = KozaConnectionResetRetryDelays[attempt];
                        _logger.LogWarning(ex, "Koza customer response read failed (attempt {Attempt}/{Total}). Retrying in {Delay} ms",
                            attempt + 1, totalAttempts, delay.TotalMilliseconds);
                        await Task.Delay(delay, ct);
                        continue;
                    }

                    return new KozaHttpResponse
                    {
                        StatusCode = statusCode,
                        IsSuccessStatusCode = isSuccess,
                        Body = body
                    };
                }
                catch (Exception ex) when (IsConnectionResetException(ex) && attempt < KozaConnectionResetRetryDelays.Length)
                {
                    var delay = KozaConnectionResetRetryDelays[attempt];
                    _logger.LogWarning(ex, "Koza customer request failed due to connection reset (attempt {Attempt}/{Total}). Retrying in {Delay} ms",
                        attempt + 1, totalAttempts, delay.TotalMilliseconds);
                    await Task.Delay(delay, ct);
                }
            }

            throw new HttpRequestException("Koza customer request failed after retry attempts.");
        }
        finally
        {
            _kozaCustomerRequestLock.Release();
        }
    }

    private static void ForceHttp11(HttpRequestMessage request)
    {
        request.Version = HttpVersion.Version11;
        request.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
    }

    private static bool IsConnectionResetException(Exception ex)
    {
        Exception? current = ex;
        while (current != null)
        {
            if (current is SocketException socketEx && socketEx.SocketErrorCode == SocketError.ConnectionReset)
            {
                return true;
            }

            if (current is IOException && current.Message.Contains("reset", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (current.Message.Contains("Connection reset", StringComparison.OrdinalIgnoreCase) ||
                current.Message.Contains("reset by peer", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            current = current.InnerException;
        }

        return false;
    }

    private sealed class KozaHttpResponse
    {
        public HttpStatusCode StatusCode { get; init; }
        public bool IsSuccessStatusCode { get; init; }
        public string Body { get; init; } = string.Empty;
    }
}
