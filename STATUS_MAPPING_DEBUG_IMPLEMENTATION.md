# Status Mapping Debug Implementation

## Purpose
Add debug logging to track how Katana order status values are being mapped and stored in the database. This will help identify if there are any discrepancies between the status values received from Katana API and what's being stored.

## Problem Context
The `SalesOrder.Status` field is correctly stored as a string (not an enum), but we need to verify:
1. What exact status values Katana API is sending (e.g., "OPEN", "NOT_SHIPPED", "SHIPPED", etc.)
2. Whether the status values are being stored correctly
3. If there are any unexpected status values that need to be handled

## Implementation

### Changes Made

#### 1. SyncController.cs (`SyncSalesOrdersFromKatana` method)

**Added debug log after SalesOrder creation (around line 947):**
```csharp
_context.SalesOrders.Add(salesOrder);
existingKatanaOrderIds.Add(order.Id);
newOrdersCount++;

// 📊 Debug: Status mapping kontrolü
_logger.LogDebug("📊 Order {OrderNo}: Katana Status='{KatanaStatus}' → Stored Status='{StoredStatus}'",
    salesOrder.OrderNo, order.Status, salesOrder.Status);
```

#### 2. KatanaSalesOrderSyncWorker.cs (`SyncSalesOrdersAsync` method)

**Added debug log after SalesOrder creation (around line 341):**
```csharp
context.SalesOrders.Add(salesOrder);
existingKatanaOrderIds.Add(order.Id);
savedSalesOrdersCount++;

// 📊 Debug: Status mapping kontrolü
_logger.LogDebug("📊 Order {OrderNo}: Katana Status='{KatanaStatus}' → Stored Status='{StoredStatus}'",
    salesOrder.OrderNo, order.Status, salesOrder.Status);

_logger.LogDebug("Saved sales order to database: {OrderNo} (KatanaId: {KatanaId})", 
    salesOrder.OrderNo, order.Id);
```

## Expected Log Output

When orders are synced, you should see logs like:

```
📊 Order SO-12345: Katana Status='NOT_SHIPPED' → Stored Status='NOT_SHIPPED'
📊 Order SO-12346: Katana Status='OPEN' → Stored Status='OPEN'
📊 Order SO-12347: Katana Status='SHIPPED' → Stored Status='SHIPPED'
📊 Order SO-12348: Katana Status='null' → Stored Status='NOT_SHIPPED'
```

## What to Look For

### 1. Status Value Variations
Check if Katana sends different status values than expected:
- Expected: `NOT_SHIPPED`, `OPEN`, `SHIPPED`, `DELIVERED`, `CANCELLED`, `DONE`
- Actual: Could be different casing, different words, or null values

### 2. Null Handling
The code has a fallback: `Status = order.Status ?? "NOT_SHIPPED"`
- If you see `Katana Status='null' → Stored Status='NOT_SHIPPED'`, it means Katana didn't provide a status
- This is expected behavior and the fallback is working correctly

### 3. Mapping Issues
If you see unexpected status values:
- Document all unique status values from Katana
- Update the status handling logic if needed
- Consider adding status normalization (e.g., lowercase, trim whitespace)

## Testing Instructions

### 1. Enable Debug Logging
Ensure Debug level logging is enabled in `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Katana.API": "Debug"
    }
  }
}
```

### 2. Trigger Sync Operations

**Manual Sync:**
```bash
POST /api/sync/from-katana/sales-orders
```

**Background Worker:**
- Wait 5 minutes for automatic sync
- Or restart the application to trigger immediate sync

### 3. Review Logs

**Search for the debug emoji:**
```bash
grep "📊" logs/application.log
```

**Or filter by log level:**
```bash
grep "LogDebug.*Order.*Status" logs/application.log
```

### 4. Analyze Results

Create a summary of status values:
```bash
grep "📊 Order" logs/application.log | awk -F"'" '{print $2}' | sort | uniq -c
```

This will show you:
- How many orders have each status value
- Which status values are most common
- Any unexpected or null values

## Example Analysis

If logs show:
```
📊 Order SO-100: Katana Status='not_shipped' → Stored Status='not_shipped'
📊 Order SO-101: Katana Status='NOT_SHIPPED' → Stored Status='NOT_SHIPPED'
📊 Order SO-102: Katana Status='Not Shipped' → Stored Status='Not Shipped'
```

**Problem Identified:** Inconsistent casing from Katana API

**Solution:** Add status normalization:
```csharp
Status = (order.Status ?? "NOT_SHIPPED").ToUpperInvariant().Replace(" ", "_")
```

## Known Status Values (from Katana API Documentation)

Based on the code comments, these are the expected status values:
- `NOT_SHIPPED` - Order created but not yet shipped
- `OPEN` - Order is open and active
- `SHIPPED` - Order has been shipped
- `FULLY_SHIPPED` - All items in order have been shipped
- `DELIVERED` - Order has been delivered
- `CANCELLED` - Order was cancelled
- `DONE` - Order is complete

## Next Steps After Analysis

1. **Document Findings**: Create a list of all unique status values found
2. **Update Code**: If needed, add status normalization or mapping logic
3. **Update Tests**: Add test cases for all discovered status values
4. **Remove Debug Logs**: Once status mapping is confirmed working, these debug logs can be removed or changed to Trace level

## Rollback

If these logs cause performance issues or log spam:
1. Change `LogDebug` to `LogTrace` (only logged when Trace level is enabled)
2. Or remove the debug logs entirely
3. The logs are non-invasive and don't affect functionality

## Related Files

- `src/Katana.Core/Entities/SalesOrder.cs` - SalesOrder entity with Status property
- `src/Katana.Core/DTOs/SalesOrderDto.cs` - DTO from Katana API
- `STATUS_MAPPING_DOCUMENTATION.md` - Existing status mapping documentation
