# SKU Versiyonlaması ve Ürün Güncelleme Analizi

## 🚨 SORUN: Gereksiz SKU Oluştuluyor

### Mevcut Durum

**Katana tarafında (ProductService.cs):**

- ✅ `CreateProductAsync`: Aynı SKU varsa hata veriyor
- ✅ `UpdateProductAsync`: Aynı SKU'ya sahip başka ürün varsa hata veriyor
- ✅ `BulkSyncProductsAsync`: SKU varsa UPDATE, yoksa CREATE yapıyor

**Luca tarafında (LucaService.Queries.cs):**

- ❌ `UpsertStockCardAsync`: **Luca API stok kartı güncellemesini desteklemiyor!**
  - Eğer SKU zaten Luca'da varsa → "duplicate" olarak işaretleniyor
  - Yeni SKU oluşturulmak istenirse → Yeni stok kartı oluşturuluyor

### Kod Kanıtı

**LucaService.Queries.cs (satır 3162-3200):**

```csharp
public async Task<SyncResultDto> UpsertStockCardAsync(LucaCreateStokKartiRequest stockCard)
{
    var sku = stockCard.KartKodu;

    // First, check if the card already exists
    var existingSkartId = await FindStockCardBySkuAsync(sku);

    if (existingSkartId.HasValue)
    {
        // Card already exists in Luca
        // NOTE: Luca Koza API does NOT support stock card updates!
        // The card already exists, so we mark it as "duplicate" (already synced)
        result.DuplicateRecords = 1;
        result.IsSuccess = true;
        result.Message = $"Stok kartı '{sku}' zaten Luca'da mevcut (skartId: {existingSkartId.Value}).
                          Luca API stok kartı güncellemesini desteklemiyor.";
        return result;
    }

    // Card doesn't exist, create new
    var sendResult = await SendStockCardsAsync(new List<LucaCreateStokKartiRequest> { stockCard });
    // ...
}
```

**LucaService.StockCards.cs (satır 714-800):**

```csharp
public async Task<bool> UpdateStockCardAsync(LucaUpdateStokKartiRequest request)
{
    // ATTEMPT 1 & 2: Luca API'ye güncelleme isteği gönderiliyor
    // Ancak Luca API bunu desteklemiyor!
    // Sonuç: Güncelleme başarısız oluyor
}
```

---

## 🔍 PROBLEM SENARYOSU

### Senaryo 1: Ürün Fiyatı Değiştiğinde

```
1. Katana'da: SKU="PROD-001", Fiyat=100 TL
   ↓ UpdateProductAsync() çağrılıyor
   ↓ DB'de güncelleniyor ✅

2. Luca'ya gönderme:
   ↓ UpsertStockCardAsync() çağrılıyor
   ↓ FindStockCardBySkuAsync("PROD-001") → Bulundu (skartId=123)
   ↓ "Duplicate" olarak işaretleniyor
   ↓ Luca'da GÜNCELLEME YAPILMIYOR ❌

3. Sonuç:
   - Katana'da: Fiyat=100 TL ✅
   - Luca'da: Fiyat=eski değer ❌
   - Veri tutarsızlığı!
```

### Senaryo 2: Ürün Adı Değiştiğinde

```
1. Katana'da: SKU="PROD-001", Ad="Eski Ürün Adı"
   ↓ UpdateProductAsync() çağrılıyor
   ↓ DB'de güncelleniyor ✅

2. Luca'ya gönderme:
   ↓ UpsertStockCardAsync() çağrılıyor
   ↓ FindStockCardBySkuAsync("PROD-001") → Bulundu
   ↓ "Duplicate" olarak işaretleniyor
   ↓ Luca'da GÜNCELLEME YAPILMIYOR ❌

3. Sonuç:
   - Katana'da: Ad="Yeni Ürün Adı" ✅
   - Luca'da: Ad="Eski Ürün Adı" ❌
```

---

## 🎯 ÇÖZÜM SEÇENEKLERI

### Seçenek 1: Luca API'de Güncelleme Desteği Ekle (Önerilen)

**Avantajlar:**

- ✅ Aynı SKU'yu korur
- ✅ Veri tutarlılığı sağlanır
- ✅ Versiyonlama gerekmez

**Dezavantajlar:**

- ❌ Luca API'de yeni endpoint gerekli
- ❌ Luca tarafında geliştirme gerekli

**Implementasyon:**

```csharp
// LucaService.StockCards.cs'de yeni metod
public async Task<bool> UpdateStockCardProperlyAsync(LucaUpdateStokKartiRequest request)
{
    // Luca'da GuncelleStkWsSkart.do endpoint'i düzgün çalışması gerekli
    // Şu anda çalışmıyor, Luca tarafında fix gerekli
}
```

---

### Seçenek 2: Versiyonlu SKU Sistemi

**Avantajlar:**

- ✅ Luca API'de değişiklik gerekmez
- ✅ Hızlı implementasyon

**Dezavantajlar:**

- ❌ Gereksiz SKU oluşuyor (PROD-001, PROD-001_v2, PROD-001_v3...)
- ❌ Luca'da karmaşıklık artıyor
- ❌ Raporlama zorlaşıyor

**Implementasyon:**

```csharp
public async Task<SyncResultDto> UpsertStockCardAsync(LucaCreateStokKartiRequest stockCard)
{
    var sku = stockCard.KartKodu;
    var existingSkartId = await FindStockCardBySkuAsync(sku);

    if (existingSkartId.HasValue)
    {
        // Versiyonlu SKU oluştur
        var version = 2;
        var newSku = $"{sku}_v{version}";

        while (await FindStockCardBySkuAsync(newSku) != null)
        {
            version++;
            newSku = $"{sku}_v{version}";
        }

        stockCard.KartKodu = newSku;
        // Yeni SKU ile stok kartı oluştur
    }

    var sendResult = await SendStockCardsAsync(new List<LucaCreateStokKartiRequest> { stockCard });
    return result;
}
```

---

### Seçenek 3: Soft Delete + Yeni Oluştur

**Avantajlar:**

- ✅ Eski veriler korunur
- ✅ Audit trail sağlanır

**Dezavantajlar:**

- ❌ Luca'da eski kartlar kalıyor
- ❌ Karmaşık mantık

---

## 📋 MEVCUT DURUM ÖZETI

| Durum         | Katana    | Luca         | Sonuç                 |
| ------------- | --------- | ------------ | --------------------- |
| Yeni ürün     | CREATE ✅ | CREATE ✅    | OK                    |
| Ürün güncelle | UPDATE ✅ | DUPLICATE ❌ | **Veri tutarsızlığı** |
| Ürün sil      | DELETE ✅ | ZOMBIE ✅    | OK                    |

---

## 🔧 ÖNERİLEN AKSIYON

1. **Kısa vadede:** Seçenek 2 (Versiyonlu SKU) ile geçici çözüm
2. **Uzun vadede:** Luca API'de güncelleme desteği ekle (Seçenek 1)

---

## 📝 NOTLAR

- **Luca Koza API Sınırlaması:** Stok kartı güncellemesi desteklenmiyor
- **UpdateStockCardAsync:** Şu anda 2 attempt yapıyor ama ikisi de başarısız
- **FindStockCardBySkuAsync:** Doğru çalışıyor, SKU'yu bulabiliyor
- **SendStockCardsAsync:** Yeni kartlar oluşturabiliyor
