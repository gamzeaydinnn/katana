# Backend Senkronizasyon Analizi

## 1. CONTROLLER → SERVICE ZİNCİRİ

### Frontend Dropdown Seçenekleri → Backend Endpoint'leri → Service Method'ları

| # | Dropdown | Frontend Value | API Endpoint | HTTP Method | Controller Action | Service Method | Yön |
|---|---|---|---|---|---|---|---|
| 1 | Stok Hareketleri | STOCK | `/Sync/stock` | POST | `RunStockSync()` | `SyncStockAsync()` | Katana→Luca |
| 2 | Fatura | INVOICE | `/Sync/invoices` | POST | `RunInvoiceSync()` | `SyncInvoicesAsync()` | Katana→Luca |
| 3 | Müşteri (Cari) | CUSTOMER | `/Sync/customers` | POST | `RunCustomerSync()` | `SyncCustomersAsync()` | Katana→Luca |
| 4 | İrsaliye | DESPATCH | `/Sync/from-luca/despatch` | POST | `SyncDespatchFromLuca()` | `SyncDespatchFromLucaAsync()` | Luca→Katana |
| 5 | Tümü | ALL | `/Sync/run` | POST | `RunCompleteSync()` | `SyncAllAsync()` | Katana→Luca (ALL) |
| 6 | Stok Kartları (Luca) | STOCK_CARD | `/Sync/to-luca/stock-cards` | POST | `SyncProductsToLuca()` | `SyncProductsToLucaAsync()` | Katana→Luca |
| 7 | Tedarikçi Kartları (Koza) | SUPPLIER | `/Sync/suppliers` | POST | `SyncSuppliers()` | `SyncSuppliersToKozaAsync()` | Katana→Koza |
| 8 | Depo Kartları (Koza) | WAREHOUSE | `/Sync/warehouses` | POST | `SyncWarehouses()` | `SyncWarehousesToKozaAsync()` | Katana→Koza |
| 9 | Müşteri Kartları (Luca Cari) | CUSTOMER_LUCA | `/Sync/customers-luca` | POST | `SyncCustomersLuca()` | `SyncCustomersToLucaAsync()` | Katana→Luca |

---

## 2. SERVICE METHOD'LARI VE LUCA API ÇAĞRILARI

### SyncService.cs - Katana→Luca Operasyonları

```csharp
// 1. STOCK SYNC (Stok Hareketleri)
public Task<SyncResultDto> SyncStockAsync(DateTime? fromDate = null)
  → ExtractProductsAsync() [Katana DB'den çek]
  → ToProductsAsync() [Transform]
  → LoadProductsAsync() [Katana'da kaydet]
  ✅ Luca'ya gitmez (Katana internal)

// 2. INVOICE SYNC (Fatura)
public Task<SyncResultDto> SyncInvoicesAsync(DateTime? fromDate = null)
  → ExtractInvoicesAsync() [Katana DB'den çek]
  → ToInvoicesAsync() [Transform]
  → LoadInvoicesAsync() [Katana'da kaydet]
  ✅ Luca'ya gitmez (Katana internal)

// 3. CUSTOMER SYNC (Müşteri)
public Task<SyncResultDto> SyncCustomersAsync(DateTime? fromDate = null)
  → ExtractCustomersAsync() [Katana DB'den çek]
  → ToCustomersAsync() [Transform]
  → LoadCustomersAsync() [Katana'da kaydet]
  ✅ Luca'ya gitmez (Katana internal)

// 4. PRODUCT STOCK CARD SYNC (Stok Kartları → Luca)
public Task<SyncResultDto> SyncProductsToLucaAsync(SyncOptionsDto options)
  → ExtractProductsAsync() [Katana DB'den çek]
  → ToProductsAsync() [Transform]
  → _loaderService.LoadProductsToLucaAsync()
    → _lucaService.SendStockCardsAsync() ✅ LUCA API ÇAĞRISI
      → POST {LucaBaseUrl}/api/StokKarti/Ekle
      → HttpClient ile gerçek HTTP request

// 5. SUPPLIER SYNC (Tedarikçi → Koza)
public Task<SyncResultDto> SyncSuppliersToKozaAsync()
  → _lucaService.SendSuppliersAsync() ✅ KOZA API ÇAĞRISI
    → POST {KozaBaseUrl}/api/Cari/Ekle
    → HttpClient ile gerçek HTTP request

// 6. WAREHOUSE SYNC (Depo → Koza)
public Task<SyncResultDto> SyncWarehousesToKozaAsync()
  → _lucaService.SendWarehousesAsync() ✅ KOZA API ÇAĞRISI
    → POST {KozaBaseUrl}/api/Depo/Ekle
    → HttpClient ile gerçek HTTP request

// 7. CUSTOMER_LUCA SYNC (Müşteri → Luca Cari)
public Task<SyncResultDto> SyncCustomersToLucaAsync()
  → _lucaService.SendCustomersAsync() ✅ LUCA API ÇAĞRISI
    → POST {LucaBaseUrl}/api/Cari/Ekle
    → HttpClient ile gerçek HTTP request
```

### SyncService.cs - Luca→Katana Operasyonları

```csharp
// 1. DESPATCH SYNC (İrsaliye)
public Task<SyncResultDto> SyncDespatchFromLucaAsync(DateTime? fromDate = null)
  → _lucaService.GetDespatchesAsync() ✅ LUCA API ÇAĞRISI
    → GET {LucaBaseUrl}/api/Irsaliye/List
    → HttpClient ile gerçek HTTP request
  → Transform ve Katana DB'ye kaydet

// 2. STOCK FROM LUCA
public Task<SyncResultDto> SyncStockFromLucaAsync(DateTime? fromDate = null)
  → _lucaService.GetStockAsync() ✅ LUCA API ÇAĞRISI
    → GET {LucaBaseUrl}/api/Stok/List
    → HttpClient ile gerçek HTTP request
  → Transform ve Katana DB'ye kaydet

// 3. INVOICE FROM LUCA
public Task<SyncResultDto> SyncInvoicesFromLucaAsync(DateTime? fromDate = null)
  → _lucaService.GetInvoicesAsync() ✅ LUCA API ÇAĞRISI
    → GET {LucaBaseUrl}/api/Fatura/List
    → HttpClient ile gerçek HTTP request
  → Transform ve Katana DB'ye kaydet

// 4. CUSTOMER FROM LUCA
public Task<SyncResultDto> SyncCustomersFromLucaAsync(DateTime? fromDate = null)
  → _lucaService.GetCustomersAsync() ✅ LUCA API ÇAĞRISI
    → GET {LucaBaseUrl}/api/Cari/List
    → HttpClient ile gerçek HTTP request
  → Transform ve Katana DB'ye kaydet
```

---

## 3. LUCA API ÇAĞRILARI - GERÇEK HTTP REQUESTS

### LucaService.Operations.cs - Send Method'ları

```csharp
// 1. SendStockCardsAsync(List<LucaCreateStokKartiRequest> stockCards)
public async Task<SyncResultDto> SendStockCardsAsync(...)
{
  await EnsureAuthenticatedAsync();      // ✅ Luca'ya login
  await EnsureBranchSelectedAsync();     // ✅ Şube seç
  await VerifyBranchSelectionAsync();    // ✅ Şube doğrula
  
  var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
  var endpoint = _settings.Endpoints.StockCardCreate;  // POST /api/StokKarti/Ekle
  
  foreach (var card in stockCards)
  {
    var existingSkartId = await FindStockCardBySkuAsync(card.KartKodu);  // ✅ Luca'dan kontrol
    if (existingSkartId.HasValue)
    {
      // Zaten var - atla
      skippedCount++;
    }
    else
    {
      // Yeni oluştur
      var response = await client.PostAsync(endpoint, content);  // ✅ GERÇEK HTTP POST
      successCount++;
    }
  }
  
  return new SyncResultDto { SuccessfulRecords = successCount, ... };
}

// 2. SendCustomersAsync(List<LucaCreateCustomerRequest> customers)
public async Task<SyncResultDto> SendCustomersAsync(...)
{
  await EnsureAuthenticatedAsync();
  await EnsureBranchSelectedAsync();
  
  var endpoint = _settings.Endpoints.CustomerCreate;  // POST /api/Cari/Ekle
  
  foreach (var customer in customers)
  {
    var response = await client.PostAsync(endpoint, content);  // ✅ GERÇEK HTTP POST
  }
}

// 3. SendInvoicesAsync(List<LucaCreateInvoiceHeaderRequest> invoices)
public async Task<SyncResultDto> SendInvoicesAsync(...)
{
  await EnsureAuthenticatedAsync();
  await EnsureBranchSelectedAsync();
  
  var endpoint = _settings.Endpoints.InvoiceCreate;  // POST /api/Fatura/Ekle
  
  foreach (var invoice in invoices)
  {
    var response = await client.PostAsync(endpoint, content);  // ✅ GERÇEK HTTP POST
  }
}

// 4. SendStockMovementsAsync(List<LucaStockDto> stockMovements)
public async Task<SyncResultDto> SendStockMovementsAsync(...)
{
  await EnsureAuthenticatedAsync();
  await EnsureBranchSelectedAsync();
  
  var endpoint = _settings.Endpoints.StockMovementCreate;  // POST /api/DepoTransferi/Ekle
  
  foreach (var movement in stockMovements)
  {
    var response = await client.PostAsync(endpoint, content);  // ✅ GERÇEK HTTP POST
  }
}
```

### LucaService.Supplier.cs - Koza Supplier Sync

```csharp
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

## 4. DB: SYNC LOG'LARI

### Tablo: SyncLogs (SyncOperationLog Entity)

```sql
CREATE TABLE SyncLogs (
    Id INT PRIMARY KEY IDENTITY(1,1),
    SyncType NVARCHAR(50) NOT NULL,           -- "STOCK", "INVOICE", "CUSTOMER", "SUPPLIER", vb.
    Status NVARCHAR(50) NOT NULL,             -- "PENDING", "RUNNING", "SUCCESS", "FAILED", "PARTIAL"
    ErrorMessage NVARCHAR(MAX),
    ProcessedRecords INT,                     -- İşlenen toplam kayıt sayısı
    SuccessfulRecords INT,                    -- Başarılı kayıt sayısı
    FailedRecords INT,                        -- Başarısız kayıt sayısı
    StartTime DATETIME NOT NULL,
    EndTime DATETIME,
    TriggeredBy NVARCHAR(100),
    Details NVARCHAR(MAX),
    
    INDEX IX_SyncType_StartTime (SyncType, StartTime),
    INDEX IX_Status (Status)
);
```

### Tablo: IntegrationLogs (Alternatif - Daha detaylı)

```sql
CREATE TABLE IntegrationLogs (
    Id INT PRIMARY KEY IDENTITY(1,1),
    SyncType NVARCHAR(50) NOT NULL,
    Status INT,                               -- SyncStatus enum (0=Pending, 1=Running, 2=Success, 3=Failed, 4=Partial)
    Source INT,                               -- DataSource enum (0=Katana, 1=Luca, 2=Koza)
    StartTime DATETIME NOT NULL,
    EndTime DATETIME,
    ProcessedRecords INT,
    SuccessfulRecords INT,
    FailedRecordsCount INT,                   -- ⚠️ NOT: "FailedRecords" değil "FailedRecordsCount"
    ErrorMessage NVARCHAR(2000),
    TriggeredBy NVARCHAR(100),
    Details NVARCHAR(MAX),
    
    INDEX IX_SyncType_StartTime (SyncType, StartTime),
    INDEX IX_Status (Status)
);
```

---

## 5. LOG OLUŞTURMA AKIŞI

### SyncService.cs - ExecuteSyncAsync() Method

```csharp
private async Task<SyncResultDto> ExecuteSyncAsync(
    string syncType, 
    Func<CancellationToken, Task<SyncResultDto>> syncOperation)
{
    var stopwatch = Stopwatch.StartNew();
    
    // 1️⃣ LOG BAŞLAT
    var logEntry = await StartOperationLogAsync(syncType);
    // → INSERT INTO SyncLogs (SyncType, Status, StartTime) VALUES ('STOCK', 'RUNNING', NOW())
    
    try
    {
        // 2️⃣ SYNC OPERASYONU ÇALIŞTIR
        var result = await syncOperation(cts.Token);
        
        // 3️⃣ LOG SONLANDIR (SUCCESS)
        await FinalizeOperationAsync(
            logEntry,
            "SUCCESS",
            result.ProcessedRecords,
            result.SuccessfulRecords,
            result.FailedRecords,
            null
        );
        // → UPDATE SyncLogs SET Status='SUCCESS', ProcessedRecords=100, SuccessfulRecords=95, FailedRecords=5, EndTime=NOW()
    }
    catch (Exception ex)
    {
        // 4️⃣ LOG SONLANDIR (FAILED)
        await FinalizeOperationAsync(
            logEntry,
            "FAILED",
            0,
            0,
            0,
            ex.Message
        );
        // → UPDATE SyncLogs SET Status='FAILED', ErrorMessage='...', EndTime=NOW()
    }
}
```

### FinalizeOperationAsync() Method

```csharp
private async Task FinalizeOperationAsync(
    SyncOperationLog log,
    string status,
    int processed,
    int successful,
    int failed,
    string? errorMessage)
{
    log.Status = status;
    log.ProcessedRecords = processed;
    log.SuccessfulRecords = successful;
    log.FailedRecords = failed;
    log.EndTime = DateTime.UtcNow;
    log.ErrorMessage = Truncate(errorMessage, 2000);
    
    await _dbContext.SaveChangesAsync();
    // → UPDATE SyncLogs SET Status=?, ProcessedRecords=?, SuccessfulRecords=?, FailedRecords=?, EndTime=?, ErrorMessage=?
}
```

---

## 6. UI'DE GÖRÜNEN SAYILAR NEREDEN GELİYOR?

### SyncManagement.tsx - Sync History

```typescript
// Frontend'de sync history yükleme:
const loadHistory = async () => {
  const data = await stockAPI.getSyncHistory();
  // → GET /api/Sync/history
};

// Backend'de:
[HttpGet("history")]
public async Task<IActionResult> GetSyncHistory()
{
    var logs = await _context.SyncOperationLogs
        .OrderByDescending(l => l.StartTime)
        .Take(50)
        .ToListAsync();
    
    // SyncLogs tablosundan son 50 kaydı çek
    // Her kayıt: { id, syncType, status, startTime, endTime, processedRecords, successfulRecords, failedRecords, errorMessage }
    
    return Ok(result);
}
```

### UI'de Görünen Sayılar

```
┌─────────────────────────────────────────────────────────────┐
│ Senkronizasyon Yönetimi                                     │
├─────────────────────────────────────────────────────────────┤
│ Sync Type    │ Status   │ Processed │ Successful │ Failed   │
├──────────────┼──────────┼───────────┼────────────┼──────────┤
│ STOCK        │ SUCCESS  │ 100       │ 95         │ 5        │ ← SyncLogs.ProcessedRecords, SuccessfulRecords, FailedRecords
│ INVOICE      │ FAILED   │ 50        │ 30         │ 20       │
│ CUSTOMER     │ RUNNING  │ 0         │ 0          │ 0        │
└─────────────────────────────────────────────────────────────┘

Kaynak: SyncLogs tablosu
  - ProcessedRecords: Toplam işlenen kayıt sayısı
  - SuccessfulRecords: Başarılı kayıt sayısı
  - FailedRecords: Başarısız kayıt sayısı
```

---

## 7. SQL SORGUSU - SYNC LOG'LARINI KONTROL ETME

### Son 50 Sync Kaydı

```sql
SELECT TOP 50 
    Id,
    SyncType,
    Status,
    ProcessedRecords,
    SuccessfulRecords,
    FailedRecords,
    StartTime,
    EndTime,
    DATEDIFF(SECOND, StartTime, EndTime) AS DurationSeconds,
    ErrorMessage,
    TriggeredBy
FROM SyncLogs
ORDER BY StartTime DESC;
```

### Sync Type Bazlı Son Durum

```sql
SELECT 
    SyncType,
    COUNT(*) AS TotalRuns,
    SUM(CASE WHEN Status = 'SUCCESS' THEN 1 ELSE 0 END) AS SuccessfulRuns,
    SUM(CASE WHEN Status = 'FAILED' THEN 1 ELSE 0 END) AS FailedRuns,
    MAX(StartTime) AS LastRun,
    SUM(ProcessedRecords) AS TotalProcessed,
    SUM(SuccessfulRecords) AS TotalSuccessful,
    SUM(FailedRecords) AS TotalFailed
FROM SyncLogs
GROUP BY SyncType
ORDER BY MAX(StartTime) DESC;
```

### Başarısız Sync'ler

```sql
SELECT TOP 50
    Id,
    SyncType,
    Status,
    ProcessedRecords,
    SuccessfulRecords,
    FailedRecords,
    StartTime,
    ErrorMessage
FROM SyncLogs
WHERE Status IN ('FAILED', 'PARTIAL')
ORDER BY StartTime DESC;
```

### Günlük Sync İstatistikleri

```sql
SELECT 
    CAST(StartTime AS DATE) AS SyncDate,
    SyncType,
    COUNT(*) AS RunCount,
    SUM(ProcessedRecords) AS TotalProcessed,
    SUM(SuccessfulRecords) AS TotalSuccessful,
    SUM(FailedRecords) AS TotalFailed,
    CAST(SUM(SuccessfulRecords) * 100.0 / NULLIF(SUM(ProcessedRecords), 0) AS DECIMAL(5,2)) AS SuccessRate
FROM SyncLogs
WHERE StartTime >= DATEADD(DAY, -7, CAST(GETDATE() AS DATE))
GROUP BY CAST(StartTime AS DATE), SyncType
ORDER BY SyncDate DESC, SyncType;
```

---

## 8. ÖZET

### ✅ Çalışan Sync'ler (Gerçek HTTP Çağrısı Yapan)

1. **STOCK_CARD** → `SendStockCardsAsync()` → POST /api/StokKarti/Ekle (Luca)
2. **SUPPLIER** → `SendSuppliersAsync()` → POST /api/Cari/Ekle (Koza)
3. **WAREHOUSE** → `SendWarehousesAsync()` → POST /api/Depo/Ekle (Koza)
4. **CUSTOMER_LUCA** → `SendCustomersAsync()` → POST /api/Cari/Ekle (Luca)
5. **DESPATCH** → `GetDespatchesAsync()` → GET /api/Irsaliye/List (Luca)

### ⚠️ Katana Internal Sync'ler (Luca'ya Gitmez)

1. **STOCK** → `SyncStockAsync()` → Katana DB'de işlem
2. **INVOICE** → `SyncInvoicesAsync()` → Katana DB'de işlem
3. **CUSTOMER** → `SyncCustomersAsync()` → Katana DB'de işlem

### 📊 DB Log'ları

- **Tablo**: `SyncLogs` (SyncOperationLog entity)
- **Sayılar**: `ProcessedRecords`, `SuccessfulRecords`, `FailedRecords`
- **Güncelleme**: `FinalizeOperationAsync()` method'u tarafından
- **Kaynak**: Her sync operasyonu başında log oluşturulur, sonunda güncellenir

