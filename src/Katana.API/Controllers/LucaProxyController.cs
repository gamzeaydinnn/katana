using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Katana.API.Controllers
{
    [ApiController]
    [Route("api/luca")]
    public class LucaProxyController : ControllerBase
    {
        private const string LucaApiBaseUrl = "https://akozas.luca.com.tr/Yetki";

        // Luca'dan gelen Set-Cookie'leri frontend'e forward eden yardımcı
        private async Task<IActionResult> ForwardResponse(HttpResponseMessage response)
        {
            if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                foreach (var cookie in cookies)
                {
                    Response.Headers.Append("Set-Cookie", cookie);
                }
            }
            var responseContent = await response.Content.ReadAsStringAsync();
            return new ContentResult
            {
                Content = responseContent,
                ContentType = "application/json",
                StatusCode = (int)response.StatusCode
            };
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] JsonElement body)
        {
            var handler = new HttpClientHandler { UseCookies = false };
            var client = new HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Post, $"{LucaApiBaseUrl}/Giris.do");
            request.Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json");
            // Tarayıcıdan gelen Cookie'yi Luca'ya forward et
            if (Request.Headers.TryGetValue("Cookie", out var cookieHeader))
            {
                request.Headers.Add("Cookie", cookieHeader.ToString());
            }
            var response = await client.SendAsync(request);
            return await ForwardResponse(response);
        }


        [HttpPost("branches")]
        public async Task<IActionResult> Branches()
        {
            var handler = new HttpClientHandler { UseCookies = false };
            var client = new HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Post, $"{LucaApiBaseUrl}/YdlUserResponsibilityOrgSs.do");
            // Tarayıcıdan gelen Cookie'yi Luca'ya forward et
            if (Request.Headers.TryGetValue("Cookie", out var cookieHeader))
            {
                request.Headers.Add("Cookie", cookieHeader.ToString());
            }
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
            var response = await client.SendAsync(request);
            return await ForwardResponse(response);
        }

        [HttpPost("select-branch")]
        public async Task<IActionResult> SelectBranch([FromBody] JsonElement body)
        {
            var handler = new HttpClientHandler { UseCookies = false };
            var client = new HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Post, $"{LucaApiBaseUrl}/GuncelleYtkSirketSubeDegistir.do");
            if (Request.Headers.TryGetValue("Cookie", out var cookieHeader))
            {
                request.Headers.Add("Cookie", cookieHeader.ToString());
            }
            request.Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json");
            var response = await client.SendAsync(request);
            return await ForwardResponse(response);
        }
    }
}
