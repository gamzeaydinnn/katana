# Lucaya Fatura Gönderimi - JSON Yapısı ve Response Analizi

## 📋 Özet

Katana sisteminden Lucaya fatura gönderirken aşağıdaki JSON yapısı kullanılıyor ve Luca'dan belirli bir response formatı dönüyor.

---

## 1️⃣ GÖNDERILEN JSON (Request)

### Endpoint

```
POST /api/sync/to-luca/sales-invoice
Content-Type: application/json; charset=utf-8
```

### Request DTO: `LucaCreateInvoiceHeaderRequest`

```json
{
  "belgeSeri": "A",
  "belgeNo": null,
  "belgeTarihi": "07/10/2025",
  "vadeTarihi": "06/11/2025",
  "belgeTakipNo": "SF-20251007-143022",
  "belgeAciklama": "Test satis faturasi - Katana Integration",
  "belgeTurDetayId": "76",
  "faturaTur": "1",
  "paraBirimKod": "TRY",
  "kurBedeli": 1.0,
  "babsFlag": false,
  "kdvFlag": true,
  "musteriTedarikci": "1",
  "cariKodu": "MUSTERI-001",
  "cariTanim": "Test Musteri A.S.",
  "cariTip": 1,
  "cariKisaAd": "Test Musteri",
  "cariYasalUnvan": "Test Musteri Anonim Sirketi",
  "vergiNo": "1234567890",
  "vergiDairesi": "Kadikoy",
  "adresSerbest": "Test Adres, Istanbul",
  "iletisimTanim": "0212 555 1234",
  "detayList": [
    {
      "kartTuru": 1,
      "kartKodu": "0200B501-0003",
      "kartAdi": "KAYNAKLI BORU UCU O R",
      "birimFiyat": 150.0,
      "miktar": 2,
      "kdvOran": 0.2,
      "tutar": 300.0,
      "depoKodu": "01"
    },
    {
      "kartTuru": 1,
      "kartKodu": "0200B501-A",
      "kartAdi": "0200B501 BUKUMLU BORU",
      "birimFiyat": 200.0,
      "miktar": 1,
      "kdvOran": 0.2,
      "tutar": 200.0,
      "depoKodu": "01"
    }
  ]
}
```

---

## 📊 Request Alanları Detaylı Açıklama

### Belge Bilgileri (Zorunlu)

| Alan              | Tip    | Örnek        | Açıklama                                    |
| ----------------- | ------ | ------------ | ------------------------------------------- |
| `belgeSeri`       | string | "A"          | Fatura serisi (A, B, C, vb.)                |
| `belgeTarihi`     | string | "07/10/2025" | Fatura tarihi (dd/MM/yyyy formatı)          |
| `belgeTurDetayId` | string | "76"         | Belge türü detay ID (76=Mal Satış Faturası) |
| `faturaTur`       | string | "1"          | Fatura türü (1=Normal Fatura)               |

### Para Bilgileri

| Alan           | Tip    | Örnek | Açıklama         |
| -------------- | ------ | ----- | ---------------- |
| `paraBirimKod` | string | "TRY" | Para birimi kodu |
| `kurBedeli`    | double | 1.0   | Kur bedeli       |

### Müşteri Bilgileri (Zorunlu)

| Alan               | Tip    | Örnek               | Açıklama                         |
| ------------------ | ------ | ------------------- | -------------------------------- |
| `musteriTedarikci` | string | "1"                 | 1=Müşteri, 2=Tedarikçi           |
| `cariKodu`         | string | "MUSTERI-001"       | Müşteri kodu (Luca'da benzersiz) |
| `cariTanim`        | string | "Test Musteri A.S." | Müşteri tanımı                   |
| `cariTip`          | int    | 1                   | 1=Şirket, 2=Kişi                 |
| `vergiNo`          | string | "1234567890"        | Vergi numarası                   |

### Adres Bilgileri

| Alan            | Tip    | Örnek                  | Açıklama                         |
| --------------- | ------ | ---------------------- | -------------------------------- |
| `adresSerbest`  | string | "Test Adres, Istanbul" | Serbest adres (500 karakter max) |
| `vergiDairesi`  | string | "Kadikoy"              | Vergi dairesi                    |
| `iletisimTanim` | string | "0212 555 1234"        | İletişim bilgisi                 |

### Opsiyonel Alanlar

| Alan            | Tip       | Açıklama                                      |
| --------------- | --------- | --------------------------------------------- |
| `belgeNo`       | int?      | Fatura numarası (null ise Luca otomatik atar) |
| `vadeTarihi`    | string    | Vade tarihi (dd/MM/yyyy)                      |
| `belgeTakipNo`  | string    | Fatura takip numarası                         |
| `belgeAciklama` | string    | Fatura açıklaması (250 karakter max)          |
| `referansNo`    | string    | Referans numarası                             |
| `siparisNo`     | string    | Sipariş numarası                              |
| `siparisTarihi` | DateTime? | Sipariş tarihi                                |

### Fatura Kalemleri (detayList)

Her kalem için `LucaCreateInvoiceDetailRequest`:

| Alan         | Tip     | Örnek                   | Açıklama                            |
| ------------ | ------- | ----------------------- | ----------------------------------- |
| `kartTuru`   | int     | 1                       | 1=Stok, 2=Hizmet                    |
| `kartKodu`   | string  | "0200B501-0003"         | Ürün kodu (Luca'da mevcut olmalı)   |
| `kartAdi`    | string  | "KAYNAKLI BORU UCU O R" | Ürün adı                            |
| `birimFiyat` | double  | 150.0                   | Birim fiyat                         |
| `miktar`     | double  | 2                       | Miktar                              |
| `kdvOran`    | double  | 0.20                    | KDV oranı (0.20 = %20)              |
| `tutar`      | double? | 300.0                   | Satır tutarı (birimFiyat \* miktar) |
| `depoKodu`   | string? | "01"                    | Depo kodu                           |
| `hesapKod`   | string? | null                    | Muhasebe hesap kodu                 |

---

## 2️⃣ DÖNEN RESPONSE (Response)

### Response DTO: `SyncResultDto`

#### Başarılı Yanıt Örneği

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
  "duration": "00:00:02.5000000"
}
```

#### Başarısız Yanıt Örneği

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
    "SF-20251007-143022: code=1001 message=Luca session süresi dolmuş"
  ],
  "syncTime": "2025-10-07T14:30:22.1234567Z",
  "syncType": "INVOICE",
  "duration": "00:00:05.2000000"
}
```

### Response Alanları

| Alan                | Tip          | Açıklama                        |
| ------------------- | ------------ | ------------------------------- |
| `isSuccess`         | bool         | İşlem başarılı mı?              |
| `message`           | string       | Özet mesaj                      |
| `processedRecords`  | int          | İşlenen toplam kayıt sayısı     |
| `successfulRecords` | int          | Başarılı kayıt sayısı           |
| `failedRecords`     | int          | Başarısız kayıt sayısı          |
| `errors`            | List<string> | Hata mesajları listesi          |
| `syncTime`          | DateTime     | İşlem zamanı (UTC)              |
| `syncType`          | string       | Senkronizasyon türü ("INVOICE") |
| `duration`          | TimeSpan     | İşlem süresi                    |

---

## 3️⃣ LUCA API'DEN DÖNEN RESPONSE (İç Yapı)

Katana, Luca API'den aşağıdaki JSON formatında response alıyor:

### Başarılı Response

```json
{
  "code": 0,
  "message": "Başarılı"
}
```

### Hata Response

```json
{
  "code": 1001,
  "message": "Luca session süresi dolmuş, lütfen tekrar giriş yapınız"
}
```

### Hata Kodları

| Kod  | Anlamı           |
| ---- | ---------------- |
| 0    | Başarılı         |
| 1001 | Session timeout  |
| 1002 | Unauthorized     |
| 1003 | Invalid request  |
| 1004 | Record not found |
| 1005 | Duplicate record |

---

## 4️⃣ BELGETUR DETAY ID'LERİ (belgeTurDetayId)

```csharp
public static class KozaBelgeTurleri
{
    // Satış Faturaları
    public const long MalSatisFaturasi = 76;           // Mal Satış Faturası
    public const long ProformaSatisFaturasi = 77;      // Proforma Satış Faturası
    public const long KurFarkiSatisFaturasi = 78;      // Kur Farkı Satış Faturası
    public const long SatisIadeFaturasi = 79;          // Satış İade Faturası

    // Alım Faturaları
    public const long AlimFaturasi = 69;               // Alım Faturası
    public const long ProformaAlimFaturasi = 70;       // Proforma Alım Faturası
    public const long KurFarkiAlisFaturasi = 71;       // Kur Farkı Alış Faturası
    public const long AlimIadeFaturasi = 72;           // Alım İade Faturası
}
```

---

## 5️⃣ FATURA GÖNDERME AKIŞI (Kod Analizi)

### Adım 1: Request Oluşturma

```csharp
var request = new LucaCreateInvoiceHeaderRequest
{
    BelgeSeri = "A",
    BelgeTarihi = DateTime.Now.ToString("dd/MM/yyyy"),
    // ... diğer alanlar
    DetayList = new List<LucaCreateInvoiceDetailRequest>
    {
        new LucaCreateInvoiceDetailRequest
        {
            KartKodu = "0200B501-0003",
            BirimFiyat = 150.0,
            Miktar = 2,
            KdvOran = 0.20
        }
    }
};
```

### Adım 2: JSON Serileştirme

```csharp
var json = JsonSerializer.Serialize(request, _jsonOptions);
// Sonuç: Yukarıdaki JSON string
```

### Adım 3: Luca API'ye Gönderme

```csharp
var content = new ByteArrayContent(encoding.GetBytes(json));
content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
{
    CharSet = encoding.WebName
};

var response = await httpClient.PostAsync(endpoint, content);
```

### Adım 4: Response Parsing

```csharp
var responseBody = await response.Content.ReadAsStringAsync();
var (isSuccess, message) = ParseKozaOperationResponse(responseBody);

// ParseKozaOperationResponse:
// - Luca'dan gelen JSON'u parse eder
// - "code" alanını kontrol eder (0=başarılı, diğer=hata)
// - "message" alanını alır
// - (bool isSuccess, string? message) tuple döner
```

### Adım 5: SyncResultDto Oluşturma

```csharp
var result = new SyncResultDto
{
    IsSuccess = isSuccess,
    Message = isSuccess ? "Invoices sent successfully" : message,
    SuccessfulRecords = isSuccess ? 1 : 0,
    FailedRecords = isSuccess ? 0 : 1,
    SyncType = "INVOICE",
    Duration = DateTime.UtcNow - startTime
};
```

---

## 6️⃣ ÖNEMLI NOTLAR

### ✅ Zorunlu Alanlar

- `belgeSeri` - Fatura serisi
- `belgeTarihi` - Fatura tarihi (dd/MM/yyyy formatı)
- `belgeTurDetayId` - Belge türü detay ID
- `cariKodu` - Müşteri kodu
- `detayList` - En az 1 kalem

### ⚠️ Tarih Formatı

- **Luca beklediği format**: `"dd/MM/yyyy"` (string)
- **Örnek**: `"07/10/2025"` (7 Ekim 2025)
- **YANLIŞ**: `"2025-10-07"` veya `"10/07/2025"`

### 🔄 Session Yönetimi

- Luca session timeout olabilir (code=1001)
- Katana otomatik olarak session refresh yapar
- Maksimum 2 retry denemesi yapılır

### 📝 Encoding

- Content-Type: `application/json; charset=utf-8`
- Türkçe karakterler (Ü, Ö, Ş, Ç, Ğ, İ) destekleniyor
- Diameter sembolü (Ø) → "O" olarak normalize ediliyor

### 💾 Depo Kodu

- `depoKodu` opsiyonel
- Boş bırakılırsa Luca varsayılan depoyu kullanır
- Örnek: "01", "02", "MERKEZ"

---

## 7️⃣ ÖRNEK CURL KOMUTU

```bash
curl -X POST http://localhost:5055/api/sync/to-luca/sales-invoice \
  -H "Content-Type: application/json; charset=utf-8" \
  -d '{
    "belgeSeri": "A",
    "belgeTarihi": "07/10/2025",
    "belgeTurDetayId": "76",
    "cariKodu": "MUSTERI-001",
    "cariTanim": "Test Musteri",
    "vergiNo": "1234567890",
    "detayList": [
      {
        "kartKodu": "0200B501-0003",
        "kartAdi": "KAYNAKLI BORU",
        "birimFiyat": 150.0,
        "miktar": 2,
        "kdvOran": 0.20
      }
    ]
  }'
```

---

## 8️⃣ HATA ÇÖZÜMLEME

### Hata: "code=1001 message=Luca session süresi dolmuş"

**Çözüm**: Sistem otomatik olarak session refresh yapar. Eğer devam ederse:

1. Luca'ya manuel login yapın
2. Session ID'yi kontrol edin
3. Backend'i restart edin

### Hata: "kartKodu not found in Luca"

**Çözüm**:

1. Ürün kodunun Luca'da mevcut olduğunu kontrol edin
2. Ürün kodunun yazımını kontrol edin (büyük/küçük harf)
3. Ürün kodunun aktif olduğunu kontrol edin

### Hata: "cariKodu not found in Luca"

**Çözüm**:

1. Müşteri kodunun Luca'da mevcut olduğunu kontrol edin
2. Müşteri kodunun yazımını kontrol edin
3. Müşteri kodunun aktif olduğunu kontrol edin

### Hata: "belgeTurDetayId invalid"

**Çözüm**:

1. belgeTurDetayId'nin string olduğunu kontrol edin (int değil)
2. Geçerli bir belgeTurDetayId kullanın (76, 69, vb.)

---

## 9️⃣ LUCA API RESPONSE PARSING KODU

```csharp
private static (bool IsSuccess, string? Message) ParseKozaOperationResponse(string? responseBody)
{
    if (string.IsNullOrWhiteSpace(responseBody))
    {
        return (false, "Empty response from Luca");
    }

    try
    {
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("code", out var codeElement))
        {
            var code = codeElement.GetInt32();

            if (code == 0)
            {
                return (true, null);  // Başarılı
            }

            var message = root.TryGetProperty("message", out var messageElement)
                ? messageElement.GetString()
                : "Unknown error";

            return (false, $"code={code} message={message}");
        }
    }
    catch (JsonException)
    {
        // JSON parse hatası
    }

    // Fallback: "Başarı" kelimesi içeriyorsa başarılı say
    return responseBody.Contains("Başar", StringComparison.OrdinalIgnoreCase)
        ? (true, null)
        : (false, responseBody);
}
```

---

## 🔟 ÖZET TABLO

| Öğe                 | Değer                                |
| ------------------- | ------------------------------------ |
| **Endpoint**        | POST /api/sync/to-luca/sales-invoice |
| **Content-Type**    | application/json; charset=utf-8      |
| **Request DTO**     | LucaCreateInvoiceHeaderRequest       |
| **Response DTO**    | SyncResultDto                        |
| **Tarih Formatı**   | dd/MM/yyyy (string)                  |
| **Başarı Kodu**     | code=0                               |
| **Hata Kodu**       | code!=0                              |
| **Max Retry**       | 2 deneme                             |
| **Session Timeout** | code=1001                            |
