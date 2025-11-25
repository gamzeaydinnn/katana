using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Katana.Data.Configuration;
using Katana.Infrastructure.APIClients;
using Katana.Core.DTOs;
using System.Net;
using System.Net.Http;
using System.Text;

namespace LucaRunner;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Simple arg parsing / fallback to env vars
        string baseUrl = GetArg(args, "--baseUrl") ?? Environment.GetEnvironmentVariable("LUCA_BASEURL") ?? "http://85.111.1.49:57005/Yetki/";
        string sessionCookie = GetArg(args, "--sessionCookie") ?? Environment.GetEnvironmentVariable("LUCA_SESSION_COOKIE") ?? string.Empty;
        string forcedBranch = GetArg(args, "--forcedBranch") ?? Environment.GetEnvironmentVariable("LUCA_FORCED_BRANCH") ?? string.Empty;
        string sku = GetArg(args, "--sku") ?? Environment.GetEnvironmentVariable("LUCA_SKU") ?? "RUNNER-SKU-" + DateTime.Now.ToString("yyyyMMddHHmmss");
        string name = GetArg(args, "--name") ?? Environment.GetEnvironmentVariable("LUCA_NAME") ?? "Runner Product";
        var olcumBirimiIdStr = GetArg(args, "--olcumBirimiId") ?? Environment.GetEnvironmentVariable("LUCA_OLCUM_ID") ?? "5";
        if (!long.TryParse(olcumBirimiIdStr, out var olcumBirimiId)) olcumBirimiId = 5;
        var useRawHttp = GetArg(args, "--raw") != null;

        Console.WriteLine("Luca Runner starting...");
        Console.WriteLine($"BaseUrl: {baseUrl}");
        Console.WriteLine($"Session cookie provided: {(string.IsNullOrEmpty(sessionCookie) ? "NO" : "YES")}");
        Console.WriteLine($"ForcedBranchId: {(string.IsNullOrEmpty(forcedBranch) ? "(none)" : forcedBranch)}");
        Console.WriteLine($"SKU: {sku}");
        Console.WriteLine($"Name: {name}");

        if (useRawHttp)
        {
            Console.WriteLine("Running RAW HttpClient flow (login -> change branch -> sample invoice)...");
            return await RunRawHttpFlow(baseUrl, forcedBranch, sku, name);
        }

        // Build configuration for LucaApiSettings
        var inMemory = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["LucaApi:BaseUrl"] = baseUrl,
            ["LucaApi:UseTokenAuth"] = "false"
        };
        if (!string.IsNullOrEmpty(sessionCookie)) inMemory["LucaApi:ManualSessionCookie"] = sessionCookie;
        if (!string.IsNullOrEmpty(forcedBranch)) inMemory["LucaApi:ForcedBranchId"] = forcedBranch;

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemory)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddSimpleConsole(options => { options.SingleLine = true; options.TimestampFormat = "HH:mm:ss "; }));
        services.Configure<LucaApiSettings>(config.GetSection("LucaApi"));

        // Register ILucaService using HttpClient factory so LucaService receives a configured HttpClient
        services.AddHttpClient<ILucaService, LucaService>(client =>
        {
            if (!baseUrl.EndsWith("/")) baseUrl += "/";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<Program>>();

        try
        {
            var luca = provider.GetRequiredService<ILucaService>();

            var request = new LucaCreateStokKartiRequest
            {
                KartAdi = name,
                KartKodu = sku,
                OlcumBirimiId = olcumBirimiId,
                BaslangicTarihi = DateTime.Today,
                KartTuru = 1,
                PerakendeSatisBirimFiyat = 100M,
                PerakendeAlisBirimFiyat = 80M,
                UzunAdi = name
            };

            Console.WriteLine("Sending stock card via ILucaService.CreateStockCardAsync...");
            var resp = await luca.CreateStockCardAsync(request);
            Console.WriteLine("Response:");
            Console.WriteLine(resp.ToString());

            Console.WriteLine("Runner finished.");
            Console.WriteLine("Check logs: scripts/logs/ and src/Katana.API/logs/ for raw HTTP artifacts.");
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running Luca push");
            Console.WriteLine($"Error: {ex.Message}");
            return 2;
        }
    }

    static string? GetArg(string[] args, string name)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                return args[i + 1];
        }
        return null;
    }

    static async Task<int> RunRawHttpFlow(string baseUrl, string forcedBranch, string sku, string name)
    {
        if (!baseUrl.EndsWith("/")) baseUrl += "/";
        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            UseCookies = true,
            AllowAutoRedirect = true
        };
        using var client = new HttpClient(handler) { BaseAddress = new Uri(baseUrl) };

        // 1) Login
        var loginJson = "{ \"orgCode\": \"1422649\", \"userName\": \"Admin\", \"userPassword\": \"WebServis\" }";
        var loginResp = await client.PostAsync("Giris.do", new StringContent(loginJson, Encoding.UTF8, "application/json"));
        var loginBody = await loginResp.Content.ReadAsStringAsync();
        Console.WriteLine("LOGIN RESPONSE:");
        Console.WriteLine(loginBody);
        loginResp.EnsureSuccessStatusCode();

        var cookies = handler.CookieContainer.GetCookies(client.BaseAddress);
        string? jsessionId = null;
        foreach (Cookie c in cookies)
        {
            if (c.Name == "JSESSIONID")
            {
                jsessionId = c.Value;
                break;
            }
        }
        Console.WriteLine($"JSESSIONID: {jsessionId ?? "(not found)"}");

        // 2) Change branch
        var branchId = !string.IsNullOrWhiteSpace(forcedBranch) ? forcedBranch : "854";
        var branchJson = $"{{ \"orgSirketSubeId\": {branchId} }}";
        var branchResp = await client.PostAsync("GuncelleYtkSirketSubeDegistir.do", new StringContent(branchJson, Encoding.UTF8, "application/json"));
        var branchBody = await branchResp.Content.ReadAsStringAsync();
        Console.WriteLine("BRANCH CHANGE RESPONSE:");
        Console.WriteLine(branchBody);
        branchResp.EnsureSuccessStatusCode();

        // 3) Sample invoice create (EkleFtrWsFaturaBaslik.do)
        var invoiceJson = $@"{{
""belgeSeri"": ""A"",
""belgeTarihi"": ""{DateTime.Today:dd/MM/yyyy}"",
""duzenlemeSaati"": ""11:09"",
""vadeTarihi"": ""{DateTime.Today:dd/MM/yyyy}"",
""belgeAciklama"": ""Runner Test Invoice"",
""belgeTurDetayId"": ""76"",
""faturaTur"": ""1"",
""paraBirimKod"": ""USD"",
""kdvFlag"": true,
""musteriTedarikci"": ""1"",
""kurBedeli"": 48.6592,
""detayList"": [
  {{
    ""kartTuru"": 1,
    ""kartAdi"": ""{name}"",
    ""kartKodu"": ""{sku}"",
    ""olcuBirimi"": 1,
    ""birimFiyat"": 32.802,
    ""kartAlisKdvOran"": 0.2,
    ""kartSatisKdvOran"": 0.2,
    ""kartTipi"": 1,
    ""miktar"": 4,
    ""tutar"": 500.00,
    ""kdvOran"": 0.1,
    ""depoKodu"": ""003.0001""
  }}
],
""cariKodu"": ""00000017"",
""cariTip"": 1,
""cariTanim"": ""VOLKAN ÜNAL"",
""cariKisaAd"": ""VOLKAN ÜNAL"",
""cariYasalUnvan"": ""VOLKAN ÜNAL"",
""vergiNo"": ""12"",
""il"": ""ANKARA"",
""ilce"": ""ÇANKAYA"",
""odemeTipi"": ""KREDIKARTI_BANKAKARTI"",
""gonderimTipi"": ""ELEKTRONIK"",
""efaturaTuru"": 1
}}";

        var invoiceResp = await client.PostAsync("EkleFtrWsFaturaBaslik.do", new StringContent(invoiceJson, Encoding.UTF8, "application/json"));
        var invoiceBody = await invoiceResp.Content.ReadAsStringAsync();
        Console.WriteLine("INVOICE RESPONSE:");
        Console.WriteLine(invoiceBody);
        invoiceResp.EnsureSuccessStatusCode();

        return 0;
    }
}
