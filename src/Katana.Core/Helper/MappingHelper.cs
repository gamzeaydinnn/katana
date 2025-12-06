using System;
using System.Collections.Generic;
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
        var sku = string.IsNullOrWhiteSpace(katanaProduct.SKU)
            ? katanaProduct.GetProductCode()
            : katanaProduct.SKU;

        var name = string.IsNullOrWhiteSpace(katanaProduct.Name)
            ? katanaProduct.GetProductCode()
            : katanaProduct.Name;

        return new Product
        {
            SKU = sku,
            Name = name,
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

    public static LucaIrsaliyeDto MapToLucaIrsaliye(
        Order order,
        Customer customer,
        List<OrderItem> items,
        long belgeTurDetayId)
    {
        var documentDate = order.OrderDate == default ? order.CreatedAt : order.OrderDate;

        return new LucaIrsaliyeDto
        {
            BelgeSeri = "IRS",
            BelgeNo = string.IsNullOrWhiteSpace(order.OrderNo) ? order.Id.ToString() : order.OrderNo,
            BelgeTarihi = documentDate,
            BelgeTurDetayId = belgeTurDetayId,
            MusteriTedarikci = 1,
            KdvFlag = true,
            YuklemeTarihi = documentDate,
            CariKodu = GenerateCustomerCode(customer.TaxNo),
            ParaBirimKod = string.IsNullOrWhiteSpace(order.Currency) ? "TRY" : order.Currency,
            DetayList = items.Select(item => new LucaIrsaliyeDetayDto
            {
                KartTuru = 1,
                KartKodu = NormalizeSku(item.Product?.SKU ?? string.Empty),
                Miktar = item.Quantity,
                BirimFiyat = (double)item.UnitPrice,
                OlcuBirimi = "ADET",
                KdvOran = 0.18,
                DepoKodu = "002" // Luca MERKEZ DEPO kodu
            }).ToList()
        };
    }

    /// <summary>
    /// Maps SalesOrder entity to Luca sales order header request
    /// </summary>
    public static LucaCreateOrderHeaderRequest MapToLucaSalesOrderHeader(
        Entities.SalesOrder order,
        Customer customer)
    {
        var cariKod = !string.IsNullOrWhiteSpace(customer.LucaCode) 
            ? customer.LucaCode 
            : GenerateCustomerCode(customer.TaxNo);
        var belgeTakipNo = string.IsNullOrWhiteSpace(order.OrderNo) ? order.Id.ToString() : order.OrderNo;

        return new LucaCreateOrderHeaderRequest
        {
            BelgeSeri = order.BelgeSeri ?? "SAT",
            BelgeNo = ParseDocumentNo(order.BelgeNo ?? order.OrderNo, order.Id),
            BelgeTakipNo = belgeTakipNo,
            BelgeTarihi = order.OrderCreatedDate ?? DateTime.UtcNow,
            DuzenlemeSaati = order.DuzenlemeSaati ?? DateTime.Now.ToString("HH:mm"),
            BelgeTurDetayId = order.BelgeTurDetayId ?? 17,
            TeklifSiparisTur = order.TeklifSiparisTur ?? 1,
            NakliyeBedeliTuru = order.NakliyeBedeliTuru ?? 0,
            OnayFlag = order.OnayFlag,
            ParaBirimKod = string.IsNullOrWhiteSpace(order.Currency) ? "TRY" : order.Currency,
            KdvFlag = true,
            TeslimTarihi = order.DeliveryDate,
            TeslimTarihiFlag = order.DeliveryDate.HasValue,
            CariKodu = cariKod,
            CariTanim = customer.Title,
            CariYasalUnvan = customer.Title,
            VergiNo = customer.Type == 1 ? customer.TaxNo : null,
            TcKimlikNo = customer.Type == 2 ? customer.TaxNo : null,
            VergiDairesi = customer.TaxOffice,
            CariTip = customer.Type == 1 ? 0 : 1,
            ReferansNo = order.CustomerRef,
            BelgeAciklama = order.AdditionalInfo,
            DetayList = order.Lines.Select(l => new LucaCreateOrderDetailRequest
            {
                KartTuru = 1,
                KartKodu = NormalizeSku(l.SKU),
                KartAdi = l.ProductName,
                BirimFiyat = (double)(l.PricePerUnit ?? 0),
                Miktar = (double)l.Quantity,
                KdvOran = (double)(l.TaxRate ?? 20),
                Tutar = (double?)(l.Total)
            }).ToList()
        };
    }

    public static LucaCreateOrderHeaderRequest MapToLucaSalesOrderHeader(
        Order order,
        Customer customer,
        List<OrderItem> items,
        long belgeTurDetayId,
        string belgeSeri)
    {
        var cariKod = GenerateCustomerCode(customer.TaxNo);
        var belgeTakipNo = string.IsNullOrWhiteSpace(order.OrderNo) ? order.Id.ToString() : order.OrderNo;

        return new LucaCreateOrderHeaderRequest
        {
            BelgeSeri = belgeSeri,
            BelgeNo = ParseDocumentNo(order.OrderNo, order.Id),
            BelgeTakipNo = belgeTakipNo,
            BelgeTarihi = order.OrderDate == default ? DateTime.UtcNow : order.OrderDate,
            BelgeTurDetayId = belgeTurDetayId,
            TeklifSiparisTur = 1,
            ParaBirimKod = string.IsNullOrWhiteSpace(order.Currency) ? "TRY" : order.Currency,
            KdvFlag = true,
            CariKodu = cariKod,
            CariTanim = customer.Title,
            VergiNo = customer.TaxNo,
            DetayList = items.Select(i => new LucaCreateOrderDetailRequest
            {
                KartTuru = 1,
                KartKodu = NormalizeSku(i.Product?.SKU ?? string.Empty),
                BirimFiyat = (double)i.UnitPrice,
                Miktar = i.Quantity,
                KdvOran = ResolveOrderItemKdvOran(i)
            }).ToList()
        };
    }

    public static LucaCreateOrderHeaderRequest MapToLucaPurchaseOrderHeader(
        PurchaseOrder po,
        Supplier supplier,
        List<PurchaseOrderItem> items,
        long belgeTurDetayId,
        string belgeSeri)
    {
        var cariKod = string.IsNullOrWhiteSpace(supplier.Code) ? supplier.Id.ToString() : supplier.Code!;
        var belgeTakipNo = string.IsNullOrWhiteSpace(po.OrderNo) ? po.Id.ToString() : po.OrderNo;

        return new LucaCreateOrderHeaderRequest
        {
            BelgeSeri = belgeSeri,
            BelgeNo = ParseDocumentNo(po.OrderNo, po.Id),
            BelgeTakipNo = belgeTakipNo,
            BelgeTarihi = po.OrderDate == default ? DateTime.UtcNow : po.OrderDate,
            BelgeTurDetayId = belgeTurDetayId,
            TeklifSiparisTur = 1,
            ParaBirimKod = "TRY",
            KdvFlag = true,
            CariKodu = cariKod,
            CariTanim = supplier.Name,
            VergiNo = supplier.TaxNo,
            DetayList = items.Select(i => new LucaCreateOrderDetailRequest
            {
                KartTuru = 2,
                KartKodu = NormalizeSku(i.Product?.SKU ?? string.Empty),
                BirimFiyat = (double)i.UnitPrice,
                Miktar = i.Quantity,
                KdvOran = ResolvePurchaseOrderItemKdvOran(i)
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

    /// <summary>
    /// Maps PurchaseOrder entity (with Luca fields) to Luca purchase order request.
    /// Uses entity's own Luca-related properties.
    /// </summary>
    public static LucaCreatePurchaseOrderRequest MapToLucaPurchaseOrderFromEntity(PurchaseOrder po, Supplier supplier)
    {
        var cariKod = !string.IsNullOrWhiteSpace(supplier.Code) 
            ? supplier.Code 
            : $"TED{supplier.Id:D6}";
        
        var belgeTakipNo = string.IsNullOrWhiteSpace(po.OrderNo) ? po.Id.ToString() : po.OrderNo;
        
        return new LucaCreatePurchaseOrderRequest
        {
            BelgeSeri = po.DocumentSeries ?? "A",
            BelgeNo = po.LucaDocumentNo ?? belgeTakipNo,
            BelgeTakipNo = belgeTakipNo,
            BelgeTarihi = po.OrderDate == default ? DateTime.UtcNow : po.OrderDate,
            BelgeTurDetayId = po.DocumentTypeDetailId > 0 ? po.DocumentTypeDetailId : 2,
            TeklifSiparisTur = 1,
            OnayFlag = 0,
            ParaBirimKod = "TRY",
            KdvFlag = po.VatIncluded,
            TeslimTarihi = po.ExpectedDate ?? DateTime.UtcNow.AddDays(7),
            TeslimTarihiFlag = po.ExpectedDate.HasValue,
            CariKodu = cariKod,
            CariTanim = supplier.Name,
            CariYasalUnvan = supplier.Name,
            VergiNo = supplier.TaxNo,
            OzelKod = po.ReferenceCode ?? $"KAT-PO-{po.Id}",
            ProjeKodu = po.ProjectCode,
            BelgeAciklama = po.Description,
            SevkAdresiId = po.ShippingAddressId,
            DetayList = po.Items.Select(item => new LucaCreatePurchaseOrderDetailRequest
            {
                KartTuru = 1, // Stok
                KartKodu = !string.IsNullOrWhiteSpace(item.LucaStockCode) 
                    ? item.LucaStockCode 
                    : NormalizeSku(item.Product?.SKU ?? string.Empty),
                KartAdi = item.Product?.Name,
                DepoKodu = item.WarehouseCode ?? "01",
                BirimKodu = item.UnitCode ?? "AD",
                BirimFiyat = (double)item.UnitPrice,
                Miktar = item.Quantity,
                KdvOran = (double)item.VatRate,
                IskontoTutar = (double)item.DiscountAmount,
                Tutar = (double)(item.UnitPrice * item.Quantity - item.DiscountAmount)
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

    public static LucaCreateWarehouseTransferRequest MapToLucaDepoTransferCreate(
        StockTransfer transfer,
        long belgeTurDetayId,
        string belgeSeri)
    {
        if (belgeTurDetayId != 33 && belgeTurDetayId != 182)
        {
            throw new ArgumentException("Depo transferi için belgeTurDetayId 33 veya 182 olmalıdır", nameof(belgeTurDetayId));
        }

        var girisDepo = NormalizeWarehouseCode(transfer.ToWarehouse);
        var cikisDepo = NormalizeWarehouseCode(transfer.FromWarehouse);

        return new LucaCreateWarehouseTransferRequest
        {
            BelgeTurDetayId = belgeTurDetayId,
            BelgeSeri = belgeSeri,
            BelgeNo = null,
            BelgeTarihi = transfer.TransferDate == default ? DateTime.UtcNow : transfer.TransferDate,
            BelgeTakipNo = null,
            BelgeAciklama = null,
            GirisDepoKodu = girisDepo,
            CikisDepoKodu = cikisDepo,
            DetayList = new List<LucaWarehouseTransferDetailRequest>
            {
                new LucaWarehouseTransferDetailRequest
                {
                    KartKodu = NormalizeSku(transfer.Product?.SKU ?? string.Empty),
                    Miktar = transfer.Quantity,
                    OlcuBirimi = null,
                    Aciklama = null
                }
            }
        };
    }

    public static LucaCreateStockCountRequest MapToLucaSayimFisi(Stock stock, long belgeTurDetayId)
    {
        return new LucaCreateStockCountRequest
        {
            BelgeTurDetayId = belgeTurDetayId,
            BelgeTarihi = stock.Timestamp == default ? DateTime.UtcNow : stock.Timestamp,
            DepoKodu = NormalizeWarehouseCode(stock.Location),
            KapamaBelgeOlustur = true,
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

    public static LucaCreateCariHareketRequest MapToLucaCariHareketCreate(
        Payment payment,
        Customer customer,
        long belgeTurDetayId,
        int cariTuru,
        string belgeSeri,
        bool avansFlag,
        string? aciklama = null)
    {
        var cariKod = GenerateCustomerCode(customer.TaxNo);
        var paraBirim = !string.IsNullOrWhiteSpace(payment.Invoice?.Currency)
            ? payment.Invoice!.Currency
            : "TRY";
        var detayAciklama = aciklama ?? payment.PaymentMethod;

        return new LucaCreateCariHareketRequest
        {
            BelgeSeri = belgeSeri,
            BelgeNo = null,
            BelgeTarihi = payment.PaymentDate,
            BelgeTurDetayId = belgeTurDetayId,
            BelgeAciklama = aciklama,
            CariTuru = cariTuru,
            ParaBirimKod = paraBirim,
            CariKodu = cariKod,
            DetayList = new List<LucaCreateCariHareketDetayRequest>
            {
                new LucaCreateCariHareketDetayRequest
                {
                    KartTuru = cariTuru,
                    KartKodu = cariKod,
                    AvansFlag = avansFlag,
                    Tutar = (double)payment.Amount,
                    Aciklama = detayAciklama
                }
            }
        };
    }

    public static LucaCreateCreditCardEntryRequest MapToLucaKrediKartiGiris(
        Payment payment,
        Customer customer,
        string belgeSeri,
        string kasaCariKodu,
        DateTime? vadeTarihi = null,
        bool? avansFlag = null)
    {
        var cariKod = GenerateCustomerCode(customer.TaxNo);
        var detayAciklama = payment.PaymentMethod;

        return new LucaCreateCreditCardEntryRequest
        {
            BelgeSeri = belgeSeri,
            BelgeTarihi = payment.PaymentDate,
            VadeTarihi = vadeTarihi ?? payment.PaymentDate,
            BelgeAciklama = $"Ödeme - {customer.Title}",
            CariKodu = kasaCariKodu,
            DetayList = new List<LucaCreditCardEntryDetailRequest>
            {
                new LucaCreditCardEntryDetailRequest
                {
                    KartTuru = 1,
                    KartKodu = cariKod,
                    AvansFlag = avansFlag,
                    Tutar = (decimal)payment.Amount,
                    VadeTarihi = vadeTarihi ?? payment.PaymentDate,
                    Aciklama = detayAciklama
                }
            }
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
        return MapToLucaStockCard(product, null, null);
    }

    /// <summary>
    /// Katana Product -> Koza stok kartı oluşturma DTO'su.
    /// Ölçü birimi ID'si farklı şube/kurulumlara göre değişebildiğinden parametre ile alınabilir.
    /// </summary>
    public static LucaCreateStokKartiRequest MapToLucaStockCard(Product product, long? olcumBirimiId, double? defaultVat)
    {
        var normalizedSku = NormalizeSkuPreserve(product.SKU);
        if (string.IsNullOrWhiteSpace(normalizedSku))
        {
            normalizedSku = $"KAT-{(product.Id > 0 ? product.Id : Guid.NewGuid().ToString("N")[..8])}";
        }
        var baseName = string.IsNullOrWhiteSpace(product.Name) ? normalizedSku : product.Name;
        var kartAdi = TrimAndTruncate(baseName, 255) ?? normalizedSku;
        // Per Admin: UzunAdi should include SKU and product name, e.g. "BFM-01 / Metal Levha 10mm"
        var uzunAdi = TrimAndTruncate($"{normalizedSku} / {kartAdi}", 500) ?? kartAdi;
        var kategoriKod = TrimAndTruncate(product.CategoryId > 0 ? product.CategoryId.ToString() : string.Empty, 50) ?? string.Empty;
        var barkod = normalizedSku;
        var detayAciklama = TrimAndTruncate(product.Description, 1000) ?? string.Empty;
        var startDate = (product.CreatedAt == default ? DateTime.UtcNow : product.CreatedAt).Date;

        // Dokümana göre KDV oranları 0.18 formatında gönderilmeli (%18)
        var vat = defaultVat ?? 0.18;

        return new LucaCreateStokKartiRequest
        {
            KartAdi = kartAdi,
            KartTuru = 1,
            BaslangicTarihi = startDate.ToString("dd/MM/yyyy"),
            // Dokümana göre "Adet" için 5 tipik ID; gerekirse dışarıdan sağlanır.
            OlcumBirimiId = olcumBirimiId ?? 5,
            KartKodu = normalizedSku,
            MaliyetHesaplanacakFlag = 1,
            KartTipi = 1,
            // Stok kartı için kategoriAgacKod boş olmalı (depo kartından farklı)
            KategoriAgacKod = string.Empty,
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
            AlisTevkifatOran = null,           // Luca doc: "7/10" formatında string veya null
            AlisTevkifatTipId = null,          // Luca doc: alisTevkifatTipId (NOT: alisTevkifatKod DEĞİL!)
            IhracatKategoriNo = string.Empty,
            SatisTevkifatOran = null,          // Luca doc: "2/10" formatında string veya null
            SatisTevkifatTipId = null,         // Luca doc: satisTevkifatTipId (NOT: satisTevkifatKod DEĞİL!)
            MinStokKontrol = 0,
            MinStokMiktari = 0,
            AlisIskontoOran1 = 0,
            SatilabilirFlag = product.IsActive ? 1 : 0,
            SatinAlinabilirFlag = product.IsActive ? 1 : 0,
            UtsVeriAktarimiFlag = 0,
            BagDerecesi = 0,
            MaxStokKontrol = 0,
            MaxStokMiktari = 0,
            SatisIskontoOran1 = 0,
            SatisAlternatifFlag = 0,
            UretimSuresi = 0,
            UretimSuresiBirim = 0,
            SeriNoFlag = 0,
            LotNoFlag = 0,
            DetayAciklama = detayAciklama,
            OtvMaliyetFlag = 0,
            OtvTutarKdvFlag = 0,
            OtvIskontoFlag = 0,
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

    /// <summary>
    /// KatanaProductDto (harici API) -> Koza stok kartı DTO'su.
    /// SKU boşsa KAT-{Id} kodu kullanılır, KDV ve para birimi için güvenli varsayılanlar atanır.
    /// </summary>
    public static LucaCreateStokKartiRequest MapToLucaStockCard(KatanaProductDto product, long? olcumBirimiId = null, double? defaultVat = null)
    {
        var skuRaw = string.IsNullOrWhiteSpace(product.SKU) ? product.GetProductCode() : product.SKU;
        var sku = NormalizeSkuPreserve(skuRaw);
        if (string.IsNullOrWhiteSpace(sku))
        {
            sku = $"KAT-{(string.IsNullOrWhiteSpace(product.Id) ? Guid.NewGuid().ToString("N")[..8] : product.Id)}";
        }
        var name = !string.IsNullOrWhiteSpace(product.Name) ? product.Name : sku;
        var desc = TrimAndTruncate(product.Description, 1000) ?? string.Empty;
        var vatRate = product.VatRate.HasValue ? product.VatRate.Value / 100d : (defaultVat ?? 0.20);
        var startDate = DateTime.UtcNow.Date;

        // Per Admin guidance: KartKodu will be set from category mappings by LoaderService before send.
        // Ensure UzunAdi contains SKU and product name so product code is preserved in Koza's card record.
        var uzun = TrimAndTruncate($"{sku} / {TrimAndTruncate(name, 255) ?? sku}", 500) ?? name;

        // 🔥 KRİTİK FİX: Versiyonlu SKU'lar için barkod NULL olmalı (Duplicate Barcode hatasını önlemek için)
        bool isVersionedSku = System.Text.RegularExpressions.Regex.IsMatch(sku, @"-V\d+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        string? barcodeToSend = null;
        
        if (isVersionedSku)
        {
            // Versiyonlu SKU - Barkod NULL gönder
            barcodeToSend = null;
        }
        else
        {
            // Normal SKU - Barkod gönder
            barcodeToSend = string.IsNullOrWhiteSpace(product.Barcode) ? sku : product.Barcode;
        }
        
        return new LucaCreateStokKartiRequest
        {
            KartAdi = TrimAndTruncate(name, 255) ?? sku,
            KartTuru = 1,
            BaslangicTarihi = startDate.ToString("dd/MM/yyyy"),
            OlcumBirimiId = olcumBirimiId ?? 5, // "Adet" varsayılanı
            KartKodu = sku,
            MaliyetHesaplanacakFlag = 1,
            KartTipi = 1,
            // Stok kartı için kategoriAgacKod boş olmalı (depo kartından farklı)
            KategoriAgacKod = string.Empty,
            KartAlisKdvOran = vatRate,
            KartSatisKdvOran = vatRate,
            Barkod = barcodeToSend, // 🔥 Versiyonlu SKU'lar için NULL
            UzunAdi = uzun,
            PerakendeAlisBirimFiyat = (double)(product.PurchasePrice ?? product.Price),
            PerakendeSatisBirimFiyat = (double)(product.SalesPrice ?? product.Price),
            SatilabilirFlag = product.IsActive ? 1 : 0,
            SatinAlinabilirFlag = product.IsActive ? 1 : 0,
            DetayAciklama = desc
        };
    }

    /// <summary>
    /// Luca stok kartı gönderimi öncesi minimum alan doğrulaması.
    /// </summary>
    public static (bool IsValid, List<string> Errors) ValidateLucaStockCard(LucaCreateStokKartiRequest stockCard)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(stockCard.KartAdi))
            errors.Add("kartAdi zorunlu");
        if (string.IsNullOrWhiteSpace(stockCard.KartKodu))
            errors.Add("kartKodu (SKU) zorunlu");
        if (stockCard.OlcumBirimiId <= 0)
            errors.Add("olcumBirimiId zorunlu");
        if (stockCard.KartAlisKdvOran < 0)
            errors.Add("KartAlisKdvOran geçersiz");
        if (stockCard.KartSatisKdvOran < 0)
            errors.Add("KartSatisKdvOran geçersiz");
        if (stockCard.PerakendeAlisBirimFiyat < 0 || stockCard.PerakendeSatisBirimFiyat < 0)
            errors.Add("Birim fiyatlar negatif olamaz");
        if (stockCard.KartSatisKdvOran < 0 || stockCard.KartSatisKdvOran > 1)
            errors.Add("KartSatisKdvOran 0 ile 1 arasında olmalı");
        if (stockCard.KartAlisKdvOran < 0 || stockCard.KartAlisKdvOran > 1)
            errors.Add("KartAlisKdvOran 0 ile 1 arasında olmalı");

        return (!errors.Any(), errors);
    }

    /// <summary>
    /// Hizmet kartı (KartTuru = 2) için stok kartı DTO'su.
    /// Temel alanları MapToLucaStockCard'dan devralır, sadece kart türünü günceller.
    /// </summary>
    public static LucaCreateStokKartiRequest MapToLucaServiceStockCard(Product product)
    {
        var dto = MapToLucaStockCard(product);
        dto.KartTuru = 2;
        return dto;
    }

    public static LucaUpdateStockCardRequest MapToLucaStockCardUpdate(Product product, long skartId)
    {
        var normalizedSku = NormalizeSku(product.SKU);
        var cardName = TrimAndTruncate(product.Name, 255) ?? normalizedSku;

        return new LucaUpdateStockCardRequest
        {
            SkartId = skartId,
            KartAdi = cardName,
            KategoriAgacKod = product.CategoryId > 0 ? product.CategoryId.ToString() : null,
            Barkod = normalizedSku,
            OlcumBirimiId = 5,
            DetayAciklama = TrimAndTruncate(product.Description, 1000)
        };
    }

    public static LucaUpdateInvoiceRequest MapToLucaInvoiceUpdate(Invoice invoice, long faturaId)
    {
        return new LucaUpdateInvoiceRequest
        {
            FaturaId = faturaId,
            BelgeAciklama = TrimAndTruncate(invoice.Notes ?? $"Invoice {invoice.InvoiceNo}", 250),
            VadeTarihi = invoice.DueDate,
            ReferansNo = invoice.InvoiceNo
        };
    }

    public static LucaUpdateCustomerRequest MapToLucaCustomerUpdate(Customer customer, string cariKodu)
    {
        return new LucaUpdateCustomerRequest
        {
            CariKodu = cariKodu,
            CariAdi = TrimAndTruncate(customer.Title, 200) ?? cariKodu,
            VergiNo = string.IsNullOrWhiteSpace(customer.TaxNo) ? null : customer.TaxNo,
            VergiDairesi = null,
            Adres = TrimAndTruncate(customer.Address, 500),
            Telefon = customer.Phone,
            Email = customer.Email
        };
    }

    public static LucaUpdateSupplierRequest MapToLucaSupplierUpdate(Supplier supplier, string tedarikciKodu)
    {
        return new LucaUpdateSupplierRequest
        {
            TedarikciKodu = tedarikciKodu,
            Tanim = TrimAndTruncate(supplier.Name, 200) ?? tedarikciKodu,
            VergiNo = supplier.TaxNo,
            Email = supplier.Email,
            Telefon = supplier.Phone
        };
    }

    public static LucaUpdateSalesOrderRequest MapToLucaSalesOrderUpdate(Order order, long siparisId)
    {
        var description = TrimAndTruncate($"Sipariş {order.OrderNo}", 250);
        var deliveryDate = order.UpdatedAt ?? order.OrderDate;

        return new LucaUpdateSalesOrderRequest
        {
            SiparisId = siparisId,
            TeslimTarihi = deliveryDate,
            Aciklama = description
        };
    }

    public static LucaGetStockCardDetailRequest MapToLucaGetStockCardDetail(long skartId)
    {
        return new LucaGetStockCardDetailRequest
        {
            StkSkart = new LucaStockCardKey
            {
                SkartId = skartId
            }
        };
    }

    public static LucaGetInvoiceDetailRequest MapToLucaGetInvoiceDetail(long faturaId)
    {
        return new LucaGetInvoiceDetailRequest
        {
            FaturaId = faturaId
        };
    }

    public static LucaGetCustomerDetailRequest MapToLucaGetCustomerDetail(long finansalNesneId)
    {
        return new LucaGetCustomerDetailRequest
        {
            FinansalNesneId = finansalNesneId
        };
    }

    public static LucaCreateCheckRequest MapToLucaCreateCheck(Payment payment, Customer customer)
    {
        var cariKod = GenerateCustomerCode(customer.TaxNo);
        var cekNo = $"CHK-{(payment.Invoice?.InvoiceNo ?? payment.InvoiceId.ToString())}";
        var description = TrimAndTruncate(payment.Invoice?.Notes ?? $"Çek - {customer.Title}", 250);

        return new LucaCreateCheckRequest
        {
            CekNo = cekNo,
            VadeTarihi = payment.Invoice?.DueDate ?? payment.PaymentDate,
            Tutar = (double)payment.Amount,
            CariKodu = cariKod,
            Aciklama = description
        };
    }

    public static LucaCreateBondRequest MapToLucaCreateBond(Payment payment, Customer customer)
    {
        var cariKod = GenerateCustomerCode(customer.TaxNo);
        var senetNo = $"BND-{(payment.Invoice?.InvoiceNo ?? payment.InvoiceId.ToString())}";
        var description = TrimAndTruncate(payment.Invoice?.Notes ?? $"Senet - {customer.Title}", 250);

        return new LucaCreateBondRequest
        {
            SenetNo = senetNo,
            VadeTarihi = payment.Invoice?.DueDate ?? payment.PaymentDate,
            Tutar = (double)payment.Amount,
            CariKodu = cariKod,
            Aciklama = description
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
        
        // Tip belirleme: 1=Şirket (VKN 10 hane), 2=Şahıs (TCKN 11 hane)
        // Önce customer.Type'a bak, yoksa TaxNo uzunluğuna göre belirle
        int tip;
        if (customer.Type > 0)
        {
            tip = customer.Type; // Entity'den gelen değer
        }
        else
        {
            // TaxNo uzunluğuna göre otomatik belirle
            tip = !string.IsNullOrWhiteSpace(customer.TaxNo) && customer.TaxNo.Length == 11 ? 2 : 1;
        }
        
        var vergiNo = tip == 1 && !string.IsNullOrWhiteSpace(customer.TaxNo) ? customer.TaxNo : null;
        var tcKimlikNo = tip == 2 ? customer.TaxNo : null;
        var shortTitle = TrimAndTruncate(title, 50);
        
        // Luca cari kart kodu - CK-{Id} formatında
        var kartKodu = !string.IsNullOrWhiteSpace(customer.LucaCode) 
            ? customer.LucaCode 
            : customer.GenerateLucaCode();

        return new LucaCreateCustomerRequest
        {
            Tip = tip,
            CariTipId = 1, // Müşteri
            Tanim = title,
            KartKod = kartKodu, // ÖNEMLİ: Artık boş değil, CK-{Id} formatında
            KategoriKod = kategoriKod ?? customer.GroupCode,
            ParaBirimKod = "TRY", // Luca'da TRY zorunlu, customer.Currency DB'de saklanır
            VergiNo = vergiNo,
            KisaAd = tip == 1 ? shortTitle : null,
            YasalUnvan = tip == 1 ? title : null,
            TcKimlikNo = tcKimlikNo,
            Ad = tip == 2 ? title : null,
            Soyad = null,
            VergiDairesiId = vergiDairesiId,
            Ulke = string.IsNullOrWhiteSpace(customer.Country) ? "TURKIYE" : NormalizeCountry(customer.Country),
            Il = customer.City,
            Ilce = customer.District,
            AdresSerbest = address,
            IletisimTanim = customer.Phone ?? customer.Email,
            EfaturaTuru = null,
            TakipNoFlag = true,
            MutabakatMektubuGonderilecek = null,
            // Ek alanlar
            OzelKod = customer.ReferenceId // Katana reference_id → Luca özel kod
        };
    }
    
    /// <summary>
    /// Luca cari kart güncelleme DTO'su oluşturur
    /// </summary>
    public static LucaUpdateCustomerFullRequest MapToLucaCustomerFullUpdate(Customer customer, string kartKodu)
    {
        var title = TrimAndTruncate(customer.Title, 200) ?? kartKodu;
        var address = TrimAndTruncate(customer.Address, 500);
        var tip = customer.Type > 0 ? customer.Type : 
            (!string.IsNullOrWhiteSpace(customer.TaxNo) && customer.TaxNo.Length == 11 ? 2 : 1);

        return new LucaUpdateCustomerFullRequest
        {
            KartKod = kartKodu,
            Tip = tip,
            Tanim = title,
            KisaAd = TrimAndTruncate(title, 50),
            YasalUnvan = tip == 1 ? title : null,
            VergiNo = tip == 1 ? customer.TaxNo : null,
            TcKimlikNo = tip == 2 ? customer.TaxNo : null,
            Ad = tip == 2 ? title : null,
            Ulke = NormalizeCountry(customer.Country ?? "Turkey"),
            Il = customer.City,
            Ilce = customer.District,
            AdresSerbest = address,
            Telefon = customer.Phone,
            Email = customer.Email,
            OzelKod = customer.ReferenceId,
            KategoriKod = customer.GroupCode
        };
    }
    
    /// <summary>
    /// Ülke adını normalize eder (Luca formatına uygun)
    /// </summary>
    private static string NormalizeCountry(string? country)
    {
        if (string.IsNullOrWhiteSpace(country)) return "TURKIYE";
        
        var normalized = country.Trim().ToUpperInvariant();
        return normalized switch
        {
            "TURKEY" => "TURKIYE",
            "TÜRKİYE" => "TURKIYE",
            "TR" => "TURKIYE",
            _ => normalized
        };
    }

    /// <summary>
    /// Customer adresi → Luca adres DTO
    /// NOT: Katana state → atlanır, city → ilce, district → Luca'da yok
    /// </summary>
    public static LucaAdresDto MapToLucaAddress(
        string? addressLine1, 
        string? addressLine2, 
        string? city, 
        string? zipCode, 
        string? country, 
        string? phone,
        long finansalNesneId,
        string addressType = "billing",
        bool isDefault = true)
    {
        return new LucaAdresDto
        {
            AdresTipId = addressType.Equals("billing", StringComparison.OrdinalIgnoreCase) ? 1 : 2,
            AdresSerbest = addressLine1,
            Adres2 = addressLine2,
            Ilce = city, // Luca'da şehir = ilçe olarak gönderilir
            PostaKodu = zipCode,
            Ulke = NormalizeCountry(country),
            Telefon = phone,
            VarsayilanFlag = isDefault ? 1 : 0,
            FinansalNesneId = finansalNesneId
        };
    }
    
    /// <summary>
    /// Customer entity'den Luca adres DTO oluşturur
    /// </summary>
    public static LucaAdresDto MapCustomerToLucaAddress(Customer customer, long finansalNesneId)
    {
        return new LucaAdresDto
        {
            AdresTipId = 1, // Fatura adresi
            AdresSerbest = customer.Address,
            Ilce = customer.City, // Luca'da city → ilce
            Ulke = NormalizeCountry(customer.Country),
            Telefon = customer.Phone,
            VarsayilanFlag = 1,
            FinansalNesneId = finansalNesneId
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

    public static LucaCreateDshBaslikRequest MapToLucaDshBaslik(
        Stock stock,
        Product product,
        long belgeTurDetayId,
        string belgeSeri,
        string? belgeAciklama = null,
        string? paraBirimKod = null,
        string? depoKodu = null)
    {
        var warehouseCode = NormalizeWarehouseCode(depoKodu ?? stock.Location);
        var hareketYonu = (stock.Type ?? string.Empty).ToUpperInvariant() == "IN" ? "GIRIS" : "CIKIS";

        return new LucaCreateDshBaslikRequest
        {
            BelgeSeri = belgeSeri,
            BelgeNo = null,
            BelgeAciklama = belgeAciklama ?? TrimAndTruncate(stock.Reason ?? stock.Reference, 250),
            BelgeTurDetayId = belgeTurDetayId,
            BelgeTarihi = stock.Timestamp == default ? DateTime.UtcNow : stock.Timestamp,
            DepoKodu = warehouseCode,
            ParaBirimKod = paraBirimKod ?? "TRY",
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
            TaxNo = dto.TaxNo ?? string.Empty,
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

    public static AccountingRecord MapFromLucaCustomerTransaction(LucaCustomerTransactionDto dto)
    {
        return CreateAccountingRecord(
            string.IsNullOrWhiteSpace(dto.BelgeNo) ? dto.HareketId.ToString() : dto.BelgeNo,
            dto.Tutar,
            "CUSTOMER_TRANSACTION",
            dto.BelgeTarihi,
            dto.Aciklama ?? $"Cari hareket - {dto.CariKodu}",
            "CUSTOMER");
    }

    public static AccountingRecord MapFromLucaCheck(LucaCheckDto dto)
    {
        return CreateAccountingRecord(
            dto.CekNo,
            dto.Tutar,
            "CHECK",
            dto.VadeTarihi,
            dto.CariAdi ?? dto.CariKodu,
            "CHEQUE");
    }

    public static AccountingRecord MapFromLucaBond(LucaBondDto dto)
    {
        return CreateAccountingRecord(
            dto.SenetNo,
            dto.Tutar,
            "BOND",
            dto.VadeTarihi,
            dto.CariAdi ?? dto.CariKodu,
            "BOND");
    }

    public static AccountingRecord MapFromLucaCash(LucaCashDto dto)
    {
        return CreateAccountingRecord(
            dto.KasaKodu,
            dto.Bakiye,
            "CASH",
            DateTime.UtcNow,
            dto.KasaAdi,
            "CASH");
    }

    public static AccountingRecord MapFromLucaCashTransaction(LucaCashTransactionDto dto)
    {
        return CreateAccountingRecord(
            dto.BelgeNo,
            dto.Tutar,
            "CASH_TRANSACTION",
            dto.Tarih,
            dto.Aciklama,
            "CASH");
    }

    public static Customer MapFromLucaCustomerDetail(LucaCustomerDetailDto dto)
    {
        var taxNo = string.IsNullOrWhiteSpace(dto.VergiNo) ? dto.CariKodu : dto.VergiNo;

        var title = string.IsNullOrWhiteSpace(dto.CariAdi) ? taxNo ?? string.Empty : dto.CariAdi;

        return new Customer
        {
            TaxNo = taxNo ?? string.Empty,
            Title = title,
            Phone = dto.Telefon,
            Email = dto.Email,
            Address = dto.Adres,
            City = null,
            Country = "TURKIYE",
            IsActive = true,
            IsSynced = true,
            SyncedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static Invoice MapFromLucaInvoiceDetail(LucaInvoiceDetailDto dto)
    {
        var invoiceNo = string.IsNullOrWhiteSpace(dto.BelgeSeri)
            ? dto.BelgeNo.ToString()
            : $"{dto.BelgeSeri}{dto.BelgeNo}";

        var items = dto.DetayList?.Select(line => new InvoiceItem
        {
            ProductId = 0,
            ProductName = line.Description ?? (line.ProductCode ?? ""),
            ProductSKU = NormalizeSku(line.ProductCode ?? string.Empty),
            Quantity = (int)Math.Round(line.Quantity),
            UnitPrice = line.UnitPrice,
            TaxRate = line.TaxRate,
            TaxAmount = line.TaxAmount,
            TotalAmount = line.GrossAmount,
            Unit = string.IsNullOrWhiteSpace(line.Unit) ? "ADET" : line.Unit!
        }).ToList() ?? new List<InvoiceItem>();

        var netAmount = items.Sum(i => i.TotalAmount - i.TaxAmount);
        var taxAmount = items.Sum(i => i.TaxAmount);
        var reportedTotal = (decimal)dto.ToplamTutar;
        decimal totalAmount = dto.ToplamTutar != default
            ? reportedTotal
            : items.Sum(i => i.TotalAmount);

        var invoice = new Invoice
        {
            InvoiceNo = invoiceNo ?? dto.FaturaId.ToString(),
            CustomerId = 0,
            Amount = netAmount,
            TaxAmount = taxAmount,
            TotalAmount = totalAmount,
            Status = "RECEIVED",
            InvoiceDate = dto.BelgeTarihi,
            DueDate = dto.BelgeTarihi,
            Currency = "TRY",
            Notes = dto.CariAdi,
            IsSynced = true,
            SyncedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            InvoiceItems = items
        };

        return invoice;
    }

    public static Product MapFromLucaStockCardDetail(LucaStockCardDetailDto dto)
    {
        var sku = NormalizeSku(dto.KartKodu ?? string.Empty);
        var name = TrimAndTruncate(dto.KartAdi, 200) ?? sku;
        var categoryId = 0;
        if (!string.IsNullOrWhiteSpace(dto.KategoriAgacKod) && int.TryParse(dto.KategoriAgacKod, out var parsedCategory))
        {
            categoryId = parsedCategory;
        }

        return new Product
        {
            SKU = sku,
            Name = name,
            Description = TrimAndTruncate(dto.DetayAciklama, 1000),
            CategoryId = categoryId,
            Price = 0m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static PurchaseOrder MapFromLucaPurchaseOrder(LucaPurchaseOrderDto dto)
    {
        var orderNo = string.IsNullOrWhiteSpace(dto.BelgeNo) ? dto.SiparisId.ToString() : dto.BelgeNo;
        var items = dto.DetayList?.Select(line => new PurchaseOrderItem
        {
            ProductId = 0,
            Quantity = Convert.ToInt32(Math.Round(line.Miktar)),
            UnitPrice = Convert.ToDecimal(line.BirimFiyat)
        }).ToList() ?? new List<PurchaseOrderItem>();

        var totalAmount = dto.ToplamTutar.HasValue
            ? Convert.ToDecimal(dto.ToplamTutar.Value)
            : items.Sum(i => i.UnitPrice * i.Quantity);

        var purchaseOrder = new PurchaseOrder
        {
            OrderNo = orderNo ?? dto.SiparisId.ToString(),
            SupplierId = 0,
            SupplierCode = dto.CariKodu,
            Status = MapPurchaseOrderStatus(dto.Durum ?? string.Empty),
            TotalAmount = totalAmount,
            OrderDate = dto.BelgeTarihi,
            ExpectedDate = dto.TeslimTarihi,
            IsSynced = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = items
        };

        return purchaseOrder;
    }

    private static AccountingRecord CreateAccountingRecord(
        string? transactionNo,
        double amount,
        string category,
        DateTime transactionDate,
        string? description,
        string paymentMethod)
    {
        var safeTransactionNo = string.IsNullOrWhiteSpace(transactionNo)
            ? Guid.NewGuid().ToString("N")
            : transactionNo;

        return new AccountingRecord
        {
            TransactionNo = safeTransactionNo,
            Type = ResolveAccountingType(amount),
            Category = category,
            Amount = Math.Abs(Convert.ToDecimal(amount)),
            Currency = "TRY",
            Description = TrimAndTruncate(description, 500),
            PaymentMethod = paymentMethod,
            TransactionDate = transactionDate == default ? DateTime.UtcNow : transactionDate,
            IsSynced = true,
            SyncedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static string ResolveAccountingType(double amount)
    {
        return amount >= 0 ? "INCOME" : "EXPENSE";
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
    private const double DefaultOrderKdvOran = 0.18;

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

    // For Luca stock card we prefer preserving case/format; only trim whitespace.
    private static string NormalizeSkuPreserve(string sku)
    {
        return string.IsNullOrWhiteSpace(sku) ? string.Empty : sku.Trim();
    }

    private static double ResolveOrderItemKdvOran(OrderItem item)
    {
        // OrderItem entity does not persist tax info yet; default to standard KDV (18%)
        return DefaultOrderKdvOran;
    }

    private static double ResolvePurchaseOrderItemKdvOran(PurchaseOrderItem item)
    {
        // PurchaseOrderItem entity also lacks tax data; default to standard KDV (18%)
        return DefaultOrderKdvOran;
    }

    private static int? ParseDocumentNo(string? rawValue, int fallback)
    {
        if (!string.IsNullOrWhiteSpace(rawValue) && int.TryParse(rawValue, out var parsed))
        {
            return parsed;
        }

        return fallback > 0 ? fallback : null;
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
    /// Luca'daki MERKEZ DEPO kodu: "002"
    /// </summary>
    private static string NormalizeWarehouseCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return "002"; // Luca MERKEZ DEPO default

        var trimmed = code.Trim();
        
        // "0001-0001" veya "001-001" formatını "001"e çevir
        if (trimmed.Contains('-'))
        {
            var parts = trimmed.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 1 && int.TryParse(parts[0], out var num))
                return num.ToString("000");
        }
        
        // Tek sayı ise 3 haneye pad et
        if (int.TryParse(trimmed, out var single))
            return single.ToString("000");

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
