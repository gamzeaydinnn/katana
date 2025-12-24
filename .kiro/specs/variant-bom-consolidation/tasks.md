# Implementation Plan

## Phase 1: Core Infrastructure

- [x] 1. Create data models and DTOs

  - [x] 1.1 Create ProductVariantGroup and VariantDetail DTOs

    - Create `src/Katana.Core/DTOs/VariantGroupDtos.cs`
    - Define ProductVariantGroup, VariantDetail, VariantAttribute classes
    - _Requirements: 1.1, 1.2_

  - [ ] 1.2 Create BOM-related DTOs

    - Create `src/Katana.Core/DTOs/BOMDtos.cs`
    - Define BOMRequirementResult, BOMLineRequirement, MaterialRequirement, StockShortage classes

    - _Requirements: 4.1, 4.2_

  - [ ] 1.3 Create SKU validation DTOs
    - Create `src/Katana.Core/DTOs/SKUValidationDtos.cs`
    - Define SKUValidationResult, SKUComponents, SKURenameResult, SKURenamePreview classes
    - _Requirements: 6.1, 6.2_

## Phase 2: Variant Grouping Service

- [x] 2. Implement IVariantGroupingService

  - [x] 2.1 Create interface and service class

    - Create `src/Katana.Business/Interfaces/IVariantGroupingService.cs`
    - Create `src/Katana.Business/Services/VariantGroupingService.cs`
    - _Requirements: 1.1_

  - [x] 2.2 Implement GroupVariantsByProductAsync method

    - Query variants with Include(v => v.Product)
    - Group by ProductId, calculate totals
    - Handle orphan variants (null ProductId)
    - _Requirements: 1.1, 1.2, 1.3, 1.4_

  - [ ]\* 2.3 Write property test for variant grouping completeness
    - **Property 1: Variant Grouping Completeness**
    - **Validates: Requirements 1.1, 1.2**
  - [ ]\* 2.4 Write property test for single variant products
    - **Property 2: Single Variant Products Display**
    - **Validates: Requirements 1.3**
  - [ ]\* 2.5 Write property test for orphan variant detection
    - **Property 3: Orphan Variant Detection**
    - **Validates: Requirements 1.4**

- [x] 3. Checkpoint - Ensure all tests pass

  - Ensure all tests pass, ask the user if questions arise.

## Phase 3: Deduplication Service Enhancement

- [x] 4. Extend existing DeduplicationService

  - [x] 4.1 Add variant-aware duplicate detection

    - Extend `src/Katana.Business/Services/Deduplication/DeduplicationService.cs`
    - Add DetectVariantDuplicatesAsync method
    - Use fuzzy matching on product names
    - _Requirements: 2.1, 2.2_

  - [x] 4.2 Implement MergeVariantsAsync method

    - Transfer order lines to canonical product
    - Transfer stock movements to canonical product
    - Update Luca mappings
    - Mark merged products as inactive
    - _Requirements: 2.3, 2.4_

  - [ ] 4.3 Implement active order protection
    - Add GetActiveOrdersForProductAsync method
    - Check for non-shipped, non-cancelled orders
    - Return blocking order references
    - _Requirements: 2.5_
  - [ ]\* 4.4 Write property test for duplicate detection consistency
    - **Property 4: Duplicate Detection Consistency**
    - **Validates: Requirements 2.1**
  - [ ]\* 4.5 Write property test for merge data integrity
    - **Property 5: Merge Data Integrity**
    - **Validates: Requirements 2.3, 2.4**
  - [ ]\* 4.6 Write property test for active order protection
    - **Property 6: Active Order Protection**
    - **Validates: Requirements 2.5**

- [ ] 5. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Phase 4: Order Consolidation

- [x] 6. Enhance order creation for multi-variant support

  - [x] 6.1 Update SalesOrdersController for multi-variant orders

    - Modify `src/Katana.API/Controllers/SalesOrdersController.cs`
    - Ensure multiple variants can be added to single order
    - Store variant-specific attributes per line
    - _Requirements: 3.1, 3.2_

  - [x] 6.2 Update order summary display logic

    - Add grouping by parent product in order details
    - Calculate subtotals per product group
    - _Requirements: 3.4_

  - [ ]\* 6.3 Write property test for multi-variant order persistence
    - **Property 7: Multi-Variant Order Persistence**
    - **Validates: Requirements 3.1, 3.2**

- [x] 7. Verify Luca invoice sync for multi-variant orders

  - [x] 7.1 Verify MappingHelper generates correct DetayList

    - Review `src/Katana.Core/Helpers/MappingHelper.cs`
    - Ensure each order line becomes separate DetayList item
    - Add variant attributes to Aciklama field
    - _Requirements: 3.3, 7.1, 7.2_

  - [ ]\* 7.2 Write property test for invoice line completeness
    - **Property 8: Invoice Line Completeness**
    - **Validates: Requirements 3.3, 7.1**
  - [ ]\* 7.3 Write property test for invoice description contains variant attributes
    - **Property 14: Invoice Description Contains Variant Attributes**
    - **Validates: Requirements 7.2**

- [ ] 8. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Phase 5: BOM Service

- [x] 9. Implement IBOMService

  - [x] 9.1 Create interface and service class

    - Create `src/Katana.Business/Interfaces/IBOMService.cs`
    - Create `src/Katana.Business/Services/BOMService.cs`
    - _Requirements: 4.1_

  - [x] 9.2 Implement GetBOMComponentsAsync method

    - Query BOM data from Katana API
    - Map to BOMComponent DTOs
    - _Requirements: 4.2_

  - [ ] 9.3 Implement CalculateBOMRequirementsAsync method

    - Load order with lines
    - For each line, get BOM components
    - Multiply component quantities by order quantity
    - Aggregate total material requirements

    - _Requirements: 4.1, 4.2_

  - [ ] 9.4 Implement DetectShortagesAsync method
    - Compare required quantities with current stock
    - Flag items where required > available
    - Calculate shortage amounts
    - _Requirements: 4.3_
  - [ ]\* 9.5 Write property test for BOM calculation accuracy
    - **Property 9: BOM Calculation Accuracy**
    - **Validates: Requirements 4.1**
  - [ ]\* 9.6 Write property test for shortage detection correctness
    - **Property 10: Shortage Detection Correctness**
    - **Validates: Requirements 4.2, 4.3**

- [x] 10. Create BOM API endpoints

  - [-] 10.1 Create BOMController

    - Create `src/Katana.API/Controllers/BOMController.cs`
    - Add GET /api/bom/order/{orderId}/requirements endpoint
    - Add GET /api/bom/product/{variantId}/components endpoint
    - _Requirements: 4.1, 4.2, 4.3_

- [ ] 11. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Phase 6: Manufacturing Order Sync

- [x] 12. Implement IManufacturingOrderSyncService

  - [x] 12.1 Create interface and service class

    - Create `src/Katana.Business/Interfaces/IManufacturingOrderSyncService.cs`
    - Create `src/Katana.Business/Services/ManufacturingOrderSyncService.cs`
    - _Requirements: 5.1_

  - [x] 12.2 Implement SyncManufacturingOrderToLucaAsync method

    - Map manufacturing order to Luca production document
    - Include consumed materials list
    - Handle scrap/waste quantities
    - _Requirements: 5.1, 5.2, 5.3_

  - [x] 12.3 Implement retry logic with exponential backoff

    - Use Polly for retry policy
    - Notify admin after 3 failed attempts
    - _Requirements: 5.4_

  - [ ]\* 12.4 Write property test for production stock movement direction
    - **Property 11: Production Stock Movement Direction**
    - **Validates: Requirements 5.1, 5.2**

- [ ] 13. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Phase 7: SKU Validation Service

- [x] 14. Implement ISKUValidationService

  - [x] 14.1 Create interface and service class

    - Create `src/Katana.Business/Interfaces/ISKUValidationService.cs`
    - Create `src/Katana.Business/Services/SKUValidationService.cs`
    - _Requirements: 6.1_

  - [x] 14.2 Implement ValidateSKU method

    - Define regex pattern: ^[A-Z0-9]+-[A-Z0-9]+-[A-Z0-9]+$
    - Parse SKU into components (Product-Variant-Attribute)
    - Return validation result with suggestions
    - _Requirements: 6.1, 6.2_

  - [ ] 14.3 Implement RenameSKUAsync method
    - Update Products table
    - Update SalesOrderLines table
    - Update StockMovements table
    - Update MappingTables (Luca mappings)
    - Use transaction for atomicity
    - _Requirements: 6.4_
  - [x] 14.4 Implement PreviewBulkRenameAsync method

    - Generate preview of changes without applying
    - Show affected record counts
    - _Requirements: 6.3_

  - [ ]\* 14.5 Write property test for SKU format validation
    - **Property 12: SKU Format Validation**
    - **Validates: Requirements 6.1, 6.2**
  - [ ]\* 14.6 Write property test for SKU rename cascade
    - **Property 13: SKU Rename Cascade**
    - **Validates: Requirements 6.4**

- [x] 15. Create SKU API endpoints

  - [x] 15.1 Create SKUController

    - Create `src/Katana.API/Controllers/SKUController.cs`
    - Add POST /api/sku/validate endpoint
    - Add POST /api/sku/rename endpoint
    - Add POST /api/sku/bulk-rename/preview endpoint
    - Add POST /api/sku/bulk-rename endpoint
    - _Requirements: 6.1, 6.2, 6.3, 6.4_

- [ ] 16. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Phase 8: API Controllers and DI Registration

- [x] 17. Create VariantController

  - [x] 17.1 Implement variant grouping endpoints

    - Create `src/Katana.API/Controllers/VariantController.cs`
    - Add GET /api/variants/grouped endpoint
    - Add GET /api/variants/orphans endpoint
    - Add GET /api/variants/product/{productId} endpoint
    - _Requirements: 1.1, 1.2, 1.3, 1.4_

- [x] 18. Register services in DI container

  - [x] 18.1 Update Program.cs with new service registrations

    - Register IVariantGroupingService
    - Register IBOMService
    - Register IManufacturingOrderSyncService
    - Register ISKUValidationService
    - _Requirements: All_

- [ ] 19. Final Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.
