# Design Document

## Overview

Product Name Deduplication sistemi, Katana'da aynı isme sahip ancak farklı SKU'lara sahip ürünlerin güvenli bir şekilde analiz edilmesini ve konsolidasyonunu sağlar. Sistem, veri bütünlüğünü korurken kullanıcı onayı gerektiren akıllı bir deduplikasyon süreci sunar.

## Architecture

### High-Level Architecture

```
┌─────────────────┐
│  Admin UI       │
│  (React)        │
└────────┬────────┘
         │
         ▼
┌─────────────────────────────────────┐
│  API Layer                          │
│  - DeduplicationController          │
│  - ProductMergeController           │
└────────┬────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────┐
│  Business Layer                     │
│  - DuplicateAnalysisService         │
│  - ProductMergeService              │
│  - MergeValidationService           │
│  - MergeHistoryService              │
└────────┬────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────┐
│  Data Layer                         │
│  - ProductRepository                │
│  - MergeHistoryRepository           │
│  - ReferenceUpdateRepository        │
└─────────────────────────────────────┘
```

### Component Interaction Flow

```
User Request → Controller → Service → Repository → Database
                    ↓
              Validation
                    ↓
              Audit Log
```

## Components and Interfaces

### 1. DuplicateAnalysisService

**Responsibility:** Ürün duplikasyonlarını analiz eder ve gruplar.

**Interface:**

```csharp
public interface IDuplicateAnalysisService
{
    Task<DuplicateAnalysisResult> AnalyzeDuplicatesAsync(DuplicateAnalysisRequest request);
    Task<DuplicateGroupDetail> GetDuplicateGroupDetailAsync(string productName);
    Task<List<DuplicateGroup>> FilterDuplicateGroupsAsync(DuplicateFilterCriteria criteria);
    Task<byte[]> ExportDuplicateAnalysisAsync(DuplicateAnalysisResult analysis);
}
```

**Key Methods:**

- `AnalyzeDuplicatesAsync`: Tüm ürünleri analiz eder ve duplicate grupları döner
- `GetDuplicateGroupDetailAsync`: Belirli bir grup için detaylı bilgi getirir
- `FilterDuplicateGroupsAsync`: Filtreleme kriterlerine göre grupları filtreler
- `ExportDuplicateAnalysisAsync`: Analiz sonuçlarını CSV olarak export eder

### 2. ProductMergeService

**Responsibility:** Ürün birleştirme işlemlerini yönetir.

**Interface:**

```csharp
public interface IProductMergeService
{
    Task<MergePreview> PreviewMergeAsync(MergeRequest request);
    Task<MergeResult> ExecuteMergeAsync(MergeRequest request, string adminUserId);
    Task<RollbackResult> RollbackMergeAsync(int mergeHistoryId, string adminUserId);
    Task MarkGroupAsKeepSeparateAsync(string productName, string reason, string adminUserId);
    Task RemoveKeepSeparateFlagAsync(string productName, string adminUserId);
}
```

**Key Methods:**

- `PreviewMergeAsync`: Merge işleminin etkisini önizler
- `ExecuteMergeAsync`: Merge işlemini gerçekleştirir
- `RollbackMergeAsync`: Merge işlemini geri alır
- `MarkGroupAsKeepSeparateAsync`: Grubu merge'den muaf tutar
- `RemoveKeepSeparateFlagAsync`: Muafiyet flagini kaldırır

### 3. MergeValidationService

**Responsibility:** Merge işlemlerini validate eder.

**Interface:**

```csharp
public interface IMergeValidationService
{
    Task<ValidationResult> ValidateMergeRequestAsync(MergeRequest request);
    Task<bool> CanonicalProductExistsAsync(int productId);
    Task<bool> HasCircularBOMReferencesAsync(int canonicalProductId, List<int> productIdsToMerge);
    Task<bool> IsProductInPendingMergeAsync(int productId);
}
```

**Key Methods:**

- `ValidateMergeRequestAsync`: Merge request'i validate eder
- `CanonicalProductExistsAsync`: Canonical ürünün varlığını kontrol eder
- `HasCircularBOMReferencesAsync`: Circular BOM referanslarını kontrol eder
- `IsProductInPendingMergeAsync`: Ürünün başka bir pending merge'de olup olmadığını kontrol eder

### 4. MergeHistoryService

**Responsibility:** Merge geçmişini yönetir ve audit log tutar.

**Interface:**

```csharp
public interface IMergeHistoryService
{
    Task<int> CreateMergeHistoryAsync(MergeHistoryEntry entry);
    Task<List<MergeHistoryEntry>> GetMergeHistoryAsync(MergeHistoryFilter filter);
    Task<MergeHistoryDetail> GetMergeHistoryDetailAsync(int mergeHistoryId);
    Task UpdateMergeHistoryStatusAsync(int mergeHistoryId, MergeStatus status);
}
```

**Key Methods:**

- `CreateMergeHistoryAsync`: Yeni merge history kaydı oluşturur
- `GetMergeHistoryAsync`: Filtrelenmiş merge history listesi döner
- `GetMergeHistoryDetailAsync`: Belirli bir merge history detayını getirir
- `UpdateMergeHistoryStatusAsync`: Merge history durumunu günceller

## Data Models

### DuplicateGroup

```csharp
public class DuplicateGroup
{
    public string ProductName { get; set; }
    public int Count { get; set; }
    public List<ProductSummary> Products { get; set; }
    public bool IsKeepSeparate { get; set; }
    public string KeepSeparateReason { get; set; }
    public DateTime? KeepSeparateDate { get; set; }
    public string KeepSeparateBy { get; set; }
}
```

### ProductSummary

```csharp
public class ProductSummary
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string SKU { get; set; }
    public string Code { get; set; }
    public string CategoryName { get; set; }
    public int? SalesOrderId { get; set; }
    public int SalesOrderCount { get; set; }
    public int BOMCount { get; set; }
    public int StockMovementCount { get; set; }
    public bool IsActive { get; set; }
    public bool IsSuggestedCanonical { get; set; }
}
```

### MergeRequest

```csharp
public class MergeRequest
{
    public int CanonicalProductId { get; set; }
    public List<int> ProductIdsToMerge { get; set; }
    public bool UpdateSalesOrders { get; set; }
    public bool UpdateBOMs { get; set; }
    public bool UpdateStockMovements { get; set; }
    public string Reason { get; set; }
}
```

### MergePreview

```csharp
public class MergePreview
{
    public ProductSummary CanonicalProduct { get; set; }
    public List<ProductSummary> ProductsToMerge { get; set; }
    public int SalesOrdersToUpdate { get; set; }
    public int BOMsToUpdate { get; set; }
    public int StockMovementsToUpdate { get; set; }
    public List<string> Warnings { get; set; }
    public List<string> CriticalWarnings { get; set; }
    public bool CanProceed { get; set; }
}
```

### MergeResult

```csharp
public class MergeResult
{
    public bool Success { get; set; }
    public int MergeHistoryId { get; set; }
    public int SalesOrdersUpdated { get; set; }
    public int BOMsUpdated { get; set; }
    public int StockMovementsUpdated { get; set; }
    public int ProductsInactivated { get; set; }
    public List<string> Errors { get; set; }
    public DateTime CompletedAt { get; set; }
}
```

### MergeHistoryEntry

```csharp
public class MergeHistoryEntry
{
    public int Id { get; set; }
    public int CanonicalProductId { get; set; }
    public string CanonicalProductName { get; set; }
    public string CanonicalProductSKU { get; set; }
    public List<int> MergedProductIds { get; set; }
    public int SalesOrdersUpdated { get; set; }
    public int BOMsUpdated { get; set; }
    public int StockMovementsUpdated { get; set; }
    public string AdminUserId { get; set; }
    public string AdminUserName { get; set; }
    public DateTime CreatedAt { get; set; }
    public MergeStatus Status { get; set; }
    public string Reason { get; set; }
}
```

### Database Schema

```sql
-- Merge history table
CREATE TABLE merge_history (
    id SERIAL PRIMARY KEY,
    canonical_product_id INTEGER NOT NULL,
    canonical_product_name VARCHAR(500) NOT NULL,
    canonical_product_sku VARCHAR(200) NOT NULL,
    merged_product_ids INTEGER[] NOT NULL,
    sales_orders_updated INTEGER DEFAULT 0,
    boms_updated INTEGER DEFAULT 0,
    stock_movements_updated INTEGER DEFAULT 0,
    admin_user_id VARCHAR(100) NOT NULL,
    admin_user_name VARCHAR(200) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    status VARCHAR(50) NOT NULL,
    reason TEXT,
    FOREIGN KEY (canonical_product_id) REFERENCES products(id)
);

-- Keep separate groups table
CREATE TABLE keep_separate_groups (
    id SERIAL PRIMARY KEY,
    product_name VARCHAR(500) NOT NULL UNIQUE,
    reason TEXT NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    created_by VARCHAR(100) NOT NULL,
    removed_at TIMESTAMP,
    removed_by VARCHAR(100)
);

-- Merge rollback data table
CREATE TABLE merge_rollback_data (
    id SERIAL PRIMARY KEY,
    merge_history_id INTEGER NOT NULL,
    entity_type VARCHAR(50) NOT NULL, -- 'SalesOrder', 'BOM', 'StockMovement'
    entity_id INTEGER NOT NULL,
    original_product_id INTEGER NOT NULL,
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    FOREIGN KEY (merge_history_id) REFERENCES merge_history(id)
);

CREATE INDEX idx_merge_history_canonical ON merge_history(canonical_product_id);
CREATE INDEX idx_merge_history_created_at ON merge_history(created_at DESC);
CREATE INDEX idx_merge_rollback_merge_id ON merge_rollback_data(merge_history_id);
```

## Correctness Properties

_A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees._

### Property Reflection

After reviewing all properties, the following consolidations were identified:

- Properties 1.3 and 2.1 are redundant (both test field presence)
- Properties 4.1, 4.2, and 4.3 can be combined into a single comprehensive property about reference counting
- Properties 9.2 and 9.3 can be combined into a single property about export completeness

### Correctness Properties

Property 1: Product grouping by name
_For any_ set of products, when grouped by name, all products with identical names should appear in the same group and no products with different names should appear together
**Validates: Requirements 1.1**

Property 2: Group count accuracy
_For any_ duplicate group, the count field should equal the actual number of products in the group
**Validates: Requirements 1.2**

Property 3: Product detail completeness
_For any_ product in a duplicate group, the output should contain ID, SKU, code, category, and sales order ID fields
**Validates: Requirements 1.3, 2.1**

Property 4: Single product exclusion
_For any_ duplicate analysis result, no group should contain only one product
**Validates: Requirements 1.4**

Property 5: Group sorting by count
_For any_ duplicate analysis result, groups should be sorted by count in descending order
**Validates: Requirements 1.5**

Property 6: Field difference detection
_For any_ two products in a duplicate group, if their SKU, code, or category fields differ, the comparison should identify these differences
**Validates: Requirements 2.3**

Property 7: BOM relationship accuracy
_For any_ product, the BOM count in the output should equal the actual number of BOMs containing that product
**Validates: Requirements 2.4**

Property 8: Stock movement count accuracy
_For any_ product, the stock movement count in the output should equal the actual number of stock movements for that product
**Validates: Requirements 2.5**

Property 9: Single canonical selection
_For any_ merge request, attempting to select multiple canonical products for the same duplicate group should be rejected
**Validates: Requirements 3.1**

Property 10: Sales order priority
_For any_ duplicate group, products with active sales orders should have higher canonical suggestion priority than products without
**Validates: Requirements 3.2**

Property 11: Data completeness priority
_For any_ duplicate group, products with more complete data (non-null fields) should have higher canonical suggestion priority
**Validates: Requirements 3.3**

Property 12: Merge preview accuracy
_For any_ merge request, the preview should list exactly the products specified in the request
**Validates: Requirements 3.4**

Property 13: Canonical product requirement
_For any_ merge request without a canonical product ID, the operation should be rejected
**Validates: Requirements 3.5**

Property 14: Reference count accuracy in preview
_For any_ merge preview, the counts of sales orders, BOMs, and stock movements to be updated should equal the actual number of references to the products being merged
**Validates: Requirements 4.1, 4.2, 4.3**

Property 15: Active sales order warning
_For any_ merge preview where products have active sales orders, a warning should be included in the warnings list
**Validates: Requirements 4.4**

Property 16: Unique data warning
_For any_ merge preview where products have unique data that would be lost, a critical warning should be included
**Validates: Requirements 4.5**

Property 17: Confirmation requirement
_For any_ merge execution request without explicit confirmation, the operation should be rejected
**Validates: Requirements 5.1**

Property 18: Sales order reference update
_For any_ completed merge operation, all sales order references to merged products should point to the canonical product
**Validates: Requirements 5.2**

Property 19: BOM reference update
_For any_ completed merge operation, all BOM references to merged products should point to the canonical product
**Validates: Requirements 5.3**

Property 20: Stock movement reference update
_For any_ completed merge operation, all stock movement references to merged products should point to the canonical product
**Validates: Requirements 5.4**

Property 21: Product inactivation not deletion
_For any_ completed merge operation, merged products should be marked as inactive and should still exist in the database
**Validates: Requirements 5.5**

Property 22: Keep separate exclusion
_For any_ duplicate group marked as "keep separate", it should not appear in merge candidate lists
**Validates: Requirements 6.2**

Property 23: Keep separate reason persistence
_For any_ group marked as "keep separate", the stored reason should match the provided reason
**Validates: Requirements 6.3**

Property 24: Keep separate metadata display
_For any_ excluded group, the output should include the exclusion reason, timestamp, and admin user
**Validates: Requirements 6.4**

Property 25: Merge history creation
_For any_ completed merge operation, a history entry should be created with all merge details
**Validates: Requirements 7.1**

Property 26: Merge history completeness
_For any_ merge history entry, it should contain canonical product details, merged product IDs, and timestamp
**Validates: Requirements 7.2**

Property 27: Merge history admin tracking
_For any_ merge history entry, it should contain the admin user ID and name who performed the operation
**Validates: Requirements 7.3**

Property 28: Merge history reference counts
_For any_ merge history entry, it should contain accurate counts of updated sales orders, BOMs, and stock movements
**Validates: Requirements 7.4**

Property 29: Rollback restoration
_For any_ rolled back merge, all references should be restored to point to their original products
**Validates: Requirements 7.5**

Property 30: Category filter accuracy
_For any_ filtered duplicate analysis by category, all results should belong to the specified category
**Validates: Requirements 8.1**

Property 31: Minimum count filter accuracy
_For any_ filtered duplicate analysis by minimum count, all results should have a count greater than or equal to the specified minimum
**Validates: Requirements 8.2**

Property 32: Name pattern search accuracy
_For any_ duplicate analysis search by name pattern, all results should match the specified pattern
**Validates: Requirements 8.3**

Property 33: SKU pattern search accuracy
_For any_ duplicate analysis search by SKU pattern, all results should contain at least one product matching the SKU pattern
**Validates: Requirements 8.4**

Property 34: Export completeness
_For any_ exported duplicate analysis, the CSV should contain all product attributes and reference counts for all groups
**Validates: Requirements 9.2, 9.3**

Property 35: Export metadata
_For any_ exported duplicate analysis, the CSV should include export timestamp and admin user information
**Validates: Requirements 9.4**

Property 36: Canonical product validation
_For any_ merge request, if the canonical product does not exist or is inactive, the validation should fail
**Validates: Requirements 10.1**

Property 37: Merge product existence validation
_For any_ merge request, if any product to be merged does not exist, the validation should fail
**Validates: Requirements 10.2**

Property 38: Circular BOM reference validation
_For any_ merge request, if the merge would create circular BOM references, the validation should fail
**Validates: Requirements 10.3**

Property 39: Pending merge validation
_For any_ merge request, if the canonical product is already in another pending merge, the validation should fail
**Validates: Requirements 10.4**

Property 40: Validation error messaging
_For any_ failed validation, specific error messages should be returned describing the validation failures
**Validates: Requirements 10.5**

## Error Handling

### Error Categories

1. **Validation Errors** (400 Bad Request)

   - Invalid merge request (missing canonical product)
   - Non-existent products
   - Circular BOM references
   - Products in pending merges

2. **Authorization Errors** (403 Forbidden)

   - Non-admin user attempting merge operations
   - Insufficient permissions for rollback

3. **Conflict Errors** (409 Conflict)

   - Concurrent merge operations on same products
   - Group already marked as keep separate

4. **Not Found Errors** (404 Not Found)

   - Duplicate group not found
   - Merge history not found

5. **Internal Errors** (500 Internal Server Error)
   - Database transaction failures
   - Reference update failures

### Error Response Format

```csharp
public class ErrorResponse
{
    public string ErrorCode { get; set; }
    public string Message { get; set; }
    public List<string> Details { get; set; }
    public DateTime Timestamp { get; set; }
}
```

### Retry Strategy

- Merge operations use database transactions with retry logic (max 3 retries)
- Exponential backoff for concurrent operation conflicts
- Rollback operations are idempotent and can be safely retried

## Testing Strategy

### Unit Testing

Unit tests will cover:

- Duplicate grouping logic
- Canonical product suggestion algorithm
- Validation rules
- Reference counting logic
- Filter and search logic

### Property-Based Testing

Property-based testing will be implemented using **Bogus** for data generation and **xUnit** for test execution. Each correctness property will be tested with a minimum of 100 iterations.

Property-based tests will:

- Generate random product sets with varying duplicate patterns
- Generate random merge requests with valid and invalid data
- Verify all correctness properties hold across all generated inputs
- Test edge cases like empty groups, single products, and large duplicate sets

Each property-based test will be tagged with a comment referencing the design document:

```csharp
// Feature: product-name-deduplication, Property 1: Product grouping by name
[Fact]
public void Property_ProductGroupingByName_ShouldGroupIdenticalNames()
{
    // Test implementation
}
```

### Integration Testing

Integration tests will cover:

- End-to-end merge workflows
- Database transaction integrity
- Reference update cascades
- Rollback operations

### Test Data Strategy

- Use Bogus to generate realistic product data
- Create test fixtures for common duplicate scenarios
- Use in-memory database for fast test execution
- Seed test database with known duplicate patterns for regression testing

## Performance Considerations

### Duplicate Analysis Optimization

- Index on product name for fast grouping
- Batch reference counting queries
- Cache duplicate analysis results for 5 minutes
- Paginate large duplicate group lists

### Merge Operation Optimization

- Use database transactions for atomicity
- Batch update operations for references
- Async processing for large merge operations
- Progress notifications via SignalR for long-running merges

### Scalability

- Support for merging up to 100 products in a single operation
- Handle duplicate analysis for up to 10,000 products
- Merge history retention for 2 years with archival strategy

## Security Considerations

- Only admin users can perform merge operations
- Audit logging for all merge and rollback operations
- Merge operations require explicit confirmation to prevent accidental merges
- Keep separate flags prevent unauthorized merges
- Rollback operations require admin authorization

## Monitoring and Observability

### Metrics to Track

- Number of duplicate groups detected
- Average merge operation duration
- Merge success/failure rate
- Rollback operation count
- Keep separate group count

### Logging

- Log all merge operations with full details
- Log validation failures with reasons
- Log rollback operations
- Log performance metrics for slow operations

### Alerts

- Alert on merge operation failures
- Alert on high rollback rate
- Alert on validation failure spikes
- Alert on slow merge operations (> 30 seconds)
