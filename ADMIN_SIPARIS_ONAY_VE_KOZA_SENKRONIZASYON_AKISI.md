# Admin Paneli Sipariş Onayı ve Koza Senkronizasyon Akışı

## 📋 İçindekiler

1. [Genel Bakış](#genel-bakış)
2. [Satış Siparişleri (Sales Orders)](#satış-siparişleri-sales-orders)
3. [Satınalma Siparişleri (Purchase Orders)](#satınalma-siparişleri-purchase-orders)
4. [Veri Akış Diyagramı](#veri-akış-diyagramı)
5. [API Endpoint'leri](#api-endpointleri)
6. [Hata Yönetimi](#hata-yönetimi)

---

## 🎯 Genel Bakış

Admin panelinde iki tür sipariş yönetimi bulunmaktadır:

### 1. **Satış Siparişleri (Sales Orders)**

- **Kaynak**: Katana ERP sisteminden otomatik senkronizasyon
- **Yön**: Katana → Sistem → Luca (Koza)
- **Amaç**: Müşteri siparişlerini Luca'ya fatura olarak göndermek
- **Admin Aksiyonları**:
  - ✅ **Admin Onayı**: Siparişi onayla ve Katana'ya stok olarak ekle
  - 🔄 **Kozaya Senkronize Et**: Luca'ya fatura olarak gönder

### 2. **Satınalma Siparişleri (Purchase Orders)**

- **Kaynak**: Manuel oluşturma veya sistem içi
- **Yön**: Sistem → Luca (Koza)
- **Amaç**: Tedarikçi siparişlerini Luca'ya fatura olarak göndermek
- **Admin Aksiyonları**:
  - ✅ **Durum Güncelleme**: Pending → Approved → Received
  - 🔄 **Kozaya Senkronize Et**: Luca'ya fatura olarak gönder

---

## 🛒 Satış Siparişleri (Sales Orders)

### Veri Kaynağı ve Senkronizasyon

#### Background Worker: `KatanaSalesOrderSyncWorker`

**Dosya**: `src/Katana.API/Workers/KatanaSalesOrderSyncWorker.cs`

**Çalışma Sıklığı**: Her 5 dakikada bir

**İşleyiş**:

```
1. Katana API'den son 7 günün siparişlerini çek
2. Her sipariş için:
   a. SalesOrders tablosuna kaydet (duplicate check ile)
   b. Sipariş satırlarını (SalesOrderLine) kaydet
   c. Aktif siparişler için PendingStockAdjustment oluştur
3. Yeni siparişler varsa:
   a. Luca'ya stok kartı senkronizasyonu tetikle
   b. Onaylanan siparişleri Luca'ya fatura olarak gönder
   c. SignalR ile admin paneline bildirim gönder
```

**Duplicate Prevention**:

- `KatanaOrderId` ile sipariş kontrolü
- `ExternalOrderId + SKU + Quantity` ile kalem kontrolü

---

### Admin Onayı İşlemi

#### Endpoint: `POST /api/sales-orders/{id}/approve`

**Controller**: `SalesOrdersController.ApproveOrder()`

**Yetki**: `Admin` veya `Manager` rolü gerekli

**İşlem Adımları**:

```csharp
1. Sipariş Kontrolü
   - Sipariş var mı?
   - Zaten onaylanmış mı? (Status: APPROVED veya SHIPPED)
   - Sipariş satırları var mı?

2. Her Sipariş Kalemi İçin Katana'ya Stok Ekleme/Güncelleme
   foreach (line in order.Lines)
   {
       a. SKU kontrolü (boş mu?)

       b. Katana'da ürün var mı kontrol et
          var existingProduct = await _katanaService.GetProductBySkuAsync(line.SKU)

       c. Ürün VARSA:
          - Mevcut stok + sipariş miktarı = yeni stok
          - UpdateProductAsync() ile stok güncelle
          - Sonuç: ✅ "Stok güncellendi"

       d. Ürün YOKSA:
          - CreateProductAsync() ile yeni ürün oluştur
          - UpdateProductAsync() ile stok set et
          - Sonuç: ✅ "Ürün oluşturuldu ve stok set edildi"
   }

3. Sipariş Durumu Güncelleme
   - Tüm kalemler başarılı: Status = "APPROVED"
   - Bazı kalemler hatalı: Status = "APPROVED_WITH_ERRORS"
   - LastSyncError alanı güncellenir

4. Audit Log ve Bildirim
   - AuditService.LogUpdate()
   - LoggingService.LogInfo()

5. Response Dönüşü
   {
     "success": true/false,
     "message": "Sipariş onaylandı. X ürün Katana'ya eklendi/güncellendi.",
     "orderNo": "SO-12345",
     "orderStatus": "APPROVED",
     "successCount": 5,
     "failCount": 0,
     "syncResults": [...]
   }
```

**Önemli Notlar**:

- ⚠️ Onay işlemi **geri alınamaz**
- ✅ Katana'ya stok ekleme **senkron** yapılır (async değil)
- 🔄 Her kalem için ayrı API çağrısı yapılır
- 📊 Detaylı sonuç raporu döner

---

### Kozaya Senkronize Et İşlemi

#### Endpoint: `POST /api/sales-orders/{id}/sync`

**Controller**: `SalesOrdersController.SyncToLuca()`

**Yetki**: `Admin` rolü gerekli

**İşlem Adımları**:

```csharp
1. Sipariş Kontrolü
   - Sipariş var mı?
   - Müşteri bilgisi var mı?
   - Sipariş satırları var mı?

2. Duplicate Kontrolü
   - Zaten senkronize edilmiş ve hata yoksa → BadRequest
   - IsSyncedToLuca = true && LastSyncError = null

3. Luca Request Hazırlama
   var lucaRequest = MappingHelper.MapToLucaSalesOrderHeader(order, customer)

   Mapping içeriği:
   - BelgeSeri (Belge Serisi)
   - BelgeNo (Belge Numarası)
   - CariId (Müşteri ID)
   - BelgeTarihi (Sipariş Tarihi)
   - DuzenlemeSaati (Düzenleme Saati)
   - Satirlar (Sipariş Kalemleri)
     * StokId
     * Miktar
     * BirimFiyat
     * KDVOrani
     * etc.

4. Luca API Çağrısı
   var result = await _lucaService.CreateSalesOrderHeaderAsync(lucaRequest)

5. Response İşleme
   a. Başarılı ise:
      - LucaOrderId = result.siparisId
      - IsSyncedToLuca = true
      - LastSyncAt = DateTime.UtcNow
      - LastSyncError = null

   b. Başarısız ise:
      - LastSyncError = hata mesajı
      - IsSyncedToLuca = false
      - LastSyncAt = DateTime.UtcNow

6. Response Dönüşü
   {
     "isSuccess": true/false,
     "message": "Luca'ya başarıyla senkronize edildi",
     "lucaOrderId": 12345,
     "syncedAt": "2024-01-15T10:30:00Z",
     "errorDetails": null
   }
```

**Hata Durumları**:

- ❌ Müşteri bilgisi eksik
- ❌ Sipariş satırları yok
- ❌ Luca API hatası
- ❌ Zaten senkronize edilmiş

---

### Toplu Senkronizasyon

#### Endpoint: `POST /api/sales-orders/sync-all?maxCount=50`

**Özellikler**:

- ⚡ **Paralel işleme**: 5 eşzamanlı istek
- 🎯 **Hedef**: Senkronize edilmemiş siparişler
- 📊 **Performance metrics**: İşlem süresi ve hız raporu

```csharp
Algoritma:
1. Bekleyen siparişleri çek (IsSyncedToLuca = false, LastSyncError = null)
2. Paralel batch processing (5x concurrency)
3. Her sipariş için SyncToLuca() çağır
4. Sonuçları topla ve raporla

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

---

## 📦 Satınalma Siparişleri (Purchase Orders)

### Durum Yönetimi

#### Sipariş Durumları (Status)

```
Pending → Approved → Received → (Cancelled)
```

#### Endpoint: `PATCH /api/purchase-orders/{id}/status`

**Request Body**:

```json
{
  "newStatus": "Approved"
}
```

**İşlem Adımları**:

```csharp
1. Durum Geçiş Kontrolü
   - StatusMapper.IsValidTransition(oldStatus, newStatus)
   - Geçersiz geçişler reddedilir

2. "Approved" Durumuna Geçişte (KRİTİK)
   - Arka planda Katana'ya ürün ekleme/güncelleme başlatılır
   - Task.Run() ile asenkron işlem

   foreach (item in order.Items)
   {
       a. Katana'da ürün var mı kontrol et
       b. Ürün VARSA:
          - Stok artışı yap (mevcut + sipariş miktarı)
       c. Ürün YOKSA:
          - Yeni ürün oluştur
          - Stok set et
   }

3. "Received" Durumuna Geçişte (KRİTİK)
   - StockMovement kayıtları oluşturulur
   - Stok artışı yapılır
   - (Kod kesik - tam implementasyon görülemiyor)

4. Sipariş Güncelleme
   - Status = newStatus
   - UpdatedAt = DateTime.UtcNow
   - SaveChanges()
```

**Önemli Notlar**:

- ✅ **Approved**: Katana'ya ürün ekleme (arka planda)
- 📦 **Received**: Stok hareketi kaydı oluşturma
- ⚠️ Durum geçişleri geri alınamaz

---

### Kozaya Senkronize Et İşlemi

#### Endpoint: `POST /api/purchase-orders/{id}/sync`

**Controller**: `PurchaseOrdersController.SyncToLuca()`

**İşlem Adımları**:

```csharp
1. Sipariş Kontrolü
   - Sipariş var mı?
   - Tedarikçi bilgisi var mı?

2. Luca FATURA Request Hazırlama
   var lucaInvoiceRequest = MappingHelper.MapToLucaInvoiceFromPurchaseOrder(order, supplier)

   ⚠️ ÖNEMLİ: Satınalma siparişi FATURA olarak gönderilir!

3. Luca API Çağrısı
   var syncResult = await _lucaService.SendInvoiceAsync(lucaInvoiceRequest)

   Not: Session yenileme otomatik (SendInvoiceAsync içinde)

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
     "success": true/false,
     "lucaPurchaseOrderId": null,
     "lucaDocumentNo": "PO-20240115-ABC123",
     "message": "Fatura başarıyla Luca'ya aktarıldı"
   }
```

---

### Toplu Senkronizasyon ve Retry

#### Endpoint: `POST /api/purchase-orders/sync-all?maxCount=50`

**Özellikler**:

- ⚡ **Paralel işleme**: 5 eşzamanlı istek
- 🎯 **Hedef**: Senkronize edilmemiş siparişler

#### Endpoint: `POST /api/purchase-orders/retry-failed?maxRetries=3`

**Özellikler**:

- 🔄 **Retry logic**: Hatalı siparişleri yeniden dene
- 📊 **Retry limit**: maxRetries parametresi ile kontrol
- ⚡ **Paralel işleme**: 3 eşzamanlı istek

---

## 🔄 Veri Akış Diyagramı

### Satış Siparişi Akışı

```
┌─────────────┐
│   Katana    │
│     ERP     │
└──────┬──────┘
       │ (Her 5 dk)
       ▼
┌─────────────────────────┐
│ KatanaSalesOrderSync    │
│       Worker            │
└──────┬──────────────────┘
       │
       ├─► SalesOrders (DB)
       ├─► SalesOrderLines (DB)
       └─► PendingStockAdjustments (DB)

       ▼
┌─────────────────────────┐
│   Admin Panel           │
│   (Siparişler)          │
└──────┬──────────────────┘
       │
       ├─► [Admin Onayı] ──► Katana (Stok Ekleme)
       │
       └─► [Kozaya Senkronize] ──► Luca (Fatura)
```

### Satınalma Siparişi Akışı

```
┌─────────────┐
│   Manuel    │
│  Oluşturma  │
└──────┬──────┘
       │
       ▼
┌─────────────────────────┐
│  PurchaseOrders (DB)    │
└──────┬──────────────────┘
       │
       ▼
┌─────────────────────────┐
│   Admin Panel           │
│   (Satınalma)           │
└──────┬──────────────────┘
       │
       ├─► [Durum: Approved] ──► Katana (Ürün Ekleme)
       │
       ├─► [Durum: Received] ──► StockMovement (DB)
       │
       └─► [Kozaya Senkronize] ──► Luca (Fatura)
```

---

## 📡 API Endpoint'leri

### Satış Siparişleri

| Method | Endpoint                             | Açıklama                 | Yetki          |
| ------ | ------------------------------------ | ------------------------ | -------------- |
| GET    | `/api/sales-orders`                  | Tüm siparişleri listele  | -              |
| GET    | `/api/sales-orders/{id}`             | Sipariş detayı           | -              |
| GET    | `/api/sales-orders/stats`            | İstatistikler            | -              |
| GET    | `/api/sales-orders/{id}/sync-status` | Senkronizasyon durumu    | -              |
| POST   | `/api/sales-orders/{id}/approve`     | ✅ Admin onayı           | Admin, Manager |
| POST   | `/api/sales-orders/{id}/sync`        | 🔄 Kozaya senkronize     | Admin          |
| POST   | `/api/sales-orders/sync-all`         | Toplu senkronizasyon     | Admin          |
| PATCH  | `/api/sales-orders/{id}/luca-fields` | Luca alanlarını güncelle | Admin          |

### Satınalma Siparişleri

| Method | Endpoint                                | Açıklama                 | Yetki |
| ------ | --------------------------------------- | ------------------------ | ----- |
| GET    | `/api/purchase-orders`                  | Tüm siparişleri listele  | -     |
| GET    | `/api/purchase-orders/{id}`             | Sipariş detayı           | -     |
| GET    | `/api/purchase-orders/stats`            | İstatistikler            | -     |
| GET    | `/api/purchase-orders/{id}/sync-status` | Senkronizasyon durumu    | -     |
| POST   | `/api/purchase-orders`                  | Yeni sipariş oluştur     | -     |
| POST   | `/api/purchase-orders/{id}/sync`        | 🔄 Kozaya senkronize     | -     |
| POST   | `/api/purchase-orders/sync-all`         | Toplu senkronizasyon     | -     |
| POST   | `/api/purchase-orders/retry-failed`     | Hatalıları yeniden dene  | -     |
| PATCH  | `/api/purchase-orders/{id}/status`      | ✅ Durum güncelle        | -     |
| PATCH  | `/api/purchase-orders/{id}/luca-fields` | Luca alanlarını güncelle | -     |

---

## ⚠️ Hata Yönetimi

### Satış Siparişleri

**Onay Hataları**:

```json
{
  "success": false,
  "message": "Sipariş satırları bulunamadı. Katana'dan tekrar senkronize edin.",
  "orderNo": "SO-12345"
}
```

**Senkronizasyon Hataları**:

- Müşteri bilgisi eksik
- Sipariş satırları yok
- Luca API hatası
- Zaten senkronize edilmiş

**Hata Kaydı**:

- `LastSyncError` alanına yazılır
- `IsSyncedToLuca = false` set edilir
- `LastSyncAt` güncellenir

### Satınalma Siparişleri

**Durum Geçiş Hataları**:

```json
{
  "message": "Geçersiz durum değişikliği: Pending -> Received"
}
```

**Senkronizasyon Hataları**:

- Tedarikçi bilgisi eksik
- Luca API hatası
- Session timeout

**Retry Mekanizması**:

- `SyncRetryCount` sayacı
- `retry-failed` endpoint ile manuel retry
- Maksimum 3 deneme

---

## 🔐 Güvenlik ve Yetkilendirme

### Rol Bazlı Erişim

**Admin Rolü**:

- Tüm işlemler
- Toplu senkronizasyon
- Luca alan güncellemeleri

**Manager Rolü**:

- Sipariş onaylama
- Durum güncelleme
- Görüntüleme

**Anonim Erişim**:

- Listeleme (GET)
- Detay görüntüleme (GET)
- İstatistikler (GET)

### Audit Trail

Tüm kritik işlemler loglanır:

```csharp
_auditService.LogUpdate(
    "SalesOrder",
    id.ToString(),
    User.Identity?.Name ?? "System",
    null,
    "Sipariş onaylandı ve Katana'ya X ürün eklendi"
);
```

---

## 📊 Performance Optimizasyonları

### Paralel İşleme

- **Satış Siparişleri**: 5 eşzamanlı istek
- **Satınalma Siparişleri**: 5 eşzamanlı istek (sync-all), 3 (retry-failed)

### Batch Processing

- Worker: 100 sipariş/batch
- Memory-efficient processing
- GC optimization

### Caching

- Stats endpoint: 1 dakika cache
- Duplicate prevention: HashSet kullanımı

### Metrics

```csharp
{
  "durationMs": 12500,
  "rateOrdersPerMinute": 230.4,
  "successCount": 48,
  "failCount": 2
}
```

---

## 🎯 Özet

### Satış Siparişleri

1. **Kaynak**: Katana (otomatik senkronizasyon)
2. **Admin Onayı**: Katana'ya stok ekleme
3. **Kozaya Senkronize**: Luca'ya fatura gönderme

### Satınalma Siparişleri

1. **Kaynak**: Manuel oluşturma
2. **Durum Yönetimi**: Pending → Approved → Received
3. **Approved**: Katana'ya ürün ekleme (arka planda)
4. **Kozaya Senkronize**: Luca'ya fatura gönderme

### Kritik Noktalar

- ✅ Onay işlemleri geri alınamaz
- 🔄 Senkronizasyon duplicate-safe
- ⚡ Paralel işleme ile yüksek performans
- 📊 Detaylı hata raporlama ve retry mekanizması
- 🔐 Rol bazlı yetkilendirme
- 📝 Tam audit trail

---

**Son Güncelleme**: 2024-01-15
**Versiyon**: 1.0
