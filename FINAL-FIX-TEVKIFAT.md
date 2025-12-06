# 🎯 SON DÜZELTİLME: Tevkifat Alan Adları

## Sorun

Luca API'nin beklediği alan adları farklıymış:

### Bizim Kullandığımız (YANLIŞ):

```json
{
  "alisTevkifatKod": 0,      ← YANLIŞ alan adı
  "satisTevkifatKod": 0      ← YANLIŞ alan adı
}
```

### Luca'nın Beklediği (DOĞRU):

```json
{
  "alisTevkifatTipId": 1,    ← DOĞRU alan adı
  "satisTevkifatTipId": 1    ← DOĞRU alan adı
}
```

## Düzeltme

`src/Katana.Infrastructure/APIClients/LucaService.Operations.cs`:

### ÖNCE:

```csharp
alisTevkifatOran = card.AlisTevkifatOran ?? "0",
satisTevkifatOran = card.SatisTevkifatOran ?? "0",
alisTevkifatKod = card.AlisTevkifatKod,        // ← YANLIŞ alan adı
satisTevkifatKod = card.SatisTevkifatKod,      // ← YANLIŞ alan adı
```

### SONRA:

```csharp
// Tevkifat oranları: "7/10" formatında veya null
alisTevkifatOran = string.IsNullOrEmpty(card.AlisTevkifatOran) || card.AlisTevkifatOran == "0"
    ? (string?)null
    : card.AlisTevkifatOran,
satisTevkifatOran = string.IsNullOrEmpty(card.SatisTevkifatOran) || card.SatisTevkifatOran == "0"
    ? (string?)null
    : card.SatisTevkifatOran,

// Tevkifat tip ID'leri: int veya null
alisTevkifatTipId = card.AlisTevkifatKod > 0 ? (int?)card.AlisTevkifatKod : null,
satisTevkifatTipId = card.SatisTevkifatKod > 0 ? (int?)card.SatisTevkifatKod : null,
```

## Rebuild

```powershell
docker-compose down
docker-compose build api
docker-compose up -d
```

## Beklenen JSON

```json
{
  "kartAdi": "O38x1,5-2",
  "kartKodu": "Ø38x1,5-2",
  "kartTipi": 4,
  "kartAlisKdvOran": 1,
  "kartSatisKdvOran": 1,
  "olcumBirimiId": 5,
  "baslangicTarihi": "06/12/2025",
  "kartTuru": 1,
  "barkod": "Ø38x1,5-2",
  "satilabilirFlag": 1,
  "satinAlinabilirFlag": 1,
  "lotNoFlag": 0,
  "minStokKontrol": 0,
  "maliyetHesaplanacakFlag": 1,
  "alisTevkifatOran": null,          ← null (çünkü "0" idi)
  "satisTevkifatOran": null,         ← null (çünkü "0" idi)
  "alisTevkifatTipId": null,         ← DOĞRU alan adı, null (çünkü 0 idi)
  "satisTevkifatTipId": null,        ← DOĞRU alan adı, null (çünkü 0 idi)
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
  "uzunAdi": "Ø38x1,5-2"
}
```

## Beklenen Response

```json
{
  "skartId": 79409,
  "error": false,
  "message": "Ø38x1,5-2 - O38x1,5-2 stok kartı başarılı bir şekilde kaydedilmiştir."
}
```

---

**SON ADIM**: Rebuild yap!

```powershell
docker-compose down && docker-compose build api && docker-compose up -d
```
