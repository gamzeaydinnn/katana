# KATANA - LUCA ENTEGRASYON AKIŞ RAPORU

## 📋 İçindekiler

1. [Genel Bakış](#genel-bakış)
2. [Katana API'sinden Gelen Bilgiler](#katana-apisinden-gelen-bilgiler)
3. [Luca'ya Aktarılan Bilgiler](#lucaya-aktarılan-bilgiler)
4. [Veri Akış Diyagramı](#veri-akış-diyagramı)
5. [Senkronizasyon Süreçleri](#senkronizasyon-süreçleri)
6. [Mapping ve Dönüşüm Kuralları](#mapping-ve-dönüşüm-kuralları)

---

## 🎯 Genel Bakış

Bu sistem, **Katana MRP** (Manufacturing Resource Planning) sisteminden gelen verileri **Luca ERP** sistemine aktaran bir entegrasyon köprüsüdür.

### Mimari Yapı

```
Katana API → KatanaService → Mapper → LucaService → Luca/Koza API
     ↓            ↓             ↓          ↓              ↓
  REST API    HttpClient    DTO Map   HttpClient    REST/SOAP
```

### Temel Bileşenler

- **KatanaService**: Katana API ile iletişim
- **LucaService**: Luca/Koza API ile iletişim
- **KatanaToLucaMapper**: Veri dönüşüm katmanı
- **SyncService**: Senkronizasyon orkestratörü
- **Workers**: Arka plan senkronizasyon işleri

---

## 📥 KATANA API'SINDEN GELEN BİLGİLER

### 1. ÜRÜNLER (Products)

**Endpoint**: `/api/v1/products`
**DTO**: `KatanaProductDto`

#### Gelen Alanlar:

```csharp
// Temel Bilgiler
- Id (long): Katana ürün ID
- SKU (string): Stok kodu
- Name (string): Ürün adı
- Barcode (string): Barkod

// Fiyat Bilgileri
- Price (decimal): Temel fiyat
- SalesPrice (decimal): Satış fiyatı
- CostPrice (decimal): Maliyet fiyatı
- PurchasePrice (decimal): Alış fiyatı

// Stok Bilgileri
- InStock (decimal): Stokta olan miktar
- Available (decimal): Kullanılabilir miktar
- OnHand (decimal): Eldeki miktar
- Committed (decimal): Taahhüt edilen miktar

// Kategori ve Birim
- Category (string): Kategori adı
- CategoryId (int): Kategori ID
- Unit (string): Ölçü birimi (pcs, kg, m, etc.)

// Diğer
- IsActive (bool): Aktif mi?
- CreatedAt (DateTime): Oluşturulma tarihi
- UpdatedAt (DateTime): Güncellenme tarihi
```

**Örnek Katana API Response**:

```json
{
  "data": [
    {
      "id": 123456,
      "sku": "PIPE-001",
      "name": "COOLING WATER PIPE Ø25mm",
      "barcode": "8690123456789",
      "sales_price": "150.00",
      "cost_price": "100.00",
      "in_stock": 50,
      "available": 45,
      "unit": "pcs",
      "category": "Pipes",
      "is_active": true
    }
  ]
}
```

---

### 2. MÜŞTERİLER (Customers)

**Endpoint**: `/api/v1/customers`
**DTO**: `KatanaCustomerDto`

#### Gelen Alanlar:

```csharp
// Temel Bilgiler
- Id (long): Katana müşteri ID
- Name (string): Müşteri adı
- FirstName (string): Ad
- LastName (string): Soyad
- Company (string): Şirket adı

// İletişim Bilgileri
- Email (string): E-posta
- Phone (string): Telefon
- Comment (string): Notlar

// Finansal Bilgiler
- Currency (string): Para birimi (TRY, USD, EUR)
- DiscountRate (decimal): İskonto oranı
- ReferenceId (string): Referans ID

// Adres Bilgileri (Addresses koleksiyonu)
- DefaultBillingId (long): Varsayılan fatura adresi ID
- DefaultShippingId (long): Varsayılan sevkiyat adresi ID
- Addresses (List<KatanaCustomerAddressDto>):
  - Line1, Line2: Adres satırları
  - City, State, Zip: Şehir, eyalet, posta kodu
  - Country: Ülke
  - EntityType: "billing" veya "shipping"
```

**Örnek Katana API Response**:

```json
{
  "data": [
    {
      "id": 91190794,
      "name": "ABC Tekstil Ltd.",
      "email": "info@abctekstil.com",
      "phone": "+90 212 555 1234",
      "currency": "TRY",
      "addresses": [
        {
          "entity_type": "billing",
          "line_1": "Atatürk Cad. No:123",
          "city": "İstanbul",
          "country": "TR"
        }
      ]
    }
  ]
}
```

---

### 3. SATIŞ SİPARİŞLERİ (Sales Orders)

**Endpoint**: `/api/v1/sales_orders`
**DTO**: `SalesOrderDto`

#### Gelen Alanlar:

```csharp
// Sipariş Bilgileri
- Id (long): Katana sipariş ID
- OrderNo (string): Sipariş numarası (SO-123)
- CustomerId (long): Müşteri ID
- Status (string): Durum (NOT_SHIPPED, OPEN, SHIPPED, DELIVERED, CANCELLED)

// Tarih Bilgileri
- OrderCreatedDate (DateTime): Sipariş tarihi
- DeliveryDate (DateTime): Teslim tarihi
- PickedDate (DateTime): Toplama tarihi

// Finansal Bilgiler
- Currency (string): Para birimi
- Total (decimal): Toplam tutar
- TotalInBaseCurrency (decimal): Ana para biriminde toplam
- ConversionRate (decimal): Döviz kuru

// Sipariş Kalemleri (SalesOrderRows)
- SalesOrderRows (List<SalesOrderRowDto>):
  - VariantId (long): Ürün varyant ID
  - Quantity (decimal): Miktar
  - PricePerUnit (decimal): Birim fiyat
  - Total (decimal): Satır toplamı
  - TaxRateId (long): KDV oranı ID
  - LocationId (long): Depo ID

// Adres Bilgileri
- BillingAddressId (long): Fatura adresi ID
- ShippingAddressId (long): Sevkiyat adresi ID
- Addresses (List<SalesOrderAddressDto>)

// Diğer
- Source (string): Kaynak (API, Manual, Shopify, etc.)
- AdditionalInfo (string): Ek bilgiler
- CustomerRef (string): Müşteri referansı
```

**Örnek Katana API Response**:

```json
{
  "data": [
    {
      "id": 123456789,
      "order_no": "SO-001",
      "customer_id": 91190794,
      "status": "NOT_SHIPPED",
      "order_created_date": "2025-01-15T10:30:00Z",
      "currency": "TRY",
      "total": "7500.00",
      "sales_order_rows": [
        {
          "variant_id": 987654,
          "quantity": 50,
          "price_per_unit": "150.00",
          "total": "7500.00"
        }
      ]
    }
  ]
}
```

---

### 4. SATIN ALMA SİPARİŞLERİ (Purchase Orders)

**Endpoint**: `/api/v1/purchase_orders`
**DTO**: `KatanaPurchaseOrderDto`

#### Gelen Alanlar:

```csharp
// Sipariş Bilgileri
- Id (string): Katana PO ID
- PoNumber (string): PO numarası
- SupplierId (int): Tedarikçi ID
- Status (string): Durum (OPEN, RECEIVED, CANCELLED)

// Tarih Bilgileri
- OrderDate (DateTime): Sipariş tarihi
- ExpectedDeliveryDate (DateTime): Beklenen teslim tarihi
- ReceivedDate (DateTime): Teslim alınma tarihi

// Finansal Bilgiler
- Currency (string): Para birimi
- Total (decimal): Toplam tutar

// Sipariş Kalemleri
- PurchaseOrderRows (List<PurchaseOrderRowDto>):
  - VariantId (long): Ürün varyant ID
  - Quantity (decimal): Miktar
  - PricePerUnit (decimal): Birim fiyat
```

---

### 5. TEDARİKÇİLER (Suppliers)

**Endpoint**: `/api/v1/suppliers`
**DTO**: `KatanaSupplierDto`

#### Gelen Alanlar:

```csharp
// Temel Bilgiler
- Id (int): Tedarikçi ID
- Name (string): Tedarikçi adı
- Email (string): E-posta
- Phone (string): Telefon
- Currency (string): Para birimi

// Adres Bilgileri
- Addresses (List<KatanaSupplierAddressDto>):
  - Line1, Line2: Adres satırları
  - City, State, Zip: Şehir, eyalet, posta kodu
  - Country: Ülke
```

---

### 6. STOK HAREKETLERİ (Stock Adjustments)

**Endpoint**: `/api/v1/stock_adjustments`
**DTO**: `StockAdjustmentDto`

#### Gelen Alanlar:

```csharp
// Hareket Bilgileri
- Id (long): Hareket ID
- StockAdjustmentNumber (string): Hareket numarası
- StockAdjustmentDate (DateTime): Hareket tarihi
- LocationId (long): Depo ID
- Reason (string): Sebep
- AdditionalInfo (string): Ek bilgiler

// Hareket Kalemleri
- StockAdjustmentRows (List<StockAdjustmentRowDto>):
  - VariantId (long): Ürün varyant ID
  - Quantity (decimal): Miktar (+ veya -)
```

---

### 7. DEPOLAR (Locations)

**Endpoint**: `/api/v1/locations`
**DTO**: `LocationDto`

#### Gelen Alanlar:

```csharp
- Id (long): Depo ID
- Name (string): Depo adı
- IsPrimary (bool): Ana depo mu?
- IsActive (bool): Aktif mi?
```

---

## 📤 LUCA'YA AKTARILAN BİLGİLER

### 1. STOK KARTLARI (Stock Cards)

**Endpoint**: `/koza/api/stokKarti/create`
**DTO**: `LucaCreateStokKartiRequest`

#### Gönderilen Alanlar:

```csharp
// ZORUNLU ALANLAR
- KartKodu (string): Stok kodu (Katana SKU)
- KartAdi (string): Stok adı (Katana Name)
- BaslangicTarihi (string): Başlangıç tarihi (dd/MM/yyyy formatında)
- OlcumBirimiId (long): Ölçü birimi ID (Luca'dan alınır)
- KartTuru (long): Kart türü (1=Stok, 2=Hizmet)
- KartTipi (long): Kart tipi (1=Ticari Mal)

// FİYAT VE KDV
- KartAlisKdvOran (double): Alış KDV oranı (1 = %100, 0.18 = %18)
- PerakendeAlisBirimFiyat (double): Alış fiyatı
- PerakendeSatisBirimFiyat (double): Satış fiyatı

// KATEGORİ VE BARKOD
- KategoriAgacKod (string): Kategori kodu (numeric, örn: "001", "220")
- Barkod (string): Barkod

// FLAGLER (0 veya 1)
- SatilabilirFlag (int): Satılabilir mi? (1=Evet, 0=Hayır)
- SatinAlinabilirFlag (int): Satın alınabilir mi?
- LotNoFlag (int): Lot takibi var mı?
- MinStokKontrol (int): Min stok kontrolü var mı?
- MaliyetHesaplanacakFlag (bool): Maliyet hesaplansın mı?

// TEVKİFAT (Opsiyonel - null gönderilebilir)
- AlisTevkifatOran (string): Alış tevkifat oranı ("7/10" formatında)
- SatisTevkifatOran (string): Satış tevkifat oranı
- AlisTevkifatTipId (long): Alış tevkifat tip ID
- SatisTevkifatTipId (long): Satış tevkifat tip ID

// DİĞER (Opsiyonel)
- UzunAdi (string): Uzun açıklama
- DetayAciklama (string): Detaylı açıklama
- GtipKodu (string): GTIP kodu
```

**Mapping Örneği**:

```
Katana Product                    →  Luca Stok Kartı
─────────────────────────────────────────────────────────
SKU: "PIPE-001"                   →  kartKodu: "PIPE-001"
Name: "COOLING WATER PIPE"        →  kartAdi: "COOLING WATER PIPE"
SalesPrice: 150.00                →  perakendeSatisBirimFiyat: 150.0
CostPrice: 100.00                 →  perakendeAlisBirimFiyat: 100.0
Unit: "pcs"                       →  olcumBirimiId: 5 (ADET)
Category: "Pipes"                 →  kategoriAgacKod: "220" (mapping'den)
Barcode: "8690123456789"          →  barkod: "8690123456789"
IsActive: true                    →  satilabilirFlag: 1
```

**Örnek Luca API Request**:

```json
{
  "kartKodu": "PIPE-001",
  "kartAdi": "COOLING WATER PIPE",
  "baslangicTarihi": "19/12/2025",
  "olcumBirimiId": 5,
  "kartTuru": 1,
  "kartTipi": 1,
  "kartAlisKdvOran": 1,
  "perakendeAlisBirimFiyat": 100.0,
  "perakendeSatisBirimFiyat": 150.0,
  "kategoriAgacKod": "220",
  "barkod": "8690123456789",
  "satilabilirFlag": 1,
  "satinAlinabilirFlag": 1,
  "lotNoFlag": 0,
  "minStokKontrol": 0,
  "maliyetHesaplanacakFlag": true
}
```

---

### 2. CARİ KARTLAR (Customer Cards)

**Endpoint**: `/koza/api/musteri/create`
**DTO**: `LucaCreateCustomerRequest`

#### Gönderilen Alanlar:

```csharp
// ZORUNLU ALANLAR
- CariKodu (string): Cari kodu (CUST_<TaxNo> formatında)
- CariTanim (string): Cari tanımı (müşteri adı)
- CariTip (int): Cari tipi (1=Şirket, 2=Şahıs)
- VergiNo (string): Vergi numarası (10 veya 11 haneli)

// İLETİŞİM BİLGİLERİ
- Email (string): E-posta
- Telefon (string): Telefon
- CariKisaAd (string): Kısa ad
- CariYasalUnvan (string): Yasal ünvan

// ADRES BİLGİLERİ
- Il (string): İl
- Ilce (string): İlçe
- Mahallesemt (string): Mahalle/Semt
- Caddesokak (string): Cadde/Sokak
- AdresSerbest (string): Serbest adres
- PostaKodu (string): Posta kodu

// FİNANSAL BİLGİLER
- ParaBirimKod (string): Para birimi (TRY, USD, EUR)
- CariTipId (long): Cari tip ID (Luca'dan alınır)
```

**Mapping Örneği**:

```
Katana Customer                   →  Luca Cari Kartı
─────────────────────────────────────────────────────────
Id: 91190794                      →  (ReferenceId olarak saklanır)
Name: "ABC Tekstil Ltd."          →  cariTanim: "ABC Tekstil Ltd."
Email: "info@abctekstil.com"     →  email: "info@abctekstil.com"
Phone: "+90 212 555 1234"         →  telefon: "+90 212 555 1234"
Currency: "TRY"                   →  paraBirimKod: "TRY"
TaxNo: "1234567890"               →  vergiNo: "1234567890"
                                  →  cariKodu: "CUST_1234567890"
                                  →  cariTip: 1 (10 haneli = Şirket)
```

---

### 3. FATURALAR (Invoices)

**Endpoint**: `/koza/api/fatura/create`
**DTO**: `LucaCreateInvoiceHeaderRequest`

#### Gönderilen Alanlar:

```csharp
// BELGE BİLGİLERİ
- BelgeSeri (string): Belge serisi (örn: "EFA2025")
- BelgeNo (string): Belge numarası
- BelgeTarihi (string): Belge tarihi (dd/MM/yyyy)
- VadeTarihi (string): Vade tarihi (dd/MM/yyyy)
- BelgeTurDetayId (string): Belge tür detay ID
- BelgeTakipNo (string): Takip numarası
- BelgeAciklama (string): Açıklama

// FATURA TİPİ
- FaturaTur (string): Fatura türü (1=Satış, 2=Alış)
- ParaBirimKod (string): Para birimi
- KurBedeli (double): Kur bedeli
- KdvFlag (bool): KDV dahil mi?
- BabsFlag (bool): BABS var mı?

// CARİ BİLGİLERİ
- MusteriTedarikci (string): Müşteri/Tedarikçi (1=Müşteri, 2=Tedarikçi)
- CariKodu (string): Cari kodu
- CariTanim (string): Cari tanımı
- CariTip (int): Cari tipi
- CariKisaAd (string): Kısa ad
- CariYasalUnvan (string): Yasal ünvan
- CariAd (string): Ad (ZORUNLU!)
- CariSoyad (string): Soyad (ZORUNLU!)
- VergiNo (string): Vergi numarası (ZORUNLU!)
- VergiDairesi (string): Vergi dairesi

// ADRES BİLGİLERİ
- Il, Ilce, Mahallesemt, Caddesokak
- Diskapino, Ickapino, PostaKodu
- AdresSerbest, Telefon, Email

// FATURA SATIRLARI (DetayList)
- DetayList (List<LucaCreateInvoiceDetailRequest>):
  - KartTuru (int): Kart türü (1=Stok)
  - KartKodu (string): Stok kodu
  - KartAdi (string): Stok adı
  - Miktar (double): Miktar
  - BirimFiyat (double): Birim fiyat
  - KdvOran (double): KDV oranı
  - Tutar (double): Tutar
  - DepoKodu (string): Depo kodu
  - HesapKod (string): Hesap kodu
```

**Mapping Örneği**:

```
Katana Sales Order                →  Luca Fatura
─────────────────────────────────────────────────────────
OrderNo: "SO-001"                 →  belgeTakipNo: "SO-001"
OrderCreatedDate: 2025-01-15      →  belgeTarihi: "15/01/2025"
CustomerId: 91190794              →  cariKodu: "CUST_1234567890"
Total: 7500.00                    →  (satırlardan hesaplanır)
Currency: "TRY"                   →  paraBirimKod: "TRY"

SalesOrderRows[0]:
  VariantId: 987654               →  kartKodu: "PIPE-001" (mapping'den)
  Quantity: 50                    →  miktar: 50.0
  PricePerUnit: 150.00            →  birimFiyat: 150.0
  Total: 7500.00                  →  tutar: 7500.0
```

---

## 🔄 VERİ AKIŞ DİYAGRAMI

### Genel Akış

```
┌─────────────────┐
│  KATANA API     │
│  (REST JSON)    │
└────────┬────────┘
         │
         │ HTTP GET
         ↓
┌─────────────────┐
│ KatanaService   │
│ - GetProducts   │
│ - GetCustomers  │
│ - GetOrders     │
└────────┬────────┘
         │
         │ DTO Mapping
         ↓
┌─────────────────┐
│ KatanaToLuca    │
│ Mapper          │
│ - MapProduct    │
│ - MapCustomer   │
│ - MapInvoice    │
└────────┬────────┘
         │
         │ Transformed DTO
         ↓
┌─────────────────┐
│  LucaService    │
│ - SendStockCard │
│ - SendCustomer  │
│ - SendInvoice   │
└────────┬────────┘
         │
         │ HTTP POST
         ↓
┌─────────────────┐
│  LUCA/KOZA API  │
│  (REST/SOAP)    │
└─────────────────┘
```

### Detaylı Ürün Senkronizasyon Akışı

```
1. KATANA'DAN ÇEKME
   ├─ KatanaService.GetProductsAsync()
   ├─ Pagination (100 ürün/sayfa)
   ├─ Rate limiting (50ms delay)
   └─ Response: List<KatanaProductDto>

2. MAPPING
   ├─ KatanaToLucaMapper.MapKatanaProductToStockCard()
   ├─ SKU normalizasyonu
   ├─ Kategori mapping (PRODUCT_CATEGORY tablosu)
   ├─ Ölçü birimi mapping (UnitMapping)
   ├─ Encoding dönüşümü (UTF-8 → ISO-8859-9)
   └─ Output: LucaCreateStokKartiRequest

3. VALIDASYON
   ├─ KatanaToLucaMapper.ValidateLucaStockCard()
   ├─ Zorunlu alan kontrolü
   ├─ Format kontrolü
   └─ Hata varsa: ValidationException

4. LUCA'YA GÖNDERME
   ├─ LucaService.SendStockCardsAsync()
   ├─ Batch processing (100 ürün/batch)
   ├─ Duplicate kontrolü (Luca tarafında)
   ├─ Retry policy (3 deneme)
   └─ Response: SyncResultDto

5. SONUÇ KAYDI
   ├─ SyncOperationLog tablosuna kayıt
   ├─ Başarılı/Başarısız sayıları
   └─ Hata mesajları
```

---

## ⚙️ SENKRONIZASYON SÜREÇLERİ

### 1. Otomatik Senkronizasyon (Background Workers)

#### KatanaSalesOrderSyncWorker

**Çalışma Sıklığı**: Her 5 dakikada bir
**Görev**: Katana'dan açık siparişleri çeker ve pending adjustments oluşturur

**Akış**:

```
1. Katana'dan OPEN siparişleri çek (status=NOT_SHIPPED)
2. Her sipariş için:
   a. Müşteri mapping kontrolü
   b. Müşteri yoksa Katana'dan çek ve oluştur
   c. SalesOrder entity oluştur
   d. SalesOrderLine'ları oluştur
   e. Database'e kaydet
3. PendingStockAdjustment oluştur (admin onayı için)
4. Luca'ya ürün senkronizasyonu tetikle
5. Onaylanan siparişleri Luca'ya fatura olarak gönder
```

**Duplicate Prevention**:

- Sipariş: `KatanaOrderId` ile kontrol
- Sipariş kalemi: `ExternalOrderId|SKU|Quantity` composite key

---

### 2. Manuel Senkronizasyon (API Endpoints)

#### POST /api/sync/products-to-luca

**Görev**: Ürünleri Luca'ya stok kartı olarak gönderir

**Parametreler**:

```csharp
{
  "dryRun": false,              // true ise sadece simülasyon
  "forceSendDuplicates": false, // true ise duplicate kontrolü atlanır
  "preferBarcodeMatch": true,   // Barkod ile eşleştirme öncelikli
  "limit": null                 // Kaç ürün gönderilecek (null=hepsi)
}
```

**Akış**:

```
1. Katana'dan tüm ürünleri çek
2. Luca'dan mevcut stok kartlarını çek (ATLANMIŞ - performans için)
3. Değişiklik tespiti (ATLANMIŞ - Luca duplicate kontrolüne güveniliyor)
4. Mapping ve validasyon
5. Luca'ya batch gönderim
6. Sonuç raporu
```

---

#### POST /api/sync/customers

**Görev**: Müşterileri Luca'ya cari kart olarak gönderir

---

#### POST /api/sync/invoices

**Görev**: Faturaları Luca'ya gönderir

---

### 3. Sipariş Onay Akışı

```
1. Katana'dan sipariş gelir (KatanaSalesOrderSyncWorker)
   ↓
2. PendingStockAdjustment oluşturulur (Status=Pending)
   ↓
3. Admin UI'da görünür (frontend/src/components/Admin/PurchaseOrders.tsx)
   ↓
4. Admin onaylar (Status=Approved)
   ↓
5. OrderInvoiceSyncService tetiklenir
   ↓
6. Katana'da stok artırılır (SyncProductStockAsync)
   ↓
7. Luca'ya fatura gönderilir (SendInvoiceAsync)
   ↓
8. SalesOrder.IsSyncedToLuca = true
```

---

## 🗺️ MAPPING VE DÖNÜŞÜM KURALLARI

### 1. Kategori Mapping

**Tablo**: `Mappings` (MappingType='PRODUCT_CATEGORY')

**Örnek**:

```
SourceValue (Katana)  →  TargetValue (Luca)
─────────────────────────────────────────────
"Pipes"               →  "220"
"Valves"              →  "221"
"Fittings"            →  "222"
"3YARI MAMUL"         →  "001"
```

**Kod**:

```csharp
var categoryMappings = await GetMappingDictionaryAsync("PRODUCT_CATEGORY");
var lucaCategory = categoryMappings.TryGetValue(katanaCategory, out var mapped)
    ? mapped
    : _lucaSettings.DefaultKategoriKodu;
```

---

### 2. Ölçü Birimi Mapping

**Kaynak**: `appsettings.json` → `LucaApiSettings.UnitMapping`

**Örnek**:

```json
{
  "UnitMapping": {
    "pcs": 5,
    "kg": 1,
    "m": 2,
    "l": 3,
    "adet": 5,
    "kilogram": 1
  }
}
```

**Kod**:

```csharp
var olcumBirimiId = _lucaSettings.UnitMapping.TryGetValue(
    katanaProduct.Unit.ToLowerInvariant(),
    out var mappedId
) ? mappedId : _lucaSettings.DefaultOlcumBirimiId;
```

---

### 3. Müşteri Tipi Mapping

**Tablo**: `Mappings` (MappingType='CUSTOMER_TYPE')

**Kural**:

- Vergi No 10 haneli → CariTip = 1 (Şirket)
- Vergi No 11 haneli → CariTip = 2 (Şahıs)

---

### 4. SKU Normalizasyonu

**Sorun**: Katana bazen Name alanına SKU değerini gönderiyor
**Çözüm**: Database'den ürün ismini çek

```csharp
// 🔥 KRİTİK FİX: Name boş veya SKU ile aynıysa database'den çek
var needsNameFix = string.IsNullOrWhiteSpace(product.Name) ||
                   string.Equals(product.Name, product.SKU, StringComparison.OrdinalIgnoreCase);

if (needsNameFix && productNameLookup.TryGetValue(product.SKU, out var dbName))
{
    product.Name = dbName; // Database'den ürün ismini kullan!
}
```

---

### 5. Encoding Dönüşümü

**Sorun**: Luca ISO-8859-9 (Turkish) encoding kullanıyor, Katana UTF-8
**Çözüm**: Özel karakterleri normalize et

```csharp
// Ø → O dönüşümü
result = result
    .Replace("Ø", "O")   // Unicode U+00D8
    .Replace("ø", "o")   // Unicode U+00F8
    .Replace("Φ", "O")   // Greek Phi
    .Replace("φ", "o");

// Türkçe karakterler korunur (Ü, Ö, Ş, Ç, Ğ, İ)
```

---

### 6. Versiyonlu SKU Yönetimi

**Sorun**: Luca'da aynı barkod birden fazla stok kartında olamaz
**Çözüm**: Versiyonlu SKU'lar için barkod NULL gönder

```csharp
// SKU: "PIPE-V2" → Versiyonlu
bool isVersionedSku = Regex.IsMatch(sku, @"-V\d+$", RegexOptions.IgnoreCase);

if (isVersionedSku)
{
    barcodeToSend = null; // Barkod NULL gönder
}
```

---

## 📊 VERİ AKIŞ ÖZETİ

### Katana → Luca Veri Akışı Tablosu

| Katana Veri Tipi  | Katana Endpoint             | Luca Veri Tipi    | Luca Endpoint                  | Mapping Tablosu    |
| ----------------- | --------------------------- | ----------------- | ------------------------------ | ------------------ |
| Products          | `/api/v1/products`          | Stok Kartları     | `/koza/api/stokKarti/create`   | PRODUCT_CATEGORY   |
| Customers         | `/api/v1/customers`         | Cari Kartlar      | `/koza/api/musteri/create`     | CUSTOMER_TYPE      |
| Sales Orders      | `/api/v1/sales_orders`      | Faturalar         | `/koza/api/fatura/create`      | -                  |
| Purchase Orders   | `/api/v1/purchase_orders`   | Alış Faturaları   | `/koza/api/fatura/create`      | -                  |
| Stock Adjustments | `/api/v1/stock_adjustments` | Stok Hareketleri  | `/koza/api/stokHareket/create` | LOCATION_WAREHOUSE |
| Suppliers         | `/api/v1/suppliers`         | Tedarikçi Kartlar | `/koza/api/tedarikci/create`   | -                  |

---

## 🔧 ÖNEMLİ NOTLAR

### 1. Luca API Özellikleri

- **Encoding**: ISO-8859-9 (Turkish)
- **Tarih Formatı**: dd/MM/yyyy
- **Session Yönetimi**: Cookie-based (JSESSIONID)
- **Rate Limiting**: 350-1000ms arası throttling
- **Duplicate Kontrolü**: Luca tarafında yapılır (kartKodu ile)

### 2. Performans Optimizasyonları

- **Batch Processing**: 100 kayıt/batch
- **Pagination**: Katana API'den 100 kayıt/sayfa
- **Memory Management**: GC.Collect() her batch sonrası
- **Caching**: Müşteri ve ürün bilgileri cache'lenir

### 3. Hata Yönetimi

- **Retry Policy**: 3 deneme, exponential backoff
- **Validation**: Her aşamada veri doğrulama
- **Logging**: Detaylı log kayıtları
- **Fallback**: Hata durumunda varsayılan değerler

### 4. Güvenlik

- **Authentication**: Token-based (Katana), Cookie-based (Luca)
- **SSL/TLS**: HTTPS zorunlu
- **Timeout**: 60 saniye (configurable)

---

## 📝 SONUÇ

Bu entegrasyon sistemi, Katana MRP'den gelen verileri Luca ERP'ye aktararak iki sistem arasında senkronizasyon sağlar. Sistem, otomatik ve manuel senkronizasyon seçenekleri sunar, hata yönetimi ve performans optimizasyonları içerir.

**Temel Özellikler**:

- ✅ Otomatik sipariş senkronizasyonu (5 dakikada bir)
- ✅ Manuel ürün/müşteri/fatura senkronizasyonu
- ✅ Duplicate prevention
- ✅ Mapping ve dönüşüm kuralları
- ✅ Detaylı loglama ve raporlama
- ✅ Admin onay mekanizması

**Desteklenen Veri Tipleri**:

- Ürünler (Products → Stok Kartları)
- Müşteriler (Customers → Cari Kartlar)
- Siparişler (Sales Orders → Faturalar)
- Satın Alma (Purchase Orders → Alış Faturaları)
- Stok Hareketleri (Stock Adjustments)
- Tedarikçiler (Suppliers)

---

**Rapor Tarihi**: 19 Aralık 2025
**Versiyon**: 1.0
**Hazırlayan**: Kiro AI Assistant
