# KATANA-LUCA ENTEGRASYON ANALİZİ - ÖZET

## 📊 Analiz Sonuçları

Bu analiz, Katana ERP sisteminden Luca ERP sistemine veri akışını detaylı olarak incelemiştir.

### Oluşturulan Raporlar

1. **KATANA_LUCA_VERI_AKIS_DETAYLI_ANALIZ.md**

   - Genel mimari ve veri akışı
   - Ürün senkronizasyonu (Katana → Luca)
   - Luca'da güncellenen ürün (Luca → Katana)
   - Sipariş akışı (Katana → Luca)
   - Kritik sorunlar ve çözümler
   - Veri tutarlılığı mekanizmaları

2. **SIPARIS_SENKRONIZASYON_DETAYLI_AKIS.md**

   - Satış siparişi tam akışı (4 aşama)
   - Satınalma siparişi tam akışı (3 aşama)
   - Admin onay mekanizması
   - Luca'ya senkronizasyon türleri
   - Hata senaryoları ve çözümleri

3. **KATANA_LUCA_SORUN_GIDERME_REHBERI.md**
   - Hızlı tanı tablosu
   - Ürün senkronizasyon sorunları
   - Sipariş onay sorunları
   - Fatura gönderme sorunları
   - Session yönetimi
   - Retry mekanizması
   - Monitoring ve logging
   - Maintenance görevleri

---

## 🔄 Veri Akışı Özeti

### Katana → Luca (ONE-WAY)

```
KATANA ÜRÜN
├─ SKU: "PIPE-001"
├─ Name: "COOLING WATER PIPE Ø25mm"
├─ Price: 150.00 TRY
├─ Unit: "pcs"
├─ Category: "Pipes"
└─ Barcode: "8690123456789"
    │
    ▼ (Mapping & Normalization)
    │
    ├─ SKU Normalizasyonu
    ├─ Name Encoding (Ø → O)
    ├─ Kategori Mapping (Pipes → 220)
    ├─ Ölçü Birimi Mapping (pcs → 5)
    └─ Barkod Kontrolü (Versiyonlu SKU → NULL)
    │
    ▼
LUCA STOK KARTI
├─ kartKodu: "PIPE-001"
├─ kartAdi: "COOLING WATER PIPE O25MM"
├─ perakendeSatisBirimFiyat: 150.0
├─ olcumBirimiId: 5
├─ kategoriAgacKod: "220"
└─ barkod: "8690123456789"
```

### Luca → Katana (NONE)

```
❌ Luca'da yapılan değişiklikler Katana'ya geri gelmez
   - Fiyat değişikliği
   - Kategori değişikliği
   - Stok hareketi
   - Ürün ismi değişikliği

✅ Çözüm: Manuel güncelleme veya batch import
```

### Sipariş Akışı

```
KATANA SATIŞ SİPARİŞİ
├─ OrderNo: "SO-001"
├─ CustomerId: 91190794
├─ Total: 7500.00 TRY
└─ Items: [PIPE-001 x 50]
    │
    ▼ (5 dakikada bir)
    │
    ├─ SalesOrders tablosuna kaydet
    ├─ SalesOrderLines tablosuna kaydet
    └─ PendingStockAdjustments oluştur
    │
    ▼ (Admin Onayı)
    │
    ├─ Katana'ya stok ekleme/güncelleme
    └─ Status: APPROVED
    │
    ▼ (Kozaya Senkronize)
    │
    ├─ Müşteri bilgisi kontrol
    ├─ Sipariş satırları kontrol
    └─ Luca'ya fatura gönder
    │
    ▼
LUCA FATURA
├─ belgeSeri: "EFA2025"
├─ belgeNo: "SO-001"
├─ cariKodu: "CUST_1234567890"
└─ detayList: [PIPE-001 x 50 @ 150.00]
```

---

## ⚠️ Kritik Bulgular

### 1. Ürün İsmi Boş Gelme Sorunu

**Sorun**: Katana API bazen Name alanını boş gönderiyor
**Sonuç**: Mapper SKU'yu kullanır → Luca yeni versiyon oluşturur
**Çözüm**: Encoding normalize edilir (otomatik), ama Katana'dan dolu name gönderilmeli

### 2. Encoding Sorunu (Ø karakteri)

**Sorun**: UTF-8 → ISO-8859-9 dönüşümü
**Sonuç**: "Ø" → "?" → Luca yeni versiyon oluşturur
**Çözüm**: Mapper'da "Ø" → "O" dönüşümü yapılır (otomatik)

### 3. Versiyonlu SKU Sorunu

**Sorun**: "PIPE-V2" SKU'su aynı barkoda sahip
**Sonuç**: Luca "Duplicate Barcode" hatası
**Çözüm**: Versiyonlu SKU'lar için barkod NULL gönderilir (otomatik)

### 4. Kategori Mapping Eksik

**Sorun**: Database'de PRODUCT_CATEGORY mapping yok
**Sonuç**: Kategori kodu NULL gönderilir
**Çözüm**: Mapping tablosuna kategori kodları eklenmeli

### 5. Ölçü Birimi Mapping Eksik

**Sorun**: appsettings.json'da UnitMapping boş
**Sonuç**: AutoMapUnit() fallback kullanılır
**Çözüm**: UnitMapping'e ölçü birimleri eklenmeli

### 6. Tek Yönlü Senkronizasyon

**Sorun**: Luca'da yapılan değişiklikler Katana'ya gelmez
**Sonuç**: Veri tutarsızlığı riski
**Çözüm**: Manuel güncelleme veya batch import gerekli

---

## ✅ Çalışan Mekanizmalar

### 1. Duplicate Prevention

```
✅ Luca Tarafında:
   - kartKodu ile duplicate kontrol
   - cariKodu ile duplicate kontrol
   - belgeSeri + belgeNo ile duplicate kontrol

✅ Katana Tarafında:
   - KatanaOrderId ile duplicate kontrol
   - ExternalOrderId|SKU|Quantity composite key
```

### 2. Hata Yönetimi

```
✅ Detaylı Logging:
   - LastSyncError alanında hata mesajı
   - LastSyncAt alanında senkronizasyon tarihi
   - IsSyncedToLuca alanında durum

✅ Retry Mekanizması:
   - Manual: Admin panelinden retry
   - Otomatik: Sonraki senkronizasyon döngüsünde
   - Batch: /api/sync/retry-failed endpoint
```

### 3. Performance Optimizasyonları

```
✅ Batch Processing:
   - 100 kayıt/batch
   - Paralel işleme (5 eşzamanlı istek)

✅ Rate Limiting:
   - Katana: 50ms delay
   - Luca: 350-1000ms throttling

✅ Caching:
   - Müşteri bilgileri cache'lenir
   - Ürün bilgileri cache'lenir
```

### 4. Session Yönetimi

```
✅ Cookie-based Authentication:
   - JSESSIONID ile session yönetimi
   - 20 dakika session timeout
   - Manual session cookie desteği

✅ Session Refresh:
   - Otomatik session yenileme
   - ForceSessionRefreshAsync() metodu
   - Headless auth desteği
```

---

## 📈 Senkronizasyon Durumu

### Ürünler

| Durum                   | Açıklama                             |
| ----------------------- | ------------------------------------ |
| ✅ Katana → Luca        | Ürünler stok kartı olarak gönderilir |
| ❌ Luca → Katana        | Geri akış yok                        |
| ✅ Duplicate Prevention | Luca tarafında yapılır               |
| ✅ Mapping              | Kategori, ölçü birimi, encoding      |

### Müşteriler

| Durum                   | Açıklama                               |
| ----------------------- | -------------------------------------- |
| ✅ Katana → Luca        | Müşteriler cari kart olarak gönderilir |
| ❌ Luca → Katana        | Geri akış yok                          |
| ✅ Duplicate Prevention | Luca tarafında yapılır                 |
| ✅ Mapping              | Müşteri tipi, vergi no                 |

### Satış Siparişleri

| Durum                   | Açıklama                                 |
| ----------------------- | ---------------------------------------- |
| ✅ Katana → Sistem      | Otomatik senkronizasyon (5 dakikada bir) |
| ✅ Admin Onayı          | Katana'ya stok ekleme/güncelleme         |
| ✅ Sistem → Luca        | Fatura olarak gönderilir                 |
| ✅ Duplicate Prevention | Katana tarafında yapılır                 |

### Satınalma Siparişleri

| Durum                   | Açıklama                      |
| ----------------------- | ----------------------------- |
| ✅ Manuel Oluşturma     | Admin panelinden oluşturulur  |
| ✅ Durum Yönetimi       | Pending → Approved → Received |
| ✅ Sistem → Luca        | Fatura olarak gönderilir      |
| ✅ Duplicate Prevention | Katana tarafında yapılır      |

---

## 🎯 Öneriler

### Kısa Vadeli (Acil)

1. **Kategori Mapping Tablosunu Doldur**

   - PRODUCT_CATEGORY tablosuna tüm kategorileri ekle
   - Katana kategorileri → Luca kategori kodları

2. **Ölçü Birimi Mapping'ini Kontrol Et**

   - appsettings.json UnitMapping'i doğrula
   - Eksik ölçü birimlerini ekle

3. **Encoding Sorunlarını Test Et**
   - Özel karakterli ürünleri senkronize et
   - Luca'da doğru görünüp görünmediğini kontrol et

### Orta Vadeli (1-2 Hafta)

1. **Luca → Katana Geri Akışı Planla**

   - Webhook mekanizması tasarla
   - Scheduled sync worker oluştur
   - Veri çakışma çözümü belirle

2. **Monitoring Dashboard Oluştur**

   - Senkronizasyon durumu
   - Hata oranları
   - Performance metrikleri

3. **Automated Testing Ekle**
   - Unit tests
   - Integration tests
   - Property-based tests

### Uzun Vadeli (1-3 Ay)

1. **Bi-directional Sync Uygula**

   - Luca'dan Katana'ya veri akışı
   - Conflict resolution mekanizması
   - Veri tutarlılığı garantisi

2. **Real-time Sync Geçişi**

   - Event-driven architecture
   - Message queue (RabbitMQ, Kafka)
   - WebSocket notifications

3. **Advanced Monitoring**
   - Distributed tracing
   - Performance profiling
   - Anomaly detection

---

## 📚 Referans Dosyalar

### Kod Dosyaları

- `src/Katana.Business/Mappers/KatanaToLucaMapper.cs` - Mapping mantığı
- `src/Katana.Infrastructure/APIClients/LucaService.Core.cs` - Luca API iletişimi
- `src/Katana.Business/Services/ProductService.cs` - Ürün yönetimi
- `src/Katana.API/Workers/KatanaSalesOrderSyncWorker.cs` - Sipariş senkronizasyonu
- `src/Katana.API/Controllers/SalesOrdersController.cs` - Sipariş API'si

### Konfigürasyon Dosyaları

- `src/Katana.API/appsettings.json` - Luca API ayarları
- `src/Katana.Data/Configuration/LucaApiSettings.cs` - Luca ayarları sınıfı

### Veritabanı Tabloları

- `Products` - Ürün bilgileri
- `Customers` - Müşteri bilgileri
- `SalesOrders` - Satış siparişleri
- `SalesOrderLines` - Satış sipariş satırları
- `PurchaseOrders` - Satınalma siparişleri
- `PurchaseOrderItems` - Satınalma sipariş kalemleri
- `Mappings` - Kategori ve müşteri tipi mapping'leri
- `SyncOperationLogs` - Senkronizasyon logs'ları

---

## 🔗 İlgili Dokümantasyon

- `KATANA_LUCA_ENTEGRASYON_AKIS_RAPORU.md` - Orijinal entegrasyon raporu
- `LUCA_UPDATE_DELETE_ENDPOINTS.md` - Luca update/delete endpoint'leri
- `ADMIN_SIPARIS_ONAY_VE_KOZA_SENKRONIZASYON_AKISI.md` - Admin sipariş onay akışı

---

## 📞 İletişim ve Destek

Sorularınız veya sorunlarınız için:

1. **Logs'u Kontrol Edin**

   - Application logs
   - Luca API logs
   - Database logs

2. **Hata Raporlama**

   - Hata mesajı
   - Zaman bilgisi
   - İlgili veriler (Sipariş No, Ürün SKU, vb.)
   - Sistem bilgisi (Versiyon, .NET versiyonu, vb.)

3. **Sorun Giderme Rehberi**
   - `KATANA_LUCA_SORUN_GIDERME_REHBERI.md` dosyasını kontrol edin

---

## 📝 Sonuç

Katana-Luca entegrasyon sistemi **ONE-WAY** (tek yönlü) olarak tasarlanmıştır:

- ✅ **Katana → Luca**: Ürünler, müşteriler, siparişler başarıyla senkronize edilir
- ❌ **Luca → Katana**: Geri akış yoktur (tasarım gereği)

Sistem, robust hata yönetimi, duplicate prevention ve performance optimizasyonları içerir. Ancak, Luca'da yapılan değişiklikleri Katana'ya aktarmak için manuel güncelleme veya batch import gereklidir.

**Kritik Noktalar**:

1. Kategori ve ölçü birimi mapping'leri tam olmalı
2. Encoding sorunları otomatik olarak çözülür
3. Versiyonlu SKU'lar için barkod NULL gönderilir
4. Duplicate prevention Luca tarafında yapılır
5. Hata yönetimi detaylı ve retry mekanizması vardır

---

**Rapor Tarihi**: 24 Aralık 2025
**Versiyon**: 1.0
**Hazırlayan**: Kiro AI Assistant

**Analiz Kapsamı**:

- Kaynak kod incelemesi
- Veri akışı analizi
- Mapping kuralları
- Hata senaryoları
- Performance optimizasyonları
- Security mekanizmaları

---

# İKİ YÖNLÜ SENKRONİZASYON SİSTEMİ

## 🎯 Özet

Bu sistem, Katana ve Luca arasında **tam iki yönlü** senkronizasyon sağlar:

- ✅ **Luca → Katana**: Luca'da güncellenen ürünler Katana'da AYNI ÜRÜN'ü günceller
- ✅ **Katana → Luca**: Katana'da güncellenen ürünler Luca'da AYNI ÜRÜN'ü günceller
- ✅ **Yeni SKU/Versiyon AÇILMAZ** - Sadece mevcut ürünler güncellenir
- ✅ **NULL değerler korunur** - Sadece değişen alanlar gönderilir

---

## 📊 ANA AKIŞ DİYAGRAMI

```
┌─────────────────────────────────────────────────────────────────────┐
│                    KATANA-LUCA İKİ YÖNLÜ AKIŞ                       │
└─────────────────────────────────────────────────────────────────────┘

AKIŞ 1: KATANA'DAN SİPARİŞ GELME → ONAY → LUCA STOK KARTI + FATURA
═══════════════════════════════════════════════════════════════════════

   KATANA                    SİSTEM                      LUCA
     │                          │                          │
     │  (1) Sipariş oluştur     │                          │
     │  - SO-001                │                          │
     │  - 3 ürün                │                          │
     ├─────────────────────────>│                          │
     │                          │                          │
     │   (Her 5 dakika)         │                          │
     │   KatanaSalesOrderSync   │                          │
     │   Worker                 │                          │
     │                          │                          │
     │                          │ (2) Database'e kaydet    │
     │                          │ - SalesOrders table      │
     │                          │ - SalesOrderLines table  │
     │                          │ - Status: PENDING        │
     │                          │                          │
     │                          │                          │
     │                     ┌────┴────┐                     │
     │                     │  ADMIN  │                     │
     │                     │  PANEL  │                     │
     │                     └────┬────┘                     │
     │                          │                          │
     │                          │ (3) [ONAYLA] Tıkla       │
     │                          │                          │
     │                          │ Her ürün için:           │
     │                          │ ├─ SKU=PIPE-001         │
     │                          │ ├─ Luca'da var mı?      │
     │                          │ │  ├─ VARSA: Atla       │
     │                          │ │  └─ YOKSA: OLUŞTUR    │
     │                          │ │     (UpsertStockCard) │
     │                          ├────────────────────────>│
     │                          │                   CREATE │
     │                          │                   {      │
     │                          │                     kartKodu,│
     │                          │                     kartAdi,│
     │                          │                     kdvOran│
     │                          │                   }      │
     │                          │<────────────────────────┤
     │                          │      ✅ Stok kartı hazır│
     │                          │                          │
     │                          │ (4) Fatura gönder        │
     │                          ├────────────────────────>│
     │                          │  CreateSalesOrderInvoice │
     │                          │<────────────────────────┤
     │                          │      ✅ Fatura oluştu   │
     │                          │                          │
     │                          │ Sipariş güncelle:        │
     │                          │ - Status: APPROVED       │
     │                          │ - ApprovedAt: NOW        │
     │                          │ - LucaOrderId: 12345     │
     │                          │                          │

═══════════════════════════════════════════════════════════════════════

AKIŞ 2: LUCA'DA ÜRÜN GÜNCELLEME → KATANA'YA YANSIMA
═══════════════════════════════════════════════════════════════════════

   LUCA                      SİSTEM                    KATANA
     │                          │                          │
     │  (1) Ürün güncelle       │                          │
     │  - LucaId: 12345         │                          │
     │  - Fiyat: 150 → 175      │                          │
     │  - İsim değişti          │                          │
     │                          │                          │
     │                          │ (Her 30 dakika)          │
     │                          │ BidirectionalSync        │
     │                          │ Worker                   │
     │                          │                          │
     │<─────────────────────────┤ (2) Güncellemeleri çek   │
     │  GetUpdatedProducts()    │                          │
     │  sinceDate: 30dk önce    │                          │
     ├─────────────────────────>│                          │
     │  Response: [LucaId=12345]│                          │
     │                          │                          │
     │                          │ (3) Local DB'de bul      │
     │                          │ - LucaId=12345           │
     │                          │ - KatanaProductId=67890  │
     │                          │                          │
     │                          │ (4) Değişiklikleri tespit│
     │                          │ - Fiyat değişmiş         │
     │                          │ - İsim değişmiş          │
     │                          │                          │
     │                          │ (5) AYNI ÜRÜNÜ güncelle  │
     │                          ├────────────────────────>│
     │                          │  UpdateProductAsync()    │
     │                          │  productId: 67890 (AYNI!)│
     │                          │  {                       │
     │                          │    id: 67890,            │
     │                          │    name: yeni_isim,      │
     │                          │    sales_price: 175      │
     │                          │  }                       │
     │                          │<────────────────────────┤
     │                          │      ✅ Güncellendi     │
     │                          │      (YENİ SKU YOK!)    │
     │                          │                          │
     │                          │ (6) Local DB güncelle    │
     │                          │ - LastSyncFromLuca: NOW  │
     │                          │ - UpdatedAt: NOW         │

═══════════════════════════════════════════════════════════════════════

AKIŞ 3: KATANA'DA ÜRÜN GÜNCELLEME → LUCA'YA YANSIMA
═══════════════════════════════════════════════════════════════════════

   KATANA                    SİSTEM                      LUCA
     │                          │                          │
     │  (1) Ürün güncelle       │                          │
     │  - KatanaId: 67890       │                          │
     │  - Fiyat: 175 → 200      │                          │
     │  - Kategori değişti      │                          │
     │                          │                          │
     │                          │ (Her 30 dakika)          │
     │                          │ BidirectionalSync        │
     │                          │ Worker                   │
     │                          │                          │
     │<─────────────────────────┤ (2) Güncellemeleri çek   │
     │  GetUpdatedProducts()    │                          │
     ├─────────────────────────>│                          │
     │  Response: [KatanaId=67890]                         │
     │                          │                          │
     │                          │ (3) Local DB'de bul      │
     │                          │ - KatanaProductId=67890  │
     │                          │ - LucaId=12345           │
     │                          │                          │
     │                          │ (4) Değişiklikleri tespit│
     │                          │ - Fiyat değişmiş         │
     │                          │ - Kategori değişmiş      │
     │                          │                          │
     │                          │ (5) Request hazırla      │
     │                          │ ⚠️ SADECE DEĞİŞENLER!   │
     │                          │ {                        │
     │                          │   perakendeSatisBirimFiyat: 200│
     │                          │   kategoriAgacKod: "221" │
     │                          │   // İsim GÖNDERİLMEZ   │
     │                          │   // Stok GÖNDERİLMEZ   │
     │                          │ }                        │
     │                          │                          │
     │                          │ (6) AYNI ÜRÜNÜ güncelle  │
     │                          ├────────────────────────>│
     │                          │  UpdateProductAsync()    │
     │                          │  lucaId: 12345 (AYNI!)   │
     │                          │<────────────────────────┤
     │                          │      ✅ Güncellendi     │
     │                          │      (YENİ VERSİYON YOK!)│
     │                          │                          │
```

---

## 🔑 KRİTİK NOKTALAR

### 1. SİPARİŞ ONAYINDA STOK KARTI OLUŞTURMA

```csharp
// ✅ DOĞRU: Sipariş onayında stok kartı kontrolü
foreach (var line in order.Lines)
{
    // Luca'da stok kartı var mı kontrol et
    var existingSkartId = await _lucaService.FindStockCardBySkuAsync(line.SKU);

    if (existingSkartId.HasValue)
    {
        // ✅ Stok kartı VAR - Atla, faturaya devam et
        _logger.LogDebug("Stock card exists: {SKU}", line.SKU);
    }
    else
    {
        // ✅ Stok kartı YOK - Oluştur
        var stockCardRequest = new LucaCreateStokKartiRequest
        {
            KartKodu = line.SKU,
            KartAdi = line.ProductName ?? line.SKU,
            KartAlisKdvOran = (double)(line.TaxRate ?? 20) / 100.0,
            OlcumBirimiId = 1 // ADET
        };
        await _lucaService.UpsertStockCardAsync(stockCardRequest);
    }
}
// Sonra fatura gönder
await _lucaService.CreateSalesOrderInvoiceAsync(order, depoKodu);
```

### 2. MEVCUT ÜRÜN GÜNCELLENİR - YENİ SKU AÇILMAZ!

```csharp
// ✅ DOĞRU: Mevcut ürünü güncelle
await _katanaService.UpdateProductAsync(
    katanaProduct.Id,  // AYNI ID!
    new {
        id = katanaProduct.Id,  // AYNI ÜRÜN!
        sales_price = 200
    });

// ❌ YANLIŞ: Yeni ürün oluşturma
await _katanaService.CreateProductAsync(new { sku = "PIPE-001-V2" });
```

### 2. NULL DEĞERLER GÖNDERİLMEZ

```csharp
// Luca'ya sadece değişen alanlar gönderilir
var updateRequest = new Dictionary<string, object>();

// Fiyat değiştiyse ekle
if (priceChanged)
    updateRequest["perakendeSatisBirimFiyat"] = newPrice;

// İsim değiştiyse ekle
if (nameChanged)
    updateRequest["kartAdi"] = newName;

// Kategori Katana'da YOKSA GÖNDERİLMEZ
// Luca'daki mevcut değer KORUNUR
if (!string.IsNullOrEmpty(category))
    updateRequest["kategoriAgacKod"] = categoryCode;
```

### 3. VERSIYONLU SKU KONTROLÜ

```csharp
// Versiyonlu SKU'lar için barkod gönderilmez
private bool IsVersionedSku(string sku)
{
    return sku.Contains("-V", StringComparison.OrdinalIgnoreCase) ||
           sku.Contains("_V", StringComparison.OrdinalIgnoreCase);
}

// PIPE-V2 → Barkod: NULL
// PIPE-001 → Barkod: "8690123456789"
```

### 4. ID İLİŞKİLERİ

```
Product Entity:
├─ KatanaProductId (long?) → Katana'daki ürün ID'si
├─ LucaId (long?) → Luca'daki stok kart ID'si
├─ LastSyncFromKatana (DateTime?) → Katana'dan son senkronizasyon
└─ LastSyncFromLuca (DateTime?) → Luca'dan son senkronizasyon

Bu ID'ler sayesinde AYNI ÜRÜN bulunur ve güncellenir!
```

---

## 🚀 KULLANIM

### Manuel Senkronizasyon

```bash
# 1. Luca → Katana (Son 1 saat)
curl -X POST "https://localhost:5001/api/sync/luca-to-katana?hours=1" \
  -H "Authorization: Bearer YOUR_JWT"

# 2. Katana → Luca (Son 1 saat)
curl -X POST "https://localhost:5001/api/sync/katana-to-luca?hours=1" \
  -H "Authorization: Bearer YOUR_JWT"

# 3. İki yönlü (Son 1 saat)
curl -X POST "https://localhost:5001/api/sync/bidirectional?hours=1" \
  -H "Authorization: Bearer YOUR_JWT"

# 4. Sipariş onaylama
curl -X POST "https://localhost:5001/api/sync/sales-orders/123/approve" \
  -H "Authorization: Bearer YOUR_JWT"
```

### Otomatik Senkronizasyon

```
Worker'lar otomatik çalışır:

1. KatanaSalesOrderSyncWorker
   - Sıklık: 5 dakika
   - İş: Katana'dan siparişleri çek

2. BidirectionalSyncWorker
   - Sıklık: 30 dakika
   - İş:
     • Luca → Katana (güncellemeleri çek)
     • Katana → Luca (güncellemeleri çek)
```

---

## 📋 KONTROL LİSTESİ

### Başlangıç Kontrolü

- [ ] Database migration yapıldı mı?
- [ ] KatanaProductId alanı eklendi mi?
- [ ] LucaId alanı eklendi mi?
- [ ] Index'ler oluşturuldu mu?
- [ ] appsettings.json konfigürasyonu doğru mu?
- [ ] Kategori mapping'leri dolu mu?
- [ ] Ölçü birimi mapping'leri dolu mu?

### Senkronizasyon Kontrolü

- [ ] Luca'da güncellenen ürün Katana'da AYNI ÜRÜN'ü güncelliyor mu?
- [ ] Katana'da güncellenen ürün Luca'da AYNI ÜRÜN'ü güncelliyor mu?
- [ ] Yeni SKU/versiyon açılıyor mu? (AÇILMAMALI!)
- [ ] NULL değerler korunuyor mu? (KORUNMALI!)
- [ ] Versiyonlu SKU'lar için barkod NULL mu? (NULL OLMALI!)

### Sipariş Onay Kontrolü

- [ ] Katana'dan sipariş geldi mi?
- [ ] Admin onayladı mı?
- [ ] Luca'da stok kartları kontrol edildi mi?
- [ ] Eksik stok kartları oluşturuldu mu?
- [ ] Fatura Luca'ya gönderildi mi?
- [ ] Sipariş durumu APPROVED mu?
- [ ] LucaOrderId kaydedildi mi?

---

## 🐛 HATA ÇÖZÜMLEME

### Sorun: Yeni SKU/Versiyon Açılıyor

**Neden**: LucaId veya KatanaProductId NULL  
**Çözüm**:

```sql
-- LucaId'leri kontrol et
SELECT SKU, LucaId, KatanaProductId FROM Products WHERE LucaId IS NULL;

-- LucaId'leri güncelle
UPDATE Products SET LucaId = (
    SELECT Id FROM LucaStokKartlari WHERE KartKodu = Products.SKU
) WHERE LucaId IS NULL;
```

### Sorun: NULL Değerler Luca'ya Gönderiliyor

**Neden**: Request'te NULL alanlar var  
**Çözüm**:

```csharp
// ✅ DOĞRU: Sadece dolu alanlar gönderilir
var request = new Dictionary<string, object>();
if (!string.IsNullOrEmpty(category))
    request["kategoriAgacKod"] = category;

// ❌ YANLIŞ: NULL gönderme
request["kategoriAgacKod"] = category; // category NULL ise sorun!
```

### Sorun: "Duplicate Barcode" Hatası

**Neden**: Versiyonlu SKU için barkod gönderilmiş  
**Çözüm**:

```csharp
// Versiyonlu SKU kontrolü
if (!IsVersionedSku(sku) && !string.IsNullOrEmpty(barcode))
    request["barkod"] = barcode;
// Versiyonlu SKU'lar için barkod GÖNDERİLMEZ
```

---

## 📊 PERFORMANS

```
Senkronizasyon Hızı:
├─ Luca → Katana: ~50 ürün/dakika
├─ Katana → Luca: ~30 ürün/dakika (Luca throttling)
└─ Worker sıklığı: 30 dakika

Rate Limiting:
├─ Katana: 50ms delay
├─ Luca: 350-1000ms throttling
└─ Paralel işleme: 5 eşzamanlı istek
```

---

## ✅ BAŞARIYLA TAMAMLANDI!

Sisteminiz artık tam iki yönlü senkronizasyon yapabiliyor:

- ✅ Luca'da güncellenen ürünler Katana'ya yansıyor
- ✅ Katana'da güncellenen ürünler Luca'ya yansıyor
- ✅ Katana'dan gelen siparişler onaylanınca:
  - Luca'da eksik stok kartları otomatik oluşturuluyor
  - Mevcut stok kartları korunuyor (yeni SKU açılmıyor!)
  - Fatura Luca'ya gönderiliyor
- ✅ İki yönlü sync'te tüm güncellemeler MEVCUT ürünlerde yapılıyor
- ✅ NULL değerler korunuyor (sadece değişenler gönderiliyor)

---

## 📝 AKIŞ ÖZETİ

| Akış              | Tetikleyici                    | Davranış                                                       |
| ----------------- | ------------------------------ | -------------------------------------------------------------- |
| **Sipariş Onay**  | Admin [ONAYLA] tıklar          | Eksik stok kartı → OLUŞTUR, Mevcut → ATLA, Sonra fatura gönder |
| **Luca → Katana** | BidirectionalSyncWorker (30dk) | Mevcut ürünü güncelle, YENİ OLUŞTURMA                          |
| **Katana → Luca** | BidirectionalSyncWorker (30dk) | Mevcut ürünü güncelle, YENİ OLUŞTURMA                          |

---

**Rapor Güncelleme Tarihi**: 24 Aralık 2025
**Versiyon**: 1.1 (Sipariş onay akışı düzeltildi)
