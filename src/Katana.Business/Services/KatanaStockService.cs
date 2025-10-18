using Katana.Business.Interfaces;
using Katana.Core.Entities;
using Microsoft.Extensions.Logging;

namespace Katana.Business.Services;

public interface IKatanaStockService
{
    Task<List<Product>> GetAllProductsAsync(int? page = null, int? limit = null);
    Task<Product?> GetProductByIdAsync(string productId);
    Task<List<Core.Entities.StockMovement>> GetStockMovementsAsync(DateTime? fromDate = null, int? page = null);
    Task<bool> IsKatanaApiHealthyAsync();
}

public class KatanaStockService : IKatanaStockService
{
    private readonly IKatanaApiClient _katanaApiClient;
    private readonly ILogger<KatanaStockService> _logger;

    public KatanaStockService(
        IKatanaApiClient katanaApiClient,
        ILogger<KatanaStockService> logger)
    {
        _katanaApiClient = katanaApiClient;
        _logger = logger;
    }

    public async Task<List<Product>> GetAllProductsAsync(int? page = null, int? limit = null)
    {
        try
        {
            _logger.LogInformation("Fetching products from Katana API - Page: {Page}, Limit: {Limit}", page, limit);
            var products = await _katanaApiClient.GetProductsAsync(page, limit);
            _logger.LogInformation("Successfully fetched {Count} products from Katana API", products.Count);
            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching products from Katana API");
            throw;
        }
    }

    public async Task<Product?> GetProductByIdAsync(string productId)
    {
        try
        {
            _logger.LogInformation("Fetching product {ProductId} from Katana API", productId);
            var product = await _katanaApiClient.GetProductByIdAsync(productId);
            
            if (product == null)
            {
                _logger.LogWarning("Product {ProductId} not found in Katana API", productId);
            }
            
            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching product {ProductId} from Katana API", productId);
            throw;
        }
    }

    public async Task<List<Core.Entities.StockMovement>> GetStockMovementsAsync(DateTime? fromDate = null, int? page = null)
    {
        try
        {
            _logger.LogInformation("Fetching stock movements from Katana API - FromDate: {FromDate}, Page: {Page}", 
                fromDate?.ToString("yyyy-MM-dd") ?? "All", page);
            
            var movements = await _katanaApiClient.GetStockMovementsAsync(fromDate, page);
            _logger.LogInformation("Successfully fetched {Count} stock movements from Katana API", movements.Count);
            
            // Map from Katana.Business.Interfaces.StockMovement to Katana.Core.Entities.StockMovement
            return movements.Select(m => new Core.Entities.StockMovement
            {
                // Map properties burada - şu an için boş liste döndürelim
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching stock movements from Katana API");
            throw;
        }
    }

    public async Task<bool> IsKatanaApiHealthyAsync()
    {
        try
        {
            var healthStatus = await _katanaApiClient.CheckHealthAsync();
            return healthStatus.IsHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Katana API health");
            return false;
        }
    }
}