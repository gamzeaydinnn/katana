using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Katana.Core.Interfaces;
using System;
using Microsoft.Extensions.Logging;

namespace Katana.API.Controllers
{
    [ApiController]
    [Route("api/luca")]
    public class LucaProxyController : ControllerBase
    {
        private const string LucaApiBaseUrl = "https://akozas.luca.com.tr/Yetki";
        private const string SessionCookieName = "LucaProxySession";
        private readonly ILucaCookieJarStore _cookieJarStore;
        private readonly ILogger<LucaProxyController> _logger;

        public LucaProxyController(ILucaCookieJarStore cookieJarStore, ILogger<LucaProxyController> logger)
        {
            _cookieJarStore = cookieJarStore;
            _logger = logger;
        }

        // Luca'dan gelen Set-Cookie'leri frontend'e forward eden yardımcı
        private async Task<IActionResult> ForwardResponse(HttpResponseMessage response)
        {
            // Not forwarding Set-Cookie from Luca. They are stored server-side in CookieContainer.
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
            // Prefer explicit header (works cross-origin without relying on cookies)
            if (Request.Headers.TryGetValue("X-Luca-Session", out var headerVals))
            {
                var hdr = headerVals.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(hdr)) return hdr;
            }

            if (Request.Cookies.TryGetValue(SessionCookieName, out var cookieVal) && !string.IsNullOrWhiteSpace(cookieVal))
            {
                return cookieVal;
            }

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
                Expires = DateTimeOffset.UtcNow.AddHours(8)
            });
            return sessionId;
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] JsonElement body)
        {
            var sessionId = EnsureSessionId();
            var cookieContainer = _cookieJarStore.GetOrCreate(sessionId);
            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = cookieContainer,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            var client = new HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Post, $"{LucaApiBaseUrl}/Giris.do");
            request.Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json");
            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Try to deserialize the remote response for convenience
            object? parsed = null;
            try
            {
                parsed = JsonSerializer.Deserialize<object>(responseContent);
            }
            catch { /* ignore */ }

            // Return the remote response along with the server-side sessionId so the frontend
            // can persist it and send it back via X-Luca-Session header on subsequent calls.
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
            var request = new HttpRequestMessage(HttpMethod.Post, $"{LucaApiBaseUrl}/YdlUserResponsibilityOrgSs.do");
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Log raw response for debugging (include a short preview when non-success)
            _logger.LogInformation("Luca /branches raw response: {Length} chars, status: {Status}", responseContent?.Length ?? 0, response.StatusCode);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Luca /branches non-success response: {Status}. Body: {BodyPreview}", response.StatusCode, responseContent?.Length > 1000 ? responseContent?.Substring(0, 1000) + "..." : responseContent);
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
                    // Materialize the found array into a standalone object so we don't
                    // return a JsonElement that references the disposed JsonDocument.
                    var branchesObj = JsonSerializer.Deserialize<object>(foundArray.Value.GetRawText());
                    var rawObj = JsonSerializer.Deserialize<object>(responseContent);
                    return Ok(new { branches = branchesObj, raw = rawObj });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse Luca branches response");
            }

            // Fallback: return raw content wrapped so frontend can reliably inspect the remote body
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
            var request = new HttpRequestMessage(HttpMethod.Post, $"{LucaApiBaseUrl}/GuncelleYtkSirketSubeDegistir.do");
            request.Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json");
            var response = await client.SendAsync(request);
            return await ForwardResponse(response);
        }
    }
}
