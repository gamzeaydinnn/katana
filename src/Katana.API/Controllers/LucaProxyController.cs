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
            return await ForwardResponse(response);
        }


        [HttpPost("branches")]
        public async Task<IActionResult> Branches()
        {
            var sessionId = Request.Cookies[SessionCookieName];
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return Unauthorized(new { message = "Missing LucaProxySession. Please login first." });
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

            // Log raw response for debugging
            _logger.LogInformation("Luca /branches raw response: {Length} chars", responseContent?.Length ?? 0);

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
                    // Return normalized shape
                    return Ok(new { branches = foundArray.Value, raw = JsonSerializer.Deserialize<object>(responseContent) });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse Luca branches response");
            }

            // Fallback: return raw content as-is (preserve status code)
            return new ContentResult
            {
                Content = responseContent,
                ContentType = "application/json",
                StatusCode = (int)response.StatusCode
            };
        }

        [HttpPost("select-branch")]
        public async Task<IActionResult> SelectBranch([FromBody] JsonElement body)
        {
            var sessionId = Request.Cookies[SessionCookieName];
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return Unauthorized(new { message = "Missing LucaProxySession. Please login first." });
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
