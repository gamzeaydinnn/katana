# Design: Luca Stok Kartı UPSERT Düzeltmesi

## Overview

Katana'dan Luca'ya ürün senkronizasyonu sırasında, aynı SKU'lu ürün geldiğinde mevcut stok kartı güncellenmelidir. Luca API'de `GuncelleStkWsSkart.do` endpoint'i mevcuttur ve çalışmaktadır. Sistem bu endpoint'i kullanarak UPDATE işlemini gerçekleştirecektir.

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│ Katana (Ürün Güncelleme)                                    │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ UpsertStockCardAsync (LucaService.Queries.cs)              │
│                                                              │
│ 1. FindStockCardBySkuAsync(SKU)                             │
│    ├─ Bulundu → UPDATE                                      │
│    └─ Bulunamadı → CREATE                                   │
└────────────────────┬────────────────────────────────────────┘
                     │
        ┌────────────┴────────────┐
        ▼                         ▼
┌──────────────────┐    ┌──────────────────┐
│ UPDATE Path      │    │ CREATE Path      │
│                  │    │                  │
│ UpdateStockCard  │    │ CreateStockCard  │
│ Async            │    │ Async            │
│                  │    │                  │
│ GuncelleStkWs    │    │ EkleStkWsKart.do │
│ Skart.do         │    │                  │
└────────┬─────────┘    └────────┬─────────┘
         │                       │
         └───────────┬───────────┘
                     ▼
            ┌─────────────────┐
            │ Luca API        │
            │ (Güncellendi)   │
            └─────────────────┘
```

## Components and Interfaces

### 1. UpsertStockCardAsync (Orchestrator)

**Sorumluluk:** SKU'ya göre UPDATE/CREATE kararı vermek

```csharp
public async Task<SyncResultDto> UpsertStockCardAsync(
    LucaCreateStokKartiRequest stockCard)
{
    // 1. SKU'ya göre mevcut kartı ara
    var existingSkartId = await FindStockCardBySkuAsync(sku);

    if (existingSkartId.HasValue)
    {
        // 2. Mevcut kartı güncelle
        var updateRequest = MapToUpdateRequest(stockCard, existingSkartId.Value);
        var updateSuccess = await UpdateStockCardAsync(updateRequest);

        if (updateSuccess)
            return SuccessResult("Güncellendi");
        else
            return ErrorResult("Güncelleme başarısız");
    }
    else
    {
        // 3. Yeni kart oluştur
        var createResult = await SendStockCardsAsync(new[] { stockCard });
        return createResult;
    }
}
```

### 2. UpdateStockCardAsync (Update Handler)

**Sorumluluk:** Luca API'ye UPDATE isteği göndermek

```csharp
public async Task<bool> UpdateStockCardAsync(
    LucaUpdateStokKartiRequest request)
{
    // 1. Luca'ya güncelleme isteği gönder
    // 2. Yanıtı parse et
    // 3. Başarı/başarısızlık döndür
}
```

### 3. FindStockCardBySkuAsync (Finder)

**Sorumluluk:** SKU'ya göre Luca'da stok kartı aramak

```csharp
public async Task<long?> FindStockCardBySkuAsync(string sku)
{
    // 1. ListStockCardsSimpleAsync() ile tüm kartları al
    // 2. SKU'ya göre filtrele
    // 3. skartId döndür
}
```

## Data Models

### LucaUpdateStokKartiRequest

```csharp
public class LucaUpdateStokKartiRequest
{
    public long SkartId { get; set; }           // Güncellenecek kartın ID'si
    public string KartKodu { get; set; }        // SKU
    public string KartAdi { get; set; }         // Ürün adı
    public string UzunAdi { get; set; }         // Uzun ad
    public string Barkod { get; set; }          // Barkod
    public string KategoriAgacKod { get; set; } // Kategori
    public decimal? PerakendeAlisBirimFiyat { get; set; }   // Alış fiyatı
    public decimal? PerakendeSatisBirimFiyat { get; set; }  // Satış fiyatı
    public string GtipKodu { get; set; }        // GTIP kodu
}
```

### SyncResultDto (Response)

```csharp
public class SyncResultDto
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; }
    public int SuccessfulRecords { get; set; }
    public int FailedRecords { get; set; }
    public int DuplicateRecords { get; set; }
    public List<string> Errors { get; set; }
}
```

## Correctness Properties

A property is a characteristic or behavior that should hold true across all valid executions of a system—essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.

### Property 1: Aynı SKU Güncelleme Garantisi

_For any_ ürün ve mevcut Luca stok kartı, aynı SKU'lu ürün gönderildiğinde sistem UPDATE yapmalı, yeni kart açmamalı.

**Validates: Requirements 1.1, 1.2, 1.3**

### Property 2: Yeni SKU Oluşturma Garantisi

_For any_ ürün ve Luca'da mevcut olmayan SKU, sistem CREATE yapmalı.

**Validates: Requirements 1.5**

### Property 3: Alanlar Doğru Eşlenme

_For any_ ürün güncellemesi, tüm güncellenebilir alanlar (kartAdi, uzunAdi, barkod, vb.) Luca'ya doğru şekilde gönderilmelidir.

**Validates: Requirements 2.1-2.8**

### Property 4: İdempotency

_For any_ ürün, aynı ürün 2 kez gönderildiğinde sistem 2. kez UPDATE yapmalı, yeni kart açmamalı.

**Validates: Requirements 4.1, 4.2**

### Property 5: Hata Yönetimi

_For any_ başarısız güncelleme, sistem hata mesajı döndürmeli ve tutarsız durum oluşmamalı.

**Validates: Requirements 3.1-3.4**

## Error Handling

### Senaryo 1: Luca API Session Expired

```
1. UpdateStockCardAsync() çağrılıyor
2. Luca HTML döndürüyor (session expired)
3. Sistem session'ı yenileyip tekrar deniyor
4. Başarılı olursa: "Güncellendi" döndür
5. Başarısız olursa: "Güncelleme başarısız" döndür
```

### Senaryo 2: Luca API Hata Döndürüyor

```
1. UpdateStockCardAsync() çağrılıyor
2. Luca error code döndürüyor
3. Sistem hata mesajını loglayıp döndürüyor
4. UpsertStockCardAsync() hata döndürüyor
```

### Senaryo 3: Network Hatası

```
1. UpdateStockCardAsync() çağrılıyor
2. Network hatası oluşuyor
3. Sistem exception'ı yakalar ve loglayıyor
4. UpsertStockCardAsync() hata döndürüyor
```

## Testing Strategy

### Unit Tests

- `UpdateStockCardAsync` başarılı güncelleme testi
- `UpdateStockCardAsync` başarısız güncelleme testi
- `UpsertStockCardAsync` UPDATE path testi
- `UpsertStockCardAsync` CREATE path testi
- `FindStockCardBySkuAsync` bulundu testi
- `FindStockCardBySkuAsync` bulunamadı testi

### Property-Based Tests

- **Property 1**: Aynı SKU'lu ürün 100 kez gönderildiğinde, sistem 100 kez UPDATE yapmalı
- **Property 2**: Farklı SKU'lu ürünler gönderildiğinde, sistem CREATE yapmalı
- **Property 3**: Tüm alanlar Luca'ya doğru şekilde gönderilmelidir
- **Property 4**: Aynı ürün 2 kez gönderildiğinde, 2. kez UPDATE yapılmalı
- **Property 5**: Başarısız güncelleme sonrası sistem tutarlı durumda kalmalı

### Test Framework

- **Unit Tests**: xUnit
- **Property-Based Tests**: FsCheck (F# property testing library for C#)
