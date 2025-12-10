# Design Document: Koza Stock Cards Enhancement

## Overview

Bu tasarım, Koza Entegrasyon sayfasındaki Stok Kartları sekmesini Admin Paneldeki LucaProducts bileşeniyle aynı işlevselliğe kavuşturmayı hedefler. Mevcut KozaIntegration bileşenindeki stok kartları tablosu genişletilecek ve LucaProducts'taki özellikler (arama, detaylı kolonlar, mobil görünüm) eklenecektir.

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    KozaIntegration.tsx                       │
│  ┌─────────────────────────────────────────────────────┐    │
│  │              Stok Kartları Tab (activeTab === 1)     │    │
│  │  ┌─────────────────────────────────────────────┐    │    │
│  │  │         Search & Filter Section              │    │    │
│  │  │  [🔍 Search Input] [Toplam: X] [Görünen: Y] │    │    │
│  │  └─────────────────────────────────────────────┘    │    │
│  │  ┌─────────────────────────────────────────────┐    │    │
│  │  │         Desktop: Table View                  │    │    │
│  │  │  Kod | Ad | Barkod | Kategori | Birim | ... │    │    │
│  │  └─────────────────────────────────────────────┘    │    │
│  │  ┌─────────────────────────────────────────────┐    │    │
│  │  │         Mobile: Card View                    │    │    │
│  │  │  [Card 1] [Card 2] [Card 3] ...             │    │    │
│  │  └─────────────────────────────────────────────┘    │    │
│  └─────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
```

## Components and Interfaces

### Modified Component: KozaIntegration.tsx

Mevcut KozaIntegration bileşeninde Stok Kartları sekmesi güncellenecek:

**Yeni State'ler:**

```typescript
const [searchTerm, setSearchTerm] = useState("");
const [filteredStockCards, setFilteredStockCards] = useState<KozaStokKarti[]>(
  []
);
```

**Yeni UI Elemanları:**

- Search TextField (LucaProducts'taki gibi)
- Chip'ler ile toplam/filtrelenmiş sayı gösterimi
- Genişletilmiş tablo kolonları
- Mobil kart görünümü

### API Interface

Mevcut `kozaAPI.stockCards.list()` endpoint'i kullanılacak. Backend'den dönen veri yapısı:

```typescript
interface KozaStokKarti {
  stokKartId?: number;
  kartKodu: string;
  kartAdi: string;
  barkod?: string;
  kategoriAgacKod?: string;
  olcumBirimiId?: number;
  olcumBirimi?: string;
  miktar?: number;
  birimFiyat?: number;
  kartSatisKdvOran: number;
  kartAlisKdvOran?: number;
  durum?: boolean;
  sonGuncelleme?: string;
}
```

## Data Models

### Extended KozaStokKarti Interface

Mevcut interface'e ek alanlar:

```typescript
interface KozaStokKarti {
  // Mevcut alanlar
  stokKartId?: number;
  kartKodu: string;
  kartAdi: string;
  kategoriAgacKod?: string;
  kartSatisKdvOran: number;

  // Yeni/genişletilmiş alanlar
  barkod?: string;
  olcumBirimi?: string;
  miktar?: number;
  birimFiyat?: number;
  durum?: boolean;
  sonGuncelleme?: string;
}
```

## Correctness Properties

_A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees._

### Property 1: Search filter returns matching items only

_For any_ search term and stock card list, all items in the filtered result should contain the search term in either kartKodu or kartAdi (case-insensitive)
**Validates: Requirements 2.2**

### Property 2: Filtered count matches actual filtered array length

_For any_ filtered stock card list, the displayed filtered count should equal the length of the filtered array
**Validates: Requirements 2.3, 3.2**

### Property 3: Missing fields display placeholder

_For any_ stock card with undefined/null optional fields (barkod, olcumBirimi, miktar, birimFiyat, sonGuncelleme), the rendered output should contain "-" as placeholder
**Validates: Requirements 1.3**

### Property 4: Total count matches original data length

_For any_ stock card data loaded from API, the displayed total count should equal the original array length before filtering
**Validates: Requirements 3.1**

## Error Handling

1. **API Hatası**: Stok kartları yüklenemezse Alert ile hata mesajı gösterilir
2. **Boş Liste**: "Henüz stok kartı kaydı yok" mesajı gösterilir
3. **Arama Sonucu Yok**: "Arama sonucu bulunamadı" mesajı gösterilir

## Testing Strategy

### Unit Tests

- Search filter fonksiyonunun doğru çalıştığını test et
- Placeholder gösteriminin doğru çalıştığını test et

### Property-Based Tests

Property-based testing için **fast-check** kütüphanesi kullanılacak.

Her property-based test:

- Minimum 100 iterasyon çalıştırılacak
- Design document'taki ilgili property'ye referans verecek
- Format: `**Feature: koza-stock-cards-enhancement, Property {number}: {property_text}**`

### Integration Tests

- KozaIntegration bileşeninin stok kartları sekmesinin doğru render edildiğini test et
- API çağrısının doğru yapıldığını test et
