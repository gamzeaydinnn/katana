# Senkronizasyon Başlat Dropdown Analizi

## 1. FRONTEND: Dropdown Seçenekleri → API Endpoint Mapping

### Dropdown Tanımı (SyncManagement.tsx)
```
Senkronizasyon Başlat Dialog → Senkronizasyon Tipi Select
```

### Seçenekler ve Mapping

| # | Dropdown Label | Frontend Value | API Endpoint | HTTP Method | Direction |
|---|---|---|---|---|---|
| 1 | Stok Hareketleri | `STOCK` | `/Sync/stock` | POST | Katana→Luca |
| 2 | Fatura | `INVOICE` | `/Sync/invoices` | POST | Katana→Luca |
| 3 | Müşteri (Cari) | `CUSTOMER` | `/Sync/customers` | POST | Katana→Luca |
| 4 | İrsaliye | `DESPATCH` | `/Sync/from-luca/despatch` | POST | Luca→Katana |
| 5 | Tümü | `ALL` | `/Sync/run` | POST | Katana→Luca (ALL) |
| 6 | Stok Kartları (Luca) | `STOCK_CARD` | `/Sync/to-luca/stock-cards` | POST | Katana→Luca |
| 7 | Tedarikçi Kartları (Koza) | `SUPPLIER` | **MISSING** | - | - |
| 8 | Depo Kartları (Koza) | `WAREHOUSE` | **MISSING** | - | - |
| 9 | Müşteri Kartları (Luca Cari) | `CUSTOMER_LUCA` | **MISSING** | - | - |

---

## 2. BACKEND: SyncController.cs Endpoint Analizi

### Mevcut Endpoint'ler (SyncController.cs)

```csharp
[HttpPost("stock")]           → SyncStockAsync()           ✅ Katana→Luca
[HttpPost("invoices")]        → SyncInvoicesAsync()        ✅ Katana→Luca
[HttpPost("customers")]       → SyncCustomersAsync()       ✅ Katana→Luca
[HttpPost("run")]             → RunCompleteSync()          ✅ Katana→Luca (ALL)
[HttpPost("to-luca/stock-cards")] → SyncProductsToLuca()  ✅ Katana→Luca
[HttpPost("from-luca/despatch")]  → SyncDespatchFromLuca() ✅ Luca→Katana
```

### Eksik Endpoint'ler (Frontend'de var, Backend'de yok)

```
SUPPLIER        → /Sync/suppliers        ❌ ENDPOINT YOK
WAREHOUSE       → /Sync/warehouses       ❌ ENDPOINT YOK
CUSTOMER_LUCA   → /Sync/customers-luca   ❌ ENDPOINT YOK
```

---

## 3. BACKEND: ISyncService Interface'i

### Implement Edilen Method'lar

```csharp
✅ SyncStockAsync()              → Katana→Luca
✅ SyncInvoicesAsync()           → Katana→Luca
✅ SyncCustomersAsync()          → Katana→Luca
✅ SyncProductsToLucaAsync()     → Katana→Luca (Stok Kartları)
✅ SyncDespatchFromLucaAsync()   → Luca→Katana
✅ SyncAllAsync()                → Katana→Luca (ALL)
✅ SyncSuppliersToKozaAsync()    → Katana→Koza (SUPPLIER)
✅ SyncWarehousesToKozaAsync()   → Katana→Koza (WAREHOUSE)
✅ SyncCustomersToLucaAsync()    → Katana→Luca (CUSTOMER_LUCA)
```

### Luca→Katana Method'ları

```csharp
✅ SyncStockFromLucaAsync()      → Luca→Katana
✅ SyncInvoicesFromLucaAsync()   → Luca→Katana
✅ SyncCustomersFromLucaAsync()  → Luca→Katana
✅ SyncDespatchFromLucaAsync()   → Luca→Katana
✅ SyncAllFromLucaAsync()        → Luca→Katana (ALL)
```

---

## 4. BACKEND: SyncController.cs StartSync() Method

```csharp
[HttpPost("start")]
public async Task<IActionResult> StartSync([FromBody] StartSyncRequest request)
{
    var syncKey = request.SyncType.ToUpperInvariant();
    
    var result = syncKey switch
    {
        "STOCK"           → await _syncService.SyncStockAsync()
        "INVOICE"         → await _syncService.SyncInvoicesAsync()
        "CUSTOMER"        → await _syncService.SyncCustomersAsync()
        "DESPATCH"        → await _syncService.SyncDespatchFromLucaAsync()
        "PRODUCT"         → await _syncService.SyncProductsToLucaAsync()
        "STOCK_CARD"      → await _syncService.SyncProductsToLucaAsync()
        "SUPPLIER"        → await _syncService.SyncSuppliersToKozaAsync()
        "WAREHOUSE"       → await _syncService.SyncWarehousesToKozaAsync()
        "CUSTOMER_LUCA"   → await _syncService.SyncCustomersToLucaAsync()
        "ALL"             → await ConvertBatchResult(await _syncService.SyncAllAsync())
        _                 → placeholder (passthrough)
    };
}
```

---

## 5. SORUN ÖZETI

### ✅ ÇALIŞAN (Backend'de implement edilmiş)

1. **Stok Hareketleri** (STOCK)
   - Frontend: `STOCK` → `/Sync/stock`
   - Backend: `SyncStockAsync()` ✅
   - Yön: Katana→Luca

2. **Fatura** (INVOICE)
   - Frontend: `INVOICE` → `/Sync/invoices`
   - Backend: `SyncInvoicesAsync()` ✅
   - Yön: Katana→Luca

3. **Müşteri (Cari)** (CUSTOMER)
   - Frontend: `CUSTOMER` → `/Sync/customers`
   - Backend: `SyncCustomersAsync()` ✅
   - Yön: Katana→Luca

4. **İrsaliye** (DESPATCH)
   - Frontend: `DESPATCH` → `/Sync/from-luca/despatch`
   - Backend: `SyncDespatchFromLucaAsync()` ✅
   - Yön: Luca→Katana

5. **Tümü** (ALL)
   - Frontend: `ALL` → `/Sync/run`
   - Backend: `SyncAllAsync()` ✅
   - Yön: Katana→Luca (ALL)

6. **Stok Kartları (Luca)** (STOCK_CARD)
   - Frontend: `STOCK_CARD` → `/Sync/to-luca/stock-cards`
   - Backend: `SyncProductsToLucaAsync()` ✅
   - Yön: Katana→Luca

### ✅ DÜZELTILDI (Frontend'de var, Backend'de endpoint eklendi)

7. **Tedarikçi Kartları (Koza)** (SUPPLIER)
   - Frontend: `SUPPLIER` → `/Sync/suppliers` ✅ (FIXED)
   - Backend: `SyncSuppliersToKozaAsync()` ✅
   - Yön: Katana→Koza

8. **Depo Kartları (Koza)** (WAREHOUSE)
   - Frontend: `WAREHOUSE` → `/Sync/warehouses` ✅ (FIXED)
   - Backend: `SyncWarehousesToKozaAsync()` ✅
   - Yön: Katana→Koza

9. **Müşteri Kartları (Luca Cari)** (CUSTOMER_LUCA)
   - Frontend: `CUSTOMER_LUCA` → `/Sync/customers-luca` ✅ (FIXED)
   - Backend: `SyncCustomersToLucaAsync()` ✅
   - Yön: Katana→Luca

---

## 6. NETWORK REQUEST DETAYI

### Request Format (SyncManagement.tsx)

```typescript
// Dropdown'dan seçim yapıldığında:
const result = await stockAPI.startSync({ syncType });

// API çağrısı (api.ts):
POST {endpoint} HTTP/1.1
Content-Type: application/json
Timeout: 120000ms

Body: {
  syncType: "STOCK" | "INVOICE" | "CUSTOMER" | "DESPATCH" | "ALL" | "STOCK_CARD" | "SUPPLIER" | "WAREHOUSE" | "CUSTOMER_LUCA"
}
```

### Endpoint Mapping (api.ts)

```typescript
const endpointMap: Record<string, string> = {
  STOCK: "/Sync/stock",
  INVOICE: "/Sync/invoices",
  CUSTOMER: "/Sync/customers",
  DESPATCH: "/Sync/from-luca/despatch",
  ALL: "/Sync/run",
  STOCK_CARD: "/Sync/to-luca/stock-cards",
  PRODUCT: "/Sync/to-luca/stock-cards",
};

// SUPPLIER, WAREHOUSE, CUSTOMER_LUCA → DEFAULT: "/Sync/to-luca/stock-cards" (YANLIŞ!)
```

---

## 7. KATANA ↔ LUCA YÖNLERİ

### Katana→Luca (Push)
- Stok Hareketleri (STOCK)
- Fatura (INVOICE)
- Müşteri (CUSTOMER)
- Stok Kartları (STOCK_CARD)
- Müşteri Kartları (CUSTOMER_LUCA)

### Luca→Katana (Pull)
- İrsaliye (DESPATCH)

### Katana→Koza (Push)
- Tedarikçi Kartları (SUPPLIER)
- Depo Kartları (WAREHOUSE)

### Katana→Luca (ALL - Mixed)
- Tümü (ALL) - Tüm sync işlemlerini çalıştırır

---

## 8. DB: SyncOperationLogs Tablosu

```sql
CREATE TABLE SyncLogs (
    Id INT PRIMARY KEY,
    SyncType VARCHAR(50),           -- "STOCK", "INVOICE", "CUSTOMER", vb.
    Status VARCHAR(20),             -- "PENDING", "RUNNING", "SUCCESS", "FAILED"
    ErrorMessage NVARCHAR(MAX),
    ProcessedRecords INT,
    SuccessfulRecords INT,
    FailedRecords INT,
    StartTime DATETIME,
    EndTime DATETIME,
    TriggeredBy VARCHAR(100),
    Details NVARCHAR(MAX)
);
```

---

## 9. ÖNERİLER

### Acil Düzeltmeler

1. **Frontend api.ts'de endpoint mapping ekle:**
   ```typescript
   const endpointMap: Record<string, string> = {
     // ... existing ...
     SUPPLIER: "/Sync/suppliers",        // ← EKLE
     WAREHOUSE: "/Sync/warehouses",      // ← EKLE
     CUSTOMER_LUCA: "/Sync/customers-luca", // ← EKLE
   };
   ```

2. **Backend SyncController.cs'de eksik endpoint'leri ekle:**
   ```csharp
   [HttpPost("suppliers")]
   public async Task<ActionResult<SyncResultDto>> SyncSuppliers()
   
   [HttpPost("warehouses")]
   public async Task<ActionResult<SyncResultDto>> SyncWarehouses()
   
   [HttpPost("customers-luca")]
   public async Task<ActionResult<SyncResultDto>> SyncCustomersLuca()
   ```

3. **SyncController.cs StartSync() method'unda eksik case'leri ekle:**
   ```csharp
   "SUPPLIER" => await _syncService.SyncSuppliersToKozaAsync(),
   "WAREHOUSE" => await _syncService.SyncWarehousesToKozaAsync(),
   "CUSTOMER_LUCA" => await _syncService.SyncCustomersToLucaAsync(),
   ```

---

## 10. TEST KOMUTLARI

### Frontend'de Dropdown Test
```bash
# Browser DevTools Console:
# SyncManagement.tsx'de "Senkronizasyon Başlat" butonuna tıkla
# Dropdown'dan "Tedarikçi Kartları (Koza)" seç
# Network tab'ında POST request'i kontrol et
# Expected: POST /api/Sync/suppliers (şu anda MISSING)
```

### Backend'de Endpoint Test
```bash
# SUPPLIER endpoint test
curl -X POST http://localhost:5000/api/Sync/suppliers \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{"syncType":"SUPPLIER"}'

# WAREHOUSE endpoint test
curl -X POST http://localhost:5000/api/Sync/warehouses \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{"syncType":"WAREHOUSE"}'

# CUSTOMER_LUCA endpoint test
curl -X POST http://localhost:5000/api/Sync/customers-luca \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{"syncType":"CUSTOMER_LUCA"}'
```

### DB'de Log Kontrol
```sql
SELECT * FROM SyncLogs 
WHERE SyncType IN ('STOCK', 'INVOICE', 'CUSTOMER', 'DESPATCH', 'SUPPLIER', 'WAREHOUSE', 'CUSTOMER_LUCA')
ORDER BY StartTime DESC;
```

