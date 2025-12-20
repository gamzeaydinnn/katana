# 📋 Sipariş Tipleri CRUD Test Rehberi

## 🎯 Genel Bakış

Bu dokümantasyon, tüm sipariş tiplerinin (SalesOrder, PurchaseOrder, ManufacturingOrder, Invoice) CRUD operasyonlarının durumunu ve test yöntemlerini açıklar.

---

## 📊 CRUD Operasyonları Durum Tablosu

| Sipariş Tipi | Create | Read | Update | Delete | Controller | Test Dosyası |
|--------------|--------|------|--------|--------|------------|--------------|
| **SalesOrder** | ❌ (Webhook) | ✅ | ✅ | ❌ | `SalesOrdersController.cs` | `SalesOrdersControllerTests.cs` |
| **PurchaseOrder** | ✅ | ✅ | ✅ | ✅ | `PurchaseOrdersController.cs` | `PurchaseOrdersControllerTests.cs` |
| **ManufacturingOrder** | ✅ | ✅ | ✅ | ✅ | `ManufacturingOrdersController.cs` | `ManufacturingOrdersControllerTests.cs` |
| **Invoice** | ✅ | ✅ | ✅ | ✅ | `InvoicesController.cs` | `InvoicesControllerTests.cs` |

---

## 🔍 Detaylı Endpoint Listesi

### 1️⃣ SalesOrder (Satış Siparişi)

**Base URL:** `/api/sales-orders`

#### Read Operations
```http
GET /api/sales-orders                    # Liste (pagination, filter)
GET /api/sales-orders/{id}               # Detay
GET /api/sales-orders/{id}/sync-status   # Senkronizasyon durumu
GET /api/sales-orders/stats              # İstatistikler
```

#### Update Operations
```http
PATCH /api/sales-orders/{id}/luca-fields # Luca alanlarını güncelle
POST  /api/sales-orders/{id}/sync        # Luca'ya senkronize et
POST  /api/sales-orders/sync-all         # Toplu senkronizasyon
```

**Not:** Create ve Delete yok çünkü:
- Create: Katana webhook'tan otomatik gelir
- Delete: İş kuralı gereği silinmez

---

### 2️⃣ PurchaseOrder (Satınalma Siparişi)

**Base URL:** `/api/purchase-orders`

#### CRUD Operations
```http
# Create
POST /api/purchase-orders
Content-Type: application/json
{
  "supplierId": 1,
  "orderDate": "2024-12-04T10:00:00Z",
  "items": [
    {
      "productId": 1,
      "quantity": 10,
      "unitPrice": 100.00
    }
  ]
}

# Read
GET /api/purchase-orders                 # Liste
GET /api/purchase-orders/{id}            # Detay
GET /api/purchase-orders/{id}/sync-status # Sync durumu
GET /api/purchase-orders/stats           # İstatistikler

# Update
PATCH /api/purchase-orders/{id}/luca-fields
Content-Type: application/json
{
  "documentSeries": "A",
  "vatIncluded": true,
  "description": "Updated description"
}

# Delete
DELETE /api/purchase-orders/{id}         # Sadece senkronize edilmemişler
```

#### Sync Operations
```http
POST /api/purchase-orders/{id}/sync      # Tek sipariş sync
POST /api/purchase-orders/sync-all       # Toplu sync
POST /api/purchase-orders/retry-failed   # Hatalıları yeniden dene
```

---

### 3️⃣ ManufacturingOrder (Üretim Emri)

**Base URL:** `/api/manufacturing-orders`

#### CRUD Operations
```http
# Create
POST /api/manufacturing-orders
Content-Type: application/json
{
  "productId": 1,
  "quantity": 100,
  "status": "NotStarted",
  "dueDate": "2024-12-15T00:00:00Z"
}

# Read
GET /api/manufacturing-orders            # Liste
GET /api/manufacturing-orders/{id}       # Detay
GET /api/manufacturing-orders/stats      # İstatistikler

# Update
PUT /api/manufacturing-orders/{id}
Content-Type: application/json
{
  "quantity": 150,
  "status": "InProgress",
  "dueDate": "2024-12-20T00:00:00Z"
}

# Delete
DELETE /api/manufacturing-orders/{id}    # Sadece senkronize edilmemişler
```

**Status Değerleri:**
- `NotStarted` - Başlamadı
- `InProgress` - Devam ediyor
- `Completed` - Tamamlandı
- `Cancelled` - İptal edildi

---

### 4️⃣ Invoice (Fatura)

**Base URL:** `/api/invoices`

#### CRUD Operations
```http
# Create
POST /api/invoices
Content-Type: application/json
{
  "customerId": 1,
  "invoiceDate": "2024-12-04T10:00:00Z",
  "dueDate": "2025-01-04T10:00:00Z",
  "items": [
    {
      "productId": 1,
      "quantity": 5,
      "unitPrice": 200.00
    }
  ]
}

# Read
GET /api/invoices                        # Tüm faturalar
GET /api/invoices/{id}                   # Detay
GET /api/invoices/by-number/{invoiceNo}  # Fatura numarasına göre
GET /api/invoices/customer/{customerId}  # Müşteriye göre
GET /api/invoices/status/{status}        # Duruma göre
GET /api/invoices/range?startDate=...&endDate=... # Tarih aralığı
GET /api/invoices/overdue                # Vadesi geçenler
GET /api/invoices/unsynced               # Senkronize edilmemişler
GET /api/invoices/statistics             # İstatistikler

# Update
PUT /api/invoices/{id}
Content-Type: application/json
{
  "dueDate": "2025-02-04T10:00:00Z"
}

PUT /api/invoices/{id}/status
Content-Type: application/json
{
  "status": "Paid"
}

# Delete
DELETE /api/invoices/{id}
```

**Status Değerleri:**
- `Draft` - Taslak
- `Pending` - Beklemede
- `Paid` - Ödendi
- `Overdue` - Vadesi geçti
- `Cancelled` - İptal edildi

---

## 🧪 Test Çalıştırma

### Unit Testler

```bash
# Tüm testleri çalıştır
dotnet test

# Belirli bir test sınıfını çalıştır
dotnet test --filter "FullyQualifiedName~SalesOrdersControllerTests"
dotnet test --filter "FullyQualifiedName~PurchaseOrdersControllerTests"
dotnet test --filter "FullyQualifiedName~ManufacturingOrdersControllerTests"
dotnet test --filter "FullyQualifiedName~InvoicesControllerTests"

# Belirli bir test metodunu çalıştır
dotnet test --filter "FullyQualifiedName~SalesOrdersControllerTests.GetAll_ReturnsOkResult_WithListOfOrders"

# Verbose output ile çalıştır
dotnet test --logger "console;verbosity=detailed"
```

### Integration Testler

```bash
# Integration testleri çalıştır
dotnet test --filter "FullyQualifiedName~OrderCrudIntegrationTests"

# Belirli bir integration test
dotnet test --filter "FullyQualifiedName~OrderCrudIntegrationTests.Invoices_FullCrudCycle_WorksCorrectly"
```

**Not:** Integration testler için API'nin çalışıyor olması gerekir:
```bash
cd src/Katana.API
dotnet run
```

---

## 📝 Manuel Test Örnekleri

### Postman/cURL ile Test

#### 1. PurchaseOrder Oluştur
```bash
curl -X POST http://localhost:5000/api/purchase-orders \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "supplierId": 1,
    "orderDate": "2024-12-04T10:00:00Z",
    "items": [
      {
        "productId": 1,
        "quantity": 10,
        "unitPrice": 100.00
      }
    ]
  }'
```

#### 2. ManufacturingOrder Oluştur
```bash
curl -X POST http://localhost:5000/api/manufacturing-orders \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "productId": 1,
    "quantity": 100,
    "status": "NotStarted",
    "dueDate": "2024-12-15T00:00:00Z"
  }'
```

#### 3. Invoice Oluştur
```bash
curl -X POST http://localhost:5000/api/invoices \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "customerId": 1,
    "invoiceDate": "2024-12-04T10:00:00Z",
    "dueDate": "2025-01-04T10:00:00Z",
    "items": [
      {
        "productId": 1,
        "quantity": 5,
        "unitPrice": 200.00
      }
    ]
  }'
```

#### 4. İstatistikleri Kontrol Et
```bash
# SalesOrder stats
curl http://localhost:5000/api/sales-orders/stats \
  -H "Authorization: Bearer YOUR_TOKEN"

# PurchaseOrder stats
curl http://localhost:5000/api/purchase-orders/stats \
  -H "Authorization: Bearer YOUR_TOKEN"

# ManufacturingOrder stats
curl http://localhost:5000/api/manufacturing-orders/stats \
  -H "Authorization: Bearer YOUR_TOKEN"

# Invoice stats
curl http://localhost:5000/api/invoices/statistics \
  -H "Authorization: Bearer YOUR_TOKEN"
```

---

## ✅ Test Checklist

### SalesOrder
- [x] Liste endpoint'i çalışıyor
- [x] Detay endpoint'i çalışıyor
- [x] Luca alanları güncelleme çalışıyor
- [x] Senkronizasyon çalışıyor
- [x] İstatistikler çalışıyor
- [x] Unit testler yazıldı

### PurchaseOrder
- [x] Create endpoint'i çalışıyor
- [x] Read endpoint'leri çalışıyor
- [x] Update endpoint'i çalışıyor
- [x] Delete endpoint'i çalışıyor
- [x] Sync operasyonları çalışıyor
- [x] Unit testler yazıldı

### ManufacturingOrder
- [x] Create endpoint'i oluşturuldu
- [x] Read endpoint'leri oluşturuldu
- [x] Update endpoint'i oluşturuldu
- [x] Delete endpoint'i oluşturuldu
- [x] İstatistikler oluşturuldu
- [x] Unit testler yazıldı

### Invoice
- [x] Create endpoint'i çalışıyor
- [x] Read endpoint'leri çalışıyor
- [x] Update endpoint'leri çalışıyor
- [x] Delete endpoint'i çalışıyor
- [x] Status güncelleme çalışıyor
- [x] Unit testler yazıldı

### Integration Tests
- [x] SalesOrder integration test
- [x] PurchaseOrder integration test
- [x] ManufacturingOrder integration test
- [x] Invoice full CRUD cycle test

---

## 🐛 Bilinen Sorunlar ve Notlar

1. **SalesOrder Create Yok:** Katana webhook'tan otomatik gelir, manuel create endpoint'i yok.

2. **Delete Kısıtlamaları:** 
   - PurchaseOrder: Sadece senkronize edilmemişler silinebilir
   - ManufacturingOrder: Sadece senkronize edilmemişler silinebilir
   - Invoice: Tüm faturalar silinebilir

3. **Authentication:** Tüm endpoint'ler `[Authorize]` attribute'u ile korunuyor. Test için valid JWT token gerekli.

4. **Integration Test Gereksinimleri:**
   - API çalışıyor olmalı
   - Database'de test için gerekli Customer, Supplier, Product kayıtları olmalı

---

## 📚 İlgili Dosyalar

### Controllers
- `src/Katana.API/Controllers/SalesOrdersController.cs`
- `src/Katana.API/Controllers/PurchaseOrdersController.cs`
- `src/Katana.API/Controllers/ManufacturingOrdersController.cs`
- `src/Katana.API/Controllers/InvoicesController.cs`

### Unit Tests
- `tests/Katana.Tests/Controllers/SalesOrdersControllerTests.cs`
- `tests/Katana.Tests/Controllers/PurchaseOrdersControllerTests.cs`
- `tests/Katana.Tests/Controllers/ManufacturingOrdersControllerTests.cs`
- `tests/Katana.Tests/Controllers/InvoicesControllerTests.cs`

### Integration Tests
- `tests/Katana.Tests/Integration/OrderCrudIntegrationTests.cs`

### Entities
- `src/Katana.Core/Entities/SalesOrder.cs`
- `src/Katana.Core/Entities/PurchaseOrder.cs`
- `src/Katana.Core/Entities/ManufacturingOrder.cs`
- `src/Katana.Core/Entities/Invoice.cs`

---

## 🎓 Test Yazma Best Practices

1. **Arrange-Act-Assert Pattern:** Her test bu 3 bölümden oluşmalı
2. **In-Memory Database:** Unit testlerde gerçek DB yerine in-memory kullan
3. **Mock Services:** External service'leri mock'la
4. **Descriptive Names:** Test isimleri ne test ettiğini açıkça belirtmeli
5. **Independent Tests:** Her test bağımsız çalışabilmeli
6. **Clean Up:** Test sonrası temizlik (in-memory DB her test için yeni instance)

---

## 🚀 Sonraki Adımlar

1. ✅ ManufacturingOrder controller oluşturuldu
2. ✅ Tüm tipler için unit testler yazıldı
3. ✅ Integration testler eklendi
4. ⏳ Performance testleri eklenebilir
5. ⏳ E2E testler eklenebilir
6. ⏳ Load testing yapılabilir

---

**Son Güncelleme:** 4 Aralık 2024
