# 🧾 Luca Fatura API Entegrasyonu

## 📋 Genel Bakış

Bu entegrasyon, Luca ERP sisteminin **Fatura Modülü**nü Katana uygulamasına entegre eder. Artık Luca API üzerinden fatura oluşturabilir, listeleyebilir, kapatabilir (ödeme), silebilir ve gönderebilirsiniz (E-Fatura/E-Arşiv).

## 🔧 Yapılan Değişiklikler

### 1. **DTOs Eklendi** (`LucaDtos.cs`)

Fatura işlemleri için gerekli DTO'lar eklendi:

- `LucaInvoicePdfLinkResponse` - PDF link yanıtı
- `LucaCreateInvoiceResponse` - Fatura oluşturma yanıtı
- `LucaCloseInvoiceResponse` - Fatura kapama yanıtı
- `LucaDeleteInvoiceResponse` - Fatura silme yanıtı
- `LucaSendInvoiceRequest` - Fatura gönderme isteği
- `LucaSendInvoiceResponse` - Fatura gönderme yanıtı
- `LucaCurrencyReport` - Döviz raporu yardımcı DTO

### 2. **Service Metodları Eklendi** (`LucaService.Queries.cs`)

Luca API'ye istek gönderen metodlar zaten mevcuttu, sadece `SendInvoiceAsync` metodu eklendi:

- ✅ `GetInvoicePdfLinkAsync` - PDF linki al
- ✅ `ListInvoicesAsync` - Fatura listele
- ✅ `ListCurrencyInvoicesAsync` - Dövizli fatura listele
- ✅ `CreateInvoiceRawAsync` - Fatura oluştur
- ✅ `CloseInvoiceAsync` - Fatura kapat/ödeme
- ✅ `DeleteInvoiceAsync` - Fatura sil
- ✅ `SendInvoiceAsync` - Fatura gönder (yeni eklendi)

### 3. **API Controller Oluşturuldu** (`LucaInvoicesController.cs`)

Tüm fatura işlemlerini expose eden REST API controller:

```
POST   /api/luca-invoices/pdf-link      - Fatura PDF linki al
POST   /api/luca-invoices/list          - Fatura listesi
POST   /api/luca-invoices/list-currency - Dövizli fatura listesi
POST   /api/luca-invoices/create        - Yeni fatura oluştur
POST   /api/luca-invoices/close         - Fatura kapat/ödeme
DELETE /api/luca-invoices/{invoiceId}   - Fatura sil
POST   /api/luca-invoices/send          - Fatura gönder (E-Fatura/E-Arşiv)
GET    /api/luca-invoices/session-status - Session durumu kontrol
POST   /api/luca-invoices/refresh-session - Session yenile
```

### 4. **HTML Response Sorunu Çözüldü**

Luca API'den HTML yanıtı alma sorunu için çözümler eklendi:

- `ForceSessionRefreshAsync` metodu kullanımı
- `ValidateSessionAsync` ile session kontrolü
- Otomatik retry mekanizması
- Session warmup işlemi

## 🚀 Kullanım Örnekleri

### 1. Fatura Oluşturma

```http
POST /api/luca-invoices/create
Content-Type: application/json

{
  "belgeSeri": "A",
  "belgeTarihi": "07/10/2025",
  "duzenlemeSaati": "11:09",
  "vadeTarihi": "07/10/2025",
  "belgeAciklama": "SP-EFatura-No:345375",
  "belgeTurDetayId": "76",
  "faturaTur": "1",
  "paraBirimKod": "USD",
  "kdvFlag": true,
  "musteriTedarikci": "1",
  "kurBedeli": 48.6592,
  "detayList": [
    {
      "kartTuru": 1,
      "kartKodu": "00003",
      "birimFiyat": 32.802,
      "miktar": 4,
      "tutar": 500.00,
      "kdvOran": 0.1,
      "depoKodu": "000.003.001"
    }
  ],
  "cariKodu": "00000017",
  "cariTip": 1,
  "cariTanim": "VOLKAN ÜNAL",
  "cariKisaAd": "VOLKAN ÜNAL",
  "cariYasalUnvan": "VOLKAN ÜNAL",
  "vergiNo": "12",
  "il": "ANKARA",
  "ilce": "ÇANKAYA",
  "odemeTipi": "KREDIKARTI_BANKAKARTI",
  "gonderimTipi": "ELEKTRONIK",
  "efaturaTuru": 1
}
```

### 2. Fatura Listesi

```http
POST /api/luca-invoices/list
Content-Type: application/json

{
  "parUstHareketTuru": "16"
}
```

### 3. Fatura PDF Linki

```http
POST /api/luca-invoices/pdf-link
Content-Type: application/json

{
  "ssFaturaBaslikId": 122042
}
```

### 4. Fatura Kapama/Ödeme

```http
POST /api/luca-invoices/close
Content-Type: application/json

{
  "belgeTurDetayId": 127,
  "faturaId": 129937,
  "belgeSeri": "A",
  "belgeTarih": "05/05/2025",
  "vadeTarih": "05/05/2025",
  "tutar": 120,
  "cariKod": "004"
}
```

### 5. Fatura Silme

```http
DELETE /api/luca-invoices/111193
```

### 6. Dövizli Fatura Listesi

```http
POST /api/luca-invoices/list-currency
Content-Type: application/json

{
  "ftrSsFaturaBaslik": {},
  "gnlParaBirimRapor": {
    "paraBirimId": 4
  },
  "parUstHareketTuru": "16"
}
```

### 7. Fatura Gönder (E-Fatura/E-Arşiv)

```http
POST /api/luca-invoices/send
Content-Type: application/json

{
  "ssFaturaBaslikId": 122042,
  "gonderimTipi": "ELEKTRONIK"
}
```

## 🔥 HTML Response Sorunu Çözümü

Eğer Luca API'den JSON yerine HTML yanıtı alıyorsanız, bu session kaybı demektir. Çözüm:

### Otomatik Çözüm

Sistem otomatik olarak şunları yapar:

1. HTML response algılar
2. `ForceSessionRefreshAsync` ile session'ı yeniler
3. İsteği otomatik olarak tekrar gönderir

### Manuel Çözüm

Eğer sorun devam ederse:

```http
POST /api/luca-invoices/refresh-session
```

Bu endpoint session'ı manuel olarak yeniler.

### Session Durumu Kontrol

```http
GET /api/luca-invoices/session-status
```

Mevcut session durumunu kontrol eder.

## 📝 Önemli Notlar

### 1. Authentication

- Luca API, cookie-based authentication kullanır
- `appsettings.json`'da `ManualSessionCookie` ayarlanabilir
- Veya otomatik login mekanizması kullanılır

### 2. Session Yönetimi

- Session 20 dakika geçerliliği vardır
- Otomatik refresh mekanizması mevcuttur (25 dakikada bir)
- HTML response = session kaybı

### 3. Endpoint Mapping

`appsettings.json` > `LucaApi` > `Endpoints`:

```json
{
  "Invoices": "EkleFtrWsFaturaBaslik.do",
  "InvoiceList": "ListeleFtrSsFaturaBaslik.do",
  "InvoiceClose": "EkleFtrWsFaturaKapama.do",
  "InvoiceDelete": "SilFtrWsFaturaBaslik.do"
}
```

### 4. Belge Tür Detay ID'leri

`appsettings.json` > `LucaApi` > `DefaultBelgeTurDetayId`:

```json
{
  "MalSatisFaturasi": 76,
  "AlimFaturasi": 69,
  "TahsilatMakbuzu": 49,
  "TediyeMakbuzu": 63,
  "KrediKartiGirisi": 127
}
```

## 🐛 Sorun Giderme

### HTML Response Alıyorum

**Sebep:** Session kaybı  
**Çözüm:** `POST /api/luca-invoices/refresh-session`

### Fatura Oluşturamıyorum

**Kontrol Listesi:**

1. ✅ `cariKodu` doğru mu?
2. ✅ `detayList` dolu mu?
3. ✅ `belgeSeri` ayarlandı mı?
4. ✅ `belgeTarih` formatı: "dd/MM/yyyy"
5. ✅ Session geçerli mi?

### 401 Unauthorized

**Sebep:** Authentication başarısız  
**Çözüm:**

1. `appsettings.json` > `LucaApi` > `Username` ve `Password` kontrol et
2. `ManualSessionCookie` geçerli mi?
3. `ForceSessionRefreshAsync` çağır

### 500 Internal Server Error

**Sebep:** Luca API hatası  
**Çözüm:**

1. Log dosyalarını kontrol et
2. İstek body'sini doğrula
3. Luca API dökümantasyonunu kontrol et

## 📚 Luca API Dökümantasyonu

Detaylı API dökümantasyonu için Luca destek ekibine başvurun veya Postman koleksiyonunu kullanın: `Luca Koza.postman_collection.json`

## 🎯 Gelecek İyileştirmeler

- [ ] Bulk invoice creation (toplu fatura oluşturma)
- [ ] Invoice template support (şablon desteği)
- [ ] Advanced filtering (gelişmiş filtreleme)
- [ ] Invoice approval workflow (onay akışı)
- [ ] Auto-retry with exponential backoff (otomatik retry)

## 📞 Destek

Sorunlar için:

1. Log dosyalarını kontrol edin (`logs/`)
2. GitHub Issues'da sorun açın
3. Luca destek ekibine başvurun

---

**Son Güncelleme:** 12 Aralık 2025  
**Versiyon:** 1.0.0  
**Geliştirici:** Katana Integration Team
