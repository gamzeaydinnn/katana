# KATANA SİPARİŞ AKIŞI DETAYLI RAPOR

## 📋 GENEL BAKIŞ

Bu rapor, Katana'dan gelen siparişlerin sistemde nasıl işlendiğini, admin onayı sonrası Katana'ya nasıl geri gönderildiğini ve tüm entegrasyon noktalarını detaylı olarak açıklar.

## 🔄 SİPARİŞ AKIŞI ADIMLARI

### 1️⃣ KATANA'DAN SİPARİŞ ÇEKME (Otomatik - Her 5 Dakika)

**Sorumlu Servis:** `KatanaSalesOrderSyncWorker`
**Dosya:** `src/Katana.API/Workers/KatanaSalesOrderSyncWorker.cs`

#### İşleyiş:

- Her 5 dakikada bir otomatik çalışır
- Son 7 gündeki siparişleri Katana API'sinden çeker
- Batch processing ile 100'er sipariş işler (memory-efficient)
- Duplicate kontrolü yapar (aynı sipariş tekrar işlenmez)

#### Yapılan İşlemler:

1. **Katana API Çağrısı:**

   ```csharp
   await katanaService.GetSalesOrdersBatchedAsync(fromDate, batchSize: 100)
   ```

2. **SalesOrders Tablosuna Kayıt:**

   - Tüm siparişler `SalesOrders` tablosuna kaydedilir
   - Sipariş satırları `SalesOrderLines` tablosuna eklenir
   - Müşteri bilgileri eşleştirilir (ReferenceId ile)

3. **PendingStockAdjustment Oluşturma:**
   - Sadece aktif siparişler için (cancelled/done/shipped hariç)
   - Her sipariş kalemi için negatif miktar ile kayıt oluşturulur
   - Composite key ile duplicate önlenir: `OrderId|SKU|Quantity`

#### Örnek Veri Akışı:

```json
{
  "katanaOrderId": 12345,
  "orderNo": "SO-2024-001",
  "customerId": 789,
  "status": "NOT_SHIPPED",
  "lines": [
    {
      "variantId": 456,
      "sku": "PROD-001",
      "quantity": 10,
      "pricePerUnit": 100.0
    }
  ]
}
```

**Sonuç:**

- ✅ Sipariş `SalesOrders` tablosuna kaydedilir
- ✅ Her kalem için `PendingStockAdjustment` oluşturulur (Quantity: -10)
- ✅ Admin panelinde "Siparişler" ekranında görünür hale gelir

---

### 2️⃣ ADMİN PANELDE SİPARİŞ GÖRÜNTÜLEME

**Sorumlu Component:** `SalesOrders.tsx`
**Dosya:** `frontend/katana-web/src/components/Admin/SalesOrders.tsx`

#### Özellikler:

- Tüm siparişler listelenir
- Durum filtreleme (Pending, Approved, Shipped, Cancelled)
- Senkronizasyon durumu gösterimi
- Müşteri bilgileri
- Sipariş detayları (kalemler, toplam tutar)

#### Admin Aksiyonları:

1. **Sipariş Detayı Görüntüleme:** Tüm kalemleri ve müşteri bilgilerini gösterir
2. **Onaylama Butonu:** Siparişi onayla ve Katana'ya gönder
3. **Luca Senkronizasyonu:** Manuel olarak Luca'ya fatura gönderme

---

### 3️⃣ ADMİN ONAYI VE KATANA'YA GÖNDERME

**Sorumlu Endpoint:** `POST /api/sales-orders/{id}/approve`
**Dosya:** `src/Katana.API/Controllers/SalesOrdersController.cs`

#### İşleyiş Adımları:

**A. Validasyon:**

```csharp
// 1. Sipariş kontrolü
var order = await _context.SalesOrders
    .Include(s => s.Lines)
    .FirstOrDefaultAsync(s => s.Id == id);

// 2. Durum kontrolü
if (order.Status == "APPROVED" || order.Status == "SHIPPED")
    return BadRequest("Bu sipariş zaten onaylanmış");

// 3. Sipariş satırları kontrolü
if (order.Lines == null || order.Lines.Count == 0)
    return BadRequest("Sipariş satırları bulunamadı");
```

**B. Katana'ya Stok Senkronizasyonu:**
Her sipariş kalemi için:

```csharp
foreach (var line in order.Lines)
{
    var ok = await _katanaService.SyncProductStockAsync(
        sku: line.SKU,
        quantity: line.Quantity,
        locationId: order.LocationId,
        productName: line.ProductName,
        salesPrice: line.PricePerUnit
    );
}
```

**C. SyncProductStockAsync Detayı:**
`src/Katana.Infrastructure/APIClients/KatanaService.cs`

1. **Variant Bulma:**

   ```csharp
   // SKU ile Katana'da variant arama
   var (variantId, productId) = await FindVariantAsync(sku);
   ```

2. **Ürün Yoksa Oluşturma:**

   ```csharp
   if (!variantId.HasValue) {
       var createDto = new KatanaProductDto {
           // İsim boş gelirse SKU ile fallback
           Name = string.IsNullOrWhiteSpace(productName) ? $"Yeni Ürün ({sku})" : productName.Trim(),
           SKU = sku.Trim(),
           SalesPrice = salesPrice ?? 0,
           Unit = "pcs",
           IsActive = true
       };
       var created = await CreateProductAsync(createDto);
       // CreateProductAsync null dönerse bile (örn: SKU zaten var / yarış durumu),
       // variant tekrar sorgulanır ve süreç kesilmez.
   }
   ```

3. **Location Çözümleme:**

   ```csharp
   // Primary location bulma veya cache'ten alma
   var resolvedLocationId = await ResolveLocationIdAsync();
   ```

4. **Stock Adjustment Oluşturma:**

   ```csharp
   var req = new StockAdjustmentCreateRequest {
       // Yeni ürün oluşturulduysa fiş numarası ADMIN-NEW ile işaretlenir
       StockAdjustmentNumber = $"{(createdNewProduct ? "ADMIN-NEW" : "ADMIN")}-{DateTime.UtcNow:yyyyMMddHHmmss}-{sku}",
       StockAdjustmentDate = DateTime.UtcNow,
       LocationId = resolvedLocationId.Value,
       Reason = "Admin approval",
       AdditionalInfo = createdNewProduct
           ? $"SalesOrder approval stock increase for NEW SKU={sku}"
           : $"SalesOrder approval stock increase for SKU={sku}",
       StockAdjustmentRows = new List<StockAdjustmentRowDto> {
           new StockAdjustmentRowDto {
               VariantId = variantId.Value,
               Quantity = quantity
           }
       }
   };

   var createdAdj = await CreateStockAdjustmentAsync(req);
   ```

**D. Sipariş Durumu Güncelleme:**

```csharp
order.Status = failCount == 0 ? "APPROVED" : "APPROVED_WITH_ERRORS";
order.LastSyncError = failCount == 0 ? null : errorMessages;
order.UpdatedAt = DateTime.UtcNow;
await _context.SaveChangesAsync();
```

**E. Audit Log:**

```csharp
_auditService.LogUpdate(
    "SalesOrder",
    id.ToString(),
    User.Identity?.Name ?? "System",
    null,
    $"Sipariş onaylandı ve Katana'ya {successCount} ürün eklendi/güncellendi"
);
```

#### Başarı Senaryosu:

```json
{
  "success": true,
  "message": "Sipariş onaylandı. 3 ürün Katana'ya eklendi/güncellendi.",
  "orderNo": "SO-2024-001",
  "orderStatus": "APPROVED",
  "successCount": 3,
  "failCount": 0,
  "syncResults": [
    { "sku": "PROD-001", "success": true, "action": "synced" },
    { "sku": "PROD-002", "success": true, "action": "synced" },
    { "sku": "PROD-003", "success": true, "action": "synced" }
  ]
}
```

#### Hata Senaryosu:

```json
{
  "success": false,
  "message": "Sipariş onaylandı ama Katana senkronunda hata var. Başarılı: 2, Hatalı: 1.",
  "orderNo": "SO-2024-001",
  "orderStatus": "APPROVED_WITH_ERRORS",
  "successCount": 2,
  "failCount": 1,
  "syncResults": [
    { "sku": "PROD-001", "success": true, "action": "synced" },
    { "sku": "PROD-002", "success": true, "action": "synced" },
    {
      "sku": "PROD-003",
      "success": false,
      "error": "Katana stok senkronu başarısız"
    }
  ]
}
```

---

### 4️⃣ KATANA API ÇAĞRILARI

**Kullanılan Endpoint'ler:**

#### A. Variant Arama:

```http
GET /api/v1/variants?sku={sku}
Authorization: Bearer {katana_api_key}
```

#### B. Ürün Oluşturma:

```http
POST /api/v1/products
Authorization: Bearer {katana_api_key}
Content-Type: application/json

{
  "name": "Ürün Adı",
  "sku": "PROD-001",
  "sales_price": 100.00,
  "unit": "pcs",
  "is_active": true
}
```

#### C. Stock Adjustment Oluşturma:

```http
POST /api/v1/stock_adjustments
Authorization: Bearer {katana_api_key}
Content-Type: application/json

{
  "stock_adjustment_number": "ADMIN-20241213120000-PROD001",
  "stock_adjustment_date": "2024-12-13T12:00:00Z",
  "location_id": 1,
  "reason": "Admin approval",
  "additional_info": "SalesOrder approval stock increase for SKU=PROD-001",
  "stock_adjustment_rows": [
    {
      "variant_id": 456,
      "quantity": 10
    }
  ]
}
```

#### D. Location Listesi:

```http
GET /api/v1/locations
Authorization: Bearer {katana_api_key}
```

---

### 5️⃣ VERİTABANI YAPISI

#### SalesOrders Tablosu:

```sql
CREATE TABLE SalesOrders (
    Id INT PRIMARY KEY IDENTITY,
    KatanaOrderId BIGINT NOT NULL,
    OrderNo NVARCHAR(100),
    CustomerId INT,
    OrderCreatedDate DATETIME2,
    DeliveryDate DATETIME2,
    Currency NVARCHAR(10),
    Status NVARCHAR(50),
    Total DECIMAL(18,2),
    TotalInBaseCurrency DECIMAL(18,2),
    LocationId BIGINT,
    IsSyncedToLuca BIT DEFAULT 0,
    LucaOrderId INT NULL,
    LastSyncAt DATETIME2 NULL,
    LastSyncError NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL
);
```

#### SalesOrderLines Tablosu:

```sql
CREATE TABLE SalesOrderLines (
    Id INT PRIMARY KEY IDENTITY,
    SalesOrderId INT NOT NULL,
    KatanaRowId BIGINT,
    VariantId BIGINT,
    SKU NVARCHAR(100),
    ProductName NVARCHAR(500),
    Quantity DECIMAL(18,2),
    PricePerUnit DECIMAL(18,2),
    Total DECIMAL(18,2),
    TaxRate DECIMAL(5,2),
    LocationId BIGINT,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (SalesOrderId) REFERENCES SalesOrders(Id)
);
```

#### PendingStockAdjustments Tablosu:

```sql
CREATE TABLE PendingStockAdjustments (
    Id INT PRIMARY KEY IDENTITY,
    ExternalOrderId NVARCHAR(100),
    ProductId INT,
    Sku NVARCHAR(100),
    Quantity INT,
    RequestedBy NVARCHAR(100),
    RequestedAt DATETIME2,
    Status NVARCHAR(50), -- Pending, Approved, Rejected
    Notes NVARCHAR(MAX),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);
```
