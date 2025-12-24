# Implementation Plan

- [x] 1. Setup project structure and core interfaces

  - Create KatanaCleanupService interface and implementation
  - Create CleanupRepository interface and implementation
  - Setup dependency injection in Program.cs
  - _Requirements: 1.1, 2.1, 3.1_

- [ ] 2. Implement order product analysis functionality

  - [x] 2.1 Create order product identification logic

    - Query all approved sales orders
    - Collect SKUs from order lines
    - Build OrderProductInfo list with order details
    - _Requirements: 1.1, 1.2_

  - [ ]\* 2.2 Write property test for order product identification

    - **Property 1: Order product identification completeness**
    - **Validates: Requirements 1.1, 1.2**

  - [x] 2.3 Implement SKU deduplication and grouping

    - Identify duplicate SKUs across orders
    - Group products by order
    - Calculate statistics
    - _Requirements: 1.3, 1.4, 1.5_

  - [ ]\* 2.4 Write property test for SKU collection

    - **Property 2: SKU collection accuracy**
    - **Validates: Requirements 1.2**

  - [x] 2.5 Create analysis report generation

    - Implement AnalyzeOrderProductsAsync() method
    - Generate OrderProductAnalysisResult with counts and lists
    - Format report for display
    - _Requirements: 1.3_

  - [ ]\* 2.6 Write unit tests for analysis functionality
    - Test order product identification
    - Test SKU deduplication
    - Test report generation
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_

- [ ] 3. Implement Katana cleanup functionality

  - [x] 3.1 Create Katana delete API integration

    - Implement DeleteProductFromKatanaAsync() method
    - Add retry policy for transient failures
    - Handle Katana API errors gracefully
    - _Requirements: 2.1_

  - [ ]\* 3.2 Write property test for Katana deletion attempt

    - **Property 3: Katana deletion attempt**
    - **Validates: Requirements 2.1**

  - [x] 3.3 Implement deletion logging

    - Log each deletion attempt with SKU and result
    - Track success/failure counts
    - Store error details
    - _Requirements: 2.2, 2.3_

  - [ ]\* 3.4 Write property test for deletion logging

    - **Property 4: Deletion logging completeness**
    - **Validates: Requirements 2.2, 2.3**

  - [x] 3.5 Add dry-run mode support

    - Implement dry-run flag in DeleteFromKatanaAsync()
    - Skip actual API calls in dry-run mode
    - Generate preview report
    - _Requirements: 2.5_

  - [ ]\* 3.6 Write property test for dry-run safety

    - **Property 5: Dry-run safety**
    - **Validates: Requirements 2.5**

  - [x] 3.7 Implement cleanup orchestration

    - Create DeleteFromKatanaAsync() method
    - Add batch processing (10 concurrent deletions)
    - Generate KatanaCleanupResult with statistics
    - _Requirements: 2.4_

  - [ ]\* 3.8 Write unit tests for Katana cleanup
    - Test Katana API integration
    - Test retry logic
    - Test error handling
    - Test dry-run mode
    - Test batch processing
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_

- [ ] 4. Implement order reset functionality

  - [x] 4.1 Create order status reset logic

    - Implement ResetOrderStatusAsync() method
    - Clear Status, ApprovedDate, ApprovedBy, SyncStatus fields
    - _Requirements: 3.1, 3.2_

  - [ ]\* 4.2 Write property test for order status reset

    - **Property 6: Order status reset completeness**
    - **Validates: Requirements 3.1, 3.2**

  - [x] 4.3 Implement KatanaOrderId nullification

    - Create ClearKatanaOrderIdsAsync() method
    - Update all SalesOrderLines for affected orders
    - _Requirements: 3.3_

  - [ ]\* 4.4 Write property test for KatanaOrderId nullification

    - **Property 7: KatanaOrderId nullification**
    - **Validates: Requirements 3.3**

  - [x] 4.5 Implement order mapping cleanup

    - Create ClearOrderMappingsAsync() method
    - Delete OrderMapping records for reset orders
    - _Requirements: 3.4_

  - [ ]\* 4.6 Write property test for mapping cleanup

    - **Property 8: Mapping cleanup**
    - **Validates: Requirements 3.4**

  - [x] 4.7 Create reset orchestration

    - Implement ResetOrderApprovalsAsync() method
    - Add dry-run mode support
    - Generate ResetResult with statistics
    - _Requirements: 3.5_

  - [ ]\* 4.8 Write unit tests for reset functionality
    - Test order status reset
    - Test KatanaOrderId clearing
    - Test mapping cleanup
    - Test reset orchestration
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

- [ ] 5. Implement backup and rollback functionality

  - [x] 5.1 Create database backup service

    - Implement CreateBackupAsync() method
    - Generate unique backup IDs
    - Store backup metadata
    - _Requirements: 7.1_

  - [x] 5.2 Implement rollback mechanism

    - Create RollbackAsync() method
    - Restore database from backup
    - Verify data integrity after restore
    - _Requirements: 7.3, 7.4_

  - [x] 5.3 Add backup validation

    - Prevent operations if backup fails
    - Log backup failures
    - _Requirements: 7.5_

  - [ ]\* 5.4 Write unit tests for backup and rollback
    - Test backup creation
    - Test rollback restoration
    - Test backup failure handling
    - _Requirements: 7.1, 7.3, 7.4, 7.5_

- [ ] 6. Implement logging and audit functionality

  - [ ] 6.1 Create cleanup operation tracking

    - Implement CleanupOperation entity
    - Track operation start, end, status, and results
    - _Requirements: 6.1_

  - [ ]\* 6.2 Write property test for operation logging

    - **Property 9: Operation logging**
    - **Validates: Requirements 6.1**

  - [ ] 6.3 Implement detailed logging

    - Create CleanupLog entity
    - Log Katana deletions with details
    - Log order resets with state transitions
    - Log errors with full context
    - _Requirements: 6.2, 6.3, 6.4_

  - [ ] 6.4 Add summary statistics logging

    - Log operation completion with metrics
    - Track duration, success rate, error count
    - _Requirements: 6.5_

  - [ ]\* 6.5 Write unit tests for logging functionality
    - Test operation tracking
    - Test detailed logging
    - Test summary statistics
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_

- [ ] 7. Implement monitoring and metrics

  - [ ] 7.1 Create metrics calculation service

    - Calculate success rate and error count
    - Calculate KatanaOrderId ratio before/after cleanup
    - _Requirements: 8.2, 8.4_

  - [ ]\* 7.2 Write property test for summary accuracy

    - **Property 10: Summary accuracy**
    - **Validates: Requirements 8.2**

  - [ ] 7.3 Add progress tracking

    - Implement progress percentage calculation
    - Update progress during long operations
    - _Requirements: 8.1_

  - [ ]\* 7.4 Write unit tests for monitoring functionality
    - Test metrics calculation
    - Test progress tracking
    - _Requirements: 8.1, 8.2, 8.4_

- [ ] 8. Create admin API endpoints

  - [x] 8.1 Create KatanaCleanupController

    - Add GET /api/katana-cleanup/analyze endpoint
    - Add POST /api/katana-cleanup/delete-from-katana endpoint
    - Add POST /api/katana-cleanup/reset-orders endpoint
    - Add POST /api/katana-cleanup/backup endpoint
    - Add POST /api/katana-cleanup/rollback endpoint
    - Add authorization (Admin role only)
    - _Requirements: All_

  - [x] 8.2 Add request/response DTOs

    - Create KatanaCleanupRequestDto
    - Create KatanaCleanupResponseDto
    - Add validation attributes
    - _Requirements: All_

  - [x] 8.3 Implement error handling middleware

    - Handle validation errors
    - Handle Katana API errors
    - Handle database errors
    - Return appropriate HTTP status codes
    - _Requirements: All_

  - [ ]\* 8.4 Write integration tests for API endpoints
    - Test analyze endpoint
    - Test delete-from-katana endpoint with dry-run
    - Test reset-orders endpoint with dry-run
    - Test backup and rollback endpoints
    - Test authorization
    - Test error handling
    - _Requirements: All_

- [ ] 9. Create admin UI components

  - [ ] 9.1 Create KatanaCleanupPanel component

    - Display analysis results
    - Show product counts and SKU lists
    - Add "Analyze" button
    - Add "Delete from Katana" button with confirmation
    - Add "Reset Orders" button with confirmation
    - Add dry-run toggle
    - _Requirements: 1.3, 2.4, 3.5_

  - [ ] 9.2 Create MonitoringDashboard component

    - Display real-time progress
    - Show success/fail counts
    - Display error messages
    - Show before/after KatanaOrderId ratio
    - _Requirements: 8.1, 8.2, 8.3, 8.4_

  - [ ] 9.3 Add cleanup operation history

    - Display past cleanup operations
    - Show operation details and results
    - Add filtering and sorting
    - _Requirements: 6.1, 6.5_

  - [ ]\* 9.4 Write UI component tests
    - Test KatanaCleanupPanel rendering and interactions
    - Test MonitoringDashboard data display
    - Test operation history display
    - _Requirements: All_

- [ ] 10. Create database migrations

  - [x] 10.1 Create CleanupOperation table

    - Add Id, OperationType, StartTime, EndTime columns
    - Add Status, UserId, Parameters columns
    - Add Result, ErrorMessage columns
    - _Requirements: 6.1_

  - [x] 10.2 Create CleanupLog table

    - Add Id, CleanupOperationId, Timestamp columns
    - Add Level, Message, EntityType columns
    - Add EntityId, Details columns
    - Add foreign key to CleanupOperation
    - _Requirements: 6.2, 6.3, 6.4_

  - [x] 10.3 Add indexes for performance

    - Index on SalesOrder.Status
    - Index on SalesOrder.KatanaOrderId
    - Index on CleanupOperation.StartTime
    - _Requirements: Performance_

- [ ] 11. Create PowerShell test scripts

  - [x] 11.1 Create test-katana-cleanup-analysis.ps1

    - Call analyze endpoint
    - Display results with SKU list
    - _Requirements: 1.3_

  - [x] 11.2 Create test-katana-cleanup-delete.ps1

    - Call delete-from-katana endpoint with dry-run
    - Display preview results
    - Call delete-from-katana endpoint without dry-run (with confirmation)
    - Display actual results
    - _Requirements: 2.4, 2.5_

  - [x] 11.3 Create test-katana-cleanup-reset.ps1

    - Call reset-orders endpoint with dry-run
    - Display preview results
    - Call reset-orders endpoint without dry-run (with confirmation)
    - Display actual results
    - _Requirements: 3.5_

- [ ] 12. Documentation and deployment

  - [ ] 12.1 Create cleanup operation guide

    - Document analysis phase
    - Document Katana cleanup phase
    - Document reset phase
    - Document rollback procedure
    - Add manual Luca cleanup instructions
    - _Requirements: All_

  - [ ] 12.2 Create troubleshooting guide

    - Document common Katana API errors
    - Document resolution steps
    - Document rollback scenarios
    - _Requirements: All_

  - [ ] 12.3 Update API documentation

    - Document new cleanup endpoints
    - Document request/response formats
    - Document error codes
    - _Requirements: All_

  - [ ] 12.4 Create deployment checklist
    - Database backup verification
    - Migration execution
    - Service deployment
    - Post-deployment validation
    - _Requirements: All_

- [ ] 13. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.
