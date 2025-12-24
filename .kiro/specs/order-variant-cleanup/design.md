# Design Document

## Overview

This feature implements a cleanup system for Katana sales orders. The current system incorrectly sends each order line as a separate product to Katana ERP, creating data fragmentation. This design provides a safe cleanup process to:

1. Identify products sent to Katana from orders
2. Delete those products from Katana
3. Reset order approvals for re-processing with correct grouping

**Note**: Luca cleanup will be handled manually by administrators.

The solution consists of three main phases:

1. **Analysis Phase**: Identify products sent to Katana from orders
2. **Cleanup Phase**: Remove order products from Katana ERP
3. **Reset Phase**: Reset order approvals and clear KatanaOrderIds

## Architecture

### System Components

```
┌─────────────────────────────────────────────────────────────┐
│                     Admin Interface                          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │   Cleanup    │  │    Reset     │  │   Monitor    │     │
│  │   Panel      │  │    Panel     │  │   Dashboard  │     │
│  └──────────────┘  └──────────────┘  └──────────────┘     │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                  Cleanup Service Layer                       │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  KatanaCleanupService                                │  │
│  │  - AnalyzeOrderProducts()                            │  │
│  │  - DeleteFromKatana()                                │  │
│  │  - ResetOrderApprovals()                             │  │
│  │  - GenerateCleanupReport()                           │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                  Data Access Layer                           │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │ SalesOrders  │  │OrderMappings │  │  CleanupLog  │     │
│  │  Repository  │  │  Repository  │  │  Repository  │     │
│  └──────────────┘  └──────────────┘  └──────────────┘     │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                  External Systems                            │
│  ┌──────────────┐  ┌──────────────┐                        │
│  │    Katana    │  │   Database   │                        │
│  │     ERP      │  │    Backup    │                        │
│  └──────────────┘  └──────────────┘                        │
└─────────────────────────────────────────────────────────────┘
```

### Data Flow

```
Analysis Phase:
SalesOrders → Filter Approved → Collect SKUs → Group by Order → Report

Cleanup Phase:
SKU List → Call Katana Delete API → Log Results → Summary Report

Reset Phase:
SalesOrders → Clear Approvals → Clear KatanaOrderIds → Clear Mappings → Log
```

## Components and Interfaces

### 1. KatanaCleanupService

Primary service for cleanup operations.

```csharp
public interface IKatanaCleanupService
{
    Task<OrderProductAnalysisResult> AnalyzeOrderProductsAsync();
    Task<KatanaCleanupResult> DeleteFromKatanaAsync(List<string> skus, bool dryRun = true);
    Task<ResetResult> ResetOrderApprovalsAsync(bool dryRun = true);
    Task<CleanupReport> GenerateCleanupReportAsync();
    Task<BackupResult> CreateBackupAsync();
    Task<RollbackResult> RollbackAsync(string backupId);
}

public class OrderProductAnalysisResult
{
    public int TotalApprovedOrders { get; set; }
    public int TotalProductsSentToKatana { get; set; }
    public int UniqueSkuCount { get; set; }
    public List<OrderProductInfo> OrderProducts { get; set; }
    public Dictionary<string, int> SkuDuplicates { get; set; }
}

public class OrderProductInfo
{
    public int OrderId { get; set; }
    public string OrderNo { get; set; }
    public string SKU { get; set; }
    public string ProductName { get; set; }
    public int? KatanaOrderId { get; set; }
    public DateTime ApprovedDate { get; set; }
}

public class KatanaCleanupResult
{
    public bool Success { get; set; }
    public int TotalAttempted { get; set; }
    public int SuccessCount { get; set; }
    public int FailCount { get; set; }
    public List<CleanupError> Errors { get; set; }
    public TimeSpan Duration { get; set; }
}

public class ResetResult
{
    public bool Success { get; set; }
    public int OrdersReset { get; set; }
    public int LinesAffected { get; set; }
    public int MappingsCleared { get; set; }
    public List<ResetError> Errors { get; set; }
    public TimeSpan Duration { get; set; }
}
```

### 2. CleanupRepository

Data access for cleanup operations.

```csharp
public interface ICleanupRepository
{
    Task<List<SalesOrder>> GetApprovedOrdersAsync();
    Task<List<OrderProductInfo>> GetOrderProductsAsync();
    Task ResetOrderAsync(int orderId);
    Task ClearOrderMappingsAsync(int orderId);
    Task LogCleanupOperationAsync(CleanupOperation operation);
}
```

## Data Models

### Order State

```csharp
public class SalesOrder
{
    public int Id { get; set; }
    public string OrderNo { get; set; }
    public int? KatanaOrderId { get; set; }
    public string Status { get; set; } // Pending, Approved, Shipped
    public DateTime? ApprovedDate { get; set; }
    public string? ApprovedBy { get; set; }
    public string? SyncStatus { get; set; }
    public List<SalesOrderLine> Lines { get; set; }
}

public class SalesOrderLine
{
    public int Id { get; set; }
    public int SalesOrderId { get; set; }
    public int? KatanaOrderId { get; set; }
    public string SKU { get; set; }
    public decimal Quantity { get; set; }
    public string ProductName { get; set; }
}
```

### Cleanup Tracking

```csharp
public class CleanupOperation
{
    public int Id { get; set; }
    public string OperationType { get; set; } // Analysis, KatanaCleanup, Reset
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Status { get; set; } // Running, Completed, Failed
    public string? UserId { get; set; }
    public string? Parameters { get; set; }
    public string? Result { get; set; }
    public string? ErrorMessage { get; set; }
}

public class CleanupLog
{
    public int Id { get; set; }
    public int CleanupOperationId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } // Info, Warning, Error
    public string Message { get; set; }
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }
    public string? Details { get; set; }
}
```

## Correctness Properties

_A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees._

### Property 1: Order product identification completeness

_For any_ approved sales order, all products sent to Katana must be identified and included in the analysis report.
**Validates: Requirements 1.1, 1.2**

### Property 2: SKU collection accuracy

_For any_ order product identified, its SKU must be collected and included in the deletion list.
**Validates: Requirements 1.2**

### Property 3: Katana deletion attempt

_For any_ SKU in the deletion list, a Katana API delete call must be attempted.
**Validates: Requirements 2.1**

### Property 4: Deletion logging completeness

_For any_ Katana deletion attempt, a log entry must exist with SKU, result, and timestamp.
**Validates: Requirements 2.2, 2.3**

### Property 5: Dry-run safety

_For any_ cleanup operation in dry-run mode, no actual deletions must occur in Katana.
**Validates: Requirements 2.5**

### Property 6: Order status reset completeness

_For any_ order with Status=Approved, after reset operation Status must be Pending AND ApprovedDate must be null AND ApprovedBy must be null.
**Validates: Requirements 3.1, 3.2**

### Property 7: KatanaOrderId nullification

_For any_ order being reset, all associated SalesOrderLines must have KatanaOrderId set to null.
**Validates: Requirements 3.3**

### Property 8: Mapping cleanup

_For any_ order being reset, all OrderMapping records must be deleted.
**Validates: Requirements 3.4**

### Property 9: Operation logging

_For any_ cleanup operation, a log entry must exist containing operation start time, user, and parameters.
**Validates: Requirements 6.1**

### Property 10: Summary accuracy

_For any_ completed operation, displayed success count plus fail count must equal total attempted count.
**Validates: Requirements 8.2**

## Error Handling

### Error Categories

1. **Katana API Errors**: Failures in Katana delete operations

   - Network timeouts
   - API errors (404, 500, etc.)
   - Authentication failures

2. **Database Errors**: Data access failures

   - Constraint violations
   - Connection failures

3. **Business Logic Errors**: Operation conflicts
   - Invalid state transitions
   - Concurrent modification conflicts

### Error Handling Strategy

```csharp
public class CleanupErrorHandler
{
    public async Task<KatanaCleanupResult> ExecuteWithErrorHandlingAsync(
        Func<Task<KatanaCleanupResult>> operation,
        string operationName)
    {
        var result = new KatanaCleanupResult();
        var sw = Stopwatch.StartNew();

        try
        {
            // Create backup before operation
            var backup = await _backupService.CreateBackupAsync();
            if (!backup.Success)
            {
                result.Success = false;
                result.Errors.Add(new CleanupError
                {
                    Message = "Backup failed - operation aborted",
                    ErrorType = "BackupFailure"
                });
                return result;
            }

            // Execute operation
            result = await operation();

            // Log completion
            await _auditService.LogOperationAsync(operationName, result);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Katana API error during {Operation}", operationName);
            result.Success = false;
            result.Errors.Add(new CleanupError
            {
                Message = "Katana API communication failed",
                ErrorType = "KatanaApiError",
                Details = ex.Message
            });
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during {Operation}", operationName);
            result.Success = false;
            result.Errors.Add(new CleanupError
            {
                Message = "Unexpected error occurred",
                ErrorType = "UnexpectedError",
                Details = ex.Message,
                StackTrace = ex.StackTrace
            });
            return result;
        }
        finally
        {
            sw.Stop();
            result.Duration = sw.Elapsed;
        }
    }
}
```

### Retry Strategy for Katana API

```csharp
public class RetryPolicy
{
    private static readonly AsyncRetryPolicy _katanaRetryPolicy = Policy
        .Handle<HttpRequestException>()
        .Or<TimeoutException>()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
            onRetry: (exception, delay, attempt, context) =>
            {
                _logger.LogWarning(
                    "Katana API retry attempt {Attempt}/3 after {Delay}s",
                    attempt, delay.TotalSeconds);
            });
}
```

## Testing Strategy

### Unit Testing

Unit tests will cover:

- Order product identification logic
- SKU collection and deduplication
- Reset logic
- Error handling paths

Example unit tests:

```csharp
[Fact]
public async Task AnalyzeOrderProducts_WithApprovedOrders_ReturnsCorrectCount()
{
    // Arrange
    var orders = new List<SalesOrder>
    {
        new() { Id = 1, Status = "Approved", Lines = new List<SalesOrderLine>
        {
            new() { SKU = "SKU1" },
            new() { SKU = "SKU2" }
        }},
        new() { Id = 2, Status = "Approved", Lines = new List<SalesOrderLine>
        {
            new() { SKU = "SKU3" }
        }}
    };

    // Act
    var result = await _service.AnalyzeOrderProductsAsync();

    // Assert
    Assert.Equal(2, result.TotalApprovedOrders);
    Assert.Equal(3, result.TotalProductsSentToKatana);
}

[Fact]
public async Task ResetOrder_ClearsApprovalFields()
{
    // Arrange
    var order = new SalesOrder
    {
        Id = 1,
        Status = "Approved",
        ApprovedDate = DateTime.UtcNow,
        ApprovedBy = "admin"
    };

    // Act
    await _service.ResetOrderAsync(order.Id);
    var updated = await _context.SalesOrders.FindAsync(order.Id);

    // Assert
    Assert.Equal("Pending", updated.Status);
    Assert.Null(updated.ApprovedDate);
    Assert.Null(updated.ApprovedBy);
}
```

### Property-Based Testing

Property-based tests will use **xUnit** with **FsCheck** library.

Each property test will run a minimum of 100 iterations.

Example property tests:

```csharp
[Property(MaxTest = 100)]
public Property AllApprovedOrders_AreIdentified(List<SalesOrder> orders)
{
    // Arrange
    var approvedOrders = orders.Where(o => o.Status == "Approved").ToList();

    // Act
    var result = _service.AnalyzeOrderProductsAsync().Result;

    // Assert
    return (result.TotalApprovedOrders == approvedOrders.Count).ToProperty();
}

[Property(MaxTest = 100)]
public Property DryRun_DoesNotDeleteFromKatana(List<string> skus)
{
    // Arrange
    var deleteCalled = false;
    _katanaService.Setup(x => x.DeleteProduct(It.IsAny<string>()))
        .Callback(() => deleteCalled = true);

    // Act
    _service.DeleteFromKatanaAsync(skus, dryRun: true).Wait();

    // Assert
    return (!deleteCalled).ToProperty();
}
```

## Implementation Notes

### Phase 1: Analysis (Read-Only)

1. Query all approved orders
2. Collect SKUs from order lines
3. Identify duplicates
4. Generate report
5. No modifications

### Phase 2: Katana Cleanup (Destructive)

1. Create database backup
2. For each SKU:
   - Call Katana delete API
   - Log result
   - Continue on error
3. Generate summary

### Phase 3: Reset (Destructive)

1. Reset order statuses to Pending
2. Clear approval metadata
3. Nullify KatanaOrderIds
4. Delete order mappings
5. Log all operations

### Rollback Procedure

If cleanup fails:

1. Stop operations
2. Log error details
3. Restore from backup
4. Verify data integrity
5. Notify administrators

### Performance Considerations

- Batch Katana deletions (10 concurrent max)
- Transaction management for resets
- Progress tracking for long operations
- Timeout handling for Katana API calls

### Security Considerations

- Admin-only access
- Audit logging
- Backup verification
- Rate limiting for Katana API
