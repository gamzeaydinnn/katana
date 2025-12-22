# Admin Onayı ve Kozaya Senkronizasyon - Akış Diyagramları

## 1. Genel Akış

```
┌─────────────────────────────────────────────────────────────────────┐
│                    ADMIN ONAY VE SENKRONIZASYON AKIŞI               │
└─────────────────────────────────────────────────────────────────────┘

KATANA ERP
    │
    │ (Her 5 dakika - Otomatik)
    │ KatanaSalesOrderSyncWorker
    ▼
┌──────────────────────────────────────────────────────────────────┐
│ SalesOrders Tablosu                                              │
│ ├─ OrderNo: SO-12345                                            │
│ ├─ Status: PENDING                                              │
│ ├─ Lines: [SKU-001, SKU-002, ...]                              │
│ └─ IsSyncedToLuca: false                                        │
└──────────────────────────────────────────────────────────────────┘
    │
    │ (Admin Panelinden - Manuel)
    │ POST /api/sales-orders/{id}/approve
    ▼
┌──────────────────────────────────────────────────────────────────┐
│ ADMIN ONAY İŞLEMİ                                                │
│ ├─ Sipariş Kontrolü                                             │
│ ├─ Müşteri Kontrolü                                             │
│ ├─ Her Satır İçin:                                              │
│ │  ├─ Katana'ya Stok Artışı (SyncProductStockAsync)            │
│ │  └─ Satış Siparişi Satırı Ekleme                             │
│ ├─ Katana'da Sales Order Oluşturma                             │
│ └─ Status: APPROVED                                             │
└──────────────────────────────────────────────────────────────────┘
    │
    │ (Admin Panelinden - Manuel)
    │ POST /api/sales-orders/{id}/sync
    ▼
┌──────────────────────────────────────────────────────────────────┐
│ KOZAYA SENKRONIZASYON İŞLEMİ                                     │
│ ├─ Sipariş Detay Kontrolü                                       │
│ ├─ Luca Request Hazırlama                                       │
│ ├─ Luca API Çağrısı (CreateSalesOrderInvoiceAsync)             │
│ └─ IsSyncedToLuca: true                                         │
└──────────────────────────────────────────────────────────────────┘
    │
    ▼
LUCA VERITABANI
    ├─ Stok Kartı (StokKarti)
    ├─ Fatura (Fatura)
    └─ Müşteri (Cari)
```

## 2. Admin Onay Detaylı Akış

```
POST /api/sales-orders/{id}/approve
    │
    ├─ 1. Sipariş Kontrolü
    │  ├─ Sipariş var mı? ──► NotFound
    │  ├─ Zaten onaylanmış mı? ──► BadRequest
    │  └─ Sipariş satırları var mı? ──► BadRequest
    │
    ├─ 2. Müşteri Kontrolü
    │  ├─ Müşteri ID'si Katana'da var mı?
    │  ├─ Yoksa müşteri adıyla ara
    │  └─ Hala yoksa yeni müşteri oluştur
    │
    ├─ 3. Her Sipariş Satırı İçin
    │  ├─ SKU Kontrolü
    │  ├─ Katana'ya Stok Artışı
    │  │  ├─ SyncProductStockAsync(sku, quantity, locationId)
    │  │  ├─ Ürün var mı kontrol et
    │  │  ├─ Varsa stok artır
    │  │  └─ Yoksa yeni ürün oluştur
    │  ├─ Variant ID'yi Çöz
    │  └─ Satış Siparişi Satırını Ekle
    │
    ├─ 4. Katana'da Sales Order Oluştur
    │  ├─ OrderNo: "SO-{order.OrderNo}"
    │  ├─ CustomerId: Bulunmuş/oluşturulmuş müşteri
    │  ├─ SalesOrderRows: Hazırlanan satırlar
    │  └─ Status: "NOT_SHIPPED"
    │
    ├─ 5. Veritabanını Güncelle (Transaction)
    │  ├─ Status: "APPROVED" (başarılı)
    │  ├─ KatanaOrderId: Oluşturulan sipariş ID'si
    │  ├─ LastSyncError: null (başarılı)
    │  └─ UpdatedAt: Şu anki zaman
    │
    └─ Response
       ├─ success: true
       ├─ message: "Sipariş onaylandı ve Katana'ya gönderildi"
       ├─ orderStatus: "APPROVED"
       └─ katanaOrderId: 456
```

## 3. Kozaya Senkronizasyon Detaylı Akış

```
POST /api/sales-orders/{id}/sync
    │
    ├─ 1. Sipariş Kontrolü
    │  ├─ Sipariş var mı? ──► NotFound
    │  ├─ Müşteri bilgisi var mı? ──► BadRequest
    │  ├─ Sipariş satırları var mı? ──► BadRequest
    │  └─ Müşteri kodu geçerli mi? ──► BadRequest
    │
    ├─ 2. Duplikasyon Kontrolü
    │  ├─ Zaten senkronize edilmiş mi?
    │  ├─ Hata yoksa reddet ──► BadRequest
    │  └─ Hata varsa yeniden dene
    │
    ├─ 3. Luca Request Hazırlama
    │  ├─ BelgeSeri: Belge serisi
    │  ├─ BelgeNo: Belge numarası
    │  ├─ CariId: Müşteri ID (Luca'da)
    │  ├─ BelgeTarihi: Sipariş tarihi
    │  ├─ Satirlar: Sipariş kalemleri
    │  │  ├─ StokId: Ürün ID (Luca'da)
    │  │  ├─ Miktar: Sipariş miktarı
    │  │  ├─ BirimFiyat: Birim fiyat
    │  │  └─ KDVOrani: KDV oranı
    │  └─ DepoKodu: Depo kodu (location mapping ile)
    │
    ├─ 4. Luca API Çağrısı (Transaction Dışında!)
    │  ├─ CreateSalesOrderInvoiceAsync(order, depoKodu)
    │  ├─ Session authentication (otomatik)
    │  ├─ Fatura oluşturma
    │  └─ Luca Order ID döner
    │
    ├─ 5. Veritabanını Güncelle (Transaction İçinde)
    │  ├─ Başarılı ise:
    │  │  ├─ IsSyncedToLuca = true
    │  │  ├─ LucaOrderId = dönen ID
    │  │  ├─ LastSyncError = null
    │  │  └─ LastSyncAt = şu anki zaman
    │  └─ Başarısız ise:
    │     ├─ IsSyncedToLuca = false
    │     ├─ LastSyncError = hata mesajı
    │     └─ LastSyncAt = şu anki zaman
    │
    └─ Response
       ├─ isSuccess: true
       ├─ message: "Luca'ya başarıyla senkronize edildi"
       ├─ lucaOrderId: 789
       └─ syncedAt: "2024-01-15T10:30:00Z"
```

## 4. Toplu Senkronizasyon Akışı

```
POST /api/sales-orders/sync-all?maxCount=50
    │
    ├─ 1. Bekleyen Siparişleri Bul
    │  └─ WHERE IsSyncedToLuca = false AND LastSyncError = null
    │     └─ TAKE maxCount (default: 50)
    │
    ├─ 2. Paralel İşleme (5 Eşzamanlı)
    │  ├─ SemaphoreSlim(5) ile kontrol
    │  └─ Her sipariş için:
    │     ├─ Müşteri kontrolü
    │     ├─ Sipariş satırları kontrolü
    │     ├─ Depo kodu mapping
    │     └─ Luca API çağrısı
    │
    ├─ 3. Sonuçları Topla
    │  ├─ Başarılı: IsSyncedToLuca = true
    │  ├─ Başarısız: LastSyncError = hata mesajı
    │  └─ LastSyncAt = şu anki zaman
    │
    ├─ 4. Performance Metrics
    │  ├─ Duration: İşlem süresi (ms)
    │  ├─ Rate: Siparişler/dakika
    │  ├─ SuccessCount: Başarılı sayı
    │  └─ FailCount: Başarısız sayı
    │
    └─ Response
       ├─ totalProcessed: 50
       ├─ successCount: 48
       ├─ failCount: 2
       ├─ durationMs: 12500
       ├─ rateOrdersPerMinute: 230.4
       └─ errors: [...]
```

## 5. Veri Modeli

```
SalesOrder
├─ Id: int
├─ OrderNo: string (SO-12345)
├─ CustomerId: int
├─ Status: string (PENDING, APPROVED, APPROVED_WITH_ERRORS, SHIPPED)
├─ KatanaOrderId: long? (Katana'da oluşturulan sipariş ID'si)
├─ LucaOrderId: int? (Luca'da oluşturulan fatura ID'si)
├─ IsSyncedToLuca: bool
├─ LastSyncAt: DateTime?
├─ LastSyncError: string?
├─ BelgeSeri: string? (Luca belge serisi)
├─ BelgeNo: string? (Luca belge numarası)
├─ DuzenlemeSaati: string? (Luca düzenleme saati)
├─ OnayFlag: bool? (Luca onay bayrağı)
├─ Lines: List<SalesOrderLine>
└─ Customer: Customer

SalesOrderLine
├─ Id: int
├─ SalesOrderId: int
├─ SKU: string
├─ ProductName: string
├─ Quantity: int
├─ PricePerUnit: decimal
├─ TaxRate: decimal
├─ VariantId: long
├─ LocationId: int?
├─ LucaStokId: int? (Luca stok kartı ID'si)
└─ LucaDetayId: int? (Luca fatura detay ID'si)
```

## 6. Hata Yönetimi

```
Admin Onay Hataları:
├─ Sipariş satırları yok
│  └─ Status: APPROVED_WITH_ERRORS
│     └─ LastSyncError: "Sipariş satırları bulunamadı"
├─ Stok artışı başarısız
│  └─ Satır atlanır (continue)
│     └─ Status: APPROVED_WITH_ERRORS (eğer tüm satırlar başarısız)
└─ Katana API hatası
   └─ Status: APPROVED_WITH_ERRORS
      └─ LastSyncError: API hata mesajı

Kozaya Senkronizasyon Hataları:
├─ Müşteri bilgisi eksik
│  └─ BadRequest: "Müşteri bilgisi eksik"
├─ Sipariş satırları yok
│  └─ BadRequest: "Sipariş satırları bulunamadı"
├─ Müşteri kodu geçersiz
│  └─ BadRequest: "Müşterinin geçerli bir Vergi No veya Luca Cari Kodu eksik"
├─ Zaten senkronize edilmiş
│  └─ BadRequest: "Order already synced to Luca"
└─ Luca API hatası
   └─ BadRequest: Luca hata mesajı
      └─ LastSyncError: Hata kaydedilir
```
