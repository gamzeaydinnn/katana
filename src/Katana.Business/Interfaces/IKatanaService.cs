using Katana.Business.Interfaces;
using System;
using Katana.Core.DTOs;

namespace Katana.Business.Interfaces;

public interface IKatanaService
{
    /// <summary>
     /// Upsert stock for a SKU in Katana: create product if missing, otherwise increase stock.
     /// </summary>
    Task<bool> SyncProductStockAsync(string sku, decimal quantity, long? locationId = null, string? productName = null, decimal? salesPrice = null);

    Task<List<KatanaStockDto>> GetStockChangesAsync(DateTime fromDate, DateTime toDate);
    Task<List<KatanaProductDto>> GetProductsAsync();
    Task<List<KatanaCustomerDto>> GetCustomersAsync();
    Task<KatanaCustomerDto?> GetCustomerByIdAsync(int customerId);
    Task<KatanaCustomerDto?> CreateCustomerAsync(KatanaCustomerDto customer);
    Task<KatanaCustomerDto?> UpdateCustomerAsync(int customerId, KatanaCustomerDto customer);
    Task<bool> DeleteCustomerAsync(int customerId);
    Task<List<KatanaInvoiceDto>> GetInvoicesAsync(DateTime fromDate, DateTime toDate);
    Task<KatanaProductDto?> GetProductBySkuAsync(string sku);
    Task<KatanaProductDto?> GetProductByIdAsync(int productId);
    Task<bool> TestConnectionAsync();
    Task<bool> UpdateProductAsync(int katanaProductId, string name, decimal? salesPrice, int? stock);
    Task<KatanaProductDto?> CreateProductAsync(KatanaProductDto product);
    Task<bool> DeleteProductAsync(int productId);
    Task<List<KatanaPurchaseOrderDto>> GetPurchaseOrdersAsync(string? status = null, DateTime? fromDate = null);
    Task<KatanaPurchaseOrderDto?> GetPurchaseOrderByIdAsync(string id);
    Task<string?> ReceivePurchaseOrderAsync(string id);
    
    // Supplier operations
    Task<List<KatanaSupplierDto>> GetSuppliersAsync();
    Task<KatanaSupplierDto?> GetSupplierByIdAsync(string id);
    Task<KatanaSupplierDto?> CreateSupplierAsync(KatanaSupplierDto supplier);
    Task<KatanaSupplierDto?> UpdateSupplierAsync(int supplierId, KatanaSupplierDto supplier);
    Task<bool> DeleteSupplierAsync(int supplierId);
    
    // Supplier Address operations (optional for now)
    Task<List<KatanaSupplierAddressDto>> GetSupplierAddressesAsync(int? supplierId = null);
    Task<KatanaSupplierAddressDto?> CreateSupplierAddressAsync(KatanaSupplierAddressDto address);
    Task<KatanaSupplierAddressDto?> UpdateSupplierAddressAsync(int addressId, KatanaSupplierAddressDto address);
    Task<bool> DeleteSupplierAddressAsync(int addressId);
    
    Task<List<KatanaManufacturingOrderDto>> GetManufacturingOrdersAsync(string? status = null);
    Task<KatanaManufacturingOrderDto?> GetManufacturingOrderByIdAsync(string id);
    Task<KatanaVariantDto?> GetVariantAsync(string variantId);
    Task<List<KatanaVariantDto>> GetVariantsAsync(string? productId = null);
    
    /// <summary>
    /// SKU ile variant ID'sini bulur
    /// </summary>
    Task<long?> FindVariantIdBySkuAsync(string sku);
    Task<List<KatanaBatchDto>> GetBatchesAsync(string? productId = null);
    Task<List<KatanaStockTransferDto>> GetStockTransfersAsync(string? status = null);
    Task<List<KatanaSalesReturnDto>> GetSalesReturnsAsync(DateTime? fromDate = null);
    Task<List<SalesOrderDto>> GetSalesOrdersAsync(DateTime? fromDate = null);
    Task<string?> GetVariantSkuAsync(long variantId);
    
    /// <summary>
    /// Variant ID'den hem SKU hem ProductName döndürür.
    /// Önce /variants/{id} endpoint'inden SKU ve product_id alır,
    /// sonra /products/{product_id} endpoint'inden ProductName alır.
    /// </summary>
    /// <param name="variantId">Katana variant ID</param>
    /// <returns>Tuple: (SKU, ProductName) - her ikisi de null olabilir</returns>
    Task<(string? Sku, string? ProductName)> GetVariantWithProductNameAsync(long variantId);
    
    /// <summary>
    /// Memory-efficient batched sales order retrieval for large datasets
    /// Prevents memory leaks when processing 1000+ orders
    /// </summary>
    IAsyncEnumerable<List<SalesOrderDto>> GetSalesOrdersBatchedAsync(DateTime? fromDate = null, int batchSize = 100);
    
    Task<SalesOrderDto?> CreateSalesOrderAsync(SalesOrderDto salesOrder);
    Task<SalesOrderDto?> UpdateSalesOrderAsync(SalesOrderDto salesOrder);
    
    /// <summary>
    /// Cancels a sales order in Katana by updating its status to CANCELLED.
    /// Used for cleaning up duplicate orders.
    /// </summary>
    /// <param name="katanaOrderId">Katana sales order ID</param>
    /// <returns>True if cancelled successfully, false otherwise</returns>
    Task<bool> CancelSalesOrderAsync(long katanaOrderId);
    
    /// <summary>
    /// Deletes a sales order from Katana (if API supports it).
    /// Falls back to cancellation if deletion is not supported.
    /// </summary>
    /// <param name="katanaOrderId">Katana sales order ID</param>
    /// <returns>True if deleted/cancelled successfully, false otherwise</returns>
    Task<bool> DeleteSalesOrderAsync(long katanaOrderId);
    
    /// <summary>
    /// Gets a single sales order by ID from Katana.
    /// </summary>
    /// <param name="katanaOrderId">Katana sales order ID</param>
    /// <returns>Sales order DTO or null if not found</returns>
    Task<SalesOrderDto?> GetSalesOrderByIdAsync(long katanaOrderId);
    Task<List<LocationDto>> GetLocationsAsync();
    Task<LocationDto?> CreateLocationAsync(LocationDto location);
    Task<LocationDto?> UpdateLocationAsync(int locationId, LocationDto location);
    Task<List<StockAdjustmentDto>> GetStockAdjustmentsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<StockAdjustmentDto?> CreateStockAdjustmentAsync(StockAdjustmentCreateRequest request);
    Task<List<InventoryMovementDto>> GetInventoryMovementsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<ServiceDto>> GetServicesAsync();
    Task<List<TaxRateDto>> GetTaxRatesAsync();
    Task<List<PriceListDto>> GetPriceListsAsync();
    Task<List<WebhookDto>> GetWebhooksAsync();
    Task<List<SerialNumberDto>> GetSerialNumbersAsync();
    Task<List<UserDto>> GetUsersAsync();
    Task<List<BomRowDto>> GetBomRowsAsync();
    Task<List<MaterialDto>> GetMaterialsAsync();
    
    /// <summary>
    /// Creates a new webhook in Katana for receiving event notifications.
    /// </summary>
    /// <param name="url">URL to receive webhook notifications</param>
    /// <param name="events">List of event types to subscribe to (e.g., "product.created", "sales_order.updated")</param>
    /// <param name="description">Optional description for the webhook</param>
    /// <returns>Created webhook DTO with token for signature verification</returns>
    Task<WebhookDto?> CreateWebhookAsync(string url, List<string> events, string? description = null);
    
    /// <summary>
    /// Deletes a webhook from Katana.
    /// </summary>
    /// <param name="webhookId">Webhook ID to delete</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteWebhookAsync(int webhookId);
    
    /// <summary>
    /// Updates an existing webhook in Katana.
    /// </summary>
    /// <param name="webhookId">Webhook ID to update</param>
    /// <param name="url">New URL (optional)</param>
    /// <param name="events">New list of events (optional)</param>
    /// <param name="description">New description (optional)</param>
    /// <returns>Updated webhook DTO</returns>
    Task<WebhookDto?> UpdateWebhookAsync(int webhookId, string? url = null, List<string>? events = null, string? description = null);
    
    /// <summary>
    /// Archives a product in Katana by setting is_archived to true.
    /// Used to soft-delete products that no longer exist in local database.
    /// </summary>
    /// <param name="productId">Katana product ID</param>
    /// <returns>True if archived successfully, false otherwise</returns>
    Task<bool> ArchiveProductAsync(int productId);
}
