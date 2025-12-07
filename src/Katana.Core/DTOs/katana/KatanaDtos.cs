namespace Katana.Core.DTOs;

public class KatanaStockDto
{
    public string ProductSKU { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Location { get; set; } = string.Empty;
    public string MovementType { get; set; } = string.Empty;
    public DateTime MovementDate { get; set; }
    public string? Reference { get; set; }
    public string? Reason { get; set; }
}

public class KatanaProductDto
{
    public string Id { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? SalesPrice { get; set; }
    public decimal? CostPrice { get; set; }
    public decimal? PurchasePrice { get; set; }
    public int CategoryId { get; set; }
    public string? Category { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Unit { get; set; }
    public string? Barcode { get; set; }
    public int? VatRate { get; set; }
    public string? Currency { get; set; }
    
    
    public int? OnHand { get; set; }
    public int? Available { get; set; }
    public int? Committed { get; set; }
    public int? InStock { get; set; }

    
    
    
    public string GetProductCode() => !string.IsNullOrWhiteSpace(SKU) ? SKU : $"KAT-{Id}";
}

public class KatanaInvoiceDto
{
    public string InvoiceNo { get; set; } = string.Empty;
    
    public int? ExternalCustomerId { get; set; }
    public string CustomerTaxNo { get; set; } = string.Empty;
    public string CustomerTitle { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string Currency { get; set; } = "TRY";
    public List<KatanaInvoiceItemDto> Items { get; set; } = new();
}

public class KatanaInvoiceItemDto
{
    public string ProductSKU { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
}

public class KatanaPurchaseOrderDto
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public List<KatanaPurchaseOrderItemDto> Items { get; set; } = new();
}

public class KatanaPurchaseOrderItemDto
{
    public string ProductSKU { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
}

public class KatanaManufacturingOrderDto
{
    public string Id { get; set; } = string.Empty;
    public string ProductSKU { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
}

public class KatanaVariantDto
{
    public string Id { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal Price { get; set; }
    public string? ParentProductId { get; set; }
}

public class KatanaSupplierDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TaxNo { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Currency { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<KatanaSupplierAddressDto> Addresses { get; set; } = new();
}

public class KatanaSupplierAddressDto
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public string? Line1 { get; set; }
    public string? Line2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public string? Country { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class KatanaBatchDto
{
    public string Id { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string BatchNo { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public decimal Quantity { get; set; }
    public string? Location { get; set; }
}

public class KatanaStockTransferDto
{
    public string Id { get; set; } = string.Empty;
    public string FromLocationId { get; set; } = string.Empty;
    public string ToLocationId { get; set; } = string.Empty;
    public DateTime TransferDate { get; set; }
    public List<KatanaStockTransferItemDto> Items { get; set; } = new();
}

public class KatanaStockTransferItemDto
{
    public string ProductSKU { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}

public class KatanaSalesReturnDto
{
    public string Id { get; set; } = string.Empty;
    public string SalesOrderId { get; set; } = string.Empty;
    public DateTime ReturnDate { get; set; }
    public List<KatanaReturnRowDto> ReturnRows { get; set; } = new();
}

public class KatanaReturnRowDto
{
    public string ProductSKU { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

// NOTE: SalesOrderDto ve SalesOrderItemDto tanımları KatanaOrders.cs dosyasında bulunmaktadır.
// Duplicate tanımlardan kaçınmak için bu dosyada tekrar tanımlanmamıştır.

// NOTE: StockAdjustmentDto, StockAdjustmentRowDto, StockAdjustmentCreateRequest
// ve InventoryMovementDto tanımları KatanaInventory.cs dosyasında bulunmaktadır.
// Duplicate tanımlardan kaçınmak için bu dosyada tekrar tanımlanmamıştır.

// Service DTOs
public class ServiceDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? Currency { get; set; }
    public decimal? TaxRate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ServiceVariantDto
{
    public string Id { get; set; } = string.Empty;
    public string ServiceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? SalesPrice { get; set; }
    public decimal? DefaultCost { get; set; }
    public string? Currency { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

// Tax Rate DTOs
public class TaxRateDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public string? Country { get; set; }
    public bool IsDefault { get; set; }
    public DateTime? CreatedAt { get; set; }
}

// Price List DTOs
public class PriceListDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Currency { get; set; }
    public bool IsDefault { get; set; }
    public List<PriceListItemDto> Items { get; set; } = new();
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class PriceListItemDto
{
    public string ProductSKU { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? MinQuantity { get; set; }
}

// Webhook DTOs
public class WebhookDto
{
    public string Id { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Event { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? Secret { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

// Serial Number DTOs
public class SerialNumberDto
{
    public string Id { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string ProductSKU { get; set; } = string.Empty;
    public string? ProductName { get; set; }
    public string? LocationId { get; set; }
    public string? LocationName { get; set; }
    public string Status { get; set; } = string.Empty; // AVAILABLE, ALLOCATED, SOLD
    public DateTime? ManufacturedDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? CreatedAt { get; set; }
}

// User DTOs
public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FullName => $"{FirstName} {LastName}".Trim();
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public DateTime? CreatedAt { get; set; }
}

// BOM (Bill of Materials) DTOs
public class BomRowDto
{
    public string Id { get; set; } = string.Empty;
    public string ParentProductSKU { get; set; } = string.Empty;
    public string? ParentProductName { get; set; }
    public string ComponentProductSKU { get; set; } = string.Empty;
    public string? ComponentProductName { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal? Scrap { get; set; }
    public int? Sequence { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

// Material DTOs
public class MaterialDto
{
    public string Id { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Unit { get; set; }
    public decimal? CostPrice { get; set; }
    public string? Currency { get; set; }
    public string? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public int? LeadTimeDays { get; set; }
    public decimal? MinOrderQuantity { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

