# 🧪 Hata Yönetimi ve Onay Mekanizması - Test ve Doğrulama Kılavuzu

## 📋 Test Checklist

### ✅ Backend Testleri

#### 1. Unit Tests (Controller)

- [x] `AdminController.GetFailedSyncRecords` - List with pagination
- [x] `AdminController.GetFailedSyncRecord` - Detail view
- [x] `AdminController.ResolveFailedRecord` - Resolution workflow
- [x] `AdminController.IgnoreFailedRecord` - Ignore workflow
- [x] `AdminController.RetryFailedRecord` - Retry mechanism
- [x] `AdminController.ApproveAdjustment` - Approval workflow
- [x] `AdminController.RejectAdjustment` - Rejection workflow

#### 2. Integration Tests

Dosya: `tests/Katana.Tests/Integration/ErrorHandlingIntegrationTests.cs`

**Test Senaryoları:**

- [x] Failed records listesinin çekilmesi (pagination ile)
- [x] Status filtresi (FAILED, RETRYING, RESOLVED, IGNORED)
- [x] RecordType filtresi (STOCK, ORDER, INVOICE, CUSTOMER)
- [x] Detay görüntüleme (OriginalData, IntegrationLog)
- [x] Hata düzeltme (ResolveFailedRecord)
- [x] Düzeltilmiş verinin veritabanına yazılması
- [x] Resend flag kontrolü
- [x] Audit log oluşturma
- [x] Ignore işlemi (status=IGNORED)
- [x] Retry işlemi (RetryCount increment, exponential backoff)
- [x] PendingAdjustment approval (status=Approved, stock update)
- [x] PendingAdjustment rejection (status=Rejected, stock unchanged)
- [x] End-to-end error correction workflow
- [x] End-to-end approval workflow

### ✅ Frontend Testleri

#### 3. Component Tests

Dosya: `frontend/katana-web/src/__tests__/components/Admin/FailedRecords.test.tsx`

**Test Senaryoları:**

- [x] Component render ve data fetching
- [x] Status filter dropdown
- [x] RecordType filter dropdown
- [x] Pagination controls
- [x] View details dialog
- [x] Edit corrected data in TextField
- [x] Resolve dialog workflow
- [x] Resolution input
- [x] Resend select option
- [x] Ignore workflow (with prompt)
- [x] Retry button
- [x] Status chip colors (FAILED=red, RETRYING=yellow, RESOLVED=green)
- [x] Refresh button
- [x] API error handling
- [x] Loading state
- [x] Empty state
- [x] Complete end-to-end workflow

---

## 🚀 Test Komutları

### Backend Tests

#### Tüm testleri çalıştır:

```powershell
cd C:\Users\GAMZE\Desktop\katana
dotnet test
```

#### Sadece Integration testleri:

```powershell
dotnet test --filter "FullyQualifiedName~ErrorHandlingIntegrationTests"
```

#### Spesifik test:

```powershell
dotnet test --filter "FullyQualifiedName~ResolveFailedRecord_ValidData_UpdatesStatusAndDatabase"
```

#### Coverage report:

```powershell
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Frontend Tests

#### Tüm testleri çalıştır:

```powershell
cd C:\Users\GAMZE\Desktop\katana\frontend\katana-web
npm test
```

#### Watch mode (geliştirme sırasında):

```powershell
npm test -- --watch
```

#### Coverage report:

```powershell
npm test -- --coverage
```

#### Sadece FailedRecords testleri:

```powershell
npm test -- FailedRecords.test.tsx
```

---

## 🔍 Manuel Test Senaryoları

### Senaryo 1: Hatalı Stok Verisi Düzeltme

**Amaç:** Admin hatalı stok verisini düzeltip yeniden gönderebilmeli

#### Adımlar:

1. **Hatalı Kayıt Oluşturma:**

```sql
-- SQL Server'da çalıştır
INSERT INTO IntegrationLogs (SyncType, Status, StartTime, EndTime, ErrorMessage)
VALUES ('KATANA_TO_LUCA', 'FAILED', GETUTCDATE(), GETUTCDATE(), 'Validation error');

DECLARE @LogId INT = SCOPE_IDENTITY();

INSERT INTO FailedSyncRecords
(RecordType, RecordId, OriginalData, ErrorMessage, ErrorCode, FailedAt, RetryCount, Status, IntegrationLogId)
VALUES
('STOCK', 'TEST-SKU-001',
'{"sku":"TEST-SKU-001","quantity":-10,"productName":"Test Product"}',
'Validation failed: Quantity cannot be negative',
'VAL-001',
GETUTCDATE(),
0,
'FAILED',
@LogId);
```

2. **Frontend'de Görüntüleme:**

   - Katana Admin Panel aç: `http://localhost:3000/admin`
   - "Hatalı Kayıtlar" tab'ına tıkla
   - Status filter: "Başarısız" seç
   - Kayıt görünüyor mu? ✅
   - Hata mesajı doğru mu? ✅

3. **Detay Görüntüleme:**

   - Göz ikonu (View) tıkla
   - "Hatalı Kayıt Detayları" dialog açıldı mı? ✅
   - OriginalData TextField'da JSON görünüyor mu? ✅
   - Quantity -10 değerinde mi? ✅

4. **Veri Düzeltme:**

   - TextField'da quantity değerini -10'dan 10'a değiştir
   - "Düzelt ve Gönder" butonuna tıkla
   - "Hatayı Çöz" dialog açıldı mı? ✅

5. **Çözüm Kaydetme:**

   - "Çözüm Açıklaması" yaz: "Negatif miktar düzeltildi"
   - "Düzeltilmiş veriyi yeniden gönder" → "Evet, yeniden gönder" seç
   - "Çöz" butonuna tıkla
   - Dialog kapandı mı? ✅
   - Liste yenilendi mi? ✅

6. **Veritabanı Kontrolü:**

```sql
-- Kayıt RESOLVED olmalı
SELECT Id, Status, Resolution, ResolvedAt, ResolvedBy, OriginalData
FROM FailedSyncRecords
WHERE RecordId = 'TEST-SKU-001';

-- Audit log oluştu mu?
SELECT TOP 5 *
FROM AuditLogs
WHERE EntityName = 'FailedSyncRecord'
ORDER BY Timestamp DESC;
```

**Beklenen Sonuç:**

- ✅ Status = 'RESOLVED'
- ✅ Resolution dolu
- ✅ ResolvedAt ve ResolvedBy dolu
- ✅ OriginalData güncellendi (quantity=10)
- ✅ AuditLog kaydı var

---

### Senaryo 2: Stok Güncelleme Onayı

**Amaç:** Admin bekleyen stok güncellemesini onaylayabilmeli

#### Adımlar:

1. **Onay Bekleyen Kayıt Oluşturma:**

```sql
-- Ürün bul veya oluştur
IF NOT EXISTS (SELECT 1 FROM Products WHERE SKU = 'TEST-APPROVAL-001')
BEGIN
    INSERT INTO Products (SKU, Name, Stock, IsActive)
    VALUES ('TEST-APPROVAL-001', 'Test Approval Product', 50, 1);
END

DECLARE @ProductId INT = (SELECT Id FROM Products WHERE SKU = 'TEST-APPROVAL-001');

-- Pending adjustment oluştur
INSERT INTO PendingStockAdjustments
(ExternalOrderId, ProductId, Sku, ProductName, OldQuantity, Quantity, Status, Source, CreatedAt)
VALUES
('TEST-ORDER-' + CAST(NEWID() AS VARCHAR(36)),
@ProductId,
'TEST-APPROVAL-001',
'Test Approval Product',
50,  -- Old quantity
100, -- New quantity (onay bekliyor)
'Pending',
'Katana',
GETUTCDATE());
```

2. **Frontend'de Görüntüleme:**

   - Admin Panel aç
   - "Genel Bakış" tab'ında "Onay Bekleyen İşlemler" tablosunu kontrol et
   - Pending kayıt görünüyor mu? ✅
   - Old Quantity = 50, Quantity = 100 mi? ✅

3. **Onaylama:**

   - "Onayla" butonuna tıkla
   - Success message geldi mi? ✅

4. **Veritabanı Kontrolü:**

```sql
-- Pending adjustment onaylandı mı?
SELECT Status, ApprovedAt, ApprovedBy
FROM PendingStockAdjustments
WHERE Sku = 'TEST-APPROVAL-001'
ORDER BY CreatedAt DESC;

-- Ürün stoğu güncellendi mi?
SELECT SKU, Stock
FROM Products
WHERE SKU = 'TEST-APPROVAL-001';
```

**Beklenen Sonuç:**

- ✅ PendingStockAdjustment.Status = 'Approved'
- ✅ ApprovedAt dolu
- ✅ ApprovedBy dolu
- ✅ Product.Stock = 100 (güncellendi)

---

### Senaryo 3: Stok Güncelleme Reddi

**Amaç:** Admin yanlış stok güncellemesini reddedebilmeli

#### Adımlar:

1. **Pending Kayıt Oluştur** (Senaryo 2'deki SQL'i tekrar çalıştır)

2. **Frontend'de Reddetme:**

   - "Reddet" butonuna tıkla
   - Rejection reason dialog açıldı mı? ✅
   - Neden gir: "Stok sayımı yanlış"
   - Confirm

3. **Veritabanı Kontrolü:**

```sql
-- Rejected mi?
SELECT Status, RejectedAt, RejectedBy, RejectionReason
FROM PendingStockAdjustments
WHERE Sku = 'TEST-APPROVAL-001'
ORDER BY CreatedAt DESC;

-- Ürün stoğu DEĞİŞMEMELİ
SELECT SKU, Stock
FROM Products
WHERE SKU = 'TEST-APPROVAL-001';
```

**Beklenen Sonuç:**

- ✅ Status = 'Rejected'
- ✅ RejectionReason = "Stok sayımı yanlış"
- ✅ Product.Stock = 50 (değişmedi)

---

### Senaryo 4: Retry Mechanism

**Amaç:** Failed record'u retry edebilmeli, exponential backoff çalışmalı

#### Adımlar:

1. **Hatalı Kayıt Oluştur** (Senaryo 1'deki SQL'i kullan)

2. **İlk Retry:**

   - Admin panel → "Hatalı Kayıtlar"
   - Restart icon (Retry) tıkla
   - Success message geldi mi? ✅

3. **Veritabanı Kontrolü:**

```sql
SELECT RetryCount, LastRetryAt, NextRetryAt, Status
FROM FailedSyncRecords
WHERE RecordId = 'TEST-SKU-001';
```

**Beklenen:**

- RetryCount = 1
- LastRetryAt = now
- NextRetryAt = now + 2 minutes (2^1)
- Status = 'RETRYING'

4. **İkinci Retry:**
   - Retry butonuna tekrar tıkla

**Beklenen:**

- RetryCount = 2
- NextRetryAt = now + 4 minutes (2^2)

5. **Üçüncü Retry:**

**Beklenen:**

- RetryCount = 3
- NextRetryAt = now + 8 minutes (2^3)

---

### Senaryo 5: Ignore Workflow

**Amaç:** Admin gereksiz hatayı ignore edebilmeli

#### Adımlar:

1. **Hatalı Kayıt Oluştur**
2. **Detay Dialog Aç**
3. **"Göz Ardı Et" Tıkla:**

   - Prompt açıldı mı? ✅
   - Neden gir: "Artık satışta olmayan ürün"
   - OK

4. **Veritabanı Kontrolü:**

```sql
SELECT Status, Resolution, ResolvedAt, ResolvedBy
FROM FailedSyncRecords
WHERE RecordId = 'TEST-SKU-001';
```

**Beklenen:**

- Status = 'IGNORED'
- Resolution = "Artık satışta olmayan ürün"

---

## 📊 Test Coverage Hedefleri

| Component          | Target Coverage | Current |
| ------------------ | --------------- | ------- |
| AdminController    | 80%             | TBD     |
| FailedRecords.tsx  | 80%             | TBD     |
| LoggingService     | 90%             | TBD     |
| PendingAdjustments | 70%             | TBD     |

---

## 🐛 Bilinen Sorunlar ve TODO

### Backend

1. ⚠️ **Resend Logic Incomplete:**
   - `ResolveFailedRecord` → Resend flag true ise servis çağrılmalı
   - TODO: IKatanaService/ILucaService integration
2. ⚠️ **Retry Worker Service Missing:**

   - Background service henüz yok
   - NextRetryAt geldiğinde otomatik retry olmalı

3. ⚠️ **JSON Deserialization:**
   - CorrectedData deserialize edilemiyor ise hata handle edilmeli

### Frontend

1. ⚠️ **JSON Validation:**

   - TextField'da JSON validate edilmiyor
   - Invalid JSON için error message gösterilmeli

2. ⚠️ **Success/Error Toasts:**

   - API success/error için toast notification yok
   - Snackbar eklenebilir

3. ⚠️ **Bulk Operations:**
   - Birden fazla kaydı seçip toplu resolve/ignore yapılamıyor

---

## ✅ Production Deployment Checklist

### Database

- [ ] FailedSyncRecords tablosu var mı?
- [ ] Index'ler oluşturuldu mu?
  ```sql
  CREATE INDEX IX_FailedSyncRecords_Status ON FailedSyncRecords(Status);
  CREATE INDEX IX_FailedSyncRecords_RecordType ON FailedSyncRecords(RecordType);
  CREATE INDEX IX_FailedSyncRecords_FailedAt ON FailedSyncRecords(FailedAt DESC);
  ```
- [ ] AuditLogs tablosu var mı?
- [ ] PendingStockAdjustments tablosu var mı?

### Backend

- [ ] Integration tests geçiyor mu?
- [ ] AdminController endpoint'leri dağıtıldı mı?
- [ ] Logging yapılandırıldı mı?
- [ ] Authorization (admin role) çalışıyor mu?

### Frontend

- [ ] Component tests geçiyor mu?
- [ ] FailedRecords component build oluyor mu?
- [ ] AdminPanel'e tab eklendi mi?
- [ ] API base URL production için doğru mu?

### Monitoring

- [ ] Application Insights yapılandırıldı mı?
- [ ] Error rate alert'leri kuruldu mu?
- [ ] Audit log monitoring var mı?

### Documentation

- [ ] API documentation (Swagger) güncellendi mi?
- [ ] Admin kullanım kılavuzu yazıldı mı?
- [ ] Deployment guide hazır mı?

---

## 🎯 Next Steps

1. **Backend TODO'ları Tamamla** (3-5 saat)

   - Resend logic implement et
   - Retry worker service oluştur
   - JSON validation ekle

2. **Frontend İyileştirmeleri** (2-3 saat)

   - JSON validation
   - Toast notifications
   - Bulk operations (optional)

3. **Integration Testing** (4-6 saat)

   - Tüm test senaryolarını manuel çalıştır
   - Edge case'leri test et
   - Performance testing

4. **User Acceptance Testing** (2-3 gün)

   - Gerçek kullanıcılarla test
   - Feedback topla
   - UI/UX iyileştirmeleri

5. **Production Deployment** (1 gün)
   - Staging'de final test
   - Database migration
   - Monitoring setup
   - Production deploy

---

**Test Raporu Oluşturma:**

```powershell
# Backend test report
dotnet test --logger "trx;LogFileName=test-results.trx" --results-directory ./TestResults

# Frontend test report
npm test -- --coverage --coverageReporters=html
```

**Son Güncelleme:** 2025-01-XX  
**Test Durumu:** ✅ Tests yazıldı, manuel test bekleniyor
