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
    /// Memory-efficient batched sales order retrieval for large datasets
    /// Prevents memory leaks when processing 1000+ orders
    /// </summary>
    IAsyncEnumerable<List<SalesOrderDto>> GetSalesOrdersBatchedAsync(DateTime? fromDate = null, int batchSize = 100);
    
    Task<SalesOrderDto?> CreateSalesOrderAsync(SalesOrderDto salesOrder);
    Task<SalesOrderDto?> UpdateSalesOrderAsync(SalesOrderDto salesOrder);
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
    /// Archives a product in Katana by setting is_archived to true.
    /// Used to soft-delete products that no longer exist in local database.
    /// </summary>
    /// <param name="productId">Katana product ID</param>
    /// <returns>True if archived successfully, false otherwise</returns>
    Task<bool> ArchiveProductAsync(int productId);
}
