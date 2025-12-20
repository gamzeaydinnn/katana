# Lucaya Fatura Gönderimi - Teknik Referans Belgesi

## 📌 İçindekiler

1. [Request JSON Yapısı](#1-request-json-yapısı)
2. [Response JSON Yapısı](#2-response-json-yapısı)
3. [Hata Yönetimi](#3-hata-yönetimi)
4. [Kod Örnekleri](#4-kod-örnekleri)
5. [Belgetur Detay ID'leri](#5-belgetur-detay-idleri)
6. [Validasyon Kuralları](#6-validasyon-kuralları)

---

## 1. Request JSON Yapısı

### 1.1 Tam Request Örneği (Satış Faturası)

```json
{
  "belgeSeri": "A",
  "belgeNo": null,
  "belgeTarihi": "07/10/2025",
  "duzenlemeSaati": null,
  "vadeTarihi": "06/11/2025",
  "belgeTakipNo": "SF-20251007-143022",
  "belgeAciklama": "Test satis faturasi - Katana Integration",
  "belgeTurDetayId": "76",
  "belgeAttribute1Deger": null,
  "belgeAttribute1Ack": null,
  "belgeAttribute2Deger": null,
  "belgeAttribute2Ack": null,
  "belgeAttribute3Deger": null,
  "belgeAttribute3Ack": null,
  "belgeAttribute4Deger": null,
  "belgeAttribute4Ack": null,
  "belgeAttribute5Deger": null,
  "belgeAttribute5Ack": null,
  "faturaTur": "1",
  "paraBirimKod": "TRY",
  "kurBedeli": 1.0,
  "babsFlag": false,
  "kdvFlag": true,
  "referansNo": null,
  "musteriTedarikci": "1",
  "cariKodu": "MUSTERI-001",
  "cariTanim": "Test Musteri A.S.",
  "cariTip": 1,
  "cariKisaAd": "Test Musteri",
  "cariYasalUnvan": "Test Musteri Anonim Sirketi",
  "vergiNo": "1234567890",
  "vergiDairesi": "Kadikoy",
  "cariAd": null,
  "cariSoyad": null,
  "tcKimlikNo": null,
  "il": null,
  "ilce": null,
  "mahallesemt": null,
  "caddesokak": null,
  "diskapino": null,
  "ickapino": null,
  "postaKodu": null,
  "adresSerbest": "Test Adres, Istanbul",
  "telefon": null,
  "email": null,
  "iletisimTanim": "0212 555 1234",
  "webAdresi": null,
  "kargoVknTckn": null,
  "odemeTipi": null,
  "gonderimTipi": null,
  "siparisTarihi": null,
  "siparisNo": null,
  "yuklemeTarihi": null,
  "tevkifatOran": null,
  "tevkifatKod": null,
  "earsivNo": null,
  "efaturaNo": null,
  "irsaliyeBilgisiList": [],
  "fhAttribute1Deger": null,
  "fhAttribute1Ack": null,
  "fhAttribute2Deger": null,
  "fhAttribute2Ack": null,
  "fhAttribute3Deger": null,
  "fhAttribute3Ack": null,
  "fhAttribute4Deger": null,
  "fhAttribute4Ack": null,
  "fhAttribute5Deger": null,
  "fhAttribute5Ack": null,
  "efaturaTuru": null,
  "detayList": [
    {
      "kartTuru": 1,
      "kartKodu": "0200B501-0003",
      "hesapKod": null,
      "kartAdi": "KAYNAKLI BORU UCU O R",
      "kartTipi": null,
      "barkod": null,
      "olcuBirimi": null,
      "kdvOran": 0.2,
      "kartSatisKdvOran": null,
      "depoKodu": "01",
      "birimFiyat": 150.0,
      "miktar": 2,
      "tutar": 300.0,
      "iskontoOran1": null,
      "iskontoOran2": null,
      "iskontoOran3": null,
      "iskontoOran4": null,
      "iskontoOran5": null,
      "iskontoOran6": null,
      "iskontoOran7": null,
      "iskontoOran8": null,
      "iskontoOran9": null,
      "iskontoOran10": null,
      "otvOran": null,
      "stopajOran": null,
      "lotNo": null,
      "aciklama": null,
      "garantiSuresi": null,
      "uretimTarihi": null,
      "konaklamaVergiOran": null
    },
    {
      "kartTuru": 1,
      "kartKodu": "0200B501-A",
      "hesapKod": null,
      "kartAdi": "0200B501 BUKUMLU BORU",
      "kartTipi": null,
      "barkod": null,
      "olcuBirimi": null,
      "kdvOran": 0.2,
      "kartSatisKdvOran": null,
      "depoKodu": "01",
      "birimFiyat": 200.0,
      "miktar": 1,
      "tutar": 200.0,
      "iskontoOran1": null,
      "iskontoOran2": null,
      "iskontoOran3": null,
      "iskontoOran4": null,
      "iskontoOran5": null,
      "iskontoOran6": null,
      "iskontoOran7": null,
      "iskontoOran8": null,
      "iskontoOran9": null,
      "iskontoOran10": null,
      "otvOran": null,
      "stopajOran": null,
      "lotNo": null,
      "aciklama": null,
      "garantiSuresi": null,
      "uretimTarihi": null,
      "konaklamaVergiOran": null
    }
  ]
}
```

### 1.2 Minimal Request (Zorunlu Alanlar Sadece)

```json
{
  "belgeSeri": "A",
  "belgeTarihi": "07/10/2025",
  "belgeTurDetayId": "76",
  "faturaTur": "1",
  "paraBirimKod": "TRY",
  "kurBedeli": 1.0,
  "kdvFlag": true,
  "musteriTedarikci": "1",
  "cariKodu": "MUSTERI-001",
  "cariTanim": "Test Musteri",
  "vergiNo": "1234567890",
  "detayList": [
    {
      "kartTuru": 1,
      "kartKodu": "0200B501-0003",
      "kartAdi": "KAYNAKLI BORU",
      "birimFiyat": 150.0,
      "miktar": 2,
      "kdvOran": 0.2,
      "tutar": 300.0
    }
  ]
}
```

### 1.3 Request Alanları Referans Tablosu

#### Belge Başlığı (Header)

| Alan            | Tip     | Zorunlu | Min | Max | Örnek           | Açıklama                        |
| --------------- | ------- | ------- | --- | --- | --------------- | ------------------------------- |
| belgeSeri       | string  | ✅      | 1   | 1   | "A"             | Fatura serisi                   |
| belgeNo         | int?    | ❌      | -   | -   | null            | Fatura numarası (null=otomatik) |
| belgeTarihi     | string  | ✅      | -   | -   | "07/10/2025"    | Tarih (dd/MM/yyyy)              |
| vadeTarihi      | string? | ❌      | -   | -   | "06/11/2025"    | Vade tarihi (dd/MM/yyyy)        |
| belgeTakipNo    | string? | ❌      | -   | 50  | "SF-20251007"   | Takip numarası                  |
| belgeAciklama   | string? | ❌      | -   | 250 | "Test faturası" | Açıklama                        |
| belgeTurDetayId | string  | ✅      | -   | -   | "76"            | Belge türü ID                   |

#### Fatura Türü

| Alan         | Tip    | Zorunlu | Örnek | Açıklama                     |
| ------------ | ------ | ------- | ----- | ---------------------------- |
| faturaTur    | string | ✅      | "1"   | 1=Normal, 2=İade, 3=Proforma |
| paraBirimKod | string | ✅      | "TRY" | Para birimi (TRY, USD, EUR)  |
| kurBedeli    | double | ✅      | 1.0   | Kur bedeli                   |
| kdvFlag      | bool   | ✅      | true  | KDV uygulanacak mı?          |
| babsFlag     | bool   | ❌      | false | BABS uygulanacak mı?         |

#### Müşteri Bilgileri

| Alan             | Tip     | Zorunlu | Min | Max | Örnek           | Açıklama               |
| ---------------- | ------- | ------- | --- | --- | --------------- | ---------------------- |
| musteriTedarikci | string  | ✅      | -   | -   | "1"             | 1=Müşteri, 2=Tedarikçi |
| cariKodu         | string  | ✅      | 1   | 20  | "MUSTERI-001"   | Müşteri kodu           |
| cariTanim        | string  | ✅      | 1   | 100 | "Test Musteri"  | Müşteri adı            |
| cariTip          | int?    | ❌      | -   | -   | 1               | 1=Şirket, 2=Kişi       |
| vergiNo          | string? | ❌      | -   | 20  | "1234567890"    | Vergi numarası         |
| vergiDairesi     | string? | ❌      | -   | 50  | "Kadikoy"       | Vergi dairesi          |
| adresSerbest     | string? | ❌      | -   | 500 | "Test Adres"    | Serbest adres          |
| iletisimTanim    | string? | ❌      | -   | 50  | "0212 555 1234" | İletişim               |

#### Fatura Kalemleri (detayList)

| Alan       | Tip     | Zorunlu | Örnek           | Açıklama             |
| ---------- | ------- | ------- | --------------- | -------------------- |
| kartTuru   | int     | ✅      | 1               | 1=Stok, 2=Hizmet     |
| kartKodu   | string  | ✅      | "0200B501-0003" | Ürün kodu            |
| kartAdi    | string  | ✅      | "KAYNAKLI BORU" | Ürün adı             |
| birimFiyat | double  | ✅      | 150.0           | Birim fiyat          |
| miktar     | double  | ✅      | 2               | Miktar               |
| kdvOran    | double  | ✅      | 0.20            | KDV oranı (0.20=%20) |
| tutar      | double? | ❌      | 300.0           | Satır tutarı         |
| depoKodu   | string? | ❌      | "01"            | Depo kodu            |
| hesapKod   | string? | ❌      | "600"           | Muhasebe hesabı      |

---

## 2. Response JSON Yapısı

### 2.1 Başarılı Response

```json
{
  "isSuccess": true,
  "message": "Invoices sent successfully to Luca",
  "processedRecords": 1,
  "successfulRecords": 1,
  "failedRecords": 0,
  "duplicateRecords": 0,
  "sentRecords": 0,
  "skippedRecords": 0,
  "isDryRun": false,
  "errors": [],
  "syncTime": "2025-10-07T14:30:22.1234567Z",
  "syncType": "INVOICE",
  "duration": "00:00:02.5000000",
  "totalChecked": 0,
  "alreadyExists": 0,
  "newCreated": 0,
  "failed": 0,
  "details": []
}
```

### 2.2 Başarısız Response (Hata)

```json
{
  "isSuccess": false,
  "message": "1 succeeded, 1 failed",
  "processedRecords": 2,
  "successfulRecords": 1,
  "failedRecords": 1,
  "duplicateRecords": 0,
  "sentRecords": 0,
  "skippedRecords": 0,
  "isDryRun": false,
  "errors": [
    "SF-20251007-143022: code=1001 message=Luca session süresi dolmuş, lütfen tekrar giriş yapınız"
  ],
  "syncTime": "2025-10-07T14:30:22.1234567Z",
  "syncType": "INVOICE",
  "duration": "00:00:05.2000000",
  "totalChecked": 0,
  "alreadyExists": 0,
  "newCreated": 0,
  "failed": 0,
  "details": []
}
```

### 2.3 Response Alanları

| Alan                | Tip          | Açıklama                |
| ------------------- | ------------ | ----------------------- |
| `isSuccess`         | bool         | İşlem başarılı mı?      |
| `message`           | string       | Özet mesaj              |
| `processedRecords`  | int          | İşlenen toplam kayıt    |
| `successfulRecords` | int          | Başarılı kayıt sayısı   |
| `failedRecords`     | int          | Başarısız kayıt sayısı  |
| `duplicateRecords`  | int          | Yinelenen kayıt sayısı  |
| `sentRecords`       | int          | Gönderilen kayıt sayısı |
| `skippedRecords`    | int          | Atlanan kayıt sayısı    |
| `isDryRun`          | bool         | Kuru çalışma mı?        |
| `errors`            | List<string> | Hata mesajları          |
| `syncTime`          | DateTime     | İşlem zamanı (UTC)      |
| `syncType`          | string       | Senkronizasyon türü     |
| `duration`          | TimeSpan     | İşlem süresi            |

---

## 3. Hata Yönetimi

### 3.1 Luca API Hata Kodları

```json
{
  "code": 1001,
  "message": "Luca session süresi dolmuş, lütfen tekrar giriş yapınız"
}
```

#### Hata Kodları Tablosu

| Kod  | Mesaj                  | Çözüm                            |
| ---- | ---------------------- | -------------------------------- |
| 0    | Başarılı               | -                                |
| 1001 | Session timeout        | Sistem otomatik refresh yapar    |
| 1002 | Unauthorized           | Kullanıcı adı/şifre kontrol edin |
| 1003 | Invalid request        | Request JSON'u kontrol edin      |
| 1004 | Record not found       | Ürün/müşteri kodu kontrol edin   |
| 1005 | Duplicate record       | Kayıt zaten mevcut               |
| 1006 | Invalid field value    | Alan değeri kontrol edin         |
| 1007 | Missing required field | Zorunlu alan eksik               |

### 3.2 Hata Mesajı Parsing

```csharp
// Luca'dan dönen response
{
  "code": 1001,
  "message": "Luca session süresi dolmuş"
}

// Katana'nın oluşturduğu error mesajı
"SF-20251007-143022: code=1001 message=Luca session süresi dolmuş"

// Format: "{belgeTakipNo}: code={code} message={message}"
```

### 3.3 Retry Mekanizması

```
Deneme 1: İlk gönderim
  ↓
  Başarılı? → Bitir ✅
  ↓
  Hata (code=1001)? → Session refresh
  ↓
Deneme 2: Tekrar gönderim
  ↓
  Başarılı? → Bitir ✅
  ↓
  Hata? → Hata döndür ❌
```

---

## 4. Kod Örnekleri

### 4.1 C# - Request Oluşturma

```csharp
var request = new LucaCreateInvoiceHeaderRequest
{
    BelgeSeri = "A",
    BelgeTarihi = DateTime.Now.ToString("dd/MM/yyyy"),
    BelgeTurDetayId = "76",  // Mal Satış Faturası
    FaturaTur = "1",
    ParaBirimKod = "TRY",
    KurBedeli = 1.0,
    KdvFlag = true,
    MusteriTedarikci = "1",
    CariKodu = "MUSTERI-001",
    CariTanim = "Test Musteri",
    VergiNo = "1234567890",
    DetayList = new List<LucaCreateInvoiceDetailRequest>
    {
        new LucaCreateInvoiceDetailRequest
        {
            KartTuru = 1,
            KartKodu = "0200B501-0003",
            KartAdi = "KAYNAKLI BORU",
            BirimFiyat = 150.0,
            Miktar = 2,
            KdvOran = 0.20,
            Tutar = 300.0
        }
    }
};
```

### 4.2 C# - JSON Serileştirme

```csharp
var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false
};

var json = JsonSerializer.Serialize(request, options);
```

### 4.3 C# - API Çağrısı

```csharp
using var client = new HttpClient();
client.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", token);

var content = new StringContent(
    json,
    Encoding.UTF8,
    "application/json"
);

var response = await client.PostAsync(
    "http://localhost:5055/api/sync/to-luca/sales-invoice",
    content
);

var responseJson = await response.Content.ReadAsStringAsync();
var result = JsonSerializer.Deserialize<SyncResultDto>(responseJson);
```

### 4.4 PowerShell - API Çağrısı

```powershell
$invoiceRequest = @{
    belgeSeri = "A"
    belgeTarihi = (Get-Date).ToString("dd/MM/yyyy")
    belgeTurDetayId = "76"
    cariKodu = "MUSTERI-001"
    cariTanim = "Test Musteri"
    vergiNo = "1234567890"
    detayList = @(
        @{
            kartKodu = "0200B501-0003"
            kartAdi = "KAYNAKLI BORU"
            birimFiyat = 150.0
            miktar = 2
            kdvOran = 0.20
        }
    )
}

$json = $invoiceRequest | ConvertTo-Json -Depth 10

$response = Invoke-RestMethod `
    -Uri "http://localhost:5055/api/sync/to-luca/sales-invoice" `
    -Method POST `
    -Body $json `
    -ContentType "application/json; charset=utf-8"

$response | ConvertTo-Json
```

### 4.5 JavaScript/TypeScript - API Çağrısı

```typescript
const invoiceRequest = {
  belgeSeri: "A",
  belgeTarihi: new Date().toLocaleDateString("tr-TR"),
  belgeTurDetayId: "76",
  cariKodu: "MUSTERI-001",
  cariTanim: "Test Musteri",
  vergiNo: "1234567890",
  detayList: [
    {
      kartKodu: "0200B501-0003",
      kartAdi: "KAYNAKLI BORU",
      birimFiyat: 150.0,
      miktar: 2,
      kdvOran: 0.2,
    },
  ],
};

const response = await fetch(
  "http://localhost:5055/api/sync/to-luca/sales-invoice",
  {
    method: "POST",
    headers: {
      "Content-Type": "application/json; charset=utf-8",
    },
    body: JSON.stringify(invoiceRequest),
  }
);

const result = await response.json();
console.log(result);
```

---

## 5. Belgetur Detay ID'leri

### 5.1 Satış Faturaları

| ID  | Türü                     | Açıklama              |
| --- | ------------------------ | --------------------- |
| 76  | Mal Satış Faturası       | Normal satış faturası |
| 77  | Proforma Satış Faturası  | Ön fatura             |
| 78  | Kur Farkı Satış Faturası | Kur farkı düzeltmesi  |
| 79  | Satış İade Faturası      | İade faturası         |

### 5.2 Alım Faturaları

| ID  | Türü                    | Açıklama             |
| --- | ----------------------- | -------------------- |
| 69  | Alım Faturası           | Normal alım faturası |
| 70  | Proforma Alım Faturası  | Ön fatura            |
| 71  | Kur Farkı Alış Faturası | Kur farkı düzeltmesi |
| 72  | Alım İade Faturası      | İade faturası        |

### 5.3 Diğer Belgeler

| ID  | Türü                | Açıklama            |
| --- | ------------------- | ------------------- |
| 80  | İrsaliye            | Sevkiyat belgesi    |
| 81  | Satın Alma Siparişi | Sipariş belgesi     |
| 82  | Depo Transferi      | Depo arası transfer |

---

## 6. Validasyon Kuralları

### 6.1 Tarih Formatı

```
✅ DOĞRU:  "07/10/2025"  (dd/MM/yyyy)
❌ YANLIŞ: "2025-10-07"  (yyyy-MM-dd)
❌ YANLIŞ: "10/07/2025"  (MM/dd/yyyy)
❌ YANLIŞ: "7/10/2025"   (d/MM/yyyy - eksik sıfır)
```

### 6.2 KDV Oranı

```
✅ DOĞRU:  0.20  (20% KDV)
✅ DOĞRU:  0.08  (8% KDV)
✅ DOĞRU:  0.01  (1% KDV)
❌ YANLIŞ: 20    (yüzde değil, ondalık)
❌ YANLIŞ: "0.20" (string değil, number)
```

### 6.3 Müşteri Kodu

```
✅ DOĞRU:  "MUSTERI-001"
✅ DOĞRU:  "CUST_12345"
✅ DOĞRU:  "M001"
❌ YANLIŞ: "" (boş)
❌ YANLIŞ: null (null)
❌ YANLIŞ: "MUSTERI-001 " (sondaki boşluk)
```

### 6.4 Ürün Kodu

```
✅ DOĞRU:  "0200B501-0003"
✅ DOĞRU:  "PIPE-001"
✅ DOĞRU:  "SKU123"
❌ YANLIŞ: "" (boş)
❌ YANLIŞ: null (null)
❌ YANLIŞ: "0200B501-0003 " (sondaki boşluk)
```

### 6.5 Belgetur Detay ID

```
✅ DOĞRU:  "76"  (string)
✅ DOĞRU:  "69"  (string)
❌ YANLIŞ: 76    (number)
❌ YANLIŞ: 69    (number)
```

### 6.6 Fatura Türü

```
✅ DOĞRU:  "1"  (Normal)
✅ DOĞRU:  "2"  (İade)
❌ YANLIŞ: 1    (number)
❌ YANLIŞ: "Normal" (string açıklama)
```

### 6.7 Müşteri Tipi

```
✅ DOĞRU:  1     (Şirket)
✅ DOĞRU:  2     (Kişi)
❌ YANLIŞ: "1"   (string)
❌ YANLIŞ: 0     (geçersiz)
```

### 6.8 Kart Türü

```
✅ DOĞRU:  1     (Stok)
✅ DOĞRU:  2     (Hizmet)
❌ YANLIŞ: "1"   (string)
❌ YANLIŞ: 0     (geçersiz)
```

---

## 📝 Notlar

### Encoding

- UTF-8 encoding kullanılmalı
- Türkçe karakterler (Ü, Ö, Ş, Ç, Ğ, İ) destekleniyor
- Diameter sembolü (Ø) → "O" olarak normalize ediliyor

### Session Yönetimi

- Luca session timeout olabilir (code=1001)
- Katana otomatik olarak session refresh yapar
- Maksimum 2 retry denemesi yapılır

### Depo Kodu

- Opsiyonel alan
- Boş bırakılırsa Luca varsayılan depoyu kullanır
- Örnek: "01", "02", "MERKEZ"

### Hesap Kodu

- Opsiyonel alan
- Muhasebe entegrasyonu için kullanılır
- Örnek: "600", "700", "800"

---

## 🔗 İlgili Dosyalar

- `LucaCreateInvoiceHeaderRequest` - Request DTO
- `LucaCreateInvoiceDetailRequest` - Kalem DTO
- `SyncResultDto` - Response DTO
- `KozaBelgeTurleri` - Belgetur ID'leri
- `LucaService.Operations.cs` - Gönderim kodu
