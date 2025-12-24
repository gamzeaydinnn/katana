# Implementation Plan

- [x] 1. Create StockCardPreparationService interface and DTOs

  - [x] 1.1 Create IStockCardPreparationService interface in Katana.Business.Interfaces

    - Define PrepareStockCardsForOrderAsync method signature
    - _Requirements: 1.1, 1.2_

  - [x] 1.2 Create StockCardPreparationResult and StockCardOperationResult DTOs in Katana.Core.DTOs

    - Include AllSucceeded, TotalLines, SuccessCount, FailedCount, SkippedCount properties
    - Include Results list with SKU, Action, SkartId, Message, Error

    - _Requirements: 2.5, 4.5_

  - [x] 1.3 Write property test for StockCardPreparationResult

    - **Property 7: Response Completeness**
    - **Validates: Requirements 2.5, 4.5**

- [x] 2. Implement StockCardPreparationService

  - [x] 2.1 Create StockCardPreparationService class in Katana.Business.Services

    - Inject ILucaService and ILogger
    - Implement PrepareStockCardsForOrderAsync method
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_

  - [x] 2.2 Implement CheckAndCreateStockCardAsync helper method

    - Call FindStockCardBySkuAsync to check existence
    - Call UpsertStockCardAsync if not exists
    - Handle errors gracefully and continue
    - _Requirements: 1.2, 1.3, 4.1, 4.3_

  - [x] 2.3 Write property test for stock card check coverage

    - **Property 1: Stock Card Check Coverage**
    - **Validates: Requirements 1.1**

  - [x] 2.4 Write property test for error isolation

    - **Property 3: Error Isolation**
    - **Validates: Requirements 1.3, 4.1**

  - [x] 2.5 Write property test for idempotency

    - **Property 4: Idempotency - No Duplicate Creation**
    - **Validates: Requirements 1.5, 5.2**

- [x] 3. Implement stock card request mapping

  - [x] 3.1 Create MapFromOrderLine static method in StockCardPreparationService

    - Map SKU to kartKodu with special char cleaning
    - Map ProductName to kartAdi with special char cleaning
    - Set kartTuru = 1, OlcumBirimiId = 1
    - _Requirements: 3.1, 3.2, 3.4, 3.5_

  - [x] 3.2 Implement CalculateKdvRate helper method

    - Handle percentage (>1) vs decimal (<=1) input
    - Default to 0.20 if null
    - _Requirements: 3.3_

  - [x] 3.3 Implement CleanSpecialChars helper method

    - Replace Ø with O, ø with o
    - Trim whitespace
    - _Requirements: 3.1, 3.2_

  - [x] 3.4 Write property test for data mapping correctness

    - **Property 5: Data Mapping Correctness**
    - **Validates: Requirements 3.1, 3.2, 3.4, 3.5**

  - [x] 3.5 Write property test for KDV rate calculation

    - **Property 6: KDV Rate Calculation**
    - **Validates: Requirements 3.3**

- [x] 4. Checkpoint - Make sure all tests pass

  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Integrate StockCardPreparationService into ApproveOrder

  - [x] 5.1 Register StockCardPreparationService in DI container (Program.cs)

    - Add scoped registration for IStockCardPreparationService
    - _Requirements: 1.1_

  - [x] 5.2 Inject IStockCardPreparationService into SalesOrdersController

    - Add constructor parameter
    - _Requirements: 1.1_

  - [x] 5.3 Update ApproveOrder method to use StockCardPreparationService

    - Call PrepareStockCardsForOrderAsync before Luca invoice creation
    - Include stockCardResults in response
    - _Requirements: 1.1, 1.4, 2.5_

  - [x] 5.4 Add detailed logging for stock card operations

    - Log total lines at start
    - Log each SKU result (exists/created/failed)
    - Log summary at end
    - _Requirements: 2.1, 2.2, 2.3, 2.4_

- [x] 6. Handle duplicate error as success

  - [x] 6.1 Update UpsertStockCardAsync to detect duplicate errors

    - Check for "daha önce kullanılmış" or similar error messages
    - Return success with DuplicateRecords = 1
    - _Requirements: 5.3, 5.5_

  - [x] 6.2 Write property test for duplicate error handling

    - **Property 8: Duplicate Error Handling**
    - **Validates: Requirements 5.3, 5.5**

- [x] 7. Final Checkpoint - Make sure all tests pass

  - Ensure all tests pass, ask the user if questions arise.
