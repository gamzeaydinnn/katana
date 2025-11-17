using Katana.Business.DTOs;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Enums;

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

    public static LucaProductUpdateDto MapToLucaProduct(Product product)
    {
        return new LucaProductUpdateDto
        {
            ProductCode = product.SKU,
            ProductName = product.Name,
            Unit = "Adet",
            Quantity = product.Stock,
            UnitPrice = product.Price,
            VatRate = 20
        };
    }

    // Reverse mappings: Luca -> Katana entities
    public static StockMovement MapFromLucaStock(LucaStockDto dto, int productId)
    {
        // Map movement type
        var type = dto.MovementType?.ToUpperInvariant() ?? "ADJUSTMENT";
        MovementType movementType = type switch
        {
            "IN" => MovementType.In,
            "OUT" => MovementType.Out,
            _ => MovementType.Adjustment
        };

        // ChangeQuantity: IN => +qty, OUT => -qty, otherwise 0 (caller may compute BALANCE delta)
        var change = type switch
        {
            "IN" => Math.Abs(dto.Quantity),
            "OUT" => -Math.Abs(dto.Quantity),
            _ => 0
        };

        return new StockMovement
        {
            ProductId = productId,
            ProductSku = dto.ProductCode,
            ChangeQuantity = change,
            MovementType = movementType,
            SourceDocument = dto.Reference ?? "LUCA_SYNC",
            Timestamp = dto.MovementDate == default ? DateTime.UtcNow : dto.MovementDate,
            // Normalize warehouse code: prefer explicit WarehouseCode (take first token if contains spaces),
            // then Entry/Exit codes; default to "001" (matches InventorySettings defaults).
            WarehouseCode = !string.IsNullOrWhiteSpace(dto.WarehouseCode)
                ? dto.WarehouseCode.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].Trim()
                : (dto.EntryWarehouseCode ?? dto.ExitWarehouseCode ?? "001"),
            IsSynced = true,
            SyncedAt = DateTime.UtcNow
        };
    }

    public static Customer MapFromLucaCustomer(LucaCustomerDto dto)
    {
        return new Customer
        {
            TaxNo = dto.CustomerCode ?? dto.CustomerCode ?? string.Empty,
            Title = dto.Title ?? string.Empty,
            ContactPerson = dto.ContactPerson,
            Phone = dto.Phone,
            Email = dto.Email,
            Address = dto.Address,
            City = dto.City,
            Country = dto.Country,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static Invoice MapFromLucaInvoice(LucaInvoiceDto dto, int customerId)
    {
        return new Invoice
        {
            InvoiceNo = dto.DocumentNo,
            CustomerId = customerId,
            Amount = dto.NetAmount,
            TaxAmount = dto.TaxAmount,
            TotalAmount = dto.GrossAmount,
            Status = "RECEIVED",
            InvoiceDate = dto.DocumentDate,
            DueDate = dto.DueDate,
            Currency = dto.Currency ?? "TRY",
            IsSynced = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
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
