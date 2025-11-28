using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using Katana.Core.Interfaces;
using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Microsoft.Extensions.Options;
using System;
using Microsoft.Extensions.Logging;

namespace Katana.API.Controllers
{
    [ApiController]
    // Accept legacy paths /api/Luca/... and /api/luca/... in addition to /api/luca-proxy/...
    [Route("api/luca-proxy")]
    [Route("api/luca")]
    public class LucaProxyController : ControllerBase
    {
        private readonly string _lucaBaseUrl;
        private const string SessionCookieName = "LucaProxySession";
        private readonly ILucaCookieJarStore _cookieJarStore;
        private readonly ILogger<LucaProxyController> _logger;
        private readonly Katana.Data.Configuration.LucaApiSettings _settings;
        private readonly ISyncService _syncService;

        public LucaProxyController(ILucaCookieJarStore cookieJarStore, IOptions<Katana.Data.Configuration.LucaApiSettings> settings, ILogger<LucaProxyController> logger, ISyncService syncService)
        {
            _cookieJarStore = cookieJarStore;
            _logger = logger;
            _settings = settings.Value;
            _syncService = syncService;
            _lucaBaseUrl = (_settings.BaseUrl ?? string.Empty).TrimEnd('/');
            if (string.IsNullOrWhiteSpace(_lucaBaseUrl))
                _lucaBaseUrl = "https://akozas.luca.com.tr/Yetki"; 
        }

        private void ApplyManualSessionCookie(HttpRequestMessage request)
        {
            try
            {
                if (request == null) return;

                // Only apply explicitly configured manual cookie; do not add anything if empty/placeholder
                var manual = _settings.ManualSessionCookie ?? string.Empty;
                manual = manual.Trim();
                if (string.IsNullOrWhiteSpace(manual)) return;
                if (manual.IndexOf("FILL_ME", StringComparison.OrdinalIgnoreCase) >= 0) return;

                if (!request.Headers.Contains("Cookie"))
                {
                    request.Headers.TryAddWithoutValidation("Cookie", manual);
                }
            }
            catch {  }
        }

        
        private async Task<IActionResult> ForwardResponse(HttpResponseMessage response)
        {
            
            var responseContent = await response.Content.ReadAsStringAsync();
            return new ContentResult
            {
                Content = responseContent,
                ContentType = "application/json",
                StatusCode = (int)response.StatusCode
            };
        }

        private string? GetSessionIdFromRequest()
        {
            // Support both legacy/new header names: 'X-Luca-Session' and 'X-Luca-Proxy-Session'
            try
            {
                if (Request.Headers.TryGetValue("X-Luca-Session", out var headerVals1))
                {
                    var hdr1 = headerVals1.FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(hdr1))
                    {
                        _logger.LogDebug("GetSessionIdFromRequest: found header X-Luca-Session (length={Len})", hdr1?.Length ?? 0);
                        return hdr1;
                    }
                }

                if (Request.Headers.TryGetValue("X-Luca-Proxy-Session", out var headerVals2))
                {
                    var hdr2 = headerVals2.FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(hdr2))
                    {
                        _logger.LogDebug("GetSessionIdFromRequest: found header X-Luca-Proxy-Session (length={Len})", hdr2?.Length ?? 0);
                        return hdr2;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GetSessionIdFromRequest: exception while reading headers");
            }

            if (Request.Cookies.TryGetValue(SessionCookieName, out var cookieVal) && !string.IsNullOrWhiteSpace(cookieVal))
            {
                _logger.LogDebug("GetSessionIdFromRequest: found cookie {CookieName} (length={Len})", SessionCookieName, cookieVal?.Length ?? 0);
                return cookieVal;
            }

            _logger.LogDebug("GetSessionIdFromRequest: no session found in headers or cookies");

            return null;
        }

        private string EnsureSessionId()
        {
            if (Request.Cookies.TryGetValue(SessionCookieName, out var sessionId) && !string.IsNullOrWhiteSpace(sessionId))
            {
                return sessionId;
            }

            sessionId = Guid.NewGuid().ToString("N");
            var isHttps = string.Equals(Request.Scheme, "https", StringComparison.OrdinalIgnoreCase);
            Response.Cookies.Append(SessionCookieName, sessionId, new Microsoft.AspNetCore.Http.CookieOptions
            {
                HttpOnly = true,
                Secure = isHttps,
                SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddMinutes(5) 
            });
            return sessionId;
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] JsonElement body)
        {
            var sessionId = EnsureSessionId();
            // expose the proxy session id to clients explicitly
            try { Response.Headers["X-Luca-Proxy-Session"] = sessionId; } catch { }
            var cookieContainer = _cookieJarStore.GetOrCreate(sessionId);
            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = cookieContainer,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            var client = new HttpClient(handler);
            // Allow callers to provide credentials in the request body. If provided, prefer them;
            // otherwise fall back to configured settings.
            string orgCodeVal = _settings.MemberNumber ?? string.Empty;
            string userNameVal = _settings.Username ?? string.Empty;
            string userPasswordVal = _settings.Password ?? string.Empty;
            try
            {
                if (body.ValueKind == JsonValueKind.Object)
                {
                    if (body.TryGetProperty("orgCode", out var o) && o.ValueKind == JsonValueKind.String) { var v = o.GetString(); if (!string.IsNullOrWhiteSpace(v)) orgCodeVal = v; }
                    if (body.TryGetProperty("userName", out var u) && u.ValueKind == JsonValueKind.String) { var v = u.GetString(); if (!string.IsNullOrWhiteSpace(v)) userNameVal = v; }
                    if (body.TryGetProperty("userPassword", out var p) && p.ValueKind == JsonValueKind.String) { var v = p.GetString(); if (!string.IsNullOrWhiteSpace(v)) userPasswordVal = v; }
                }
            }
            catch { }

            var defaultPayload = new
            {
                orgCode = string.IsNullOrWhiteSpace(orgCodeVal) ? (object)"" : orgCodeVal,
                userName = string.IsNullOrWhiteSpace(userNameVal) ? (object)"" : userNameVal,
                userPassword = string.IsNullOrWhiteSpace(userPasswordVal) ? (object)"" : userPasswordVal
            };
            var payloadJson = JsonSerializer.Serialize(defaultPayload);

            var requestUrl = $"{_lucaBaseUrl}/{_settings.Endpoints.Auth}";
            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
            ApplyManualSessionCookie(request);

            _logger.LogDebug("LucaProxy: Sending login request to {Url}. Payload preview: {PayloadPreview}", requestUrl, payloadJson?.Length > 1000 ? payloadJson.Substring(0, 1000) + "..." : payloadJson);

            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            var respPreview = responseContent != null && responseContent.Length > 1000 ? responseContent.Substring(0, 1000) + "..." : responseContent;
            _logger.LogDebug("LucaProxy: Received response from {Url}. Status: {Status}. Body preview: {Preview}", requestUrl, response.StatusCode, respPreview);

            
            object? parsed = null;
            try
            {
                if (!string.IsNullOrEmpty(responseContent))
                    parsed = JsonSerializer.Deserialize<object>(responseContent);
            }
            catch {  }

            
            
            
            try
            {
                if (parsed is JsonElement el && el.ValueKind == JsonValueKind.Object)
                {
                    if (el.TryGetProperty("code", out var codeProp) && codeProp.ValueKind == JsonValueKind.Number && codeProp.GetInt32() != 0)
                    {
                        var msg = el.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString() : "Login failed";
                        _logger.LogWarning("LucaProxy: Login returned code {Code}: {Message}", codeProp.GetInt32(), msg);
                        return Unauthorized(new { raw = parsed, sessionId, message = msg });
                    }
                }
            }
            catch { }

            if (response.IsSuccessStatusCode)
                return Ok(new { raw = parsed, sessionId });

            return StatusCode((int)response.StatusCode, new { raw = parsed, sessionId });
        }


        [HttpPost("branches")]
        public async Task<IActionResult> Branches()
        {
            var sessionId = GetSessionIdFromRequest();
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return Unauthorized(new { message = "Missing LucaProxySession. Please login first (send X-Luca-Session header or login first)." });
            }
            var cookieContainer = _cookieJarStore.GetOrCreate(sessionId);
            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = cookieContainer,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            var client = new HttpClient(handler);
            var requestUrl = $"{_lucaBaseUrl}/{_settings.Endpoints.Branches}";
            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
            ApplyManualSessionCookie(request);

            _logger.LogDebug("LucaProxy: Sending branches request to {Url}", requestUrl);

            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            var respPreview = responseContent != null && responseContent.Length > 1000 ? responseContent.Substring(0, 1000) + "..." : responseContent;
            _logger.LogDebug("LucaProxy: Branches response from {Url}. Status: {Status}. Body preview: {Preview}", requestUrl, response.StatusCode, respPreview);

            _logger.LogInformation("Luca /branches raw response: {Length} chars, status: {Status}", responseContent?.Length ?? 0, response.StatusCode);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Luca /branches non-success response: {Status}. Body: {Body}", response.StatusCode, responseContent);
                return StatusCode((int)response.StatusCode, new { raw = responseContent, status = (int)response.StatusCode, message = "Luca API error" });
            }

            
            if ((responseContent?.Length ?? 0) < 200)
            {
                _logger.LogWarning("Luca /branches suspiciously short response ({Length} chars): {Body}", responseContent?.Length ?? 0, responseContent);
            }

            try
            {
                using var doc = JsonDocument.Parse(responseContent ?? "null");
                var root = doc.RootElement;

                JsonElement? foundArray = null;
                if (root.ValueKind == JsonValueKind.Array) foundArray = root;
                else if (root.TryGetProperty("list", out var list) && list.ValueKind == JsonValueKind.Array) foundArray = list;
                else if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array) foundArray = data;
                else if (root.TryGetProperty("branches", out var branches) && branches.ValueKind == JsonValueKind.Array) foundArray = branches;
                else if (root.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array) foundArray = items;

                if (foundArray.HasValue)
                {
                    
                    
                    var branchesObj = JsonSerializer.Deserialize<object>(foundArray.Value.GetRawText());
                    var rawObj = JsonSerializer.Deserialize<object>(responseContent ?? "null");
                    return Ok(new { branches = branchesObj, raw = rawObj });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse Luca branches response");
            }

            
            return StatusCode((int)response.StatusCode, new { raw = responseContent, status = (int)response.StatusCode });
        }

        [HttpPost("select-branch")]
        public async Task<IActionResult> SelectBranch([FromBody] JsonElement body)
        {
            var sessionId = GetSessionIdFromRequest();
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return Unauthorized(new { message = "Missing LucaProxySession. Please login first (send X-Luca-Session header or login first)." });
            }
            var cookieContainer = _cookieJarStore.GetOrCreate(sessionId);
            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = cookieContainer,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            var client = new HttpClient(handler);
            var requestUrl = $"{_lucaBaseUrl}/{_settings.Endpoints.ChangeBranch}";
            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            var bodyStr = body.ToString();
            request.Content = new StringContent(bodyStr, Encoding.UTF8, "application/json");
            ApplyManualSessionCookie(request);

            _logger.LogDebug("LucaProxy: Sending select-branch request to {Url}. Payload preview: {PayloadPreview}", requestUrl, bodyStr?.Length > 1000 ? bodyStr.Substring(0, 1000) + "..." : bodyStr);

            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            var respPreview = responseContent != null && responseContent.Length > 1000 ? responseContent.Substring(0, 1000) + "..." : responseContent;
            _logger.LogDebug("LucaProxy: Select-branch response from {Url}. Status: {Status}. Body preview: {Preview}", requestUrl, response.StatusCode, respPreview);

            return await ForwardResponse(response);
        }

        [HttpPost("sync-products")]
        public async Task<IActionResult> SyncProducts([FromBody] SyncOptionsDto? options)
        {
            try
            {
                _logger.LogInformation("API /api/luca/sync-products called. Forwarding to SyncService.SyncProductsToLucaAsync");
                var opts = options ?? new SyncOptionsDto();

                // Try to get sessionId from header or cookie; if missing attempt auto-login using configured credentials
                var sessionId = GetSessionIdFromRequest();
                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    _logger.LogInformation("No session provided to sync-products; attempting auto-login using configured credentials");
                    sessionId = await AutoLoginAndReturnSessionId();
                }

                // Ensure branch is selected for this session before syncing
                var branchSelected = await EnsureBranchSelectedForSessionAsync(sessionId, _settings.ForcedBranchId ?? _settings.DefaultBranchId);
                if (!branchSelected)
                {
                    _logger.LogWarning("SyncProducts: Branch selection failed for session {SessionId}", sessionId);

                    // Attempt automatic re-login once and retry branch selection. This helps when the sessionId
                    // exists but the Luca-specific cookies are missing/expired: try to recover automatically.
                    try
                    {
                        _logger.LogInformation("SyncProducts: attempting auto-login and retry branch selection for session {SessionId}", sessionId);
                        var newSession = await AutoLoginAndReturnSessionId();
                        if (!string.IsNullOrWhiteSpace(newSession) && !string.Equals(newSession, sessionId, StringComparison.Ordinal))
                        {
                            sessionId = newSession;
                            _logger.LogInformation("SyncProducts: retrying EnsureBranchSelectedForSessionAsync with new session {SessionId}", sessionId);
                            var retried = await EnsureBranchSelectedForSessionAsync(sessionId, _settings.ForcedBranchId ?? _settings.DefaultBranchId);
                            if (retried)
                            {
                                branchSelected = true;
                                _logger.LogInformation("SyncProducts: branch selection retry succeeded for session {SessionId}", sessionId);
                            }
                            else
                            {
                                _logger.LogWarning("SyncProducts: branch selection retry failed for session {SessionId}", sessionId);
                            }
                        }
                        else if (!string.IsNullOrWhiteSpace(newSession))
                        {
                            // session id unchanged but cookies might have been refreshed; attempt branch selection again
                            var retried = await EnsureBranchSelectedForSessionAsync(sessionId, _settings.ForcedBranchId ?? _settings.DefaultBranchId);
                            if (retried)
                            {
                                branchSelected = true;
                                _logger.LogInformation("SyncProducts: branch selection retry (same session) succeeded for session {SessionId}", sessionId);
                            }
                            else
                            {
                                _logger.LogWarning("SyncProducts: branch selection retry (same session) failed for session {SessionId}", sessionId);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "SyncProducts: automatic retry/login attempt threw an exception for session {SessionId}", sessionId);
                    }

                    if (!branchSelected)
                    {
                        return StatusCode(500, new { message = "Luca branch selection failed before sync; please retry or login manually." });
                    }
                }

                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    return Unauthorized(new { message = "No LucaProxySession found. Please login first via /api/luca/login or provide X-Luca-Session header." });
                }

                var result = await _syncService.SyncProductsToLucaAsync(sessionId, opts);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while handling /api/luca/sync-products");
                return StatusCode(500, new { message = "Server error while syncing products to Luca", error = ex.Message });
            }
        }

        // Try to perform an automatic login with configured credentials and return the sessionId (or null)
        private async Task<string?> AutoLoginAndReturnSessionId()
        {
            try
            {
                var sessionId = EnsureSessionId();
                var cookieContainer = _cookieJarStore.GetOrCreate(sessionId);
                var handler = new HttpClientHandler
                {
                    UseCookies = true,
                    CookieContainer = cookieContainer,
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };
                using var client = new HttpClient(handler);

                var payload = new
                {
                    orgCode = _settings.MemberNumber ?? "",
                    userName = _settings.Username ?? "",
                    userPassword = _settings.Password ?? ""
                };
                var payloadJson = JsonSerializer.Serialize(payload);

                var requestUrl = $"{_lucaBaseUrl}/{_settings.Endpoints.Auth}";
                var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
                ApplyManualSessionCookie(request);

                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Auto-login successful, sessionId: {SessionId}", sessionId);
                    try { Response.Headers["X-Luca-Proxy-Session"] = sessionId; } catch { }
                    // Attempt to select preferred branch immediately after login
                    await EnsureBranchSelectedForSessionAsync(sessionId, _settings.ForcedBranchId ?? _settings.DefaultBranchId);
                    return sessionId;
                }

                _logger.LogWarning("Auto-login failed with status {Status}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auto-login failed");
                return null;
            }
        }

        // Ensure the desired branch is selected for the given proxy session
        private async Task<bool> EnsureBranchSelectedForSessionAsync(string? sessionId, long? preferredBranchId)
        {
            if (string.IsNullOrWhiteSpace(sessionId)) return false;
            if (!preferredBranchId.HasValue) return true;

            try
            {
                _logger.LogDebug("EnsureBranchSelectedForSessionAsync: session={SessionId}, preferredBranch={BranchId}", sessionId, preferredBranchId);

                var cookieContainer = _cookieJarStore.GetOrCreate(sessionId);
                var handler = new HttpClientHandler
                {
                    UseCookies = true,
                    CookieContainer = cookieContainer,
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };

                using var client = new HttpClient(handler);
                var payload = new { orgSirketSubeId = preferredBranchId.Value };
                var payloadJson = JsonSerializer.Serialize(payload);

                var requestUrl = $"{_lucaBaseUrl}/{_settings.Endpoints.ChangeBranch}";
                var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                request.Content = new StringContent(payloadJson, Encoding.GetEncoding(_settings.Encoding ?? "iso-8859-9"), "application/json");
                ApplyManualSessionCookie(request);

                _logger.LogDebug("EnsureBranchSelectedForSessionAsync: sending branch-change request to {Url}. Payload: {PayloadPreview}", requestUrl, payloadJson?.Length > 1000 ? payloadJson.Substring(0, 1000) + "..." : payloadJson);

                // Attempt to list cookies associated with the Luca base URL for debugging
                try
                {
                    var uri = new Uri(_lucaBaseUrl);
                    var cookies = cookieContainer.GetCookies(uri);
                    var cookieSb = new System.Text.StringBuilder();
                    foreach (System.Net.Cookie c in cookies)
                    {
                        cookieSb.Append(c.Name).Append("=").Append(c.Value).Append("; ");
                    }
                    var cookieDump = cookieSb.Length > 0 ? cookieSb.ToString() : "(no cookies)";
                    _logger.LogDebug("EnsureBranchSelectedForSessionAsync: cookies for {Host}: {Cookies}", uri.Host, cookieDump);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "EnsureBranchSelectedForSessionAsync: failed to enumerate cookies for session {SessionId}", sessionId);
                }

                var response = await client.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("EnsureBranchSelectedForSessionAsync: branch-change response status={Status}, length={Len}", response.StatusCode, responseContent?.Length ?? 0);

                var respNonNull = responseContent ?? string.Empty;

                var ok = response.IsSuccessStatusCode &&
                         (respNonNull.Contains("\"code\":0") ||
                          respNonNull.IndexOf("başarı", StringComparison.OrdinalIgnoreCase) >= 0 ||
                          respNonNull.IndexOf("Basari", StringComparison.OrdinalIgnoreCase) >= 0);

                if (!ok)
                {
                    _logger.LogWarning("EnsureBranchSelectedForSession failed. Status: {Status}, Body: {Body}", response.StatusCode, responseContent);
                }
                else
                {
                    _logger.LogInformation("Branch selection succeeded for session {SessionId} to branch {BranchId}", sessionId, preferredBranchId.Value);
                }

                return ok;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "EnsureBranchSelectedForSessionAsync threw an exception");
                return false;
            }
        }
    }
}
