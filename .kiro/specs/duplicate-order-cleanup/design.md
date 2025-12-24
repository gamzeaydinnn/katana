# Design Document

## Overview

Bu özellik, Katana entegrasyon sistemindeki duplike siparişleri tespit edip temizlemeyi ve gelecekte duplikasyonların önlenmesini sağlar. Ayrıca admin onay akışının (yeşil başparmak → Katana → Luca) doğru çalıştığını garanti eder.

**Mevcut Sorunlar:**

1. Aynı sipariş numarasına sahip birden fazla kayıt var (örn: SO-84 ve SO-SO-84)
2. Bozuk formatlı sipariş numaraları var (SO-SO-84, SO-SO-SO-56)
3. Sync işlemi sırasında mevcut siparişler güncellenmek yerine yeni kayıt oluşturuluyor

**Çözüm:**

1. Duplike siparişleri tespit eden analiz endpoint'i
2. Önizleme ile güvenli silme mekanizması
3. Sync sırasında upsert mantığı (varsa güncelle, yoksa oluştur)
4. Bozuk OrderNo formatlarını düzelten temizlik aracı

## Architecture

### System Components

```
┌─────────────────────────────────────────────────────────────┐
│                  Admin UI (Frontend)                         │
│  - Duplicate Analysis View                                   │
│  - Preview & Confirm Deletion                                │
│  - Cleanup Progress Display                                  │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│            DuplicateOrderCleanupController                   │
│  - GET /api/admin/orders/duplicates/analyze                  │
│  - POST /api/admin/orders/duplicates/cleanup                 │
│  - POST /api/admin/orders/malformed/cleanup                  │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│            DuplicateOrderCleanupService                      │
│  - AnalyzeDuplicatesAsync()                                  │
│  - CleanupDuplicatesAsync()                                  │
│  - CleanupMalformedOrderNosAsync()                           │
│  - ExtractBaseOrderNo()                                      │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                  IntegrationDbContext                        │
│  - SalesOrders                                               │
│  - AuditLogs                                                 │
└─────────────────────────────────────────────────────────────┘
```

### Data Flow - Duplicate Analysis

```
1. Admin requests duplicate analysis
   ↓
2. Service queries all orders grouped by OrderNo
   ↓
3. Groups with count > 1 are identified as duplicates
   ↓
4. For each group:
   - Determine which order to keep (oldest or most advanced status)
   - Mark others for deletion
   ↓
5. Return analysis result with preview
```

### Data Flow - Cleanup

```
1. Admin confirms cleanup
   ↓
2. Service loads duplicate groups
   ↓
3. For each group:
   - Keep the selected order
   - Delete others
   - Log deletion to audit
   ↓
4. Return cleanup result with counts
```

## Components and Interfaces

### 1. DuplicateOrderCleanupController

```csharp
[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = "Admin")]
public class DuplicateOrderCleanupController : ControllerBase
{
    [HttpGet("duplicates/analyze")]
    public async Task<ActionResult<DuplicateAnalysisResult>> AnalyzeDuplicates();

    [HttpPost("duplicates/cleanup")]
    public async Task<ActionResult<CleanupResult>> CleanupDuplicates(
        [FromBody] CleanupRequest request);

    [HttpGet("malformed/analyze")]
    public async Task<ActionResult<MalformedAnalysisResult>> AnalyzeMalformed();

    [HttpPost("malformed/cleanup")]
    public async Task<ActionResult<CleanupResult>> CleanupMalformed(
        [FromBody] CleanupRequest request);
}
```

### 2. DuplicateOrderCleanupService

```csharp
public interface IDuplicateOrderCleanupService
{
    Task<DuplicateAnalysisResult> AnalyzeDuplicatesAsync();
    Task<CleanupResult> CleanupDuplicatesAsync(bool dryRun = true);
    Task<MalformedAnalysisResult> AnalyzeMalformedAsync();
    Task<CleanupResult> CleanupMalformedAsync(bool dryRun = true);
    string ExtractBaseOrderNo(string orderNo);
}
```

### 3. OrderNo Parsing Logic

```csharp
public string ExtractBaseOrderNo(string orderNo)
{
    // Pattern: SO-SO-84 → SO-84
    // Pattern: SO-SO-SO-56 → SO-56
    // Pattern: SO-TO-01 → SO-TO-01 (valid, no change)

    if (string.IsNullOrWhiteSpace(orderNo))
        return orderNo;

    // Regex to detect repeated "SO-" prefix
    var match = Regex.Match(orderNo, @"^(SO-)+(\d+)$");
    if (match.Success)
    {
        return $"SO-{match.Groups[2].Value}";
    }

    // Check for other malformed patterns
    var repeatedMatch = Regex.Match(orderNo, @"^([A-Z]+-)+([A-Z]+-\d+)$");
    if (repeatedMatch.Success)
    {
        return repeatedMatch.Groups[2].Value;
    }

    return orderNo;
}
```

## Data Models

### DuplicateAnalysisResult

```csharp
public class DuplicateAnalysisResult
{
    public int TotalOrders { get; set; }
    public int DuplicateGroups { get; set; }
    public int OrdersToDelete { get; set; }
    public List<DuplicateGroup> Groups { get; set; }
}

public class DuplicateGroup
{
    public string OrderNo { get; set; }
    public int Count { get; set; }
    public DuplicateOrderInfo OrderToKeep { get; set; }
    public List<DuplicateOrderInfo> OrdersToDelete { get; set; }
}

public class DuplicateOrderInfo
{
    public int Id { get; set; }
    public string OrderNo { get; set; }
    public string CustomerName { get; set; }
    public decimal? Total { get; set; }
    public string Status { get; set; }
    public DateTime? CreatedAt { get; set; }
    public long? KatanaOrderId { get; set; }
    public long? LucaOrderId { get; set; }
    public string KeepReason { get; set; } // "Oldest" or "Most Advanced Status"
}
```

### MalformedAnalysisResult

```csharp
public class MalformedAnalysisResult
{
    public int TotalMalformed { get; set; }
    public int CanMerge { get; set; }
    public int CanRename { get; set; }
    public List<MalformedOrderInfo> Orders { get; set; }
}

public class MalformedOrderInfo
{
    public int Id { get; set; }
    public string CurrentOrderNo { get; set; }
    public string CorrectOrderNo { get; set; }
    public string Action { get; set; } // "Merge" or "Rename"
    public int? MergeTargetId { get; set; }
}
```

### CleanupResult

```csharp
public class CleanupResult
{
    public bool Success { get; set; }
    public int OrdersDeleted { get; set; }
    public int OrdersMerged { get; set; }
    public int OrdersRenamed { get; set; }
    public List<string> Errors { get; set; }
    public List<CleanupLogEntry> Log { get; set; }
}

public class CleanupLogEntry
{
    public int OrderId { get; set; }
    public string OrderNo { get; set; }
    public string Action { get; set; }
    public string Details { get; set; }
}
```

## Correctness Properties

_A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees._

### Property 1: Duplicate Detection Completeness

_For any_ set of orders, if two or more orders have the same OrderNo, they must all appear in the same duplicate group.
**Validates: Requirements 1.1**

### Property 2: Keep Selection Consistency

_For any_ duplicate group, exactly one order must be marked as "keep" and all others must be marked as "delete".
**Validates: Requirements 1.2, 2.2**

### Property 3: Status Priority Preservation

_For any_ duplicate group where orders have different statuses, the order with the most advanced status (APPROVED > SHIPPED > PENDING) must be kept.
**Validates: Requirements 1.5**

### Property 4: Data Integrity After Cleanup

_For any_ cleanup operation, the kept order must retain all its original data (lines, customer, totals) unchanged.
**Validates: Requirements 1.3**

### Property 5: Malformed OrderNo Extraction

_For any_ malformed OrderNo (e.g., SO-SO-84), the extracted base OrderNo must be a valid format (e.g., SO-84).
**Validates: Requirements 5.2**

### Property 6: Upsert Idempotency

_For any_ order sync operation, syncing the same order twice must result in exactly one order in the database.
**Validates: Requirements 4.2**

### Property 7: Approved Order Protection

_For any_ order with Status="APPROVED", re-sync from Katana must not modify the order.
**Validates: Requirements 4.4**

## Error Handling

### Error Categories

1. **Analysis Errors**: Database query failures
2. **Cleanup Errors**: Deletion failures, constraint violations
3. **Merge Errors**: Data conflicts during merge

### Error Handling Strategy

```csharp
public async Task<CleanupResult> CleanupDuplicatesAsync(bool dryRun = true)
{
    var result = new CleanupResult { Success = true, Errors = new List<string>() };

    try
    {
        var analysis = await AnalyzeDuplicatesAsync();

        if (dryRun)
        {
            result.OrdersDeleted = analysis.OrdersToDelete;
            return result;
        }

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var group in analysis.Groups)
            {
                foreach (var orderToDelete in group.OrdersToDelete)
                {
                    try
                    {
                        var order = await _context.SalesOrders
                            .Include(o => o.Lines)
                            .FirstOrDefaultAsync(o => o.Id == orderToDelete.Id);

                        if (order != null)
                        {
                            _context.SalesOrderLines.RemoveRange(order.Lines);
                            _context.SalesOrders.Remove(order);
                            result.OrdersDeleted++;

                            // Audit log
                            _auditService.LogDelete("SalesOrder", order.Id.ToString(),
                                User.Identity?.Name ?? "System",
                                $"Duplicate cleanup: kept order {group.OrderToKeep.Id}");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Failed to delete order {orderToDelete.Id}: {ex.Message}");
                    }
                }
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
    catch (Exception ex)
    {
        result.Success = false;
        result.Errors.Add($"Cleanup failed: {ex.Message}");
    }

    return result;
}
```

## Testing Strategy

### Unit Testing

Unit tests will cover:

- OrderNo parsing and extraction logic
- Duplicate detection algorithm
- Keep selection logic (oldest vs status priority)
- Malformed pattern detection

### Property-Based Testing

Property-based tests using FsCheck or similar library:

- Generate random orders with duplicates
- Verify all duplicates are detected
- Verify exactly one order is kept per group
- Verify status priority is respected

### Integration Testing

Integration tests will verify:

- End-to-end cleanup flow
- Database transactions
- Audit logging
- Error recovery

## Implementation Notes

### Phase 1: Analysis Endpoints

1. Create DuplicateOrderCleanupService
2. Implement AnalyzeDuplicatesAsync
3. Implement AnalyzeMalformedAsync
4. Create controller endpoints

### Phase 2: Cleanup Endpoints

1. Implement CleanupDuplicatesAsync with dry-run support
2. Implement CleanupMalformedAsync with dry-run support
3. Add transaction support
4. Add audit logging

### Phase 3: Sync Prevention

1. Update KatanaSalesOrderSyncWorker to use upsert logic
2. Add OrderNo uniqueness check before insert
3. Skip approved orders during sync

### Phase 4: UI Integration

1. Add duplicate analysis view to admin panel
2. Add preview and confirm dialog
3. Add cleanup progress display

</content>
</invoke>
