# Implementation Plan

- [-] 1. Create/Update DTOs for Stock Card Creation

  - [x] 1.1 Create LucaCreateStockCardRequest DTO with new API format

    - Add required fields: kartAdi, kartKodu, baslangicTarihi, kartTuru
    - Add optional fields: kartTipi, kartAlisKdvOran, olcumBirimiId, kategoriAgacKod, barkod
    - Add tevkifat fields: alisTevkifatOran, satisTevkifatOran, alisTevkifatTipId, satisTevkifatTipId
    - Add boolean flags with defaults: satilabilirFlag=1, satinAlinabilirFlag=1, lotNoFlag=0
    - _Requirements: 2.1, 2.2, 2.3_

  - [x] 1.2 Create LucaCreateStockCardResponse DTO

    - Add fields: skartId, error, message
    - _Requirements: 1.3, 3.3_

  - [ ] 1.3 Write property test for response round-trip serialization
    - **Property 3: Response parsing round-trip**
    - **Validates: Requirements 1.3, 3.3**

- [-] 2. Implement Stock Card Validator

  - [x] 2.1 Create IStockCardValidator interface and ValidationResult class

    - Define Validate method signature
    - _Requirements: 1.2, 5.1, 5.2, 5.3, 5.4_

  - [ ] 2.2 Implement StockCardValidator class
    - Validate kartKodu is not empty
    - Validate kartAdi is not empty
    - Validate kartTuru is 1 or 2
    - Validate baslangicTarihi format (dd/MM/yyyy)
    - _Requirements: 5.1, 5.2, 5.3, 5.4_
  - [ ] 2.3 Write property test for valid request validation
    - **Property 1: Valid requests produce success responses**
    - **Validates: Requirements 1.1, 1.2**
  - [ ] 2.4 Write property test for invalid field validation
    - **Property 2: Invalid required fields produce validation errors**
    - **Validates: Requirements 1.2, 5.1, 5.2, 5.3, 5.4**

- [ ] 3. Implement Date Formatting Utility

  - [x] 3.1 Create date formatting helper for baslangicTarihi

    - Format DateTime to "dd/MM/yyyy" string
    - Parse "dd/MM/yyyy" string to DateTime
    - _Requirements: 3.1_

  - [ ] 3.2 Write property test for date format consistency
    - **Property 5: Date format consistency**
    - **Validates: Requirements 3.1**

- [ ] 4. Update LucaService with CreateStockCardV2Async

  - [x] 4.1 Add CreateStockCardV2Async method to LucaService.StockCards.cs

    - Call validator before sending request
    - Serialize request to JSON with correct property names
    - Send POST request to EkleStkWsKart.do endpoint
    - Parse response and return LucaCreateStockCardResponse
    - _Requirements: 1.1, 1.3, 1.4, 3.2, 4.1_

  - [x] 4.2 Add authentication and retry logic

    - Ensure authenticated before request
    - Re-authenticate on 401 and retry
    - Handle branch selection if required
    - _Requirements: 4.1, 4.2, 4.3_

  - [ ] 4.3 Write property test for request serialization
    - **Property 4: Request serialization preserves all fields**
    - **Validates: Requirements 2.1, 3.2**
  - [ ] 4.4 Write property test for default values
    - **Property 6: Default values are applied**
    - **Validates: Requirements 2.3**

- [x] 5. Update ILucaService Interface

  - [x] 5.1 Add CreateStockCardV2Async method signature to ILucaService

    - _Requirements: 1.1_

- [x] 6. Update API Controller

  - [x] 6.1 Add new endpoint to KozaStockCardsController

    - POST /api/admin/koza/stock-cards/v2
    - Accept LucaCreateStockCardRequest
    - Return LucaCreateStockCardResponse
    - _Requirements: 1.1, 1.3, 1.4_

- [ ] 7. Checkpoint - Ensure all tests pass

  - Ensure all tests pass, ask the user if questions arise.

- [ ] 8. Write Integration Tests

  - [ ] 8.1 Write integration test for successful stock card creation
    - Mock Luca API response
    - Verify request format
    - _Requirements: 1.1, 1.3_
  - [ ] 8.2 Write integration test for error handling
    - Test validation errors
    - Test API error responses
    - _Requirements: 1.2, 1.4_

- [ ] 9. Final Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.
