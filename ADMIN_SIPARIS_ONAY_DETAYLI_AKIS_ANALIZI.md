# Admin Sipariş Onayı - Detaylı Akış Analizi

## 📋 Özet

Admin bir satış siparişini onayladığında, sistem **4 ürünlü bir siparişi** şu adımlarla işler:

1. **Stok Kartı Hazırlığı** (Luca'da her SKU için stok kartı oluştur/kontrol et)
2. **Katana'ya Gönderme** (Sipariş Katana'ya kaydedilir)
3. **Luca'ya Fatura Gönderme** (Satış faturası oluştur)
4. **Veritabanı Güncelleme** (Durumlar ve mapping'ler kaydedilir)

---

## 🔄 DETAYLI AKIŞ (4 Ürünlü Sipariş Örneği)

### **ADIM 1: Admin Onay Butonuna Basıyor**

```
POST /api/sales-orders/{id}/approve
User: Admin
```

### **ADIM 2: Sipariş Doğrulama (Validation)**

Sistem şu kontrolleri yapar:

- ✅ Sipariş var mı? (OrderId = 123)
- ✅ Zaten onaylanmış mı? (Status != "APPROVED" && Status != "SHIPPED")
- ✅ Sipariş satırları var mı? (Lines.Count > 0)
- ✅ Her satırda SKU var mı?
- ✅ Her satırda pozitif miktar var mı?
- ✅ Her satırda VariantId var mı?

**Örnek Sipariş:**

```
OrderId: 123
OrderNo: "SO-2025-001"
Status: "PENDING"
Lines: 4 satır
  - Line 1: SKU="PROD-001", ProductName="Ürün A", Quantity=10, VariantId=1
  - Line 2: SKU="PROD-002", ProductName="Ürün B", Quantity=5, VariantId=2
  - Line 3: SKU="PROD-003", ProductName="Ürün C", Quantity=20, VariantId=3
  - Line 4: SKU="PROD-004", ProductName="Ürün D", Quantity=15, VariantId=4
```

### **ADIM 3: Katana'ya Gönderme (Opsiyonel)**

```csharp
if (order.KatanaOrderId > 0)
{
    // Sipariş zaten Katana'dan gelmiş → Katana'ya YAZMA YOK
    // Sadece local status güncelle
    _logger.LogInformation("Order already exists in Katana. Skipping Katana API call");
}
else
{
    // Yeni sipariş → Katana'ya gönder
    var katanaOrder = BuildKatanaOrderFromSalesOrder(order);
    var katanaResult = await _katanaService.CreateSalesOrderAsync(katanaOrder);
    // KatanaOrderId = katanaResult.Id (örn: 5001)
}
```

**Katana'ya Gönderilen Veri:**

```json
{
  "OrderNo": "SO-2025-001",
  "CustomerId": 42,
  "SalesOrderRows": [
    {
      "SKU": "PROD-001",
      "ProductName": "Ürün A",
      "Quantity": 10,
      "VariantId": 1
    },
    {
      "SKU": "PROD-002",
      "ProductName": "Ürün B",
      "Quantity": 5,
      "VariantId": 2
    },
    {
      "SKU": "PROD-003",
      "ProductName": "Ürün C",
      "Quantity": 20,
      "VariantId": 3
    },
    {
      "SKU": "PROD-004",
      "ProductName": "Ürün D",
      "Quantity": 15,
      "VariantId": 4
    }
  ]
}
```

### **ADIM 4: Stok Kartı Hazırlığı (YENI - StockCardPreparationService)**

Bu adım **Luca'ya fatura göndermeden ÖNCE** çalışır!

```csharp
// StockCardPreparationService.PrepareStockCardsForOrderAsync()
var stockCardResult = await _stockCardPreparationService.PrepareStockCardsForOrderAsync(order);
```

**Her satır için (4 kez) şu işlem yapılır:**

#### **Satır 1: PROD-001 (Ürün A)**

```
1. FindStockCardBySkuAsync("PROD-001") → Luca'da arama
   ├─ Bulundu mu?
   │  ├─ EVET → Action="exists", SkartId=1001
   │  └─ HAYIR → Adım 2'ye git
   │
2. UpsertStockCardAsync(request) → Luca'da oluştur
   ├─ Request:
   │  {
   │    "KartKodu": "PROD-001",
   │    "KartAdi": "Ürün A",
   │    "KartTuru": 1,
   │    "OlcumBirimiId": 1,
   │    "KartAlisKdvOran": 0.20,
   │    "Barkod": "PROD-001"
   │  }
   │
   └─ Sonuç:
      ├─ Başarılı → Action="created", SkartId=1001, Message="Stock card created"
      ├─ Duplicate → Action="exists", Message="Stock card already exists"
      └─ Hata → Action="failed", Error="Luca error message"
```

#### **Satır 2: PROD-002 (Ürün B)**

```
1. FindStockCardBySkuAsync("PROD-002") → Luca'da arama
   └─ Bulundu → Action="exists", SkartId=1002
```

#### **Satır 3: PROD-003 (Ürün C)**

```
1. FindStockCardBySkuAsync("PROD-003") → Luca'da arama
   └─ Bulunmadı → UpsertStockCardAsync() → Oluştur
      └─ Başarılı → Action="created", SkartId=1003
```

#### **Satır 4: PROD-004 (Ürün D)**

```
1. FindStockCardBySkuAsync("PROD-004") → Luca'da arama
   └─ Bulunmadı → UpsertStockCardAsync() → Oluştur
      └─ Başarılı → Action="created", SkartId=1004
```

**Stok Kartı Hazırlığı Sonucu:**

```json
{
  "TotalLines": 4,
  "SuccessCount": 4,
  "FailedCount": 0,
  "SkippedCount": 0,
  "AllSucceeded": true,
  "Results": [
    {
      "SKU": "PROD-001",
      "ProductName": "Ürün A",
      "Action": "exists",
      "SkartId": 1001,
      "Message": "Stock card already exists with skartId: 1001"
    },
    {
      "SKU": "PROD-002",
      "ProductName": "Ürün B",
      "Action": "exists",
      "SkartId": 1002,
      "Message": "Stock card already exists with skartId: 1002"
    },
    {
      "SKU": "PROD-003",
      "ProductName": "Ürün C",
      "Action": "created",
      "SkartId": 1003,
      "Message": "Stock card created successfully"
    },
    {
      "SKU": "PROD-004",
      "ProductName": "Ürün D",
      "Action": "created",
      "SkartId": 1004,
      "Message": "Stock card created successfully"
    }
  ]
}
```

### **ADIM 5: Luca'ya Fatura Gönderme**

Stok kartları hazırlandıktan sonra, fatura oluşturulur:

```csharp
var depoKodu = await _locationMappingService.GetDepoKoduByLocationIdAsync(order.LocationId);
// depoKodu = "001" (varsayılan depo)

var lucaSync = await _lucaService.CreateSalesOrderInvoiceAsync(order, depoKodu);
```

**Luca'ya Gönderilen Fatura Başlığı:**

```json
{
  "BelgeSeri": "EFA2025",
  "BelgeNo": "1001",
  "BelgeTarihi": "25/12/2025",
  "VadeTarihi": "24/01/2026",
  "BelgeAciklama": "Katana Sipariş: SO-2025-001",
  "BelgeTakipNo": "SO-2025-001",
  "FaturaTur": "1",
  "ParaBirimKod": "TRY",
  "KurBedeli": 1.0,
  "KdvFlag": true,
  "ReferansNo": "SO-2025-001",
  "MusteriTedarikci": "1",
  "CariKodu": "MUS-00042",
  "CariAd": "Müşteri",
  "CariSoyad": "Adı",
  "CariKisaAd": "Müşteri Adı",
  "VergiNo": "12345678901",
  "Il": "ISTANBUL",
  "Ilce": "MERKEZ",
  "GonderimTipi": "ELEKTRONIK",
  "OdemeTipi": "DIGER",
  "EfaturaTuru": 1,
  "SiparisNo": "SO-2025-001",
  "SiparisTarihi": "2025-12-25",
  "DetayList": [
    {
      "StokKodu": "PROD-001",
      "StokAdi": "Ürün A",
      "Miktar": 10,
      "BirimFiyat": 100.0,
      "Tutar": 1000.0,
      "KdvOrani": 0.2,
      "KdvTutari": 200.0,
      "GenelToplam": 1200.0,
      "DepoKodu": "001"
    },
    {
      "StokKodu": "PROD-002",
      "StokAdi": "Ürün B",
      "Miktar": 5,
      "BirimFiyat": 200.0,
      "Tutar": 1000.0,
      "KdvOrani": 0.2,
      "KdvTutari": 200.0,
      "GenelToplam": 1200.0,
      "DepoKodu": "001"
    },
    {
      "StokKodu": "PROD-003",
      "StokAdi": "Ürün C",
      "Miktar": 20,
      "BirimFiyat": 50.0,
      "Tutar": 1000.0,
      "KdvOrani": 0.2,
      "KdvTutari": 200.0,
      "GenelToplam": 1200.0,
      "DepoKodu": "001"
    },
    {
      "StokKodu": "PROD-004",
      "StokAdi": "Ürün D",
      "Miktar": 15,
      "BirimFiyat": 66.67,
      "Tutar": 1000.0,
      "KdvOrani": 0.2,
      "KdvTutari": 200.0,
      "GenelToplam": 1200.0,
      "DepoKodu": "001"
    }
  ]
}
```

**Luca'dan Dönen Yanıt:**

```json
{
  "basarili": true,
  "ssFaturaBaslikId": 5001,
  "mesaj": "Fatura başarıyla oluşturuldu"
}
```

### **ADIM 6: Veritabanı Güncelleme (Transaction)**

```csharp
using (var tx = await _context.Database.BeginTransactionAsync())
{
    try
    {
        // 1. Sipariş durumunu güncelle
        order.Status = "APPROVED";
        order.ApprovedDate = DateTime.UtcNow;
        order.ApprovedBy = "admin@example.com";
        order.UpdatedAt = DateTime.UtcNow;

        // 2. Luca senkronizasyon bilgilerini kaydet
        order.IsSyncedToLuca = true;
        order.LucaOrderId = 5001;
        order.LastSyncAt = DateTime.UtcNow;
        order.LastSyncError = null;

        // 3. Tüm satırları güncelle
        foreach (var line in order.Lines)
        {
            line.UpdatedAt = DateTime.UtcNow;
            // Eğer Katana'ya gönderildiyse KatanaOrderId'yi set et
            if (isNewKatanaOrder)
            {
                line.KatanaOrderId = katanaResult.Id;
            }
        }

        await _context.SaveChangesAsync();

        // 4. OrderMapping kaydı oluştur (idempotency için)
        await _orderMappingRepo.SaveLucaInvoiceIdAsync(
            orderId: 123,
            lucaFaturaId: 5001,
            orderType: "SalesOrder",
            externalOrderId: "SO-2025-001",
            belgeSeri: "EFA2025",
            belgeNo: "1001",
            belgeTakipNo: "SO-2025-001"
        );

        await tx.CommitAsync();
    }
    catch (Exception ex)
    {
        await tx.RollbackAsync();
        throw;
    }
}
```

**Veritabanı Sonrası Durum:**

```
SalesOrders Tablosu:
┌─────┬──────────────┬──────────┬──────────────┬─────────────────┬──────────────┐
│ Id  │ OrderNo      │ Status   │ KatanaOrderId│ IsSyncedToLuca  │ LucaOrderId  │
├─────┼──────────────┼──────────┼──────────────┼─────────────────┼──────────────┤
│ 123 │ SO-2025-001  │ APPROVED │ 5001         │ true            │ 5001         │
└─────┴──────────────┴──────────┴──────────────┴─────────────────┴──────────────┘

SalesOrderLines Tablosu:
┌─────┬──────────────┬──────────┬──────────────┬──────────────┐
│ Id  │ SalesOrderId │ SKU      │ KatanaOrderId│ ProductName  │
├─────┼──────────────┼──────────┼──────────────┼──────────────┤
│ 1   │ 123          │ PROD-001 │ 5001         │ Ürün A       │
│ 2   │ 123          │ PROD-002 │ 5001         │ Ürün B       │
│ 3   │ 123          │ PROD-003 │ 5001         │ Ürün C       │
│ 4   │ 123          │ PROD-004 │ 5001         │ Ürün D       │
└─────┴──────────────┴──────────┴──────────────┴──────────────┘

OrderMappings Tablosu:
┌─────┬──────────┬──────────────┬──────────────┬──────────────┐
│ Id  │ OrderId  │ EntityType   │ LucaInvoiceId│ BelgeSeri    │
├─────┼──────────┼──────────────┼──────────────┼──────────────┤
│ 1   │ 123      │ SalesOrder   │ 5001         │ EFA2025      │
└─────┴──────────┴──────────────┴──────────────┴──────────────┘
```

### **ADIM 7: API Yanıtı**

```json
{
  "success": true,
  "message": "Sipariş başarıyla onaylandı",
  "orderNo": "SO-2025-001",
  "katanaOrderId": 5001,
  "lucaOrderId": 5001,
  "status": "APPROVED",
  "approvedAt": "2025-12-25T10:30:00Z",
  "stockCardResults": {
    "totalLines": 4,
    "successCount": 4,
    "failedCount": 0,
    "skippedCount": 0,
    "allSucceeded": true,
    "results": [
      {
        "sku": "PROD-001",
        "action": "exists",
        "skartId": 1001,
        "message": "Stock card already exists"
      },
      {
        "sku": "PROD-002",
        "action": "exists",
        "skartId": 1002,
        "message": "Stock card already exists"
      },
      {
        "sku": "PROD-003",
        "action": "created",
        "skartId": 1003,
        "message": "Stock card created successfully"
      },
      {
        "sku": "PROD-004",
        "action": "created",
        "skartId": 1004,
        "message": "Stock card created successfully"
      }
    ]
  }
}
```

---

## 🔗 VERİ AKIŞI DİYAGRAMI

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         ADMIN ONAY BUTONUNA BASMA                       │
└────────────────────────────────┬────────────────────────────────────────┘
                                 │
                    ┌────────────▼────────────┐
                    │  Sipariş Doğrulama      │
                    │  (Validation)           │
                    └────────────┬────────────┘
                                 │
                    ┌────────────▼────────────┐
                    │  Katana'ya Gönderme     │
                    │  (Opsiyonel)            │
                    └────────────┬────────────┘
                                 │
        ┌────────────────────────▼────────────────────────────┐
        │  STOK KARTI HAZIRLIĞI (YENİ)                        │
        │  ┌──────────────────────────────────────────────┐   │
        │  │ Her satır için:                              │   │
        │  │ 1. Luca'da SKU arama (FindStockCardBySkuAsync)│   │
        │  │ 2. Yoksa oluştur (UpsertStockCardAsync)      │   │
        │  │ 3. Sonucu kaydet                             │   │
        │  └──────────────────────────────────────────────┘   │
        │  Satır 1 (PROD-001) → exists                        │
        │  Satır 2 (PROD-002) → exists                        │
        │  Satır 3 (PROD-003) → created                       │
        │  Satır 4 (PROD-004) → created                       │
        └────────────────────────┬─────────────────────────────┘
                                 │
                    ┌────────────▼────────────┐
                    │  Luca'ya Fatura Gönder  │
                    │  (4 satırlı fatura)     │
                    │  BelgeNo: 1001          │
                    │  BelgeSeri: EFA2025     │
                    └────────────┬────────────┘
                                 │
                    ┌────────────▼────────────┐
                    │  Luca'dan ID Al         │
                    │  LucaOrderId = 5001     │
                    └────────────┬────────────┘
                                 │
                    ┌────────────▼────────────┐
                    │  DB Güncelle (TX)       │
                    │  - Status = APPROVED    │
                    │  - IsSyncedToLuca=true  │
                    │  - LucaOrderId=5001     │
                    │  - OrderMapping kaydet  │
                    └────────────┬────────────┘
                                 │
                    ┌────────────▼────────────┐
                    │  API Yanıt Gönder       │
                    │  (success=true)         │
                    └────────────────────────┘
```

---

## 📊 LUCA'DA OLUŞAN KAYITLAR

### **Stok Kartları (4 adet)**

```
Luca Stok Kartları:
┌──────────┬──────────┬──────────┬──────────────┐
│ SkartId  │ KartKodu │ KartAdi  │ OlcumBirimi  │
├──────────┼──────────┼──────────┼──────────────┤
│ 1001     │ PROD-001 │ Ürün A   │ ADET (1)     │
│ 1002     │ PROD-002 │ Ürün B   │ ADET (1)     │
│ 1003     │ PROD-003 │ Ürün C   │ ADET (1)     │
│ 1004     │ PROD-004 │ Ürün D   │ ADET (1)     │
└──────────┴──────────┴──────────┴──────────────┘
```

### **Satış Faturası (1 adet)**

```
Luca Satış Faturası:
┌──────────────┬──────────┬──────────┬──────────────┐
│ FaturaId     │ BelgeSeri│ BelgeNo  │ BelgeTarihi  │
├──────────────┼──────────┼──────────┼──────────────┤
│ 5001         │ EFA2025  │ 1001     │ 25/12/2025   │
└──────────────┴──────────┴──────────┴──────────────┘

Fatura Detayları (4 satır):
┌──────────┬──────────┬──────────┬──────────┬──────────┐
│ DetayId  │ StokKodu │ Miktar   │ Tutar    │ KdvTutar │
├──────────┼──────────┼──────────┼──────────┼──────────┤
│ 1        │ PROD-001 │ 10       │ 1000.00  │ 200.00   │
│ 2        │ PROD-002 │ 5        │ 1000.00  │ 200.00   │
│ 3        │ PROD-003 │ 20       │ 1000.00  │ 200.00   │
│ 4        │ PROD-004 │ 15       │ 1000.00  │ 200.00   │
└──────────┴──────────┴──────────┴──────────┴──────────┘
```

---

## ⚠️ HATA SENARYOLARI

### **Senaryo 1: Stok Kartı Oluşturma Başarısız**

```
Satır 3 (PROD-003) için UpsertStockCardAsync() başarısız
↓
Action = "failed"
Error = "Luca API error: Invalid KartKodu format"
↓
Sistem devam eder (diğer satırlar işlenir)
↓
Fatura yine gönderilir (stok kartı hatası faturayı engellemiyor)
↓
Response'da hata gösterilir ama onay tamamlanır
```

### **Senaryo 2: Luca'ya Fatura Gönderme Başarısız**

```
CreateSalesOrderInvoiceAsync() başarısız
↓
lucaSync.IsSuccess = false
↓
order.IsSyncedToLuca = false
order.LastSyncError = "Luca error message"
↓
OrderMapping kaydı OLUŞTURULMAZ
↓
Response: success=false, message="Luca API error"
↓
Sipariş Status = "APPROVED" (yine de onaylanır)
↓
Kullanıcı manuel olarak tekrar senkronize edebilir
```

### **Senaryo 3: Duplicate Stok Kartı**

```
Satır 1 (PROD-001) için UpsertStockCardAsync()
↓
Luca: "Bu SKU daha önce kullanılmış" hatası
↓
Sistem bunu başarı olarak işler
↓
Action = "exists"
Message = "Stock card already exists (duplicate detected)"
↓
Devam eder
```

---

## 🔐 İDEMPOTENSİ (Tekrar Onay Yapılırsa Ne Olur?)

```
Admin aynı siparişi 2. kez onay butonuna basarsa:
↓
1. Sipariş Status = "APPROVED" → Zaten onaylanmış
   ↓
   return BadRequest("Bu sipariş zaten onaylanmış")
   ↓
   Hiçbir işlem yapılmaz
```

---

## 📝 LOGGING

Sistem şu noktaları loglar:

```
[INFO] ApproveOrder started. OrderId=123, User=admin@example.com
[INFO] ApproveOrder: Validation passed. OrderId=123, OrderNo=SO-2025-001, LineCount=4
[INFO] ApproveOrder: Creating new order in Katana. OrderNo=SO-2025-001
[INFO] ApproveOrder: Katana order created. OrderId=123, KatanaOrderId=5001
[INFO] ApproveOrder: Preparing stock cards for 4 lines. OrderId=123
[INFO] Starting stock card preparation for order SO-2025-001 with 4 lines
[DEBUG] Stock card exists for SKU PROD-001: skartId=1001
[DEBUG] Stock card exists for SKU PROD-002: skartId=1002
[INFO] Creating stock card for SKU: PROD-003
[INFO] Stock card created for SKU PROD-003: Stock card created successfully
[INFO] Creating stock card for SKU: PROD-004
[INFO] Stock card created for SKU PROD-004: Stock card created successfully
[INFO] Stock card preparation completed for order SO-2025-001: Total=4, Success=4, Failed=0, Skipped=0
[INFO] ApproveOrder: Stock card preparation complete. Total=4, Success=4, Failed=0, Skipped=0
[INFO] ApproveOrder: Sending to Luca. OrderId=123, DepoKodu=001
[INFO] ApproveOrder: Luca sync successful. OrderId=123, LucaOrderId=5001
[INFO] ApproveOrder: OrderMapping created. OrderId=123, LucaInvoiceId=5001
[INFO] ApproveOrder: Database updated. OrderId=123, KatanaOrderId=5001, Status=APPROVED
```

---

## 🎯 ÖZET

| Adım | İşlem                | Gidilen Sistem | Sonuç                   |
| ---- | -------------------- | -------------- | ----------------------- |
| 1    | Doğrulama            | Lokal DB       | ✅ Geçti                |
| 2    | Katana'ya Gönder     | Katana API     | ✅ OrderId=5001         |
| 3    | Stok Kartı Hazırlığı | Luca API       | ✅ 4 SKU kontrol edildi |
| 4    | Fatura Gönder        | Luca API       | ✅ FaturaId=5001        |
| 5    | DB Güncelle          | Lokal DB       | ✅ Status=APPROVED      |
| 6    | Yanıt Gönder         | Frontend       | ✅ success=true         |

**Toplam Süre:** ~2-5 saniye (Luca API'nin hızına bağlı)

**Luca'da Oluşan Kayıtlar:**

- 4 Stok Kartı (PROD-001, PROD-002, PROD-003, PROD-004)
- 1 Satış Faturası (BelgeNo=1001, 4 detay satırı)

**Veritabanında Oluşan Kayıtlar:**

- 1 OrderMapping (idempotency için)
- SalesOrder Status güncellemesi
- SalesOrderLines KatanaOrderId güncellemesi
