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
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class KatanaInvoiceDto
{
    public string InvoiceNo { get; set; } = string.Empty;
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

