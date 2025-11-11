using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

public class LucaInvoiceDto
{
    public string DocumentNo { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerTitle { get; set; } = string.Empty;
    public string CustomerTaxNo { get; set; } = string.Empty;
    public DateTime DocumentDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal NetAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrossAmount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string DocumentType { get; set; } = "SALES_INVOICE";
    public List<LucaInvoiceItemDto> Lines { get; set; } = new();
}

public class LucaInvoiceItemDto
{
    public string AccountCode { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "ADET";
    public decimal UnitPrice { get; set; }
    public decimal NetAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrossAmount { get; set; }
}

public class LucaStockDto
{
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string MovementType { get; set; } = string.Empty; // IN, OUT
    public DateTime MovementDate { get; set; }
    public string? Reference { get; set; }
    public string? Description { get; set; }
}

public class LucaCustomerDto
{
    public string CustomerCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string TaxNo { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
}

public class LucaProductUpdateDto
{
    [JsonPropertyName("productCode")]
    public string ProductCode { get; set; } = string.Empty;
    
    [JsonPropertyName("productName")]
    public string ProductName { get; set; } = string.Empty;
    
    [JsonPropertyName("unit")]
    public string? Unit { get; set; }
    
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
    
    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }
    
    [JsonPropertyName("vatRate")]
    public int? VatRate { get; set; }
}

