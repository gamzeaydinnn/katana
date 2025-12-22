# Design Document

## Overview

Bu tasarım, ExtractorService'deki stok mapping eksikliğini giderir. Mevcut durumda KatanaService.MapProductElement metodu Katana API'sinden gelen InStock/OnHand/Available/Committed alanlarını KatanaProductDto'ya doğru şekilde dolduruyor, ancak ExtractorService.ExtractProductsAsync metodu bu değerleri ProductDto.Stock alanına aktarmıyor.

### Sorunun Kök Nedeni

```
KatanaService.GetProductsAsync()
    └── MapProductElement(prodEl)
        └── KatanaProductDto { InStock=10, OnHand=8, Available=5 } ✅

ExtractorService.ExtractProductsAsync()
    └── new ProductDto { Stock = ??? } ❌ (hiç set edilmiyor, default 0)
```

### Çözüm

ExtractorService.ExtractProductsAsync metodunda ProductDto oluşturulurken Stock alanını KatanaProductDto'dan alınan stok değerleriyle doldurmak.

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        KatanaService (Infrastructure)                        │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ GetProductsAsync()                                                    │   │
│  │   └── MapProductElement(prodEl)                                       │   │
│  │         └── KatanaProductDto {                                        │   │
│  │               InStock = 10,    // Katana API'den                      │   │
│  │               OnHand = 8,      // Katana API'den                      │   │
│  │               Available = 5    // Katana API'den                      │   │
│  │             }                                                         │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                        ExtractorService (Business)                           │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ ExtractProductsAsync()                                                │   │
│  │   └── foreach (var product in katanaProducts)                         │   │
│  │         └── new ProductDto {                                          │   │
│  │               SKU = product.SKU,                                      │   │
│  │               Name = product.Name,                                    │   │
│  │               Stock = product.InStock ?? product.OnHand               │   │
│  │                       ?? product.Available ?? 0  // YENİ              │   │
│  │             }                                                         │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                        TransformerService (Business)                         │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ TransformProductsAsync()                                              │   │
│  │   └── StockSnapshot = dto.Stock  // Artık doğru değer gelecek        │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Components and Interfaces

### 1. ExtractorService (Güncelleme)

Mevcut ExtractProductsAsync metodunda ProductDto oluşturma kısmı güncellenir.

```csharp
// ÖNCE (Mevcut kod)
var dto = new ProductDto
{
    SKU = product.SKU,
    Name = product.Name,
    Price = product.Price,
    CategoryId = product.CategoryId,
    MainImageUrl = product.ImageUrl,
    Description = product.Description,
    IsActive = product.IsActive,
    CreatedAt = DateTime.UtcNow
    // Stock alanı hiç set edilmiyor!
};

// SONRA (Güncellenmiş kod)
var dto = new ProductDto
{
    SKU = product.SKU,
    Name = product.Name,
    Price = product.Price,
    CategoryId = product.CategoryId,
    MainImageUrl = product.ImageUrl,
    Description = product.Description,
    IsActive = product.IsActive,
    CreatedAt = DateTime.UtcNow,
    Stock = product.InStock ?? product.OnHand ?? product.Available ?? 0
};
```

### 2. Stok Mapping Öncelik Zinciri

| Öncelik | Alan      | Açıklama                                |
| ------- | --------- | --------------------------------------- |
| 1       | InStock   | Toplam stok miktarı (tercih edilen)     |
| 2       | OnHand    | Eldeki stok miktarı                     |
| 3       | Available | Kullanılabilir stok miktarı             |
| 4       | 0         | Varsayılan değer (tüm alanlar null ise) |

## Data Models

### KatanaProductDto (Mevcut - Değişiklik Yok)

```csharp
public class KatanaProductDto
{
    // ... diğer alanlar ...

    public int? OnHand { get; set; }
    public int? Available { get; set; }
    public int? Committed { get; set; }
    public int? InStock { get; set; }
}
```

### ProductDto (Mevcut - Değişiklik Yok)

```csharp
public class ProductDto
{
    // ... diğer alanlar ...

    public int Stock { get; set; }  // Bu alan zaten var, sadece doldurulmuyor
}
```

## Correctness Properties

_A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees._

### Property 1: Stock Mapping Priority Chain

_For any_ KatanaProductDto with any combination of InStock, OnHand, and Available values (including null), the mapped ProductDto.Stock should equal the first non-null value in the priority order: InStock → OnHand → Available → 0.

Mathematically: `Stock = InStock ?? OnHand ?? Available ?? 0`

**Validates: Requirements 1.1, 1.2, 1.3, 1.4, 1.5**

### Property 2: Zero is Valid Value

_For any_ KatanaProductDto where InStock = 0 (not null), the mapped ProductDto.Stock should be 0, regardless of OnHand or Available values.

**Validates: Requirements 3.5**

## Error Handling

### Null Safety

- Tüm stok alanları nullable int olarak tanımlı
- Null coalescing operatörü (`??`) ile güvenli fallback
- Hiçbir alan null değilse bile varsayılan 0 değeri garanti

### Logging

- Debug seviyesinde: Her ürün için SKU, kaynak alan ve stok değeri
- Warning seviyesinde: Tüm stok alanları null olan ürünler
- Info seviyesinde: İşlem özeti (toplam ürün, sıfır olmayan stok sayısı)

## Testing Strategy

### Unit Testing Framework

- **Framework**: xUnit
- **Mocking**: Moq for IKatanaService
- **Assertions**: FluentAssertions

### Property-Based Testing Framework

- **Framework**: FsCheck.Xunit
- **Minimum Iterations**: 100 per property
- **Generators**: Custom generator for KatanaProductDto with nullable stock fields

### Test Categories

#### Unit Tests

1. **ExtractorService Stock Mapping Tests**
   - Test InStock priority
   - Test OnHand fallback
   - Test Available fallback
   - Test all null defaults to 0
   - Test zero is valid value

#### Property-Based Tests

Property 1 will have a corresponding property-based test that:

- Generates random KatanaProductDto with nullable InStock, OnHand, Available
- Verifies Stock = InStock ?? OnHand ?? Available ?? 0

### Test Annotations

All property-based tests must include:

```csharp
// **Feature: katana-stock-mapping-fix, Property 1: Stock Mapping Priority Chain**
// **Validates: Requirements 1.1, 1.2, 1.3, 1.4, 1.5**
```

## Implementation Notes

### Minimal Change Approach

Bu düzeltme tek bir satır değişikliği gerektirir:

```csharp
// ExtractorService.cs, ExtractProductsAsync metodu içinde
Stock = product.InStock ?? product.OnHand ?? product.Available ?? 0
```

### Backward Compatibility

- Mevcut ProductDto yapısı değişmiyor
- Mevcut TransformerService ve LoaderService değişiklik gerektirmiyor
- Sadece veri akışındaki eksik mapping düzeltiliyor
