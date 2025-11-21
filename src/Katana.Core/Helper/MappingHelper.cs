using System;
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

    public static PurchaseOrder MapToPurchaseOrder(KatanaPurchaseOrderDto dto)
    {
        return new PurchaseOrder
        {
            OrderNo = dto.Id,
            SupplierCode = dto.SupplierCode,
            SupplierId = 0,
            OrderDate = dto.OrderDate == default ? DateTime.UtcNow : dto.OrderDate,
            Status = MapPurchaseOrderStatus(dto.Status),
            TotalAmount = dto.Items?.Sum(x => x.TotalAmount) ?? 0,
            IsSynced = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Supplier MapToSupplier(KatanaSupplierDto dto)
    {
        return new Supplier
        {
            Code = dto.Id,
            Name = dto.Name,
            Email = dto.Email,
            Phone = dto.Phone,
            TaxNo = dto.TaxNo,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static ProductVariant MapToVariant(KatanaVariantDto dto, int productId)
    {
        return new ProductVariant
        {
            ProductId = productId,
            SKU = NormalizeSku(dto.SKU),
            Barcode = dto.Barcode,
            Price = dto.Price,
            Attributes = dto.ParentProductId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Batch MapToBatch(KatanaBatchDto dto, int productId)
    {
        return new Batch
        {
            ProductId = productId,
            BatchNo = dto.BatchNo,
            ExpiryDate = dto.ExpiryDate,
            Quantity = dto.Quantity,
            Location = dto.Location,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static ManufacturingOrder MapToManufacturingOrder(KatanaManufacturingOrderDto dto, int productId)
    {
        return new ManufacturingOrder
        {
            OrderNo = dto.Id,
            ProductId = productId,
            Quantity = dto.Quantity,
            Status = dto.Status,
            DueDate = dto.DueDate,
            IsSynced = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static StockTransfer MapToStockTransfer(KatanaStockTransferDto dto, int productId)
    {
        return new StockTransfer
        {
            ProductId = productId,
            FromWarehouse = NormalizeWarehouseCode(dto.FromLocationId),
            ToWarehouse = NormalizeWarehouseCode(dto.ToLocationId),
            Quantity = dto.Items?.Sum(i => i.Quantity) ?? 0,
            TransferDate = dto.TransferDate == default ? DateTime.UtcNow : dto.TransferDate,
            Status = "Pending"
        };
    }

    public static Order MapToSalesReturn(KatanaSalesReturnDto dto, int customerId)
    {
        var items = dto.ReturnRows ?? new List<KatanaReturnRowDto>();
        return new Order
        {
            OrderNo = dto.Id,
            CustomerId = customerId,
            Status = OrderStatus.Returned,
            TotalAmount = items.Sum(x => x.UnitPrice * x.Quantity),
            OrderDate = dto.ReturnDate == default ? DateTime.UtcNow : dto.ReturnDate,
            IsSynced = false,
            CreatedAt = DateTime.UtcNow,
            Items = items.Select(i => new OrderItem
            {
                ProductId = 0,
                Quantity = (int)i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
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

    public static LucaIrsaliyeDto MapToLucaIrsaliye(Order order, Customer customer, List<OrderItem> items)
    {
        return new LucaIrsaliyeDto
        {
            BelgeSeri = "IRS",
            BelgeNo = string.IsNullOrWhiteSpace(order.OrderNo) ? order.Id.ToString() : order.OrderNo,
            BelgeTarihi = order.OrderDate == default ? order.CreatedAt : order.OrderDate,
            CariKodu = GenerateCustomerCode(customer.TaxNo),
            ParaBirimKod = string.IsNullOrWhiteSpace(order.Currency) ? "TRY" : order.Currency,
            DetayList = items.Select(item => new LucaIrsaliyeDetayDto
            {
                KartKodu = NormalizeSku(item.Product?.SKU ?? string.Empty),
                Miktar = item.Quantity,
                BirimFiyat = (double)item.UnitPrice,
                DepoKodu = "0001-0001"
            }).ToList()
        };
    }

    public static LucaSatinalmaSiparisDto MapToLucaPurchaseOrder(PurchaseOrder po, Supplier supplier, List<PurchaseOrderItem> items)
    {
        return new LucaSatinalmaSiparisDto
        {
            BelgeSeri = "SAT",
            BelgeNo = string.IsNullOrWhiteSpace(po.OrderNo) ? po.Id.ToString() : po.OrderNo,
            TeslimTarihi = po.ExpectedDate ?? DateTime.UtcNow.AddDays(7),
            TedarikciKodu = string.IsNullOrWhiteSpace(supplier.Code) ? supplier.Id.ToString() : supplier.Code!,
            TeklifSiparisTur = 1,
            OnayFlag = 0,
            DetayList = items.Select(item => new LucaSipariDetayDto
            {
                KartKodu = NormalizeSku(item.Product?.SKU ?? string.Empty),
                Miktar = item.Quantity,
                BirimFiyat = (double)item.UnitPrice
            }).ToList()
        };
    }

    public static LucaCreateSupplierRequest MapToLucaSupplierCreate(Supplier supplier)
    {
        return new LucaCreateSupplierRequest
        {
            CariTipId = 2,
            Tanim = TrimAndTruncate(supplier.Name, 200) ?? supplier.Name,
            KartKod = string.IsNullOrWhiteSpace(supplier.Code) ? null : supplier.Code,
            VergiNo = supplier.TaxNo,
            IletisimTanim = supplier.Phone ?? supplier.Email,
            ParaBirimKod = "TRY"
        };
    }

    public static LucaDepoTransferDto MapToLucaDepoTransfer(StockTransfer transfer)
    {
        return new LucaDepoTransferDto
        {
            GirisDepoKodu = NormalizeWarehouseCode(transfer.ToWarehouse),
            CikisDepoKodu = NormalizeWarehouseCode(transfer.FromWarehouse),
            BelgeTarihi = transfer.TransferDate,
            DetayList = new List<LucaTransferDetayDto>
            {
                new LucaTransferDetayDto
                {
                    KartKodu = NormalizeSku(transfer.Product?.SKU ?? string.Empty),
                    Miktar = (double)transfer.Quantity
                }
            }
        };
    }

    public static LucaCreateStockCountRequest MapToLucaSayimFisi(Stock stock)
    {
        return new LucaCreateStockCountRequest
        {
            BelgeTurDetayId = 0,
            BelgeTarihi = stock.Timestamp == default ? DateTime.UtcNow : stock.Timestamp,
            DepoKodu = NormalizeWarehouseCode(stock.Location),
            DetayList = new List<LucaStockCountDetailRequest>
            {
                new LucaStockCountDetailRequest
                {
                    KartKodu = NormalizeSku(stock.Product?.SKU ?? string.Empty),
                    SayimMiktari = stock.Quantity
                }
            }
        };
    }

    public static LucaCariHareketDto MapToLucaCariHareket(Payment payment, Customer customer)
    {
        return new LucaCariHareketDto
        {
            CariTuru = 1,
            CariKodu = GenerateCustomerCode(customer.TaxNo),
            Tutar = (double)payment.Amount,
            BelgeTarihi = payment.PaymentDate
        };
    }

    public static LucaFaturaKapamaDto MapToLucaFaturaKapama(Payment payment, long faturaId, int cariTur, string kasaBankaKod)
    {
        return new LucaFaturaKapamaDto
        {
            FaturaId = faturaId,
            Tutar = (double)payment.Amount,
            CariKod = kasaBankaKod,
            CariTur = cariTur,
            BelgeTarih = payment.PaymentDate
        };
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
        var kategoriKod = TrimAndTruncate(product.CategoryId > 0 ? product.CategoryId.ToString() : string.Empty, 50) ?? string.Empty;
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
            // kartKodu boş gönderildiğinde Koza otomatik kod üretir
            KartKodu = string.IsNullOrWhiteSpace(product.SKU) ? string.Empty : normalizedSku,
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

    public static LucaCreateCustomerRequest MapToLucaCustomerCreate(Customer customer, long? vergiDairesiId = null, string? kategoriKod = null)
    {
        var title = TrimAndTruncate(customer.Title, 200) ?? GenerateCustomerCode(customer.TaxNo);
        var address = TrimAndTruncate(customer.Address, 500);
        var code = string.IsNullOrWhiteSpace(customer.TaxNo) ? GenerateCustomerCode(customer.Id.ToString()) : customer.TaxNo;

        return new LucaCreateCustomerRequest
        {
            CariTipId = 1,
            Tanim = title,
            KartKod = string.Empty, // boş gönderildiğinde Koza kod üretir
            KategoriKod = kategoriKod,
            ParaBirimKod = "TRY",
            VergiNo = string.IsNullOrWhiteSpace(customer.TaxNo) ? null : customer.TaxNo,
            KisaAd = TrimAndTruncate(title, 50),
            YasalUnvan = title,
            TcKimlikNo = customer.TaxNo?.Length == 11 ? customer.TaxNo : null,
            Ad = title,
            Soyad = null,
            VergiDairesiId = vergiDairesiId,
            Ulke = string.IsNullOrWhiteSpace(customer.Country) ? "TURKIYE" : customer.Country,
            Il = customer.City,
            AdresSerbest = address,
            IletisimTanim = customer.Phone ?? customer.Email,
            EfaturaTuru = null,
            TakipNoFlag = true,
            MutabakatMektubuGonderilecek = null
        };
    }

    public static LucaCreateSupplierRequest MapToLucaSupplierCreate(Supplier supplier, long? vergiDairesiId = null, string? kategoriKod = null)
    {
        var name = TrimAndTruncate(supplier.Name, 200) ?? "TEDARIKCI";
        var address = TrimAndTruncate(supplier.Address, 300);

        return new LucaCreateSupplierRequest
        {
            CariTipId = 2,
            Tanim = name,
            KartKod = string.Empty,
            KategoriKod = kategoriKod,
            ParaBirimKod = "TRY",
            YasalUnvan = name,
            KisaAd = TrimAndTruncate(name, 50),
            AdresSerbest = address,
            IletisimTanim = supplier.Phone ?? supplier.Email,
            Ulke = "TURKIYE",
            Il = null,
            Ilce = null,
            VergiDairesiId = vergiDairesiId
        };
    }

    public static LucaCreateDshBaslikRequest MapToLucaDshBaslik(Stock stock, Product product, long belgeTurDetayId, string? depoKodu = null)
    {
        var warehouseCode = NormalizeWarehouseCode(depoKodu ?? stock.Location);
        var hareketYonu = (stock.Type ?? string.Empty).ToUpperInvariant() == "IN" ? "GIRIS" : "CIKIS";

        return new LucaCreateDshBaslikRequest
        {
            BelgeTurDetayId = belgeTurDetayId,
            BelgeTarihi = stock.Timestamp == default ? DateTime.UtcNow : stock.Timestamp,
            DepoKodu = warehouseCode,
            HareketYonu = hareketYonu,
            DetayList = new List<LucaCreateDshDetayRequest>
            {
                new LucaCreateDshDetayRequest
                {
                    KartKodu = NormalizeSku(product.SKU),
                    DepoKodu = warehouseCode,
                    Miktar = Math.Abs(stock.Quantity),
                    BirimFiyat = (double?)product.Price,
                    KdvOran = 0.18,
                    Aciklama = TrimAndTruncate(stock.Reason ?? stock.Reference, 250)
                }
            }
        };
    }

    [Obsolete("Use MapToLucaStockCard for Koza stock card creation")]
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

    private static PurchaseOrderStatus MapPurchaseOrderStatus(string status)
    {
        return status?.ToUpperInvariant() switch
        {
            "RECEIVED" => PurchaseOrderStatus.Received,
            "NOT_RECEIVED" => PurchaseOrderStatus.Pending,
            "APPROVED" => PurchaseOrderStatus.Approved,
            "CANCELLED" => PurchaseOrderStatus.Cancelled,
            _ => PurchaseOrderStatus.Pending
        };
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
