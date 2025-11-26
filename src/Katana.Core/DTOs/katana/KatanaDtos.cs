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
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? TaxNo { get; set; }
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

