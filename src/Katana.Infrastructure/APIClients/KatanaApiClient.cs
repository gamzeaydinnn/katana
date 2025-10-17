using System.Net.Http.Headers;
using System.Text.Json;
using Katana.Business.Interfaces;
using Katana.Core.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Katana.Infrastructure.ExternalServices.Katana;

public class KatanaApiClient : IKatanaApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<KatanaApiClient> _logger;
    private readonly string _apiKey;
    private readonly string _baseUrl;

    public KatanaApiClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<KatanaApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        _baseUrl = configuration["KatanaApi:BaseUrl"] ?? throw new InvalidOperationException("KatanaApi:BaseUrl not configured");
        _apiKey = configuration["KatanaApi:ApiKey"] ?? throw new InvalidOperationException("KatanaApi:ApiKey not configured");
        
        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        
        _logger.LogInformation("KatanaApiClient initialized with BaseUrl: {BaseUrl}", _baseUrl);
    }

    public async Task<List<Product>> GetProductsAsync(int? page = null, int? limit = null)
    {
        try
        {
            var queryParams = new List<string>();
            if (page.HasValue) queryParams.Add($"page={page}");
            if (limit.HasValue) queryParams.Add($"limit={limit}");
            
            var query = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
            var endpoint = $"/v1/products{query}";
            
            _logger.LogInformation("Fetching products from Katana API: {Endpoint}", endpoint);
            _logger.LogInformation("API Key (first 10 chars): {ApiKey}", _apiKey[..Math.Min(10, _apiKey.Length)]);
            
            var response = await _httpClient.GetAsync(endpoint);
            
            _logger.LogInformation("Katana API Response Status: {StatusCode}", response.StatusCode);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Katana API Error Response: {StatusCode} - {Content}", response.StatusCode, errorContent);
                response.EnsureSuccessStatusCode();
            }
            
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Katana API Response received, length: {Length}", content.Length);
            
            var result = JsonSerializer.Deserialize<KatanaProductsResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            return result?.Data ?? new List<Product>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching products from Katana API");
            throw new InvalidOperationException("Failed to fetch products from Katana API", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching products from Katana API");
            throw;
        }
    }

    public async Task<Product?> GetProductByIdAsync(string productId)
    {
        try
        {
            var endpoint = $"/v1/products/{productId}";
            _logger.LogInformation("Fetching product {ProductId} from Katana API", productId);
            
            var response = await _httpClient.GetAsync(endpoint);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var product = JsonSerializer.Deserialize<Product>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching product {ProductId} from Katana API", productId);
            throw;
        }
    }

    public async Task<List<StockMovement>> GetStockMovementsAsync(DateTime? fromDate = null, int? page = null)
    {
        try
        {
            var queryParams = new List<string>();
            if (fromDate.HasValue) queryParams.Add($"from_date={fromDate:yyyy-MM-dd}");
            if (page.HasValue) queryParams.Add($"page={page}");
            
            var query = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
            var endpoint = $"/v1/stock-movements{query}";
            
            _logger.LogInformation("Fetching stock movements from Katana API: {Endpoint}", endpoint);
            
            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<KatanaStockMovementsResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            return result?.Data ?? new List<StockMovement>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching stock movements from Katana API");
            throw;
        }
    }

    public async Task<ApiHealthStatus> CheckHealthAsync()
    {
        try
        {
            _logger.LogInformation("Starting Katana API health check...");
            var response = await _httpClient.GetAsync("/v1/products?limit=1");
            
            _logger.LogInformation("Health check response: {StatusCode}", response.StatusCode);
            
            return new ApiHealthStatus
            {
                IsHealthy = response.IsSuccessStatusCode,
                Message = response.IsSuccessStatusCode ? "API is healthy" : $"API returned status: {response.StatusCode}",
                CheckedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for Katana API");
            return new ApiHealthStatus
            {
                IsHealthy = false,
                Message = $"Health check failed: {ex.Message}",
                CheckedAt = DateTime.UtcNow
            };
        }
    }
}

// Response DTOs for Katana API
internal class KatanaProductsResponse
{
    public List<Product> Data { get; set; } = new();
    public PaginationInfo? Meta { get; set; }
}

internal class KatanaStockMovementsResponse
{
    public List<StockMovement> Data { get; set; } = new();
    public PaginationInfo? Meta { get; set; }
}

internal class PaginationInfo
{
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
}