# KATANA → LUCA VERİ AKIŞI DETAYLI ANALİZİ

## 📋 İçindekiler

1. [Genel Mimari](#genel-mimari)
2. [Ürün Senkronizasyonu (Katana → Luca)](#ürün-senkronizasyonu)
3. [Luca'da Güncellenen Ürün (Luca → Katana)](#lucada-güncellenen-ürün)
4. [Sipariş Akışı (Katana → Luca)](#sipariş-akışı)
5. [Kritik Sorunlar ve Çözümler](#kritik-sorunlar)
6. [Veri Tutarlılığı](#veri-tutarlılığı)

---

## 🏗️ Genel Mimari

```
┌─────────────────────────────────────────────────────────────────┐
│                    KATANA ERP SISTEMI                           │
│  (Manufacturing Resource Planning - Üretim Planlama)            │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     │ REST API (JSON)
                     │ - Products
                     │ - Customers
                     │ - Sales Orders
                     │ - Purchase Orders
                     │ - Stock Adjustments
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│              KATANA INTEGRATION SYSTEM (Bu Sistem)              │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ KatanaService: API çağrıları ve veri çekme              │   │
│  │ KatanaToLucaMapper: Veri dönüşümü ve mapping            │   │
│  │ LucaService: Luca API ile iletişim                      │   │
│  │ SyncService: Senkronizasyon orkestratörü                │   │
│  │ Workers: Arka plan işleri (5 dakikada bir)              │   │
│  └──────────────────────────────────────────────────────────┘   │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     │ REST/SOAP API (XML/JSON)
                     │ - Stok Kartları (Create/Update/Delete)
                     │ - Cari Kartlar (Create/Update)
                     │ - Faturalar (Create)
                     │ - Stok Hareketleri
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│                  LUCA/KOZA ERP SISTEMI                          │
│  (Muhasebe ve İş Yönetimi - Accounting & Business Management)   │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔄 Ürün Senkronizasyonu (Katana → Luca)

### 1. Veri Akışı Diyagramı

```
KATANA ÜRÜN
    │
    ├─ SKU: "PIPE-001"
    ├─ Name: "COOLING WATER PIPE Ø25mm"
    ├─ Price: 150.00 TRY
    ├─ CostPrice: 100.00 TRY
    ├─ Unit: "pcs"
    ├─ Category: "Pipes"
    ├─ Barcode: "8690123456789"
    └─ IsActive: true
         │
         ▼
    KatanaToLucaMapper.MapKatanaProductToStockCard()
         │
         ├─ SKU Normalizasyonu
         │  └─ "PIPE-001" → "PIPE-001" (trim + upper)
         │
         ├─ Name Kontrolü
         │  ├─ Boş mu? → SKU kullan (UYARI!)
         │  ├─ Encoding Dönüşümü
         │  │  └─ "Ø" → "O" (ISO-8859-9 uyumluluğu)
         │  └─ Normalize: "COOLING WATER PIPE O25MM"
         │
         ├─ Kategori Mapping
         │  ├─ Database PRODUCT_CATEGORY tablosundan ara
         │  ├─ appsettings.json CategoryMapping'den ara
         │  └─ Sonuç: "220" (Luca kategori kodu)
         │
         ├─ Ölçü Birimi Mapping
         │  ├─ "pcs" → 5 (ADET)
         │  ├─ "kg" → 1 (KİLOGRAM)
         │  └─ Fallback: AutoMapUnit()
         │
         ├─ Barkod Kontrolü
         │  ├─ Versiyonlu SKU? (-V2, -V3)
         │  │  └─ Evet → Barkod NULL (Duplicate Barcode hatası önleme)
         │  └─ Hayır → Barkod gönder
         │
         └─ Fiyat Dönüşümü
            ├─ Alış Fiyatı: 100.00
            └─ Satış Fiyatı: 150.00
         │
         ▼
    LucaCreateStokKartiRequest
    {
      "kartKodu": "PIPE-001",
      "kartAdi": "COOLING WATER PIPE O25MM",
      "baslangicTarihi": "24/12/2025",
      "olcumBirimiId": 5,
      "kartTuru": 1,
      "kartTipi": 1,
      "kartAlisKdvOran": 1,
      "perakendeAlisBirimFiyat": 100.0,
      "perakendeSatisBirimFiyat": 150.0,
      "kategoriAgacKod": "220",
      "barkod": "8690123456789",
      "satilabilirFlag": 1,
      "satinAlinabilirFlag": 1,
      "maliyetHesaplanacakFlag": true
    }
         │
         ▼
    LucaService.SendStockCardsAsync()
         │
         ├─ Authentication (Session/Token)
         ├─ Branch Selection
         ├─ HTTP POST /koza/api/stokKarti/create
         └─ Response: { "skartId": 12345, "success": true }
         │
         ▼
    Database Update
    {
      "LucaId": 12345,
      "IsSyncedToLuca": true,
      "LastSyncAt": "2025-12-24T10:30:00Z",
      "LastSyncError": null
    }
```

### 2. Kritik Mapping Kuralları

#### A. SKU Normalizasyonu

```csharp
// Katana'dan gelen SKU
var sku = product.SKU?.Trim() ?? product.GetProductCode();

// Versiyonlu SKU Kontrolü
bool isVersionedSku = Regex.IsMatch(sku, @"-V\d+$", RegexOptions.IgnoreCase);
// Örnek: "PIPE-V2", "silll12344-V3" → Versiyonlu

if (isVersionedSku)
{
    barcodeToSend = null;  // 🔥 Barkod NULL gönder (Duplicate Barcode hatası önleme)
}
```

#### B. Ürün İsmi Kontrolü

```csharp
// 🔥 KRİTİK SORUN: Katana bazen Name alanını boş gönderiyor
var rawName = string.IsNullOrWhiteSpace(product.Name)
    ? sku  // SKU kullan (UYARI!)
    : product.Name.Trim();

// Encoding Dönüşümü (UTF-8 → ISO-8859-9)
var name = NormalizeProductNameForLuca(rawName);
// "COOLING WATER PIPE Ø25mm" → "COOLING WATER PIPE O25MM"
```

#### C. Kategori Mapping

```csharp
// Mapping Önceliği:
// 1. Database PRODUCT_CATEGORY tablosu
// 2. appsettings.json CategoryMapping
// 3. DefaultKategoriKodu
// 4. NULL (Luca kabul eder)

var category = null;
if (productCategoryMappings?.TryGetValue("PIPES", out var mapped) == true)
{
    category = mapped;  // "220"
}
```

#### D. Ölçü Birimi Mapping

```csharp
// Mapping Önceliği:
// 1. Override parametresi
// 2. Database UNIT mapping
// 3. appsettings.json UnitMapping
// 4. LucaApiSettings.UnitMapping
// 5. AutoMapUnit() fallback
// 6. DefaultOlcumBirimiId

var unitMappings = new Dictionary<string, int>
{
    { "pcs", 5 },      // ADET
    { "kg", 1 },       // KİLOGRAM
    { "m", 2 },        // METRE
    { "l", 3 },        // LİTRE
    { "m2", 6 },       // METREKARE
    { "m3", 7 },       // METREKÜP
    { "ton", 8 },      // TON
    { "box", 9 }       // KUTU
};
```

---

## 🔄 Luca'da Güncellenen Ürün (Luca → Katana)

### ⚠️ ÖNEMLİ: Luca'dan Katana'ya Geri Akış YOK!

**Mevcut Durum**: Sistem **ONE-WAY** (tek yönlü) çalışıyor:

```
KATANA → LUCA ✅ (Ürün gönderme)
LUCA → KATANA ❌ (Geri akış YOK)
```

### Luca'da Yapılan Değişiklikler

Luca'da bir stok kartı güncellenirse:

1. **Fiyat Değişikliği**

   - Luca'da: 150.00 → 200.00
   - Katana'da: Hala 150.00 (Senkronize edilmez)

2. **Kategori Değişikliği**

   - Luca'da: "220" → "221"
   - Katana'da: Hala "Pipes" (Senkronize edilmez)

3. **Stok Hareketi**
   - Luca'da: Stok artırılır/azaltılır
   - Katana'da: Hala eski değer (Senkronize edilmez)

### Neden Geri Akış Yok?

1. **Sistem Tasarımı**: Katana master sistem, Luca slave sistem
2. **Veri Sahipliği**: Katana ürün verilerinin sahibi
3. **Senkronizasyon Yönü**: Katana → Luca (tek yön)
4. **Çakışma Riski**: İki yönlü senkronizasyon veri çakışmasına neden olabilir

### Çözüm: Luca'da Yapılan Değişiklikleri Katana'ya Aktarmak İçin

Eğer Luca'da yapılan değişiklikleri Katana'ya aktarmak istiyorsanız:

1. **Manuel Güncelleme**: Katana admin panelinden ürünü güncelleyin
2. **Batch Import**: Luca'dan export → Katana'ya import
3. **Webhook**: Luca'dan webhook gönder → Katana'da güncelle (Uygulanmadı)
4. **Scheduled Sync**: Luca'dan periyodik olarak veri çek (Uygulanmadı)

---

## 📦 Sipariş Akışı (Katana → Luca)

### 1. Satış Siparişi Akışı

```
KATANA SATIŞ SİPARİŞİ
    │
    ├─ OrderNo: "SO-001"
    ├─ CustomerId: 91190794
    ├─ Status: "NOT_SHIPPED"
    ├─ OrderCreatedDate: 2025-01-15
    ├─ Currency: "TRY"
    ├─ Total: 7500.00
    └─ SalesOrderRows:
       ├─ VariantId: 987654
       ├─ Quantity: 50
       ├─ PricePerUnit: 150.00
       └─ Total: 7500.00
         │
         ▼
    KatanaSalesOrderSyncWorker (Her 5 dakikada bir)
         │
         ├─ Katana API'den son 7 günün siparişlerini çek
         ├─ SalesOrders tablosuna kaydet (duplicate check)
         ├─ SalesOrderLines tablosuna kaydet
         └─ PendingStockAdjustments oluştur (Admin onayı için)
         │
         ▼
    Admin Paneli
         │
         ├─ [Admin Onayı] → Katana'ya stok ekleme
         │  ├─ Ürün var mı kontrol et
         │  ├─ Ürün VARSA: Stok güncelle
         │  └─ Ürün YOKSA: Yeni ürün oluştur
         │
         └─ [Kozaya Senkronize] → Luca'ya fatura gönderme
            │
            ├─ Müşteri bilgisi kontrol
            ├─ Sipariş satırları kontrol
            ├─ Mapping: SalesOrder → LucaInvoice
            └─ HTTP POST /koza/api/fatura/create
         │
         ▼
    LUCA FATURA
    {
      "belgeSeri": "EFA2025",
      "belgeNo": "SO-001",
      "belgeTarihi": "15/01/2025",
      "cariKodu": "CUST_1234567890",
      "cariTanim": "ABC Tekstil Ltd.",
      "paraBirimKod": "TRY",
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
```

### 2. Satınalma Siparişi Akışı

```
MANUEL SATINALMA SİPARİŞİ OLUŞTURMA
    │
    ├─ PoNumber: "PO-001"
    ├─ SupplierId: 123
    ├─ Status: "Pending"
    └─ Items: [...]
         │
         ▼
    Admin Paneli - Durum Güncelleme
         │
         ├─ [Durum: Approved]
         │  └─ Arka planda Katana'ya ürün ekleme/güncelleme
         │
         ├─ [Durum: Received]
         │  └─ StockMovement kayıtları oluştur
         │
         └─ [Kozaya Senkronize]
            └─ Luca'ya FATURA olarak gönder
         │
         ▼
    LUCA FATURA (Alış Faturası)
    {
      "belgeSeri": "EFA2025",
      "belgeNo": "PO-001",
      "belgeTarihi": "24/12/2025",
      "faturaTur": "2",  // 2 = Alış
      "cariKodu": "SUPP_123",
      "detayList": [...]
    }
```

### 3. Sipariş Onay Mekanizması

```
┌─────────────────────────────────────────────────────────┐
│ KATANA'DAN GELEN SİPARİŞ                                │
│ (KatanaSalesOrderSyncWorker tarafından çekilen)         │
└────────────────────┬────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────┐
│ PendingStockAdjustment (Status: Pending)                │
│ - Admin panelinde görünür                               │
│ - Onay bekliyor                                         │
└────────────────────┬────────────────────────────────────┘
                     │
                     ├─ [Admin Onayı]
                     │  │
                     │  ├─ Katana'ya stok ekleme/güncelleme
                     │  │  ├─ Ürün var mı kontrol
                     │  │  ├─ Ürün VARSA: Stok = Mevcut + Sipariş Miktarı
                     │  │  └─ Ürün YOKSA: Yeni ürün oluştur
                     │  │
                     │  └─ Status: Approved
                     │
                     └─ [Kozaya Senkronize]
                        │
                        ├─ Müşteri bilgisi kontrol
                        ├─ Sipariş satırları kontrol
                        ├─ Luca'ya fatura gönder
                        └─ IsSyncedToLuca: true
```

---

## ⚠️ Kritik Sorunlar ve Çözümler

### 1. Ürün İsmi Boş Gelme Sorunu

**Sorun**:

```
Katana API Response:
{
  "id": 123456,
  "sku": "PIPE-001",
  "name": "",  // 🔥 BOŞ!
  "price": 150.00
}
```

**Sonuç**:

- Mapper SKU'yu kullanır: "PIPE-001"
- Luca'da: kartAdi = "PIPE-001"
- Luca'da mevcut: kartAdi = "COOLING WATER PIPE"
- **Sonuç**: Luca yeni versiyon oluşturur (Duplicate!)

**Çözüm**:

```csharp
// Mapper'da kontrol
var name = string.IsNullOrWhiteSpace(product.Name)
    ? sku  // SKU kullan
    : product.Name.Trim();

// ⚠️ UYARI LOG'U
if (string.IsNullOrWhiteSpace(product.Name))
{
    Console.WriteLine($"⚠️ MAPPING HATASI: Katana'dan Name boş geldi, SKU kullanılıyor: {sku}");
}
```

**Kalıcı Çözüm**:

- Katana API'sinden `name` alanını dolu gönder
- Veya database'den ürün ismini çek

---

### 2. Encoding Sorunu (Ø karakteri)

**Sorun**:

```
Katana: "COOLING WATER PIPE Ø25mm"
Luca (ISO-8859-9): "COOLING WATER PIPE ??25mm"
```

**Sonuç**:

- Luca'da mevcut: "COOLING WATER PIPE Ø25mm"
- Gönderilen: "COOLING WATER PIPE O25MM"
- **Sonuç**: Luca yeni versiyon oluşturur (Duplicate!)

**Çözüm**:

```csharp
// Encoding normalize et
var name = NormalizeProductNameForLuca(rawName);
// "Ø" → "O"
// "ø" → "o"
// Türkçe karakterler korunur (Ü, Ö, Ş, Ç, Ğ, İ)
```

---

### 3. Versiyonlu SKU Sorunu

**Sorun**:

```
Katana: SKU = "PIPE-V2", Barcode = "8690123456789"
Luca'da mevcut: SKU = "PIPE", Barcode = "8690123456789"

Gönderilen: kartKodu = "PIPE-V2", barkod = "8690123456789"
```

**Sonuç**:

- Luca: "Duplicate Barcode" hatası
- Senkronizasyon başarısız

**Çözüm**:

```csharp
// Versiyonlu SKU'lar için barkod NULL gönder
bool isVersionedSku = Regex.IsMatch(sku, @"-V\d+$", RegexOptions.IgnoreCase);
if (isVersionedSku)
{
    barcodeToSend = null;  // 🔥 Barkod NULL
}
```

---

### 4. Kategori Mapping Sorunu

**Sorun**:

```
Katana: Category = "Pipes"
Mapping: "Pipes" → "220" (Luca kategori kodu)

Ama mapping tablosu boş veya yanlış!
```

**Sonuç**:

- Luca'ya kategori kodu gönderilmez (NULL)
- Luca'da varsayılan kategori kullanılır

**Çözüm**:

```csharp
// Mapping Önceliği:
// 1. Database PRODUCT_CATEGORY tablosu
// 2. appsettings.json CategoryMapping
// 3. DefaultKategoriKodu
// 4. NULL (Luca kabul eder)

var category = null;
if (productCategoryMappings?.TryGetValue("PIPES", out var mapped) == true)
{
    category = mapped;  // "220"
}
```

---

### 5. Ölçü Birimi Mapping Sorunu

**Sorun**:

```
Katana: Unit = "pcs"
Mapping: "pcs" → 5 (Luca ADET ID)

Ama mapping tablosu boş!
```

**Sonuç**:

- AutoMapUnit() fallback kullanılır
- Yanlış ölçü birimi gönderilir

**Çözüm**:

```csharp
// appsettings.json'da UnitMapping tanımla
"UnitMapping": {
  "pcs": 5,
  "kg": 1,
  "m": 2,
  "l": 3
}
```

---

## 📊 Veri Tutarlılığı

### 1. Duplicate Prevention

**Luca Tarafında**:

- Stok kartı: `kartKodu` ile duplicate kontrol
- Cari kart: `cariKodu` ile duplicate kontrol
- Fatura: `belgeSeri + belgeNo` ile duplicate kontrol

**Katana Tarafında**:

- Sipariş: `KatanaOrderId` ile duplicate kontrol
- Sipariş kalemi: `ExternalOrderId|SKU|Quantity` composite key

### 2. Veri Senkronizasyon Durumu

```csharp
// Product tablosunda
public class Product
{
    public long? LucaId { get; set; }              // Luca stok kartı ID
    public bool IsSyncedToLuca { get; set; }       // Senkronize edildi mi?
    public DateTime? LastSyncAt { get; set; }      // Son senkronizasyon tarihi
    public string? LastSyncError { get; set; }     // Son hata mesajı
}

// SalesOrder tablosunda
public class SalesOrder
{
    public long? LucaOrderId { get; set; }         // Luca fatura ID
    public bool IsSyncedToLuca { get; set; }       // Senkronize edildi mi?
    public DateTime? LastSyncAt { get; set; }      // Son senkronizasyon tarihi
    public string? LastSyncError { get; set; }     // Son hata mesajı
}
```

### 3. Hata Yönetimi

```
Senkronizasyon Hatası
    │
    ├─ LastSyncError: "Duplicate Barcode"
    ├─ IsSyncedToLuca: false
    ├─ LastSyncAt: 2025-12-24T10:30:00Z
    │
    └─ Retry Mekanizması
       ├─ Manual: Admin panelinden "Retry" butonu
       ├─ Otomatik: Sonraki senkronizasyon döngüsünde
       └─ Batch: /api/sync/retry-failed endpoint
```

---

## 🔐 Güvenlik ve Performans

### 1. Authentication

```
Katana API:
- Token-based (JWT)
- Timeout: 60 saniye

Luca API:
- Cookie-based (JSESSIONID)
- Session timeout: 20 dakika
- Manual session cookie desteği
```

### 2. Rate Limiting

```
Katana API:
- 50ms delay (pagination)

Luca API:
- 350-1000ms throttling
- Batch processing: 100 kayıt/batch
```

### 3. Retry Policy

```
Başarısız istek:
- 1. Deneme: Hemen
- 2. Deneme: 2 saniye sonra
- 3. Deneme: 4 saniye sonra
- 4. Deneme: 6 saniye sonra
- Başarısız: Hata kaydı ve manual retry
```

---

## 📝 Özet

### Katana → Luca Akışı

| Veri Tipi             | Katana Endpoint           | Luca Endpoint                | Durum        |
| --------------------- | ------------------------- | ---------------------------- | ------------ |
| Ürünler               | `/api/v1/products`        | `/koza/api/stokKarti/create` | ✅ Çalışıyor |
| Müşteriler            | `/api/v1/customers`       | `/koza/api/musteri/create`   | ✅ Çalışıyor |
| Satış Siparişleri     | `/api/v1/sales_orders`    | `/koza/api/fatura/create`    | ✅ Çalışıyor |
| Satınalma Siparişleri | `/api/v1/purchase_orders` | `/koza/api/fatura/create`    | ✅ Çalışıyor |

### Luca → Katana Akışı

| Veri Tipi            | Durum  | Açıklama                                  |
| -------------------- | ------ | ----------------------------------------- |
| Ürün Güncellemeleri  | ❌ YOK | Tek yönlü senkronizasyon                  |
| Fiyat Değişiklikleri | ❌ YOK | Katana master sistem                      |
| Stok Hareketleri     | ❌ YOK | Luca'da yapılan değişiklikler geri gelmez |

### Kritik Noktalar

1. ✅ **Ürün İsmi**: Boş gelirse SKU kullanılır (UYARI!)
2. ✅ **Encoding**: Ø karakteri O'ya dönüştürülür
3. ✅ **Versiyonlu SKU**: Barkod NULL gönderilir
4. ✅ **Kategori Mapping**: Database → appsettings → Default
5. ✅ **Ölçü Birimi**: Mapping → AutoMap → Default
6. ✅ **Duplicate Prevention**: Luca tarafında yapılır
7. ✅ **Hata Yönetimi**: Detaylı logging ve retry mekanizması

---

**Rapor Tarihi**: 24 Aralık 2025
**Versiyon**: 2.0
**Hazırlayan**: Kiro AI Assistant
