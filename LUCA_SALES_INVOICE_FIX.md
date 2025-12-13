# Luca Satış Faturası Mapping Düzeltmeleri

## Sorun Özeti

Luca API'ye satış faturası gönderilirken kritik mapping farkları vardı:

1. **`belgeNo`** - DTO tipi `string?` olmalı; ayrıca **satış faturasında hiç gönderilmemeli** (null/omit)
2. **`cariTip`** - 0 yerine 1 veya 2 olmalı (0 geçersiz değer)
3. **`irsaliyeBilgisiList`** - Boş liste `[]` yerine `null` gönderilmeli
4. **E-Fatura alanları** - `odemeTipi`, `gonderimTipi`, `efaturaTuru` satış faturasında set edilmeli

## Yapılan Düzeltmeler

### 1. BelgeNo Tipi + Satış Faturasında Omit

**Dosya:** `src/Katana.Core/DTOs/LucaDtos.cs`

```csharp
// ❌ ÖNCE (YANLIŞ)
[JsonPropertyName("belgeNo")]
public int? BelgeNo { get; set; }

// ✅ SONRA (DOĞRU)
[JsonPropertyName("belgeNo")]
public string? BelgeNo { get; set; }
```

**Etkilenen Sınıf:**

- `LucaCreateInvoiceHeaderRequest`

**Satış Faturası Mapping (BelgeNo gönderme):**

**Dosya:** `src/Katana.Core/Helpers/MappingHelper.cs`

```csharp
// Satış faturasında Luca belge numarasını kendisi üretir; gönderme
request.BelgeNo = null;
```

**Yeni Helper Metod Eklendi:**

**Dosya:** `src/Katana.Core/Helpers/MappingHelper.cs`

```csharp
private static string? ParseDocumentNoAsString(string? rawValue, int fallback)
{
    if (!string.IsNullOrWhiteSpace(rawValue))
    {
        return rawValue.Trim();
    }

    return fallback > 0 ? fallback.ToString() : null;
}
```

### 2. CariTip Mapping Düzeltildi (0 → 1 veya 2)

**Dosya:** `src/Katana.Core/Helpers/MappingHelper.cs`

**Luca API Kuralları:**

- `CariTip = 1` → Gerçek Kişi (Bireysel Müşteri)
- `CariTip = 2` → Tüzel Kişi (Kurumsal Müşteri)
- `CariTip = 0` → ❌ GEÇERSİZ DEĞER!

**Katana Customer.Type Mapping:**

- `customer.Type == 1` → Kurumsal → `CariTip = 2`
- `customer.Type == 2` → Bireysel → `CariTip = 1`
- `customer.Type == 0` veya `null` → Varsayılan → `CariTip = 1`

```csharp
// ❌ ÖNCE (YANLIŞ)
CariTip = customer.Type == 1 ? 0 : 1,

// ✅ SONRA (DOĞRU)
CariTip = customer.Type == 1 ? 2 : 1, // 1=Kurumsal->2(Tüzel), 2=Bireysel->1(Gerçek), 0/null->1(Varsayılan)
```

**Düzeltilen Metodlar:**

1. `MapToLucaSalesOrderHeader` (SalesOrder entity versiyonu)
2. `MapToLucaInvoiceFromSalesOrder`

### 3. IrsaliyeBilgisiList Null Gönderimi

**Dosya:** `src/Katana.Core/DTOs/LucaDtos.cs`

```csharp
// ❌ ÖNCE (YANLIŞ)
[JsonPropertyName("irsaliyeBilgisiList")]
public List<LucaLinkedDocument> IrsaliyeBilgisiList { get; set; } = new();

// ✅ SONRA (DOĞRU)
[JsonPropertyName("irsaliyeBilgisiList")]
public List<LucaLinkedDocument>? IrsaliyeBilgisiList { get; set; }
```

**Dosya:** `src/Katana.Core/Helpers/MappingHelper.cs`

```csharp
// Mapping metodlarına eklendi:
IrsaliyeBilgisiList = null, // Boş liste yerine null gönder
```

**Düzeltilen Metodlar:**

1. `MapToLucaInvoiceFromSalesOrder`
2. `MapToLucaInvoiceFromPurchaseOrder`

### 4. Satış Faturası E-Fatura Alanları

**Dosya:** `src/Katana.Core/DTOs/LucaDtos.cs`

- `OdemeTipi` (örn: `"DIGER"`, `"KREDIKARTI_BANKAKARTI"`)
- `GonderimTipi` (örn: `"ELEKTRONIK"`, `"KAGIT"`)
- `EfaturaTuru` (örn: `1`)

**Dosya:** `src/Katana.Core/Helpers/MappingHelper.cs`

```csharp
request.OdemeTipi = "DIGER";
request.GonderimTipi = "ELEKTRONIK";
request.EfaturaTuru = 1;
```

## Hedef JSON Yapısı

Düzeltmelerden sonra Luca API'ye gönderilen JSON:

```json
{
  "belgeSeri": "A",
  // "belgeNo": "...", // ✅ Satış faturasında gönderilmez
  "belgeTarihi": "11.12.2025",
  "duzenlemeSaati": "22:08",
  "vadeTarihi": "10.01.2026",
  "belgeTakipNo": "SO-56",
  "belgeAciklama": "...",
  "belgeTurDetayId": "76",
  "faturaTur": "1",
  "paraBirimKod": "EUR",
  "kurBedeli": 1,
  "kdvFlag": true,
  "musteriTedarikci": "1",
  "cariKodu": "CK-123",
  "cariTanim": "Test Müşterisi A.S.",
  "cariTip": 1,  // ✅ 1 veya 2 (0 DEĞİL!)
  "cariKisaAd": "Test Müşterisi A.S.",
  "cariYasalUnvan": "Test Müşterisi A.S.",
  "vergiNo": "120.01.001",
  "odemeTipi": "DIGER",
  "gonderimTipi": "ELEKTRONIK",
  "efaturaTuru": 1,
  // "irsaliyeBilgisiList": null,  // ✅ JSON'da görünmez (NullValueHandling.Ignore)
  "detayList": [...]
}
```

## Test Senaryosu

1. **Kurumsal Müşteri (Type=1):**

   - `CariTip = 2` (Tüzel Kişi)
   - `VergiNo` dolu
   - `TcKimlikNo` null

2. **Bireysel Müşteri (Type=2):**

   - `CariTip = 1` (Gerçek Kişi)
   - `TcKimlikNo` dolu
   - `VergiNo` null

3. **Varsayılan (Type=0 veya null):**
   - `CariTip = 1` (Gerçek Kişi)

## Etkilenen Dosyalar

1. `src/Katana.Core/DTOs/LucaDtos.cs`

   - `LucaCreateInvoiceHeaderRequest.BelgeNo` → `string?`
   - `LucaCreateInvoiceHeaderRequest.IrsaliyeBilgisiList` → nullable

2. `src/Katana.Core/Helpers/MappingHelper.cs`
   - `ParseDocumentNoAsString()` metodu eklendi
   - `MapToLucaSalesOrderHeader()` → CariTip düzeltildi
   - `MapToLucaInvoiceFromSalesOrder()` → CariTip + IrsaliyeBilgisiList düzeltildi
   - `MapToLucaInvoiceFromPurchaseOrder()` → BelgeNo + IrsaliyeBilgisiList düzeltildi

## Sonuç

✅ Tüm düzeltmeler tamamlandı ve compile hatasız geçti.
✅ Luca API artık doğru formatta JSON alacak.
✅ `cariTip: 0` hatası çözüldü.
✅ `belgeNo` string olarak gönderiliyor.
✅ `irsaliyeBilgisiList` boş liste yerine null gönderiliyor.

## Rebuild Gerekli

Değişiklikler backend kodunda yapıldı, Docker container'ı rebuild edilmeli:

```powershell
docker-compose down
docker-compose build katana-api
docker-compose up -d
```

Veya hızlı restart:

```powershell
.\force-backend-restart.ps1
```
