# Implementation Plan

- [x] 1. Update SalesOrdersController.ApproveOrder method

  - [x] 1.1 Remove old stock sync logic

    - Remove foreach loop over order lines
    - Remove SyncProductStockAsync calls
    - Remove individual product creation logic
    - _Requirements: 1.1, 1.3_

  - [x] 1.2 Add order validation

    - Implement ValidateOrderForApproval method
    - Check order exists and has lines
    - Validate each line has SKU and positive quantity
    - Validate customer exists
    - _Requirements: 3.1, 3.2, 3.3_

  - [x] 1.3 Add duplicate approval check

    - Check if order.Status == "APPROVED"
    - Check if order.KatanaOrderId is not null
    - Return error if already approved
    - _Requirements: 6.1, 6.2, 6.3_

  - [x] 1.4 Implement Katana order builder

    - Create BuildKatanaOrderFromSalesOrder method
    - Map order header fields (OrderNo, CustomerId, etc.)
    - Map all lines to sales_order_rows array
    - Include variant_id, quantity, price_per_unit for each line
    - _Requirements: 1.2, 2.1, 2.2, 2.3, 2.4_

  - [x] 1.5 Add variant ID resolution

    - Create ResolveVariantIdFromSKU method
    - Query Katana API to find variant by SKU
    - Handle case where variant doesn't exist
    - Cache variant lookups for performance
    - _Requirements: 2.2_

  - [x] 1.6 Implement Katana order creation

    - Call \_katanaService.CreateSalesOrderAsync with built order
    - Handle null response from Katana
    - Log request and response payloads
    - _Requirements: 1.1, 1.2_

  - [x] 1.7 Update database with transaction

    - Begin database transaction
    - Set order.KatanaOrderId from Katana response
    - Set order.Status = "APPROVED"
    - Set order.ApprovedDate and ApprovedBy
    - Update all lines with same KatanaOrderId
    - Commit transaction
    - Rollback on error
    - _Requirements: 1.4, 1.5_

  - [x] 1.8 Add Luca sync (non-blocking)

    - Call LucaService.CreateSalesOrderInvoiceAsync
    - Update order.LucaOrderId if successful
    - Set order.IsSyncedToLuca flag
    - Log errors but don't fail approval
    - Store LastSyncError if sync fails
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

  - [x] 1.9 Implement error handling

    - Catch validation errors and return 400
    - Catch Katana API errors and return 500
    - Catch database errors and rollback
    - Log all errors with context
    - Return detailed error messages
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_

  - [x] 1.10 Add comprehensive logging

    - Log approval start with order ID and user
    - Log Katana request payload
    - Log Katana response
    - Log Luca request payload
    - Log Luca response
    - Log final approval status
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

- [ ] 2. Update data models if needed

  - [ ] 2.1 Verify SalesOrder.KatanaOrderId column

    - Check if column exists in database
    - Add migration if missing
    - Ensure nullable long type
    - _Requirements: 1.4_

  - [ ] 2.2 Verify SalesOrderLine.KatanaOrderId column

    - Check if column exists in database
    - Add migration if missing
    - Ensure nullable long type
    - _Requirements: 1.5_

  - [ ] 2.3 Add database indexes
    - Add index on SalesOrder.KatanaOrderId
    - Add index on SalesOrder.Status
    - Add index on SalesOrderLine.KatanaOrderId
    - _Requirements: Performance_

- [ ] 3. Update frontend UI

  - [ ] 3.1 Update SalesOrders component

    - Display KatanaOrderId in order list
    - Show approval status badge
    - Display Luca sync status
    - Show error messages if present
    - _Requirements: 8.1, 8.2, 8.3, 8.4_

  - [ ] 3.2 Update order detail view

    - Show KatanaOrderId in header
    - Display all lines with their sync status
    - Show Luca sync status and error
    - Add manual Luca retry button if sync failed
    - _Requirements: 8.5_

  - [ ] 3.3 Update approval button
    - Disable if order already approved
    - Show loading state during approval
    - Display success/error messages
    - Refresh order data after approval
    - _Requirements: 8.1_

- [ ] 4. Add helper methods and utilities

  - [ ] 4.1 Create OrderValidationResult class

    - Add IsValid property
    - Add ErrorMessage property
    - Add Success() static method
    - Add Fail(message) static method
    - _Requirements: 3.4_

  - [ ] 4.2 Create variant caching service

    - Cache variant lookups by SKU
    - Set cache expiration (5 minutes)
    - Implement cache invalidation
    - _Requirements: Performance_

  - [ ] 4.3 Add approval audit logging
    - Log to AuditService
    - Include order ID, user, timestamp
    - Include Katana and Luca IDs
    - Include approval result
    - _Requirements: 7.1, 7.5_

- [x] 5. Write unit tests

  - [x] 5.1 Test order validation

    - Test with no lines - should fail
    - Test with empty SKU - should fail
    - Test with zero quantity - should fail
    - Test with no customer - should fail
    - Test with valid order - should pass
    - _Requirements: 3.1, 3.2, 3.3_

  - [x] 5.2 Test Katana order builder

    - Test with single line - should create 1 row
    - Test with multiple lines - should create N rows
    - Test field mapping (OrderNo, CustomerId, etc.)
    - Test line mapping (VariantId, Quantity, Price)
    - _Requirements: 1.2, 2.1, 2.2, 2.3, 2.4_

  - [x] 5.3 Test duplicate approval prevention

    - Test with Status="APPROVED" - should reject
    - Test with KatanaOrderId set - should reject
    - Test with Status="Pending" - should allow
    - _Requirements: 6.1, 6.2, 6.3_

  - [x] 5.4 Test error handling

    - Test Katana API failure - should return 500
    - Test database failure - should rollback
    - Test Luca failure - should still approve
    - _Requirements: 5.1, 5.2, 5.3, 5.4_

- [x] 6. Write integration tests

  - [x] 6.1 Test end-to-end approval flow

    - Create test order with multiple lines
    - Call approve endpoint
    - Verify Katana order created
    - Verify database updated correctly
    - Verify all lines have same KatanaOrderId
    - _Requirements: 1.1, 1.2, 1.4, 1.5_

  - [x] 6.2 Test Luca sync integration

    - Approve order
    - Verify Luca invoice created
    - Verify LucaOrderId stored
    - Verify IsSyncedToLuca flag set
    - _Requirements: 4.1, 4.2, 4.3, 4.5_

  - [x] 6.3 Test error recovery

    - Test with invalid Katana credentials
    - Test with network timeout
    - Test with database constraint violation
    - Verify proper error messages returned
    - _Requirements: 5.1, 5.2, 5.3, 5.5_

- [x] 7. Create test scripts

  - [x] 7.1 Create test-order-approval.ps1

    - Create test order via API
    - Call approve endpoint
    - Display Katana response
    - Display Luca response
    - Show final order status
    - _Requirements: All_

  - [x] 7.2 Create test-order-approval-validation.ps1

    - Test with invalid orders
    - Test with missing lines
    - Test with invalid SKUs
    - Verify error messages
    - _Requirements: 3.1, 3.2, 3.3, 3.4_

  - [x] 7.3 Create test-duplicate-approval.ps1

    - Approve same order twice
    - Verify second attempt rejected
    - Display error message
    - _Requirements: 6.1, 6.2, 6.4_

- [x] 8. Update documentation

  - [x] 8.1 Update API documentation

    - Document new approval flow
    - Document request/response format
    - Document error codes
    - Add example payloads
    - _Requirements: All_

  - [x] 8.2 Create migration guide

    - Document changes from old to new flow
    - Explain impact on existing orders
    - Provide rollback instructions
    - _Requirements: All_

  - [x] 8.3 Update user guide

    - Document new approval process
    - Explain KatanaOrderId field
    - Explain Luca sync status
    - Add troubleshooting section
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5_

- [ ] 9. Deployment and verification

  - [ ] 9.1 Deploy to test environment

    - Build and deploy backend
    - Build and deploy frontend
    - Run database migrations
    - Verify services running
    - _Requirements: All_

  - [ ] 9.2 Run smoke tests

    - Approve test order
    - Verify Katana order created
    - Verify Luca invoice created
    - Check database state
    - _Requirements: All_

  - [ ] 9.3 Monitor logs

    - Check for errors in logs
    - Verify logging is working
    - Check Katana API calls
    - Check Luca API calls
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

  - [ ] 9.4 Performance testing
    - Approve orders with many lines (10, 50, 100)
    - Measure response times
    - Compare with old implementation
    - Verify no performance regression
    - _Requirements: Performance_

- [ ] 10. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.
