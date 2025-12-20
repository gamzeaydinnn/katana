# Lucaya Fatura Gönderimi - Quick Reference Card

## 🚀 Hızlı Başlangıç

### Minimal Request

```json
{
  "belgeSeri": "A",
  "belgeTarihi": "07/10/2025",
  "belgeTurDetayId": "76",
  "cariKodu": "MUSTERI-001",
  "cariTanim": "Müşteri Adı",
  "vergiNo": "1234567890",
  "detayList": [
    {
      "kartKodu": "URUN-001",
      "kartAdi": "Ürün Adı",
      "birimFiyat": 100.0,
      "miktar": 1,
      "kdvOran": 0.2
    }
  ]
}
```

### Başarılı Response

```json
{
  "isSuccess": true,
  "message": "Invoices sent successfully to Luca",
  "successfulRecords": 1,
  "failedRecords": 0
}
```

---

## 📋 Zorunlu Alanlar Checklist

### Header

- [ ] `belgeSeri` - Fatura serisi (örn: "A")
- [ ] `belgeTarihi` - Tarih (dd/MM/yyyy formatı)
- [ ] `belgeTurDetayId` - Belge türü (örn: "76")

### Müşteri

- [ ] `cariKodu` - Müşteri kodu
- [ ] `cariTanim` - Müşteri adı
- [ ] `vergiNo` - Vergi numarası

### Kalemler

- [ ] `detayList` - En az 1 kalem
- [ ] Her kalem için: `kartKodu`, `kartAdi`, `birimFiyat`, `miktar`, `kdvOran`

---

## 🔢 Belgetur Detay ID'leri

| Tür   | ID  | Açıklama                |
| ----- | --- | ----------------------- |
| Satış | 76  | Mal Satış Faturası      |
| Satış | 77  | Proforma Satış Faturası |
| Satış | 79  | Satış İade Faturası     |
| Alım  | 69  | Alım Faturası           |
| Alım  | 70  | Proforma Alım Faturası  |
| Alım  | 72  | Alım İade Faturası      |

---

## ⚠️ Sık Yapılan Hatalar

### ❌ Tarih Formatı Hatası

```
YANLIŞ: "2025-10-07"
DOĞRU:  "07/10/2025"
```

### ❌ belgeTurDetayId Tipi Hatası

```
YANLIŞ: 76 (number)
DOĞRU:  "76" (string)
```

### ❌ KDV Oranı Hatası

```
YANLIŞ: 20 (yüzde)
DOĞRU:  0.20 (ondalık)
```

### ❌ Boş Müşteri Kodu

```
YANLIŞ: "" (boş string)
DOĞRU:  "MUSTERI-001"
```

### ❌ Null Ürün Kodu

```
YANLIŞ: null
DOĞRU:  "URUN-001"
```

---

## 🔍 Hata Kodları

| Kod  | Anlamı           | Çözüm                            |
| ---- | ---------------- | -------------------------------- |
| 0    | ✅ Başarılı      | -                                |
| 1001 | Session timeout  | Sistem otomatik refresh yapar    |
| 1002 | Unauthorized     | Kullanıcı adı/şifre kontrol edin |
| 1004 | Record not found | Ürün/müşteri kodu kontrol edin   |
| 1005 | Duplicate        | Kayıt zaten mevcut               |
| 1006 | Invalid field    | Alan değeri kontrol edin         |
| 1007 | Missing field    | Zorunlu alan eksik               |

---

## 📊 KDV Oranları

| Oran | Değer | Örnek           |
| ---- | ----- | --------------- |
| %1   | 0.01  | Temel gıda      |
| %8   | 0.08  | Bazı gıdalar    |
| %18  | 0.18  | Standart        |
| %20  | 0.20  | Standart (eski) |

---

## 🔗 API Endpoint

```
POST /api/sync/to-luca/sales-invoice
Content-Type: application/json; charset=utf-8
```

---

## 💡 İpuçları

### Tarih Oluşturma

```csharp
// C#
DateTime.Now.ToString("dd/MM/yyyy")

// PowerShell
(Get-Date).ToString("dd/MM/yyyy")

// JavaScript
new Date().toLocaleDateString('tr-TR')
```

### Müşteri Kodu Oluşturma

```
Format: CUST_{VergiNo}
Örnek: CUST_1234567890

veya

Format: CUST_{ID}
Örnek: CUST_000001
```

### Ürün Kodu Oluşturma

```
Format: SKU_{Kod}
Örnek: SKU_0200B501-0003

veya

Format: {Kod}
Örnek: 0200B501-0003
```

---

## 🧪 Test Komutu (PowerShell)

```powershell
$invoice = @{
    belgeSeri = "A"
    belgeTarihi = (Get-Date).ToString("dd/MM/yyyy")
    belgeTurDetayId = "76"
    cariKodu = "TEST-001"
    cariTanim = "Test Müşteri"
    vergiNo = "1234567890"
    detayList = @(
        @{
            kartKodu = "TEST-URUN"
            kartAdi = "Test Ürün"
            birimFiyat = 100.0
            miktar = 1
            kdvOran = 0.20
        }
    )
}

$json = $invoice | ConvertTo-Json -Depth 10

Invoke-RestMethod `
    -Uri "http://localhost:5055/api/sync/to-luca/sales-invoice" `
    -Method POST `
    -Body $json `
    -ContentType "application/json; charset=utf-8" | ConvertTo-Json
```

---

## 🧪 Test Komutu (cURL)

```bash
curl -X POST http://localhost:5055/api/sync/to-luca/sales-invoice \
  -H "Content-Type: application/json; charset=utf-8" \
  -d '{
    "belgeSeri": "A",
    "belgeTarihi": "07/10/2025",
    "belgeTurDetayId": "76",
    "cariKodu": "TEST-001",
    "cariTanim": "Test Müşteri",
    "vergiNo": "1234567890",
    "detayList": [
      {
        "kartKodu": "TEST-URUN",
        "kartAdi": "Test Ürün",
        "birimFiyat": 100.0,
        "miktar": 1,
        "kdvOran": 0.20
      }
    ]
  }'
```

---

## 📱 Response Alanları

| Alan                | Anlamı             |
| ------------------- | ------------------ |
| `isSuccess`         | İşlem başarılı mı? |
| `message`           | Özet mesaj         |
| `successfulRecords` | Başarılı sayısı    |
| `failedRecords`     | Başarısız sayısı   |
| `errors`            | Hata listesi       |
| `duration`          | İşlem süresi       |

---

## 🔐 Encoding

```
Content-Type: application/json; charset=utf-8
```

Türkçe karakterler destekleniyor:

- Ü, Ö, Ş, Ç, Ğ, İ ✅
- Ø (Diameter) → "O" olarak normalize edilir

---

## 📞 Destek

### Sık Sorulan Sorular

**S: Fatura numarası otomatik atanır mı?**
A: Evet, `belgeNo` null bırakılırsa Luca otomatik atar.

**S: Depo kodu zorunlu mu?**
A: Hayır, opsiyonel. Boş bırakılırsa varsayılan depo kullanılır.

**S: Kaç kalem gönderebilirim?**
A: Sınır yok, ancak performans için 100+ kalem önerilmez.

**S: Session timeout olursa ne olur?**
A: Sistem otomatik olarak session refresh yapar ve tekrar dener.

**S: Aynı faturayı iki kez gönderebilirim?**
A: Evet, ancak Luca'da yinelenen kayıt hatası verebilir.

---

## 🎯 Başarı Kriterleri

✅ Response `isSuccess: true`
✅ `failedRecords: 0`
✅ `errors: []` (boş)
✅ `duration` makul (< 10 saniye)

---

## 📚 Detaylı Belgeler

- `LUCA_FATURA_GONDERIM_ANALIZI.md` - Tam analiz
- `LUCA_FATURA_TEKNIK_REFERANS.md` - Teknik referans
- `test-sales-invoice.ps1` - Test scripti
