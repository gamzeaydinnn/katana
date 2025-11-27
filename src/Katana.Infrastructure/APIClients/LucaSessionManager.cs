using System;
using System.Linq;
using System.Threading.Tasks;
using Katana.Data.Configuration;
using Microsoft.Playwright;
using Microsoft.Extensions.Logging;

namespace Katana.Infrastructure.APIClients
{
    public class LucaSessionManager
    {
        private readonly LucaApiSettings _settings;
        private readonly ILogger _logger;

        public LucaSessionManager(LucaApiSettings settings, ILogger logger)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger;
        }

        public async Task<string> GetFreshCookieAsync()
        {
            _logger.LogInformation("LucaSessionManager: starting headless browser login to obtain JSESSIONID");

            using var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox" }
            });

            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                // Accept compressed content and emulate a normal browser UA
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36"
            });

            var page = await context.NewPageAsync();

            var loginUrl = (_settings.BaseUrl?.TrimEnd('/') ?? string.Empty) + "/Giris.do";
            _logger.LogDebug("LucaSessionManager: navigating to {Url}", loginUrl);

            await page.GotoAsync(loginUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });

            try
            {
                // Fill form fields - selectors may need adjustment if the login page differs
                await page.FillAsync("input[name='uyeNo']", _settings.MemberNumber ?? string.Empty);
                await page.FillAsync("input[name='kullaniciAdi']", _settings.Username ?? string.Empty);
                await page.FillAsync("input[name='sifre']", _settings.Password ?? string.Empty);

                await Task.WhenAll(
                    page.ClickAsync("button[type='submit']"),
                    page.WaitForLoadStateAsync(LoadState.NetworkIdle)
                );

                // Small delay to allow cookies to be set
                await page.WaitForTimeoutAsync(1000);

                var cookies = await context.CookiesAsync();
                var jsession = cookies.FirstOrDefault(c => string.Equals(c.Name, "JSESSIONID", StringComparison.OrdinalIgnoreCase))?.Value;

                if (string.IsNullOrWhiteSpace(jsession))
                {
                    var all = string.Join(";", cookies.Select(c => c.Name + "=" + c.Value));
                    _logger.LogWarning("LucaSessionManager: JSESSIONID not found in cookies. Cookies present: {Cookies}", all);
                    throw new InvalidOperationException("JSESSIONID not found after headless login");
                }

                _logger.LogInformation("LucaSessionManager: obtained JSESSIONID (preview): {Preview}", jsession.Length > 12 ? jsession.Substring(0, 12) + "..." : jsession);
                await browser.CloseAsync();
                return jsession!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LucaSessionManager: headless login failed");
                try { await browser.CloseAsync(); } catch { }
                throw;
            }
        }
    }
}
