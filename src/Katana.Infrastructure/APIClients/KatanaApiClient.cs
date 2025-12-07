using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Enums;
using Katana.Core.Helpers;
using Katana.Data.Configuration;
using Microsoft.Extensions.Options;

namespace Katana.Infrastructure.APIClients;

public class KatanaApiClient : IKatanaApiClient
{
    private readonly IKatanaService _katanaService;
    private readonly KatanaMappingSettings _mapping;

    public KatanaApiClient(
        IKatanaService katanaService,
        IOptions<KatanaMappingSettings> mapping)
    {
        _katanaService = katanaService;
        _mapping = mapping.Value;
    }

    public async Task<List<Product>> GetProductsAsync(int? page = null, int? limit = null)
    {
        var productDtos = await _katanaService.GetProductsAsync();
        var mapped = productDtos.Select(MappingHelper.MapToProduct).ToList();
        return Paginate(mapped, page, limit);
    }

    public async Task<Product?> GetProductByIdAsync(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
            return null;

        // Try parsing as integer for direct ID lookup
        if (int.TryParse(productId, out var id))
        {
            var byId = await _katanaService.GetProductByIdAsync(id);
            if (byId != null)
                return MappingHelper.MapToProduct(byId);
        }

        // Fallback to SKU search
        var bySku = await _katanaService.GetProductBySkuAsync(productId);
        if (bySku != null)
            return MappingHelper.MapToProduct(bySku);

        return null;
    }

    public async Task<Product?> CreateProductAsync(Product product)
    {
        if (product == null || string.IsNullOrWhiteSpace(product.Name))
            return null;

        // Map domain Product to KatanaProductDto
        var dto = MappingHelper.MapToKatanaProductDto(product);
        
        var createdDto = await _katanaService.CreateProductAsync(dto);
        
        return createdDto != null ? MappingHelper.MapToProduct(createdDto) : null;
    }

    public async Task<bool> DeleteProductAsync(int katanaProductId)
    {
        if (katanaProductId <= 0)
            return false;

        return await _katanaService.DeleteProductAsync(katanaProductId);
    }

    public async Task<List<Customer>> GetCustomersAsync()
    {
        var dtos = await _katanaService.GetCustomersAsync();
        return dtos.Select(MappingHelper.MapToCustomer).ToList();
    }

    public async Task<Customer?> GetCustomerByIdAsync(int customerId)
    {
        if (customerId <= 0)
            return null;

        var dto = await _katanaService.GetCustomerByIdAsync(customerId);
        return dto != null ? MappingHelper.MapToCustomer(dto) : null;
    }

    public async Task<Customer?> CreateCustomerAsync(Customer customer)
    {
        if (customer == null || string.IsNullOrWhiteSpace(customer.Title))
            return null;

        var dto = MappingHelper.MapToKatanaCustomerDto(customer);
        var createdDto = await _katanaService.CreateCustomerAsync(dto);
        
        return createdDto != null ? MappingHelper.MapToCustomer(createdDto) : null;
    }

    public async Task<Customer?> UpdateCustomerAsync(int customerId, Customer customer)
    {
        if (customerId <= 0 || customer == null)
            return null;

        var dto = MappingHelper.MapToKatanaCustomerDto(customer);
        var updatedDto = await _katanaService.UpdateCustomerAsync(customerId, dto);
        
        return updatedDto != null ? MappingHelper.MapToCustomer(updatedDto) : null;
    }

    public async Task<bool> DeleteCustomerAsync(int customerId)
    {
        if (customerId <= 0)
            return false;

        return await _katanaService.DeleteCustomerAsync(customerId);
    }

    public async Task<List<StockMovement>> GetStockMovementsAsync(DateTime? fromDate = null, int? page = null)
    {
        var start = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var end = DateTime.UtcNow;
        var katanaMovements = await _katanaService.GetStockChangesAsync(start, end);
        var mapped = katanaMovements.Select(MapStockMovement).ToList();

        return Paginate(mapped, page, null);
    }

    public async Task<List<Stock>> GetStockAdjustmentsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var dtos = await _katanaService.GetStockAdjustmentsAsync(fromDate, toDate);
        var stocks = new List<Stock>();
        foreach (var adj in dtos)
        {
            foreach (var row in adj.StockAdjustmentRows ?? new List<StockAdjustmentRowDto>())
            {
                stocks.Add(new Stock
                {
                    ProductId = 0,
                    Location = adj.LocationId.ToString(),
                    Quantity = (int)row.Quantity,
                    Type = "ADJUSTMENT",
                    Reason = adj.Reason,
                    Timestamp = adj.StockAdjustmentDate,
                    Reference = adj.StockAdjustmentNumber,
                    IsSynced = false
                });
            }
        }
        return stocks;
    }

    public async Task<List<InventoryMovement>> GetInventoryMovementsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var dtos = await _katanaService.GetInventoryMovementsAsync(fromDate, toDate);
        return dtos.Select(m => new InventoryMovement
        {
            Id = 0,
            ProductId = 0,
            VariantId = m.VariantId,
            LocationId = m.LocationId,
            MovementType = string.IsNullOrWhiteSpace(m.ResourceType) ? "UNKNOWN" : m.ResourceType,
            Quantity = m.Quantity,
            Timestamp = m.CreatedAt == default ? DateTime.UtcNow : m.CreatedAt,
            Reference = !string.IsNullOrWhiteSpace(m.CausedByOrderNo)
                ? m.CausedByOrderNo
                : (m.ResourceId?.ToString() ?? string.Empty)
        }).ToList();
    }

    public async Task<List<Invoice>> GetInvoicesAsync(DateTime fromDate, DateTime toDate)
    {
        var invoiceDtos = await _katanaService.GetInvoicesAsync(fromDate, toDate);
        var invoices = new List<Invoice>();

        foreach (var dto in invoiceDtos)
        {
            var customer = MappingHelper.MapToCustomer(dto);
            var invoice = MappingHelper.MapToInvoice(dto, customer.Id);
            invoice.Customer = customer;
            invoice.InvoiceItems = dto.Items?.Select(item => new InvoiceItem
            {
                ProductId = 0,
                ProductSKU = NormalizeSku(item.ProductSKU),
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TaxRate = item.TaxRate,
                TaxAmount = item.TaxAmount,
                TotalAmount = item.TotalAmount
            }).ToList() ?? new List<InvoiceItem>();

            invoices.Add(invoice);
        }

        return invoices;
    }

    public async Task<List<PurchaseOrder>> GetPurchaseOrdersAsync(string? status = null, DateTime? fromDate = null)
    {
        var dtos = await _katanaService.GetPurchaseOrdersAsync(status, fromDate);
        return dtos.Select(MapPurchaseOrder).ToList();
    }

    public async Task<List<Order>> GetSalesOrdersAsync(DateTime? fromDate = null)
    {
        var dtos = await _katanaService.GetSalesOrdersAsync(fromDate);
        return dtos.Select(MapSalesOrder).ToList();
    }

    public async Task<Order?> CreateSalesOrderAsync(Order order)
    {
        var dto = await _katanaService.CreateSalesOrderAsync(MapToSalesOrderDto(order));
        return dto is null ? null : MapSalesOrder(dto);
    }

    public async Task<Order?> UpdateSalesOrderAsync(Order order)
    {
        var dto = await _katanaService.UpdateSalesOrderAsync(MapToSalesOrderDto(order));
        return dto is null ? null : MapSalesOrder(dto);
    }

    public async Task<LocationDto?> CreateLocationAsync(LocationDto location)
    {
        if (location == null)
            return null;

        if (string.IsNullOrWhiteSpace(location.Name))
            return null;

        var createdDto = await _katanaService.CreateLocationAsync(location);
        return createdDto;
    }

    public async Task<LocationDto?> UpdateLocationAsync(int locationId, LocationDto location)
    {
        if (locationId <= 0)
            return null;

        if (location == null)
            return null;

        if (string.IsNullOrWhiteSpace(location.Name))
            return null;

        var updatedDto = await _katanaService.UpdateLocationAsync(locationId, location);
        return updatedDto;
    }

    public async Task<PurchaseOrder?> GetPurchaseOrderByIdAsync(string id)
    {
        var dto = await _katanaService.GetPurchaseOrderByIdAsync(id);
        return dto is null ? null : MapPurchaseOrder(dto);
    }

    public Task<string?> ReceivePurchaseOrderAsync(string id)
    {
        return _katanaService.ReceivePurchaseOrderAsync(id);
    }

    public async Task<List<Supplier>> GetSuppliersAsync()
    {
        var dtos = await _katanaService.GetSuppliersAsync();
        return dtos.Select(MappingHelper.MapToSupplier).ToList();
    }

    public async Task<Supplier?> GetSupplierByIdAsync(string id)
    {
        var dto = await _katanaService.GetSupplierByIdAsync(id);
        return dto is null ? null : MappingHelper.MapToSupplier(dto);
    }

    public async Task<List<ManufacturingOrder>> GetManufacturingOrdersAsync(string? status = null)
    {
        var dtos = await _katanaService.GetManufacturingOrdersAsync(status);
        return dtos.Select(dto => MappingHelper.MapToManufacturingOrder(dto, 0)).ToList();
    }

    public async Task<ManufacturingOrder?> GetManufacturingOrderByIdAsync(string id)
    {
        var dto = await _katanaService.GetManufacturingOrderByIdAsync(id);
        return dto is null ? null : MappingHelper.MapToManufacturingOrder(dto, 0);
    }

    public async Task<List<ProductVariant>> GetVariantsAsync(string? productId = null)
    {
        var dtos = await _katanaService.GetVariantsAsync(productId);
        return dtos.Select(dto => MappingHelper.MapToVariant(dto, 0)).ToList();
    }

    public async Task<ProductVariant?> GetVariantAsync(string variantId)
    {
        var dto = await _katanaService.GetVariantAsync(variantId);
        return dto is null ? null : MappingHelper.MapToVariant(dto, 0);
    }

    public async Task<List<Batch>> GetBatchesAsync(string? productId = null)
    {
        var dtos = await _katanaService.GetBatchesAsync(productId);
        return dtos.Select(dto => MappingHelper.MapToBatch(dto, 0)).ToList();
    }

    public Task<List<LocationDto>> GetLocationsAsync()
    {
        return _katanaService.GetLocationsAsync();
    }

    public Task<List<ServiceDto>> GetServicesAsync()
    {
        return _katanaService.GetServicesAsync();
    }

    public Task<List<TaxRateDto>> GetTaxRatesAsync()
    {
        return _katanaService.GetTaxRatesAsync();
    }

    public Task<List<PriceListDto>> GetPriceListsAsync()
    {
        return _katanaService.GetPriceListsAsync();
    }

    public Task<List<WebhookDto>> GetWebhooksAsync()
    {
        return _katanaService.GetWebhooksAsync();
    }

    public Task<List<SerialNumberDto>> GetSerialNumbersAsync()
    {
        return _katanaService.GetSerialNumbersAsync();
    }

    public Task<List<UserDto>> GetUsersAsync()
    {
        return _katanaService.GetUsersAsync();
    }

    public Task<List<BomRowDto>> GetBomRowsAsync()
    {
        return _katanaService.GetBomRowsAsync();
    }

    public Task<List<MaterialDto>> GetMaterialsAsync()
    {
        return _katanaService.GetMaterialsAsync();
    }

    public async Task<List<StockTransfer>> GetStockTransfersAsync(string? status = null)
    {
        var dtos = await _katanaService.GetStockTransfersAsync(status);
        return dtos.Select(dto => MappingHelper.MapToStockTransfer(dto, 0)).ToList();
    }

    public async Task<List<Order>> GetSalesReturnsAsync(DateTime? fromDate = null)
    {
        var dtos = await _katanaService.GetSalesReturnsAsync(fromDate);
        return dtos.Select(dto => MappingHelper.MapToSalesReturn(dto, 0)).ToList();
    }

    public async Task<ApiHealthStatus> CheckHealthAsync()
    {
        var isHealthy = await _katanaService.TestConnectionAsync();
        return new ApiHealthStatus
        {
            IsHealthy = isHealthy,
            Message = isHealthy ? "API is reachable" : "API check failed",
            CheckedAt = DateTime.UtcNow
        };
    }

    public Task<bool> UpdateProductAsync(int katanaProductId, string name, decimal? salesPrice, int? stock)
    {
        return _katanaService.UpdateProductAsync(katanaProductId, name, salesPrice, stock);
    }

    private static List<T> Paginate<T>(List<T> items, int? page, int? limit)
    {
        if (!page.HasValue || !limit.HasValue || limit.Value <= 0 || page.Value <= 0)
            return items;

        return items.Skip((page.Value - 1) * limit.Value).Take(limit.Value).ToList();
    }

    private StockMovement MapStockMovement(KatanaStockDto dto)
    {
        var movementType = ResolveMovementType(dto.MovementType);
        var quantity = movementType == MovementType.Out ? -dto.Quantity : dto.Quantity;
        var warehouse = ResolveWarehouse(dto.Location);

        return new StockMovement
        {
            ProductId = 0,
            ProductSku = NormalizeSku(dto.ProductSKU),
            ChangeQuantity = quantity,
            MovementType = movementType,
            SourceDocument = dto.Reference ?? dto.Reason ?? string.Empty,
            Timestamp = dto.MovementDate == default ? DateTime.UtcNow : dto.MovementDate,
            WarehouseCode = warehouse,
            IsSynced = false
        };
    }

    private static MovementType ResolveMovementType(string katanaType)
    {
        return katanaType?.ToUpperInvariant() switch
        {
            "INCREASE" or "PURCHASE" or "PRODUCTION" => MovementType.In,
            "DECREASE" or "SALE" or "CONSUMPTION" => MovementType.Out,
            "ADJUSTMENT" => MovementType.Adjustment,
            _ => MovementType.Adjustment
        };
    }

    private static PurchaseOrder MapPurchaseOrder(KatanaPurchaseOrderDto dto)
    {
        var mapped = MappingHelper.MapToPurchaseOrder(dto);
        mapped.Items = dto.Items?.Select(i => new PurchaseOrderItem
        {
            ProductId = 0,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList() ?? new List<PurchaseOrderItem>();

        return mapped;
    }

    private static Order MapSalesOrder(SalesOrderDto dto)
    {
        return new Order
        {
            OrderNo = string.IsNullOrWhiteSpace(dto.OrderNo) ? dto.Id.ToString() : dto.OrderNo,
            CustomerId = 0,
            Status = OrderStatus.Pending,
            TotalAmount = dto.Total ?? 0,
            OrderDate = dto.OrderCreatedDate ?? DateTime.UtcNow,
            Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "TRY" : dto.Currency,
            IsSynced = false
        };
    }

    private static SalesOrderDto MapToSalesOrderDto(Order order)
    {
        return new SalesOrderDto
        {
            OrderNo = order.OrderNo,
            OrderCreatedDate = order.OrderDate,
            DeliveryDate = order.OrderDate,
            Status = order.Status.ToString(),
            Currency = order.Currency,
            Total = order.TotalAmount
        };
    }

    private static string NormalizeSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            return string.Empty;

        var trimmed = sku.Trim();
        var allowedChars = trimmed.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray();
        var normalized = new string(allowedChars);
        if (normalized.Length > 50)
            normalized = normalized.Substring(0, 50);

        return normalized.ToUpperInvariant();
    }

    private string ResolveWarehouse(string? location)
    {
        if (!string.IsNullOrWhiteSpace(location) && _mapping.LocationToWarehouse.TryGetValue(location, out var mapped))
            return mapped;

        if (!string.IsNullOrWhiteSpace(_mapping.DefaultWarehouseCode))
            return _mapping.DefaultWarehouseCode;

        return string.IsNullOrWhiteSpace(location) ? "MAIN" : location;
    }
}
