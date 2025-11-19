using System.Linq;
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

        lucaInvoice.Lines = items.Select(item =>
        {
            var productCode = NormalizeSku(item.ProductSKU);
            var description = TrimAndTruncate(item.ProductName, 255) ?? string.Empty;

            return new LucaInvoiceItemDto
            {
                AccountCode = skuToAccountMapping.TryGetValue(item.ProductSKU, out var acc)
                    ? acc
                    : (skuToAccountMapping.TryGetValue("DEFAULT", out var defAcc) ? defAcc : "600.01"),
                ProductCode = productCode,
                Description = description,
                Quantity = item.Quantity,
                Unit = item.Unit,
                UnitPrice = item.UnitPrice,
                NetAmount = item.TotalAmount - item.TaxAmount,
                TaxRate = item.TaxRate,
                TaxAmount = item.TaxAmount,
                GrossAmount = item.TotalAmount
            };
        }).ToList();

        return lucaInvoice;
    }

    public static LucaStockDto MapToLucaStock(Stock stock, Product product, Dictionary<string, string> locationMapping)
    {
        var rawWarehouse = locationMapping.TryGetValue(stock.Location, out var wh)
            ? wh
            : (locationMapping.TryGetValue("DEFAULT", out var defWh) ? defWh : "001");
        var warehouseCode = NormalizeWarehouseCode(rawWarehouse);

        var productCode = NormalizeSku(product.SKU);
        var productName = TrimAndTruncate(product.Name, 255) ?? product.Name;
        var description = TrimAndTruncate(stock.Reason, 500);
        var reference = TrimAndTruncate(stock.Reference, 100);

        return new LucaStockDto
        {
            ProductCode = productCode,
            ProductName = productName,
            WarehouseCode = warehouseCode,
            EntryWarehouseCode = warehouseCode,
            ExitWarehouseCode = warehouseCode,
            Quantity = Math.Abs(stock.Quantity),
            MovementType = stock.Type == "IN" ? "IN" : "OUT",
            MovementDate = stock.Timestamp,
            Reference = reference,
            Description = description
        };
    }

    /// <summary>
    /// Katana Product -> Koza stok kartı oluşturma DTO'su (EkleStkWsSkart.do).
    /// Burada zorunlu alanlar doldurulur, diğerleri güvenli varsayılanlarla bırakılır.
    /// </summary>
    public static LucaCreateStokKartiRequest MapToLucaStockCard(Product product)
    {
        var normalizedSku = NormalizeSku(product.SKU);
        var baseName = string.IsNullOrWhiteSpace(product.Name) ? normalizedSku : product.Name;
        var kartAdi = TrimAndTruncate(baseName, 255) ?? normalizedSku;
        var uzunAdi = TrimAndTruncate(!string.IsNullOrWhiteSpace(product.Description) ? product.Description : baseName, 500) ?? kartAdi;
        var kategoriKod = TrimAndTruncate(product.CategoryId > 0 ? product.CategoryId.ToString() : "0000", 50) ?? "0000";
        var barkod = normalizedSku;
        var detayAciklama = TrimAndTruncate(product.Description, 1000) ?? string.Empty;

        // Dokümana göre KDV oranları 0.18 formatında gönderilmeli (%18)
        var vat = 0.18;

        return new LucaCreateStokKartiRequest
        {
            KartAdi = kartAdi,
            KartTuru = 1,
            BaslangicTarihi = null,
            // Dokümana göre "Adet" için 5 tipik ID; gerekirse konfigürasyona taşınabilir.
            OlcumBirimiId = 5,
            KartKodu = normalizedSku,
            MaliyetHesaplanacakFlag = true,
            KartTipi = 1,
            KategoriAgacKod = kategoriKod,
            KartAlisKdvOran = vat,
            KartSatisKdvOran = vat,
            BitisTarihi = null,
            Barkod = barkod,
            KartToptanAlisKdvOran = vat,
            RafOmru = 0,
            UzunAdi = uzunAdi,
            KartToptanSatisKdvOran = vat,
            GarantiSuresi = 0,
            GtipKodu = string.Empty,
            AlisTevkifatOran = "0",
            AlisTevkifatKod = 0,
            IhracatKategoriNo = string.Empty,
            SatisTevkifatOran = "0",
            SatisTevkifatKod = 0,
            MinStokKontrol = 0,
            MinStokMiktari = 0,
            AlisIskontoOran1 = 0,
            SatilabilirFlag = product.IsActive,
            SatinAlinabilirFlag = product.IsActive,
            UtsVeriAktarimiFlag = false,
            BagDerecesi = 0,
            MaxStokKontrol = 0,
            MaxStokMiktari = 0,
            SatisIskontoOran1 = 0,
            SatisAlternatifFlag = false,
            UretimSuresi = 0,
            UretimSuresiBirim = 0,
            SeriNoFlag = false,
            LotNoFlag = false,
            DetayAciklama = detayAciklama,
            OtvMaliyetFlag = false,
            OtvTutarKdvFlag = false,
            OtvIskontoFlag = false,
            OtvTipi = string.Empty,
            StopajOran = 0,
            AlisIskontoOran2 = 0,
            AlisIskontoOran3 = 0,
            AlisIskontoOran4 = 0,
            AlisIskontoOran5 = 0,
            SatisIskontoOran2 = 0,
            SatisIskontoOran3 = 0,
            SatisIskontoOran4 = 0,
            SatisIskontoOran5 = 0,
            AlisMaktuVergi = 0,
            SatisMaktuVergi = 0,
            AlisOtvOran = 0,
            AlisOtvTutar = 0,
            AlisTecilOtv = 0,
            SatisOtvOran = 0,
            SatisOtvTutar = 0,
            SatisTecilOtv = 0,
            PerakendeAlisBirimFiyat = (double)product.Price,
            PerakendeSatisBirimFiyat = (double)product.Price
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
        // Map Katana Product to Koza's expected EkleStkWsSkart.do payload
        var normalizedSku = NormalizeSku(product.SKU);
        var baseName = string.IsNullOrWhiteSpace(product.Name) ? normalizedSku : product.Name;
        var productName = TrimAndTruncate(baseName, 255) ?? normalizedSku;
        var uzunAd = TrimAndTruncate(!string.IsNullOrWhiteSpace(product.Description) ? product.Description : baseName, 500) ?? productName;
        var kategoriKod = TrimAndTruncate(product.CategoryId > 0 ? product.CategoryId.ToString() : "0000", 50) ?? "0000";

        return new LucaProductUpdateDto
        {
            ProductCode = normalizedSku,
            ProductName = productName,
            // Koza commonly expects uppercase unit codes like ADET
            Unit = "ADET",
            // When creating/updating catalog card, don't rely on local stock snapshot here — send 0
            Quantity = 0,
            UnitPrice = product.Price,
            // Default to a common VAT rate; adjust if Koza requires a different value
            VatRate = 18,

            // Koza fields
            KartAdi = productName,
            // 1 => Stok Kartı per docs
            KartTuru = 1,
            // Ölçü birimi ID: 5 → "Adet" (Koza tarafında yaygın)
            OlcumBirimiId = 5,
            // Use SKU as kartKodu (Koza will create new card if not exists)
            KartKodu = normalizedSku,
            // Use numeric category id as tree code when available, otherwise a default placeholder
            KategoriAgacKod = kategoriKod,
            // Prefer a longer description if available
            UzunAdi = uzunAd
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

    public static Product MapFromLucaProduct(LucaProductDto dto)
    {
        var sku = NormalizeSku(dto.ProductCode ?? string.Empty);
        var name = TrimAndTruncate(dto.ProductName, 200) ?? dto.ProductCode ?? sku;

        return new Product
        {
            SKU = sku,
            Name = name,
            Description = $"Luca ID: {dto.SkartId}",
            Price = 0m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CategoryId = 0
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

    private const int MaxSkuLength = 50;

    private static string NormalizeSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return string.Empty;
        }

        var trimmed = sku.Trim();
        var allowedChars = trimmed
            .Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_')
            .ToArray();

        var normalized = new string(allowedChars);
        if (normalized.Length > MaxSkuLength)
        {
            normalized = normalized.Substring(0, MaxSkuLength);
        }

        return normalized.ToUpperInvariant();
    }

    private static string? TrimAndTruncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length <= maxLength)
        {
            return trimmed;
        }

        return trimmed.Substring(0, maxLength);
    }

    /// <summary>
    /// Koza depo kod formatını normalize eder.
    /// Örnek: "1-1" veya "001-001" → "0001-0001"
    /// </summary>
    private static string NormalizeWarehouseCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return "0001-0001";
        }

        var trimmed = code.Trim();
        var parts = trimmed.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2)
        {
            var left = parts[0].Trim();
            var right = parts[1].Trim();

            if (int.TryParse(left, out var l) && int.TryParse(right, out var r))
            {
                return $"{l:0000}-{r:0000}";
            }
        }

        // Tek parça ise 4 haneye pad et
        if (int.TryParse(trimmed, out var single))
        {
            return $"{single:0000}-{single:0000}";
        }

        // Fallback: geleni olduğu gibi döndür
        return trimmed;
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
