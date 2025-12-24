# Luca Stok Kartı UPSERT Çözüm Özeti

## 🎯 Sorun

Katana'dan Luca'ya aynı SKU'lu ürün gönderildiğinde:

- ❌ Yeni stok kartı açılıyor (gereksiz)
- ❌ Mevcut kartı güncellenmiyor
- ❌ Veri tutarsızlığı oluşuyor

## ✅ Çözüm

Luca API'de `GuncelleStkWsSkart.do` endpoint'i **mevcuttur ve çalışmaktadır**. Sistem bu endpoint'i kullanarak UPDATE işlemini yapacaktır.

### Akış

```
Aynı SKU'lu ürün geldi
        ↓
FindStockCardBySkuAsync(SKU)
        ↓
    ┌───┴───┐
    ▼       ▼
Bulundu  Bulunamadı
    ↓       ↓
UPDATE  CREATE
    ↓       ↓
Güncelle  Oluştur
```

## 📝 Implementasyon Detayları

### 1. UpsertStockCardAsync (Orchestrator)

**Dosya:** `src/Katana.Infrastructure/APIClients/LucaService.Queries.cs`

**Mevcut Kod (Yanlış):**

```csharp
if (existingSkartId.HasValue)
{
    // Duplicate olarak işaretleniyor, güncelleme yapılmıyor ❌
    result.DuplicateRecords = 1;
    result.IsSuccess = true;
    result.Message = "Stok kartı zaten mevcut";
    return result;
}
```

**Yeni Kod (Doğru):**

```csharp
if (existingSkartId.HasValue)
{
    // UPDATE yapılacak ✅
    var updateRequest = MapToUpdateRequest(stockCard, existingSkartId.Value);
    var updateSuccess = await UpdateStockCardAsync(updateRequest);

    if (updateSuccess)
    {
        result.IsSuccess = true;
        result.SuccessfulRecords = 1;
        result.Message = $"Stok kartı '{sku}' güncellendi";
    }
    else
    {
        result.IsSuccess = false;
        result.FailedRecords = 1;
        result.Message = $"Stok kartı '{sku}' güncellenemedi";
    }
    return result;
}
```

### 2. UpdateStockCardAsync (Update Handler)

**Dosya:** `src/Katana.Infrastructure/APIClients/LucaService.StockCards.cs`

**Endpoint:** `POST /GuncelleStkWsSkart.do`

**Request Body:**

```json
{
  "skartId": 74004,
  "kartKodu": "00004",
  "kartAdi": "FANTA GAZOS",
  "uzunAdi": "TEST MAL ADI II",
  "barkod": "TEST BARKOD",
  "kategoriAgacKod": "01",
  "perakendeAlisBirimFiyat": 20,
  "perakendeSatisBirimFiyat": 30,
  "gtipKodu": "TEST GTIP"
}
```

**Güncellenebilir Alanlar:**

- ✅ kartKodu (SKU)
- ✅ kartAdi (ürün adı)
- ✅ uzunAdi (uzun ad)
- ✅ barkod
- ✅ kategoriAgacKod (kategori)
- ✅ perakendeAlisBirimFiyat (alış fiyatı)
- ✅ perakendeSatisBirimFiyat (satış fiyatı)
- ✅ gtipKodu

### 3. MapToUpdateRequest (Mapper)

**Dosya:** `src/Katana.Business/Mappers/KatanaToLucaMapper.cs`

```csharp
public static LucaUpdateStokKartiRequest MapToUpdateRequest(
    LucaCreateStokKartiRequest createRequest,
    long skartId)
{
    return new LucaUpdateStokKartiRequest
    {
        SkartId = skartId,
        KartKodu = createRequest.KartKodu,
        KartAdi = createRequest.KartAdi,
        UzunAdi = createRequest.UzunAdi,
        Barkod = createRequest.Barkod,
        KategoriAgacKod = createRequest.KategoriAgacKod,
        PerakendeAlisBirimFiyat = createRequest.PerakendeAlisBirimFiyat,
        PerakendeSatisBirimFiyat = createRequest.PerakendeSatisBirimFiyat,
        GtipKodu = createRequest.GtipKodu
    };
}
```

## 🧪 Test Senaryoları

### Senaryo 1: Yeni Ürün

```
1. SKU="PROD-001" gönderiliyor
2. FindStockCardBySkuAsync("PROD-001") → null
3. CreateStockCardAsync() çağrılıyor
4. Luca'da yeni kart oluşturuluyor ✅
```

### Senaryo 2: Mevcut Ürün Güncelleme

```
1. SKU="PROD-001" gönderiliyor (fiyat değişti)
2. FindStockCardBySkuAsync("PROD-001") → 74004
3. UpdateStockCardAsync(74004, ...) çağrılıyor
4. Luca'da kart güncelleniyor ✅
5. Yeni kart açılmıyor ✅
```

### Senaryo 3: İdempotency

```
1. SKU="PROD-001" gönderiliyor
2. FindStockCardBySkuAsync("PROD-001") → 74004
3. UpdateStockCardAsync(74004, ...) çağrılıyor
4. Luca'da kart güncelleniyor ✅
5. Aynı ürün tekrar gönderiliyor
6. FindStockCardBySkuAsync("PROD-001") → 74004
7. UpdateStockCardAsync(74004, ...) çağrılıyor (tekrar)
8. Luca'da kart güncelleniyor ✅
9. Yeni kart açılmıyor ✅
```

## 📊 Sonuç

| Durum            | Eski         | Yeni      |
| ---------------- | ------------ | --------- |
| Yeni ürün        | CREATE ✅    | CREATE ✅ |
| Ürün güncelle    | DUPLICATE ❌ | UPDATE ✅ |
| Aynı ürün 2 kez  | 2 kart ❌    | 1 kart ✅ |
| Veri tutarlılığı | Bozuk ❌     | Sağlam ✅ |

## 🔧 Spec Dosyaları

- `.kiro/specs/luca-stock-card-upsert-fix/requirements.md` - Gereksinimler
- `.kiro/specs/luca-stock-card-upsert-fix/design.md` - Tasarım
- `.kiro/specs/luca-stock-card-upsert-fix/tasks.md` - Implementasyon planı
