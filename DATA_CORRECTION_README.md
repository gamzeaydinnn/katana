# 🎯 Veri Düzeltme & Karşılaştırma Sistemi

## ✅ Yapılanlar

### Backend (C# .NET 8)

1. **Entity**: `DataCorrectionLog` - Veri düzeltme kayıtları

   - Kaynak sistem (Katana/Luca), varlık tipi, alan adı
   - Orijinal ve düzeltilmiş değer
   - Onay durumu, senkronizasyon durumu

2. **DTOs**:

   - `DataCorrectionDto`, `CreateCorrectionDto`
   - `ComparisonProductDto` - Katana ↔ Luca karşılaştırma
   - `KatanaProductData`, `LucaProductData`
   - `DataIssue` - Sorun detayları (Critical/Warning/Info)

3. **Service**: `DataCorrectionService`

   - `CompareKatanaAndLucaProductsAsync()` - İki sistemi karşılaştır
   - `CreateCorrectionAsync()` - Düzeltme kaydı oluştur
   - `ApproveCorrectionAsync()` - Admin onayı
   - `ApplyCorrectionToLucaAsync()` - Düzeltmeyi Luca'ya uygula
   - `ApplyCorrectionToKatanaAsync()` - (Placeholder - Katana API write gerekir)

4. **Controller**: `DataCorrectionController`

   - `GET /api/DataCorrection/compare/products` - Karşılaştırma
   - `GET /api/DataCorrection/pending` - Bekleyen düzeltmeler
   - `POST /api/DataCorrection` - Düzeltme oluştur
   - `POST /api/DataCorrection/{id}/approve` - Onayla
   - `POST /api/DataCorrection/{id}/apply-to-luca` - Luca'ya uygula

5. **Database**: Migration uygulandı ✅
   - `DataCorrectionLogs` tablosu oluşturuldu

### Frontend (React + TypeScript)

1. **Component**: `DataCorrectionPanel.tsx`

   - **Tab 1**: Katana ↔ Luca karşılaştırma tablosu
   - **Tab 2**: Bekleyen düzeltmeler listesi
   - Düzeltme dialog'u (alan, değer, sebep)
   - Onaylama ve uygulama butonları

2. **AdminPanel** güncellendi:
   - 5 tab'lı yapı:
     1. Genel Bakış
     2. Katana Ürünleri
     3. **Veri Düzeltme** (YENİ)
     4. Loglar
     5. Ayarlar

## 🎬 Kullanım Senaryosu

### 1. Karşılaştırma Yap

- Admin panelde "Veri Düzeltme" tab'ına git
- "Katana ↔ Luca Karşılaştırma" tab'ı açık gelir
- Sistem otomatik:
  - Katana'dan ürünleri çeker
  - Luca'dan ürünleri çeker
  - SKU bazlı karşılaştırır
  - Farkları gösterir (Fiyat, İsim, Stok, Aktiflik)

### 2. Sorun Tespit Edildi

Örnek sorunlar:

- ❌ **Critical**: Fiyat uyuşmazlığı (Katana: 100₺, Luca: 95₺)
- ⚠️ **Warning**: İsim farklılığı
- ℹ️ **Info**: Stok farkı

### 3. Manuel Düzeltme

- "Düzelt" butonuna tıkla
- Dialog açılır:
  - Alan: "Price"
  - Düzeltilmiş Değer: "100"
  - Sebep: "Katana fiyatı doğru, Luca güncellenecek"
- "Oluştur" - Düzeltme `DataCorrectionLogs`'a kaydedilir

### 4. Admin Onayı

- "Bekleyen Düzeltmeler" tab'ına git
- Düzeltmeyi gör
- "Onayla" butonuna tıkla
- `IsApproved = true` olur

### 5. Sisteme Uygula

- "Luca'ya Uygula" butonuna tıkla
- Service:
  - Luca DB'de SKU'ya göre ürünü bulur
  - Fiyatı günceller (`UpdateProductAsync`)
  - `IsSynced = true` yapar
- ✅ Düzeltme tamamlandı!

## 📊 Veri Akışı

```
┌─────────────┐         ┌──────────────┐
│   KATANA    │ ◄────── │ Admin Panel  │
│   API       │         │              │
└─────────────┘         │ Karşılaştır  │
      ▲                 │ & Düzelt     │
      │                 └──────────────┘
      │                        │
      │                        ▼
      │                ┌──────────────┐
      │                │ Data         │
      │                │ Correction   │
      │                │ Service      │
      │                └──────────────┘
      │                        │
      │                        ▼
      │                ┌──────────────┐
      └────────────────│ Luca DB      │
                       │ (Products)   │
                       └──────────────┘
```

## 🔧 Özellikler

### Şu An Çalışıyor:

✅ Katana → Luca karşılaştırma
✅ Düzeltme kaydı oluşturma
✅ Admin onayı
✅ Luca'ya uygulama (Price, Name, Stock)
✅ Sorun tespit (Critical/Warning/Info)

### Geliştirilecek:

⏳ Katana API'ye write (şu an placeholder)
⏳ Bulk düzeltme (birden fazla ürün)
⏳ Düzeltme geçmişi grafiği

## 🚀 Başlat

### Backend:

```bash
cd c:\Users\GAMZE\Desktop\katana\src\Katana.API
dotnet run --urls "http://localhost:5055"
```

### Frontend:

```bash
cd c:\Users\GAMZE\Desktop\katana\frontend\katana-web
npm start
```

### Test:

1. Admin panele gir
2. "Veri Düzeltme" tab'ına tıkla
3. Karşılaştırma otomatik yüklenecek
4. 50 Katana ürününü göreceksin
5. Sorunları inceleyip düzelt!

## 📝 Not

**Katana'daki 50 ürün** şu şekilde gösterilir:

- "Katana Ürünleri" tab → Direkt Katana API verisi
- "Veri Düzeltme" tab → Katana ↔ Luca karşılaştırması

İkisi de `/api/Products/katana` endpoint'ini kullanır.
