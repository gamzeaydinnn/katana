# Lucaya Fatura Gönderimi - Analiz Özeti

**Tarih**: 7 Ekim 2025  
**Konu**: Katana → Luca Fatura Gönderimi JSON Yapısı ve Response Analizi  
**Durum**: ✅ Tamamlandı

---

## 📌 Özet

Katana sisteminden Lucaya fatura gönderirken kullanılan JSON yapısı ve Luca'dan dönen response'lar detaylı olarak analiz edilmiştir.

---

## 🔍 Analiz Bulguları

### 1. Request JSON Yapısı

#### Gönderilen Endpoint

```
POST /api/sync/to-luca/sales-invoice
Content-Type: application/json; charset=utf-8
```

#### Request DTO

- **Sınıf**: `LucaCreateInvoiceHeaderRequest`
- **Kalem DTO**: `LucaCreateInvoiceDetailRequest`
- **Namespace**: `Katana.Core.DTOs`

#### Zorunlu Alanlar

1. **belgeSeri** (string) - Fatura serisi (örn: "A")
2. **belgeTarihi** (string) - Tarih (dd/MM/yyyy formatı)
3. **belgeTurDetayId** (string) - Belge türü ID (örn: "76")
4. **cariKodu** (string) - Müşteri kodu
5. **cariTanim** (string) - Müşteri adı
6. **vergiNo** (string) - Vergi numarası
7. **detayList** (List) - Fatura kalemleri (en az 1)

#### Kalem Zorunlu Alanları

1. **kartKodu** (string) - Ürün kodu
2. **kartAdi** (string) - Ürün adı
3. **birimFiyat** (double) - Birim fiyat
4. **miktar** (double) - Miktar
5. **kdvOran** (double) - KDV oranı (0.20 = %20)

#### Opsiyonel Alanlar

- `belgeNo` - Fatura numarası (null=otomatik)
- `vadeTarihi` - Vade tarihi
- `belgeTakipNo` - Takip numarası
- `belgeAciklama` - Açıklama (max 250 karakter)
- `adresSerbest` - Serbest adres (max 500 karakter)
- `depoKodu` - Depo kodu
- `hesapKod` - Muhasebe hesabı kodu
- `referansNo` - Referans numarası
- `siparisNo` - Sipariş numarası

### 2. Response JSON Yapısı

#### Response DTO

- **Sınıf**: `SyncResultDto`
- **Namespace**: `Katana.Core.DTOs`

#### Başarılı Response Örneği

```json
{
  "isSuccess": true,
  "message": "Invoices sent successfully to Luca",
  "processedRecords": 1,
  "successfulRecords": 1,
  "failedRecords": 0,
  "errors": [],
  "syncTime": "2025-10-07T14:30:22.1234567Z",
  "syncType": "INVOICE",
  "duration": "00:00:02.5000000"
}
```

#### Başarısız Response Örneği

```json
{
  "isSuccess": false,
  "message": "1 succeeded, 1 failed",
  "processedRecords": 2,
  "successfulRecords": 1,
  "failedRecords": 1,
  "errors": [
    "SF-20251007-143022: code=1001 message=Luca session süresi dolmuş"
  ],
  "syncTime": "2025-10-07T14:30:22.1234567Z",
  "syncType": "INVOICE",
  "duration": "00:00:05.2000000"
}
```

### 3. Luca API Response Yapısı

Katana, Luca API'den aşağıdaki JSON formatında response alıyor:

#### Başarılı

```json
{
  "code": 0,
  "message": "Başarılı"
}
```

#### Hata

```json
{
  "code": 1001,
  "message": "Luca session süresi dolmuş, lütfen tekrar giriş yapınız"
}
```

### 4. Belgetur Detay ID'leri

#### Satış Faturaları

| ID  | Tür                      |
| --- | ------------------------ |
| 76  | Mal Satış Faturası       |
| 77  | Proforma Satış Faturası  |
| 78  | Kur Farkı Satış Faturası |
| 79  | Satış İade Faturası      |

#### Alım Faturaları

| ID  | Tür                     |
| --- | ----------------------- |
| 69  | Alım Faturası           |
| 70  | Proforma Alım Faturası  |
| 71  | Kur Farkı Alış Faturası |
| 72  | Alım İade Faturası      |

### 5. Hata Kodları

| Kod  | Anlamı                 | Çözüm                            |
| ---- | ---------------------- | -------------------------------- |
| 0    | Başarılı               | -                                |
| 1001 | Session timeout        | Sistem otomatik refresh yapar    |
| 1002 | Unauthorized           | Kullanıcı adı/şifre kontrol edin |
| 1003 | Invalid request        | Request JSON'u kontrol edin      |
| 1004 | Record not found       | Ürün/müşteri kodu kontrol edin   |
| 1005 | Duplicate record       | Kayıt zaten mevcut               |
| 1006 | Invalid field value    | Alan değeri kontrol edin         |
| 1007 | Missing required field | Zorunlu alan eksik               |

---

## 🔄 Fatura Gönderme Akışı

```
1. Request Oluşturma
   ↓
2. JSON Serileştirme
   ↓
3. Luca API'ye POST
   ↓
4. Response Alma
   ↓
5. Response Parsing
   ├─ code=0 → Başarılı ✅
   └─ code!=0 → Hata ❌
   ↓
6. SyncResultDto Oluşturma
   ↓
7. Client'a Döndürme
```

### Retry Mekanizması

```
Deneme 1: İlk gönderim
  ↓
  Başarılı? → Bitir ✅
  ↓
  code=1001 (Session timeout)? → Session refresh
  ↓
Deneme 2: Tekrar gönderim
  ↓
  Başarılı? → Bitir ✅
  ↓
  Hata? → Hata döndür ❌
```

---

## ⚠️ Kritik Noktalar

### 1. Tarih Formatı

- **Beklenen**: `"dd/MM/yyyy"` (string)
- **Örnek**: `"07/10/2025"`
- **YANLIŞ**: `"2025-10-07"` veya `"10/07/2025"`

### 2. belgeTurDetayId Tipi

- **Beklenen**: String (`"76"`)
- **YANLIŞ**: Number (`76`)

### 3. KDV Oranı

- **Beklenen**: Ondalık (`0.20` = %20)
- **YANLIŞ**: Yüzde (`20`)

### 4. Encoding

- **Beklenen**: UTF-8
- **Türkçe karakterler**: Destekleniyor (Ü, Ö, Ş, Ç, Ğ, İ)
- **Diameter sembolü**: Ø → "O" olarak normalize edilir

### 5. Session Yönetimi

- Luca session timeout olabilir (code=1001)
- Katana otomatik olarak session refresh yapar
- Maksimum 2 retry denemesi yapılır

---

## 📊 Kod Kaynakları

### Request Oluşturma

- **Dosya**: `src/Katana.Business/Mappers/KatanaToLucaMapper.cs`
- **Metod**: `MapInvoiceToCreateRequest()`
- **Satırlar**: 197-272

### Gönderim

- **Dosya**: `src/Katana.Infrastructure/APIClients/LucaService.Operations.cs`
- **Metod**: `SendInvoicesAsync()`, `SendInvoicesViaKozaAsync()`
- **Satırlar**: 71-244

### Response Parsing

- **Dosya**: `src/Katana.Infrastructure/APIClients/LucaService.Operations.cs`
- **Metod**: `ParseKozaOperationResponse()`
- **Satırlar**: 327-351

### DTO Tanımları

- **Dosya**: `src/Katana.Core/DTOs/LucaDtos.cs`
- **Sınıflar**: `LucaCreateInvoiceHeaderRequest`, `LucaCreateInvoiceDetailRequest`
- **Satırlar**: 1275-1650

---

## 📈 İstatistikler

| Metrik                     | Değer                 |
| -------------------------- | --------------------- |
| Zorunlu Header Alanları    | 7                     |
| Zorunlu Kalem Alanları     | 5                     |
| Opsiyonel Alanlar          | 30+                   |
| Belgetur Detay ID'leri     | 8                     |
| Hata Kodları               | 8+                    |
| Maksimum Retry             | 2                     |
| Tarih Format Varyasyonları | 3 (doğru 1, yanlış 2) |

---

## 🎯 Başarı Kriterleri

✅ Response `isSuccess: true`  
✅ `failedRecords: 0`  
✅ `errors: []` (boş)  
✅ `duration` < 10 saniye

---

## 📚 Oluşturulan Belgeler

1. **LUCA_FATURA_GONDERIM_ANALIZI.md**

   - Tam JSON yapısı ve açıklamalar
   - Tüm alanların detaylı tablosu
   - Örnek curl komutu

2. **LUCA_FATURA_TEKNIK_REFERANS.md**

   - Teknik referans belgesi
   - Kod örnekleri (C#, PowerShell, JavaScript)
   - Validasyon kuralları

3. **LUCA_FATURA_QUICK_REFERENCE.md**

   - Hızlı referans kartı
   - Sık yapılan hatalar
   - Test komutları

4. **LUCA_FATURA_ANALIZ_OZETI.md** (bu dosya)
   - Analiz özeti
   - Bulguların özeti

---

## 💡 Öneriler

### 1. Validasyon

- Request gönderilmeden önce tüm zorunlu alanları kontrol edin
- Tarih formatını (dd/MM/yyyy) doğrulayın
- belgeTurDetayId'nin string olduğunu kontrol edin

### 2. Error Handling

- Response'daki `errors` listesini kontrol edin
- Hata kodlarına göre uygun mesaj gösterin
- Session timeout (code=1001) için otomatik retry yapın

### 3. Logging

- Gönderilen JSON'u log'a yazın (debug seviyesinde)
- Response'u log'a yazın
- Hataları detaylı olarak log'a yazın

### 4. Testing

- Minimal request ile test edin
- Tüm belgetur detay ID'lerini test edin
- Hata senaryolarını test edin

---

## 🔗 İlgili Dosyalar

```
src/
├── Katana.Core/DTOs/
│   ├── LucaDtos.cs (DTO tanımları)
│   └── SyncDtos.cs (Response DTO)
├── Katana.Business/Mappers/
│   └── KatanaToLucaMapper.cs (Request oluşturma)
└── Katana.Infrastructure/APIClients/
    └── LucaService.Operations.cs (Gönderim ve parsing)

test-sales-invoice.ps1 (Test scripti)
```

---

## ✅ Sonuç

Katana sisteminden Lucaya fatura gönderimi işlemi detaylı olarak analiz edilmiştir. Gönderilen JSON yapısı, dönen response'lar, hata kodları ve validasyon kuralları belgelenmiştir.

**Tüm belgeler hazır ve kullanıma açıktır.**

---

## 📞 Hızlı Erişim

- **Tam Analiz**: `LUCA_FATURA_GONDERIM_ANALIZI.md`
- **Teknik Referans**: `LUCA_FATURA_TEKNIK_REFERANS.md`
- **Quick Reference**: `LUCA_FATURA_QUICK_REFERENCE.md`
- **Bu Özet**: `LUCA_FATURA_ANALIZ_OZETI.md`
