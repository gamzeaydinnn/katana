# Requirements Document: Admin Order Approval and Luca Synchronization

## Introduction

This specification defines the complete workflow for admin order approval and synchronization to Luca (Koza) system. The system handles two types of orders:

1. **Sales Orders**: Originate from Katana ERP, require admin approval, sync to Katana inventory, then to Luca as invoices
2. **Purchase Orders**: Created manually, require status management, sync to Katana products, then to Luca as invoices

The workflow ensures proper data flow from source systems through approval processes to final synchronization with the external Luca accounting system.

## Glossary

- **Sales_Order**: Customer order synced from Katana ERP that requires admin approval before inventory update
- **Purchase_Order**: Supplier order created manually in the system for procurement management
- **Katana**: Source ERP system that provides sales orders and receives inventory updates
- **Luca**: External accounting/ERP system (Koza) that receives invoice data
- **Admin_Approval**: Process where admin reviews and approves orders, triggering inventory updates
- **Sync_To_Luca**: Process of sending approved orders to Luca as invoice documents
- **Background_Worker**: Automated service that syncs sales orders from Katana every 5 minutes
- **Stock_Adjustment**: Inventory change triggered by order approval
- **Invoice_Sync**: Transfer of order data to Luca in invoice format

## Requirements

### Requirement 1: Sales Order Background Synchronization

**User Story:** As a system, I want to automatically sync sales orders from Katana every 5 minutes, so that new orders are available for admin review without manual intervention.

#### Acceptance Criteria

1. WHEN the background worker runs, THE System SHALL fetch sales orders from Katana API for the last 7 days
2. WHEN processing each order, THE System SHALL check for duplicates using KatanaOrderId before creating new records
3. WHEN saving order lines, THE System SHALL prevent duplicates using ExternalOrderId + SKU + Quantity combination
4. WHEN new orders are detected, THE System SHALL create PendingStockAdjustment records for active orders
5. WHEN sync completes successfully, THE System SHALL trigger stock card synchronization to Luca
6. WHEN new approved orders exist, THE System SHALL automatically send them to Luca as invoices
7. WHEN sync completes, THE System SHALL send SignalR notifications to admin panel with order counts

### Requirement 2: Sales Order Admin Approval

**User Story:** As an admin user, I want to approve sales orders and automatically update Katana inventory, so that approved orders reflect in the ERP system immediately.

#### Acceptance Criteria

1. WHEN an admin clicks approve, THE System SHALL verify the order exists and has not been previously approved
2. WHEN approving an order, THE System SHALL require Admin or Manager role authorization
3. WHEN processing approval, THE System SHALL validate that order lines exist and have valid SKU values
4. WHEN an order line's product exists in Katana, THE System SHALL update the product stock by adding the order quantity
5. WHEN an order line's product does not exist in Katana, THE System SHALL create the product and set initial stock to order quantity
6. WHEN all order lines process successfully, THE System SHALL set order status to "APPROVED"
7. WHEN some order lines fail, THE System SHALL set order status to "APPROVED_WITH_ERRORS" and log specific failures
8. WHEN approval completes, THE System SHALL return detailed results including success count, fail count, and per-item sync results
9. WHEN approval completes, THE System SHALL log the action in audit trail with user information

### Requirement 3: Sales Order Luca Synchronization

**User Story:** As an admin user, I want to sync approved sales orders to Luca as invoices, so that accounting records are updated with customer orders.

#### Acceptance Criteria

1. WHEN initiating Luca sync, THE System SHALL verify the order exists with valid customer information and order lines
2. WHEN checking sync eligibility, THE System SHALL prevent duplicate syncs if order is already synced without errors
3. WHEN preparing Luca request, THE System SHALL map order data to Luca invoice format including BelgeSeri, BelgeNo, CariId, dates, and line items
4. WHEN mapping order lines, THE System SHALL include StokId, Miktar, BirimFiyat, KDVOrani for each item
5. WHEN sync succeeds, THE System SHALL update order with LucaOrderId, set IsSyncedToLuca=true, record LastSyncAt timestamp, and clear LastSyncError
6. WHEN sync fails, THE System SHALL record error message in LastSyncError, set IsSyncedToLuca=false, and update LastSyncAt timestamp
7. WHEN sync completes, THE System SHALL return structured response with success status, Luca order ID, sync timestamp, and error details if applicable

### Requirement 4: Bulk Sales Order Synchronization

**User Story:** As an admin user, I want to sync multiple orders to Luca in parallel, so that I can efficiently process large batches of orders.

#### Acceptance Criteria

1. WHEN initiating bulk sync, THE System SHALL accept maxCount parameter to limit number of orders processed (default 50)
2. WHEN selecting orders, THE System SHALL target orders where IsSyncedToLuca=false and LastSyncError is null
3. WHEN processing orders, THE System SHALL use parallel processing with 5 concurrent requests
4. WHEN processing each order, THE System SHALL call individual SyncToLuca operation
5. WHEN all orders complete, THE System SHALL return summary with totalProcessed, successCount, failCount, durationMs, and rateOrdersPerMinute
6. WHEN errors occur, THE System SHALL include detailed error list in response
7. WHEN processing completes, THE System SHALL log performance metrics for monitoring

### Requirement 5: Purchase Order Status Management

**User Story:** As an admin user, I want to manage purchase order status transitions, so that I can track orders through their lifecycle from pending to received.

#### Acceptance Criteria

1. WHEN updating status, THE System SHALL validate the transition is allowed (Pending→Approved→Received, or any→Cancelled)
2. WHEN transitioning to "Approved" status, THE System SHALL asynchronously trigger Katana product creation/update in background
3. WHEN processing approved status, THE System SHALL check if each product exists in Katana
4. WHEN product exists in Katana, THE System SHALL increase stock by order quantity
5. WHEN product does not exist in Katana, THE System SHALL create new product and set initial stock
6. WHEN transitioning to "Received" status, THE System SHALL create StockMovement records for inventory tracking
7. WHEN status update completes, THE System SHALL update order Status field and UpdatedAt timestamp
8. WHEN invalid transition is attempted, THE System SHALL return error message indicating the invalid state change

### Requirement 6: Purchase Order Luca Synchronization

**User Story:** As an admin user, I want to sync purchase orders to Luca as invoices, so that supplier invoices are recorded in the accounting system.

#### Acceptance Criteria

1. WHEN initiating sync, THE System SHALL verify purchase order exists with valid supplier information
2. WHEN preparing request, THE System SHALL map purchase order to Luca invoice format (not purchase order format)
3. WHEN calling Luca API, THE System SHALL use SendInvoiceAsync method with automatic session renewal
4. WHEN sync succeeds, THE System SHALL set IsSyncedToLuca=true, record LastSyncAt, clear LastSyncError, and reset SyncRetryCount to 0
5. WHEN sync fails, THE System SHALL record error in LastSyncError and increment SyncRetryCount
6. WHEN sync completes, THE System SHALL return response with success status, Luca document number, and message
7. WHEN multiple sync attempts fail, THE System SHALL track retry count for monitoring

### Requirement 7: Bulk Purchase Order Operations

**User Story:** As an admin user, I want to sync multiple purchase orders and retry failed syncs, so that I can efficiently manage large volumes of supplier orders.

#### Acceptance Criteria

1. WHEN initiating bulk sync, THE System SHALL accept maxCount parameter (default 50)
2. WHEN processing bulk sync, THE System SHALL use parallel processing with 5 concurrent requests
3. WHEN initiating retry operation, THE System SHALL accept maxRetries parameter to limit retry attempts
4. WHEN selecting orders for retry, THE System SHALL target orders with LastSyncError not null and SyncRetryCount less than maxRetries
5. WHEN processing retries, THE System SHALL use parallel processing with 3 concurrent requests
6. WHEN operations complete, THE System SHALL return summary statistics including counts and performance metrics

### Requirement 8: API Security and Authorization

**User Story:** As a system administrator, I want proper role-based access control on order operations, so that only authorized users can perform sensitive actions.

#### Acceptance Criteria

1. WHEN approving sales orders, THE System SHALL require Admin or Manager role
2. WHEN syncing sales orders to Luca, THE System SHALL require Admin role
3. WHEN performing bulk operations, THE System SHALL require Admin role
4. WHEN updating Luca fields, THE System SHALL require Admin role
5. WHEN viewing orders and statistics, THE System SHALL allow anonymous access for read operations
6. WHEN unauthorized access is attempted, THE System SHALL return 401 or 403 status code with appropriate message

### Requirement 9: Audit Trail and Logging

**User Story:** As a system administrator, I want comprehensive logging of all order operations, so that I can track changes and troubleshoot issues.

#### Acceptance Criteria

1. WHEN order approval occurs, THE System SHALL log action to audit service with entity type, ID, user name, and description
2. WHEN approval completes, THE System SHALL log info message with order number and result counts
3. WHEN sync operations occur, THE System SHALL log start, progress, and completion with timestamps
4. WHEN errors occur, THE System SHALL log error details with context for troubleshooting
5. WHEN bulk operations complete, THE System SHALL log performance metrics including duration and throughput

### Requirement 10: Error Handling and Recovery

**User Story:** As an admin user, I want clear error messages and recovery options when operations fail, so that I can resolve issues without technical support.

#### Acceptance Criteria

1. WHEN order lines are missing, THE System SHALL return error message instructing to re-sync from Katana
2. WHEN customer/supplier information is missing, THE System SHALL return error indicating which fields are required
3. WHEN duplicate sync is attempted, THE System SHALL return BadRequest with message indicating order already synced
4. WHEN Katana API fails, THE System SHALL record specific error and allow retry
5. WHEN Luca API fails, THE System SHALL record error with session context and allow retry
6. WHEN partial failures occur in approval, THE System SHALL set APPROVED_WITH_ERRORS status and provide detailed failure list
7. WHEN retry operations are available, THE System SHALL provide clear indication of retry count and limits

### Requirement 11: Performance and Scalability

**User Story:** As a system administrator, I want the system to handle high volumes of orders efficiently, so that processing doesn't become a bottleneck.

#### Acceptance Criteria

1. WHEN processing bulk operations, THE System SHALL use parallel processing with configurable concurrency limits
2. WHEN syncing sales orders, THE System SHALL process up to 5 orders concurrently
3. WHEN retrying purchase orders, THE System SHALL process up to 3 orders concurrently
4. WHEN background worker runs, THE System SHALL process up to 100 orders per batch
5. WHEN operations complete, THE System SHALL report performance metrics including duration and throughput (orders per minute)
6. WHEN memory usage is high, THE System SHALL use efficient batch processing to prevent memory issues

### Requirement 12: Data Integrity and Duplicate Prevention

**User Story:** As a system administrator, I want the system to prevent duplicate records and maintain data integrity, so that order data remains consistent across systems.

#### Acceptance Criteria

1. WHEN syncing from Katana, THE System SHALL check KatanaOrderId before creating sales order records
2. WHEN saving order lines, THE System SHALL use ExternalOrderId + SKU + Quantity combination to prevent duplicates
3. WHEN syncing to Luca, THE System SHALL check IsSyncedToLuca flag and LastSyncError to prevent duplicate submissions
4. WHEN creating stock adjustments, THE System SHALL ensure one adjustment per order line
5. WHEN updating order status, THE System SHALL use optimistic concurrency to prevent race conditions

## API Endpoints Reference

### Sales Orders
- `GET /api/sales-orders` - List all sales orders
- `GET /api/sales-orders/{id}` - Get order details
- `GET /api/sales-orders/stats` - Get statistics
- `GET /api/sales-orders/{id}/sync-status` - Get sync status
- `POST /api/sales-orders/{id}/approve` - Approve order (Admin/Manager)
- `POST /api/sales-orders/{id}/sync` - Sync to Luca (Admin)
- `POST /api/sales-orders/sync-all` - Bulk sync (Admin)
- `PATCH /api/sales-orders/{id}/luca-fields` - Update Luca fields (Admin)

### Purchase Orders
- `GET /api/purchase-orders` - List all purchase orders
- `GET /api/purchase-orders/{id}` - Get order details
- `GET /api/purchase-orders/stats` - Get statistics
- `GET /api/purchase-orders/{id}/sync-status` - Get sync status
- `POST /api/purchase-orders` - Create new order
- `POST /api/purchase-orders/{id}/sync` - Sync to Luca
- `POST /api/purchase-orders/sync-all` - Bulk sync
- `POST /api/purchase-orders/retry-failed` - Retry failed syncs
- `PATCH /api/purchase-orders/{id}/status` - Update status
- `PATCH /api/purchase-orders/{id}/luca-fields` - Update Luca fields

## Data Flow Diagrams

### Sales Order Flow
```
Katana ERP (Every 5 min)
    ↓
Background Worker (KatanaSalesOrderSyncWorker)
    ↓
Database (SalesOrders, SalesOrderLines, PendingStockAdjustments)
    ↓
Admin Panel
    ├─→ [Approve] → Katana (Stock Update)
    └─→ [Sync to Luca] → Luca (Invoice)
```

### Purchase Order Flow
```
Manual Creation
    ↓
Database (PurchaseOrders)
    ↓
Admin Panel
    ├─→ [Status: Approved] → Katana (Product Creation)
    ├─→ [Status: Received] → StockMovement (Inventory)
    └─→ [Sync to Luca] → Luca (Invoice)
```

## Notes

- Background worker runs every 5 minutes syncing last 7 days of Katana orders
- All Katana inventory updates are synchronous (not async) to ensure immediate consistency
- Purchase orders are sent to Luca as invoices, not purchase orders
- Parallel processing limits: 5 for sales orders, 3-5 for purchase orders
- Approval operations are irreversible
- Session management for Luca API is handled automatically
- SignalR notifications keep admin panel updated in real-time

## Test Scenarios

### Test Case 1: SO-66 Complete Flow
**Reference**: `test-so-66-approval.sh`

1. Login as admin user
2. Verify SO-66 exists in Katana invoices
3. Confirm order is already approved (from Katana)
4. Trigger Luca synchronization
5. Verify order synced successfully
6. Confirm order appears in Luca system

**Expected Result**: Order flows from Katana → System → Luca without errors

### Test Case 2: Manual Purchase Order Flow
1. Create purchase order with supplier and items
2. Update status to "Approved"
3. Verify products created/updated in Katana
4. Sync to Luca
5. Verify invoice created in Luca

**Expected Result**: Purchase order processed through all stages successfully

### Test Case 3: Bulk Sync Performance
1. Create 50 pending sales orders
2. Execute bulk sync operation
3. Verify all orders processed
4. Check performance metrics (orders per minute)

**Expected Result**: All orders sync with >200 orders/minute throughput

## Success Metrics

- **Sync Success Rate**: >95% of orders sync successfully on first attempt
- **Processing Speed**: >200 orders per minute for bulk operations
- **Error Recovery**: Failed orders can be retried and succeed after data correction
- **Audit Coverage**: 100% of critical operations logged
- **Authorization**: 0 unauthorized access to protected endpoints
- **Duplicate Prevention**: 0 duplicate orders created in Luca

