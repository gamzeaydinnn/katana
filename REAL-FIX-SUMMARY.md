# 🎯 GERÇEK SORUN BULUNDU VE DÜZELTİLDİ!

## Sorun

`SendStockCardsAsync` metodunda (line 1827) **HARDCODED** değerler vardı:

```csharp
maliyetHesaplanacakFlag = true  // ← HARDCODED!
minStokKontrol = 0              // ← HARDCODED!
```

Ve tevkifat alanları hiç gönderilmiyordu!

## Neden Fark Edemedik?

1. `CreateStockCardAsync` metodunu düzeltmiştik ✅
2. Ama sync flow `SendStockCardsAsync` kullanıyor ❌
3. `SendStockCardsAsync` içinde anonymous object oluşturuluyor
4. Bu object'te değerler HARDCODED yazılmış!

## Düzeltme

`src/Katana.Infrastructure/APIClients/LucaService.Operations.cs` line ~1810:

### ÖNCE (YANLIŞ):

```csharp
var jsonRequest = new
{
    ...
    minStokKontrol = 0,                      // ← HARDCODED 0
    maliyetHesaplanacakFlag = true           // ← HARDCODED true
    // Tevkifat alanları YOK!
};
```

### SONRA (DOĞRU):

```csharp
var jsonRequest = new
{
    ...
    minStokKontrol = card.MinStokKontrol,              // ← DTO'dan al
    maliyetHesaplanacakFlag = card.MaliyetHesaplanacakFlag,  // ← DTO'dan al (int)
    // 🔥 FIX: Tevkifat alanları eklendi
    alisTevkifatOran = card.AlisTevkifatOran ?? "0",
    satisTevkifatOran = card.SatisTevkifatOran ?? "0",
    alisTevkifatKod = card.AlisTevkifatKod,
    satisTevkifatKod = card.SatisTevkifatKod,
    // 🔥 FIX: Diğer eksik alanlar
    gtipKodu = card.GtipKodu ?? "",
    ihracatKategoriNo = card.IhracatKategoriNo ?? "",
    detayAciklama = card.DetayAciklama ?? "",
    stopajOran = card.StopajOran,
    alisIskontoOran1 = card.AlisIskontoOran1,
    satisIskontoOran1 = card.SatisIskontoOran1,
    perakendeAlisBirimFiyat = card.PerakendeAlisBirimFiyat,
    perakendeSatisBirimFiyat = card.PerakendeSatisBirimFiyat,
    rafOmru = card.RafOmru,
    garantiSuresi = card.GarantiSuresi,
    uzunAdi = card.UzunAdi ?? safeName
};
```

## Şimdi Ne Yapmalı?

```powershell
# Hızlı rebuild
.\QUICK-FIX-REBUILD.ps1

# Veya manuel:
docker-compose down
docker-compose build api
docker-compose up -d
```

## Beklenen Sonuç

### Önceki JSON (YANLIŞ):

```json
{
  "kartAdi": "Presli Boru",
  "kartKodu": "PUT. Ø22*1,5",
  ...
  "minStokKontrol": 0,
  "maliyetHesaplanacakFlag": true  ← boolean!
  // Tevkifat alanları YOK
}
```

### Yeni JSON (DOĞRU):

```json
{
  "kartAdi": "Presli Boru",
  "kartKodu": "PUT. Ø22*1,5",
  ...
  "minStokKontrol": 0,
  "maliyetHesaplanacakFlag": 1,     ← int!
  "alisTevkifatOran": "0",          ← YENİ
  "satisTevkifatOran": "0",         ← YENİ
  "alisTevkifatKod": 0,             ← YENİ
  "satisTevkifatKod": 0,            ← YENİ
  "gtipKodu": "",
  "ihracatKategoriNo": "",
  "detayAciklama": "",
  "stopajOran": 0,
  "alisIskontoOran1": 0,
  "satisIskontoOran1": 0,
  "perakendeAlisBirimFiyat": 0,
  "perakendeSatisBirimFiyat": 0,
  "rafOmru": 0,
  "garantiSuresi": 0,
  "uzunAdi": "Presli Boru"
}
```

### Luca Response (BAŞARILI):

```json
{
  "skartId": 79409,
  "error": false,
  "message": "PUT. Ø22*1,5 - Presli Boru stok kartı başarılı bir şekilde kaydedilmiştir."
}
```

## Özet

- ✅ Gerçek sorun bulundu: `SendStockCardsAsync` içinde hardcoded değerler
- ✅ Düzeltildi: Tüm alanlar DTO'dan alınıyor
- ✅ Tevkifat alanları eklendi
- ✅ Diğer eksik alanlar eklendi
- ⏳ Rebuild gerekli: `.\QUICK-FIX-REBUILD.ps1`

---

**SON ADIM**: Rebuild yap ve test et!
