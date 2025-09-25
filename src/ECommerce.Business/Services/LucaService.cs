using ECommerce.Core.DTOs;
using ECommerce.Core.Interfaces;
using ECommerce.Business.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;

namespace ECommerce.Business.Services;

public class LucaService : ILucaService
{
    private readonly HttpClient _httpClient;
    private readonly LucaApiSettings _settings;
    private readonly ILogger<LucaService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private string? _authToken;
    private DateTime? _tokenExpiry;

    public LucaService(HttpClient httpClient, IOptions<LucaApiSettings> settings, ILogger<LucaService> logger)
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
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (!string.IsNullOrEmpty(_settings.ApiKey) && !_settings.UseTokenAuth)
        {
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _settings.ApiKey);
        }
    }

    private async Task EnsureAuthenticatedAsync()
    {
        if (!_settings.UseTokenAuth) return;

        if (_authToken == null || _tokenExpiry == null || DateTime.UtcNow >= _tokenExpiry)
        {
            await AuthenticateAsync();
        }
    }

    private async Task AuthenticateAsync()
    {
        try
        {
            _logger.LogInformation("Authenticating with Luca API");

            var authRequest = new
            {
                username = _settings.Username,
                password = _settings.Password
            };

            var json = JsonSerializer.Serialize(authRequest, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_settings.Endpoints.Auth, content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var authResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                _authToken = authResponse.GetProperty("token").GetString();
                var expiresIn = authResponse.GetProperty("expiresIn").GetInt32();
                _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60); // Refresh 1 minute before expiry

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);
                
                _logger.LogInformation("Successfully authenticated with Luca API");
            }
            else
            {
                _logger.LogError("Failed to authenticate with Luca API. Status: {StatusCode}", response.StatusCode);
                throw new UnauthorizedAccessException("Failed to authenticate with Luca API");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating with Luca API");
            throw;
        }
    }

    public async Task<SyncResultDto> SendInvoicesAsync(List<LucaInvoiceDto> invoices)
    {
        var result = new SyncResultDto
        {
            SyncType = "INVOICE",
            ProcessedRecords = invoices.Count
        };

        var startTime = DateTime.UtcNow;

        try
        {
            await EnsureAuthenticatedAsync();
            
            _logger.LogInformation("Sending {Count} invoices to Luca", invoices.Count);

            var json = JsonSerializer.Serialize(invoices, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_settings.Endpoints.Invoices, content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var lucaResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                result.IsSuccess = true;
                result.SuccessfulRecords = invoices.Count;
                result.Message = "Invoices sent successfully to Luca";
                
                _logger.LogInformation("Successfully sent {Count} invoices to Luca", invoices.Count);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                result.IsSuccess = false;
                result.FailedRecords = invoices.Count;
                result.Message = $"Failed to send invoices to Luca: {response.StatusCode}";
                result.Errors.Add(errorContent);
                
                _logger.LogError("Failed to send invoices to Luca. Status: {StatusCode}, Error: {Error}", 
                    response.StatusCode, errorContent);
            }
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.FailedRecords = invoices.Count;
            result.Message = ex.Message;
            result.Errors.Add(ex.ToString());
            
            _logger.LogError(ex, "Error sending invoices to Luca");
        }

        result.Duration = DateTime.UtcNow - startTime;
        return result;
    }

    public async Task<SyncResultDto> SendStockMovementsAsync(List<LucaStockDto> stockMovements)
    {
        var result = new SyncResultDto
        {
            SyncType = "STOCK",
            ProcessedRecords = stockMovements.Count
        };

        var startTime = DateTime.UtcNow;

        try
        {
            await EnsureAuthenticatedAsync();
            
            _logger.LogInformation("Sending {Count} stock movements to Luca", stockMovements.Count);

            var json = JsonSerializer.Serialize(stockMovements, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_settings.Endpoints.Stock, content);

            if (response.IsSuccessStatusCode)
            {
                result.IsSuccess = true;
                result.SuccessfulRecords = stockMovements.Count;
                result.Message = "Stock movements sent successfully to Luca";
                
                _logger.LogInformation("Successfully sent {Count} stock movements to Luca", stockMovements.Count);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                result.IsSuccess = false;
                result.FailedRecords = stockMovements.Count;
                result.Message = $"Failed to send stock movements to Luca: {response.StatusCode}";
                result.Errors.Add(errorContent);
                
                _logger.LogError("Failed to send stock movements to Luca. Status: {StatusCode}, Error: {Error}", 
                    response.StatusCode, errorContent);
            }
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.FailedRecords = stockMovements.Count;
            result.Message = ex.Message;
            result.Errors.Add(ex.ToString());
            
            _logger.LogError(ex, "Error sending stock movements to Luca");
        }

        result.Duration = DateTime.UtcNow - startTime;
        return result;
    }

    public async Task<SyncResultDto> SendCustomersAsync(List<LucaCustomerDto> customers)
    {
        var result = new SyncResultDto
        {
            SyncType = "CUSTOMER",
            ProcessedRecords = customers.Count
        };

        var startTime = DateTime.UtcNow;

        try
        {
            await EnsureAuthenticatedAsync();
            
            _logger.LogInformation("Sending {Count} customers to Luca", customers.Count);

            var json = JsonSerializer.Serialize(customers, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_settings.Endpoints.Customers, content);

            if (response.IsSuccessStatusCode)
            {
                result.IsSuccess = true;
                result.SuccessfulRecords = customers.Count;
                result.Message = "Customers sent successfully to Luca";
                
                _logger.LogInformation("Successfully sent {Count} customers to Luca", customers.Count);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                result.IsSuccess = false;
                result.FailedRecords = customers.Count;
                result.Message = $"Failed to send customers to Luca: {response.StatusCode}";
                result.Errors.Add(errorContent);
                
                _logger.LogError("Failed to send customers to Luca. Status: {StatusCode}, Error: {Error}", 
                    response.StatusCode, errorContent);
            }
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.FailedRecords = customers.Count;
            result.Message = ex.Message;
            result.Errors.Add(ex.ToString());
            
            _logger.LogError(ex, "Error sending customers to Luca");
        }

        result.Duration = DateTime.UtcNow - startTime;
        return result;
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            _logger.LogInformation("Testing connection to Luca API");

            var response = await _httpClient.GetAsync(_settings.Endpoints.Health);
            var isConnected = response.IsSuccessStatusCode;
            
            _logger.LogInformation("Luca API connection test result: {IsConnected}", isConnected);
            return isConnected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing connection to Luca API");
            return false;
        }
    }
}