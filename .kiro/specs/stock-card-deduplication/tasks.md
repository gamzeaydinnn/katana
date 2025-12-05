# Implementation Plan: Stock Card Deduplication

- [x] 1. Create core data models and DTOs

  - Create `DuplicateAnalysisResult`, `DuplicateGroup`, `StockCardInfo` DTOs
  - Create `DeduplicationPreview`, `DeduplicationAction` DTOs
  - Create `DeduplicationExecutionResult`, `ExecutionError` DTOs
  - Create `DeduplicationRules`, `CanonicalSelectionRule` DTOs
  - Create enums: `DuplicateCategory`, `ActionType`, `RuleType`, `ExportFormat`
  - _Requirements: 1.4, 5.3, 6.5, 7.2_

- [x] 2. Implement duplicate detection service

  - [x] 2.1 Create `IDuplicateDetector` interface and implementation

    - Implement `DetectDuplicates` method to group stock cards by name
    - Implement `CategorizeDuplicate` method for classification
    - _Requirements: 1.2, 1.3_

  - [ ]\* 2.2 Write property test for duplicate identification

    - **Property 1: Duplicate identification by name**
    - **Validates: Requirements 1.2**

  - [ ]\* 2.3 Write property test for categorization

    - **Property 2: Duplicate categorization completeness**
    - **Validates: Requirements 1.3**

  - [x] 2.4 Implement version suffix detection

    - Create regex pattern for "-V\d+" detection
    - Implement version number extraction and parsing
    - _Requirements: 2.1_

  - [ ]\* 2.5 Write property test for version detection

    - **Property 4: Version suffix detection**
    - **Validates: Requirements 2.1**

  - [x] 2.6 Implement versioned card grouping and sorting

    - Group by base code (code without version suffix)
    - Sort by version number ascending
    - _Requirements: 2.2, 2.3_

  - [ ]\* 2.7 Write property test for versioned grouping

    - **Property 5: Versioned card grouping and sorting**
    - **Validates: Requirements 2.2, 2.3**

  - [x] 2.8 Implement concatenation error detection

    - Detect when first half of string equals second half
    - Handle separators (-, \_, space)
    - Generate corrected values
    - _Requirements: 3.1, 3.2, 3.4_

  - [ ]\* 2.9 Write property test for concatenation detection

    - **Property 7: Concatenation error detection**
    - **Validates: Requirements 3.1, 3.2**

  - [ ]\* 2.10 Write property test for concatenation reporting

    - **Property 8: Concatenation error reporting**
    - **Validates: Requirements 3.4**

  - [x] 2.11 Implement character encoding issue detection

    - Detect question marks in Turkish text
    - Implement similarity matching for encoding variants
    - Group cards with similar names differing only in encoding
    - _Requirements: 4.1, 4.2_

  - [ ]\* 2.12 Write property test for encoding detection

    - **Property 9: Character encoding issue detection**
    - **Validates: Requirements 4.1**

  - [ ]\* 2.13 Write property test for encoding grouping
    - **Property 10: Encoding similarity grouping**
    - **Validates: Requirements 4.2**

- [x] 3. Implement canonical selection service

  - [x] 3.1 Create `ICanonicalSelector` interface and implementation

    - Implement rule evaluation engine
    - Implement priority-based rule selection
    - Implement default rule (shortest code)
    - _Requirements: 5.2, 7.2, 7.3, 7.4_

  - [ ]\* 3.2 Write property test for rule-based selection

    - **Property 12: Canonical selection by rules**
    - **Validates: Requirements 5.2, 7.2, 7.3**

  - [ ]\* 3.3 Write property test for default selection

    - **Property 13: Default canonical selection**
    - **Validates: Requirements 7.4**

  - [x] 3.4 Implement specific rule types

    - `PreferNoVersionSuffix`: Select card without version suffix
    - `PreferLowerVersion`: Select lowest version number
    - `PreferShorterCode`: Select shortest stock code
    - `PreferNoSpecialCharacters`: Avoid special chars
    - `PreferCorrectEncoding`: Select properly encoded Turkish
    - _Requirements: 7.2_

- [ ] 4. Implement deduplication service orchestration

  - [ ] 4.1 Create `IDeduplicationService` interface and implementation

    - Implement `AnalyzeDuplicatesAsync` method
    - Integrate with `LucaService` to fetch stock cards
    - Call `DuplicateDetector` for analysis
    - Generate statistics
    - _Requirements: 1.1, 1.4_

  - [ ]\* 4.2 Write property test for analysis report structure

    - **Property 3: Analysis report structure**
    - **Validates: Requirements 1.4**

  - [ ]\* 4.3 Write property test for version count accuracy

    - **Property 6: Version count accuracy**
    - **Validates: Requirements 2.4**

  - [ ] 4.4 Implement `GeneratePreviewAsync` method

    - Call `CanonicalSelector` for each duplicate group
    - Generate `DeduplicationAction` for each group
    - Include reasons and corrected values
    - Calculate preview statistics
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_

  - [ ]\* 4.5 Write property test for preview completeness

    - **Property 11: Preview completeness**
    - **Validates: Requirements 5.3, 5.4, 5.5**

  - [ ] 4.6 Implement `ExecuteDeduplicationAsync` method

    - Verify canonical card exists before each deletion
    - Call Luca API to delete duplicate stock cards
    - Track successes, failures, and errors
    - Halt on first error
    - Generate execution summary
    - _Requirements: 6.2, 6.3, 6.4, 6.5_

  - [ ]\* 4.7 Write property test for execution following preview

    - **Property 14: Execution follows preview**
    - **Validates: Requirements 6.2**

  - [ ]\* 4.8 Write property test for canonical existence check

    - **Property 15: Canonical existence check**
    - **Validates: Requirements 6.3**

  - [ ]\* 4.9 Write property test for execution summary

    - **Property 16: Execution summary completeness**
    - **Validates: Requirements 6.5**

  - [ ] 4.10 Implement rules management
    - Implement `GetRulesAsync` to load from configuration
    - Implement `UpdateRulesAsync` to update configuration
    - _Requirements: 7.1, 7.5_

- [ ] 5. Implement export service

  - [ ] 5.1 Create `IExportService` interface and implementation

    - Implement `ExportToJsonAsync` method
    - Implement `ExportToCsvAsync` method
    - _Requirements: 8.1, 8.2_

  - [ ] 5.2 Implement JSON export

    - Serialize `DuplicateAnalysisResult` to JSON
    - Preserve hierarchical structure
    - Save to file or return content
    - _Requirements: 8.4, 8.5_

  - [ ]\* 5.3 Write property test for JSON structure preservation

    - **Property 17: Export structure preservation (JSON)**
    - **Validates: Requirements 8.4**

  - [ ] 5.4 Implement CSV export

    - Flatten duplicate groups to rows
    - Include columns: stock code, name, category, group ID, action
    - Generate CSV with headers
    - _Requirements: 8.3, 8.5_

  - [ ]\* 5.5 Write property test for CSV column completeness
    - **Property 18: Export column completeness (CSV)**
    - **Validates: Requirements 8.3**

- [ ] 6. Create deduplication controller

  - [ ] 6.1 Create `DeduplicationController` with all endpoints

    - POST `/api/admin/deduplication/analyze`
    - POST `/api/admin/deduplication/preview`
    - POST `/api/admin/deduplication/execute`
    - GET `/api/admin/deduplication/export?format=json|csv`
    - GET `/api/admin/deduplication/rules`
    - PUT `/api/admin/deduplication/rules`
    - Add `[Authorize(Roles = "Admin")]` attribute
    - _Requirements: All_

  - [ ] 6.2 Implement error handling and responses
    - Return appropriate HTTP status codes
    - Use `ErrorResponse` DTO for errors
    - Log all operations with admin user ID
    - _Requirements: All_

- [ ] 7. Add configuration and dependency injection

  - [ ] 7.1 Add deduplication rules to appsettings.json

    - Define default rules with priorities
    - Define default rule (shortest code)
    - _Requirements: 7.1, 7.4_

  - [ ] 7.2 Register services in DI container
    - Register `IDeduplicationService`
    - Register `IDuplicateDetector`
    - Register `ICanonicalSelector`
    - Register `IExportService`
    - Configure options for `DeduplicationRules`

- [ ] 8. Checkpoint - Ensure all tests pass

  - Ensure all tests pass, ask the user if questions arise.

- [ ] 9. Add Luca service integration for stock card deletion

  - [ ] 9.1 Add delete stock card method to `LucaService`

    - Implement `DeleteStockCardAsync(long skartId)` method
    - Handle authentication and error responses
    - _Requirements: 6.2_

  - [ ]\* 9.2 Write unit tests for delete operation
    - Test successful deletion
    - Test error handling
    - Test authentication retry

- [ ] 10. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.
