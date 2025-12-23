# Katana Sipariş Yapısı, Varyant Yönetimi ve BOM Dokümantasyonu

## İçindekiler

1. [Katana Sipariş Yapısı](#1-katana-sipariş-yapısı)
2. [Varyant Yönetimi](#2-varyant-yönetimi)
3. [BOM (Bill of Materials) Sistemi](#3-bom-bill-of-materials-sistemi)
4. [Luca Entegrasyonu - Belge Gönderimi](#4-luca-entegrasyonu---belge-gönderimi)
5. [Mevcut Sorunlar ve Çözüm Önerileri](#5-mevcut-sorunlar-ve-çözüm-önerileri)

---

## 1. Katana Sipariş Yapısı

### 1.1 Sipariş Türleri

Katana'da 3 ana sipariş türü bulunmaktadır:

#### A) Sales Order (Satış Siparişi)

- **Amaç**: Müşteriye satış yapıldığında oluşturulur
- **Durum Akışı**: `NOT_SHIPPED` → `PARTIALLY_SHIPPED` → `SHIPPED` → `DELIVERED`
- **Önemli Alanlar**:
  - `customer_id`: Müşteri referansı
  - `order_no`: Sipariş numarası
  - `sales_order_rows`: Sipariş kalemleri (her biri bir varyant içerir)
  - `status`: Sipariş durumu
  - `invoicing_status`: Faturalama durumu

```csharp
public class SalesOrderDto
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public string OrderNo { get; set; }
    public string Status { get; set; } // NOT_SHIPPED, SHIPPED, DELIVERED
    public List<SalesOrderRowDto> SalesOrderRows { get; set; }
    public DateTime? OrderCreatedDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
}
```

#### B) Purchase Order (Satınalma Siparişi)

- **Amaç**: Tedarikçiden mal alımı için oluşturulur
- **Durum Akışı**: `Pending` → `Approved` → `Received` → `Cancelled`
- **Önemli Alanlar**:
  - `supplier_id`: Tedarikçi referansı
  - `order_no`: Sipariş numarası
  - `items`: Sipariş kalemleri
  - `status`: Sipariş durumu

```csharp
public class PurchaseOrder
{
    public int Id { get; set; }
    public string OrderNo { get; set; }
    public int SupplierId { get; set; }
    public PurchaseOrderStatus Status { get; set; }
    public List<PurchaseOrderItem> Items { get; set; }
    public decimal TotalAmount { get; set; }
}
```

#### C) Manufacturing Order (Üretim Emri)

- **Amaç**: Ürün üretimi için oluşturulur
- **BOM ile İlişki**: BOM'a göre hammadde tüketimi yapar
- **Durum Akışı**: `PLANNED` → `IN_PROGRESS` → `DONE` → `CANCELLED`

```csharp
public class ManufacturingOrderDto
{
    public long Id { get; set; }
    public long VariantId { get; set; }
    public decimal Quantity { get; set; }
    public string Status { get; set; }
    public DateTime? DueDate { get; set; }
    public List<ManufacturingOrderBatchTransactionDto> BatchTransactions { get; set; }
}
```

### 1.2 Sipariş Satırları (Order Rows)

Her sipariş, birden fazla satır (row) içerebilir. Her satır bir **varyant** referans eder:

```csharp
public class SalesOrderRowDto
{
    public long Id { get; set; }
    public decimal Quantity { get; set; }
    public long VariantId { get; set; }  // ⚠️ ÖNEMLİ: Her satır bir varyant
    public decimal? PricePerUnit { get; set; }
    public long? LocationId { get; set; }
    public long? LinkedManufacturingOrderId { get; set; }
}
```

**Önemli Not**: Katana'da sipariş satırları **varyant bazlıdır**. Yani:

- 1 sipariş = N satır
- Her satır = 1 varyant + miktar

---

## 2. Varyant Yönetimi

### 2.1 Varyant Nedir?

Katana'da **Variant (Varyant)**, bir ürünün satılabilir/stoklanabilir en küçük birimidir.

**Örnek**:

- **Ürün**: T-Shirt
- **Varyantlar**:
  - T-Shirt - Kırmızı - S (SKU: TSHIRT-RED-S)
  - T-Shirt - Kırmızı - M (SKU: TSHIRT-RED-M)
  - T-Shirt - Mavi - L (SKU: TSHIRT-BLUE-L)

### 2.2 Varyant Yapısı

```csharp
public class VariantDto
{
    public long Id { get; set; }
    public string SKU { get; set; }           // Benzersiz stok kodu
    public string? Barcode { get; set; }
    public long ProductId { get; set; }       // Ana ürün referansı
    public decimal? SalesPrice { get; set; }
    public decimal? CostPrice { get; set; }
    public int? InStock { get; set; }         // Mevcut stok
    public int? Available { get; set; }       // Kullanılabilir stok
    public int? Committed { get; set; }       // Rezerve edilmiş stok
}
```

### 2.3 Mevcut Durum: Her Varyant Ayrı SKU

**Sorun**: Katana'da her varyant için yeni bir SKU oluşturulmuş ve hem Katana'ya hem Luca'ya ayrı ürün olarak eklenmiş.

**Örnek Senaryo**:

```
Sipariş #12345
├── Satır 1: SKU-001-RED-S  (Qty: 10)
├── Satır 2: SKU-001-RED-M  (Qty: 15)
└── Satır 3: SKU-001-BLUE-L (Qty: 20)
```

Bu durumda:

- Katana'da 3 ayrı ürün/varyant var
- Luca'da 3 ayrı stok kartı var
- Sipariş 3 satır olarak gönderiliyor

### 2.4 İstenilen Durum: Tek Sipariş, Çoklu Satır

**Hedef**: Tüm varyantları tek bir sipariş altında, çoklu satır olarak Luca'ya göndermek.

**Çözüm Yaklaşımı**:

1. Katana'daki varyantları **grupla** (aynı ana ürüne ait olanlar)
2. Her varyantı **ayrı satır** olarak Luca faturasına ekle
3. Luca'da **tek belge** oluştur

---

## 3. BOM (Bill of Materials) Sistemi

### 3.1 BOM Nedir?

**BOM (Bill of Materials - Malzeme Listesi)**, bir ürünün üretimi için gereken hammadde ve bileşenlerin listesidir.

**Örnek**:

```
Ürün: Ahşap Masa
BOM:
├── Ahşap Tabla: 1 adet
├── Masa Ayağı: 4 adet
├── Vida: 16 adet
└── Cila: 0.5 litre
```

### 3.2 BOM Yapısı

```csharp
public class BomRowDto
{
    public string Id { get; set; }
    public string ParentProductSKU { get; set; }      // Ana ürün (Masa)
    public string ComponentProductSKU { get; set; }   // Bileşen (Ahşap Tabla)
    public decimal Quantity { get; set; }             // Gerekli miktar
    public string? Unit { get; set; }                 // Birim (adet, kg, litre)
    public decimal? Scrap { get; set; }               // Fire oranı (%)
    public int? Sequence { get; set; }                // Sıra numarası
}
```

### 3.3 BOM Nasıl Çalışır?

#### Adım 1: BOM Tanımlama

```
Ürün: MASA-001
BOM:
- AHSAP-TABLA-001: 1 adet
- AYAK-001: 4 adet
- VIDA-M6: 16 adet
```

#### Adım 2: Manufacturing Order Oluşturma

```csharp
// 10 adet masa üretmek için Manufacturing Order
var manufacturingOrder = new ManufacturingOrderDto
{
    VariantId = 12345,  // MASA-001 variant ID
    Quantity = 10,
    Status = "PLANNED"
};
```

#### Adım 3: Otomatik Hammadde Rezervasyonu

Katana otomatik olarak hesaplar:

```
10 masa için gerekli:
- AHSAP-TABLA-001: 10 adet
- AYAK-001: 40 adet
- VIDA-M6: 160 adet
```

#### Adım 4: Üretim Tamamlama

```csharp
// Üretim tamamlandığında
var production = new ManufacturingOrderProductionCreateRequest
{
    ManufacturingOrderId = 12345,
    CompletedQuantity = 10,
    Ingredients = new List<ManufacturingOrderProductionIngredientDto>
    {
        new() { VariantId = 111, Quantity = 10 },  // Ahşap tabla
        new() { VariantId = 222, Quantity = 40 },  // Ayak
        new() { VariantId = 333, Quantity = 160 }  // Vida
    }
};
```

**Sonuç**:

- Hammadde stoğu azalır
- Bitmiş ürün stoğu artar
- Maliyet hesaplanır

### 3.4 BOM ve Sales Order İlişkisi

**Make-to-Order (Siparişe Göre Üretim)**:

```csharp
// Sales Order oluşturulduğunda
var salesOrder = new SalesOrderDto
{
    OrderNo = "SO-12345",
    SalesOrderRows = new List<SalesOrderRowDto>
    {
        new()
        {
            VariantId = 12345,  // MASA-001
            Quantity = 10,
            LinkedManufacturingOrderId = 67890  // ⚠️ Üretim emri bağlantısı
        }
    }
};
```

**Akış**:

1. Müşteri 10 masa sipariş eder
2. Sistem otomatik Manufacturing Order oluşturur
3. BOM'a göre hammadde rezerve edilir
4. Üretim tamamlanınca Sales Order karşılanır

---

## 4. Luca Entegrasyonu - Belge Gönderimi

### 4.1 Mevcut Durum

**Sorun**: Her varyant ayrı ürün olarak Luca'ya gönderiliyor.

```csharp
// Mevcut kod (SalesOrdersController.cs)
public async Task<ActionResult> SyncToLuca(int id)
{
    var order = await _context.SalesOrders
        .Include(s => s.Lines)
        .FirstOrDefaultAsync(s => s.Id == id);

    // Her satır ayrı ürün olarak işleniyor
    var lucaResult = await _lucaService.CreateSalesOrderInvoiceAsync(order, depoKodu);
}
```

### 4.2 Luca Fatura Yapısı

Luca'da bir fatura şu yapıdadır:

```json
{
  "belge_seri": "A",
  "belge_no": "12345",
  "cari_kodu": "CUST-001",
  "belge_tarihi": "2025-01-15",
  "satirlar": [
    {
      "stok_kodu": "SKU-001",
      "miktar": 10,
      "birim_fiyat": 100.0,
      "kdv_orani": 20
    },
    {
      "stok_kodu": "SKU-002",
      "miktar": 5,
      "birim_fiyat": 150.0,
      "kdv_orani": 20
    }
  ]
}
```

### 4.3 Çözüm: Tek Belge, Çoklu Satır

**Hedef**: Tüm varyantları tek faturada göndermek.

```csharp
// Önerilen yaklaşım
public async Task<ActionResult> SyncToLucaConsolidated(int orderId)
{
    var order = await _context.SalesOrders
        .Include(s => s.Lines)
        .Include(s => s.Customer)
        .FirstOrDefaultAsync(s => s.Id == orderId);

    // Tüm satırları tek faturada topla
    var lucaInvoice = new LucaInvoiceRequest
    {
        BelgeSeri = order.BelgeSeri ?? "A",
        BelgeNo = order.OrderNo,
        CariKodu = order.Customer.LucaCode,
        BelgeTarihi = order.OrderCreatedDate ?? DateTime.UtcNow,
        Satirlar = order.Lines.Select(line => new LucaInvoiceLineRequest
        {
            StokKodu = line.SKU,
            Miktar = line.Quantity,
            BirimFiyat = line.PricePerUnit ?? 0,
            KdvOrani = line.VatRate ?? 20,
            // Varyant bilgisi açıklama alanında
            Aciklama = $"{line.ProductName} - {line.VariantAttributes}"
        }).ToList()
    };

    var result = await _lucaService.SendInvoiceAsync(lucaInvoice);
    return Ok(result);
}
```

### 4.4 Belge Türleri

Luca'da farklı belge türleri vardır:

| Belge Türü     | Kod | Açıklama          |
| -------------- | --- | ----------------- |
| Satış Faturası | 1   | Müşteriye satış   |
| Alış Faturası  | 2   | Tedarikçiden alım |
| İrsaliye       | 3   | Sevkiyat belgesi  |
| Sipariş        | 4   | Sipariş belgesi   |
| Teklif         | 5   | Teklif belgesi    |

```csharp
// Belge türü ayarlama
order.BelgeTurDetayId = 1;  // Satış Faturası
order.BelgeSeri = "A";
order.BelgeNo = "12345";
```

---

## 5. Mevcut Sorunlar ve Çözüm Önerileri

### 5.1 Sorun: Gereksiz Varyant Çoğalması

**Durum**: Her renk/beden kombinasyonu için ayrı SKU oluşturulmuş.

**Çözüm Adımları**:

#### Adım 1: Ana Ürün Belirleme

```sql
-- Katana'da ana ürünleri bul
SELECT
    p.Id AS ProductId,
    p.Name AS ProductName,
    COUNT(v.Id) AS VariantCount
FROM Products p
LEFT JOIN Variants v ON v.ProductId = p.Id
GROUP BY p.Id, p.Name
HAVING COUNT(v.Id) > 1;
```

#### Adım 2: Varyantları Grupla

```csharp
public class VariantConsolidationService
{
    public async Task<List<VariantGroup>> GroupVariantsByProduct()
    {
        var variants = await _context.Variants
            .Include(v => v.Product)
            .ToListAsync();

        return variants
            .GroupBy(v => v.ProductId)
            .Select(g => new VariantGroup
            {
                ProductId = g.Key,
                ProductName = g.First().Product.Name,
                Variants = g.ToList()
            })
            .ToList();
    }
}
```

#### Adım 3: Siparişleri Birleştir

```csharp
public async Task<ConsolidatedOrder> ConsolidateOrderVariants(int orderId)
{
    var order = await _context.SalesOrders
        .Include(s => s.Lines)
        .FirstOrDefaultAsync(s => s.Id == orderId);

    // Aynı ürüne ait varyantları grupla
    var consolidatedLines = order.Lines
        .GroupBy(l => l.ProductId)
        .Select(g => new ConsolidatedOrderLine
        {
            ProductId = g.Key,
            ProductName = g.First().ProductName,
            Variants = g.Select(v => new VariantDetail
            {
                SKU = v.SKU,
                Quantity = v.Quantity,
                Attributes = v.VariantAttributes
            }).ToList(),
            TotalQuantity = g.Sum(v => v.Quantity)
        })
        .ToList();

    return new ConsolidatedOrder
    {
        OrderNo = order.OrderNo,
        Lines = consolidatedLines
    };
}
```

### 5.2 Sorun: Katana'da Gereksiz Ürünler

**Durum**: Aynı ürünün farklı varyantları ayrı ürün olarak eklenmiş.

**Çözüm**: Deduplication (Tekil Hale Getirme)

```csharp
public class ProductDeduplicationService
{
    public async Task<DeduplicationResult> DeduplicateProducts()
    {
        // 1. Benzer ürünleri bul
        var duplicates = await FindDuplicateProducts();

        // 2. Canonical (ana) ürünü seç
        var canonical = SelectCanonicalProduct(duplicates);

        // 3. Diğer ürünleri canonical'a merge et
        await MergeProducts(duplicates, canonical);

        // 4. Eski ürünleri sil
        await DeleteObsoleteProducts(duplicates.Except(new[] { canonical }));

        return new DeduplicationResult
        {
            CanonicalProduct = canonical,
            MergedCount = duplicates.Count - 1
        };
    }

    private async Task<List<Product>> FindDuplicateProducts()
    {
        // Benzerlik algoritması (Levenshtein distance, fuzzy matching)
        var products = await _context.Products.ToListAsync();

        return products
            .GroupBy(p => NormalizeName(p.Name))
            .Where(g => g.Count() > 1)
            .SelectMany(g => g)
            .ToList();
    }

    private string NormalizeName(string name)
    {
        // Normalize: küçük harf, boşluk temizle, özel karakter kaldır
        return name.ToLowerInvariant()
            .Replace(" ", "")
            .Replace("-", "")
            .Trim();
    }
}
```

### 5.3 Sorun: Sipariş Satırlarını Birleştirme

**Hedef**: Katana'daki ayrı varyantları tek siparişte birleştir.

```csharp
public class OrderConsolidationService
{
    public async Task<SalesOrderDto> CreateConsolidatedOrder(
        long customerId,
        List<VariantOrderItem> items)
    {
        // 1. Tüm varyantları tek siparişte topla
        var salesOrder = new SalesOrderCreateRequest
        {
            CustomerId = customerId,
            OrderNo = GenerateOrderNo(),
            SalesOrderRows = items.Select(item => new SalesOrderRowCreateDto
            {
                VariantId = item.VariantId,
                Quantity = item.Quantity,
                PricePerUnit = item.UnitPrice,
                Attributes = new List<SalesOrderRowAttributeDto>
                {
                    new() { Key = "Color", Value = item.Color },
                    new() { Key = "Size", Value = item.Size }
                }
            }).ToList()
        };

        // 2. Katana'ya gönder
        var createdOrder = await _katanaService.CreateSalesOrderAsync(salesOrder);

        return createdOrder;
    }
}
```

### 5.4 Önerilen Mimari

```
┌─────────────────────────────────────────────────────────────┐
│                    KATANA INTEGRATION                        │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│              VARIANT CONSOLIDATION SERVICE                   │
│  • Varyantları grupla                                        │
│  • Ana ürün belirle                                          │
│  • Sipariş satırlarını birleştir                            │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                 ORDER MAPPING SERVICE                        │
│  • Katana Order → Luca Invoice                              │
│  • Çoklu satır desteği                                       │
│  • Varyant bilgilerini açıklama alanına ekle               │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    LUCA SERVICE                              │
│  • Tek belge oluştur                                         │
│  • Tüm satırları ekle                                        │
│  • Belge numarası al                                         │
└─────────────────────────────────────────────────────────────┘
```

---

## 6. Uygulama Önerileri

### 6.1 Kısa Vadeli Çözüm (Hızlı)

1. **Mevcut varyantları olduğu gibi bırak**
2. **Sipariş gönderirken birleştir**:
   ```csharp
   // Tüm varyantları tek faturada gönder
   var lucaInvoice = ConsolidateOrderLines(order);
   await _lucaService.SendInvoiceAsync(lucaInvoice);
   ```

### 6.2 Orta Vadeli Çözüm (Önerilen)

1. **Varyant yapısını düzenle**:

   - Ana ürünleri belirle
   - Varyantları ana ürün altında grupla
   - SKU yapısını standartlaştır: `PRODUCT-COLOR-SIZE`

2. **Deduplication yap**:

   - Benzer ürünleri birleştir
   - Eski kayıtları temizle

3. **Sipariş akışını optimize et**:
   - Tek sipariş, çoklu satır
   - Varyant bilgilerini attribute olarak sakla

### 6.3 Uzun Vadeli Çözüm (İdeal)

1. **Master Data Management**:

   - Merkezi ürün kataloğu
   - Varyant yönetim sistemi
   - SKU standardizasyonu

2. **Otomatik Senkronizasyon**:

   - Katana → Luca otomatik aktarım
   - Hata yönetimi ve retry mekanizması
   - Webhook entegrasyonu

3. **BOM Entegrasyonu**:
   - Üretim emirlerini Luca'ya aktar
   - Hammadde tüketimini izle
   - Maliyet hesaplama

---

## 7. Örnek Senaryolar

### Senaryo 1: Varyantlı Ürün Siparişi

**Durum**: Müşteri 3 farklı renkte t-shirt sipariş ediyor.

**Katana'da**:

```
Sales Order: SO-12345
├── Row 1: TSHIRT-RED-M   (Qty: 10, Price: 50 TL)
├── Row 2: TSHIRT-BLUE-M  (Qty: 15, Price: 50 TL)
└── Row 3: TSHIRT-GREEN-M (Qty: 20, Price: 50 TL)
```

**Luca'ya Gönderim**:

```json
{
  "belge_seri": "A",
  "belge_no": "SO-12345",
  "cari_kodu": "CUST-001",
  "satirlar": [
    {
      "stok_kodu": "TSHIRT-RED-M",
      "miktar": 10,
      "birim_fiyat": 50.0,
      "aciklama": "T-Shirt - Kırmızı - M Beden"
    },
    {
      "stok_kodu": "TSHIRT-BLUE-M",
      "miktar": 15,
      "birim_fiyat": 50.0,
      "aciklama": "T-Shirt - Mavi - M Beden"
    },
    {
      "stok_kodu": "TSHIRT-GREEN-M",
      "miktar": 20,
      "birim_fiyat": 50.0,
      "aciklama": "T-Shirt - Yeşil - M Beden"
    }
  ]
}
```

### Senaryo 2: BOM ile Üretim

**Durum**: Müşteri 10 adet masa sipariş ediyor, stokta yok, üretilmesi gerekiyor.

**Adım 1: Sales Order**

```
Sales Order: SO-67890
└── Row 1: MASA-001 (Qty: 10, Price: 1000 TL)
```

**Adım 2: Manufacturing Order (Otomatik)**

```
Manufacturing Order: MO-11111
├── Product: MASA-001
├── Quantity: 10
└── BOM:
    ├── AHSAP-TABLA: 10 adet
    ├── AYAK: 40 adet
    └── VIDA: 160 adet
```

**Adım 3: Üretim Tamamlama**

```
Production: PROD-22222
├── Manufacturing Order: MO-11111
├── Completed Quantity: 10
└── Ingredients Consumed:
    ├── AHSAP-TABLA: 10 adet (stok azaldı)
    ├── AYAK: 40 adet (stok azaldı)
    └── VIDA: 160 adet (stok azaldı)
```

**Adım 4: Stok Güncelleme**

```
MASA-001: +10 adet (üretim tamamlandı)
```

**Adım 5: Sales Order Karşılama**

```
Sales Order: SO-67890
└── Status: SHIPPED (10 adet masa gönderildi)
```

---

## 8. Sonuç ve Öneriler

### Yapılması Gerekenler

1. **Acil (1 Hafta)**:

   - [ ] Mevcut sipariş gönderim kodunu düzenle
   - [ ] Tüm varyantları tek faturada gönder
   - [ ] Test senaryoları oluştur

2. **Kısa Vade (1 Ay)**:

   - [ ] Varyant deduplication servisi yaz
   - [ ] Benzer ürünleri birleştir
   - [ ] SKU standardizasyonu yap

3. **Orta Vade (3 Ay)**:

   - [ ] BOM entegrasyonunu tamamla
   - [ ] Manufacturing Order → Luca aktarımı
   - [ ] Otomatik senkronizasyon

4. **Uzun Vade (6 Ay)**:
   - [ ] Master Data Management sistemi
   - [ ] Webhook entegrasyonu
   - [ ] Gerçek zamanlı stok takibi

### Kritik Noktalar

⚠️ **DİKKAT**:

- Varyant silme işlemi **geri alınamaz**
- Önce test ortamında dene
- Backup al
- Adım adım ilerle

✅ **BAŞARI KRİTERLERİ**:

- Tek sipariş = Tek fatura
- Tüm varyantlar ayrı satır olarak
- Stok takibi doğru çalışıyor
- BOM entegrasyonu aktif

---

## 9. İletişim ve Destek

Sorularınız için:

- **Teknik Destek**: [email]
- **Dokümantasyon**: Bu dosya
- **API Referansı**: Katana API Docs, Luca API Docs

**Son Güncelleme**: 2025-01-15
**Versiyon**: 1.0
