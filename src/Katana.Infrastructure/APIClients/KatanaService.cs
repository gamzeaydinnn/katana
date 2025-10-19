using Katana.Business.DTOs;
using Katana.Business.Interfaces;
using Katana.Core.Enums;
using Katana.Data.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;
using Katana.Core.DTOs;
/*Katana API'sından veri okuma operasyonlarını içerecek.

Amacı: Sadece Katana'dan veri okumaktan sorumlu olmak.

Sorumlulukları (Yeni):

Belirli bir tarihten sonraki stok hareketlerini getirme metodu.

Ödeme durumu değişen faturaları getirme metodu.*/
namespace Katana.Infrastructure.APIClients;

public class KatanaService : IKatanaService
{
    private readonly HttpClient _httpClient;
    private readonly KatanaApiSettings _settings;
    private readonly ILogger<KatanaService> _logger;
    private readonly ILoggingService _loggingService;
    private readonly JsonSerializerOptions _jsonOptions;

    public KatanaService(HttpClient httpClient, IOptions<KatanaApiSettings> settings, ILogger<KatanaService> logger, ILoggingService loggingService)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
        _loggingService = loggingService;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);

        if (!string.IsNullOrEmpty(_settings.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        }

        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<List<KatanaStockDto>> GetStockChangesAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            _logger.LogInformation("Getting stock changes from {FromDate} to {ToDate}", fromDate, toDate);

            var url = $"{_settings.Endpoints.Stock}?fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}";
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                var stockChanges = JsonSerializer.Deserialize<List<KatanaStockDto>>(jsonContent, _jsonOptions) ?? new List<KatanaStockDto>();
                
                _logger.LogInformation("Retrieved {Count} stock changes from Katana", stockChanges.Count);
                return stockChanges;
            }

            _logger.LogWarning("Failed to get stock changes from Katana. Status: {StatusCode}, Reason: {ReasonPhrase}", 
                response.StatusCode, response.ReasonPhrase);
            return new List<KatanaStockDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock changes from Katana API");
            throw;
        }
    }

    public async Task<List<KatanaProductDto>> GetProductsAsync()
    {
        try
        {
            _logger.LogInformation("Getting products from Katana");
            _loggingService.LogInfo("Katana API: Fetching products", null, "GetProductsAsync", LogCategory.ExternalAPI);

            var response = await _httpClient.GetAsync(_settings.Endpoints.Products);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Katana API failed. Status: {StatusCode}, Response: {Response}", 
                    response.StatusCode, responseContent);
                return new List<KatanaProductDto>();
            }

            _logger.LogDebug("Katana API Response: {Response}", responseContent);
            var apiResponse = JsonSerializer.Deserialize<KatanaApiResponse>(responseContent, _jsonOptions);
            var products = apiResponse?.Data ?? new List<KatanaProductDto>();
            
            _logger.LogInformation("Retrieved {Count} products from Katana", products.Count);
            _loggingService.LogInfo($"Successfully fetched {products.Count} products from Katana", null, "GetProductsAsync", LogCategory.ExternalAPI);
            return products;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Katana API connection error: {Message}", ex.Message);
            _loggingService.LogError("Katana API connection failed", ex, null, "GetProductsAsync", LogCategory.ExternalAPI);
            return new List<KatanaProductDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Katana API");
            _loggingService.LogError("Unexpected error in Katana API call", ex, null, "GetProductsAsync", LogCategory.ExternalAPI);
            return new List<KatanaProductDto>();
        }
    }

    private class KatanaApiResponse
    {
        public List<KatanaProductDto>? Data { get; set; }
    }

    public async Task<List<KatanaInvoiceDto>> GetInvoicesAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            _logger.LogInformation("Getting invoices from {FromDate} to {ToDate}", fromDate, toDate);

            var url = $"{_settings.Endpoints.Invoices}?fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}";
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                var invoices = JsonSerializer.Deserialize<List<KatanaInvoiceDto>>(jsonContent, _jsonOptions) ?? new List<KatanaInvoiceDto>();
                
                _logger.LogInformation("Retrieved {Count} invoices from Katana", invoices.Count);
                return invoices;
            }

            _logger.LogWarning("Failed to get invoices from Katana. Status: {StatusCode}, Reason: {ReasonPhrase}", 
                response.StatusCode, response.ReasonPhrase);
            return new List<KatanaInvoiceDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoices from Katana API");
            throw;
        }
    }

    public async Task<KatanaProductDto?> GetProductBySkuAsync(string sku)
    {
        try
        {
            _logger.LogInformation("Getting product by SKU: {SKU}", sku);

            var url = $"{_settings.Endpoints.Products}/{sku}";
            var response = await _httpClient.GetAsync(url);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Product with SKU {SKU} not found", sku);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Katana API failed for SKU {SKU}. Status: {StatusCode}", 
                    sku, response.StatusCode);
                return null;
            }

            var product = JsonSerializer.Deserialize<KatanaProductDto>(responseContent, _jsonOptions);
            _logger.LogInformation("Retrieved product {SKU}", sku);
            return product;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Katana API connection error for SKU {SKU}", sku);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting product {SKU}", sku);
            return null;
        }
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            _logger.LogInformation("Testing Katana API connection");

            var response = await _httpClient.GetAsync($"{_settings.Endpoints.Products}?limit=1");
            var isConnected = response.IsSuccessStatusCode;
            
            _logger.LogInformation("Katana API connection: {Status}", isConnected ? "OK" : "Failed");
            return isConnected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing connection to Katana API");
            return false;
        }
    }
}
