using Katana.Business.Interfaces;
using Katana.Business.DTOs;
using System;
using Katana.Core.DTOs;

namespace Katana.Business.Interfaces;

public interface IKatanaService
{
    Task<List<KatanaStockDto>> GetStockChangesAsync(DateTime fromDate, DateTime toDate);
    Task<List<KatanaProductDto>> GetProductsAsync();
    Task<List<KatanaInvoiceDto>> GetInvoicesAsync(DateTime fromDate, DateTime toDate);
    Task<KatanaProductDto?> GetProductBySkuAsync(string sku);
    Task<bool> TestConnectionAsync();
    Task<bool> UpdateProductAsync(int katanaProductId, string name, decimal? salesPrice, int? stock);
    Task<List<KatanaPurchaseOrderDto>> GetPurchaseOrdersAsync(string? status = null, DateTime? fromDate = null);
    Task<KatanaPurchaseOrderDto?> GetPurchaseOrderByIdAsync(string id);
    Task<string?> ReceivePurchaseOrderAsync(string id);
    Task<List<KatanaSupplierDto>> GetSuppliersAsync();
    Task<KatanaSupplierDto?> GetSupplierByIdAsync(string id);
    Task<List<KatanaManufacturingOrderDto>> GetManufacturingOrdersAsync(string? status = null);
    Task<KatanaManufacturingOrderDto?> GetManufacturingOrderByIdAsync(string id);
    Task<KatanaVariantDto?> GetVariantAsync(string variantId);
    Task<List<KatanaVariantDto>> GetVariantsAsync(string? productId = null);
    Task<List<KatanaBatchDto>> GetBatchesAsync(string? productId = null);
    Task<List<KatanaStockTransferDto>> GetStockTransfersAsync(string? status = null);
    Task<List<KatanaSalesReturnDto>> GetSalesReturnsAsync(DateTime? fromDate = null);
}
