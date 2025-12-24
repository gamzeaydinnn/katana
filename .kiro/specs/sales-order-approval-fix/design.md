# Design Document

## Overview

This feature fixes the sales order approval process to correctly send orders to Katana as single orders with multiple lines, rather than creating separate products for each order line. The current implementation incorrectly calls `SyncProductStockAsync` for each line, which creates individual products. The correct approach is to use `CreateSalesOrderAsync` to send the entire order with all lines in one API call.

**Current Flow (WRONG)**:

```
Admin Approves Order
  ↓
For Each Order Line:
  → SyncProductStockAsync(SKU, Quantity)
  → Creates separate product in Katana
  → Creates stock adjustment
  ↓
Result: N products for N lines
```

**New Flow (CORRECT)**:

```
Admin Approves Order
  ↓
Build Katana Order Payload:
  → Order Header (customer, dates, etc.)
  → All Lines as sales_order_rows[]
  ↓
CreateSalesOrderAsync(order)
  ↓
Store KatanaOrderId
  ↓
Send to Luca as single invoice
```

## Architecture

### System Components

```
┌─────────────────────────────────────────────────────────────┐
│                  Admin UI (Frontend)                         │
│  - Approve Order Button                                      │
│  - Order Status Display                                      │
│  - Error Messages                                            │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│            SalesOrdersController.ApproveOrder()              │
│  - Validate order data                                       │
│  - Build Katana order payload                                │
│  - Call KatanaService.CreateSalesOrderAsync()                │
│  - Store KatanaOrderId                                       │
│  - Call LucaService.CreateSalesOrderInvoiceAsync()           │
│  - Update order status                                       │
└─────────────────────────────────────────────────────────────┘
                            │
                ┌───────────┴───────────┐
                ▼                       ▼
┌─────────────────────────┐  ┌─────────────────────────┐
│    KatanaService        │  │    LucaService          │
│  CreateSalesOrderAsync()│  │  CreateSalesOrder       │
│                         │  │  InvoiceAsync()         │
└─────────────────────────┘  └─────────────────────────┘
                │                       │
                ▼                       ▼
┌─────────────────────────┐  ┌─────────────────────────┐
│    Katana ERP API       │  │    Luca/Koza API        │
│  POST /sales_orders     │  │  POST /satislar         │
└─────────────────────────┘  └─────────────────────────┘
```

### Data Flow

```
1. Admin clicks "Approve" on order
   ↓
2. Controller validates:
   - Order exists
   - Order status is Pending
   - Order has lines
   - Each line has SKU and quantity
   ↓
3. Build Katana payload:
   {
     order_no: "SO-12345",
     customer_id: 123,
     sales_order_rows: [
       { variant_id: 1, quantity: 10, price_per_unit: 100 },
       { variant_id: 2, quantity: 5, price_per_unit: 150 }
     ]
   }
   ↓
4. Send to Katana API
   ↓
5. Receive KatanaOrderId
   ↓
6. Update database:
   - SalesOrder.KatanaOrderId = returned ID
   - SalesOrder.Status = "APPROVED"
   - All SalesOrderLines.KatanaOrderId = same ID
   ↓
7. Send to Luca as single invoice
   ↓
8. Update Luca sync status
```

## Components and Interfaces

### 1. SalesOrdersController (Modified)

```csharp
[HttpPost("{id}/approve")]
[Authorize(Roles = "Admin,Manager")]
public async Task<ActionResult> ApproveOrder(int id)
{
    // 1. Load order with lines and customer
    var order = await _context.SalesOrders
        .Include(s => s.Lines)
        .Include(s => s.Customer)
        .FirstOrDefaultAsync(s => s.Id == id);

    // 2. Validate
    var validation = ValidateOrderForApproval(order);
    if (!validation.IsValid)
        return BadRequest(validation.ErrorMessage);

    // 3. Check for duplicate approval
    if (order.Status == "APPROVED" || order.KatanaOrderId.HasValue)
        return BadRequest("Order already approved");

    // 4. Build Katana order
    var katanaOrder = BuildKatanaOrderFromSalesOrder(order);

    // 5. Send to Katana
    var katanaResult = await _katanaService.CreateSalesOrderAsync(katanaOrder);
    if (katanaResult == null)
        return StatusCode(500, "Failed to create order in Katana");

    // 6. Update database
    await using var tx = await _context.Database.BeginTransactionAsync();
    try
    {
        order.KatanaOrderId = katanaResult.Id;
        order.Status = "APPROVED";
        order.ApprovedDate = DateTime.UtcNow;
        order.ApprovedBy = User.Identity?.Name;
        order.UpdatedAt = DateTime.UtcNow;

        // Update all lines with same KatanaOrderId
        foreach (var line in order.Lines)
        {
            line.KatanaOrderId = katanaResult.Id;
        }

        await _context.SaveChangesAsync();
        await tx.CommitAsync();
    }
    catch
    {
        await tx.RollbackAsync();
        throw;
    }

    // 7. Send to Luca (non-blocking, errors logged but don't fail approval)
    try
    {
        var depoKodu = await _locationMappingService
            .GetDepoKoduByLocationIdAsync(order.LocationId?.ToString() ?? "");
        var lucaResult = await _lucaService
            .CreateSalesOrderInvoiceAsync(order, depoKodu);

        if (lucaResult.IsSuccess && lucaResult.LucaOrderId.HasValue)
        {
            order.LucaOrderId = lucaResult.LucaOrderId;
            order.IsSyncedToLuca = true;
            order.LastSyncError = null;
        }
        else
        {
            order.LastSyncError = lucaResult.ErrorDetails;
        }

        order.LastSyncAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Luca sync failed for order {OrderId}", id);
        order.LastSyncError = ex.Message;
        await _context.SaveChangesAsync();
    }

    return Ok(new
    {
        success = true,
        message = "Order approved successfully",
        katanaOrderId = order.KatanaOrderId,
        lucaOrderId = order.LucaOrderId,
        lucaSyncStatus = order.IsSyncedToLuca ? "synced" : "error"
    });
}
```

### 2. Order Validation

```csharp
private OrderValidationResult ValidateOrderForApproval(SalesOrder? order)
{
    if (order == null)
        return OrderValidationResult.Fail("Order not found");

    if (order.Lines == null || order.Lines.Count == 0)
        return OrderValidationResult.Fail("Order has no lines");

    foreach (var line in order.Lines)
    {
        if (string.IsNullOrWhiteSpace(line.SKU))
            return OrderValidationResult.Fail($"Line {line.Id} has no SKU");

        if (line.Quantity <= 0)
            return OrderValidationResult.Fail($"Line {line.Id} has invalid quantity");
    }

    if (order.Customer == null)
        return OrderValidationResult.Fail("Order has no customer");

    return OrderValidationResult.Success();
}

public class OrderValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }

    public static OrderValidationResult Success() =>
        new() { IsValid = true };

    public static OrderValidationResult Fail(string message) =>
        new() { IsValid = false, ErrorMessage = message };
}
```

### 3. Katana Order Builder

```csharp
private SalesOrderDto BuildKatanaOrderFromSalesOrder(SalesOrder order)
{
    return new SalesOrderDto
    {
        OrderNo = order.OrderNo,
        CustomerId = order.CustomerId,
        LocationId = order.LocationId,
        DeliveryDate = order.DeliveryDate,
        Currency = order.Currency ?? "TRY",
        Status = "NOT_SHIPPED",
        AdditionalInfo = order.AdditionalInfo,
        CustomerRef = order.CustomerRef,

        // Map all lines to sales_order_rows
        SalesOrderRows = order.Lines.Select(line => new SalesOrderRowDto
        {
            VariantId = line.VariantId ?? 0,
            Quantity = line.Quantity,
            PricePerUnit = line.PricePerUnit,
            TaxRateId = line.TaxRateId,
            LocationId = line.LocationId ?? order.LocationId,
            Attributes = new List<SalesOrderRowAttributeDto>()
        }).ToList(),

        // Include addresses if available
        Addresses = new List<SalesOrderAddressDto>()
    };
}
```

### 4. Variant Resolution

Since order lines may not have `VariantId`, we need to resolve it from SKU:

```csharp
private async Task<long?> ResolveVariantIdFromSKU(string sku)
{
    // Try to find variant in Katana by SKU
    var variants = await _katanaService.GetVariantsAsync();
    var variant = variants.FirstOrDefault(v =>
        v.SKU?.Equals(sku, StringComparison.OrdinalIgnoreCase) == true);

    if (variant != null)
        return variant.Id;

    // If not found, create product first
    var product = new KatanaProductDto
    {
        Name = $"Product {sku}",
        SKU = sku,
        Unit = "pcs",
        IsActive = true
    };

    var created = await _katanaService.CreateProductAsync(product);
    return created?.Id;
}
```

## Data Models

### SalesOrder (Updated)

```csharp
public class SalesOrder
{
    public int Id { get; set; }
    public string OrderNo { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    // Katana integration
    public long? KatanaOrderId { get; set; }  // ✅ Single ID for entire order

    // Luca integration
    public long? LucaOrderId { get; set; }
    public bool IsSyncedToLuca { get; set; }
    public string? LastSyncError { get; set; }
    public DateTime? LastSyncAt { get; set; }

    // Status
    public string Status { get; set; } // Pending, Approved, Shipped
    public DateTime? ApprovedDate { get; set; }
    public string? ApprovedBy { get; set; }

    // Lines
    public List<SalesOrderLine> Lines { get; set; }

    // Other fields...
}
```

### SalesOrderLine (Updated)

```csharp
public class SalesOrderLine
{
    public int Id { get; set; }
    public int SalesOrderId { get; set; }

    // Katana integration
    public long? KatanaOrderId { get; set; }  // ✅ Same as parent order
    public long? VariantId { get; set; }      // Katana variant ID

    // Product info
    public string SKU { get; set; }
    public string ProductName { get; set; }
    public decimal Quantity { get; set; }
    public decimal? PricePerUnit { get; set; }

    // Other fields...
}
```

## Correctness Properties

_A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees._

### Property 1: Single Katana order per approval

_For any_ sales order approval, exactly one Katana order must be created regardless of the number of lines.
**Validates: Requirements 1.1**

### Property 2: All lines included

_For any_ sales order with N lines, the Katana order must contain exactly N sales_order_rows.
**Validates: Requirements 1.2**

### Property 3: KatanaOrderId consistency

_For any_ approved sales order, all SalesOrderLines must have the same KatanaOrderId as the parent SalesOrder.
**Validates: Requirements 1.5**

### Property 4: No product creation during approval

_For any_ order approval, no calls to SyncProductStockAsync or CreateProductAsync should occur.
**Validates: Requirements 1.3**

### Property 5: Validation before submission

_For any_ order approval attempt, if validation fails, no Katana API call should be made.
**Validates: Requirements 3.4**

### Property 6: Duplicate prevention

_For any_ order with Status="APPROVED" or KatanaOrderId != null, approval attempts must be rejected.
**Validates: Requirements 6.1, 6.2**

### Property 7: Transaction atomicity

_For any_ order approval, if database update fails, the order Status must remain unchanged.
**Validates: Requirements 5.2**

### Property 8: Luca sync independence

_For any_ order approval, if Luca sync fails, the order must still be marked as APPROVED with KatanaOrderId set.
**Validates: Requirements 4.4**

## Error Handling

### Error Categories

1. **Validation Errors**: Invalid order data

   - Missing lines
   - Invalid SKU
   - Invalid quantity
   - Missing customer

2. **Katana API Errors**: Failures in Katana order creation

   - Network timeouts
   - API errors (400, 422, 500)
   - Authentication failures

3. **Database Errors**: Data persistence failures

   - Transaction rollback
   - Constraint violations

4. **Luca API Errors**: Failures in Luca invoice creation
   - Network timeouts
   - API errors
   - Authentication failures

### Error Handling Strategy

```csharp
public async Task<ActionResult> ApproveOrder(int id)
{
    try
    {
        // 1. Validation errors - return 400
        var validation = ValidateOrderForApproval(order);
        if (!validation.IsValid)
            return BadRequest(new {
                success = false,
                message = validation.ErrorMessage
            });

        // 2. Duplicate check - return 400
        if (order.Status == "APPROVED")
            return BadRequest(new {
                success = false,
                message = "Order already approved"
            });

        // 3. Katana API call - catch and return 500
        SalesOrderDto? katanaResult;
        try
        {
            katanaResult = await _katanaService.CreateSalesOrderAsync(katanaOrder);
            if (katanaResult == null)
                throw new Exception("Katana returned null");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Katana API error for order {OrderId}", id);
            return StatusCode(500, new {
                success = false,
                message = "Failed to create order in Katana",
                error = ex.Message
            });
        }

        // 4. Database update - use transaction
        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            // Update order and lines
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _logger.LogError(ex, "Database error for order {OrderId}", id);
            return StatusCode(500, new {
                success = false,
                message = "Failed to save order status"
            });
        }

        // 5. Luca sync - log errors but don't fail
        try
        {
            await SyncToLuca(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Luca sync error for order {OrderId}", id);
            // Don't return error - order is still approved
        }

        return Ok(new { success = true, katanaOrderId = order.KatanaOrderId });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error approving order {OrderId}", id);
        return StatusCode(500, new {
            success = false,
            message = "Unexpected error occurred"
        });
    }
}
```

## Testing Strategy

### Unit Testing

Unit tests will cover:

- Order validation logic
- Katana order payload building
- Error handling paths
- Transaction rollback scenarios

Example unit tests:

```csharp
[Fact]
public void ValidateOrderForApproval_WithNoLines_ReturnsInvalid()
{
    // Arrange
    var order = new SalesOrder { Id = 1, Lines = new List<SalesOrderLine>() };

    // Act
    var result = ValidateOrderForApproval(order);

    // Assert
    Assert.False(result.IsValid);
    Assert.Contains("no lines", result.ErrorMessage);
}

[Fact]
public void BuildKatanaOrder_WithMultipleLines_CreatesCorrectPayload()
{
    // Arrange
    var order = new SalesOrder
    {
        OrderNo = "SO-123",
        Lines = new List<SalesOrderLine>
        {
            new() { SKU = "SKU1", Quantity = 10, VariantId = 1 },
            new() { SKU = "SKU2", Quantity = 5, VariantId = 2 }
        }
    };

    // Act
    var katanaOrder = BuildKatanaOrderFromSalesOrder(order);

    // Assert
    Assert.Equal(2, katanaOrder.SalesOrderRows.Count);
    Assert.Equal(10, katanaOrder.SalesOrderRows[0].Quantity);
    Assert.Equal(5, katanaOrder.SalesOrderRows[1].Quantity);
}
```

### Integration Testing

Integration tests will verify:

- End-to-end approval flow
- Katana API integration
- Database transactions
- Luca sync behavior

```csharp
[Fact]
public async Task ApproveOrder_WithValidOrder_CreatesKatanaOrder()
{
    // Arrange
    var order = await CreateTestOrder();

    // Act
    var response = await _client.PostAsync($"/api/sales-orders/{order.Id}/approve", null);

    // Assert
    Assert.True(response.IsSuccessStatusCode);
    var updated = await _context.SalesOrders.FindAsync(order.Id);
    Assert.Equal("APPROVED", updated.Status);
    Assert.NotNull(updated.KatanaOrderId);
}
```

## Implementation Notes

### Phase 1: Remove Old Logic

1. Remove `SyncProductStockAsync` calls from `ApproveOrder`
2. Remove loop over order lines
3. Remove individual product creation

### Phase 2: Add New Logic

1. Add order validation
2. Add Katana order builder
3. Add `CreateSalesOrderAsync` call
4. Add KatanaOrderId storage

### Phase 3: Update Database

1. Ensure `SalesOrder.KatanaOrderId` column exists
2. Ensure `SalesOrderLine.KatanaOrderId` column exists
3. Add indexes for performance

### Phase 4: Update UI

1. Display KatanaOrderId in order list
2. Show approval status
3. Display error messages

### Migration Strategy

**For existing approved orders**:

- They will have `KatanaOrderId = null`
- They can be re-approved if needed
- Or leave as-is (historical data)

**For new orders**:

- All will use new approval flow
- All will have KatanaOrderId

## Performance Considerations

- Single API call instead of N calls (N = number of lines)
- Reduced network overhead
- Faster approval process
- Less load on Katana API

**Before**: 10 lines = 10 API calls = ~5 seconds
**After**: 10 lines = 1 API call = ~0.5 seconds

## Security Considerations

- Admin/Manager role required for approval
- Transaction-based updates prevent partial failures
- Audit logging for all approvals
- Error messages don't expose sensitive data

## Rollback Plan

If issues occur:

1. Revert controller changes
2. Restore old `ApproveOrder` implementation
3. Database schema unchanged (backward compatible)
4. No data loss

## Success Criteria

✅ One Katana order per approval
✅ All lines included in single order
✅ KatanaOrderId stored correctly
✅ Luca sync works with new structure
✅ Error handling robust
✅ Performance improved
✅ No duplicate orders created
