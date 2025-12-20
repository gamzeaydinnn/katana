# Koza Entegrasyon Yapısı

Bu klasör Koza ERP sistemi ile entegrasyon için gerekli tüm TypeScript tipler, mapper'lar ve servisler içerir.

## 📁 Klasör Yapısı

```
luca-koza/
├── cards/              # Kart tipleri (Depo, Stok, Cari vb.)
│   ├── DepoKarti.ts    # Depo kartı tipleri ve interface'leri
│   ├── DepoMapper.ts   # Katana Location → Koza Depo dönüşümü
│   ├── DepoService.ts  # Depo CRUD işlemleri
│   ├── StokKarti.ts    # Stok kartı tipleri
│   ├── StokMapper.ts   # Katana Product → Koza Stok dönüşümü
│   ├── StokService.ts  # Stok kartı CRUD işlemleri
│   └── index.ts        # Export hub
├── sync/               # Toplu senkronizasyon servisleri
│   ├── LocationSync.ts # Location → Depo toplu sync
│   └── ProductSync.ts  # Product → Stok kartı toplu sync
├── config.ts           # Varsayılan değerler ve konfigürasyon
├── index.ts            # Genel export hub
└── README.md           # Bu dosya
```

## 🎯 Kullanım Prensibi

### 1. Backend-First Yaklaşım
Frontend **ASLA** direkt Koza API'ye bağlanmaz. Tüm istekler `api.ts` üzerinden backend'e gider:

```typescript
// ❌ YANLIŞ - Direkt Koza'ya bağlanma
import lucaApi from "services/lucaApi";

// ✅ DOĞRU - Backend proxy kullan
import { kozaAPI } from "services/api";
```

### 2. Servis Katmanı
Her kart tipi için 3 dosya:
- **{Tip}Karti.ts**: TypeScript tipleri ve interface'ler
- **{Tip}Mapper.ts**: Katana → Koza veri dönüşümü
- **{Tip}Service.ts**: CRUD işlemleri (list, create, getOrCreate)

### 3. API Entegrasyonu
Servisler `services/api.ts` içindeki `kozaAPI` objesini kullanır:

```typescript
// services/api.ts
export const kozaAPI = {
  depots: {
    list: () => api.get("/admin/koza/depots"),
    create: (payload) => api.post("/admin/koza/depots/create", payload),
  },
  stockCards: {
    list: () => api.get("/admin/koza/stocks"),
    create: (payload) => api.post("/admin/koza/stocks/create", payload),
  },
};
```

## 📝 Yeni Kart Tipi Ekleme

Örnek: Cari Kart (Customer/Supplier) eklemek için:

### 1. Tip Tanımları (`CariKarti.ts`)
```typescript
export interface KozaCariKart {
  cariKodu: string;
  cariAdi: string;
  cariTip: "MUSTERI" | "TEDARIKCI" | "MUSTERI_TEDARIKCI";
  // ... diğer alanlar
}

export interface CariKartiEkleRequest {
  cariKart: KozaCariKart;
}

export interface CariKartiEkleResponse {
  error?: boolean;
  message?: string;
  cariKartId?: number;
}
```

### 2. Mapper (`CariMapper.ts`)
```typescript
import { KozaCariKart } from "./CariKarti";

export interface KatanaCustomer {
  id: number;
  name: string;
  // ... diğer alanlar
}

export function mapKatanaCustomerToKozaCariKart(
  customer: KatanaCustomer,
  defaults: Partial<KozaCariKart> = {}
): KozaCariKart {
  return {
    cariKodu: `CUST-${customer.id}`,
    cariAdi: customer.name,
    cariTip: "MUSTERI",
    ...defaults,
  };
}
```

### 3. Servis (`CariService.ts`)
```typescript
import { kozaAPI } from "../../../../services/api";
import { KozaCariKart, CariKartiEkleRequest } from "./CariKarti";

export class CariService {
  async listele(): Promise<KozaCariKart[]> {
    try {
      const response = await kozaAPI.customers.list();
      return Array.isArray(response) ? response : [];
    } catch (error) {
      console.error("Cari kart listeleme hatası:", error);
      return [];
    }
  }

  async ekle(req: CariKartiEkleRequest) {
    return kozaAPI.customers.create(req);
  }

  async getirVeyaOlustur(cari: KozaCariKart): Promise<KozaCariKart> {
    // Önce var mı kontrol et, yoksa oluştur
  }
}

export const cariService = new CariService();
```

### 4. API Ekle (`services/api.ts`)
```typescript
export const kozaAPI = {
  depots: { ... },
  stockCards: { ... },
  customers: {
    list: () => api.get("/admin/koza/customers"),
    create: (payload: any) => api.post("/admin/koza/customers/create", payload),
  },
};
```

### 5. Export Ekle (`cards/index.ts`)
```typescript
// Cari Kart
export * from "./CariKarti";
export * from "./CariMapper";
export * from "./CariService";
```

## 🔒 Güvenlik Notları

1. **Session Yönetimi**: Backend `LucaService` içinde yönetilir
2. **Cookie Handling**: Frontend cookie'lere dokunmaz
3. **Error Handling**: Backend'de merkezi error handling var
4. **Timeout**: api.ts'de 120 saniye timeout tanımlı (toplu sync için)

## 🗂️ Backend Karşılıkları

```
Frontend                          → Backend
──────────────────────────────────────────────────────────
kozaAPI.depots.list()             → KozaDepotsController.GetDepots()
kozaAPI.depots.create()           → KozaDepotsController.CreateDepot()
kozaAPI.stockCards.list()         → KozaStockCardsController.GetStockCards()
kozaAPI.stockCards.create()       → KozaStockCardsController.CreateStockCard()

Backend Controller                → LucaService
──────────────────────────────────────────────────────────
KozaDepotsController              → LucaService.Depots.cs (partial)
KozaStockCardsController          → LucaService.StockCards.cs (partial)
```

## 📚 İlgili Dokümantasyon

- `/docs/KOZA_DEPO_INTEGRATION.md` - Depo entegrasyonu detayları
- `/docs/KOZA_STRUCTURE_FIX.md` - Yapısal düzeltme açıklamaları
- `/docs/Luca-Koza-API.md` - Koza API referansı
