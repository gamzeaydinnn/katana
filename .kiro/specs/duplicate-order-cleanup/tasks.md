# Implementation Plan

- [x] 1. Create DuplicateOrderCleanupService

  - [x] 1.1 Create service interface and implementation

    - Create IDuplicateOrderCleanupService interface
    - Create DuplicateOrderCleanupService class
    - Register in DI container
    - _Requirements: 1.1, 1.2_

  - [x] 1.2 Implement ExtractBaseOrderNo method

    - Parse malformed OrderNo patterns (SO-SO-84 → SO-84)
    - Handle edge cases (valid formats, null/empty)
    - Add unit tests for parsing logic
    - _Requirements: 5.2_

  - [ ]\* 1.3 Write property test for OrderNo extraction

    - **Property 5: Malformed OrderNo Extraction**
    - **Validates: Requirements 5.2**

  - [x] 1.4 Implement AnalyzeDuplicatesAsync method

    - Query orders grouped by OrderNo
    - Identify groups with count > 1
    - Determine which order to keep (status priority, then oldest)
    - Return DuplicateAnalysisResult
    - _Requirements: 1.1, 1.2, 1.5, 2.1, 2.2, 2.3_

  - [ ]\* 1.5 Write property test for duplicate detection

    - **Property 1: Duplicate Detection Completeness**
    - **Validates: Requirements 1.1**

  - [ ]\* 1.6 Write property test for keep selection

    - **Property 2: Keep Selection Consistency**
    - **Validates: Requirements 1.2, 2.2**

  - [ ]\* 1.7 Write property test for status priority
    - **Property 3: Status Priority Preservation**
    - **Validates: Requirements 1.5**

- [x] 2. Implement cleanup functionality

  - [x] 2.1 Implement CleanupDuplicatesAsync method

    - Support dry-run mode for preview
    - Delete duplicate orders with transaction
    - Preserve kept order data
    - Log all deletions to audit
    - _Requirements: 1.3, 1.4, 2.4, 2.5_

  - [ ]\* 2.2 Write property test for data integrity

    - **Property 4: Data Integrity After Cleanup**
    - **Validates: Requirements 1.3**

  - [x] 2.3 Implement AnalyzeMalformedAsync method

    - Find orders with malformed OrderNo patterns
    - Determine action (merge or rename)
    - Return MalformedAnalysisResult
    - _Requirements: 5.1, 5.3, 5.4_

  - [x] 2.4 Implement CleanupMalformedAsync method

    - Merge malformed orders with base orders
    - Rename standalone malformed orders
    - Report cleanup counts
    - _Requirements: 5.3, 5.4, 5.5_

- [x] 3. Create API controller

  - [x] 3.1 Create DuplicateOrderCleanupController

    - Add GET /api/admin/orders/duplicates/analyze endpoint
    - Add POST /api/admin/orders/duplicates/cleanup endpoint
    - Add GET /api/admin/orders/malformed/analyze endpoint
    - Add POST /api/admin/orders/malformed/cleanup endpoint
    - Require Admin role authorization
    - _Requirements: 2.1, 2.4_

  - [x] 3.2 Add request/response DTOs

    - Create CleanupRequest DTO (dryRun flag)
    - Create DuplicateAnalysisResult DTO
    - Create MalformedAnalysisResult DTO
    - Create CleanupResult DTO
    - _Requirements: 2.2, 2.3_

- [x] 4. Update sync logic to prevent future duplicates

  - [x] 4.1 Update KatanaSalesOrderSyncWorker

    - Check for existing order by OrderNo before insert
    - Update existing order instead of creating new
    - Skip approved orders during sync
    - Log create vs update operations
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

  - [ ]\* 4.2 Write property test for upsert idempotency

    - **Property 6: Upsert Idempotency**
    - **Validates: Requirements 4.2**

  - [ ]\* 4.3 Write property test for approved order protection
    - **Property 7: Approved Order Protection**
    - **Validates: Requirements 4.4**

- [x] 5. Checkpoint - Ensure all tests pass

  - Ensure all tests pass, ask the user if questions arise.

- [x] 6. Create test scripts

  - [x] 6.1 Create test-duplicate-analysis.ps1

    - Call analyze endpoint
    - Display duplicate groups
    - Show which orders will be kept/deleted
    - _Requirements: 2.1, 2.2, 2.3_

  - [x] 6.2 Create test-duplicate-cleanup.ps1

    - Run dry-run first
    - Confirm and run actual cleanup
    - Display results
    - _Requirements: 2.4, 2.5_

  - [x] 6.3 Create test-malformed-cleanup.ps1

    - Analyze malformed orders
    - Run cleanup
    - Verify results
    - _Requirements: 5.1, 5.3, 5.4, 5.5_

- [x] 7. Final Checkpoint - Ensure all tests pass

  - Ensure all tests pass, ask the user if questions arise.

</content>
</invoke>
