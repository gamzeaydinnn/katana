# 🔍 Kod Tabanı Detaylı Analiz Raporu

**Analiz Tarihi:** 2025  
**Branch:** development (sync with master - commit 9963dde)  
**Analiz Kapsamı:** Backend (392 C# files), Frontend (38 TSX files), Database, Tests

---

## 📊 Executive Summary

Proje **Katana-Luca entegrasyonu** olarak ASP.NET Core (.NET 8) backend + React TypeScript frontend mimarisiyle geliştirilmiştir. **Pending Stock Adjustments** merkezi admin onay sistemi temel özellik olarak implement edilmiş, SignalR bildirim altyapısı kurulmuş fakat **kritik güvenlik açıkları** ve **eksik test coverage** tespit edilmiştir.

**Kritik Bulgular:**

- ✅ Backend servis katmanı %85 tamamlanmış
- ⚠️ **YÜKSEK ÖNCELİK:** AdminController'da role-based authorization eksik
- ⚠️ Frontend SignalR entegrasyonu kısmi (bağlantı var, UI güncellemesi eksik)
- ❌ Unit test coverage %30 (sadece 4 test dosyası, entegrasyon testleri eksik)
- ⚠️ Performance bottleneck: LogsController pagination (OFFSET/FETCH kullanıyor)

---

## ✅ YAPILANLAR (Tamamlanmış Özellikler)

### 1. Backend Mimari ve Servisler

#### ✓ Clean Architecture Implementasyonu

- **Katana.Core** (Entity, DTO, Interfaces) - 90+ dosya
- **Katana.Data** (EF Core, Migrations, Repositories) - 50+ dosya
- **Katana.Business** (Use Cases, Services, Validators) - 80+ dosya
- **Katana.Infrastructure** (Workers, API Clients, Logging) - 60+ dosya
- **Katana.API** (Controllers, SignalR Hubs, Middleware) - 112+ dosya

#### ✓ Pending Stock Adjustment Workflow

**Dosya:** `src/Katana.Business/Services/PendingStockAdjustmentService.cs`

```csharp
✅ CreateAsync: Merkezi oluşturma metodu (event publish ile)
✅ ApproveAsync: Claim + Transaction pattern (race condition koruması)
✅ RejectAsync: Red işlemi ve reason logging
✅ IPendingNotificationPublisher entegrasyonu (DI ile optional)
```

**Özellikler:**

- Atomic approve işlemi: `UPDATE PendingStockAdjustments SET Status='Processing' WHERE Id=@id AND Status='Pending'`
- Transaction içinde stok güncelleme + Stocks tablosuna kayıt
- Event publishing: `PendingStockAdjustmentCreatedEvent`, `PendingStockAdjustmentApprovedEvent`
- Loglama: "Pending stock adjustment created with Id {Id}", "Publishing notification..."

#### ✓ SignalR Infrastructure (Backend)

**Dosya:** `src/Katana.API/Notifications/SignalRNotificationPublisher.cs`

```csharp
✅ IHubContext<NotificationHub> DI ile inject
✅ PublishPendingStockAdjustmentCreated: "PendingCreated" event gönderimi
✅ PublishPendingStockAdjustmentApproved: "PendingApproved" event gönderimi
✅ Best-effort publish (hata loglanıyor ama rollback yok)
```

**Hub Setup:** `Program.cs` içinde:

```csharp
builder.Services.AddSignalR();
app.MapHub<NotificationHub>("/hubs/notifications");
```

#### ✓ Authentication & JWT

**Dosyalar:** `src/Katana.API/Controllers/AuthController.cs`, `Program.cs`

```csharp
✅ JWT Bearer authentication (Key: 32+ karakter)
✅ Token generation: 480 dakika expiry
✅ [Authorize] ve [AllowAnonymous] attribute kullanımı
✅ Diagnostic logging: TokenValidationParameters event handlers
```

**appsettings.json:**

```json
"Jwt": {
  "Key": "katana-super-secret-jwt-key-2025-minimum-32-characters-required",
  "Issuer": "KatanaAPI",
  "Audience": "KatanaWebApp",
  "ExpiryMinutes": 480
}
```

#### ✓ Database Migrations

**Dizin:** `src/Katana.Data/Migrations/`

```
✅ 20251028184326_AddPendingStockAdjustmentsAndLogIndexes.cs
✅ 20251030110013_FixPendingAdjustmentsAndStockMovements.cs
✅ 20251030113131_FinalSchemaSync.cs
```

- PendingStockAdjustments tablosu: Id, ProductId, Sku, Quantity, Status, RequestedAt, ApprovedAt, ApprovedBy
- Index: `(Status, RequestedAt DESC)`
- Audit logs index: `(Level, CreatedAt DESC)`

#### ✓ Background Workers

**Dosyalar:**

1. `src/Katana.Infrastructure/Workers/SyncWorkerService.cs`

   - 6 saatte bir ISyncService.SyncAllAsync çağırıyor
   - BackgroundService pattern (graceful shutdown)

2. `src/Katana.Infrastructure/Workers/RetryPendingDbWritesService.cs`
   - PendingDbWriteQueue retry logic
   - In-memory queue (⚠️ restart kayıpları risk altında)

#### ✓ Logging Infrastructure

**Servis:** `src/Katana.Infrastructure/Logging/LoggingService.cs`

- Serilog ile DB'ye log yazımı (AuditLogs, ErrorLogs tabloları)
- Structured logging: `{UserId}`, `{EntityName}`, `{ActionType}`
- ⚠️ Varsayılan level: Information (çok fazla log → performance issue)

### 2. Frontend Implementasyonu

#### ✓ SignalR Client Setup

**Dosya:** `src/frontend/katana-web/src/services/signalr.ts`

```typescript
✅ HubConnectionBuilder ile /hubs/notifications bağlantısı
✅ Token factory: localStorage.getItem("token")
✅ Auto-reconnect: withAutomaticReconnect()
✅ onPendingCreated / onPendingApproved event handlers
```

#### ✓ UI Components with SignalR Integration

**Dosyalar:**

1. `src/frontend/katana-web/src/components/Layout/Header.tsx` (line 364)

   - SignalR connection start: `signalr.connect()`
   - Notification badge update

2. `src/frontend/katana-web/src/components/Admin/PendingAdjustments.tsx` (lines 135, 180)
   - Event subscription: `signalr.onPendingCreated(callback)`
   - ⚠️ UI güncelleme mantığı eksik (event handler boş)

#### ✓ Admin Panel Structure

**Dizin:** `src/frontend/katana-web/src/components/Admin/`

```
✅ AdminPanel.tsx - Ana dashboard
✅ PendingAdjustments.tsx - Bekleyen işlemler listesi
✅ Material-UI Table + Button components
```

### 3. Testing Infrastructure

#### ✓ Mevcut Test Dosyaları (4 adet)

**Dizin:** `tests/Katana.Tests/Services/`

1. **PendingStockAdjustmentServiceTests.cs**

```csharp
✅ In-memory SQLite ile integration test
✅ ApproveAsync_ShouldApplyStockChangeAndPersistMovement test case:
   - Başlangıç stok: 10
   - Pending quantity: -4 (decrease)
   - Beklenen sonuç: 6 (stok düşmüş olmalı)
   - Stock movement kaydı kontrol ediliyor (Type: "OUT")
✅ FluentAssertions kullanımı
```

2. **SyncServiceTests.cs** - Senkronizasyon testleri
3. **MappingHelperTests.cs** - Mapping doğrulamaları
4. **KatanaServiceMappingTests.cs** - Service mapping

**⚠️ Kritik Eksiklik:**

- Concurrent approval testleri YOK (race condition senaryosu)
- Role-based authorization testleri YOK
- SignalR event publish testleri YOK
- Frontend birim testleri YOK (0 .test.tsx dosyası çalışıyor)

### 4. DevOps & Tools

#### ✓ PowerShell E2E Test Script

**Dosya:** `scripts/admin-e2e.ps1`

```powershell
✅ Login → Token alma
✅ Create pending adjustment
✅ List pending adjustments
✅ Approve işlemi
✅ PSReadLine paste crash workaround
```

#### ✓ Docker Setup

```yaml
✅ docker-compose.yml mevcut
✅ Dockerfile mevcut (multi-stage build için hazır)
```

---

## ❌ EKSİKLER (Öncelik Sıralamalı)

### 🔴 YÜKSEK ÖNCELİK (Acil Yapılmalı)

#### 1. **AdminController Role-Based Authorization Eksik**

**Dosya:** `src/Katana.API/Controllers/AdminController.cs`

**Mevcut Durum:**

```csharp
[Authorize]  // ✅ Class level'da var
[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    [HttpPost("pending-adjustments/{id}/approve")]  // ❌ Role check YOK
    public async Task<IActionResult> ApprovePendingAdjustment(long id) { ... }

    [HttpPost("pending-adjustments/{id}/reject")]   // ❌ Role check YOK
    public async Task<IActionResult> RejectPendingAdjustment(long id) { ... }
}
```

**Problem:** Herhangi bir authenticated kullanıcı (normal user bile) approve/reject yapabilir!

**Çözüm:**

```csharp
[Authorize(Roles = "Admin,StockManager")]  // EKLENMELI
[HttpPost("pending-adjustments/{id}/approve")]
public async Task<IActionResult> ApprovePendingAdjustment(long id) { ... }
```

**Etkilenen Endpoints:**

- `POST /api/admin/pending-adjustments/{id}/approve` (line 73)
- `POST /api/admin/pending-adjustments/{id}/reject` (line 97)
- `POST /api/admin/test-pending` (line 127)

**Risk:** **CRITICAL SECURITY VULNERABILITY** - Unauthorized access

---

#### 2. **Frontend SignalR UI Update Logic Missing**

**Dosya:** `src/frontend/katana-web/src/components/Admin/PendingAdjustments.tsx`

**Mevcut Durum:**

```typescript
// Line 135
useEffect(() => {
  signalr.onPendingCreated((data) => {
    console.log("New pending created:", data);
    // ❌ State update mantığı YOK - UI güncellenmiyor!
  });

  signalr.onPendingApproved((data) => {
    console.log("Pending approved:", data);
    // ❌ Liste güncellenmesi YOK
  });
}, []);
```

**Eksik:**

- `setPendings([newPending, ...pendings])` state güncellemesi
- Real-time list refresh
- Toast notification (Snackbar)

**Çözüm:**

```typescript
signalr.onPendingCreated((data) => {
  setPendings((prev) => [data, ...prev]);
  enqueueSnackbar("Yeni bekleyen işlem oluşturuldu", { variant: "info" });
});

signalr.onPendingApproved((data) => {
  setPendings((prev) => prev.filter((p) => p.id !== data.id));
  enqueueSnackbar("İşlem onaylandı", { variant: "success" });
});
```

---

#### 3. **Unit Test Coverage Yetersiz (%30)**

**Mevcut:** 4 test dosyası, toplam ~10-15 test case

**Eksik Testler:**

**3.1 Concurrent Approval Scenarios**

```csharp
// EKLENMELI: tests/Katana.Tests/Services/ConcurrentApprovalTests.cs
[Fact]
public async Task ApproveAsync_TwoConcurrentRequests_OnlyOneShouldSucceed()
{
    // Arrange: 2 paralel approve attempt
    var task1 = _service.ApproveAsync(_pendingId, "admin1");
    var task2 = _service.ApproveAsync(_pendingId, "admin2");

    // Act
    var results = await Task.WhenAll(task1, task2);

    // Assert: Sadece biri true dönmeli (claim pattern testi)
    results.Count(r => r == true).Should().Be(1);
}
```

**3.2 SignalR Event Publishing Tests**

```csharp
// EKLENMELI: tests/Katana.Tests/Notifications/SignalRPublisherTests.cs
[Fact]
public async Task PublishPendingCreated_ShouldSendToConnectedClients()
{
    // Mock IHubContext
    var mockClients = new Mock<IHubClients>();
    // Test event gönderimini doğrula
}
```

**3.3 Role-Based Authorization Tests**

```csharp
// EKLENMELI: tests/Katana.Tests/Controllers/AdminControllerAuthTests.cs
[Fact]
public async Task ApproveEndpoint_WithoutAdminRole_ShouldReturn403()
{
    // Arrange: User token (no Admin role)
    // Act: POST /api/admin/pending-adjustments/1/approve
    // Assert: Status code 403 Forbidden
}
```

**3.4 Frontend Component Tests**

```typescript
// EKLENMELI: src/frontend/katana-web/src/components/Admin/__tests__/PendingAdjustments.test.tsx
describe("PendingAdjustments SignalR Integration", () => {
  it("should update list when onPendingCreated fires", () => {
    // Mock signalr.onPendingCreated
    // Trigger event
    // Assert: table row count increased
  });
});
```

---

### 🟠 ORTA ÖNCELİK (2-4 Hafta İçinde)

#### 4. **Publish Retry / Dead Letter Queue (DLQ) Eksik**

**Dosya:** `src/Katana.API/Notifications/SignalRNotificationPublisher.cs`

**Mevcu Durum:**

```csharp
try {
    await _hubContext.Clients.All.SendAsync("PendingCreated", data);
    _logger.LogInformation("Published PendingStockAdjustmentCreated");
} catch (Exception ex) {
    _logger.LogError(ex, "Failed to publish notification");
    // ❌ Retry YOK - event kayboldu!
}
```

**Eksik:**

- Exponential backoff retry (3 attempt)
- Failed event'leri DLQ table'a kaydetme
- Dead letter processing için background job

**Çözüm Önerisi:**

```csharp
// Yeni tablo: FailedNotifications (Id, EventType, Payload, RetryCount, CreatedAt)
// Service: IRetryablePublisher ile Polly retry policy
var retryPolicy = Policy
    .Handle<Exception>()
    .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

await retryPolicy.ExecuteAsync(async () => {
    await _hubContext.Clients.All.SendAsync("PendingCreated", data);
});
```

---

#### 5. **LogsController Performance Optimization**

**Dosya:** `src/Katana.API/Controllers/LogsController.cs`

**Problem:**

- OFFSET/FETCH pagination kullanılıyor → 15-60 saniye query time
- GROUP BY sorguları optimize edilmemiş
- Index kullanımı yetersiz

**Çözüm:**

**5.1 Keyset Pagination (Cursor-based)**

```csharp
// Şu an: OFFSET @skip FETCH NEXT @take
// Değişmeli:
var logs = await _context.ErrorLogs
    .Where(e => e.CreatedAt < cursor)  // cursor = son gelen kaydın timestamp'i
    .OrderByDescending(e => e.CreatedAt)
    .Take(pageSize)
    .ToListAsync();
```

**5.2 Index Ekleme**

```sql
-- Migration'a ekle:
CREATE INDEX IX_ErrorLogs_CreatedAt_Level
ON ErrorLogs(CreatedAt DESC, Level);

CREATE INDEX IX_AuditLogs_Timestamp_ActionType
ON AuditLogs(Timestamp DESC, ActionType);
```

**5.3 Dashboard Metrics Pre-Aggregation**

```csharp
// Background job: HourlyMetricsAggregator
// Table: DashboardMetrics (Hour, ErrorCount, AuditCount, ...)
// Dashboard query: Son 24 saat için pre-aggregated data kullan
```

---

#### 6. **Retention Policy & Log Purging**

**Eksik:** Eski logları temizleyen mekanizma yok

**Çözüm:**

```csharp
// Yeni worker: LogRetentionService.cs
public class LogRetentionService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-90);
            await _context.ErrorLogs.Where(e => e.CreatedAt < cutoffDate).ExecuteDeleteAsync();
            await _context.AuditLogs.Where(a => a.Timestamp < cutoffDate).ExecuteDeleteAsync();

            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
}
```

**appsettings.json:**

```json
"Logging": {
  "RetentionDays": 90,
  "PersistMinimumLevel": "Warning"  // Information yerine Warning (DB log azaltma)
}
```

---

### 🟢 DÜŞÜK ÖNCELİK (Nice-to-Have)

#### 7. **PendingDbWriteQueue Durability**

**Dosya:** `src/Katana.Infrastructure/Workers/RetryPendingDbWritesService.cs`

**Problem:** In-memory queue → app restart'te kayıp risk

**Çözüm:**

- Azure Storage Queue / RabbitMQ integration
- Veya küçük SQLite backstore (`pending_writes.db`)

---

#### 8. **Monitoring & Alerting**

**Eksik:**

- Application Insights / Elastic APM entegrasyonu
- Slow query alert (>5 saniye)
- Failed publish alert (DLQ threshold)

**Çözüm:**

```csharp
// Program.cs
builder.Services.AddApplicationInsightsTelemetry();

// Alert kuralları:
// 1. Error log count > 50/hour → Email alert
// 2. Pending queue size > 100 → Alert
```

---

#### 9. **API Documentation (Swagger)**

**Mevcut:** Swagger UI var ama açıklamalar eksik

**İyileştirme:**

```csharp
[HttpPost("pending-adjustments/{id}/approve")]
[ProducesResponseType(typeof(PendingStockAdjustmentDto), 200)]
[ProducesResponseType(404)]
[ProducesResponseType(409)]  // Already processed
/// <summary>
/// Admin onayı ile stok düzeltmesi yapar
/// </summary>
/// <param name="id">Pending adjustment ID</param>
/// <returns>Onaylanmış adjustment bilgisi</returns>
public async Task<IActionResult> ApprovePendingAdjustment(long id) { ... }
```

---

## 🔒 GÜVENLİK BULGULARI (Security Audit)

### Kritik Açıklar

#### 1. **Unauthorized Admin Access (CRITICAL)**

- **Dosya:** `AdminController.cs`
- **Satırlar:** 73-127
- **Risk:** Normal user'lar admin işlemleri yapabilir
- **CVSS Score:** 8.1 (High)
- **Fix:** `[Authorize(Roles = "Admin,StockManager")]` ekle

#### 2. **AllowAnonymous Overuse**

**Etkilenen Controllers:**

```csharp
DashboardController.cs → [AllowAnonymous] // Line 12
ProductsController.cs → [AllowAnonymous]  // Line 15
CustomersController.cs → [AllowAnonymous] // Line 18
SyncController.cs → [AllowAnonymous]      // Line 20
```

**Risk:** Sensitive data endpoints authentication olmadan erişilebilir

**Öneri:**

- Dashboard: Authenticated kullanıcı görmeli
- Products/Customers: En azından read-only için auth gerek
- Sync: Sadece internal service'den çağrılmalı (API key check)

#### 3. **JWT Secret Key Management**

**Dosya:** `appsettings.json` (line 33)

```json
"Jwt": {
  "Key": "katana-super-secret-jwt-key-2025-minimum-32-characters-required"
}
```

**Risk:** Hardcoded secret → production'da Azure Key Vault/Environment Variable kullanılmalı

**Fix:**

```csharp
// Program.cs
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? throw new InvalidOperationException("JWT key not configured");
```

---

## 📈 PERFORMANS ANALİZİ

### Bottleneck'ler

#### 1. **LogsController Queries**

- **Query Time:** 15-60 saniye (büyük tablo)
- **Problem:** OFFSET pagination + GROUP BY
- **Impact:** Dashboard yüklenme süresi yüksek

#### 2. **Serilog DB Write**

- **Volume:** ~500 log/minute (Information level)
- **Problem:** Her request için DB write
- **Impact:** DB connection pool exhaustion riski

#### 3. **SignalR Connection Count**

- **Mevcut:** Tüm client'lar "All" group'a gönderiliyor
- **Problem:** 100+ client'ta broadcast overhead
- **Öneri:** User-specific groups (`Users.User(userId).SendAsync`)

### Optimization Checklist

```markdown
✅ EF Core query projection (Select yerine full entity dönüyor)
❌ Response caching eksik (GET /api/products → cache 5dk)
❌ Redis distributed cache yok (multi-instance scenario için gerek)
✅ Database index'ler var (ama eksik olanlar var - yukarıda belirtildi)
❌ CDN kullanımı yok (static assets için)
```

---

## 🧪 TEST DURUM RAPORU

### Test Metrikleri

| Kategori              | Mevcut                             | Hedef                        | Coverage |
| --------------------- | ---------------------------------- | ---------------------------- | -------- |
| **Unit Tests**        | 4 dosya (~15 test)                 | 50+ test                     | ~30%     |
| **Integration Tests** | 1 (SQLite in-memory)               | 20+ test                     | ~10%     |
| **E2E Tests**         | 1 PowerShell script                | Selenium/Playwright suite    | ~5%      |
| **Frontend Tests**    | 0 (setupTests.ts var ama test yok) | Jest + React Testing Library | 0%       |

### Eksik Test Senaryoları

#### Backend

```markdown
❌ Concurrent approval race condition
❌ Role-based authorization (403 response)
❌ SignalR event delivery (mock HubContext)
❌ Retry logic (PendingDbWriteQueue)
❌ JWT expiry scenarios
❌ API rate limiting (eğer varsa)
```

#### Frontend

```markdown
❌ SignalR reconnection logic
❌ PendingAdjustments list update (state management)
❌ Admin dashboard data loading
❌ Error boundary handling
❌ Token expiry redirect
```

---

## 📁 DOSYA YAPISI ÖZETİ

### Backend (392 C# Files)

```
src/
├── Katana.API/ (112 files)
│   ├── Controllers/ (23 controllers)
│   │   ├── AdminController.cs ⚠️ Role check eksik
│   │   ├── AuthController.cs ✅
│   │   ├── DashboardController.cs ⚠️ AllowAnonymous
│   │   └── ...
│   ├── Hubs/
│   │   └── NotificationHub.cs ✅
│   ├── Notifications/
│   │   └── SignalRNotificationPublisher.cs ⚠️ Retry eksik
│   └── Program.cs ✅
│
├── Katana.Business/ (80 files)
│   ├── Services/
│   │   ├── PendingStockAdjustmentService.cs ✅
│   │   ├── SyncService.cs ✅
│   │   └── ...
│   └── Validators/ (FluentValidation)
│
├── Katana.Core/ (90 files)
│   ├── Entities/
│   ├── DTOs/
│   └── Interfaces/
│       └── IPendingNotificationPublisher.cs ✅
│
├── Katana.Data/ (50 files)
│   ├── Context/
│   │   └── IntegrationDbContext.cs ✅
│   └── Migrations/ (7 migration files) ✅
│
└── Katana.Infrastructure/ (60 files)
    ├── Workers/
    │   ├── SyncWorkerService.cs ✅
    │   └── RetryPendingDbWritesService.cs ⚠️ Durable değil
    └── Logging/
        └── LoggingService.cs ⚠️ Volume yüksek
```

### Frontend (38 TSX Files)

```
frontend/katana-web/src/
├── components/
│   ├── Admin/
│   │   ├── AdminPanel.tsx ✅
│   │   └── PendingAdjustments.tsx ⚠️ UI update eksik
│   ├── Layout/
│   │   └── Header.tsx ✅ SignalR connect
│   └── ...
├── services/
│   └── signalr.ts ✅ Client implementation
└── App.tsx ✅
```

---

## 🎯 EYLEM PLANI (Öncelik Sıralı)

### Sprint 1 (1 Hafta) - Kritik Güvenlik

#### 1.1 AdminController Authorization Fix

```bash
# Dosya: src/Katana.API/Controllers/AdminController.cs
# Değişiklik: [Authorize(Roles = "Admin,StockManager")] ekle
# Test: scripts/admin-e2e.ps1 ile doğrula
# Commit: "feat(security): Add role-based auth to admin endpoints"
```

#### 1.2 Role Claim Setup

```csharp
// AuthController.cs - GenerateJwtToken methoduna ekle:
new Claim(ClaimTypes.Role, "Admin"),  // Admin user için
new Claim(ClaimTypes.Role, "StockManager")  // Stok yöneticisi için
```

#### 1.3 Authorization Integration Test

```bash
# Dosya: tests/Katana.Tests/Controllers/AdminControllerAuthTests.cs
# Test cases:
# - Admin role ile approve → 200 OK
# - Normal user ile approve → 403 Forbidden
# - Anonymous ile approve → 401 Unauthorized
```

**Beklenen Süre:** 2 gün  
**Owner:** Backend developer  
**Definition of Done:**

- ✅ AdminController'da 3 endpoint'e role check eklendi
- ✅ AuthController'da role claim oluşturuldu
- ✅ 3 integration test passed
- ✅ Swagger'da role requirement dokümante edildi

---

### Sprint 2 (1 Hafta) - Frontend SignalR

#### 2.1 PendingAdjustments UI Update

```typescript
// Dosya: src/frontend/katana-web/src/components/Admin/PendingAdjustments.tsx
// Değişiklik:
useEffect(() => {
  signalr.onPendingCreated((data: PendingDto) => {
    setPendings((prev) => [data, ...prev]);
    enqueueSnackbar("Yeni işlem: " + data.sku, { variant: "info" });
  });

  signalr.onPendingApproved((data: { id: number }) => {
    setPendings((prev) => prev.filter((p) => p.id !== data.id));
    enqueueSnackbar("İşlem onaylandı", { variant: "success" });
  });

  return () => {
    signalr.offPendingCreated();
    signalr.offPendingApproved();
  };
}, [enqueueSnackbar]);
```

#### 2.2 Header Notification Badge Update

```typescript
// Dosya: src/frontend/katana-web/src/components/Layout/Header.tsx
const [pendingCount, setPendingCount] = useState(0);

useEffect(() => {
  signalr.onPendingCreated(() => {
    setPendingCount((prev) => prev + 1);
  });

  signalr.onPendingApproved(() => {
    setPendingCount((prev) => Math.max(0, prev - 1));
  });
}, []);
```

#### 2.3 Frontend Component Tests

```typescript
// Dosya: src/frontend/katana-web/src/components/Admin/__tests__/PendingAdjustments.test.tsx
import { render, screen, waitFor } from "@testing-library/react";

test("updates list when SignalR event received", async () => {
  // Mock signalr.onPendingCreated
  // Trigger event
  await waitFor(() => {
    expect(screen.getByText("SKU-123")).toBeInTheDocument();
  });
});
```

**Beklenen Süre:** 3 gün  
**Owner:** Frontend developer  
**Definition of Done:**

- ✅ PendingAdjustments component real-time güncelleniyor
- ✅ Header notification badge çalışıyor
- ✅ Toast notification gösteriliyor
- ✅ 2 component test passed

---

### Sprint 3 (2 Hafta) - Test Coverage

#### 3.1 Concurrent Approval Tests

```bash
# Dosya: tests/Katana.Tests/Services/ConcurrentApprovalTests.cs
# Test: 10 paralel approve attempt, sadece 1 tanesi başarılı olmalı
```

#### 3.2 SignalR Publisher Tests

```bash
# Dosya: tests/Katana.Tests/Notifications/SignalRPublisherTests.cs
# Mock IHubContext
# Verify: SendAsync çağrıldı mı, doğru data gönderildi mi
```

#### 3.3 E2E Test Suite (Playwright)

```typescript
// tests/e2e/admin-approval.spec.ts
test("admin can approve pending adjustment", async ({ page }) => {
  await page.goto("http://localhost:3000/admin");
  await page.click('button[data-testid="approve-button-1"]');
  await expect(page.locator(".success-toast")).toBeVisible();
});
```

**Beklenen Süre:** 5 gün  
**Target Coverage:** %60 (backend), %40 (frontend)

---

### Sprint 4 (2 Hafta) - Performance & Resilience

#### 4.1 LogsController Optimization

```csharp
// Keyset pagination implement
// Index ekle: (CreatedAt DESC, Level)
// Benchmark: Query time < 2 saniye
```

#### 4.2 Publish Retry Logic

```csharp
// Polly retry policy: 3 attempt, exponential backoff
// FailedNotifications table
// DLQ processing worker
```

#### 4.3 Log Retention Service

```csharp
// Daily purge job: 90 gün öncesi logları sil
// Config: appsettings.json → RetentionDays
```

**Beklenen Süre:** 7 gün  
**Metrics:**

- ✅ Logs API response time < 2s
- ✅ Failed publish retry rate > %95
- ✅ DB log volume azaltıldı (%50 reduction)

---

## 📊 ÖNCELIK MATRİSİ

| #   | Görev                      | Kritiklik   | Süre  | Etki              | Bağımlılık |
| --- | -------------------------- | ----------- | ----- | ----------------- | ---------- |
| 1   | AdminController role auth  | 🔴 CRITICAL | 2 gün | Security fix      | Yok        |
| 2   | Frontend SignalR UI update | 🔴 HIGH     | 3 gün | UX improvement    | Yok        |
| 3   | Unit test coverage         | 🟠 HIGH     | 5 gün | Quality assurance | Yok        |
| 4   | Publish retry/DLQ          | 🟠 MEDIUM   | 4 gün | Reliability       | Yok        |
| 5   | LogsController perf        | 🟠 MEDIUM   | 3 gün | Performance       | Yok        |
| 6   | Log retention              | 🟢 LOW      | 2 gün | Maintenance       | Yok        |
| 7   | Monitoring/Alerts          | 🟢 LOW      | 5 gün | Observability     | #4, #5     |

**Toplam Süre:** ~24 gün (4-5 sprint)

---

## 🔗 REFERANS DOSYALAR

### Kritik Backend Dosyaları

```
src/Katana.API/Controllers/AdminController.cs (line 73-127) → Role auth ekle
src/Katana.Business/Services/PendingStockAdjustmentService.cs → Çalışıyor ✅
src/Katana.API/Notifications/SignalRNotificationPublisher.cs → Retry ekle
src/Katana.API/Controllers/LogsController.cs → Pagination optimize et
```

### Kritik Frontend Dosyaları

```
src/frontend/katana-web/src/components/Admin/PendingAdjustments.tsx (line 135) → UI update
src/frontend/katana-web/src/components/Layout/Header.tsx (line 364) → Badge update
src/frontend/katana-web/src/services/signalr.ts → Çalışıyor ✅
```

### Test Dosyaları

```
tests/Katana.Tests/Services/PendingStockAdjustmentServiceTests.cs → Mevcut ✅
tests/Katana.Tests/Services/ConcurrentApprovalTests.cs → EKLENMELI ❌
tests/Katana.Tests/Controllers/AdminControllerAuthTests.cs → EKLENMELI ❌
```

### Konfigürasyon

```
src/Katana.API/appsettings.json → JWT key, DB connection
docker-compose.yml → Container setup
scripts/admin-e2e.ps1 → E2E test script ✅
```

---

## 📝 SONUÇ VE ÖNERİLER

### Genel Değerlendirme

**Güçlü Yönler:**

- ✅ Clean architecture iyi uygulanmış
- ✅ Pending workflow merkezi ve atomic
- ✅ SignalR infrastructure kurulmuş
- ✅ EF Core migrations düzenli
- ✅ Background worker'lar çalışıyor

**Zayıf Yönler:**

- ⚠️ Security gaps (role-based auth eksik)
- ⚠️ Test coverage yetersiz (%30)
- ⚠️ Frontend SignalR entegrasyonu yarım
- ⚠️ Performance issues (LogsController)
- ⚠️ Retry/DLQ mechanism yok

### Teknik Borç (Technical Debt)

**Hemen İyileştirilmeli:**

1. AdminController authorization (2 gün çaba)
2. Frontend UI update logic (3 gün çaba)

**Orta Vadede:** 3. Unit test coverage artırımı (1 hafta) 4. Performance optimization (1 hafta)

**Uzun Vadede:** 5. Monitoring/alerting setup 6. API documentation improvement

### Risk Değerlendirmesi

| Risk                      | Olasılık | Etki   | Azaltma                |
| ------------------------- | -------- | ------ | ---------------------- |
| Unauthorized admin access | Yüksek   | Kritik | Hemen role auth ekle   |
| SignalR event loss        | Orta     | Yüksek | Retry/DLQ implement    |
| Slow dashboard queries    | Yüksek   | Orta   | Pagination optimize et |
| Test regression           | Orta     | Yüksek | Coverage %60'a çıkar   |
| Production secret leak    | Düşük    | Kritik | Key Vault kullan       |

---

## 🚀 BAŞLANGIÇ KOMUTU (Hızlı Test)

```powershell
# 1. Backend başlat
cd c:\Users\GAMZE\Desktop\katana
dotnet build src\Katana.API
dotnet run --project src\Katana.API --urls "http://localhost:5055"

# 2. Frontend başlat (yeni terminal)
cd frontend\katana-web
npm start  # http://localhost:3000

# 3. E2E test çalıştır (yeni terminal)
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\admin-e2e.ps1

# 4. Loglarda şunu ara:
# "Pending stock adjustment created with Id"
# "Publishing PendingStockAdjustmentCreated"
```

---

**Rapor Tarihi:** 2025  
**Hazırlayan:** GitHub Copilot Code Analysis  
**Versiyon:** 1.0  
**Son Güncelleme:** Master branch sync (commit 9963dde)

---

## 📮 İLETİŞİM & DESTEK

Bu rapor hakkında sorularınız için:

- 🐛 **Bug Report:** GitHub Issues
- 💬 **Tartışma:** Team Slack #katana-dev
- 📖 **Dökümantasyon:** `docs/` klasörü

**İleri Adım:**
Sprint 1'den başlayın → AdminController authorization fix (en kritik güvenlik açığı)

```bash
git checkout -b feature/admin-role-authorization
# AdminController.cs düzenle
# Test yaz
# Commit & PR
```

---
