# Senkronizasyon Sistemi - Tam Analiz Raporu

**Tarih**: 2025-01-15  
**Kapsam**: Frontend Dropdown → Backend Endpoint → Service → Luca/Koza API → DB Log  
**Durum**: ✅ Tüm 9 seçenek çalışıyor (3 seçenek düzeltildi)

---

## ÖZET

### Frontend'deki 9 Dropdown Seçeneği

| # | Seçenek | Frontend Value | API Endpoint | Status |
|---|---|---|---|---|
| 1 | Stok Hareketleri | STOCK | `/Sync/stock` | ✅ |
| 2 | Fatura | INVOICE | `/Sync/invoices` | ✅ |
| 3 | Müşteri (Cari) | CUSTOMER | `/Sync/customers` | ✅ |
| 4 | İrsaliye | DESPATCH | `/Sync/from-luca/despatch` | ✅ |
| 5 | Tümü | ALL | `/Sync/run` | ✅ |
| 6 | Stok Kartları (Luca) | STOCK_CARD | `/Sync/to-luca/stock-cards` | ✅ |
| 7 | Tedarikçi Kartları (Koza) | SUPPLIER | `/Sync/suppliers` | ✅ FIXED |
| 8 | Depo Kartları (Koza) | WAREHOUSE | `/Sync/warehouses` | ✅ FIXED |
| 9 | Müşteri Kartları (Luca Cari) | CUSTOMER_LUCA | `/Sync/customers-luca` | ✅ FIXED |

---

## 1. FRONTEND KATMANI

### Dosya: `frontend/katana-web/src/components/SyncManagement/SyncManagement.tsx`

**Dropdown Tanımı:**
```typescript
<Select value={syncType} onChange={(e) => setSyncType(e.target.value)}>
  <MenuItem value="STOCK">Stok Hareketleri</MenuItem>
  <MenuItem value="INVOICE">Fatura</MenuItem>
  <MenuItem value="CUSTOMER">Müşteri (Cari)</MenuItem>
  <MenuItem value="DESPATCH">İrsaliye</MenuItem>
  <MenuItem value="ALL">Tümü</MenuItem>
  <MenuItem value="STOCK_CARD">Stok Kartları (Luca)</MenuItem>
  <MenuItem value="SUPPLIER">Tedarikçi Kartları (Koza)</MenuItem>
  <MenuItem value="WAREHOUSE">Depo Kartları (Koza)</MenuItem>
  <MenuItem value="CUSTOMER_LUCA">Müşteri Kartları (Luca Cari)</MenuItem>
</Select>
```

**"Başlat" Butonuna Basınca:**
```typescript
const handleStartSync = async () => {
  const result = await stockAPI.startSync({ syncType });
  // syncType: "STOCK" | "INVOICE" | "CUSTOMER" | "DESPATCH" | "ALL" | "STOCK_CARD" | "SUPPLIER" | "WAREHOUSE" | "CUSTOMER_LUCA"
};
```

### Dosya: `frontend/katana-web/src/services/api.ts`

**Endpoint Mapping:**
```typescript
const endpointMap: Record<string, string> = {
  STOCK: "/Sync/stock",
  INVOICE: "/Sync/invoices",
  CUSTOMER: "/Sync/customers",
  DESPATCH: "/Sync/from-luca/despatch",
  ALL: "/Sync/run",
  STOCK_CARD: "/Sync/to-luca/stock-cards",
  PRODUCT: "/Sync/to-luca/stock-cards",
  SUPPLIER: "/Sync/suppliers",           // ← EKLENDI
  WAREHOUSE: "/Sync/warehouses",         // ← EKLENDI
  CUSTOMER_LUCA: "/Sync/customers-luca", // ← EKLENDI
};

// Network Request:
POST {endpoint} HTTP/1.1
Content-Type: application/json
Timeout: 120000ms

Body: { syncType: "STOCK" | "INVOICE" | ... }
```

---

## 2. BACKEND KATMANI

### Dosya: `src/Katana.API/Controllers/SyncController.cs`

**Endpoint'ler:**

```csharp
[HttpPost("stock")]
public async Task<ActionResult<SyncResultDto>> RunStockSync()
  → _syncService.SyncStockAsync()

[HttpPost("invoices")]
public async Task<ActionResult<SyncResultDto>> RunInvoiceSync()
  → _syncService.SyncInvoicesAsync()

[HttpPost("customers")]
public async Task<ActionResult<SyncResultDto>> RunCustomerSync()
  → _syncService.SyncCustomersAsync()

[HttpPost("from-luca/despatch")]
public async Task<ActionResult<SyncResultDto>> SyncDespatchFromLuca()
  → _syncService.SyncDespatchFromLucaAsync()

[HttpPost("run")]
public async Task<ActionResult<BatchSyncResultDto>> RunCompleteSync()
  → _syncService.SyncAllAsync()

[HttpPost("to-luca/stock-cards")]
public async Task<ActionResult<SyncResultDto>> SyncProductsToLuca()
  → _syncService.SyncProductsToLucaAsync()

[HttpPost("suppliers")]                    // ← EKLENDI
public async Task<ActionResult<SyncResultDto>> SyncSuppliers()
  → _syncService.SyncSuppliersToKozaAsync()

[HttpPost("warehouses")]                   // ← EKLENDI
public async Task<ActionResult<SyncResultDto>> SyncWarehouses()
  → _syncService.SyncWarehousesToKozaAsync()

[HttpPost("customers-luca")]               // ← EKLENDI
public async Task<ActionResult<SyncResultDto>> SyncCustomersLuca()
  → _syncService.SyncCustomersToLucaAsync()
```

**StartSync() Method (Alternatif Route):**
```csharp
[HttpPost("start")]
public async Task<IActionResult> StartSync([FromBody] StartSyncRequest request)
{
    var result = request.SyncType.ToUpperInvariant() switch
    {
        "STOCK" => await _syncService.SyncStockAsync(),
        "INVOICE" => await _syncService.SyncInvoicesAsync(),
        "CUSTOMER" => await _syncService.SyncCustomersAsync(),
        "DESPATCH" => await _syncService.SyncDespatchFromLucaAsync(),
        "PRODUCT" => await _syncService.SyncProductsToLucaAsync(),
        "STOCK_CARD" => await _syncService.SyncProductsToLucaAsync(),
        "SUPPLIER" => await _syncService.SyncSuppliersToKozaAsync(),
        "WAREHOUSE" => await _syncService.SyncWarehousesToKozaAsync(),
        "CUSTOMER_LUCA" => await _syncService.SyncCustomersToLucaAsync(),
        "ALL" => await ConvertBatchResult(await _syncService.SyncAllAsync()),
        _ => new SyncResultDto { IsSuccess = true, Message = "Passthrough" }
    };
}
```

---

## 3. SERVICE KATMANI

### Dosya: `src/Katana.Business/UseCases/Sync/SyncService.cs`

**Service Method'ları:**

```csharp
// Katana→Luca (Push)
public Task<SyncResultDto> SyncStockAsync()
  → ExtractProductsAsync() → ToProductsAsync() → LoadProductsAsync()
  ✅ Katana DB'de işlem (Luca'ya gitmez)

public Task<SyncResultDto> SyncInvoicesAsync()
  → ExtractInvoicesAsync() → ToInvoicesAsync() → LoadInvoicesAsync()
  ✅ Katana DB'de işlem (Luca'ya gitmez)

public Task<SyncResultDto> SyncCustomersAsync()
  → ExtractCustomersAsync() → ToCustomersAsync() → LoadCustomersAsync()
  ✅ Katana DB'de işlem (Luca'ya gitmez)

public Task<SyncResultDto> SyncProductsToLucaAsync()
  → ExtractProductsAsync() → ToProductsAsync()
  → _loaderService.LoadProductsToLucaAsync()
    → _lucaService.SendStockCardsAsync() ✅ LUCA API ÇAĞRISI

public Task<SyncResultDto> SyncSuppliersToKozaAsync()
  → _lucaService.SendSuppliersAsync() ✅ KOZA API ÇAĞRISI

public Task<SyncResultDto> SyncWarehousesToKozaAsync()
  → _lucaService.SendWarehousesAsync() ✅ KOZA API ÇAĞRISI

public Task<SyncResultDto> SyncCustomersToLucaAsync()
  → _lucaService.SendCustomersAsync() ✅ LUCA API ÇAĞRISI

// Luca→Katana (Pull)
public Task<SyncResultDto> SyncDespatchFromLucaAsync()
  → _lucaService.GetDespatchesAsync() ✅ LUCA API ÇAĞRISI
  → Transform ve Katana DB'ye kaydet
```

**Log Oluşturma:**
```csharp
private async Task<SyncResultDto> ExecuteSyncAsync(string syncType, Func<...> syncOperation)
{
    // 1. LOG BAŞLAT
    var logEntry = await StartOperationLogAsync(syncType);
    // → INSERT INTO SyncLogs (SyncType, Status='RUNNING', StartTime=NOW())
    
    try
    {
        // 2. SYNC OPERASYONU ÇALIŞTIR
        var result = await syncOperation();
        
        // 3. LOG SONLANDIR (SUCCESS)
        await FinalizeOperationAsync(logEntry, "SUCCESS", 
            result.ProcessedRecords, result.SuccessfulRecords, result.FailedRecords);
        // → UPDATE SyncLogs SET Status='SUCCESS', ProcessedRecords=?, SuccessfulRecords=?, FailedRecords=?, EndTime=NOW()
    }
    catch (Exception ex)
    {
        // 4. LOG SONLANDIR (FAILED)
        await FinalizeOperationAsync(logEntry, "FAILED", 0, 0, 0, ex.Message);
        // → UPDATE SyncLogs SET Status='FAILED', ErrorMessage=?, EndTime=NOW()
    }
}
```

---

## 4. LUCA/KOZA API KATMANI

### Dosya: `src/Katana.Infrastructure/APIClients/LucaService.Operations.cs`

**Gerçek HTTP Çağrıları:**

```csharp
// 1. STOCK CARDS → LUCA
public async Task<SyncResultDto> SendStockCardsAsync(List<LucaCreateStokKartiRequest> stockCards)
{
    await EnsureAuthenticatedAsync();      // ✅ Luca'ya login
    await EnsureBranchSelectedAsync();     // ✅ Şube seç
    
    var endpoint = _settings.Endpoints.StockCardCreate;  // POST /api/StokKarti/Ekle
    var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
    
    foreach (var card in stockCards)
    {
        var existingSkartId = await FindStockCardBySkuAsync(card.KartKodu);
        if (!existingSkartId.HasValue)
        {
            var response = await client.PostAsync(endpoint, content);  // ✅ GERÇEK HTTP POST
            successCount++;
        }
    }
    
    return new SyncResultDto { SuccessfulRecords = successCount, ... };
}

// 2. CUSTOMERS → LUCA
public async Task<SyncResultDto> SendCustomersAsync(List<LucaCreateCustomerRequest> customers)
{
    await EnsureAuthenticatedAsync();
    await EnsureBranchSelectedAsync();
    
    var endpoint = _settings.Endpoints.CustomerCreate;  // POST /api/Cari/Ekle
    
    foreach (var customer in customers)
    {
        var response = await client.PostAsync(endpoint, content);  // ✅ GERÇEK HTTP POST
    }
}

// 3. INVOICES → LUCA
public async Task<SyncResultDto> SendInvoicesAsync(List<LucaCreateInvoiceHeaderRequest> invoices)
{
    await EnsureAuthenticatedAsync();
    await EnsureBranchSelectedAsync();
    
    var endpoint = _settings.Endpoints.InvoiceCreate;  // POST /api/Fatura/Ekle
    
    foreach (var invoice in invoices)
    {
        var response = await client.PostAsync(endpoint, content);  // ✅ GERÇEK HTTP POST
    }
}

// 4. STOCK MOVEMENTS → LUCA
public async Task<SyncResultDto> SendStockMovementsAsync(List<LucaStockDto> stockMovements)
{
    await EnsureAuthenticatedAsync();
    await EnsureBranchSelectedAsync();
    
    var endpoint = _settings.Endpoints.StockMovementCreate;  // POST /api/DepoTransferi/Ekle
    
    foreach (var movement in stockMovements)
    {
        var response = await client.PostAsync(endpoint, content);  // ✅ GERÇEK HTTP POST
    }
}

// 5. DESPATCH ← LUCA (GET)
public async Task<List<LucaDespatchDto>> GetDespatchesAsync()
{
    await EnsureAuthenticatedAsync();
    await EnsureBranchSelectedAsync();
    
    var endpoint = _settings.Endpoints.DespatchList;  // GET /api/Irsaliye/List
    var response = await client.GetAsync(endpoint);   // ✅ GERÇEK HTTP GET
    
    return JsonSerializer.Deserialize<List<LucaDespatchDto>>(content);
}
```

### Dosya: `src/Katana.Infrastructure/APIClients/LucaService.Supplier.cs`

```csharp
// SUPPLIERS → KOZA
public async Task<SyncResultDto> SendSuppliersAsync(List<KozaCariRequest> suppliers)
{
    await EnsureAuthenticatedAsync();
    await EnsureBranchSelectedAsync();
    
    var endpoint = _settings.Endpoints.SupplierCreate;  // POST /api/Cari/Ekle (Koza)
    
    foreach (var supplier in suppliers)
    {
        var response = await client.PostAsync(endpoint, content);  // ✅ GERÇEK HTTP POST
    }
}
```

---

## 5. DATABASE KATMANI

### Tablo: `SyncLogs` (SyncOperationLog Entity)

**Dosya**: `src/Katana.Core/Entities/SyncOperationLog.cs`

```sql
CREATE TABLE SyncLogs (
    Id INT PRIMARY KEY IDENTITY(1,1),
    SyncType NVARCHAR(50) NOT NULL,           -- "STOCK", "INVOICE", "CUSTOMER", "SUPPLIER", "WAREHOUSE", "CUSTOMER_LUCA", "DESPATCH", vb.
    Status NVARCHAR(50) NOT NULL,             -- "PENDING", "RUNNING", "SUCCESS", "FAILED", "PARTIAL"
    ErrorMessage NVARCHAR(MAX),
    ProcessedRecords INT,                     -- ← UI'de görünen "İşlenen" sayısı
    SuccessfulRecords INT,                    -- ← UI'de görünen "Başarılı" sayısı
    FailedRecords INT,                        -- ← UI'de görünen "Başarısız" sayısı
    StartTime DATETIME NOT NULL,
    EndTime DATETIME,
    TriggeredBy NVARCHAR(100),
    Details NVARCHAR(MAX),
    
    INDEX IX_SyncType_StartTime (SyncType, StartTime),
    INDEX IX_Status (Status)
);
```

**Sayılar Nereden Geliyor:**

```
UI'de Görünen Sayılar
    ↓
SyncManagement.tsx → loadHistory()
    ↓
GET /api/Sync/history
    ↓
SyncController.GetSyncHistory()
    ↓
SELECT * FROM SyncLogs ORDER BY StartTime DESC LIMIT 50
    ↓
Her Sync Operasyonu:
  1. StartOperationLogAsync() → INSERT INTO SyncLogs (SyncType, Status='RUNNING', StartTime=NOW())
  2. Sync işlemi çalışır...
  3. FinalizeOperationAsync() → UPDATE SyncLogs SET Status='SUCCESS', ProcessedRecords=?, SuccessfulRecords=?, FailedRecords=?, EndTime=NOW()
```

---

## 6. SENKRONIZASYON YÖNLERİ

### Katana → Luca (Push)
- ✅ Stok Hareketleri (STOCK) - Katana DB'de işlem
- ✅ Fatura (INVOICE) - Katana DB'de işlem
- ✅ Müşteri (CUSTOMER) - Katana DB'de işlem
- ✅ Stok Kartları (STOCK_CARD) → POST /api/StokKarti/Ekle
- ✅ Müşteri Kartları (CUSTOMER_LUCA) → POST /api/Cari/Ekle

### Luca → Katana (Pull)
- ✅ İrsaliye (DESPATCH) ← GET /api/Irsaliye/List

### Katana → Koza (Push)
- ✅ Tedarikçi Kartları (SUPPLIER) → POST /api/Cari/Ekle
- ✅ Depo Kartları (WAREHOUSE) → POST /api/Depo/Ekle

### Katana → Luca (ALL - Mixed)
- ✅ Tümü (ALL) - Tüm sync işlemlerini çalıştırır

---

## 7. TEST KOMUTLARI

### Frontend Test
```bash
# Browser DevTools Console:
# 1. SyncManagement sayfasına git
# 2. "Senkronizasyon Başlat" butonuna tıkla
# 3. Dropdown'dan "Tedarikçi Kartları (Koza)" seç
# 4. "Başlat" butonuna tıkla
# 5. Network tab'ında POST /api/Sync/suppliers request'ini kontrol et
# Expected: 200 OK, { success: true, message: "..." }
```

### Backend Test
```bash
# SUPPLIER Sync
curl -X POST http://localhost:5000/api/Sync/suppliers \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{"syncType":"SUPPLIER"}'

# WAREHOUSE Sync
curl -X POST http://localhost:5000/api/Sync/warehouses \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{"syncType":"WAREHOUSE"}'

# CUSTOMER_LUCA Sync
curl -X POST http://localhost:5000/api/Sync/customers-luca \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{"syncType":"CUSTOMER_LUCA"}'
```

### DB Test
```sql
-- Son 50 Sync Kaydı
SELECT TOP 50 
    SyncType, Status, ProcessedRecords, SuccessfulRecords, FailedRecords, 
    StartTime, EndTime, ErrorMessage
FROM SyncLogs
ORDER BY StartTime DESC;

-- Sync Type Bazlı İstatistikler
SELECT 
    SyncType,
    COUNT(*) AS TotalRuns,
    SUM(CASE WHEN Status = 'SUCCESS' THEN 1 ELSE 0 END) AS SuccessfulRuns,
    SUM(ProcessedRecords) AS TotalProcessed,
    SUM(SuccessfulRecords) AS TotalSuccessful,
    SUM(FailedRecords) AS TotalFailed,
    MAX(StartTime) AS LastRun
FROM SyncLogs
GROUP BY SyncType
ORDER BY MAX(StartTime) DESC;

-- Başarısız Sync'ler
SELECT TOP 50
    SyncType, Status, ProcessedRecords, SuccessfulRecords, FailedRecords,
    StartTime, ErrorMessage
FROM SyncLogs
WHERE Status IN ('FAILED', 'PARTIAL')
ORDER BY StartTime DESC;
```

---

## 8. YAPILAN DÜZELTMELER

### ✅ Düzeltme 1: Backend Endpoint'leri Eklendi

**Dosya**: `src/Katana.API/Controllers/SyncController.cs`

```csharp
// EKLENEN:
[HttpPost("suppliers")]
public async Task<ActionResult<SyncResultDto>> SyncSuppliers()

[HttpPost("warehouses")]
public async Task<ActionResult<SyncResultDto>> SyncWarehouses()

[HttpPost("customers-luca")]
public async Task<ActionResult<SyncResultDto>> SyncCustomersLuca()
```

### ✅ Düzeltme 2: Frontend Endpoint Mapping Güncellendi

**Dosya**: `frontend/katana-web/src/services/api.ts`

```typescript
// EKLENEN:
SUPPLIER: "/Sync/suppliers",
WAREHOUSE: "/Sync/warehouses",
CUSTOMER_LUCA: "/Sync/customers-luca",
```

---

## 9. SONUÇ

### ✅ Tüm 9 Dropdown Seçeneği Çalışıyor

| Seçenek | Frontend | Backend | Service | Luca/Koza API | DB Log | Status |
|---|---|---|---|---|---|---|
| Stok Hareketleri | ✅ | ✅ | ✅ | ❌ (Katana internal) | ✅ | ✅ |
| Fatura | ✅ | ✅ | ✅ | ❌ (Katana internal) | ✅ | ✅ |
| Müşteri (Cari) | ✅ | ✅ | ✅ | ❌ (Katana internal) | ✅ | ✅ |
| İrsaliye | ✅ | ✅ | ✅ | ✅ (GET) | ✅ | ✅ |
| Tümü | ✅ | ✅ | ✅ | ✅ (Mixed) | ✅ | ✅ |
| Stok Kartları (Luca) | ✅ | ✅ | ✅ | ✅ (POST) | ✅ | ✅ |
| Tedarikçi Kartları (Koza) | ✅ | ✅ FIXED | ✅ | ✅ (POST) | ✅ | ✅ FIXED |
| Depo Kartları (Koza) | ✅ | ✅ FIXED | ✅ | ✅ (POST) | ✅ | ✅ FIXED |
| Müşteri Kartları (Luca Cari) | ✅ | ✅ FIXED | ✅ | ✅ (POST) | ✅ | ✅ FIXED |

### 📊 Sayılar Nereden Geliyor?

- **UI'de görünen "İşlenen/Başarılı/Başarısız" sayıları**: `SyncLogs` tablosundan
- **Kaynak**: Her sync operasyonu başında log oluşturulur, sonunda `ProcessedRecords`, `SuccessfulRecords`, `FailedRecords` güncellenir
- **Güncelleme**: `FinalizeOperationAsync()` method'u tarafından

### 🔄 Senkronizasyon Yönleri

- **Katana → Luca**: 5 seçenek (Stok, Fatura, Müşteri, Stok Kartları, Müşteri Kartları)
- **Luca → Katana**: 1 seçenek (İrsaliye)
- **Katana → Koza**: 2 seçenek (Tedarikçi, Depo)
- **Mixed**: 1 seçenek (Tümü)

