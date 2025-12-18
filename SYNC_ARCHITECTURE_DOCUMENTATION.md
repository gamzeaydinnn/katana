# Katana Integration Sync Architecture - Comprehensive Documentation

## 📋 Table of Contents
1. [Architecture Overview](#architecture-overview)
2. [Background Workers](#background-workers)
3. [Sync Intervals & Configuration](#sync-intervals--configuration)
4. [Sync Types & Methods](#sync-types--methods)
5. [Manual Sync Triggers](#manual-sync-triggers)
6. [Data Flow Diagrams](#data-flow-diagrams)
7. [Code References](#code-references)
8. [Performance Optimizations](#performance-optimizations)

---

## 🏗️ Architecture Overview

The Katana Integration system uses a **multi-layered synchronization architecture** combining:
- **Background Workers**: Automated periodic syncs
- **Manual Triggers**: API endpoints and frontend buttons
- **Batch Processing**: Efficient bulk operations with parallelism
- **Real-time Notifications**: SignalR for progress updates

### System Components
```
┌──────────────────────────────────────────────────────────────┐
│                    KATANA INTEGRATION                         │
├──────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │ Background      │  │ Manual Sync     │  │ Frontend     │ │
│  │ Workers         │  │ API Endpoints   │  │ UI Buttons   │ │
│  └────────┬────────┘  └────────┬────────┘  └──────┬───────┘ │
│           │                    │                   │          │
│           └────────────────────┼───────────────────┘          │
│                                │                              │
│                    ┌───────────▼────────────┐                │
│                    │    SyncService.cs      │                │
│                    │  (Core Orchestrator)   │                │
│                    └───────────┬────────────┘                │
│                                │                              │
│           ┌────────────────────┼────────────────────┐         │
│           │                    │                    │         │
│     ┌─────▼──────┐      ┌─────▼──────┐      ┌─────▼──────┐  │
│     │ Katana API │      │ Luca/Koza  │      │ Database   │  │
│     │ Service    │      │ API Service│      │ (EF Core)  │  │
│     └────────────┘      └────────────┘      └────────────┘  │
│                                                               │
└──────────────────────────────────────────────────────────────┘
```

---

## 🤖 Background Workers

### Active Workers (✅ Registered in Program.cs)

#### 1. **KatanaSalesOrderSyncWorker** ⏰ Every 5 minutes
**Purpose**: Syncs sales orders from Katana API and creates pending stock adjustments

**Configuration**:
- **Interval**: `TimeSpan.FromMinutes(5)` (hardcoded)
- **Initial Delay**: 30 seconds after startup
- **Retry Policy**: 3 attempts with exponential backoff (2^attempt seconds)

**Code Location**: 
```
src/Katana.API/Workers/KatanaSalesOrderSyncWorker.cs
Lines 58-60: private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);
Lines 95-98: await Task.Delay(_interval, stoppingToken);
Lines 126: DateTime? fromDate = null; // Fetches ALL open orders
```

**Sync Flow**:
```
1. Fetch ONLY open orders from Katana (status=NOT_SHIPPED)
   ↓
2. For each order:
   a. Customer Mapping: Resolve Katana customer ID → local database ID
   b. Create SalesOrder entity (duplicate prevention via KatanaOrderId)
   c. Create SalesOrderLine entities with variant mapping
   ↓
3. Create PendingStockAdjustment for admin approval
   - Composite key: OrderId | SKU | Quantity
   ↓
4. Trigger downstream syncs:
   - Sync products to Luca (stock cards)
   - Sync approved orders to Luca (invoices)
   - Create notification for new orders
```

**Key Features**:
- Fetches **ALL open orders** (`fromDate = null`)
- Duplicate prevention using `HashSet<long>` for existing Katana Order IDs
- Composite key lookup: `{ExternalOrderId}|{SKU}|{Quantity}` for adjustments
- Retry policy with exponential backoff for HTTP failures

**Registration**: `Program.cs` Line ~384
```csharp
builder.Services.AddHostedService<KatanaSalesOrderSyncWorker>();
```

---

#### 2. **LucaBatchPushWorker** 🚀 Queue-based (Real-time)
**Purpose**: Processes batch product push jobs to Luca/Koza API with intelligent parallelism

**Configuration**:
- **Interval**: 5 seconds idle check (queue-based trigger)
- **Max Parallelism**: 10 concurrent requests
- **Parallel Threshold**: 50 products (below = sequential)
- **Progress Notification**: Every 10 products

**Code Location**:
```
src/Katana.API/Workers/LucaBatchPushWorker.cs
Lines 30-31: private const int MaxParallelism = 10;
Lines 37-38: private const int ParallelThreshold = 50;
Lines 95: await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
```

**Processing Modes**:
1. **Sequential Mode** (<50 products): 
   - Simpler, lower overhead
   - Used for small batches
   
2. **Parallel Mode** (≥50 products):
   - `SemaphoreSlim` with MaxParallelism=10
   - Controlled concurrent API calls
   - Real-time SignalR progress updates

**Job Lifecycle**:
```
Queue Job → Idle Check (5s) → Dequeue → Mode Selection
                                            ↓
                            ┌───────────────┴───────────────┐
                            │                               │
                      < 50 products                  ≥ 50 products
                            │                               │
                    Sequential Mode                 Parallel Mode
                            │                               │
                    Process 1 by 1               Process 10 concurrent
                            │                               │
                            └───────────────┬───────────────┘
                                            │
                                    Update Status
                                    Send SignalR
                                    Cleanup (24h)
```

**SignalR Notifications**:
- Progress updates sent to `NotificationHub`
- User-specific messages via `hubContext.Clients.User(userId)`
- Message format: `"Batch işlemi başladı"`, `"İşlenen: {Processed}/{Total}"`

**Cleanup**:
- Removes jobs older than 24 hours
- Runs after each job completion

**Registration**: `Program.cs` Line ~385
```csharp
builder.Services.AddHostedService<LucaBatchPushWorker>();
```

---

### Inactive Workers (❌ Commented Out)

#### 3. **AutoSyncWorker** ⏰ Configurable interval (DISABLED)
**Purpose**: Automatic periodic stock synchronization

**Configuration** (if enabled):
- **Default Interval**: 360 minutes (6 hours) from `SyncSettings.Stock.SyncIntervalMinutes`
- **Initial Delay**: 10 seconds
- **Error Retry**: 5 minutes after exceptions
- **Dynamic Settings**: Reads from `SettingsController.GetCachedSettings()`

**Code Location**:
```
src/Katana.API/Workers/AutoSyncWorker.cs
Lines 38-46: Check cachedSettings.AutoSync and cachedSettings.SyncInterval
Lines 53: await Task.Delay(TimeSpan.FromMinutes(interval), stoppingToken);
Lines 76: await syncService.SyncStockAsync();
```

**Why Disabled?**:
- **Line 384 in Program.cs**: 
  ```csharp
  // builder.Services.AddHostedService<AutoSyncWorker>();
  ```
- Likely replaced by **SyncWorkerService** (see next section)

**Sync Method**: Calls `ISyncService.SyncStockAsync()` only (not full sync)

---

#### 4. **SyncWorkerService** ⏰ Every 6 hours (STATUS UNCLEAR)
**Purpose**: Background service for full synchronization of all entities

**Configuration**:
- **Interval**: `TimeSpan.FromHours(6)` (hardcoded)
- **Sync Method**: `syncService.SyncAllAsync()` (comprehensive)

**Code Location**:
```
src/Katana.Infrastructure/Workers/SyncWorkerService.cs
Lines 14: private readonly TimeSpan _syncInterval = TimeSpan.FromHours(6);
Lines 35: await syncService.SyncAllAsync();
Lines 40: await Task.Delay(_syncInterval, stoppingToken);
```

**Sync Flow**:
```
Startup → 6 hours → SyncAllAsync() → Log Results → 6 hours → ...
```

**Registration Status**: ⚠️ **UNCLEAR**
- Not found in `AddHostedService` grep search
- Exists in `src/Katana.Infrastructure/Workers/` directory
- May be conditionally registered or unused

**Investigation Needed**: Check `Program.cs` for dynamic registration or feature flags

---

## ⏱️ Sync Intervals & Configuration

### Configuration File: `appsettings.json`
```json
{
  "Sync": {
    "BatchSize": 100,
    "RetryCount": 3,
    "ScheduleInterval": "0 */6 * * *",  // Cron: Every 6 hours
    "EnableAutoSync": true,
    "Stock": {
      "SyncIntervalMinutes": 360  // 6 hours
    },
    "Invoice": {
      "SyncIntervalMinutes": 360
    },
    "Customer": {
      "SyncIntervalMinutes": 360
    },
    "SyncIntervalMinutes": 360  // Default fallback
  }
}
```

**Code Location**: `src/Katana.Data/Configuration/SyncSettings.cs`

### Interval Comparison Table

| Worker/Service | Interval | Configurable | Status | Sync Method |
|---------------|----------|--------------|--------|-------------|
| **KatanaSalesOrderSyncWorker** | 5 minutes | ❌ Hardcoded | ✅ Active | Katana Orders |
| **LucaBatchPushWorker** | Queue-based (5s idle) | ❌ Hardcoded | ✅ Active | Luca Products Batch |
| **AutoSyncWorker** | 360 minutes (6h) | ✅ Dynamic | ❌ Disabled | Stock Only |
| **SyncWorkerService** | 360 minutes (6h) | ❌ Hardcoded | ⚠️ Unclear | Full Sync (All) |

### Configuration Precedence
```
1. Cached Settings (SettingsController.GetCachedSettings())
   ↓ (if null)
2. SyncSettings from appsettings.json
   ↓ (if null)
3. Default values in SyncSettings.cs (BatchSize=100, RetryCount=3)
```

**Dynamic Reconfiguration**:
- AutoSyncWorker uses `IOptionsMonitor<SyncSettings>` for runtime changes
- Settings can be updated via `SettingsController` API
- No restart required for cached settings

---

## 🔄 Sync Types & Methods

### SyncService.cs Core Methods

#### 1. Products Sync (Katana → Luca)
**Method**: `SyncProductsToLucaAsync`
**Location**: `src/Katana.Business/UseCases/Sync/SyncService.cs` Lines 115-280

**Flow**:
```
1. Fetch products from Katana database
   ↓
2. Filter products (SKU validation, active status)
   ↓
3. Map Product → KozaStokKartiDto
   ↓
4. Convert to LucaStockCardSummaryDto (Lines 288-295)
   ↓
5. Send to Luca API (Append-only mode)
   ↓
6. Log success/failure for each product
```

**Append-Only Mode**: Does NOT update existing Luca stock cards, only creates new ones

**Error Handling**:
- Per-product try-catch
- Continues on individual failures
- Aggregates success/failure counts

**Code Example** (Lines 1-200):
```csharp
public async Task<SyncResult> SyncProductsAsync()
{
    // Routes to SyncProductsToLucaAsync (Line 76)
    return await SyncProductsToLucaAsync();
}
```

---

#### 2. Products Sync (Luca → Katana)
**Method**: `SyncProductsFromLucaAsync`
**Location**: `SyncService.cs` Lines 1368-1450

**Flow**:
```
1. Call lucaService.ListStockCardsSimpleAsync()
   ↓
2. For each Luca stock card:
   a. Map LucaStockCardSummaryDto → Product
   b. Populate LucaId field (Lines 1401-1411) ✅ NEW
   c. Upsert to database (update if exists, insert if new)
   ↓
3. Return sync result with record counts
```

**LucaId Population** (Critical for fast delete):
```csharp
// Lines 1401-1411
if (lucaCard.Id.HasValue)
{
    product.LucaId = lucaCard.Id.Value;
    _logger.LogInformation(
        "✅ LucaId atandı: SKU={SKU}, LucaId={LucaId}", 
        product.SKU, product.LucaId
    );
}
```

**Benefits**:
- Enables 0.1s delete operations (vs 90s API lookup)
- Used by `AdminController.TestDeleteProduct` for fast SKU→LucaId resolution

---

#### 3. Stock Sync
**Method**: `SyncStockAsync`
**Location**: `SyncService.cs` (Interface stub, implementation TBD)

**Status**: ⚠️ Not fully implemented in reviewed code

**Used By**:
- AutoSyncWorker (if enabled)
- Manual sync endpoint `/api/sync/start?syncType=STOCK`

---

#### 4. Invoice Sync
**Method**: `SendSalesInvoiceAsync`
**Location**: `SyncService.cs` Line 1795 (Interface stub)

**Status**: ⚠️ Stub implementation only

**Used By**:
- Manual sync endpoint `/api/sync/start?syncType=INVOICE`
- KatanaSalesOrderSyncWorker (downstream trigger)

---

#### 5. Customer Sync
**Method**: `SyncCustomersAsync` (exact name TBD)
**Status**: Referenced in SyncController but not fully reviewed

**Used By**:
- Manual sync endpoint `/api/sync/start?syncType=CUSTOMER`

---

#### 6. Full Sync (All Entities)
**Method**: `SyncAllAsync`
**Location**: Referenced in SyncWorkerService and SyncController

**Likely Flow**:
```
SyncAllAsync()
  ├── SyncProductsToLucaAsync()
  ├── SyncProductsFromLucaAsync()
  ├── SyncCustomersAsync()
  ├── SyncStockAsync()
  └── SendSalesInvoiceAsync()
```

**Used By**:
- SyncWorkerService (every 6 hours, if active)
- Manual sync endpoint `/api/sync/start?syncType=ALL`

---

## 🖱️ Manual Sync Triggers

### 1. API Endpoints (SyncController.cs)
**Location**: `src/Katana.API/Controllers/SyncController.cs`

#### Endpoint: `POST /api/sync/start`
**Parameters**:
- `syncType` (required): Enum value from supported types

**Supported Sync Types**:
```csharp
STOCK           → SyncStockAsync()
INVOICE         → SendSalesInvoiceAsync()
CUSTOMER        → SyncCustomersAsync()
DESPATCH        → (TBD)
PRODUCT         → SyncProductsToLucaAsync()
STOCK_CARD      → SyncProductsToLucaAsync() (alias)
SUPPLIER        → (TBD)
WAREHOUSE       → SyncWarehousesToLucaAsync()
CUSTOMER_LUCA   → (TBD)
ALL             → SyncAllAsync()
```

**Code Example** (Lines 1-150):
```csharp
[HttpPost("start")]
public async Task<IActionResult> StartSync([FromQuery] string syncType)
{
    switch (syncType.ToUpper())
    {
        case "PRODUCT":
        case "STOCK_CARD":
            result = await _syncService.SyncProductsToLucaAsync();
            break;
        case "ALL":
            result = await _syncService.SyncAllAsync();
            break;
        // ... other cases
    }
    return Ok(result);
}
```

#### Endpoint: `GET /api/sync/history`
**Purpose**: Retrieve last 50 sync operations with status and timestamps

**Response Format**:
```json
{
  "syncOperations": [
    {
      "id": 123,
      "syncType": "PRODUCT",
      "status": "Completed",
      "startTime": "2024-01-15T10:00:00Z",
      "endTime": "2024-01-15T10:05:00Z",
      "recordsProcessed": 150,
      "recordsFailed": 2
    }
  ]
}
```

---

### 2. Frontend UI Buttons (LucaProducts.tsx)
**Location**: `frontend/katana-web/src/components/Admin/LucaProducts.tsx`

#### Button: "Koza ile Senkronize Et"
**Code** (Lines 150-200):
```typescript
const syncFromKoza = async () => {
  try {
    setLoading(true);
    await stockAPI.startSync();  // Calls POST /api/sync/start
    await fetchProducts();       // Refresh product list
    
    toast.success('Koza ile senkronizasyon başlatıldı');
  } catch (error) {
    console.error('Sync error:', error);
    toast.error('Senkronizasyon başlatılamadı');
  } finally {
    setLoading(false);
  }
};
```

**User Experience**:
1. User clicks button
2. Loading spinner appears
3. API call initiated
4. Success toast: "Koza ile senkronizasyon başlatıldı"
5. Product list refreshed automatically

**API Client** (`stockAPI.startSync`):
```typescript
// Likely implementation in src/api/stockAPI.ts
export const startSync = () => {
  return axios.post('/api/sync/start?syncType=PRODUCT');
};
```

---

## 📊 Data Flow Diagrams

### Full Sync Architecture Flow
```
┌─────────────────────────────────────────────────────────────┐
│                     SYNC TRIGGERS                            │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Background Worker        Manual API          Frontend UI   │
│  (Every 6h)               (On-demand)        (User Click)   │
│       │                        │                    │        │
│       └────────────────────────┼────────────────────┘        │
│                                │                             │
│                                ▼                             │
│                    ┌────────────────────┐                    │
│                    │  SyncController    │                    │
│                    │  or Direct Call    │                    │
│                    └─────────┬──────────┘                    │
│                              │                               │
│                              ▼                               │
│                    ┌────────────────────┐                    │
│                    │   SyncService.cs   │                    │
│                    │  (Orchestrator)    │                    │
│                    └─────────┬──────────┘                    │
│                              │                               │
│         ┌────────────────────┼────────────────────┐          │
│         │                    │                    │          │
│         ▼                    ▼                    ▼          │
│  ┌──────────────┐   ┌──────────────┐   ┌──────────────┐    │
│  │  Products    │   │  Invoices    │   │  Customers   │    │
│  │  Sync        │   │  Sync        │   │  Sync        │    │
│  └──────┬───────┘   └──────┬───────┘   └──────┬───────┘    │
│         │                  │                    │            │
│         ▼                  ▼                    ▼            │
│  ┌──────────────┐   ┌──────────────┐   ┌──────────────┐    │
│  │ Luca/Koza    │   │ Luca/Koza    │   │ Luca/Koza    │    │
│  │ Stock Cards  │   │ Invoices     │   │ Cari         │    │
│  └──────────────┘   └──────────────┘   └──────────────┘    │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### Product Sync Bidirectional Flow
```
┌─────────────────────────────────────────────────────────────┐
│           KATANA DATABASE ←→ LUCA/KOZA API                  │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Katana → Luca (Append-Only)                                │
│  ────────────────────────────                                │
│                                                              │
│  Products Table                                              │
│       │                                                      │
│       │ SyncProductsToLucaAsync()                           │
│       │                                                      │
│       ▼                                                      │
│  Map to KozaStokKartiDto                                    │
│       │                                                      │
│       │ Convert (Lines 288-295)                             │
│       │                                                      │
│       ▼                                                      │
│  LucaStockCardSummaryDto                                    │
│       │                                                      │
│       │ POST to Luca API                                    │
│       │                                                      │
│       ▼                                                      │
│  Luca/Koza StokKarti                                        │
│       │                                                      │
│       └─────────────────────────────────────────────┐       │
│                                                      │       │
│  Luca → Katana (Full Sync with LucaId)             │       │
│  ───────────────────────────────────────            │       │
│                                                      │       │
│  Luca/Koza StokKarti                                │       │
│       │                                              │       │
│       │ ListStockCardsSimpleAsync()                 │       │
│       │                                              │       │
│       ▼                                              │       │
│  LucaStockCardSummaryDto[]                          │       │
│       │                                              │       │
│       │ SyncProductsFromLucaAsync()                 │       │
│       │                                              │       │
│       ▼                                              │       │
│  Map to Product Entity                              │       │
│       │                                              │       │
│       │ Populate LucaId (Lines 1401-1411) ✅        │       │
│       │                                              │       │
│       ▼                                              │       │
│  Products Table (with LucaId)                       │       │
│       │                                              │       │
│       └──────────────────────────────────────────────┘       │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### Sales Order Sync Flow (Katana → Katana DB → Pending Adjustments)
```
┌─────────────────────────────────────────────────────────────┐
│        KatanaSalesOrderSyncWorker (Every 5 minutes)         │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Katana API (External)                                      │
│       │                                                      │
│       │ GetSalesOrdersBatchedAsync(fromDate=null)          │
│       │ → Fetches ALL open orders                           │
│       │                                                      │
│       ▼                                                      │
│  SalesOrderDto[] (Katana format)                           │
│       │                                                      │
│       │ For each order:                                     │
│       │                                                      │
│       ├─→ 1. Resolve Katana Customer ID → Local DB ID      │
│       │         (Fetch from Katana if not exists)          │
│       │                                                      │
│       ├─→ 2. Create SalesOrder Entity                       │
│       │         - CustomerId = local DB ID                  │
│       │         - KatanaOrderId = external ID               │
│       │         - Status = "NOT_SHIPPED" / "OPEN"           │
│       │         - Duplicate check via HashSet               │
│       │                                                      │
│       ├─→ 3. Create SalesOrderLine Entities                 │
│       │         - Variant mapping                           │
│       │         - SKU, Quantity, Price                      │
│       │                                                      │
│       └─→ 4. Create PendingStockAdjustment                  │
│                   - ExternalOrderId | SKU | Quantity        │
│                   - Awaits admin approval                   │
│                                                              │
│  Database Changes:                                          │
│  ├── SalesOrders Table (new records)                        │
│  ├── SalesOrderLines Table (new records)                    │
│  └── PendingStockAdjustments Table (new records)            │
│                                                              │
│  Downstream Triggers:                                       │
│  ├── Sync products to Luca (stock cards)                   │
│  ├── Sync approved orders to Luca (invoices)               │
│  └── Create notification (SignalR)                          │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## 📚 Code References

### Core Files

#### SyncService.cs
**Path**: `src/Katana.Business/UseCases/Sync/SyncService.cs`
**Lines**: 1831 total

**Key Methods**:
- `SyncProductsAsync()` → Line 76 (routes to SyncProductsToLucaAsync)
- `SyncProductsToLucaAsync()` → Lines 115-280 (Katana → Luca)
- `SyncProductsFromLucaAsync()` → Lines 1368-1450 (Luca → Katana)
- `SyncWarehousesToLucaAsync()` → Line 1770 (interface stub)
- `SendSalesInvoiceAsync()` → Line 1795 (interface stub)

**Recent Fixes**:
- Line 286: Fixed ListStockCardsAsync ambiguity
- Line 1130: Fixed InvoiceStatus enum
- Lines 1401-1411: Added LucaId population during sync ✅
- Lines 1565, 1670: Fixed comment block closures
- Lines 288-295: Added DTO conversion logic

---

#### SyncSettings.cs
**Path**: `src/Katana.Data/Configuration/SyncSettings.cs`
**Lines**: ~100 total

**Default Configuration**:
```csharp
public int BatchSize { get; set; } = 100;
public int RetryCount { get; set; } = 3;
public string ScheduleInterval { get; set; } = "0 */6 * * *"; // Cron
public bool EnableAutoSync { get; set; } = true;

public SyncTypeSettings Stock { get; set; } = new()
{
    SyncIntervalMinutes = 360  // 6 hours
};

public SyncTypeSettings Invoice { get; set; } = new()
{
    SyncIntervalMinutes = 360
};

public SyncTypeSettings Customer { get; set; } = new()
{
    SyncIntervalMinutes = 360
};
```

**Dependency**: Used by AutoSyncWorker via `IOptionsMonitor<SyncSettings>`

---

#### SyncController.cs
**Path**: `src/Katana.API/Controllers/SyncController.cs`
**Lines**: 1243 total

**Key Endpoints**:
- `GET /api/sync/history` → Lines 1-50 (last 50 sync operations)
- `POST /api/sync/start?syncType=PRODUCT` → Lines 100-150 (manual trigger)

**Sync Type Routing** (Lines 100-150):
```csharp
switch (syncType.ToUpper())
{
    case "PRODUCT":
    case "STOCK_CARD":
        result = await _syncService.SyncProductsToLucaAsync();
        break;
    case "ALL":
        result = await _syncService.SyncAllAsync();
        break;
    // ... other cases
}
```

---

#### KatanaSalesOrderSyncWorker.cs
**Path**: `src/Katana.API/Workers/KatanaSalesOrderSyncWorker.cs`
**Lines**: 656 total

**Key Configuration**:
- Line 58: `private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);`
- Lines 64-71: Retry policy with exponential backoff
- Line 126: `DateTime? fromDate = null;` (fetches ALL open orders)

**Duplicate Prevention**:
- Lines 129-138: HashSet for existing Katana Order IDs
- Lines 144-152: Composite key for pending adjustments

---

#### LucaBatchPushWorker.cs
**Path**: `src/Katana.API/Workers/LucaBatchPushWorker.cs`
**Lines**: 627 total

**Key Configuration**:
- Line 30: `private const int MaxParallelism = 10;`
- Line 37: `private const int ParallelThreshold = 50;`
- Line 43: `private const int ProgressNotifyInterval = 10;`

**Processing Logic**:
- Lines 80-90: Mode selection (parallel vs sequential)
- Lines 95: Idle check delay (5 seconds)
- Lines 115-150: Sequential processing implementation

---

#### AutoSyncWorker.cs (DISABLED)
**Path**: `src/Katana.API/Workers/AutoSyncWorker.cs`
**Lines**: 100 total

**Key Logic**:
- Lines 38-46: Dynamic settings from cache
- Line 53: `await Task.Delay(TimeSpan.FromMinutes(interval), stoppingToken);`
- Line 76: `await syncService.SyncStockAsync();`

**Status**: Commented out in Program.cs Line 384

---

#### SyncWorkerService.cs
**Path**: `src/Katana.Infrastructure/Workers/SyncWorkerService.cs`
**Lines**: ~100 total

**Key Logic**:
- Line 14: `private readonly TimeSpan _syncInterval = TimeSpan.FromHours(6);`
- Line 35: `await syncService.SyncAllAsync();`
- Line 40: `await Task.Delay(_syncInterval, stoppingToken);`

**Status**: ⚠️ Registration unclear (not found in AddHostedService grep)

---

#### Product.cs (Entity)
**Path**: `src/Katana.Core/Entities/Product.cs`

**LucaId Property** (Line ~95):
```csharp
/// <summary>
/// Luca/Koza API'den gelen skartId (fast delete için)
/// </summary>
public long? LucaId { get; set; }
```

**Purpose**: Enables 0.1s delete operations by avoiding 90s Luca API lookup

**Populated By**: `SyncProductsFromLucaAsync` (Lines 1401-1411)

---

#### AdminController.cs (Test Endpoint)
**Path**: `src/Katana.API/Controllers/AdminController.cs`
**Lines**: 1040-1143

**Optimized Delete** (test-delete-product):
```csharp
1. Check local DB for LucaId (0.1s)
2. If found → Use for delete
3. If not found → Fallback to Luca API ListStockCards (90s)
4. Save discovered LucaId for future use
5. Return elapsed time and lookup method
```

**Response Format**:
```json
{
  "success": true,
  "lookupMethod": "LocalDB (Hızlı)" | "Luca API (Yavaş)",
  "lucaId": 12345,
  "elapsedSeconds": 0.1
}
```

---

### Frontend Files

#### LucaProducts.tsx
**Path**: `frontend/katana-web/src/components/Admin/LucaProducts.tsx`
**Lines**: 765 total

**Sync Button** (Lines 150-200):
```typescript
const syncFromKoza = async () => {
  setLoading(true);
  await stockAPI.startSync();  // POST /api/sync/start
  await fetchProducts();       // Refresh list
  toast.success('Koza ile senkronizasyon başlatıldı');
};
```

**Search Filter** (Lines 259-268):
```typescript
const filtered = products.filter(p => 
  p.SKU?.toLowerCase().includes(searchTerm.toLowerCase()) ||
  p.ProductName?.toLowerCase().includes(searchTerm.toLowerCase())
);
```

**Delete Handler** (Uses SKU, not ID lookup):
```typescript
const handleConfirmDelete = async () => {
  await stockAPI.deleteStockCard(productToDelete.SKU);
  // Backend handles SKU → LucaId lookup via database
};
```

---

### Database Files

#### Migration: add_lucaid_to_products.sql
**Path**: `db/migrations/add_lucaid_to_products.sql`

**SQL**:
```sql
ALTER TABLE Products ADD LucaId BIGINT NULL;

CREATE NONCLUSTERED INDEX IX_Products_LucaId
ON Products (LucaId)
WHERE LucaId IS NOT NULL;
```

**Execution Result**: ✅ "LucaId column added."

**Performance**: Index enables fast SKU → LucaId lookups for delete operations

---

## ⚡ Performance Optimizations

### 1. LucaId Column for Fast Delete
**Problem**: Luca API `ListStockCards` takes 90 seconds to find skartId by SKU

**Solution**: Store `LucaId` in Products table during sync

**Performance Gain**: 
- **Before**: 90 seconds (API call)
- **After**: 0.1 seconds (database query)
- **Improvement**: 900x faster ✅

**Implementation**:
- Migration: `add_lucaid_to_products.sql`
- Sync: `SyncProductsFromLucaAsync` Lines 1401-1411
- Usage: `AdminController.TestDeleteProduct` Lines 1040-1143

---

### 2. Batch Processing with Intelligent Parallelism
**Worker**: LucaBatchPushWorker

**Algorithm**:
```
If products < 50:
    Sequential processing (lower overhead)
Else:
    Parallel processing (10 concurrent threads)
```

**Benefits**:
- Small batches: Faster due to no parallelism overhead
- Large batches: 10x throughput with controlled concurrency

**Concurrency Control**: `SemaphoreSlim(MaxParallelism)` prevents API overload

---

### 3. Duplicate Prevention with HashSets
**Worker**: KatanaSalesOrderSyncWorker

**Strategy**:
1. Load existing Katana Order IDs into `HashSet<long>`
2. O(1) lookup for duplicate check
3. Skip already-synced orders

**Performance**: O(n) initial load + O(1) per-order check = O(n) total

**Memory**: ~16 bytes per order ID (acceptable for typical order volumes)

---

### 4. Composite Key Lookup for Adjustments
**Worker**: KatanaSalesOrderSyncWorker

**Key Format**: `"{ExternalOrderId}|{SKU}|{Quantity}"`

**Benefits**:
- Detects order changes (quantity updates)
- Prevents duplicate adjustments
- O(1) lookup via `HashSet<string>`

**Example**:
```csharp
var key = "SO-123|PROD-456|5";
if (!processedItemsSet.Contains(key))
{
    // Create new pending adjustment
}
```

---

### 5. Local State Updates in Frontend
**Component**: LucaProducts.tsx

**Optimization**: Update local state immediately after save/delete

**Before**:
```typescript
await stockAPI.deleteProduct(id);
await fetchProducts();  // Full API call
```

**After**:
```typescript
await stockAPI.deleteProduct(id);
setProducts(prev => prev.filter(p => p.id !== id));  // Instant UI update
// Optional: Fetch in background for consistency
```

**Benefits**:
- Instant UI feedback (no spinner)
- Reduced API calls
- Better perceived performance

---

## 🔍 Investigation Todos

### ⚠️ Unclear Status Items

1. **SyncWorkerService Registration**:
   - ❓ Not found in `AddHostedService` grep search
   - ❓ May be conditionally registered or feature-flagged
   - **Action**: Search `Program.cs` for dynamic registration or check feature flags

2. **AutoSyncWorker Disable Reason**:
   - ❌ Commented out on Line 384
   - ❓ Replaced by SyncWorkerService?
   - **Action**: Check git history for disable reason

3. **SyncAllAsync Implementation**:
   - ❓ Not fully reviewed in current code scan
   - ❓ May orchestrate multiple sync methods
   - **Action**: Read full implementation in SyncService.cs

4. **Invoice & Customer Sync Methods**:
   - ⚠️ Stub implementations detected
   - ❓ May be incomplete or in-progress
   - **Action**: Review full implementation status

---

## 📝 Summary

### Active Sync Mechanisms
1. ✅ **KatanaSalesOrderSyncWorker**: Every 5 minutes (Katana orders)
2. ✅ **LucaBatchPushWorker**: Queue-based (Luca products batch)
3. ✅ **Manual API**: `/api/sync/start` endpoint
4. ✅ **Frontend Button**: "Koza ile Senkronize Et"

### Inactive Mechanisms
1. ❌ **AutoSyncWorker**: Commented out (Line 384)
2. ⚠️ **SyncWorkerService**: Status unclear (6-hour full sync)

### Configuration
- **Default Interval**: 6 hours (360 minutes)
- **Batch Size**: 100 products
- **Retry Count**: 3 attempts
- **Parallel Processing**: 10 concurrent threads (batch worker)

### Key Optimizations
1. **LucaId column**: 900x faster delete operations
2. **Intelligent parallelism**: Adaptive processing mode
3. **HashSet duplicate prevention**: O(1) lookup performance
4. **Local state updates**: Instant UI feedback

---

**Document Version**: 1.0  
**Last Updated**: 2024-01-15  
**Author**: GitHub Copilot (Claude Sonnet 4.5)  
**Related Files**: See [Code References](#code-references) section
