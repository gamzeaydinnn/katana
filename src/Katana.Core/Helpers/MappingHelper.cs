using System;
using System.Collections.Generic;
using System.Linq;
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

    public static Customer MapToCustomer(KatanaCustomerDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        return new Customer
        {
            // Map Katana ID as ReferenceId (not database ID)
            ReferenceId = dto.ReferenceId ?? dto.Id.ToString(),
            
            // Basic info
            Title = dto.Company ?? dto.Name,
            ContactPerson = string.IsNullOrWhiteSpace(dto.FirstName) && string.IsNullOrWhiteSpace(dto.LastName)
                ? null
                : $"{dto.FirstName} {dto.LastName}".Trim(),
            
            // Contact details
            Phone = dto.Phone,
            Email = dto.Email,
            
            // Address from first shipping address
            Address = dto.Addresses?.FirstOrDefault(a => a.EntityType == "shipping")?.Line1,
            City = dto.Addresses?.FirstOrDefault(a => a.EntityType == "shipping")?.City,
            District = dto.Addresses?.FirstOrDefault(a => a.EntityType == "shipping")?.State,
            Country = dto.Addresses?.FirstOrDefault(a => a.EntityType == "shipping")?.Country ?? "Turkey",
            
            // Business fields
            Currency = dto.Currency,
            DefaultDiscountRate = dto.DiscountRate,
            GroupCode = dto.Category,
            
            // Status
            IsActive = true,
            IsSynced = false,
            SyncStatus = "PENDING",
            
            // Timestamps
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt == default ? DateTime.UtcNow : dto.UpdatedAt
        };
    }

    public static KatanaCustomerDto MapToKatanaCustomerDto(Customer customer)
    {
        if (customer == null)
            throw new ArgumentNullException(nameof(customer));

        var dto = new KatanaCustomerDto
        {
            Name = customer.Title,
            Company = customer.Title,
            Email = customer.Email,
            Phone = customer.Phone,
            Currency = customer.Currency ?? "TRY",
            ReferenceId = customer.ReferenceId,
            Category = customer.GroupCode,
            DiscountRate = customer.DefaultDiscountRate,
            Comment = string.Empty
        };
        
        // Split contact person into first/last name if possible
        if (!string.IsNullOrWhiteSpace(customer.ContactPerson))
        {
            var parts = customer.ContactPerson.Split(' ', 2);
            dto.FirstName = parts.Length > 0 ? parts[0] : null;
            dto.LastName = parts.Length > 1 ? parts[1] : null;
        }
        
        // Add shipping address if available
        if (!string.IsNullOrWhiteSpace(customer.Address))
        {
            dto.Addresses = new List<KatanaCustomerAddressDto>
            {
                new KatanaCustomerAddressDto
                {
                    EntityType = "shipping",
                    Default = true,
                    Line1 = customer.Address,
                    City = customer.City,
                    State = customer.District,
                    Country = customer.Country ?? "Turkey"
                }
            };
        }
        
        return dto;
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
            Status = InvoiceStatus.Sent,
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
            Code = dto.Id.ToString(), // Store Katana ID
            Name = dto.Name,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Addresses?.FirstOrDefault()?.Line1,
            City = dto.Addresses?.FirstOrDefault()?.City,
            IsActive = true,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            IsSynced = false,
            SyncStatus = "PENDING"
        };
    }

    public static KatanaSupplierDto MapToKatanaSupplierDto(Supplier supplier)
    {
        var dto = new KatanaSupplierDto
        {
            Id = int.TryParse(supplier.Code, out var id) ? id : 0,
            Name = supplier.Name,
            Email = supplier.Email,
            Phone = supplier.Phone,
            Currency = "TRY",
            CreatedAt = supplier.CreatedAt,
            UpdatedAt = supplier.UpdatedAt
        };
        
        if (!string.IsNullOrWhiteSpace(supplier.Address))
        {
            dto.Addresses.Add(new KatanaSupplierAddressDto
            {
                Line1 = supplier.Address,
                City = supplier.City,
                Country = "TR",
                IsDefault = true
            });
        }
        
        return dto;
    }

    public static string GenerateSupplierLucaCode(string katanaId)
    {
        return $"TED-{katanaId}";
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
            OnayFlag = order.OnayFlag ? 1 : 0,
            ParaBirimKod = string.IsNullOrWhiteSpace(order.Currency) ? "TRY" : order.Currency,
            KdvFlag = true,
            TeslimTarihi = order.DeliveryDate,
            TeslimTarihiFlag = order.DeliveryDate.HasValue ? 1 : null,
            CariKodu = cariKod,
            CariTanim = customer.Title,
            CariYasalUnvan = customer.Title,
            VergiNo = customer.Type == 1 ? customer.TaxNo : null,
            TcKimlikNo = customer.Type == 2 ? customer.TaxNo : null,
            VergiDairesi = customer.TaxOffice,
            CariTip = customer.Type == 1 ? 2 : 1, // 1=Kurumsal->2(Tüzel), 2=Bireysel->1(Gerçek), 0/null->1(Varsayılan)
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
            BelgeSeri = po.DocumentSeries ?? "EFA2025",
            BelgeNo = int.TryParse(po.LucaDocumentNo ?? belgeTakipNo, out var belgeNoInt) ? belgeNoInt : null,
            BelgeTakipNo = belgeTakipNo,
            BelgeTarihi = po.OrderDate == default ? DateTime.UtcNow : po.OrderDate,
            BelgeTurDetayId = po.DocumentTypeDetailId > 0 ? po.DocumentTypeDetailId : 2,
            TeklifSiparisTur = 1,
            OnayFlag = 0,
            ParaBirimKod = "TRY",
            KdvFlag = po.VatIncluded,
            TeslimTarihi = po.ExpectedDate ?? DateTime.UtcNow.AddDays(7),
            TeslimTarihiFlag = po.ExpectedDate.HasValue ? 1 : null,
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

    /// <summary>
    /// Maps PurchaseOrder entity to Luca INVOICE request (Alım Faturası - BelgeTurDetayId: 69)
    /// </summary>
    public static LucaCreateInvoiceHeaderRequest MapToLucaInvoiceFromPurchaseOrder(PurchaseOrder po, Supplier supplier)
    {
        var cariKod = !string.IsNullOrWhiteSpace(supplier.Code) 
            ? supplier.Code 
            : $"TED{supplier.Id:D6}";
        
        var belgeTakipNo = string.IsNullOrWhiteSpace(po.OrderNo) ? po.Id.ToString() : po.OrderNo;
        
        var belgeTarihi = po.OrderDate == default ? DateTime.UtcNow : po.OrderDate;
        var vadeTarihi = po.ExpectedDate ?? DateTime.UtcNow.AddDays(30);

        return new LucaCreateInvoiceHeaderRequest
        {
            BelgeSeri = po.DocumentSeries ?? "EFA2025",
            BelgeNo = po.LucaDocumentNo ?? belgeTakipNo,
            BelgeTakipNo = belgeTakipNo,
            BelgeTarihi = belgeTarihi.ToString("dd/MM/yyyy"),
            VadeTarihi = vadeTarihi.ToString("dd/MM/yyyy"),
            BelgeTurDetayId = "69", // Alım Faturası (appsettings.json'daki değer)
            FaturaTur = "1", // Normal fatura
            ParaBirimKod = "TRY",
            KurBedeli = 1.0,
            MusteriTedarikci = "2", // Tedarikçi
            CariKodu = cariKod,
            CariTanim = supplier.Name,
            CariTip = 2, // Tedarikçi
            CariKisaAd = supplier.Name.Length > 50 ? supplier.Name.Substring(0, 50) : supplier.Name,
            CariYasalUnvan = supplier.Name,
            CariAd = supplier.Name, // Koza API zorunlu alan
            CariSoyad = supplier.Name, // Koza API zorunlu alan
            VergiNo = supplier.TaxNo,
            AdresSerbest = supplier.Address,
            KdvFlag = po.VatIncluded,
            ReferansNo = po.ReferenceCode ?? $"KAT-PO-{po.Id}",
            IrsaliyeBilgisiList = null, // Boş liste yerine null gönder
            DetayList = po.Items.Select(item => new LucaCreateInvoiceDetailRequest
            {
                KartTuru = 1, // Stok
                KartKodu = !string.IsNullOrWhiteSpace(item.LucaStockCode) 
                    ? item.LucaStockCode 
                    : NormalizeSku(item.Product?.SKU ?? string.Empty),
                KartAdi = item.Product?.Name,
                DepoKodu = item.WarehouseCode ?? "01",
                BirimFiyat = (double)item.UnitPrice,
                Miktar = item.Quantity,
                KdvOran = (double)item.VatRate / 100.0, // Convert percentage to decimal (20 -> 0.20)
                IskontoOran1 = item.DiscountAmount > 0 ? (double)(item.DiscountAmount / ((decimal)item.UnitPrice * item.Quantity) * 100.0m) : null,
                Tutar = (double)(item.UnitPrice * item.Quantity - item.DiscountAmount)
            }).ToList()
        };
    }

    /// <summary>
    /// Maps SalesOrder entity to Luca INVOICE request (Satış Faturası - BelgeTurDetayId: 76)
    /// </summary>
    public static LucaCreateInvoiceHeaderRequest MapToLucaInvoiceFromSalesOrder(Entities.SalesOrder order, Customer customer, string? depoKodu = null)
    {
        // Müşteri kodu: LucaCode varsa kullan, yoksa TaxNo'dan üret
        var cariKod = !string.IsNullOrWhiteSpace(customer.LucaCode)
            ? customer.LucaCode
            : !string.IsNullOrWhiteSpace(customer.TaxNo)
                ? customer.TaxNo
                : $"CUST{customer.Id:D6}";

        var belgeTakipNo = string.IsNullOrWhiteSpace(order.OrderNo) ? $"KAT-{order.Id}" : order.OrderNo;
        var belgeTarihi = order.OrderCreatedDate ?? DateTime.UtcNow;
        var vadeTarihi = belgeTarihi.AddDays(30);
        
        // Depo kodu formatı: "001.0001" (Luca formatı)
        var resolvedDepoKodu = string.IsNullOrWhiteSpace(depoKodu) ? "001.0001" : depoKodu;
        if (!resolvedDepoKodu.Contains(".") && resolvedDepoKodu.Length == 3)
        {
            resolvedDepoKodu = $"{resolvedDepoKodu}.0001";
        }

        // KDV oranı normalize et (0.18 formatında olmalı)
        static double NormalizeKdvOran(decimal? taxRatePercentOrDecimal)
        {
            var rate = (double)(taxRatePercentOrDecimal ?? 18m);
            if (rate > 1.0) rate /= 100.0;
            return Math.Round(rate, 2);
        }

        // Müşteri adı ve soyadı ayır
        var customerTitle = !string.IsNullOrWhiteSpace(customer.Title) ? customer.Title.Trim() : "Müşteri";
        var nameParts = customerTitle.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string cariAd, cariSoyad;
        if (nameParts.Length >= 2)
        {
            cariSoyad = nameParts[^1];
            cariAd = string.Join(" ", nameParts[..^1]);
        }
        else
        {
            cariAd = customerTitle;
            cariSoyad = customerTitle;
        }

        // CariTip: VKN (10 hane) = 1 (Tüzel), TCKN (11 hane) = 2 (Gerçek)
        var taxNoDigits = string.IsNullOrWhiteSpace(customer.TaxNo) 
            ? "" 
            : new string(customer.TaxNo.Where(char.IsDigit).ToArray());
        var cariTip = taxNoDigits.Length == 11 ? 2 : 1;

        // BelgeSeri - Luca örneğine göre "EFA2025" kabul ediliyor
        var belgeSeri = order.BelgeSeri ?? "EFA2025";

        // Döviz kuru hesaplama: TRY için 1.0, diğer para birimleri için ConversionRate kullan
        var currency = string.IsNullOrWhiteSpace(order.Currency) ? "TRY" : order.Currency;
        var kurBedeli = currency.ToUpperInvariant() == "TRY" 
            ? 1.0 
            : (double)(order.ConversionRate ?? 1m);

        return new LucaCreateInvoiceHeaderRequest
        {
            BelgeSeri = belgeSeri,
            BelgeNo = null, // Luca otomatik üretir
            BelgeTarihi = belgeTarihi.ToString("dd/MM/yyyy"),
            DuzenlemeSaati = null, // Luca örneğinde null
            VadeTarihi = vadeTarihi.ToString("dd/MM/yyyy"),
            BelgeTakipNo = null, // Luca örneğinde null
            BelgeAciklama = !string.IsNullOrWhiteSpace(order.AdditionalInfo)
                ? order.AdditionalInfo
                : $"Katana Sipariş: {order.OrderNo ?? order.Id.ToString()}",
            BelgeTurDetayId = "76", // Satış faturası
            FaturaTur = "1",
            ParaBirimKod = currency,
            KurBedeli = kurBedeli,
            KdvFlag = true,
            ReferansNo = order.CustomerRef ?? belgeTakipNo,
            MusteriTedarikci = "1",
            CariKodu = cariKod,
            CariTanim = customerTitle,
            CariTip = cariTip,
            CariKisaAd = customerTitle.Length > 50 ? customerTitle.Substring(0, 50) : customerTitle,
            CariYasalUnvan = customerTitle,
            VergiNo = taxNoDigits.Length == 10 ? taxNoDigits : null,
            TcKimlikNo = taxNoDigits.Length == 11 ? taxNoDigits : null,
            VergiDairesi = customer.TaxOffice ?? "Vergi Dairesi",
            Il = !string.IsNullOrWhiteSpace(customer.City) ? NormalizeTurkishText(customer.City) : "İstanbul",
            Ilce = !string.IsNullOrWhiteSpace(customer.District) ? NormalizeTurkishText(customer.District) : "Merkez",
            AdresSerbest = customer.Address ?? "Adres",
            Telefon = customer.Phone,
            Email = customer.Email,
            EfaturaTuru = 1,
            IrsaliyeBilgisiList = null,
            DetayList = order.Lines.Select(l => new LucaCreateInvoiceDetailRequest
            {
                KartTuru = 1,
                KartKodu = NormalizeSku(l.SKU),
                HesapKod = null,
                KartAdi = !string.IsNullOrWhiteSpace(l.ProductName) ? NormalizeTurkishText(l.ProductName) : NormalizeSku(l.SKU),
                KartTipi = 1,
                Barkod = null,
                OlcuBirimi = 1, // Adet
                KdvOran = NormalizeKdvOran(l.TaxRate),
                KartSatisKdvOran = NormalizeKdvOran(l.TaxRate),
                KartAlisKdvOran = NormalizeKdvOran(l.TaxRate),
                DepoKodu = resolvedDepoKodu,
                BirimFiyat = (double)(l.PricePerUnit ?? 0),
                Miktar = (double)l.Quantity,
                Tutar = null, // Luca hesaplar
                IskontoOran1 = 0.0,
                Aciklama = BuildInvoiceLineDescription(l.ProductName, l.SKU),
                LotNo = null
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
        // ✅ RAPOR UYUMLU: SADECE LUCA DOKÜMANTASYONUNDA OLAN ALANLAR!
        var barkod = normalizedSku;
        var startDate = (product.CreatedAt == default ? DateTime.UtcNow : product.CreatedAt).Date;

        return new LucaCreateStokKartiRequest
        {
            // Zorunlu alanlar
            KartAdi = kartAdi,
            KartKodu = normalizedSku,
            BaslangicTarihi = startDate.ToString("dd/MM/yyyy"), // dd/MM/yyyy format!
            
            // Tip ve kategori
            KartTipi = 1,
            KartTuru = 1, // 1=Stok
            KategoriAgacKod = null, // Rapor: null gönderilmeli
            
            // KDV ve ölçü birimi
            KartAlisKdvOran = 1, // Rapor: Sadece alış KDV, 1=sabit ID
            OlcumBirimiId = olcumBirimiId ?? 1, // 1=ADET
            
            // Barkod
            Barkod = barkod,
            
            // Tevkifat bilgileri (null)
            AlisTevkifatOran = null,
            SatisTevkifatOran = null,
            AlisTevkifatTipId = null,
            SatisTevkifatTipId = null,
            
            // Flagler
            SatilabilirFlag = 1,
            SatinAlinabilirFlag = 1,
            LotNoFlag = 1,
            MinStokKontrol = 0,
            MaliyetHesaplanacakFlag = true // ✅ BOOLEAN!
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
        
        // ✅ RAPOR UYUMLU: SADECE LUCA DOKÜMANTASYONUNDA OLAN ALANLAR!
        return new LucaCreateStokKartiRequest
        {
            // Zorunlu alanlar
            KartAdi = TrimAndTruncate(name, 255) ?? sku,
            KartKodu = sku,
            BaslangicTarihi = startDate.ToString("dd/MM/yyyy"),
            
            // Tip ve kategori
            KartTipi = 1,
            KartTuru = 1,
            KategoriAgacKod = null,
            
            // KDV ve ölçü birimi
            KartAlisKdvOran = 1, // Rapor: Sabit 1 (ID)
            OlcumBirimiId = olcumBirimiId ?? 1,
            
            // Barkod (versiyonlu SKU'lar için NULL)
            Barkod = barcodeToSend,
            
            // Tevkifat (null)
            AlisTevkifatOran = null,
            SatisTevkifatOran = null,
            AlisTevkifatTipId = null,
            SatisTevkifatTipId = null,
            
            // Flagler
            SatilabilirFlag = 1,
            SatinAlinabilirFlag = 1,
            LotNoFlag = 1,
            MinStokKontrol = 0,
            MaliyetHesaplanacakFlag = true // ✅ BOOLEAN!
        };
    }

    /// <summary>
    /// Luca stok kartı gönderimi öncesi minimum alan doğrulaması.
    /// ✅ RAPOR UYUMLU: Sadece dokümantasyondaki zorunlu alanları kontrol eder
    /// </summary>
    public static (bool IsValid, List<string> Errors) ValidateLucaStockCard(LucaCreateStokKartiRequest stockCard)
    {
        var errors = new List<string>();

        // Zorunlu alanlar
        if (string.IsNullOrWhiteSpace(stockCard.KartAdi))
            errors.Add("kartAdi zorunlu");
        if (string.IsNullOrWhiteSpace(stockCard.KartKodu))
            errors.Add("kartKodu (SKU) zorunlu");
        if (string.IsNullOrWhiteSpace(stockCard.BaslangicTarihi))
            errors.Add("baslangicTarihi zorunlu (dd/MM/yyyy formatında)");
        if (stockCard.OlcumBirimiId <= 0)
            errors.Add("olcumBirimiId zorunlu");
        if (stockCard.KartAlisKdvOran < 0)
            errors.Add("KartAlisKdvOran geçersiz");

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

    /// <summary>
    /// Katana ürününü Luca güncelleme formatına dönüştürür.
    /// Güncelleme için mevcut Luca ID'si şarttır.
    /// </summary>
    public static LucaUpdateStokKartiRequest MapToLucaUpdateStockCard(
        KatanaProductDto product, 
        long existingLucaId, 
        double? defaultVat = null)
    {
        // SKU Normalizasyonu (Mevcut mantık)
        var skuRaw = string.IsNullOrWhiteSpace(product.SKU) ? product.GetProductCode() : product.SKU;
        var sku = NormalizeSkuPreserve(skuRaw);

        // Versiyonlu SKU kontrolü (Mevcut mantık - Duplicate barcode önleme)
        bool isVersionedSku = System.Text.RegularExpressions.Regex.IsMatch(
            sku ?? string.Empty, 
            @"-V\d+$", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return new LucaUpdateStokKartiRequest
        {
            SkartId = existingLucaId,
            KartKodu = sku ?? string.Empty,
            KartAdi = TrimAndTruncate(product.Name, 255) ?? sku ?? string.Empty,
            UzunAdi = TrimAndTruncate(product.Description, 500),
            Barkod = isVersionedSku ? null : (product.Barcode ?? sku),
            PerakendeSatisBirimFiyat = (decimal)product.Price,
            KategoriAgacKod = null,
            KartTipi = 1,
            KartTuru = 1,
            OlcumBirimiId = 1, // ADET
            SatilabilirFlag = 1,
            SatinAlinabilirFlag = 1,
            MaliyetHesaplanacakFlag = true
        };
    }

    /// <summary>
    /// Product entity'sini Luca güncelleme formatına dönüştürür.
    /// </summary>
    public static LucaUpdateStokKartiRequest MapToLucaUpdateStockCard(
        Product product, 
        long existingLucaId)
    {
        var normalizedSku = NormalizeSkuPreserve(product.SKU);
        
        bool isVersionedSku = System.Text.RegularExpressions.Regex.IsMatch(
            normalizedSku ?? string.Empty, 
            @"-V\d+$", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return new LucaUpdateStokKartiRequest
        {
            SkartId = existingLucaId,
            KartKodu = normalizedSku ?? string.Empty,
            KartAdi = TrimAndTruncate(product.Name, 255) ?? normalizedSku ?? string.Empty,
            UzunAdi = TrimAndTruncate(product.Description, 500),
            Barkod = isVersionedSku ? null : normalizedSku,
            PerakendeSatisBirimFiyat = product.Price,
            KategoriAgacKod = product.CategoryId > 0 ? product.CategoryId.ToString() : null,
            KartTipi = 1,
            KartTuru = 1,
            OlcumBirimiId = 1,
            SatilabilirFlag = 1,
            SatinAlinabilirFlag = 1,
            MaliyetHesaplanacakFlag = true
        };
    }

    /// <summary>
    /// Luca stok kartı güncelleme isteği için validasyon kontrolü.
    /// </summary>
    public static (bool IsValid, List<string> Errors) ValidateLucaUpdateStockCard(LucaUpdateStokKartiRequest stockCard)
    {
        var errors = new List<string>();

        // Zorunlu alanlar
        if (stockCard.SkartId <= 0)
            errors.Add("skartId zorunlu ve pozitif olmalı");
        if (string.IsNullOrWhiteSpace(stockCard.KartAdi))
            errors.Add("kartAdi zorunlu");
        if (string.IsNullOrWhiteSpace(stockCard.KartKodu))
            errors.Add("kartKodu (SKU) zorunlu");

        return (!errors.Any(), errors);
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
            Status = InvoiceStatus.Received,
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
            Status = InvoiceStatus.Received,
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
        // Koza/Luca tarafında "CUST_" gibi prefix'ler 500/HTML response'a sebep olabiliyor.
        // En güvenli fallback: mümkünse doğrudan vergi no kullan, yoksa geçici varsayılan cari kodu.
        if (string.IsNullOrWhiteSpace(taxNo))
        {
            return "120.01.001";
        }

        return taxNo.Trim();
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
        
        // ✅ Türkçe karakterleri İngilizce'ye çevir (ş→s, ğ→g, ı→i, ö→o, ü→u, ç→c)
        // Böylece Türkçe SKU'lar da kontrol edilebilir
        var turkishNormalized = trimmed
            .Replace("ş", "s").Replace("Ş", "S")
            .Replace("ğ", "g").Replace("Ğ", "G")
            .Replace("ı", "i").Replace("İ", "I")
            .Replace("ö", "o").Replace("Ö", "O")
            .Replace("ü", "u").Replace("Ü", "U")
            .Replace("ç", "c").Replace("Ç", "C");
        
        // Sadece alfanumerik, dash, underscore karakterleri al
        var allowedChars = turkishNormalized
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

    /// <summary>
    /// Türkçe karakterleri İngilizce'ye normalize eder
    /// </summary>
    private static string NormalizeTurkishText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var normalized = text.Trim()
            .Replace("ş", "s").Replace("Ş", "S")
            .Replace("ğ", "g").Replace("Ğ", "G")
            .Replace("ı", "i").Replace("İ", "I")
            .Replace("ö", "o").Replace("Ö", "O")
            .Replace("ü", "u").Replace("Ü", "U")
            .Replace("ç", "c").Replace("Ç", "C");

        return normalized;
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

    private static string? ParseDocumentNoAsString(string? rawValue, int fallback)
    {
        if (!string.IsNullOrWhiteSpace(rawValue))
        {
            return rawValue.Trim();
        }

        return fallback > 0 ? fallback.ToString() : null;
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
        {
            // ✅ Boş depo kodu - Luca API formatında default döndür
            return "002.001.0001"; // Luca MERKEZ DEPO (Postman formatı)
        }

        var trimmed = code.Trim();
        
        // ✅ "0001-0001" formatını "001.001.0001" Luca API formatına çevir
        if (trimmed.Contains('-'))
        {
            var parts = trimmed.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && int.TryParse(parts[0], out var first) && int.TryParse(parts[1], out var second))
            {
                // "0001-0001" → "001.001.0001" (Postman irsaliye formatı)
                return $"{first:000}.{second:000}.{first:0000}";
            }
            else if (parts.Length >= 1 && int.TryParse(parts[0], out var num))
            {
                // "001-X" → "001.001.0001" (fallback)
                return $"{num:000}.001.{num:0000}";
            }
        }
        
        // ✅ Tek sayı ise Luca formatına çevir: "1" → "001.001.0001"
        if (int.TryParse(trimmed, out var single))
            return $"{single:000}.{single:000}.{single:0000}";

        // ✅ Zaten noktalı format varsa aynen döndür: "002.001.0001"
        if (trimmed.Contains('.'))
            return trimmed;

        // ✅ Alfanumerik kod - uppercase + fallback format
        return trimmed.ToUpperInvariant() + ".001.0001";
    }

    /// <summary>
    /// Depo kodunu validate et - boş veya geçersizse exception fırlat
    /// </summary>
    public static void ValidateWarehouseCode(string? code, string paramName = "warehouseCode")
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Depo kodu boş olamaz. Fatura/irsaliye/satış siparişi satırı eklenirken depo kodu zorunludur.", paramName);
        }

        var normalized = NormalizeWarehouseCode(code);
        
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException($"Depo kodu geçersiz: '{code}'. Normalize edilmiş değer boş.", paramName);
        }
    }

    /// <summary>
    /// Fatura satırı açıklaması oluşturur - varyant attribute'larını içerir
    /// SKU formatı: PRODUCT-VARIANT-ATTRIBUTE (örn: TSHIRT-RED-M)
    /// </summary>
    private static string BuildInvoiceLineDescription(string? productName, string? sku)
    {
        var description = !string.IsNullOrWhiteSpace(productName) ? productName : sku ?? string.Empty;
        
        // SKU'dan varyant bilgilerini çıkar
        if (!string.IsNullOrWhiteSpace(sku))
        {
            var parts = sku.Split('-');
            if (parts.Length >= 2)
            {
                var variantInfo = new List<string>();
                
                // İkinci parça genellikle renk/varyant
                if (parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[1]))
                {
                    variantInfo.Add($"Varyant: {parts[1]}");
                }
                
                // Üçüncü parça genellikle beden/attribute
                if (parts.Length >= 3 && !string.IsNullOrWhiteSpace(parts[2]))
                {
                    variantInfo.Add($"Özellik: {parts[2]}");
                }
                
                if (variantInfo.Any())
                {
                    description = $"{description} ({string.Join(", ", variantInfo)})";
                }
            }
        }
        
        // Luca açıklama alanı max 500 karakter
        return TrimAndTruncate(description, 500) ?? description;
    }

    // Core Entity -> Katana DTO mapping
    public static KatanaProductDto MapToKatanaProductDto(Product product)
    {
        if (product == null)
            throw new ArgumentNullException(nameof(product));

        var dto = new KatanaProductDto
        {
            Name = product.Name,
            SKU = product.SKU,
            Description = product.Description,
            Category = product.Category,
            Price = product.Price,
            SalesPrice = product.SalesPrice ?? product.Price,
            PurchasePrice = product.PurchasePrice,
            CostPrice = product.CostPrice,
            Unit = "pcs", // Default unit
            IsActive = product.IsActive,
            Currency = "TRY", // Default currency
            Barcode = product.Barcode,
            ImageUrl = product.MainImageUrl
        };

        return dto;
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
