# SİPARİŞ SENKRONIZASYONU DETAYLI AKIŞI

## 📋 İçindekiler

1. [Satış Siparişi Tam Akışı](#satış-siparişi-tam-akışı)
2. [Satınalma Siparişi Tam Akışı](#satınalma-siparişi-tam-akışı)
3. [Admin Onay Mekanizması](#admin-onay-mekanizması)
4. [Luca'ya Senkronizasyon](#lucaya-senkronizasyon)
5. [Hata Senaryoları](#hata-senaryoları)

---

## 🛒 Satış Siparişi Tam Akışı

### Aşama 1: Katana'dan Sipariş Çekme

**Worker**: `KatanaSalesOrderSyncWorker`
**Sıklık**: Her 5 dakikada bir
**Dosya**: `src/Katana.API/Workers/KatanaSalesOrderSyncWorker.cs`

```
1. Katana API'den son 7 günün siparişlerini çek
   GET /api/v1/sales_orders?status=NOT_SHIPPED&days=7

   Response:
   {
     "data": [
       {
         "id": 123456789,
         "order_no": "SO-001",
         "customer_id": 91190794,
         "status": "NOT_SHIPPED",
         "order_created_date": "2025-01-15T10:30:00Z",
         "currency": "TRY",
         "total": "7500.00",
         "sales_order_rows": [
           {
             "variant_id": 987654,
             "quantity": 50,
             "price_per_unit": "150.00",
             "total": "7500.00"
           }
         ]
       }
     ]
   }

2. Her sipariş için:
   a. Duplicate kontrol
      - SalesOrders tablosunda KatanaOrderId ile ara
      - Varsa: Skip (zaten var)
      - Yoksa: Devam et

   b. Müşteri bilgisi kontrol
      - Customers tablosunda CustomerId ile ara
      - Yoksa: Katana'dan müşteri çek ve oluştur

   c. SalesOrder entity oluştur
      {
        "KatanaOrderId": 123456789,
        "OrderNo": "SO-001",
        "CustomerId": 91190794,
        "Status": "PENDING",
        "OrderDate": "2025-01-15T10:30:00Z",
        "Currency": "TRY",
        "Total": 7500.00,
        "IsSyncedToLuca": false,
        "CreatedAt": "2025-12-24T10:30:00Z"
      }

   d. SalesOrderLine entity'leri oluştur
      {
        "SalesOrderId": 1,
        "SKU": "PIPE-001",
        "Quantity": 50,
        "UnitPrice": 150.00,
        "Total": 7500.00
      }

   e. Database'e kaydet

3. PendingStockAdjustment oluştur (Admin onayı için)
   {
     "Type": "SalesOrder",
     "ReferenceId": 123456789,
     "Status": "Pending",
     "CreatedAt": "2025-12-24T10:30:00Z"
   }

4. SignalR ile admin paneline bildirim gönder
   - "Yeni sipariş: SO-001"
   - Admin paneli otomatik yenilenir
```

### Aşama 2: Admin Panelinde Görüntüleme

**Endpoint**: `GET /api/sales-orders`
**Dosya**: `src/Katana.API/Controllers/SalesOrdersController.cs`

```
Admin Paneli (Frontend)
    │
    ├─ Siparişler Listesi
    │  ├─ SO-001 | ABC Tekstil | 7500.00 TRY | Pending
    │  ├─ SO-002 | XYZ Ltd. | 5000.00 TRY | Pending
    │  └─ ...
    │
    └─ Her sipariş için:
       ├─ [Detayları Gör] → Satırları göster
       ├─ [Admin Onayı] → Katana'ya stok ekleme
       └─ [Kozaya Senkronize] → Luca'ya fatura gönderme
```

### Aşama 3: Admin Onayı

**Endpoint**: `POST /api/sales-orders/{id}/approve`
**Yetki**: Admin, Manager
**Dosya**: `src/Katana.API/Controllers/SalesOrdersController.cs`

```
Admin [Admin Onayı] Butonuna Tıklar
    │
    ▼
1. Sipariş Kontrolü
   - Sipariş var mı?
   - Zaten onaylanmış mı?
   - Sipariş satırları var mı?

2. Her Sipariş Kalemi İçin Katana'ya Stok Ekleme

   foreach (line in order.Lines)
   {
       a. SKU kontrolü
          if (string.IsNullOrWhiteSpace(line.SKU))
              throw new Exception("SKU boş!");

       b. Katana'da ürün var mı kontrol et
          var existingProduct = await _katanaService.GetProductBySkuAsync(line.SKU);

       c. Ürün VARSA:
          - Mevcut stok: 100
          - Sipariş miktarı: 50
          - Yeni stok: 100 + 50 = 150
          - UpdateProductAsync(productId, newStock: 150)
          - Sonuç: ✅ "Stok güncellendi"

       d. Ürün YOKSA:
          - CreateProductAsync(sku, name, stock: 50)
          - Sonuç: ✅ "Ürün oluşturuldu ve stok set edildi"
   }

3. Sipariş Durumu Güncelleme
   - Tüm kalemler başarılı: Status = "APPROVED"
   - Bazı kalemler hatalı: Status = "APPROVED_WITH_ERRORS"
   - LastSyncError alanı güncellenir

4. Response Dönüşü
   {
     "success": true,
     "message": "Sipariş onaylandı. 1 ürün Katana'ya eklendi/güncellendi.",
     "orderNo": "SO-001",
     "orderStatus": "APPROVED",
     "successCount": 1,
     "failCount": 0,
     "syncResults": [
       {
         "sku": "PIPE-001",
         "quantity": 50,
         "status": "success",
         "message": "Stok güncellendi: 100 → 150"
       }
     ]
   }

5. Audit Log
   - AuditService.LogUpdate("SalesOrder", id, "Sipariş onaylandı...")
   - LoggingService.LogInfo("SO-001 onaylandı")
```

### Aşama 4: Kozaya Senkronize Et

**Endpoint**: `POST /api/sales-orders/{id}/sync`
**Yetki**: Admin
**Dosya**: `src/Katana.API/Controllers/SalesOrdersController.cs`

```
Admin [Kozaya Senkronize] Butonuna Tıklar
    │
    ▼
1. Sipariş Kontrolü
   - Sipariş var mı?
   - Müşteri bilgisi var mı?
   - Sipariş satırları var mı?

2. Duplicate Kontrolü
   - Zaten senkronize edilmiş ve hata yoksa → BadRequest
   - if (order.IsSyncedToLuca && order.LastSyncError == null)
       throw new BadRequestException("Zaten senkronize edilmiş");

3. Luca Request Hazırlama
   var lucaRequest = MappingHelper.MapToLucaSalesOrderHeader(order, customer);

   Mapping:
   {
     "belgeSeri": "EFA2025",
     "belgeNo": "SO-001",
     "belgeTarihi": "15/01/2025",
     "vadeTarihi": "15/01/2025",
     "faturaTur": "1",  // 1 = Satış
     "paraBirimKod": "TRY",
     "cariKodu": "CUST_1234567890",
     "cariTanim": "ABC Tekstil Ltd.",
     "cariAd": "ABC Tekstil Ltd.",
     "cariSoyad": "ABC Tekstil Ltd.",
     "vergiNo": "1234567890",
     "detayList": [
       {
         "kartKodu": "PIPE-001",
         "kartAdi": "COOLING WATER PIPE",
         "miktar": 50.0,
         "birimFiyat": 150.0,
         "kdvOran": 0.18,
         "tutar": 7500.0
       }
     ]
   }

4. Luca API Çağrısı
   var result = await _lucaService.CreateSalesOrderHeaderAsync(lucaRequest);

   Luca Response:
   {
     "success": true,
     "siparisId": 12345,
     "belgeTakipNo": "SO-001"
   }

5. Response İşleme
   a. Başarılı ise:
      - LucaOrderId = 12345
      - IsSyncedToLuca = true
      - LastSyncAt = DateTime.UtcNow
      - LastSyncError = null
      - SaveChanges()

   b. Başarısız ise:
      - LastSyncError = hata mesajı
      - IsSyncedToLuca = false
      - LastSyncAt = DateTime.UtcNow
      - SaveChanges()

6. Response Dönüşü
   {
     "isSuccess": true,
     "message": "Luca'ya başarıyla senkronize edildi",
     "lucaOrderId": 12345,
     "syncedAt": "2025-01-15T10:30:00Z",
     "errorDetails": null
   }
```

---

## 📦 Satınalma Siparişi Tam Akışı

### Aşama 1: Manuel Oluşturma

**Endpoint**: `POST /api/purchase-orders`
**Dosya**: `src/Katana.API/Controllers/PurchaseOrdersController.cs`

```
Admin Paneli → [Yeni Satınalma Siparişi]
    │
    ▼
Request Body:
{
  "poNumber": "PO-001",
  "supplierId": 123,
  "orderDate": "2025-12-24",
  "expectedDeliveryDate": "2025-12-31",
  "currency": "TRY",
  "items": [
    {
      "sku": "PIPE-001",
      "quantity": 100,
      "unitPrice": 100.00
    }
  ]
}

    │
    ▼
1. Tedarikçi Kontrolü
   - Supplier var mı?
   - Supplier aktif mi?

2. PurchaseOrder Entity Oluştur
   {
     "PoNumber": "PO-001",
     "SupplierId": 123,
     "Status": "Pending",
     "OrderDate": "2025-12-24",
     "ExpectedDeliveryDate": "2025-12-31",
     "Currency": "TRY",
     "Total": 10000.00,
     "CreatedAt": "2025-12-24T10:30:00Z"
   }

3. PurchaseOrderItem Entity'leri Oluştur
   {
     "PurchaseOrderId": 1,
     "SKU": "PIPE-001",
     "Quantity": 100,
     "UnitPrice": 100.00,
     "Total": 10000.00
   }

4. Database'e Kaydet
   - SaveChanges()
   - Status: Pending
```

### Aşama 2: Durum Güncelleme

**Endpoint**: `PATCH /api/purchase-orders/{id}/status`
**Dosya**: `src/Katana.API/Controllers/PurchaseOrdersController.cs`

```
Admin Paneli → [Durum Güncelle]
    │
    ├─ Pending → Approved
    ├─ Approved → Received
    └─ Received → (Kapalı)
    │
    ▼
1. Durum Geçiş Kontrolü
   - StatusMapper.IsValidTransition(oldStatus, newStatus)
   - Geçersiz geçişler reddedilir

2. "Approved" Durumuna Geçişte

   ✅ Arka planda Katana'ya ürün ekleme/güncelleme başlatılır

   Task.Run(async () =>
   {
       foreach (item in order.Items)
       {
           a. Katana'da ürün var mı kontrol et
              var existingProduct = await _katanaService.GetProductBySkuAsync(item.SKU);

           b. Ürün VARSA:
              - Mevcut stok: 50
              - Satınalma miktarı: 100
              - Yeni stok: 50 + 100 = 150
              - UpdateProductAsync(productId, newStock: 150)

           c. Ürün YOKSA:
              - CreateProductAsync(sku, name, stock: 100)
       }
   });

3. "Received" Durumuna Geçişte

   ✅ StockMovement kayıtları oluşturulur

   foreach (item in order.Items)
   {
       var movement = new StockMovement
       {
           ProductId = product.Id,
           ProductSku = item.SKU,
           ChangeQuantity = item.Quantity,
           MovementType = MovementType.In,
           SourceDocument = "PurchaseOrder",
           Timestamp = DateTime.UtcNow,
           WarehouseCode = "MAIN",
           IsSynced = false
       };
       _context.StockMovements.Add(movement);
   }
   SaveChanges();

4. Status Güncelleme
   - order.Status = newStatus
   - order.UpdatedAt = DateTime.UtcNow
   - SaveChanges()
```

### Aşama 3: Kozaya Senkronize Et

**Endpoint**: `POST /api/purchase-orders/{id}/sync`
**Dosya**: `src/Katana.API/Controllers/PurchaseOrdersController.cs`

```
Admin Paneli → [Kozaya Senkronize]
    │
    ▼
1. Sipariş Kontrolü
   - Sipariş var mı?
   - Tedarikçi bilgisi var mı?

2. Luca FATURA Request Hazırlama

   ⚠️ ÖNEMLİ: Satınalma siparişi FATURA olarak gönderilir!

   var lucaInvoiceRequest = MappingHelper.MapToLucaInvoiceFromPurchaseOrder(order, supplier);

   Mapping:
   {
     "belgeSeri": "EFA2025",
     "belgeNo": "PO-001",
     "belgeTarihi": "24/12/2025",
     "faturaTur": "2",  // 2 = Alış
     "paraBirimKod": "TRY",
     "cariKodu": "SUPP_123",
     "cariTanim": "Tedarikçi Adı",
     "cariAd": "Tedarikçi Adı",
     "cariSoyad": "Tedarikçi Adı",
     "vergiNo": "1234567890",
     "detayList": [
       {
         "kartKodu": "PIPE-001",
         "kartAdi": "COOLING WATER PIPE",
         "miktar": 100.0,
         "birimFiyat": 100.0,
         "kdvOran": 0.18,
         "tutar": 10000.0
       }
     ]
   }

3. Luca API Çağrısı
   var syncResult = await _lucaService.SendInvoiceAsync(lucaInvoiceRequest);

   ⚠️ Session yenileme otomatik (SendInvoiceAsync içinde)

4. Response İşleme
   a. Başarılı ise:
      - IsSyncedToLuca = true
      - LastSyncAt = DateTime.UtcNow
      - LastSyncError = null
      - SyncRetryCount = 0

   b. Başarısız ise:
      - LastSyncError = hata mesajı
      - SyncRetryCount++

5. Response Dönüşü
   {
     "success": true,
     "lucaPurchaseOrderId": null,
     "lucaDocumentNo": "PO-20240115-ABC123",
     "message": "Fatura başarıyla Luca'ya aktarıldı"
   }
```

---

## ✅ Admin Onay Mekanizması

### Onay Akışı

```
┌─────────────────────────────────────────────────────────┐
│ KATANA'DAN GELEN SİPARİŞ                                │
│ (KatanaSalesOrderSyncWorker tarafından çekilen)         │
└────────────────────┬────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────┐
│ SalesOrder (Status: PENDING)                            │
│ - IsSyncedToLuca: false                                 │
│ - LastSyncError: null                                   │
└────────────────────┬────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────┐
│ Admin Paneli                                            │
│ - Siparişler Listesi                                    │
│ - [Admin Onayı] Butonu                                  │
└────────────────────┬────────────────────────────────────┘
                     │
                     ├─ [Admin Onayı] Tıkla
                     │  │
                     │  ├─ Katana'ya stok ekleme/güncelleme
                     │  │  ├─ Ürün var mı kontrol
                     │  │  ├─ Ürün VARSA: Stok güncelle
                     │  │  └─ Ürün YOKSA: Yeni ürün oluştur
                     │  │
                     │  └─ Status: APPROVED
                     │
                     └─ [Kozaya Senkronize] Tıkla
                        │
                        ├─ Müşteri bilgisi kontrol
                        ├─ Sipariş satırları kontrol
                        ├─ Luca'ya fatura gönder
                        └─ IsSyncedToLuca: true
```

### Onay Sonrası Durumlar

```
Başarılı Onay:
├─ Status: APPROVED
├─ IsSyncedToLuca: false (henüz Luca'ya gönderilmedi)
└─ LastSyncError: null

Başarılı Onay + Senkronizasyon:
├─ Status: APPROVED
├─ IsSyncedToLuca: true
├─ LucaOrderId: 12345
└─ LastSyncError: null

Onay Hatası:
├─ Status: PENDING (değişmez)
├─ LastSyncError: "SKU boş!"
└─ IsSyncedToLuca: false

Senkronizasyon Hatası:
├─ Status: APPROVED (onay başarılı)
├─ IsSyncedToLuca: false
└─ LastSyncError: "Müşteri bilgisi eksik"
```

---

## 🔄 Luca'ya Senkronizasyon

### Senkronizasyon Türleri

#### 1. Tekil Senkronizasyon

```
POST /api/sales-orders/{id}/sync
    │
    ├─ Tek bir sipariş
    ├─ Senkron işlem
    └─ Hemen sonuç döner
```

#### 2. Toplu Senkronizasyon

```
POST /api/sales-orders/sync-all?maxCount=50
    │
    ├─ Bekleyen siparişleri çek (IsSyncedToLuca = false)
    ├─ Paralel işleme (5 eşzamanlı istek)
    ├─ Performance metrics
    └─ Rapor döner

Response:
{
  "totalProcessed": 50,
  "successCount": 48,
  "failCount": 2,
  "durationMs": 12500,
  "rateOrdersPerMinute": 230.4,
  "errors": [...]
}
```

#### 3. Retry Mekanizması

```
POST /api/purchase-orders/retry-failed?maxRetries=3
    │
    ├─ Hatalı siparişleri çek (LastSyncError != null)
    ├─ Paralel işleme (3 eşzamanlı istek)
    ├─ SyncRetryCount kontrol
    └─ Rapor döner
```

---

## ⚠️ Hata Senaryoları

### Senaryo 1: Müşteri Bilgisi Eksik

```
Sipariş: SO-001
Müşteri: Customers tablosunda yok

Onay Sırasında:
- Katana'dan müşteri çek
- Müşteri yoksa: Exception
- Sonuç: ❌ Onay başarısız

Çözüm:
- Müşteri bilgisini Katana'da kontrol et
- Müşteri yoksa: Katana'da oluştur
- Sonra siparişi tekrar senkronize et
```

### Senaryo 2: SKU Boş

```
Sipariş Kalemi: SKU = ""

Onay Sırasında:
- SKU kontrolü
- SKU boş: Exception
- Sonuç: ❌ Onay başarısız

Çözüm:
- Katana'da sipariş satırını kontrol et
- SKU'yu doldur
- Siparişi tekrar senkronize et
```

### Senaryo 3: Luca API Hatası

```
Luca'ya Senkronizasyon:
- HTTP 500 Internal Server Error
- Luca session timeout
- Network hatası

Sonuç:
- IsSyncedToLuca: false
- LastSyncError: "Luca API hatası"
- LastSyncAt: Güncellenir

Çözüm:
- Luca'nın durumunu kontrol et
- /api/sales-orders/{id}/sync ile retry
- Veya /api/sales-orders/retry-failed ile toplu retry
```

### Senaryo 4: Duplicate Barcode

```
Ürün: PIPE-V2 (Versiyonlu)
Barkod: "8690123456789"

Luca'da mevcut: PIPE (aynı barkod)

Senkronizasyon:
- Luca: "Duplicate Barcode" hatası
- Sonuç: ❌ Senkronizasyon başarısız

Çözüm:
- Mapper'da versiyonlu SKU kontrolü
- Barkod NULL gönder
- Retry
```

---

## 📊 Özet

### Satış Siparişi Akışı

1. **Katana'dan Çekme** (5 dakikada bir)

   - KatanaSalesOrderSyncWorker
   - SalesOrders tablosuna kaydet
   - PendingStockAdjustments oluştur

2. **Admin Onayı**

   - Katana'ya stok ekleme/güncelleme
   - Status: APPROVED

3. **Kozaya Senkronize**
   - Luca'ya fatura gönder
   - IsSyncedToLuca: true

### Satınalma Siparişi Akışı

1. **Manuel Oluşturma**

   - Admin panelinden oluştur
   - Status: Pending

2. **Durum Güncelleme**

   - Pending → Approved (Katana'ya ürün ekleme)
   - Approved → Received (StockMovement oluştur)

3. **Kozaya Senkronize**
   - Luca'ya fatura gönder (Alış Faturası)
   - IsSyncedToLuca: true

### Kritik Noktalar

- ✅ Onay işlemleri geri alınamaz
- ✅ Senkronizasyon duplicate-safe
- ✅ Paralel işleme ile yüksek performans
- ✅ Detaylı hata raporlama ve retry mekanizması
- ✅ Rol bazlı yetkilendirme
- ✅ Tam audit trail

---

**Rapor Tarihi**: 24 Aralık 2025
**Versiyon**: 1.0
**Hazırlayan**: Kiro AI Assistant
