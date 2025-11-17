using Katana.Core.Entities;

namespace Katana.Business.Interfaces;

public interface IKatanaApiClient
{
    Task<List<Product>> GetProductsAsync(int? page = null, int? limit = null);
    Task<Product?> GetProductByIdAsync(string productId);
    Task<List<StockMovement>> GetStockMovementsAsync(DateTime? fromDate = null, int? page = null);
    Task<ApiHealthStatus> CheckHealthAsync();
}

public class ApiHealthStatus
{
    public bool IsHealthy { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; }
}

public class StockMovement
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
}