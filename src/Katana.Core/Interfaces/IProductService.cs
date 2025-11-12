using Katana.Core.DTOs;

namespace Katana.Core.Interfaces;

public interface IProductService
{
    Task<IEnumerable<ProductDto>> GetAllProductsAsync();
    Task<IEnumerable<ProductSummaryDto>> GetActiveProductsAsync();
    Task<ProductDto?> GetProductByIdAsync(int id);
    Task<ProductDto?> GetProductBySkuAsync(string sku);
    Task<IEnumerable<ProductDto>> GetProductsByCategoryAsync(int categoryId);
    Task<IEnumerable<ProductDto>> SearchProductsAsync(string searchTerm);
    Task<IEnumerable<ProductDto>> GetLowStockProductsAsync(int threshold = 10);
    Task<IEnumerable<ProductDto>> GetOutOfStockProductsAsync();
    Task<ProductDto> CreateProductAsync(CreateProductDto dto);
    Task<ProductDto> UpdateProductAsync(int id, UpdateProductDto dto);
    Task<bool> UpdateStockAsync(int id, int quantity);
    Task<bool> DeleteProductAsync(int id);
    Task<bool> ActivateProductAsync(int id);
    Task<bool> DeactivateProductAsync(int id);
    Task<ProductStatisticsDto> GetProductStatisticsAsync();
    Task<(int created, int updated, int skipped, List<string> errors)> BulkSyncProductsAsync(IEnumerable<CreateProductDto> products, int defaultCategoryId);
    
}
