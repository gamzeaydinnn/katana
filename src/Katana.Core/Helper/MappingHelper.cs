using Katana.Business.DTOs;
using Katana.Core.DTOs;
using Katana.Core.Entities;

namespace Katana.Core.Helpers;

public static class MappingHelper
{
    // Katana -> Core Entity mapping
    public static Product MapToProduct(KatanaProductDto katanaProduct)
    {
        return new Product
        {
            SKU = katanaProduct.SKU,
            Name = katanaProduct.Name,
            Price = katanaProduct.Price,
            CategoryId = katanaProduct.CategoryId,
            MainImageUrl = katanaProduct.ImageUrl,
            Description = katanaProduct.Description,
            IsActive = katanaProduct.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static Stock MapToStock(KatanaStockDto katanaStock, int productId)
    {
        return new Stock
        {
            ProductId = productId,
            Location = katanaStock.Location,
            Quantity = katanaStock.Quantity,
            Type = MapStockMovementType(katanaStock.MovementType),
            Reason = katanaStock.Reason,
            Timestamp = katanaStock.MovementDate,
            Reference = katanaStock.Reference,
            IsSynced = false
        };
    }

    public static Customer MapToCustomer(KatanaInvoiceDto katanaInvoice)
    {
        return new Customer
        {
            TaxNo = katanaInvoice.CustomerTaxNo,
            Title = katanaInvoice.CustomerTitle,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static Invoice MapToInvoice(KatanaInvoiceDto katanaInvoice, int customerId)
    {
        return new Invoice
        {
            InvoiceNo = katanaInvoice.InvoiceNo,
            CustomerId = customerId,
            Amount = katanaInvoice.Amount,
            TaxAmount = katanaInvoice.TaxAmount,
            TotalAmount = katanaInvoice.TotalAmount,
            Status = "SENT",
            InvoiceDate = katanaInvoice.InvoiceDate,
            DueDate = katanaInvoice.DueDate,
            Currency = katanaInvoice.Currency,
            IsSynced = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    // Core Entity -> Luca DTO mapping
    public static LucaInvoiceDto MapToLucaInvoice(Invoice invoice, Customer customer, List<InvoiceItem> items, Dictionary<string, string> skuToAccountMapping)
    {
        var lucaInvoice = new LucaInvoiceDto
        {
            DocumentNo = invoice.InvoiceNo,
            CustomerCode = GenerateCustomerCode(customer.TaxNo),
            CustomerTitle = customer.Title,
            CustomerTaxNo = customer.TaxNo,
            DocumentDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,
            NetAmount = invoice.Amount,
            TaxAmount = invoice.TaxAmount,
            GrossAmount = invoice.TotalAmount,
            Currency = invoice.Currency,
            DocumentType = "SALES_INVOICE"
        };

        lucaInvoice.Lines = items.Select(item => new LucaInvoiceItemDto
        {
            AccountCode = skuToAccountMapping.TryGetValue(item.ProductSKU, out var acc)
                ? acc
                : (skuToAccountMapping.TryGetValue("DEFAULT", out var defAcc) ? defAcc : "600.01"),
            ProductCode = item.ProductSKU,
            Description = item.ProductName,
            Quantity = item.Quantity,
            Unit = item.Unit,
            UnitPrice = item.UnitPrice,
            NetAmount = item.TotalAmount - item.TaxAmount,
            TaxRate = item.TaxRate,
            TaxAmount = item.TaxAmount,
            GrossAmount = item.TotalAmount
        }).ToList();

        return lucaInvoice;
    }

    public static LucaStockDto MapToLucaStock(Stock stock, Product product, Dictionary<string, string> locationMapping)
    {
        var warehouseCode = locationMapping.TryGetValue(stock.Location, out var wh)
            ? wh
            : (locationMapping.TryGetValue("DEFAULT", out var defWh) ? defWh : "001");

        return new LucaStockDto
        {
            ProductCode = product.SKU,
            ProductName = product.Name,
            WarehouseCode = warehouseCode,
            EntryWarehouseCode = warehouseCode,
            ExitWarehouseCode = warehouseCode,
            Quantity = Math.Abs(stock.Quantity),
            MovementType = stock.Type == "IN" ? "IN" : "OUT",
            MovementDate = stock.Timestamp,
            Reference = stock.Reference,
            Description = stock.Reason
        };
    }

    public static LucaCustomerDto MapToLucaCustomer(Customer customer)
    {
        return new LucaCustomerDto
        {
            CustomerCode = GenerateCustomerCode(customer.TaxNo),
            Title = customer.Title,
            TaxNo = customer.TaxNo,
            ContactPerson = customer.ContactPerson,
            Phone = customer.Phone,
            Email = customer.Email,
            Address = customer.Address,
            City = customer.City,
            Country = customer.Country
        };
    }

    // Helper methods
    private static string MapStockMovementType(string katanaType)
    {
        return katanaType.ToUpper() switch
        {
            "INCREASE" or "PURCHASE" or "PRODUCTION" => "IN",
            "DECREASE" or "SALE" or "CONSUMPTION" => "OUT",
            "ADJUSTMENT" => "ADJUSTMENT",
            _ => "ADJUSTMENT"
        };
    }

    private static string GenerateCustomerCode(string taxNo)
    {
        return $"CUST_{taxNo}";
    }

    // Validation methods
    public static bool IsValidKatanaStock(KatanaStockDto stock)
    {
        return !string.IsNullOrEmpty(stock.ProductSKU) &&
               !string.IsNullOrEmpty(stock.Location) &&
               !string.IsNullOrEmpty(stock.MovementType) &&
               stock.MovementDate != default;
    }

    public static bool IsValidKatanaInvoice(KatanaInvoiceDto invoice)
    {
        return !string.IsNullOrEmpty(invoice.InvoiceNo) &&
               !string.IsNullOrEmpty(invoice.CustomerTaxNo) &&
               !string.IsNullOrEmpty(invoice.CustomerTitle) &&
               invoice.InvoiceDate != default &&
               invoice.Items.Any() &&
               invoice.Items.All(IsValidKatanaInvoiceItem);
    }

    public static bool IsValidKatanaInvoiceItem(KatanaInvoiceItemDto item)
    {
        return !string.IsNullOrEmpty(item.ProductSKU) &&
               !string.IsNullOrEmpty(item.ProductName) &&
               item.Quantity > 0 &&
               item.UnitPrice >= 0;
    }
}
