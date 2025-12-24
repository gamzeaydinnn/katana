# KATANA-LUCA ENTEGRASYON SORUN GİDERME REHBERİ

## 🔍 Hızlı Tanı Tablosu

| Sorun                         | Belirti                 | Çözüm                                                           |
| ----------------------------- | ----------------------- | --------------------------------------------------------------- |
| Ürün senkronize edilmiyor     | Luca'da ürün yok        | [Ürün Senkronizasyon Sorunları](#ürün-senkronizasyon-sorunları) |
| Duplicate ürün oluşturuluyor  | Luca'da aynı ürün 2x    | [Duplicate Prevention](#duplicate-prevention)                   |
| Sipariş onaylanamıyor         | "SKU boş" hatası        | [Sipariş Onay Sorunları](#sipariş-onay-sorunları)               |
| Luca'ya fatura gönderilemiyor | "Müşteri bilgisi eksik" | [Fatura Gönderme Sorunları](#fatura-gönderme-sorunları)         |
| Session timeout               | "Login olunmalı" hatası | [Session Yönetimi](#session-yönetimi)                           |

---

## 🐛 Ürün Senkronizasyon Sorunları

### Sorun 1: Ürün Luca'da Görünmüyor

**Belirti**:

- Katana'da ürün var
- Luca'da ürün yok
- Senkronizasyon başarılı gösteriyor

**Tanı**:

```bash
# 1. Database'de kontrol et
SELECT * FROM Products WHERE SKU = 'PIPE-001';
# Sonuç: LucaId = NULL veya IsSyncedToLuca = false

# 2. Logs'ta kontrol et
# LastSyncError alanını kontrol et
```

**Çözüm**:

```csharp
// 1. Ürünü manuel olarak senkronize et
POST /api/sync/products-to-luca
{
  "dryRun": false,
  "limit": 1,
  "preferBarcodeMatch": true
}

// 2. Hata mesajını kontrol et
// Response'da LastSyncError alanını oku

// 3. Hata türüne göre çözüm uygula
```

**Olası Nedenler**:

1. **Kategori Mapping Eksik**

   ```
   Katana: Category = "Pipes"
   Mapping: Boş
   Sonuç: Luca'da kategori kodu NULL

   Çözüm:
   - PRODUCT_CATEGORY mapping tablosuna ekle
   - "Pipes" → "220"
   ```

2. **Ölçü Birimi Mapping Eksik**

   ```
   Katana: Unit = "pcs"
   Mapping: Boş
   Sonuç: AutoMapUnit() fallback kullanılır

   Çözüm:
   - appsettings.json UnitMapping'e ekle
   - "pcs" → 5
   ```

3. **Encoding Sorunu**

   ```
   Katana: "COOLING WATER PIPE Ø25mm"
   Luca: "COOLING WATER PIPE ??25mm"
   Sonuç: Luca yeni versiyon oluşturur

   Çözüm:
   - Mapper'da encoding normalize edilir
   - Ø → O dönüşümü otomatik
   ```

4. **Barkod Duplicate**

   ```
   Katana: SKU = "PIPE-V2", Barcode = "8690123456789"
   Luca'da mevcut: SKU = "PIPE", Barcode = "8690123456789"
   Sonuç: "Duplicate Barcode" hatası

   Çözüm:
   - Versiyonlu SKU'lar için barkod NULL gönder
   - Mapper'da otomatik kontrol edilir
   ```

---

### Sorun 2: Duplicate Ürün Oluşturuluyor

**Belirti**:

- Luca'da aynı ürün 2-3 kez görünüyor
- SKU'lar farklı: "PIPE-001", "PIPE-001-V2", "PIPE-001-V3"

**Tanı**:

```bash
# Luca'da kontrol et
SELECT * FROM StokKarti WHERE KartKodu LIKE 'PIPE-001%';
# Sonuç: 3 kayıt

# Katana'da kontrol et
SELECT * FROM Products WHERE SKU LIKE 'PIPE-001%';
# Sonuç: 1 kayıt
```

**Çözüm**:

```csharp
// 1. Luca'da duplicate'leri kontrol et
// Admin paneli → Ürünler → Arama: "PIPE-001"

// 2. Hangi versiyonun doğru olduğunu belirle
// - Fiyat doğru mu?
// - Kategori doğru mu?
// - Barkod doğru mu?

// 3. Yanlış versiyonları sil
// Admin paneli → Ürünler → [Sil]

// 4. Katana'da ürünü güncelle
// Fiyat, kategori, barkod kontrol et

// 5. Tekrar senkronize et
POST /api/sync/products-to-luca
{
  "dryRun": false,
  "forceSendDuplicates": false,
  "limit": 1
}
```

**Neden Oluşuyor?**

1. **Ürün İsmi Değişikliği**

   ```
   Luca'da: "COOLING WATER PIPE Ø25mm"
   Katana'dan gelen: "COOLING WATER PIPE O25MM"

   Luca: "İsim farklı, yeni ürün mü?" → Yeni versiyon oluştur

   Çözüm: Encoding normalize edilir (otomatik)
   ```

2. **Barkod Değişikliği**

   ```
   Luca'da: Barcode = "8690123456789"
   Katana'dan gelen: Barcode = "8690123456790"

   Luca: "Barkod farklı, yeni ürün mü?" → Yeni versiyon oluştur

   Çözüm: Barkod kontrol et, doğru barkod gönder
   ```

3. **Kategori Değişikliği**

   ```
   Luca'da: Kategori = "220"
   Katana'dan gelen: Kategori = "221"

   Luca: "Kategori farklı, yeni ürün mü?" → Yeni versiyon oluştur

   Çözüm: Kategori mapping kontrol et
   ```

---

## 📦 Sipariş Onay Sorunları

### Sorun 1: "SKU Boş" Hatası

**Belirti**:

```
Admin [Onayı] Tıklar
Hata: "SKU boş!"
Sipariş onaylanamıyor
```

**Tanı**:

```bash
# Database'de kontrol et
SELECT * FROM SalesOrderLines WHERE SKU IS NULL OR SKU = '';
# Sonuç: Boş SKU'lar var

# Katana'da kontrol et
# Sipariş satırlarında SKU alanı dolu mu?
```

**Çözüm**:

```csharp
// 1. Katana'da sipariş satırını kontrol et
// Admin paneli → Siparişler → SO-001 → Satırlar

// 2. SKU'yu doldur
// Ürün seç veya SKU manuel gir

// 3. Siparişi tekrar senkronize et
// Katana'dan yeniden çek

// 4. Admin onayını tekrar dene
POST /api/sales-orders/{id}/approve
```

**Neden Oluşuyor?**

- Katana API'den SKU boş geldi
- Ürün seçilmeden sipariş oluşturuldu
- Veri tabanında bozulma

---

### Sorun 2: "Ürün Bulunamadı" Hatası

**Belirti**:

```
Admin [Onayı] Tıklar
Hata: "Ürün bulunamadı: PIPE-001"
Sipariş onaylanamıyor
```

**Tanı**:

```bash
# Katana'da ürün var mı?
SELECT * FROM Products WHERE SKU = 'PIPE-001';
# Sonuç: Boş

# Katana API'de kontrol et
GET /api/v1/products?sku=PIPE-001
# Sonuç: Boş
```

**Çözüm**:

```csharp
// 1. Ürünü Katana'da oluştur
// Katana admin paneli → Ürünler → [Yeni Ürün]

// 2. SKU: PIPE-001
// 3. Name: COOLING WATER PIPE
// 4. Price: 150.00
// 5. Stock: 0 (başlangıç)

// 6. Siparişi tekrar senkronize et
// Katana'dan yeniden çek

// 7. Admin onayını tekrar dene
POST /api/sales-orders/{id}/approve
```

---

## 💬 Fatura Gönderme Sorunları

### Sorun 1: "Müşteri Bilgisi Eksik" Hatası

**Belirti**:

```
Admin [Kozaya Senkronize] Tıklar
Hata: "Müşteri bilgisi eksik"
Fatura Luca'ya gönderilemiyor
```

**Tanı**:

```bash
# Database'de müşteri var mı?
SELECT * FROM Customers WHERE Id = 91190794;
# Sonuç: Boş

# Müşteri bilgileri tam mı?
SELECT * FROM Customers WHERE Id = 91190794;
# Kontrol: TaxNo, Email, Phone, Address
```

**Çözüm**:

```csharp
// 1. Müşteri bilgisini kontrol et
// Admin paneli → Müşteriler → ABC Tekstil

// 2. Eksik alanları doldur
// - TaxNo (Vergi No)
// - Email
// - Phone
// - Address

// 3. Siparişi tekrar senkronize et
POST /api/sales-orders/{id}/sync
```

**Zorunlu Alanlar**:

```
- CariAd (Müşteri Adı) ✅
- CariSoyad (Müşteri Soyadı) ✅
- VergiNo (Vergi Numarası) ✅
- CariKodu (Müşteri Kodu) ✅
- ParaBirimKod (Para Birimi) ✅
```

---

### Sorun 2: "Sipariş Satırları Yok" Hatası

**Belirti**:

```
Admin [Kozaya Senkronize] Tıklar
Hata: "Sipariş satırları yok"
Fatura Luca'ya gönderilemiyor
```

**Tanı**:

```bash
# Database'de sipariş satırları var mı?
SELECT * FROM SalesOrderLines WHERE SalesOrderId = 1;
# Sonuç: Boş

# Katana'da sipariş satırları var mı?
GET /api/v1/sales_orders/123456789
# Response'da sales_order_rows alanı kontrol et
```

**Çözüm**:

```csharp
// 1. Katana'da sipariş satırlarını kontrol et
// Admin paneli → Siparişler → SO-001 → Satırlar

// 2. Satırlar yoksa:
// - Katana'da sipariş satırlarını ekle
// - Veya siparişi sil ve yeniden oluştur

// 3. Siparişi tekrar senkronize et
// Katana'dan yeniden çek

// 4. Admin onayını ve senkronizasyonu tekrar dene
POST /api/sales-orders/{id}/sync
```

---

### Sorun 3: "Luca API Hatası" Hatası

**Belirti**:

```
Admin [Kozaya Senkronize] Tıklar
Hata: "Luca API hatası: HTTP 500"
Fatura Luca'ya gönderilemiyor
```

**Tanı**:

```bash
# Luca'nın durumunu kontrol et
# 1. Luca server çalışıyor mu?
# 2. Network bağlantısı var mı?
# 3. Luca session timeout mu?

# Logs'ta kontrol et
# LastSyncError alanında detaylı hata mesajı
```

**Çözüm**:

```csharp
// 1. Luca'nın durumunu kontrol et
// Luca admin paneli → System → Status

// 2. Luca session'ı yenile
// LucaService.ForceSessionRefreshAsync()

// 3. Siparişi tekrar senkronize et
POST /api/sales-orders/{id}/sync

// 4. Hala hata varsa:
// - Luca'nın logs'unu kontrol et
// - Network bağlantısını kontrol et
// - Firewall kurallarını kontrol et
```

---

## 🔐 Session Yönetimi

### Sorun 1: "Login Olunmalı" Hatası

**Belirti**:

```
Senkronizasyon başarısız
Hata: "Login olunmalı"
Luca API'ye erişilemiyor
```

**Tanı**:

```bash
# Session cookie'si var mı?
# LucaService._sessionCookie kontrol et

# Session timeout mu?
# _cookieExpiresAt kontrol et

# Logs'ta kontrol et
# "Session expired" mesajı var mı?
```

**Çözüm**:

```csharp
// 1. Session'ı manuel olarak yenile
POST /api/admin/refresh-luca-session

// 2. Veya otomatik yenileme
// LucaService.ForceSessionRefreshAsync()

// 3. Senkronizasyonu tekrar dene
POST /api/sales-orders/{id}/sync

// 4. Hala hata varsa:
// - Luca credentials kontrol et
// - appsettings.json'da LucaApiSettings kontrol et
// - Username/Password doğru mu?
```

**Luca Credentials Kontrol**:

```json
{
  "LucaApiSettings": {
    "BaseUrl": "https://luca.example.com",
    "Username": "admin",
    "Password": "password",
    "UseTokenAuth": false,
    "ManualSessionCookie": "JSESSIONID=..."
  }
}
```

---

### Sorun 2: "Session Timeout" Hatası

**Belirti**:

```
Senkronizasyon başarılı
Ama sonra hata: "Session timeout"
```

**Tanı**:

```bash
# Session timeout süresi kontrol et
# appsettings.json → LucaApiSettings → SessionTimeoutMinutes

# Logs'ta kontrol et
# "Session expired" mesajı var mı?
```

**Çözüm**:

```csharp
// 1. Session timeout süresini artır
// appsettings.json
{
  "LucaApiSettings": {
    "SessionTimeoutMinutes": 30  // Varsayılan: 20
  }
}

// 2. Veya session'ı periyodik olarak yenile
// Background worker'da ForceSessionRefreshAsync() çağır

// 3. Senkronizasyonu tekrar dene
POST /api/sales-orders/sync-all
```

---

## 🔄 Retry Mekanizması

### Hatalı Siparişleri Yeniden Senkronize Etme

```csharp
// 1. Hatalı siparişleri listele
GET /api/sales-orders?status=failed

// 2. Tekil retry
POST /api/sales-orders/{id}/sync

// 3. Toplu retry
POST /api/sales-orders/retry-failed?maxRetries=3

// 4. Satınalma siparişleri için
POST /api/purchase-orders/retry-failed?maxRetries=3
```

### Retry Sonuçları

```json
{
  "totalProcessed": 10,
  "successCount": 8,
  "failCount": 2,
  "durationMs": 5000,
  "rateOrdersPerMinute": 96.0,
  "errors": [
    {
      "orderId": 1,
      "orderNo": "SO-001",
      "error": "Müşteri bilgisi eksik"
    }
  ]
}
```

---

## 📊 Monitoring ve Logging

### Logs Nerede?

```
1. Application Logs
   - File: logs/application.log
   - Format: [Timestamp] [Level] [Component] Message

2. Luca API Logs
   - File: logs/luca-api.log
   - Format: [Timestamp] [Method] [URL] [Status] [Response]

3. Database Logs
   - Table: SyncOperationLogs
   - Columns: OperationType, Status, ErrorMessage, CreatedAt
```

### Önemli Log Mesajları

```
✅ Başarılı:
- "Ürün senkronize edildi: PIPE-001"
- "Sipariş onaylandı: SO-001"
- "Fatura Luca'ya gönderildi: SO-001"

❌ Hata:
- "Duplicate Barcode: 8690123456789"
- "Müşteri bilgisi eksik: CustomerId=91190794"
- "Luca API hatası: HTTP 500"
- "Session timeout"
```

---

## 🛠️ Maintenance Görevleri

### Günlük

```
1. Senkronizasyon durumunu kontrol et
   - Admin paneli → Dashboard
   - Hatalı siparişler var mı?

2. Logs'u kontrol et
   - Tekrarlayan hatalar var mı?
   - Performance sorunları var mı?

3. Session'ı kontrol et
   - Luca session aktif mi?
   - Timeout hatası var mı?
```

### Haftalık

```
1. Duplicate ürünleri kontrol et
   - Luca'da duplicate var mı?
   - Katana'da duplicate var mı?

2. Mapping'leri kontrol et
   - Kategori mapping tam mı?
   - Ölçü birimi mapping tam mı?

3. Performance metrikleri
   - Senkronizasyon hızı
   - Hata oranı
```

### Aylık

```
1. Veri tutarlılığı kontrolü
   - Katana ve Luca'da aynı ürünler var mı?
   - Fiyatlar tutarlı mı?

2. Backup kontrol
   - Database backup alındı mı?
   - Logs backup alındı mı?

3. Sistem güncellemeleri
   - Katana API güncellemesi var mı?
   - Luca API güncellemesi var mı?
```

---

## 📞 Destek İletişim

### Hata Raporlama

Hata raporlarken aşağıdaki bilgileri sağlayın:

```
1. Hata Mesajı
   - Tam hata metni
   - Hata kodu (varsa)

2. Zaman Bilgisi
   - Hata ne zaman oluştu?
   - Kaç kez tekrarlandı?

3. İlgili Veriler
   - Sipariş No: SO-001
   - Ürün SKU: PIPE-001
   - Müşteri: ABC Tekstil

4. Logs
   - Application logs
   - Luca API logs
   - Database logs

5. Sistem Bilgisi
   - Katana versiyonu
   - Luca versiyonu
   - .NET versiyonu
```

---

**Rapor Tarihi**: 24 Aralık 2025
**Versiyon**: 1.0
**Hazırlayan**: Kiro AI Assistant
