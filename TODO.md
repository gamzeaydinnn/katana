# Katana Integration - TODO & Action Plan

**Last Updated:** 2025 (Post Master Branch Sync - commit 9963dde)  
**Status:** Development in progress

---

## 🔥 ACIL (CRITICAL) - Hemen Yapılacaklar

### 1. ⚠️ AdminController Security Fix (2 gün)

**Priority:** CRITICAL  
**File:** `src/Katana.API/Controllers/AdminController.cs`

```csharp
// DEĞIŞIKLIK:
[Authorize(Roles = "Admin,StockManager")]  // EKLE
[HttpPost("pending-adjustments/{id}/approve")]
public async Task<IActionResult> ApprovePendingAdjustment(long id) { ... }

[Authorize(Roles = "Admin,StockManager")]  // EKLE
[HttpPost("pending-adjustments/{id}/reject")]
public async Task<IActionResult> RejectPendingAdjustment(long id) { ... }
```

**Adımlar:**

- [ ] AdminController.cs'ye role decorator ekle (lines 73, 97, 127)
- [ ] AuthController.cs'de role claim oluştur (GenerateJwtToken method)
- [ ] Integration test yaz (AdminControllerAuthTests.cs)
- [ ] E2E script ile doğrula (`scripts/admin-e2e.ps1`)

**Test Case:**

```bash
# Normal user ile approve attempt → 403 Forbidden dönmeli
# Admin user ile approve → 200 OK dönmeli
```

---

### 2. 🔴 Frontend SignalR UI Update (3 gün)

**Priority:** HIGH  
**File:** `frontend/katana-web/src/components/Admin/PendingAdjustments.tsx`

```typescript
// DEĞIŞIKLIK (Line 135):
useEffect(() => {
  signalr.onPendingCreated((data: PendingDto) => {
    setPendings((prev) => [data, ...prev]); // STATE UPDATE EKLE
    enqueueSnackbar("Yeni işlem: " + data.sku, { variant: "info" });
  });

  signalr.onPendingApproved((data: { id: number }) => {
    setPendings((prev) => prev.filter((p) => p.id !== data.id)); // REMOVE EKLE
    enqueueSnackbar("İşlem onaylandı", { variant: "success" });
  });

  return () => {
    signalr.offPendingCreated();
    signalr.offPendingApproved();
  };
}, [enqueueSnackbar]);
```

**Adımlar:**

- [ ] PendingAdjustments.tsx state update logic ekle
- [ ] Header.tsx notification badge güncellemesi (line 364)
- [ ] Toast notification implement (notistack)
- [ ] Component test yaz (`__tests__/PendingAdjustments.test.tsx`)

**Test:**

```bash
# Backend'den event gönder → Frontend list güncellensin
# Badge sayısı artmalı
```

---

## 🟠 YÜKSEK ÖNCELİK (1-2 Hafta)

### 3. 🧪 Unit Test Coverage Artırımı (5 gün)

**Target Coverage:** Backend %60, Frontend %40

#### 3.1 Concurrent Approval Tests

**File:** `tests/Katana.Tests/Services/ConcurrentApprovalTests.cs` (YENİ)

```csharp
[Fact]
public async Task ApproveAsync_TwoConcurrentRequests_OnlyOneShouldSucceed()
{
    // 2 paralel approve attempt
    var task1 = _service.ApproveAsync(_pendingId, "admin1");
    var task2 = _service.ApproveAsync(_pendingId, "admin2");

    var results = await Task.WhenAll(task1, task2);

    // Sadece biri başarılı olmalı (claim pattern testi)
    results.Count(r => r == true).Should().Be(1);
}
```

**Adımlar:**

- [ ] ConcurrentApprovalTests.cs oluştur
- [ ] 10 paralel attempt scenario ekle
- [ ] Race condition verify et

#### 3.2 SignalR Publisher Tests

**File:** `tests/Katana.Tests/Notifications/SignalRPublisherTests.cs` (YENİ)

```csharp
[Fact]
public async Task PublishPendingCreated_ShouldCallHubSendAsync()
{
    // Mock IHubContext + IHubClients
    var mockClients = new Mock<IHubClients>();

    // Act
    await _publisher.PublishPendingStockAdjustmentCreated(data);

    // Assert
    mockClients.Verify(c => c.All.SendAsync("PendingCreated", It.IsAny<object>()));
}
```

**Adımlar:**

- [ ] SignalRPublisherTests.cs oluştur
- [ ] Hub event publish verify et
- [ ] Failed publish scenario test et

#### 3.3 Frontend Component Tests

**File:** `frontend/katana-web/src/components/Admin/__tests__/PendingAdjustments.test.tsx` (YENİ)

```typescript
import { render, screen, waitFor } from "@testing-library/react";

test("updates list when onPendingCreated fires", async () => {
  const { rerender } = render(<PendingAdjustments />);

  // Mock signalr event
  act(() => {
    signalr.triggerEvent("PendingCreated", { id: 1, sku: "SKU-123" });
  });

  await waitFor(() => {
    expect(screen.getByText("SKU-123")).toBeInTheDocument();
  });
});
```

**Adımlar:**

- [ ] Jest + React Testing Library setup
- [ ] PendingAdjustments component test
- [ ] SignalR mock service oluştur

---

## 🟡 ORTA ÖNCELİK (2-4 Hafta)

### 4. 🔄 Publish Retry & Dead Letter Queue (4 gün)

**File:** `src/Katana.API/Notifications/SignalRNotificationPublisher.cs`

**Değişiklik:**

```csharp
// Polly retry policy ekle
using Polly;

private readonly AsyncRetryPolicy _retryPolicy = Policy
    .Handle<Exception>()
    .WaitAndRetryAsync(3,
        attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
        onRetry: (ex, timespan, attempt, context) => {
            _logger.LogWarning("Retry attempt {Attempt} after {Delay}s", attempt, timespan.TotalSeconds);
        });

public async Task PublishPendingStockAdjustmentCreated(PendingDto data)
{
    try {
        await _retryPolicy.ExecuteAsync(async () => {
            await _hubContext.Clients.All.SendAsync("PendingCreated", data);
        });
    } catch (Exception ex) {
        // DLQ'ya kaydet
        await SaveToDeadLetterQueue("PendingCreated", data, ex.Message);
    }
}
```

**Yeni Tablo:**

```sql
CREATE TABLE FailedNotifications (
    Id BIGINT PRIMARY KEY IDENTITY,
    EventType NVARCHAR(100),
    Payload NVARCHAR(MAX),  -- JSON
    ErrorMessage NVARCHAR(MAX),
    RetryCount INT DEFAULT 0,
    CreatedAt DATETIME2,
    LastRetryAt DATETIME2
);
```

**Adımlar:**

- [ ] Polly NuGet package ekle
- [ ] Retry policy implement et
- [ ] FailedNotifications migration oluştur
- [ ] DLQ processing worker ekle (`FailedNotificationRetryService.cs`)

---

### 5. ⚡ LogsController Performance Optimization (3 gün)

**File:** `src/Katana.API/Controllers/LogsController.cs`

#### 5.1 Keyset Pagination

**Şu An:**

```csharp
// OFFSET/FETCH kullanılıyor (yavaş)
var logs = await _context.ErrorLogs
    .Skip(page * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

**Değişmeli:**

```csharp
// Cursor-based pagination
var logs = await _context.ErrorLogs
    .Where(e => e.CreatedAt < cursorTimestamp)  // cursor = son kayıt timestamp
    .OrderByDescending(e => e.CreatedAt)
    .Take(pageSize)
    .ToListAsync();
```

#### 5.2 Index Ekle

**Migration:**

```csharp
migrationBuilder.CreateIndex(
    name: "IX_ErrorLogs_CreatedAt_Level",
    table: "ErrorLogs",
    columns: new[] { "CreatedAt", "Level" });

migrationBuilder.CreateIndex(
    name: "IX_AuditLogs_Timestamp_ActionType",
    table: "AuditLogs",
    columns: new[] { "Timestamp", "ActionType" });
```

#### 5.3 Dashboard Metrics Pre-Aggregation

**Yeni Worker:**

```csharp
// HourlyMetricsAggregator.cs
// Her saat başı ErrorLogs/AuditLogs'u aggregate et
// DashboardMetrics tablosuna yaz
// Dashboard query: aggregate'ten oku (raw logs yerine)
```

**Adımlar:**

- [ ] Keyset pagination implement
- [ ] Index migration oluştur
- [ ] HourlyMetricsAggregator worker ekle
- [ ] Dashboard controller aggregate query kullan
- [ ] Benchmark: Query time < 2s olmalı

---

### 6. 🗑️ Log Retention Policy (2 gün)

**File:** `src/Katana.Infrastructure/Workers/LogRetentionService.cs` (YENİ)

```csharp
public class LogRetentionService : BackgroundService
{
    private readonly int _retentionDays = 90;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-_retentionDays);

            await _context.ErrorLogs
                .Where(e => e.CreatedAt < cutoffDate)
                .ExecuteDeleteAsync();

            await _context.AuditLogs
                .Where(a => a.Timestamp < cutoffDate)
                .ExecuteDeleteAsync();

            _logger.LogInformation("Purged logs older than {Days} days", _retentionDays);

            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
}
```

**appsettings.json:**

```json
"Logging": {
  "RetentionDays": 90,
  "PersistMinimumLevel": "Warning"  // Information yerine Warning (volume düşürmek için)
}
```

**Adımlar:**

- [ ] LogRetentionService.cs oluştur
- [ ] Program.cs'de register et (AddHostedService)
- [ ] Config'e RetentionDays ekle
- [ ] Test: 91 gün önce test log oluştur → purge ediliyor mu?

---

## 🟢 DÜŞÜK ÖNCELİK (Nice-to-Have)

### 7. 📊 Monitoring & Alerting

- [ ] Application Insights entegrasyonu
- [ ] Slow query alert (>5s)
- [ ] Failed publish alert (DLQ threshold > 10)
- [ ] Dashboard metrics widget

### 8. 📖 API Documentation

- [ ] Swagger açıklamaları ekle (XML comments)
- [ ] Response type annotations (`[ProducesResponseType]`)
- [ ] Example payloads

### 9. 🔐 Production Security

- [ ] JWT Key → Azure Key Vault
- [ ] AllowAnonymous controller'ları review et
- [ ] API rate limiting (AspNetCoreRateLimit)

### 10. 🎨 Frontend Modernization

- [ ] Theme enhancement (glassmorphism, gradients)
- [ ] Micro-animations (hover effects)
- [ ] Responsive design improvements
- [ ] Dark mode toggle

---

## 📋 SPRINT PLAN

### Sprint 1 (Hafta 1-2): Kritik Güvenlik + SignalR

- **Görev 1:** AdminController role authorization ✅
- **Görev 2:** Frontend SignalR UI update ✅
- **Görev 3:** Authorization integration tests ✅
- **Görev 4:** Component tests (SignalR) ✅

**Definition of Done:**

- ✅ Admin approve/reject sadece Admin/StockManager yapabilir (403 test passed)
- ✅ Frontend pending list real-time güncelleniyor
- ✅ Toast notification çalışıyor
- ✅ 5+ test case passed

---

### Sprint 2 (Hafta 3-4): Test Coverage

- **Görev 5:** Concurrent approval tests ✅
- **Görev 6:** SignalR publisher tests ✅
- **Görev 7:** Frontend component tests ✅
- **Görev 8:** E2E test suite (Playwright) ✅

**Target:**

- ✅ Backend coverage %60
- ✅ Frontend coverage %40

---

### Sprint 3 (Hafta 5-6): Performance & Resilience

- **Görev 9:** LogsController keyset pagination ✅
- **Görev 10:** Index optimization ✅
- **Görev 11:** Publish retry/DLQ ✅
- **Görev 12:** Log retention service ✅

**Metrics:**

- ✅ Logs API < 2s response time
- ✅ Retry success rate > %95
- ✅ DB log volume -50%

---

### Sprint 4 (Hafta 7-8): Polish & Production Prep

- **Görev 13:** Monitoring/alerting setup ✅
- **Görev 14:** API documentation ✅
- **Görev 15:** Production security hardening ✅
- **Görev 16:** Load testing ✅

---

## 🚦 STATUS TRACKING

### Yapılanlar ✅

- ✓ Pending workflow merkezi (PendingStockAdjustmentService.CreateAsync)
- ✓ Approve atomic işlem (claim + transaction)
- ✓ SignalR backend publish (IPendingNotificationPublisher)
- ✓ SignalR frontend client (signalr.ts)
- ✓ E2E test script (admin-e2e.ps1)
- ✓ Database migrations (7 adet)
- ✓ Background workers (SyncWorkerService, RetryPendingDbWritesService)
- ✓ JWT authentication
- ✓ Serilog logging (DB persist)

### Devam Ediyor 🔄

- 🔄 AdminController role authorization (IN PROGRESS)
- 🔄 Frontend SignalR UI update (IN PROGRESS)
- 🔄 Test coverage artırımı (30% → 60%)

### Bekliyor ⏳

- ⏳ Publish retry/DLQ
- ⏳ LogsController performance optimization
- ⏳ Log retention policy
- ⏳ Monitoring/alerting

---

## 📁 REFERENCE FILES

### Backend

```
src/Katana.API/Controllers/AdminController.cs (lines 73-127) → Role auth EKLE
src/Katana.Business/Services/PendingStockAdjustmentService.cs → OK ✅
src/Katana.API/Notifications/SignalRNotificationPublisher.cs → Retry EKLE
src/Katana.API/Controllers/LogsController.cs → Pagination optimize et
src/Katana.API/Program.cs → DI setup
```

### Frontend

```
frontend/katana-web/src/components/Admin/PendingAdjustments.tsx (line 135) → UI update EKLE
frontend/katana-web/src/components/Layout/Header.tsx (line 364) → Badge update
frontend/katana-web/src/services/signalr.ts → OK ✅
```

### Tests

```
tests/Katana.Tests/Services/PendingStockAdjustmentServiceTests.cs → OK ✅
tests/Katana.Tests/Services/ConcurrentApprovalTests.cs → EKLE
tests/Katana.Tests/Controllers/AdminControllerAuthTests.cs → EKLE
tests/Katana.Tests/Notifications/SignalRPublisherTests.cs → EKLE
```

### Config

```
src/Katana.API/appsettings.json → JWT, DB connection, retention settings
docker-compose.yml → Container setup
scripts/admin-e2e.ps1 → E2E test
```

---

## 🎯 NEXT ACTIONS

**Bugün yapılacak:**

1. 🔴 AdminController.cs'ye `[Authorize(Roles = "Admin,StockManager")]` ekle
2. 🔴 AuthController.cs'de role claim oluştur
3. 🔴 AdminControllerAuthTests.cs yaz (3 test case)
4. 🔴 E2E script ile doğrula

**Bu hafta:** 5. Frontend PendingAdjustments.tsx UI update 6. Header.tsx badge update 7. Component test setup (Jest)

**Gelecek sprint:** 8. Concurrent approval tests 9. Publish retry/DLQ 10. Performance optimization

---

**Komut (Hızlı Test):**

```powershell
# Backend
dotnet run --project src\Katana.API --urls "http://localhost:5055"

# Frontend (yeni terminal)
cd frontend\katana-web; npm start

# E2E test
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\admin-e2e.ps1
```

---

**Son Güncelleme:** Master branch sync (commit 9963dde)  
**Detaylı Rapor:** Bkz. `IMPLEMENTATION_REPORT.md`
