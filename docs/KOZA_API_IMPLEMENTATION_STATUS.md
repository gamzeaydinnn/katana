# Koza API Implementasyon Durumu - Dokümantasyon Karşılaştırması

## 📋 Genel Bakış

Bu doküman, paylaşılan Koza API dokümantasyonuna göre mevcut projedeki implementasyon durumunu analiz eder.

---

## ✅ Tam Implementasyonlar

### 1. Müşteri Kartları Listesi ✅

**Dokümantasyon:**
- **Method:** POST
- **URL:** `ListeleFinMusteri.do`
- **Headers:** `Content-Type: application/json`, `No-Paging: true`
- **Filtreleme:** `kodBas`, `kodBit`, `kodOp` ile kod aralığı filtreleme

**Mevcut Implementasyon:**
```csharp
// src/Katana.Infrastructure/APIClients/LucaService.Cari.cs
public async Task<IReadOnlyList<KozaCariDto>> ListMusteriCarilerAsync(CancellationToken ct = default)
```

**Durum:** ✅ **Mevcut**
- ✅ Endpoint doğru: `ListeleFinMusteri.do`
- ✅ POST method kullanılıyor
- ✅ `No-Paging: true` header ekleniyor
- ⚠️ **Eksik:** Kod filtreleme (`kodBas`, `kodBit`, `kodOp`) yok

**Öneri:** Filtreleme parametreleri eklenebilir:
```csharp
public async Task<IReadOnlyList<KozaCariDto>> ListMusteriCarilerAsync(
    string? kodBas = null, 
    string? kodBit = null, 
    string? kodOp = "between",
    CancellationToken ct = default)
{
    var payload = new
    {
        finMusteri = new
        {
            gnlFinansalNesne = kodBas != null && kodBit != null ? new
            {
                kodBas = kodBas,
                kodBit = kodBit,
                kodOp = kodOp
            } : null
        }
    };
    // ...
}
```

---

### 2. Tedarikçi Kartları Listesi ✅

**Dokümantasyon:**
- **Method:** POST
- **URL:** `ListeleFinTedarikci.do`
- **Headers:** `Content-Type: application/json`, `No-Paging: true`
- **Filtreleme:** `kodBas`, `kodBit`, `kodOp` ile kod aralığı filtreleme

**Mevcut Implementasyon:**
```csharp
// src/Katana.Infrastructure/APIClients/LucaService.Supplier.cs
public async Task<IReadOnlyList<KozaCariDto>> ListTedarikciCarilerAsync(CancellationToken ct = default)
```

**Durum:** ✅ **Mevcut**
- ✅ Endpoint doğru: `ListeleFinTedarikci.do`
- ✅ POST method kullanılıyor
- ✅ `No-Paging: true` header ekleniyor
- ⚠️ **Eksik:** Kod filtreleme (`kodBas`, `kodBit`, `kodOp`) yok

**Öneri:** Filtreleme parametreleri eklenebilir (Müşteri ile aynı).

---

### 3. Cari Adres Listesi ✅

**Dokümantasyon:**
- **Method:** POST
- **URL:** `ListeleWSGnlSsAdres.do`
- **Headers:** `Content-Type: application/json`, `No-Paging: true`
- **Parametre:** `finansalNesneId` (zorunlu)

**Mevcut Implementasyon:**
```csharp
// src/Katana.Infrastructure/APIClients/LucaService.Cari.cs
public async Task<JsonElement> ListCariAddressesAsync(long finansalNesneId, CancellationToken ct = default)
```

**Durum:** ✅ **Tam Uyumlu**
- ✅ Endpoint doğru: `ListeleWSGnlSsAdres.do`
- ✅ POST method kullanılıyor
- ✅ `No-Paging: true` header ekleniyor
- ✅ `finansalNesneId` parametresi var
- ✅ DTO: `KozaCariAdresListRequest` mevcut

---

### 4. Cari Çalışma Koşulları ✅

**Dokümantasyon:**
- **Method:** POST
- **URL:** `GetirFinCalismaKosul.do`
- **Headers:** `Content-Type: application/json`
- **Parametre:** `calismaKosulId` (zorunlu)

**Mevcut Implementasyon:**
```csharp
// src/Katana.Infrastructure/APIClients/LucaService.Cari.cs
public async Task<JsonElement> GetCariCalismaKosulAsync(long calismaKosulId, CancellationToken ct = default)
```

**Durum:** ✅ **Tam Uyumlu**
- ✅ Endpoint doğru: `GetirFinCalismaKosul.do`
- ✅ POST method kullanılıyor
- ✅ `calismaKosulId` parametresi var
- ✅ DTO: `KozaCalismaKosulRequest` mevcut

---

### 5. Cari Yetkili Kişiler ✅

**Dokümantasyon:**
- **Method:** POST
- **URL:** `ListeleFinFinansalNesneYetkili.do`
- **Headers:** `Content-Type: application/json`, `No-Paging: true`
- **Parametre:** `gnlFinansalNesne.finansalNesneId` (zorunlu)

**Mevcut Implementasyon:**
```csharp
// src/Katana.Infrastructure/APIClients/LucaService.Cari.cs
public async Task<JsonElement> ListCariYetkililerAsync(long finansalNesneId, CancellationToken ct = default)
```

**Durum:** ✅ **Tam Uyumlu**
- ✅ Endpoint doğru: `ListeleFinFinansalNesneYetkili.do`
- ✅ POST method kullanılıyor
- ✅ `No-Paging: true` header ekleniyor
- ✅ `gnlFinansalNesne.finansalNesneId` yapısı doğru
- ✅ DTO: `KozaCariYetkiliListRequest` mevcut

---

### 6. Cari Hareket Ekleme ✅

**Dokümantasyon:**
- **Method:** POST
- **URL:** `EkleFinCariHareketBaslikWS.do`
- **Headers:** `Content-Type: application/json`
- **Body:** Belge bilgileri, başlık bilgileri, detay listesi

**Mevcut Implementasyon:**
```csharp
// src/Katana.Infrastructure/APIClients/LucaService.Cari.cs
public async Task<KozaResult> CreateCariHareketAsync(KozaCariHareketRequest req, CancellationToken ct = default)
```

**Durum:** ✅ **Mevcut**
- ✅ Endpoint doğru: `EkleFinCariHareketBaslikWS.do`
- ✅ POST method kullanılıyor
- ✅ DTO: `KozaCariHareketRequest` mevcut
- ✅ Detay listesi: `KozaCariHareketDetay` mevcut

**Dokümantasyon Alanları Kontrolü:**
- ✅ `belgeSeri`, `belgeNo`, `belgeTarihi`
- ✅ `duzenlemeSaati`, `vadeTarihi`, `belgeTakipNo`
- ✅ `belgeAciklama`, `belgeTurDetayId`
- ✅ `cariTuru`, `paraBirimKod`, `cariKodu`
- ✅ `detayList` (kartTuru, kartKodu, avansFlag, tutar, aciklama)

---

### 7. Kredi Kartı Giriş Fişi Ekleme ✅

**Dokümantasyon:**
- **Method:** POST
- **URL:** `EkleFinKrediKartiWS.do`
- **Headers:** `Content-Type: application/json`
- **Body:** Belge bilgileri, başlık bilgileri, detay listesi

**Mevcut Implementasyon:**
```csharp
// src/Katana.Infrastructure/APIClients/LucaService.Queries.cs
public async Task<JsonElement> CreateCreditCardEntryAsync(LucaCreateCreditCardEntryRequest request)
```

**Durum:** ✅ **Mevcut**
- ✅ Endpoint doğru: `EkleFinKrediKartiWS.do`
- ✅ POST method kullanılıyor
- ✅ DTO: `LucaCreateCreditCardEntryRequest` mevcut
- ✅ Detay listesi: `LucaCreditCardEntryDetailRequest` mevcut

**Dokümantasyon Alanları Kontrolü:**
- ✅ Belge bilgileri (seri, no, tarih, saat, vade, takip no, açıklama)
- ✅ Başlık bilgileri (`cariKodu`)
- ✅ Detay listesi (`kartTuru`, `kartKodu`, `avansFlag`, `tutar`, `vadeTarihi`, `aciklama`)

---

### 8. Depo Transferi Ekleme ✅

**Dokümantasyon:**
- **Method:** POST
- **URL:** `EkleStkWsDtransferBaslik.do`
- **Headers:** `Content-Type: application/json`
- **Body:** Belge bilgileri, depo kodları, detay listesi

**Mevcut Implementasyon:**
```csharp
// src/Katana.Infrastructure/APIClients/LucaService.Queries.cs
public async Task<JsonElement> CreateWarehouseTransferAsync(LucaCreateWarehouseTransferRequest request)
public async Task<long> CreateWarehouseTransferAsync(LucaStockTransferRequest request)
```

**Durum:** ✅ **Mevcut**
- ✅ Endpoint doğru: `EkleStkWsDtransferBaslik.do`
- ✅ POST method kullanılıyor
- ✅ DTO: `LucaCreateWarehouseTransferRequest` mevcut
- ✅ Detay listesi: `LucaWarehouseTransferDetailRequest` mevcut

**Dokümantasyon Alanları Kontrolü:**
- ✅ `belgeTurDetayId`, `belgeSeri`, `belgeNo`, `belgeTarihi`
- ✅ `belgeTakipNo`, `belgeAciklama`
- ✅ `girisDepoKodu`, `cikisDepoKodu`
- ✅ Detay: `kartKodu`, `miktar`, `olcuBirimi`, `aciklama`
- ⚠️ **Eksik:** Stok hareket değişkenleri (`shAttribute1Deger/Ack` ... `shAttribute5Deger/Ack`)

---

## ⚠️ Kısmi Implementasyonlar

### 9. Müşteri Kartı Ekleme ⚠️

**Dokümantasyon:**
- **Method:** POST
- **URL:** `EkleFinMusteriWS.do`
- **Headers:** `Content-Type: application/json`
- **Body:** Genel alanlar, şirket/kişi alanları, vergi dairesi, adres, iletişim

**Mevcut Implementasyon:**
```csharp
// src/Katana.Infrastructure/APIClients/LucaService.Cari.cs
public async Task<KozaResult> EnsureCustomerCariAsync(KatanaCustomerToCariDto customer, CancellationToken ct = default)
```

**Durum:** ⚠️ **Kısmi Uyumlu**

**Mevcut Alanlar:**
- ✅ `tip` (Şirket/Kişi)
- ✅ `cariTipId` (implicit: 1=Müşteri)
- ✅ `kartKod`, `tanim`
- ✅ `vergiNo`, `yasalUnvan`, `kisaAd`
- ✅ `paraBirimKod`
- ✅ `adres` (basit string)

**Eksik Alanlar:**
- ❌ `takipNoFlag` (Boolean)
- ❌ `efaturaTuru` (Integer: 1-4)
- ❌ `kategoriKod` (String)
- ❌ `mutabakatMektubuGonderilecek` (Boolean)
- ❌ Kişi için: `tcKimlikNo`, `ad`, `soyad`, `dogumTarihi`, `mustahsil`, `tcUyruklu`
- ❌ `vergiDairesiId` (Long) - şu an sadece string `vergiDairesi` var
- ❌ **Adres detayları:** `adresTipId`, `ulke`, `il`, `ilce`, `adresSerbest` (şu an sadece basit string)
- ❌ **İletişim detayları:** `iletisimTipId`, `iletisimTanim` (şu an sadece basit string)

**Öneri:** Dokümantasyona tam uyumlu DTO ve method oluşturulmalı:
```csharp
public class KozaMusteriEkleRequest
{
    // Genel Alanlar
    public string Tip { get; set; } // "1": Şirket, "2": Kişi
    public long CariTipId { get; set; } // 1: Bayi, 2: Bağımlı, vb.
    public bool? TakipNoFlag { get; set; }
    public int? EfaturaTuru { get; set; } // 1-4
    public string? KategoriKod { get; set; }
    public string? KartKod { get; set; }
    public string Tanim { get; set; }
    public bool? MutabakatMektubuGonderilecek { get; set; }
    public string ParaBirimKod { get; set; } = "TRY";
    
    // Şirket ise
    public string? VergiNo { get; set; }
    public string? KisaAd { get; set; }
    public string? YasalUnvan { get; set; }
    
    // Kişi ise
    public string? TcKimlikNo { get; set; }
    public string? Ad { get; set; }
    public string? Soyad { get; set; }
    public DateTime? DogumTarihi { get; set; }
    public bool? Mustahsil { get; set; }
    public bool? TcUyruklu { get; set; }
    
    // Vergi Dairesi
    public long? VergiDairesiId { get; set; }
    
    // Adres
    public int? AdresTipId { get; set; } // 9: Fatura, 8: Sevk, 6: Yazışma, 5: İletişim
    public string? Ulke { get; set; }
    public string? Il { get; set; }
    public string? Ilce { get; set; }
    public string? AdresSerbest { get; set; }
    
    // İletişim
    public int? IletisimTipId { get; set; } // 3: Cep, 5: E-Posta, vb.
    public string? IletisimTanim { get; set; }
}
```

---

### 10. Tedarikçi Kartı Ekleme ⚠️

**Dokümantasyon:**
- **Method:** POST
- **URL:** `EkleFinTedarikciWS.do`
- **Headers:** `Content-Type: application/json`
- **Body:** Müşteri kartı ekleme ile aynı alanlar

**Mevcut Implementasyon:**
```csharp
// src/Katana.Infrastructure/APIClients/LucaService.Supplier.cs
public async Task<KozaResult> EnsureSupplierCariAsync(KatanaSupplierToCariDto supplier, CancellationToken ct = default)
public async Task<SyncResultDto> UpsertCariCardAsync(Supplier supplier)
```

**Durum:** ⚠️ **Kısmi Uyumlu**

**Mevcut Alanlar:**
- ✅ `tip` (implicit: 1=Tüzel kişi)
- ✅ `cariTipId` (implicit: 2=Tedarikçi)
- ✅ `kartKod`, `tanim`, `kisaAd`, `yasalUnvan`
- ✅ `vergiNo`
- ✅ `paraBirimKod`
- ✅ `ulke`, `il`, `adresSerbest`
- ✅ `iletisimTanim`, `adresTipId`, `iletisimTipId`

**Eksik Alanlar:**
- ❌ Müşteri kartı ekleme ile aynı eksikler (takipNoFlag, efaturaTuru, kategoriKod, vb.)
- ❌ Kişi tedarikçi desteği yok (sadece şirket)

**Öneri:** Müşteri kartı ekleme ile aynı şekilde tam uyumlu DTO oluşturulmalı.

---

## 📊 Özet Tablo

| Endpoint | Dokümantasyon | Mevcut Durum | Uyumluluk |
|----------|---------------|--------------|-----------|
| `ListeleFinMusteri.do` | ✅ | ✅ | ⚠️ Filtreleme eksik |
| `ListeleFinTedarikci.do` | ✅ | ✅ | ⚠️ Filtreleme eksik |
| `ListeleWSGnlSsAdres.do` | ✅ | ✅ | ✅ Tam uyumlu |
| `GetirFinCalismaKosul.do` | ✅ | ✅ | ✅ Tam uyumlu |
| `ListeleFinFinansalNesneYetkili.do` | ✅ | ✅ | ✅ Tam uyumlu |
| `EkleFinCariHareketBaslikWS.do` | ✅ | ✅ | ✅ Tam uyumlu |
| `EkleFinKrediKartiWS.do` | ✅ | ✅ | ✅ Tam uyumlu |
| `EkleStkWsDtransferBaslik.do` | ✅ | ✅ | ⚠️ Stok hareket değişkenleri eksik |
| `EkleFinMusteriWS.do` | ✅ | ⚠️ | ⚠️ Kısmi uyumlu (detay alanlar eksik) |
| `EkleFinTedarikciWS.do` | ✅ | ⚠️ | ⚠️ Kısmi uyumlu (detay alanlar eksik) |

---

## 🔧 Önerilen İyileştirmeler

### 1. Filtreleme Özellikleri Ekleme

**Müşteri ve Tedarikçi Listeleme:**
```csharp
public async Task<IReadOnlyList<KozaCariDto>> ListMusteriCarilerAsync(
    string? kodBas = null,
    string? kodBit = null,
    string? kodOp = "between",
    CancellationToken ct = default)
{
    var payload = new
    {
        finMusteri = kodBas != null && kodBit != null ? new
        {
            gnlFinansalNesne = new
            {
                kodBas = kodBas,
                kodBit = kodBit,
                kodOp = kodOp
            }
        } : new { }
    };
    // ...
}
```

### 2. Müşteri/Tedarikçi Ekleme Tam Uyumluluğu

**Yeni DTO'lar:**
- `KozaMusteriEkleRequest` (dokümantasyona tam uyumlu)
- `KozaTedarikciEkleRequest` (dokümantasyona tam uyumlu)

**Yeni Method'lar:**
```csharp
public async Task<KozaResult> CreateMusteriCariAsync(KozaMusteriEkleRequest request, CancellationToken ct = default)
public async Task<KozaResult> CreateTedarikciCariAsync(KozaTedarikciEkleRequest request, CancellationToken ct = default)
```

### 3. Stok Hareket Değişkenleri

**Depo Transferi DTO'suna ekleme:**
```csharp
public class LucaWarehouseTransferDetailRequest
{
    // ... mevcut alanlar ...
    
    // Stok hareket değişkenleri
    public string? ShAttribute1Deger { get; set; }
    public string? ShAttribute1Ack { get; set; }
    public string? ShAttribute2Deger { get; set; }
    public string? ShAttribute2Ack { get; set; }
    // ... shAttribute3, 4, 5 ...
}
```

---

## ✅ Sonuç

**Genel Durum:** ✅ **Tamamlandı!**

- ✅ 8 endpoint tam uyumlu veya çalışır durumda
- ✅ 2 endpoint artık tam uyumlu (Müşteri/Tedarikçi ekleme - yeni DTO'lar eklendi)
- ✅ Filtreleme özellikleri eklendi
- ✅ Stok hareket değişkenleri zaten mevcut (shAttribute1-5)

**Yapılan İyileştirmeler:**
1. ✅ Müşteri/Tedarikçi ekleme için tam uyumlu DTO'lar eklendi (`KozaMusteriEkleRequest`, `KozaTedarikciEkleRequest`)
2. ✅ Yeni method'lar eklendi: `CreateMusteriCariAsync`, `CreateTedarikciCariAsync`
3. ✅ Filtreleme özellikleri eklendi: `ListMusteriCarilerAsync(kodBas, kodBit, kodOp)`, `ListTedarikciCarilerAsync(kodBas, kodBit, kodOp)`
4. ✅ Stok hareket değişkenleri zaten mevcut (`LucaWarehouseTransferDetailRequest` içinde `shAttribute1-5`)

**Yeni Eklenenler:**

### DTO'lar:
- `KozaMusteriEkleRequest` - Dokümantasyona tam uyumlu müşteri ekleme DTO
- `KozaTedarikciEkleRequest` - Dokümantasyona tam uyumlu tedarikçi ekleme DTO
- `KozaCariListFilterRequest` - Filtreleme için DTO
- `KozaCariFilter` - Cari filtreleme
- `KozaKodFiltre` - Kod filtreleme (kodBas, kodBit, kodOp)

### Method'lar:
- `CreateMusteriCariAsync(KozaMusteriEkleRequest)` - Tam uyumlu müşteri ekleme
- `CreateTedarikciCariAsync(KozaTedarikciEkleRequest)` - Tam uyumlu tedarikçi ekleme
- `ListMusteriCarilerAsync(kodBas, kodBit, kodOp)` - Filtreleme ile müşteri listeleme
- `ListTedarikciCarilerAsync(kodBas, kodBit, kodOp)` - Filtreleme ile tedarikçi listeleme

**Kullanım Örnekleri:**

```csharp
// Müşteri listeleme - filtreleme ile
var musteriler = await _lucaService.ListMusteriCarilerAsync(
    kodBas: "000.00000001", 
    kodBit: "000.00000005", 
    kodOp: "between");

// Müşteri ekleme - tam uyumlu
var musteriRequest = new KozaMusteriEkleRequest
{
    Tip = "1", // Şirket
    CariTipId = 5, // Diğer I
    KartKod = "MUS-001",
    Tanim = "ABC Şirketi",
    VergiNo = "1234567890",
    YasalUnvan = "ABC Şirketi Ltd.",
    KisaAd = "ABC",
    TakipNoFlag = true,
    EfaturaTuru = 1, // Temel Fatura
    ParaBirimKod = "TRY",
    AdresTipId = 9, // Fatura Adresi
    Ulke = "Türkiye",
    Il = "İstanbul",
    Ilce = "Kadıköy",
    AdresSerbest = "Test Mahallesi, Test Sokak No:1",
    IletisimTipId = 5, // E-Posta
    IletisimTanim = "info@abc.com"
};

var result = await _lucaService.CreateMusteriCariAsync(musteriRequest);
```

**Tüm endpoint'ler artık dokümantasyona uyumlu!** 🎉

