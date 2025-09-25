using ECommerce.Core.DTOs;
using ECommerce.Core.Interfaces;
using ECommerce.Business.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;

namespace ECommerce.Business.Services;

public class KatanaService : IKatanaService
{
    private readonly HttpClient _httpClient;
    private readonly KatanaApiSettings _settings;
    private readonly ILogger<KatanaService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public KatanaService(HttpClient httpClient, IOptions<KatanaApiSettings> settings, ILogger<KatanaService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
        
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

        if (_settings.UseBasicAuth && !string.IsNullOrEmpty(_settings.Username))
        {
            var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.Username}:{_settings.Password}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
        }
        else if (!string.IsNullOrEmpty(_settings.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _settings.ApiKey);
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

            var response = await _httpClient.GetAsync(_settings.Endpoints.Products);

            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                var products = JsonSerializer.Deserialize<List<KatanaProductDto>>(jsonContent, _jsonOptions) ?? new List<KatanaProductDto>();
                
                _logger.LogInformation("Retrieved {Count} products from Katana", products.Count);
                return products;
            }

            _logger.LogWarning("Failed to get products from Katana. Status: {StatusCode}, Reason: {ReasonPhrase}", 
                response.StatusCode, response.ReasonPhrase);
            return new List<KatanaProductDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products from Katana API");
            throw;
        }
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

            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                var product = JsonSerializer.Deserialize<KatanaProductDto>(jsonContent, _jsonOptions);
                
                _logger.LogInformation("Retrieved product {SKU} from Katana", sku);
                return product;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Product with SKU {SKU} not found in Katana", sku);
                return null;
            }

            _logger.LogWarning("Failed to get product {SKU} from Katana. Status: {StatusCode}, Reason: {ReasonPhrase}", 
                sku, response.StatusCode, response.ReasonPhrase);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product {SKU} from Katana API", sku);
            throw;
        }
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            _logger.LogInformation("Testing connection to Katana API");

            var response = await _httpClient.GetAsync(_settings.Endpoints.Health);
            var isConnected = response.IsSuccessStatusCode;
            
            _logger.LogInformation("Katana API connection test result: {IsConnected}", isConnected);
            return isConnected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing connection to Katana API");
            return false;
        }
    }
}