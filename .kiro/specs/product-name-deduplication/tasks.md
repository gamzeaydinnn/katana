# Implementation Plan

- [x] 1. Set up database schema and migrations

  - Create merge_history table
  - Create keep_separate_groups table
  - Create merge_rollback_data table
  - Add indexes for performance
  - _Requirements: 7.1, 6.2, 7.5_

- [x] 2. Implement core data models and DTOs

  - [x] 2.1 Create DuplicateGroup and ProductSummary models

    - Define DuplicateGroup class with all properties
    - Define ProductSummary class with reference counts
    - _Requirements: 1.1, 1.2, 1.3_

  - [x] 2.2 Create MergeRequest and MergePreview models

    - Define MergeRequest class
    - Define MergePreview class with warnings
    - _Requirements: 3.4, 4.1, 4.4, 4.5_

  - [x] 2.3 Create MergeResult and MergeHistoryEntry models

    - Define MergeResult class
    - Define MergeHistoryEntry class
    - _Requirements: 5.2, 7.1, 7.2_

  - [ ]\* 2.4 Write property test for data models
    - **Property 2: Group count accuracy**
    - **Validates: Requirements 1.2**

- [x] 3. Implement DuplicateAnalysisService

  - [x] 3.1 Implement AnalyzeDuplicatesAsync method

    - Group products by name
    - Calculate reference counts
    - Exclude single-product groups
    - Sort by count descending
    - _Requirements: 1.1, 1.2, 1.4, 1.5_

  - [ ]\* 3.2 Write property test for product grouping

    - **Property 1: Product grouping by name**
    - **Validates: Requirements 1.1**

  - [ ]\* 3.3 Write property test for single product exclusion

    - **Property 4: Single product exclusion**
    - **Validates: Requirements 1.4**

  - [ ]\* 3.4 Write property test for group sorting

    - **Property 5: Group sorting by count**
    - **Validates: Requirements 1.5**

  - [x] 3.5 Implement GetDuplicateGroupDetailAsync method

    - Fetch all products in group
    - Calculate BOM counts
    - Calculate stock movement counts
    - Identify suggested canonical product
    - _Requirements: 2.1, 2.4, 2.5, 3.2, 3.3_

  - [ ]\* 3.6 Write property test for reference count accuracy

    - **Property 7: BOM relationship accuracy**
    - **Property 8: Stock movement count accuracy**
    - **Validates: Requirements 2.4, 2.5**

  - [ ]\* 3.7 Write property test for canonical suggestion

    - **Property 10: Sales order priority**
    - **Property 11: Data completeness priority**
    - **Validates: Requirements 3.2, 3.3**

  - [x] 3.8 Implement FilterDuplicateGroupsAsync method

    - Apply category filter
    - Apply minimum count filter
    - Apply name pattern search
    - Apply SKU pattern search
    - _Requirements: 8.1, 8.2, 8.3, 8.4_

  - [ ]\* 3.9 Write property tests for filtering

    - **Property 30: Category filter accuracy**
    - **Property 31: Minimum count filter accuracy**
    - **Property 32: Name pattern search accuracy**
    - **Property 33: SKU pattern search accuracy**
    - **Validates: Requirements 8.1, 8.2, 8.3, 8.4**

  - [x] 3.10 Implement ExportDuplicateAnalysisAsync method

    - Generate CSV with all groups
    - Include all product attributes
    - Include reference counts
    - Include export metadata
    - _Requirements: 9.1, 9.2, 9.3, 9.4_

  - [ ]\* 3.11 Write property test for export completeness
    - **Property 34: Export completeness**
    - **Property 35: Export metadata**
    - **Validates: Requirements 9.2, 9.3, 9.4**

- [x] 4. Implement MergeValidationService

  - [x] 4.1 Implement ValidateMergeRequestAsync method

    - Validate canonical product exists and is active
    - Validate all merge products exist
    - Check for circular BOM references
    - Check for pending merges
    - Generate specific error messages
    - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5_

  - [ ]\* 4.2 Write property tests for validation

    - **Property 36: Canonical product validation**
    - **Property 37: Merge product existence validation**
    - **Property 38: Circular BOM reference validation**
    - **Property 39: Pending merge validation**
    - **Property 40: Validation error messaging**
    - **Validates: Requirements 10.1, 10.2, 10.3, 10.4, 10.5**

  - [x] 4.3 Implement helper validation methods

    - CanonicalProductExistsAsync
    - HasCircularBOMReferencesAsync
    - IsProductInPendingMergeAsync
    - _Requirements: 10.1, 10.3, 10.4_

- [x] 5. Implement ProductMergeService

  - [x] 5.1 Implement PreviewMergeAsync method

    - Count sales orders to update
    - Count BOMs to update
    - Count stock movements to update
    - Generate warnings for active sales orders
    - Generate critical warnings for unique data
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

  - [ ]\* 5.2 Write property test for preview accuracy

    - **Property 12: Merge preview accuracy**
    - **Property 14: Reference count accuracy in preview**
    - **Property 15: Active sales order warning**
    - **Property 16: Unique data warning**
    - **Validates: Requirements 3.4, 4.1, 4.2, 4.3, 4.4, 4.5**

  - [x] 5.3 Implement ExecuteMergeAsync method

    - Validate merge request
    - Start database transaction
    - Update sales order references
    - Update BOM references
    - Update stock movement references
    - Mark products as inactive
    - Create merge history entry
    - Create rollback data entries
    - Commit transaction
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 7.1_

  - [ ]\* 5.4 Write property tests for merge execution

    - **Property 17: Confirmation requirement**
    - **Property 18: Sales order reference update**
    - **Property 19: BOM reference update**
    - **Property 20: Stock movement reference update**
    - **Property 21: Product inactivation not deletion**
    - **Property 25: Merge history creation**
    - **Validates: Requirements 5.1, 5.2, 5.3, 5.4, 5.5, 7.1**

  - [x] 5.5 Implement RollbackMergeAsync method

    - Fetch rollback data
    - Start database transaction
    - Restore sales order references
    - Restore BOM references
    - Restore stock movement references
    - Reactivate merged products
    - Update merge history status
    - Commit transaction
    - _Requirements: 7.5_

  - [ ]\* 5.6 Write property test for rollback

    - **Property 29: Rollback restoration**
    - **Validates: Requirements 7.5**

  - [x] 5.7 Implement MarkGroupAsKeepSeparateAsync method

    - Create keep_separate_groups entry
    - Store reason and admin user
    - _Requirements: 6.1, 6.3_

  - [ ]\* 5.8 Write property test for keep separate

    - **Property 22: Keep separate exclusion**
    - **Property 23: Keep separate reason persistence**
    - **Validates: Requirements 6.2, 6.3**

  - [x] 5.9 Implement RemoveKeepSeparateFlagAsync method

    - Update keep_separate_groups entry
    - Mark as removed with timestamp and admin user
    - _Requirements: 6.5_

- [x] 6. Implement MergeHistoryService

  - [x] 6.1 Implement CreateMergeHistoryAsync method

    - Insert merge history entry
    - Return history ID
    - _Requirements: 7.1_

  - [x] 6.2 Implement GetMergeHistoryAsync method

    - Fetch merge history with filters
    - Include canonical product details
    - Include admin user details
    - _Requirements: 7.2, 7.3_

  - [ ]\* 6.3 Write property test for history completeness

    - **Property 26: Merge history completeness**
    - **Property 27: Merge history admin tracking**
    - **Property 28: Merge history reference counts**
    - **Validates: Requirements 7.2, 7.3, 7.4**

  - [x] 6.3 Implement GetMergeHistoryDetailAsync method

    - Fetch specific history entry
    - Include all merge details
    - _Requirements: 7.2_

  - [x] 6.4 Implement UpdateMergeHistoryStatusAsync method

    - Update history entry status
    - _Requirements: 7.5_

- [x] 7. Implement API controllers

  - [x] 7.1 Create DeduplicationController

    - GET /api/deduplication/analyze endpoint
    - GET /api/deduplication/groups/{name} endpoint
    - POST /api/deduplication/filter endpoint
    - GET /api/deduplication/export endpoint
    - _Requirements: 1.1, 2.1, 8.1, 9.1_

  - [x] 7.2 Create ProductMergeController

    - POST /api/merge/preview endpoint
    - POST /api/merge/execute endpoint
    - POST /api/merge/rollback/{id} endpoint
    - POST /api/merge/keep-separate endpoint
    - DELETE /api/merge/keep-separate/{name} endpoint
    - _Requirements: 4.1, 5.1, 7.5, 6.1, 6.5_

  - [x] 7.3 Create MergeHistoryController

    - GET /api/merge/history endpoint
    - GET /api/merge/history/{id} endpoint
    - _Requirements: 7.2, 7.3_

  - [ ]\* 7.4 Write integration tests for API endpoints
    - Test end-to-end merge workflow
    - Test validation error responses
    - Test authorization
    - _Requirements: All_

- [ ] 8. Implement frontend components

  - [ ] 8.1 Create DuplicateAnalysis component

    - Display duplicate groups table
    - Show product details
    - Implement filtering UI
    - Implement search UI
    - Add export button
    - _Requirements: 1.1, 1.2, 1.3, 8.1, 9.1_

  - [ ] 8.2 Create MergePreview component

    - Display canonical product selection
    - Show products to merge
    - Display reference counts
    - Show warnings and critical warnings
    - Add confirmation dialog
    - _Requirements: 3.1, 4.1, 4.4, 4.5, 5.1_

  - [ ] 8.3 Create MergeHistory component

    - Display merge history table
    - Show merge details
    - Add rollback button
    - _Requirements: 7.2, 7.3, 7.5_

  - [ ] 8.4 Create KeepSeparate component

    - Display keep separate groups
    - Add reason input
    - Add remove button
    - _Requirements: 6.1, 6.4, 6.5_

  - [ ]\* 8.5 Write component tests
    - Test user interactions
    - Test data display
    - Test error handling
    - _Requirements: All_

- [ ] 9. Implement SignalR notifications

  - [ ] 9.1 Create MergeProgressHub

    - Send progress updates during merge
    - Send completion notifications
    - Send error notifications
    - _Requirements: 5.2_

  - [ ] 9.2 Integrate SignalR in frontend
    - Connect to MergeProgressHub
    - Display progress bar
    - Show real-time updates
    - _Requirements: 5.2_

- [ ] 10. Add caching and performance optimizations

  - [ ] 10.1 Implement duplicate analysis caching

    - Cache analysis results for 5 minutes
    - Invalidate cache on product changes
    - _Requirements: 1.1_

  - [ ] 10.2 Optimize reference counting queries

    - Use batch queries
    - Add database indexes
    - _Requirements: 2.4, 2.5_

  - [ ] 10.3 Implement async processing for large merges
    - Queue large merge operations
    - Process in background
    - Send notifications on completion
    - _Requirements: 5.2_

- [ ] 11. Add monitoring and logging

  - [ ] 11.1 Implement metrics tracking

    - Track duplicate group count
    - Track merge operation duration
    - Track success/failure rates
    - _Requirements: All_

  - [ ] 11.2 Implement comprehensive logging

    - Log all merge operations
    - Log validation failures
    - Log rollback operations
    - _Requirements: 7.1, 10.5_

  - [ ] 11.3 Set up alerts
    - Alert on merge failures
    - Alert on high rollback rate
    - Alert on slow operations
    - _Requirements: All_

- [ ] 12. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.
