# Implementation Plan

- [x] 1. Create DTOs and Interfaces

  - [x] 1.1 Create ArchiveSyncResult, ProductArchivePreview, and ArchiveError DTOs in `src/Katana.Core/DTOs/ArchiveSyncDtos.cs`

    - _Requirements: 1.5, 2.2, 2.3_

  - [x] 1.2 Create IKatanaArchiveSyncService interface in `src/Katana.Business/Interfaces/IKatanaArchiveSyncService.cs`

    - _Requirements: 1.1, 2.1_

- [x] 2. Extend KatanaService with Archive Method

  - [x] 2.1 Add ArchiveProductAsync method to IKatanaService interface

    - _Requirements: 1.4_

  - [x] 2.2 Implement ArchiveProductAsync in KatanaService using PATCH with `{"is_archived": true}`

    - _Requirements: 1.3, 1.4_

  - [x] 2.3 Write property test for archive method

    - **Property 2: Preview Mode Immutability**
    - **Validates: Requirements 2.1, 3.2**

- [x] 3. Implement KatanaArchiveSyncService

  - [x] 3.1 Create KatanaArchiveSyncService class with constructor injection

    - _Requirements: 1.1_

  - [x] 3.2 Implement GetArchivePreviewAsync method - fetch and compare products

    - _Requirements: 1.1, 1.2, 2.1, 2.2, 2.3_

  - [x] 3.3 Write property test for SKU matching

    - **Property 1: SKU Matching Correctness**
    - **Validates: Requirements 1.2, 1.3**

  - [x] 3.4 Implement SyncArchiveAsync method with preview and execute modes

    - _Requirements: 1.3, 1.5, 3.2, 3.3_

  - [x] 3.5 Write property test for archive summary consistency

    - **Property 3: Archive Summary Consistency**
    - **Validates: Requirements 1.5**

  - [x] 3.6 Write property test for partial failure resilience

    - **Property 4: Partial Failure Resilience**
    - **Validates: Requirements 3.4**

  - [x] 3.7 Write property test for preview result completeness

    - **Property 5: Preview Result Completeness**
    - **Validates: Requirements 2.2, 2.3**

- [x] 4. Checkpoint - Make sure all tests are passing

  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Create API Controller

  - [x] 5.1 Create KatanaArchiveSyncController with authentication

    - _Requirements: 3.1_

  - [x] 5.2 Implement POST /sync endpoint with previewOnly parameter

    - _Requirements: 3.2, 3.3_

  - [x] 5.3 Implement GET /preview endpoint

    - _Requirements: 2.1_

- [x] 6. Register Services in DI

  - [x] 6.1 Register IKatanaArchiveSyncService in Program.cs

    - _Requirements: 1.1_

- [x] 7. Add Error Handling and Logging

  - [x] 7.1 Implement rate limiting retry logic with exponential backoff

    - _Requirements: 4.2_

  - [x] 7.2 Add comprehensive logging for errors and summary

    - _Requirements: 4.1, 4.3_

- [ ] 8. Final Checkpoint - Make sure all tests are passing

  - Ensure all tests pass, ask the user if questions arise.
