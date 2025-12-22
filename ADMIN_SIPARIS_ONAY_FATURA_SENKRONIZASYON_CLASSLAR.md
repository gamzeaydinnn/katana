# Admin Sipariş Onayı, Faturalar ve Senkronizasyon - Tüm Classes ve Metotlar

## 📋 İçindekiler
1. [SalesOrdersController - Satış Siparişleri](#1-salesorderscontroller)
2. [PurchaseOrdersController - Satınalma Siparişleri](#2-purchaseorderscontroller)
3. [OrderInvoiceSyncService - Fatura Senkronizasyon Servisi](#3-orderinvoicesyncsservice)
4. [AdminService - Admin Paneli Servisi](#4-adminservice)

---

## 1. SalesOrdersController

**Dosya**: `src/Katana.API/Controllers/SalesOrdersController.cs`  
**Amaç**: Katana'dan senkronize edilen satış siparişlerinin yönetimi, onayı ve Luca'ya senkronizasyonu

### Class Özellikleri
```csharp
[Authorize]
[ApiController]
[Route("api/sales-orders")]
public class SalesOrdersController : ControllerBase
```

### Dependencies (Bağımlılıklar)
- `IntegrationDbContext` - Veritabanı konteksti
- `ILucaService` - Luca entegrasyon servisi
- `ILoggingService` - Logging servisi
- `IAuditService` - Audit log servisi
- `IKatanaService` - Katana ERP entegrasyonu
- `ILocationMappingService` - Depo kodu eşleştirme servisi
- `ILogger<SalesOrdersController>` - İç logging

---

### Metotlar

#### 1.1 GetAll()
```csharp
[HttpGet]
public async Task<ActionResult<IEnumerable<SalesOrderSummaryDto>>> GetAll(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 50,
    [FromQuery] string? status = null,
    [FromQuery] string? syncStatus = null)
```

**Açıklama**: Tüm satış siparişlerini listele  
**Yetki**: Authorized  
**Parametreler**:
- `page`: Sayfa numarası (default: 1)
- `pageSize`: Sayfa başına kayıt sayısı (default: 50)
- `status`: Sipariş durumu filtresi (opsiyonel)
- `syncStatus`: Senkronizasyon durumu filtresi
  - `"synced"`: Senkronize edilmiş ve hatasız
  - `"error"`: Senkronizasyon hatalı
  - `"not_synced"`: Senkronize edilmemiş

**Dönüş**: Sayfalı liste (SalesOrderSummaryDto)

---

#### 1.2 GetById()
```csharp
[HttpGet("{id}")]
public async Task<ActionResult<LocalSalesOrderDto>> GetById(int id)
```

**Açıklama**: Satış siparişi detayını getir  
**Yetki**: Authorized  
**Parametreler**:
- `id`: Sipariş ID'si

**Dönüş**: Sipariş detayı (LocalSalesOrderDto)

---

#### 1.3 UpdateLucaFields()
```csharp
[HttpPatch("{id}/luca-fields")]
[Authorize(Roles = "Admin")]
public async Task<ActionResult<LocalSalesOrderDto>> UpdateLucaFields(
    int id, 
    [FromBody] UpdateSalesOrderLucaFieldsDto dto)
```

**Açıklama**: Luca alanlarını güncelle  
**Yetki**: Admin  
**Parametreler**:
- `id`: Sipariş ID'si
- `dto`: Güncellenecek Luca alanları
  - `BelgeSeri`: Belge serisi
  - `BelgeNo`: Belge numarası
  - `DuzenlemeSaati`: Düzenleme saati
  - `BelgeTurDetayId`: Belge türü detay ID
  - `NakliyeBedeliTuru`: Nakliye bedeli türü
  - `TeklifSiparisTur`: Teklif/Sipariş türü
  - `OnayFlag`: Onay bayrağı
  - `BelgeAciklama`: Belge açıklaması

**Dönüş**: Güncellenmiş sipariş detayı

---

#### 1.4 SyncToLuca()
```csharp
[HttpPost("{id}/sync")]
[Authorize(Roles = "Admin")]
public async Task<ActionResult<SalesOrderSyncResultDto>> SyncToLuca(
    int id,
    [FromBody] UpdateSalesOrderLucaFieldsDto? lucaFields = null)
```

**Açıklama**: Siparişi Luca'ya manuel senkronize et (fatura olarak)  
**Yetki**: Admin  
**Parametreler**:
- `id`: Sipariş ID'si
- `lucaFields`: Senkronizasyon sırasında güncellenecek Luca alanları (opsiyonel)

**İşlem Adımları**:
1. Sipariş ve müşteri bilgisini getir
2. **Müşteri Validasyonu**:
   - Vergi No veya Luca Cari Kodu gerekli
   - Vergi No formatı kontrolü (10 veya 11 hane)
   - Müşteri adı kontrolü
3. **Depo Kodu Eşleştirme**: LocationId'den depo kodu çözümle
4. **Duplikasyon Kontrolü**: Zaten senkronize edilmişse hata dönüş
5. **Döviz Kuru Validasyonu**: Dövizli siparişlerde kur gerekli
6. **Luca Request Hazırlama**: Sipariş verilerini Luca formatına dönüştür
7. **Luca API Çağrısı**: CreateSalesOrderInvoiceAsync() ile gönder
8. **Response İşleme**:
   - Başarılı: IsSyncedToLuca=true, LucaOrderId kaydedilir
   - Başarısız: LastSyncError kaydedilir

**Dönüş**: SalesOrderSyncResultDto

**Hata Senaryoları**:
- ❌ Müşteri bilgisi eksik
- ❌ Sipariş satırları yok
- ❌ Vergi No/Cari kod geçersiz
- ❌ Luca API hatası
- ❌ Zaten senkronize edilmiş

---

#### 1.5 GetSyncStatus()
```csharp
[HttpGet("{id}/sync-status")]
public async Task<ActionResult<SalesOrderSyncStatusDto>> GetSyncStatus(int id)
```

**Açıklama**: Senkronizasyon durumunu getir  
**Yetki**: Authorized  
**Parametreler**:
- `id`: Sipariş ID'si

**Dönüş**: 
```csharp
{
  "salesOrderId": int,
  "lucaOrderId": int?,
  "isSyncedToLuca": bool,
  "lastSyncAt": DateTime?,
  "lastSyncError": string?,
  "status": "synced" | "error" | "not_synced"
}
```

---

#### 1.6 SyncAllPending()
```csharp
[HttpPost("sync-all")]
[Authorize(Roles = "Admin")]
public async Task<ActionResult<object>> SyncAllPending([FromQuery] int maxCount = 50)
```

**Açıklama**: Toplu senkronizasyon - senkronize edilmemiş tüm siparişleri Luca'ya gönder  
**Yetki**: Admin  
**Parametreler**:
- `maxCount`: Maximum kaç sipariş işlenecek (default: 50)

**Özellikler**:
- ⚡ **Paralel işleme**: 5 eşzamanlı istek
- 🎯 **Hedef**: `IsSyncedToLuca=false` ve `LastSyncError=null` olan siparişler
- 📊 **Performance metrics**: İşlem süresi ve hız raporu

**İşlem Akışı**:
1. Bekleyen siparişleri çek (senkronize edilmemiş + hatasız)
2. Paralel batch processing (5 concurrent)
3. Her sipariş için SyncToLuca() çağrısı
4. Sonuçları topla ve raporla

**Dönüş**:
```json
{
  "totalProcessed": 50,
  "successCount": 48,
  "failCount": 2,
  "durationMs": 12500,
  "rateOrdersPerMinute": 230.4,
  "errors": [
    {
      "orderId": 123,
      "orderNo": "SO-12345",
      "error": "Hata açıklaması"
    }
  ]
}
```

---

#### 1.7 GetStats()
```csharp
[HttpGet("stats")]
public async Task<ActionResult<object>> GetStats()
```

**Açıklama**: Sipariş istatistikleri  
**Yetki**: Public  
**Dönüş**:
```json
{
  "totalOrders": 150,
  "syncedOrders": 140,
  "errorOrders": 5,
  "pendingOrders": 5,
  "totalValue": 50000.00
}
```

---

#### 1.8 ApproveOrder()
```csharp
[HttpPost("{id}/approve")]
[Authorize(Roles = "Admin,Manager")]
public async Task<ActionResult> ApproveOrder(int id)
```

**Açıklama**: Admin onayı - Siparişi onayla ve Katana'da stok artırımı yap  
**Yetki**: Admin, Manager  
**Parametreler**:
- `id`: Sipariş ID'si

**İşlem Adımları**:

1. **Sipariş Validasyonu**:
   - Sipariş var mı?
   - Zaten onaylanmış mı? (Status: APPROVED veya SHIPPED)
   - Sipariş satırları var mı?
   - Geçerli satırlar var mı? (SKU ve Quantity dolu)

2. **Katana'ya Stok Ekleme/Güncelleme** (Her satır için):
   ```
   a. SyncProductStockAsync() çağrı
   b. Ürün var mı kontrol
   c. Ürün VARSA: Mevcut stok + sipariş miktarı = yeni stok
   d. Ürün YOKSA: Yeni ürün oluştur ve stok set et
   ```
   
   - Sonuç: ✅ Stok güncellendi / ❌ Hata

3. **Sipariş Durumu Güncelleme**:
   - Tüm kalemler başarılı: Status = "APPROVED"
   - Bazı kalemler hatalı: Status = "APPROVED_WITH_ERRORS"

4. **Luca'ya Senkronizasyon** (Opsiyonel, koşullu):
   - Şart 1: Katana stok güncellemesi tamamen başarılı olmalı
   - Şart 2: Müşteri bilgisi tam olmalı
   - Şart 3: Müşteri validasyonu geçmeli
   
   Eğer şartlar karşılanırsa: CreateSalesOrderInvoiceAsync() çağrısı

5. **Audit Log ve Bildirim**:
   - AuditService.LogUpdate()
   - LoggingService.LogInfo()

**Dönüş**:
```json
{
  "success": true,
  "message": "Sipariş onaylandı",
  "orderNo": "SO-12345",
  "orderStatus": "APPROVED",
  "katanaOrderId": "KAT-001234",
  "successCount": 5,
  "failCount": 0,
  "syncResults": [
    {
      "sku": "SKU-001",
      "quantity": 10,
      "success": true,
      "error": null
    }
  ],
  "lucaSync": {
    "attempted": true,
    "isSuccess": true,
    "lucaOrderId": 5678,
    "message": "Luca'ya başarıyla senkronize edildi",
    "errorDetails": null
  }
}
```

**Hata Senaryoları**:
- ❌ Sipariş bulunamadı
- ❌ Sipariş zaten onaylanmış
- ❌ Sipariş satırları bulunamadı
- ❌ Katana stok güncellemesi başarısız
- ❌ Müşteri validasyonu başarısız

**Önemli Notlar**:
- ⚠️ Onay işlemi **geri alınamaz**
- ✅ Katana'ya stok ekleme **senkron** yapılır
- 🔄 Her kalem için ayrı API çağrısı yapılır
- 📡 Luca çağrısı DB retry stratejisinin dışında kalır (duplicate fatura önlemek için)

---

#### 1.9 ClearApprovedErrors()
```csharp
[HttpPost("clear-errors")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> ClearApprovedErrors()
```

**Açıklama**: APPROVED_WITH_ERRORS durumundaki siparişlerin durumunu temizle  
**Yetki**: Admin  
**Amaç**: Charset sorunu düzeltildikten sonra eski hataları temizlemek

**İşlem**:
1. `Status = "APPROVED_WITH_ERRORS"` olan siparişleri bul
2. Status'ü "APPROVED"'e değiştir
3. LastSyncError'ı null'la

**Dönüş**:
```json
{
  "success": true,
  "message": "5 siparişin hata durumu temizlendi.",
  "clearedCount": 5
}
```

---

## 2. PurchaseOrdersController

**Dosya**: `src/Katana.API/Controllers/PurchaseOrdersController.cs`  
**Amaç**: Satınalma siparişlerinin yönetimi, onayı, stok alımı ve Luca'ya senkronizasyonu

### Class Özellikleri
```csharp
[Authorize]
[ApiController]
[Route("api/purchase-orders")]
public class PurchaseOrdersController : ControllerBase
```

### Dependencies
- `IntegrationDbContext` - Veritabanı konteksti
- `ILucaService` - Luca entegrasyon servisi
- `ILoggingService` - Logging servisi
- `IAuditService` - Audit log servisi
- `IMemoryCache` - Cache servisi (istatistikler)
- `IHubContext<NotificationHub>` - SignalR bildirimleri
- `IKatanaService` - Katana ERP entegrasyonu
- `ISupplierService` - Tedarikçi servisi

---

### Metotlar

#### 2.1 GetAll()
```csharp
[HttpGet]
[AllowAnonymous]
public async Task<ActionResult<IEnumerable<PurchaseOrderListDto>>> GetAll(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 50,
    [FromQuery] string? status = null,
    [FromQuery] string? syncStatus = null,
    [FromQuery] string? search = null)
```

**Açıklama**: Tüm satınalma siparişlerini listele  
**Yetki**: Public  
**Parametreler**:
- `page`: Sayfa numarası (default: 1)
- `pageSize`: Sayfa başına kayıt sayısı (default: 50)
- `status`: Durum filtresi (Pending, Approved, Received, Cancelled)
- `syncStatus`: Senkronizasyon durumu filtresi
  - `"synced"`: Luca'ya senkronize edilmiş
  - `"error"`: Senkronizasyon hatası var
  - `"not_synced"`: Senkronize edilmemiş
- `search`: Arama (OrderNo veya Supplier Name)

**Dönüş**: Sayfalı liste + pagination bilgisi + uyarılar

**Özellik**: LEFT JOIN kullanarak tedarikçi olmayan siparişleri de gösterir

---

#### 2.2 GetById()
```csharp
[HttpGet("{id}")]
[AllowAnonymous]
public async Task<ActionResult<PurchaseOrderDetailDto>> GetById(int id)
```

**Açıklama**: Satınalma siparişi detayını getir  
**Yetki**: Public  
**Parametreler**:
- `id`: Sipariş ID'si

**Dönüş**: Sipariş detayı (PurchaseOrderDetailDto)

---

#### 2.3 Create()
```csharp
[HttpPost]
[AllowAnonymous]
public async Task<ActionResult<PurchaseOrderDetailDto>> Create(
    [FromBody] CreatePurchaseOrderRequest request)
```

**Açıklama**: Yeni satınalma siparişi oluştur  
**Yetki**: Public  
**Parametreler**:
- `request`:
  - `SupplierId`: Tedarikçi ID (zorunlu)
  - `OrderDate`: Sipariş tarihi
  - `ExpectedDate`: Beklenen teslim tarihi
  - `Items`: Sipariş kalemleri
    - `ProductId`: Ürün ID
    - `Quantity`: Miktar
    - `UnitPrice`: Birim fiyat
    - `LucaStockCode`: Luca stok kodu (opsiyonel)
    - `WarehouseCode`: Depo kodu
    - `VatRate`: KDV oranı (default: 20)
    - `UnitCode`: Birim kodu (default: AD)
    - `DiscountAmount`: İndirim (opsiyonel)

**İşlem**:
1. Tedarikçi validasyonu
2. Sipariş numarası oluştur (PO-YYYYMMDD-XXXXXXXX)
3. Her kalem için ürün kontrolü
4. Toplam tutarı hesapla
5. Sipariş ve kalemleri kaydet

**Dönüş**: Oluşturulmuş sipariş detayı

---

#### 2.4 UpdateLucaFields()
```csharp
[HttpPatch("{id}/luca-fields")]
public async Task<ActionResult> UpdateLucaFields(
    int id, 
    [FromBody] UpdatePurchaseOrderLucaFieldsRequest request)
```

**Açıklama**: Satınalma siparişi Luca alanlarını güncelle  
**Parametreler**:
- `id`: Sipariş ID'si
- `request`:
  - `DocumentSeries`: Belge serisi
  - `DocumentTypeDetailId`: Belge türü detay ID
  - `VatIncluded`: KDV dahil mi?
  - `ReferenceCode`: Referans kodu
  - `ProjectCode`: Proje kodu
  - `Description`: Açıklama
  - `ShippingAddressId`: Kargo adresi ID

**Dönüş**: Başarı mesajı

---

#### 2.5 SyncToLuca()
```csharp
[HttpPost("{id}/sync")]
public async Task<ActionResult<PurchaseOrderSyncResultDto>> SyncToLuca(int id)
```

**Açıklama**: Tek satınalma siparişini Luca'ya fatura olarak senkronize et  
**Parametreler**:
- `id`: Sipariş ID'si

**İşlem Adımları**:
1. **Sipariş Validasyonu**:
   - Sipariş var mı?
   - Tedarikçi bilgisi var mı?

2. **Luca Fatura Request Hazırlama**:
   - Satınalma siparişi FATURA olarak gönderilir (alım faturası)
   - MappingHelper.MapToLucaInvoiceFromPurchaseOrder()

3. **Luca API Çağrısı**:
   - SendInvoiceAsync() ile gönder
   - Session yenileme otomatik

4. **Response İşleme**:
   - **Başarılı**:
     - `IsSyncedToLuca = true`
     - `LastSyncAt = DateTime.UtcNow`
     - `LastSyncError = null`
     - `SyncRetryCount = 0`
   
   - **Başarısız**:
     - `LastSyncError = hata mesajı`
     - `SyncRetryCount++`

5. **Audit Log**: Başarılı senkronizasyon loglanır

**Dönüş**:
```json
{
  "success": true,
  "lucaPurchaseOrderId": null,
  "lucaDocumentNo": "PO-20240115-ABC123",
  "message": "Fatura başarıyla Luca'ya aktarıldı"
}
```

---

#### 2.6 GetSyncStatus()
```csharp
[HttpGet("{id}/sync-status")]
public async Task<ActionResult> GetSyncStatus(int id)
```

**Açıklama**: Senkronizasyon durumunu sorgula  
**Parametreler**:
- `id`: Sipariş ID'si

**Dönüş**:
```json
{
  "id": 1,
  "orderNo": "PO-12345",
  "isSyncedToLuca": true,
  "lucaPurchaseOrderId": null,
  "lucaDocumentNo": "FTR-001",
  "lastSyncAt": "2024-01-15T10:30:00Z",
  "lastSyncError": null,
  "syncRetryCount": 0
}
```

---

#### 2.7 SyncAll()
```csharp
[HttpPost("sync-all")]
public async Task<ActionResult> SyncAll([FromQuery] int maxCount = 50)
```

**Açıklama**: Bekleyen tüm satınalma siparişlerini senkronize et  
**Parametreler**:
- `maxCount`: Maximum kaç sipariş işlenecek (default: 50)

**Özellikler**:
- ⚡ **Paralel işleme**: 5 eşzamanlı istek
- 🎯 **Hedef**: `IsSyncedToLuca=false` ve `LastSyncError=null` olan siparişler
- 📊 **Performance metrics**: İşlem süresi ve hız raporu

**Dönüş**:
```json
{
  "message": "50 sipariş işlendi",
  "totalProcessed": 50,
  "successCount": 48,
  "failCount": 2,
  "durationMs": 12500,
  "rateOrdersPerMinute": 230.4,
  "results": [...]
}
```

---

#### 2.8 RetryFailed()
```csharp
[HttpPost("retry-failed")]
public async Task<ActionResult> RetryFailed([FromQuery] int maxRetries = 3)
```

**Açıklama**: Hatalı siparişleri yeniden dene  
**Parametreler**:
- `maxRetries`: Maximum retry sayısı (default: 3)

**Logik**:
1. `IsSyncedToLuca=false` ve `LastSyncError!=null` olan siparişleri bul
2. `SyncRetryCount < maxRetries` kontrol et
3. En düşük retry count'lu siparişlerden başla
4. Paralel işle (3 concurrent)

**Dönüş**: SyncAll() gibi rapor

---

#### 2.9 GetStats()
```csharp
[HttpGet("stats")]
public async Task<ActionResult> GetStats()
```

**Açıklama**: Satınalma siparişi istatistikleri  
**Yetki**: Public  
**Cache**: 1 dakika

**Dönüş**:
```json
{
  "total": 100,
  "synced": 85,
  "notSynced": 10,
  "withErrors": 5,
  "pending": 30,
  "approved": 50,
  "received": 15,
  "cancelled": 5
}
```

---

#### 2.10 UpdateStatus()
```csharp
[HttpPatch("{id}/status")]
public async Task<ActionResult> UpdateStatus(
    int id, 
    [FromBody] UpdatePurchaseOrderStatusRequest request)
```

**Açıklama**: Sipariş durumunu güncelle (Pending → Approved → Received)  
**Parametreler**:
- `id`: Sipariş ID'si
- `request.NewStatus`: Yeni durum

**Durum Geçişleri**:
```
Pending  →  Approved  →  Received
         ↘  Cancelled
```

**Kritik**: "Approved" durumuna geçildiğinde:
1. Katana'ya ürünler eklenir/güncellenir (arka planda)
2. Her kalem için:
   - Katana'da ürün var mı kontrol
   - Varsa: Stok artır
   - Yoksa: Yeni ürün oluştur ve stok set et

**İşlem**:
```csharp
_ = Task.Run(async () =>
{
    await Task.Delay(1000); // DB commit olsun
    foreach (var item in order.Items)
    {
        var existingProduct = await _katanaService.GetProductBySkuAsync(item.Product.SKU);
        if (existingProduct != null)
            await _katanaService.UpdateProductAsync(...);
        else
            await _katanaService.CreateProductAsync(...);
    }
});
```

**Dönüş**: Başarı mesajı

---

## 3. OrderInvoiceSyncService

**Dosya**: `src/Katana.Business/Services/OrderInvoiceSyncService.cs`  
**Amaç**: Katana satış ve satınalma siparişlerini Luca'ya fatura olarak senkronize etmek (tam entegrasyon)

### Class Özellikleri
```csharp
public class OrderInvoiceSyncService : IOrderInvoiceSyncService
```

### Özellikler (Features)
- 🔄 **Akış**: Order → LucaInvoice mapping → Luca API gönderimi → Fatura ID kaydı
- 💾 **Veri Yönetimi**: Mapping tablosunda belge bilgisini kalıcı tutma
- 🔌 **Resilience**: Circuit Breaker + Retry Pattern
- 📡 **Event Publishing**: InvoiceSyncedEvent yayınlanması

### Resilience Patterns

#### Circuit Breaker
```csharp
private static readonly AsyncCircuitBreakerPolicy _lucaCircuitBreaker = Policy
    .Handle<HttpRequestException>()
    .Or<TimeoutException>()
    .CircuitBreakerAsync(
        exceptionsAllowedBeforeBreaking: 5,        // 5 hata sonrası aç
        durationOfBreak: TimeSpan.FromMinutes(2),  // 2 dakika aç tut
        onBreak: (ex, duration) => ...,
        onReset: () => ...,
        onHalfOpen: () => ...);
```

**Durumlar**:
- **CLOSED**: Normal (istekler geçer)
- **OPEN**: API down (istekler hemen fail)
- **HALF-OPEN**: Recovery testi yapılıyor

#### Retry Policy
```csharp
private static readonly AsyncRetryPolicy _lucaSyncRetryPolicy = Policy
    .Handle<HttpRequestException>()
    .Or<TimeoutException>()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
        // Exponential backoff: 2s, 4s, 8s
```

### Dependencies
- `IntegrationDbContext` - Veritabanı
- `ILucaService` - Luca API servisi
- `IOrderMappingRepository` - Mapping bilgisi (belgeSeri, belgeNo, etc.)
- `IAuditService` - Audit logging
- `IEventPublisher` - Event yayınlama
- `LucaApiSettings` - Konfigürasyon

---

### Metotlar

#### 3.1 SyncSalesOrderToLucaAsync()
```csharp
public async Task<OrderSyncResultDto> SyncSalesOrderToLucaAsync(int orderId)
```

**Açıklama**: Katana Sales Order'ı Luca'ya Satış Faturası olarak gönder  
**Parametreler**:
- `orderId`: Sales Order ID

**İşlem Adımları**:

1. **Sales Order Kontrolü**:
   - Sipariş var mı?
   - Daha önce gönderilmiş mi? (LucaInvoiceId kontrol)

2. **Luca Request Oluşturma** (BuildSalesInvoiceRequestFromSalesOrderAsync):
   - Belge alanları (BelgeSeri, BelgeNo, belgeTakipNo)
   - Müşteri bilgisi (CariKodu, CariAd, CariSoyad)
   - Vergi No validasyonu
   - CariTip hesaplaması (11 haneli=şahıs, 10 haneli=firma)
   - Döviz bilgisi

3. **Circuit Breaker Kontrolü**:
   - OPEN durumda olmasını kontrol
   - OPEN ise hızlı fail dönüş

4. **Luca API Çağrısı** (Circuit Breaker + Retry ile):
   - CreateInvoiceRawAsync() çağrı
   - Exponential backoff retry (max 3 kere)

5. **Response Parsing**:
   - **Hata Kodları**:
     - `code=1001 veya 1002`: Login gerekli (Session sorunu)
   - **Başarı**:
     - `basarili=true` ve `ssFaturaBaslikId` var
   
6. **Mapping Kaydı**:
   - Luca Fatura ID'sini SaveLucaInvoiceIdAsync() ile kaydet
   - SalesOrder'ı IsSyncedToLuca=true işaretle

7. **Event Publishing**:
   - InvoiceSyncedEvent yayınla
   - Hata alınsa loglayıp devam et

**Dönüş**: OrderSyncResultDto

---

#### 3.2 BuildSalesInvoiceRequestFromSalesOrderAsync()
```csharp
private async Task<LucaCreateInvoiceHeaderRequest?> 
    BuildSalesInvoiceRequestFromSalesOrderAsync(SalesOrder order)
```

**Açıklama**: SalesOrder'ı Luca fatura formatına dönüştür  
**Dönüş**: LucaCreateInvoiceHeaderRequest

**Dönüştürülen Alanlar**:

| Katana | → | Luca | Açıklama |
|--------|---|------|----------|
| OrderNo | → | BelgeTakipNo | Sipariş numarası |
| Customer.Title / ContactPerson | → | CariAd / CariSoyad | Müşteri adı-soyadı |
| Customer.TaxNo | → | VergiNo | Vergi No (10-11 haneli) |
| Customer.LucaCode | → | CariKodu | Cari kodu |
| OrderCreatedDate | → | BelgeTarihi | Tarih |
| Currency | → | ParaBirimKod | Para birimi |
| ConversionRate | → | KurBedeli | Döviz kuru |
| TotalAmount | → | Toplam | Tutar |

**Validasyonlar**:

1. **Cari Kodu Validasyonu**:
   - ✅ Geçerli mu?
   - ❌ "CUST..." ile başlıyor mu?

2. **VergiNo Validasyonu**:
   - 10 hane (VKN) → CariTip=1 (Firma)
   - 11 hane (TCKN) → CariTip=2 (Şahıs)
   - Boş → Fallback: cariKodu'dan rakamları çıkar veya "11111111111" kullan

3. **CariAd / CariSoyad Ayırma**:
   - Birden fazla kelime: Son kelime=CariSoyad, kalan=CariAd
   - Tek kelime: CariAd=kelime, CariSoyad="UNKNOWN"
   - Boş: CariAd="Unknown Customer (KatanaID)", CariSoyad="UNKNOWN"

4. **Belge No Fallback Akışı**:
   - Mapping tablosundan var mı?
   - OrderNo'dan kullan
   - Son 9 haneli sayı çıkar
   - Fallback: 1000000 + OrderId

5. **Döviz Kuru Kontrolü**:
   - TRY → KurBedeli=1.0
   - Diğer → KurBedeli=ConversionRate (default 1)

**Dönüştürülen Request Alanları**:
- `BelgeSeri`: Belge serisi
- `BelgeNo`: Belge numarası (string)
- `BelgeTarihi`: dd/MM/yyyy formatı
- `VadeTarihi`: Vadesi (tarih+30gün)
- `BelgeAciklama`: "Katana Sales Order #SO-12345"
- `BelgeTurDetayId`: Satış faturası türü
- `ParaBirimKod`: TRY, USD, EUR, vb.
- `KurBedeli`: Döviz kuru
- `KdvFlag`: false (KDV detaylarında)
- `MusteriTedarikci`: "1" (müşteri)
- `CariKodu`: Müşteri kodu
- `CariAd`: Müşteri adı
- `CariSoyad`: Müşteri soyadı
- `VergiNo`: Vergi No
- `CariTip`: 1 (firma) veya 2 (şahıs)

---

#### 3.3 SyncPurchaseOrderToLucaAsync()
```csharp
public async Task<OrderSyncResultDto> SyncPurchaseOrderToLucaAsync(int orderId)
```

**Açıklama**: Katana Purchase Order'ı Luca'ya Alım Faturası olarak gönder  
**Fark**: Müşteri yerine Tedarikçi, Satış Faturası yerine Alım Faturası

---

#### 3.4 LucaCircuitState Property
```csharp
public static CircuitState LucaCircuitState => _lucaCircuitBreaker.CircuitState;
```

**Açıklama**: Circuit Breaker durumunu kontrol et  
**Kullanım**: Admin panelinde "Luca API Durumu" göstermek için

---

### Sabitler (Constants)

```csharp
private const int LUCA_SATIS_FATURASI = 18;      // Satış Faturası
private const int LUCA_ALIM_FATURASI = 16;       // Alım Faturası
private const int MUSTERI = 1;                   // Müşteri
private const int TEDARIKCI = 2;                 // Tedarikçi
private const int MAL_HIZMET = 1;                // Mal/Hizmet faturası
private const int STOK_KARTI = 1;                // Stok kartı türü
```

---

## 4. AdminService

**Dosya**: `src/Katana.Business/Services/AdminService.cs`  
**Amaç**: Admin paneli için özet raporlar ve senkronizasyon durumu takibi

### Class Özellikleri
```csharp
public class AdminService : IAdminService
```

### Dependencies
- `IntegrationDbContext` - Veritabanı
- `IKatanaService` - Katana API
- `ILucaService` - Luca API
- `ISyncService` - Senkronizasyon servisi

---

### Metotlar

#### 4.1 GetSyncStatusesAsync()
```csharp
public async Task<List<AdminSyncStatusDto>> GetSyncStatusesAsync()
```

**Açıklama**: Tüm senkronizasyon türlerinin durumunu getir  
**Dönüş**:
```csharp
List<AdminSyncStatusDto>
{
    IntegrationName: "STOCK" | "INVOICE" | "CUSTOMER",
    LastSyncDate: DateTime?,
    Status: "SUCCESS" | "FAILED" | "PENDING" | "Unknown"
}
```

**Senkronizasyon Türleri**:
- **STOCK**: Stok senkronizasyonu
- **INVOICE**: Fatura senkronizasyonu
- **CUSTOMER**: Müşteri senkronizasyonu

---

#### 4.2 GetErrorLogsAsync()
```csharp
public async Task<List<ErrorLogDto>> GetErrorLogsAsync(int page = 1, int pageSize = 50)
```

**Açıklama**: Hata loglarını sayfalı getir  
**Parametreler**:
- `page`: Sayfa numarası
- `pageSize`: Sayfa başına kayıt sayısı

**Dönüş**:
```csharp
List<ErrorLogDto>
{
    Id: int,
    IntegrationName: string,
    Message: string,
    CreatedAt: DateTime
}
```

---

#### 4.3 GetSyncReportAsync()
```csharp
public async Task<SyncReportDto> GetSyncReportAsync(string integrationName)
```

**Açıklama**: Belirli bir senkronizasyon türü için rapor  
**Parametreler**:
- `integrationName`: "STOCK", "INVOICE" veya "CUSTOMER"

**Dönüş**:
```csharp
{
    IntegrationName: string,
    TotalRecords: int,
    SuccessCount: int,
    FailedCount: int,
    ReportDate: DateTime
}
```

---

## 📊 API Endpoint Özeti

### Satış Siparişleri (Sales Orders)

| Method | Endpoint | Yetki | Açıklama |
|--------|----------|-------|----------|
| GET | `/api/sales-orders` | Auth | Tüm siparişleri listele |
| GET | `/api/sales-orders/{id}` | Auth | Sipariş detayı |
| GET | `/api/sales-orders/{id}/sync-status` | Auth | Senkronizasyon durumu |
| GET | `/api/sales-orders/stats` | Public | İstatistikler |
| PATCH | `/api/sales-orders/{id}/luca-fields` | Admin | Luca alanlarını güncelle |
| POST | `/api/sales-orders/{id}/sync` | Admin | Manuel senkronizasyon |
| POST | `/api/sales-orders/{id}/approve` | Admin, Mgr | Admin onayı |
| POST | `/api/sales-orders/sync-all` | Admin | Toplu senkronizasyon |
| POST | `/api/sales-orders/clear-errors` | Admin | Hata durumunu temizle |

### Satınalma Siparişleri (Purchase Orders)

| Method | Endpoint | Yetki | Açıklama |
|--------|----------|-------|----------|
| GET | `/api/purchase-orders` | Public | Tüm siparişleri listele |
| GET | `/api/purchase-orders/{id}` | Public | Sipariş detayı |
| GET | `/api/purchase-orders/{id}/sync-status` | Public | Senkronizasyon durumu |
| GET | `/api/purchase-orders/stats` | Public | İstatistikler |
| POST | `/api/purchase-orders` | Public | Yeni sipariş oluştur |
| PATCH | `/api/purchase-orders/{id}/luca-fields` | - | Luca alanlarını güncelle |
| PATCH | `/api/purchase-orders/{id}/status` | - | Durum güncelle |
| POST | `/api/purchase-orders/{id}/sync` | - | Manuel senkronizasyon |
| POST | `/api/purchase-orders/sync-all` | - | Toplu senkronizasyon |
| POST | `/api/purchase-orders/retry-failed` | - | Hatalı siparişleri yeniden dene |

---

## 🔄 Akış Diyagramları

### Satış Siparişi Onayı ve Senkronizasyon

```
1. Admin /api/sales-orders/{id}/approve çağrır
   ↓
2. Sipariş ve satırları validasyon
   ↓
3. Her satır için Katana'ya stok ekleme
   ├─ Ürün var mı? → Stok artır
   └─ Ürün yok mu? → Ürün oluştur
   ↓
4. Status: APPROVED / APPROVED_WITH_ERRORS
   ↓
5. Müşteri validasyonu başarılı mı?
   ├─ Evet → Luca'ya satış faturası gönder
   └─ Hayır → LucaSync skipped
   ↓
6. Response: success + lucaSync bilgisi
```

### Satınalma Siparişi Durumu Güncelleme

```
1. Admin /api/purchase-orders/{id}/status çağrır (Approved)
   ↓
2. Durum geçişi validasyonu
   ↓
3. Status: Approved
   ↓
4. Arka planda Katana'ya ürünleri ekle/güncelle
   (1 saniye sonra, async Task.Run)
   ├─ Ürün var mı?
   ├─ Katana API çağrısı
   └─ Log yaz
   ↓
5. Fatura senkronizasyonu (manual /sync ile)
   └─ Luca'ya alım faturası gönder
```

### Toplu Senkronizasyon

```
1. Admin /api/sales-orders/sync-all?maxCount=50 çağrır
   ↓
2. Bekleyen siparişleri çek (IsSyncedToLuca=false, LastSyncError=null)
   ↓
3. Paralel batch processing (5 concurrent)
   ├─ SyncToLuca(orderId) × 5 eşzamanlı
   ├─ Luca API çağrısı
   └─ İlerleme kaydedilir
   ↓
4. Sonuçlar topla
   ├─ successCount
   ├─ failCount
   ├─ performance metrics
   └─ error details
   ↓
5. DB'ye sonuçları kaydet
   └─ IsSyncedToLuca, LastSyncError, LastSyncAt güncelle
   ↓
6. Response: summary + errors
```

---

## ⚠️ Hata Yönetimi

### Circuit Breaker Durumları

| Durum | Davranış | Sebep |
|-------|----------|-------|
| **CLOSED** | İstekler geçer | Normal işleme |
| **OPEN** | İstekler hemen fail | 5+ ardışık hata |
| **HALF-OPEN** | Test isteği gönder | Recovery deneniyor |

### Retry Stratejisi

```
İstek → Hata → Wait 2s → Retry
              → Hata → Wait 4s → Retry
              → Hata → Wait 8s → Retry
              → Hata → Fail
```

### Validasyon Hataları

#### Müşteri Validasyonu
- ❌ VergiNo/LucaCode eksik
- ❌ VergiNo formatı geçersiz (10-11 hane değil)
- ❌ LucaCode "CUST..." ile başlıyor (geçersiz)
- ❌ Müşteri Title/ContactPerson eksik

#### Sipariş Validasyonu
- ❌ Sipariş satırları yok
- ❌ SKU boş
- ❌ Quantity=0
- ❌ Zaten senkronize edilmiş

---

## 📝 Logging ve Audit

### Log Kategorileri
- `LogCategory.UserAction`: Kullanıcı işlemleri (approve, sync)
- `LogCategory.Business`: İş mantığı (Katana/Luca entegrasyonu)
- `LogCategory.Integration`: API entegrasyonu (HTTP çağrıları)
- `LogCategory.Error`: Hata durumları

### Audit İşlemleri
- `AuditService.LogCreate()`: Yeni kayıt oluşturma
- `AuditService.LogUpdate()`: Kayıt güncelleme
- `AuditService.LogSync()`: Senkronizasyon
- `AuditService.LogDelete()`: Silme işlemi

---

## 🔐 Yetkilendirme (Authorization)

### Roller
- **Admin**: Tam kontrol (approve, sync, clear-errors)
- **Manager**: Approve yetkisi
- **Public** (AllowAnonymous): Listeleme ve detay görüntüleme

### Endpoint Yetkileri

| Endpoint | Role | Not |
|----------|------|-----|
| `/api/sales-orders/{id}/approve` | Admin, Manager | Admin onayı |
| `/api/sales-orders/{id}/sync` | Admin | Manuel sync |
| `/api/sales-orders/sync-all` | Admin | Toplu sync |
| `/api/sales-orders/clear-errors` | Admin | Admin-only |
| `/api/purchase-orders` | Public | Listeleme açık |
| `/api/purchase-orders/{id}` | Public | Detay açık |
| `/api/purchase-orders/{id}/sync` | Public | Herkes yapabilir |

---

## 🚀 Performance İpuçları

### Paralel İşleme
- Sales Orders: 5 concurrent (SyncAllPending)
- Purchase Orders: 5 concurrent (SyncAll)
- Retry Failed: 3 concurrent (RetryFailed)

### Caching
- Stats (Purchase Orders): 1 dakika cache
- Location → Depo Mapping: Startup'ta yüklenir

### İndeksler
- `SalesOrders`: `IsSyncedToLuca`, `LastSyncError`, `Status`
- `PurchaseOrders`: `Status`, `IsSyncedToLuca`, `LastSyncError`

### Database Transactions
- Luca çağrısı DB retry dışında (duplicate fatura önlemek)
- Status güncelleme transaction içinde

---

## 🔗 İlişkiler

```
SalesOrder
  ├─ Customer (ManyToOne)
  ├─ Lines (OneToMany → SalesOrderLine)
  └─ OrderInvoiceMapping (OneToOne) → LucaFaturaId, BelgeInfo

PurchaseOrder
  ├─ Supplier (ManyToOne)
  └─ Items (OneToMany → PurchaseOrderItem)
      └─ Product (ManyToOne)

LocationMapping
  └─ LocationId → DepoKodu

OrderInvoiceMapping
  ├─ OrderId + EntityType (SalesOrder/PurchaseOrder)
  ├─ LucaInvoiceId
  ├─ BelgeSeri, BelgeNo, BelgeTakipNo
  └─ ExternalOrderId
```

---

**Son Güncellenme**: 22 Aralık 2025  
**Versiyon**: 1.0
