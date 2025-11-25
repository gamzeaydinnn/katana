using Katana.Core.Entities;
using Katana.Core.DTOs;

namespace Katana.Business.Interfaces;

public interface IKatanaApiClient
{
    Task<List<Product>> GetProductsAsync(int? page = null, int? limit = null);
    Task<Product?> GetProductByIdAsync(string productId);
    Task<List<StockMovement>> GetStockMovementsAsync(DateTime? fromDate = null, int? page = null);
    Task<List<Stock>> GetStockAdjustmentsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<InventoryMovement>> GetInventoryMovementsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<Invoice>> GetInvoicesAsync(DateTime fromDate, DateTime toDate);
    Task<List<PurchaseOrder>> GetPurchaseOrdersAsync(string? status = null, DateTime? fromDate = null);
    Task<PurchaseOrder?> GetPurchaseOrderByIdAsync(string id);
    Task<string?> ReceivePurchaseOrderAsync(string id);
    Task<List<Order>> GetSalesOrdersAsync(DateTime? fromDate = null);
    Task<Order?> CreateSalesOrderAsync(Order order);
    Task<Order?> UpdateSalesOrderAsync(Order order);
    Task<List<ServiceDto>> GetServicesAsync();
    Task<List<TaxRateDto>> GetTaxRatesAsync();
    Task<List<PriceListDto>> GetPriceListsAsync();
    Task<List<WebhookDto>> GetWebhooksAsync();
    Task<List<SerialNumberDto>> GetSerialNumbersAsync();
    Task<List<UserDto>> GetUsersAsync();
    Task<List<BomRowDto>> GetBomRowsAsync();
    Task<List<MaterialDto>> GetMaterialsAsync();
    Task<List<Supplier>> GetSuppliersAsync();
    Task<Supplier?> GetSupplierByIdAsync(string id);
    Task<List<ManufacturingOrder>> GetManufacturingOrdersAsync(string? status = null);
    Task<ManufacturingOrder?> GetManufacturingOrderByIdAsync(string id);
    Task<List<ProductVariant>> GetVariantsAsync(string? productId = null);
    Task<ProductVariant?> GetVariantAsync(string variantId);
    Task<List<Batch>> GetBatchesAsync(string? productId = null);
    Task<List<StockTransfer>> GetStockTransfersAsync(string? status = null);
    Task<List<Order>> GetSalesReturnsAsync(DateTime? fromDate = null);
    Task<ApiHealthStatus> CheckHealthAsync();
    Task<bool> UpdateProductAsync(int katanaProductId, string name, decimal? salesPrice, int? stock);
}

public class ApiHealthStatus
{
    public bool IsHealthy { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; }
}
