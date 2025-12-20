# Koza Entegrasyon Yapı Düzeltmeleri

## 🔍 Tespit Edilen Sorunlar

### ❌ ÖNCE (Tutarsız Yapı)
```
frontend/katana-web/src/features/integrations/luca-koza/
├── cards/
│   ├── DepoKarti.ts       ✅ Depo kartı var
│   ├── DepoMapper.ts      ✅ Depo mapper var
│   └── DepoService.ts     ✅ Depo service var
└── sync/
    └── LocationSync.ts    ✅ Location sync var

❌ SORUN: Stok Kartı YOKTU!
```

**Backend'de varken frontend'de yoktu:**
- ✅ Backend: `LucaCreateStokKartiRequest` → Var
- ✅ Backend: `SendStockCardsAsync()` → Var
- ✅ Backend: `CreateStockCardAsync()` → Var
- ❌ Frontend: Stok kartı tipi → YOK
- ❌ Frontend: Stok kartı servisi → YOK
- ❌ Frontend: Product mapper → YOK

## ✅ SONRA (Tutarlı Yapı)

### Frontend Yapısı
```
frontend/katana-web/src/features/integrations/luca-koza/
├── cards/
│   ├── StokKarti.ts       ✅ EKLENDI - Tip tanımları
│   ├── StokMapper.ts      ✅ EKLENDI - Katana Product → Koza Stok
│   ├── StokService.ts     ✅ EKLENDI - Backend API çağrıları
│   ├── DepoKarti.ts       ✅ Mevcut
│   ├── DepoMapper.ts      ✅ Mevcut
│   ├── DepoService.ts     ✅ Mevcut
│   └── index.ts           ✅ Güncellendi
├── sync/
│   ├── ProductSync.ts     ✅ EKLENDI - Toplu product sync
│   └── LocationSync.ts    ✅ Mevcut
├── config.ts              ✅ Güncellendi - Stok varsayılanları
└── index.ts               ✅ Güncellendi
```

### Backend Yapısı
```
src/
├── Katana.Business/
│   ├── DTOs/Koza/
│   │   ├── KozaDepoDtos.cs         ✅ Mevcut
│   │   └── KozaStokKartiDtos.cs    ✅ EKLENDI - Basit DTO'lar
│   └── Interfaces/
│       └── ILucaService.cs          ✅ Güncellendi
├── Katana.Infrastructure/
│   └── APIClients/
│       ├── LucaService.cs           ✅ partial class
│       ├── LucaService.Depots.cs    ✅ Mevcut
│       └── LucaService.StockCards.cs ✅ EKLENDI
└── Katana.API/
    └── Controllers/Admin/
        ├── KozaDepotsController.cs      ✅ Mevcut
        └── KozaStockCardsController.cs  ✅ EKLENDI
```

## 🎯 Tutarlılık Sağlandı

### Master Data Eşleşmeleri (Şimdi Her İkisi de Var)

| Katana | Koza | Frontend | Backend |
|--------|------|----------|---------|
| **Product** | **Stok Kartı** | ✅ StokKarti.ts | ✅ KozaStokKartiDtos.cs |
| **Location** | **Depo Kartı** | ✅ DepoKarti.ts | ✅ KozaDepoDtos.cs |
| Customer | Cari Kart | 🔜 TODO | 🔜 TODO |
| Supplier | Cari Kart | 🔜 TODO | 🔜 TODO |

### API Endpoint'leri

#### Depo Kartı ✅
```
GET  /api/admin/koza/depots        → Listele
POST /api/admin/koza/depots/create → Oluştur
```

#### Stok Kartı ✅ EKLENDI
```
GET  /api/admin/koza/stocks        → Listele
POST /api/admin/koza/stocks/create → Oluştur
```

## 📋 Kullanım Örnekleri

### Stok Kartı Oluşturma

**Frontend:**
```typescript
import { stokService, mapKatanaProductToKozaStokKarti } from '@/features/integrations/luca-koza';

const product = {
  id: 123,
  sku: "PRD-001",
  name: "Ürün 1",
  price: 100,
  taxRate: 0.18,
};

const kozaStokKarti = mapKatanaProductToKozaStokKarti(product, {
  kategoriAgacKod: "001",
  olcumBirimiId: 1,  // Adet
});

const result = await stokService.ekle({ stkKart: kozaStokKarti });
```

**Backend:**
```csharp
var request = new KozaCreateStokKartiRequest
{
    StkKart = new KozaStokKartiDto
    {
        KartKodu = "PRD-001",
        KartAdi = "Ürün 1",
        OlcumBirimiId = 1,
        KategoriAgacKod = "001",
        KartTuru = 1,
        KartTipi = 1
    }
};

var result = await _lucaService.CreateStockCardSimpleAsync(request);
```

### Toplu Senkronizasyon

```typescript
import { ProductSyncService } from '@/features/integrations/luca-koza';

const syncService = new ProductSyncService({
  kategoriAgacKod: "001",
  olcumBirimiId: 1,
});

const products = await fetchKatanaProducts();
const results = await syncService.syncProducts(products);

// Mapping'ler oluştur
const stokKartIdMap = syncService.buildStokKartIdMapping(results);
const stokKartKodMap = syncService.buildStokKartKodMapping(results);
```

## 🔧 Yapılan İyileştirmeler

### 1. Tutarlı Klasör Yapısı
- ✅ Tüm kartlar `cards/` altında
- ✅ Tüm sync işlemleri `sync/` altında
- ✅ Her kart için: Tip, Mapper, Service üçlüsü

### 2. Backend-First Yaklaşım
- ✅ Frontend Koza'ya direkt gitmez
- ✅ Tüm işlemler backend üzerinden
- ✅ Güvenli ve merkezi auth yönetimi

### 3. Basitleştirilmiş DTO'lar
- ✅ Frontend için `KozaStokKartiDto` (sadece gerekli alanlar)
- ✅ Backend içinde tam `LucaCreateStokKartiRequest` (tüm alanlar)
- ✅ Mapping katmanı otomatik

### 4. Tip Güvenliği
- ✅ TypeScript interface'leri
- ✅ C# sealed class'lar
- ✅ JsonPropertyName attribute'ları

## ⚠️ Önemli Notlar

### Stok Kartı Zorunlu Alanlar
```typescript
{
  kartKodu: string;        // SKU
  kartAdi: string;         // Ürün adı
  kartTuru: number;        // 1: Ürün, 2: Hizmet
  kartTipi: number;        // 1: Normal
  olcumBirimiId: number;   // Ölçüm birimi (Koza'dan al)
  kategoriAgacKod: string; // Kategori kodu
  kartAlisKdvOran: number; // KDV oranı
  kartSatisKdvOran: number;
}
```

### Depo Kartı Zorunlu Alanlar
```typescript
{
  kod: string;             // Depo kodu
  tanim: string;          // Depo adı
  kategoriKod: string;    // Depo kategorisi
}
```

## 🎯 Sonraki Adımlar

1. **Cari Kart (Customer/Supplier)** → TODO
   - `CariKarti.ts`
   - `CariMapper.ts`
   - `CariService.ts`
   - Backend controller

2. **Entity Mapping'leri** → TODO
   - `ProductKozaStockMapping` entity
   - Migration oluştur
   - Sync sonrası mapping kaydet

3. **UI Components** → TODO
   - Admin panel stok kartı yönetimi
   - Product → Koza sync butonu
   - Sync sonuçları tablosu

## ✨ Özet

**SORUN:** Depo kartı vardı ama stok kartı yoktu → Tutarsızlık!

**ÇÖZÜM:** Stok kartı için tam mimari eklendi:
- ✅ Frontend: Tip + Mapper + Service
- ✅ Backend: DTO + Service + Controller
- ✅ Tutarlı klasör yapısı
- ✅ Backend-first yaklaşım
- ✅ Tip güvenliği

Artık **Depo** ve **Stok Kartı** aynı mimari prensiplerle çalışıyor! 🎉
