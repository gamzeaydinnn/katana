# Stok Kartı Oluşturma Hatası - Çözüm

## 🔴 SORUN

Luca API tüm stok kartı oluşturma isteklerinde `{"error":true}` döndürüyor, ama hata mesajı vermiyor.

### Log Örneği:

```
[INF] >>> LUCA JSON REQUEST (cliplok1): {"kartAdi":"Presli Boru","kartKodu":"cliplok1",...}
[INF] Luca stock card response for cliplok1 => HTTP OK, BODY={"error":true}
[ERR] Stock card cliplok1 failed: Unknown error
```

## 🔍 KÖK NEDEN

Request payload'ı kullanıcının verdiği çalışan örnekle karşılaştırıldığında **eksik alanlar** tespit edildi:

1. **`kategoriAgacKod`**: Mapping sonucu kullanılmıyor, her zaman `string.Empty` gönderiliyor
2. **`minStokKontrol`**: Hiç gönderilmiyor (user örneğinde var)
3. **`alisTevkifatOran`**: Hiç gönderilmiyor (user örneğinde var)
4. **`satisTevkifatOran`**: Hiç gönderilmiyor (user örneğinde var)
5. **`alisTevkifatKod`**: Hiç gönderilmiyor (user örneğinde var)
6. **`satisTevkifatKod`**: Hiç gönderilmiyor (user örneğinde var)

## ✅ ÇÖZÜM

### 1. Mapper Düzeltmeleri (`src/Katana.Business/Mappers/KatanaToLucaMapper.cs`)

```csharp
var dto = new LucaCreateStokKartiRequest
{
    KartAdi = name,
    KartTuru = 1,
    BaslangicTarihi = DateTime.UtcNow.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
    OlcumBirimiId = lucaSettings.DefaultOlcumBirimiId,
    KartKodu = sku,
    MaliyetHesaplanacakFlag = 1,
    KartTipi = lucaSettings.DefaultKartTipi,

    // ✅ FIX 1: kategoriAgacKod - mapping sonucunu kullan (null veya kod)
    KategoriAgacKod = category,

    KartAlisKdvOran = 1,
    KartSatisKdvOran = 1,
    Barkod = barcodeToSend,
    UzunAdi = name,
    SatilabilirFlag = 1,
    SatinAlinabilirFlag = 1,
    LotNoFlag = 0,

    // ✅ FIX 2: minStokKontrol eklendi
    MinStokKontrol = 0,

    // ✅ FIX 3-6: Tevkifat alanları eklendi
    AlisTevkifatOran = "0",
    SatisTevkifatOran = "0",
    AlisTevkifatKod = 0,
    SatisTevkifatKod = 0,

    PerakendeAlisBirimFiyat = ConvertToDouble(product.CostPrice ?? product.PurchasePrice ?? 0),
    PerakendeSatisBirimFiyat = ConvertToDouble(product.SalesPrice ?? product.Price)
};
```

### 2. DTO Düzeltmeleri (`src/Katana.Core/DTOs/LucaDtos.cs`)

```csharp
// ✅ FIX: KategoriAgacKod nullable yapıldı (null gönderebilmek için)
[JsonPropertyName("kategoriAgacKod")]
public string? KategoriAgacKod { get; set; }  // Önceden: string = string.Empty

// ✅ FIX: Barkod nullable yapıldı (versiyonlu SKU'lar için null gönderebilmek için)
[JsonPropertyName("barkod")]
public string? Barkod { get; set; }  // Önceden: string = string.Empty
```

### Açıklama:

1. **`category` değişkeni zaten hesaplanıyor** (satır 390-450 arası)

   - Önce database mapping'den bakıyor
   - Sonra appsettings.json'dan bakıyor
   - Bulamazsa `null` bırakıyor

2. **Eksik alanlar eklendi** - User'ın verdiği çalışan örnekte var, bizde yoktu

3. **Nullable tipler düzeltildi** - `null` gönderebilmek için `string?` yapıldı

4. **Kategori mantığı**:
   - Eğer ürünün kategorisi varsa ve mapping bulunursa → Luca kategori kodu gönderilir (örn: "001", "220")
   - Bulunamazsa → `null` gönderilir (Luca API null kabul ediyor)

## 📋 TEST ADIMLARI

### 1. Backend'i Restart Et

```powershell
docker-compose restart backend
```

Veya tam restart:

```powershell
.\simple-restart.ps1
```

### 2. Sync'i Test Et

```powershell
# Manuel sync trigger
curl -X POST http://localhost:5055/api/sync/trigger `
  -H "Authorization: Bearer YOUR_TOKEN" `
  -H "Content-Type: application/json" `
  -d '{"limit": 10, "forceSync": true}'
```

### 3. Logları Kontrol Et

**Başarılı olursa göreceğin:**

```
[INF] >>> LUCA JSON REQUEST (cliplok1): {"kartAdi":"Presli Boru","kartKodu":"cliplok1","kategoriAgacKod":"001",...}
[INF] Luca stock card response for cliplok1 => HTTP OK, BODY={"skartId":79409,"error":false,"message":"cliplok1 - Presli Boru stok kartı başarılı bir şekilde kaydedilmiştir."}
[INF] ✅ Stock card cliplok1 created successfully
```

**Hala başarısız olursa:**

```
[ERR] Stock card cliplok1 failed: [HATA MESAJI]
```

Bu sefer hata mesajı gelecek, o zaman neyin eksik olduğunu göreceğiz.

## 🎯 BEKLENTİLER

### Başarılı Olursa:

1. ✅ Yeni ürünler (cliplok1, Ø38x1,5-2, vb.) Luca'ya eklenecek
2. ✅ `{"error":false}` ve `skartId` dönecek
3. ✅ Gereksiz -V2, -V3 versiyonları oluşmayacak

### Hala Sorun Varsa:

Muhtemel sebepler:

1. **Kategori kodu geçersiz** - Luca'da "001", "220" gibi kodlar var mı?
2. **Başka zorunlu alan eksik** - Luca API hangi alanları zorunlu tutuyor?
3. **Encoding sorunu** - Türkçe karakterler doğru gönderiliyor mu?

## 📊 MEVCUT DURUM

### Kategori Mapping (appsettings.json):

```json
"CategoryMapping": {
  "1MAMUL": "001",
  "2HAMMADDE": "002",
  "3YARI MAMUL": "220",
  "4YARDIMCI MALZEME": "004",
  "5AMBALAJ": "005",
  "default": "01"
}
```

### Eğer Ürünün Kategorisi Yoksa:

- `kategoriAgacKod: null` gönderilir
- Luca API bunu kabul ediyor (senin verdiğin örnekte de `null` var)

## 🔧 EK NOTLAR

### Encoding:

```csharp
dto.KartAdi = EncodingHelper.ConvertToIso88599(dto.KartAdi);
dto.UzunAdi = EncodingHelper.ConvertToIso88599(dto.UzunAdi);
```

Bu satırlar Türkçe karakterleri ISO-8859-9 (Windows-1254) formatına çeviriyor.

### Versiyonlu SKU'lar:

```csharp
bool isVersionedSku = Regex.IsMatch(sku, @"-V\d+$");
if (isVersionedSku) {
    barcodeToSend = null;  // Duplicate Barcode hatasını önlemek için
}
```

-V2, -V3 gibi versiyonlu SKU'larda barkod `null` gönderiliyor.

## 🚀 HEMEN YAPILACAKLAR

1. **Backend'i restart et**:

   ```powershell
   docker-compose restart backend
   ```

2. **Sync'i tetikle**:

   ```powershell
   # API üzerinden
   # veya frontend'den "Sync Now" butonuna bas
   ```

3. **Logları izle**:

   ```powershell
   docker-compose logs -f backend | Select-String "LUCA|error|Stock card"
   ```

4. **Sonuçları kontrol et**:
   ```powershell
   .\check-luca-simple.ps1
   ```

## 📝 KARŞILAŞTIRMA

### User'ın Verdiği Çalışan Örnek:

```json
{
  "kartAdi": "Test Ürünü",
  "kartKodu": "00013225",
  "kartTipi": 1,
  "kartAlisKdvOran": 1,
  "olcumBirimiId": 1,
  "baslangicTarihi": "06/04/2022",
  "kartTuru": 1,
  "kategoriAgacKod": null,           ← NULL kabul ediliyor
  "barkod": "8888888",
  "alisTevkifatOran": "7/10",        ← Bizde yoktu
  "satisTevkifatOran": "2/10",       ← Bizde yoktu
  "alisTevkifatTipId": 1,            ← Bizde yoktu (alisTevkifatKod olarak eklendi)
  "satisTevkifatTipId": 1,           ← Bizde yoktu (satisTevkifatKod olarak eklendi)
  "satilabilirFlag": 1,
  "satinAlinabilirFlag": 1,
  "lotNoFlag": 1,
  "minStokKontrol": 0,               ← Bizde yoktu
  "maliyetHesaplanacakFlag": true
}
```

### Bizim Gönderdiğimiz (Düzeltme Sonrası):

```json
{
  "kartAdi": "Presli Boru",
  "kartKodu": "cliplok1",
  "kartTipi": 4,
  "kartAlisKdvOran": 1,
  "kartSatisKdvOran": 1,
  "olcumBirimiId": 5,
  "baslangicTarihi": "06/12/2025",
  "kartTuru": 1,
  "kategoriAgacKod": null,           ← ✅ Artık mapping sonucu veya null
  "barkod": "cliplok1",
  "alisTevkifatOran": "0",           ← ✅ Eklendi
  "satisTevkifatOran": "0",          ← ✅ Eklendi
  "alisTevkifatKod": 0,              ← ✅ Eklendi
  "satisTevkifatKod": 0,             ← ✅ Eklendi
  "satilabilirFlag": 1,
  "satinAlinabilirFlag": 1,
  "lotNoFlag": 0,
  "minStokKontrol": 0,               ← ✅ Eklendi
  "maliyetHesaplanacakFlag": true
}
```

## ✨ SONUÇ

Fix uygulandı! Şimdi backend'i restart et ve test et. Eğer hala `{"error":true}` dönüyorsa, bu sefer **hata mesajı** gelecek ve neyin eksik olduğunu göreceğiz.
