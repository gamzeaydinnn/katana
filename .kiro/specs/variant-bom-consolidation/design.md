# Design Document: Variant & BOM Consolidation

## Overview

Bu tasarım, Katana-Luca entegrasyonunda varyant yönetimi ve BOM (Bill of Materials) konsolidasyonunu ele almaktadır. Sistem, ürün varyantlarını gruplandırma, duplicate ürünleri tespit etme/birleştirme, BOM gereksinimlerini hesaplama ve üretim emirlerini Luca'ya senkronize etme yetenekleri kazanacaktır.

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         API Layer                                │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │ VariantController│  │DeduplicationCtrl│  │  BOMController  │ │
│  └────────┬────────┘  └────────┬────────┘  └────────┬────────┘ │
└───────────┼─────────────────────┼─────────────────────┼─────────┘
            │                     │                     │
┌───────────┼─────────────────────┼─────────────────────┼─────────┐
│           ▼                     ▼                     ▼         │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │VariantGrouping  │  │ Deduplication   │  │   BOMService    │ │
│  │    Service      │  │    Service      │  │                 │ │
│  └────────┬────────┘  └────────┬────────┘  └────────┬────────┘ │
│           │                     │                     │         │
│           └─────────────────────┼─────────────────────┘         │
│                                 ▼                               │
│                    ┌─────────────────────┐                      │
│                    │  OrderConsolidation │                      │
│                    │      Service        │                      │
│                    └──────────┬──────────┘                      │
│                               │                                 │
│                    Business Layer                               │
└───────────────────────────────┼─────────────────────────────────┘
                                │
┌───────────────────────────────┼─────────────────────────────────┐
│                               ▼                                 │
│                    ┌─────────────────────┐                      │
│                    │   LucaService       │                      │
│                    │  (Invoice/Stock)    │                      │
│                    └──────────┬──────────┘                      │
│                               │                                 │
│                    Infrastructure Layer                         │
└───────────────────────────────┼─────────────────────────────────┘
                                │
                                ▼
                    ┌─────────────────────┐
                    │    Luca/Koza API    │
                    └─────────────────────┘
```

## Components and Interfaces

### 1. IVariantGroupingService

```csharp
public interface IVariantGroupingService
{
    /// <summary>
    /// Varyantları ana ürün altında gruplar
    /// </summary>
    Task<List<ProductVariantGroup>> GroupVariantsByProductAsync();

    /// <summary>
    /// Belirli bir ürünün varyantlarını getirir
    /// </summary>
    Task<ProductVariantGroup?> GetVariantGroupAsync(long productId);

    /// <summary>
    /// Orphan (ana ürünsüz) varyantları tespit eder
    /// </summary>
    Task<List<VariantDto>> GetOrphanVariantsAsync();
}
```

### 2. IDeduplicationService (Mevcut - Genişletilecek)

```csharp
public interface IDeduplicationService
{
    // Mevcut metodlar...

    /// <summary>
    /// Varyant bazlı duplicate tespiti
    /// </summary>
    Task<List<VariantDuplicateGroup>> DetectVariantDuplicatesAsync();

    /// <summary>
    /// Varyantları canonical ürüne birleştirir
    /// </summary>
    Task<MergeResult> MergeVariantsAsync(long canonicalProductId, List<long> duplicateProductIds);

    /// <summary>
    /// Aktif siparişi olan ürünleri kontrol eder
    /// </summary>
    Task<List<ActiveOrderReference>> GetActiveOrdersForProductAsync(long productId);
}
```

### 3. IBOMService

```csharp
public interface IBOMService
{
    /// <summary>
    /// Sipariş için BOM gereksinimlerini hesaplar
    /// </summary>
    Task<BOMRequirementResult> CalculateBOMRequirementsAsync(int salesOrderId);

    /// <summary>
    /// Ürün için BOM bileşenlerini getirir
    /// </summary>
    Task<List<BOMComponent>> GetBOMComponentsAsync(long variantId);

    /// <summary>
    /// Stok eksikliklerini tespit eder
    /// </summary>
    Task<List<StockShortage>> DetectShortagesAsync(BOMRequirementResult requirements);
}
```

### 4. IManufacturingOrderSyncService

```csharp
public interface IManufacturingOrderSyncService
{
    /// <summary>
    /// Üretim emrini Luca'ya senkronize eder
    /// </summary>
    Task<SyncResultDto> SyncManufacturingOrderToLucaAsync(long manufacturingOrderId);

    /// <summary>
    /// Üretim tamamlama işlemini Luca'ya bildirir
    /// </summary>
    Task<SyncResultDto> SyncProductionCompletionAsync(long manufacturingOrderId, decimal completedQty, List<MaterialConsumption> consumedMaterials);
}
```

### 5. ISKUValidationService

```csharp
public interface ISKUValidationService
{
    /// <summary>
    /// SKU formatını doğrular
    /// </summary>
    SKUValidationResult ValidateSKU(string sku);

    /// <summary>
    /// SKU değişikliğini tüm ilişkili kayıtlara uygular
    /// </summary>
    Task<SKURenameResult> RenameSKUAsync(string oldSku, string newSku);

    /// <summary>
    /// Toplu SKU değişikliği önizlemesi
    /// </summary>
    Task<List<SKURenamePreview>> PreviewBulkRenameAsync(List<SKURenameRequest> requests);
}
```

## Data Models

### ProductVariantGroup

```csharp
public class ProductVariantGroup
{
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int VariantCount { get; set; }
    public List<VariantDetail> Variants { get; set; } = new();
    public decimal TotalStock { get; set; }
    public bool HasOrphanVariants { get; set; }
}

public class VariantDetail
{
    public long VariantId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public Dictionary<string, string> Attributes { get; set; } = new(); // Color, Size, etc.
    public decimal InStock { get; set; }
    public decimal Available { get; set; }
    public decimal Committed { get; set; }
    public decimal? SalesPrice { get; set; }
}
```

### BOMRequirementResult

```csharp
public class BOMRequirementResult
{
    public int SalesOrderId { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public List<BOMLineRequirement> LineRequirements { get; set; } = new();
    public List<MaterialRequirement> TotalMaterialRequirements { get; set; } = new();
    public bool HasShortages { get; set; }
    public List<StockShortage> Shortages { get; set; } = new();
}

public class BOMLineRequirement
{
    public long VariantId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal OrderedQuantity { get; set; }
    public bool HasBOM { get; set; }
    public List<MaterialRequirement> Materials { get; set; } = new();
}

public class MaterialRequirement
{
    public long ComponentVariantId { get; set; }
    public string ComponentSKU { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public decimal RequiredQuantity { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal ShortageQuantity { get; set; }
    public string Unit { get; set; } = "ADET";
}

public class StockShortage
{
    public long VariantId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Required { get; set; }
    public decimal Available { get; set; }
    public decimal Shortage { get; set; }
    public bool SuggestPurchaseOrder { get; set; }
}
```

### SKUValidationResult

```csharp
public class SKUValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuggestedFormat { get; set; }
    public SKUComponents? ParsedComponents { get; set; }
}

public class SKUComponents
{
    public string ProductCode { get; set; } = string.Empty;
    public string? VariantCode { get; set; }
    public string? AttributeCode { get; set; }
}

public class SKURenameResult
{
    public bool Success { get; set; }
    public string OldSKU { get; set; } = string.Empty;
    public string NewSKU { get; set; } = string.Empty;
    public int UpdatedOrderLines { get; set; }
    public int UpdatedStockMovements { get; set; }
    public int UpdatedLucaMappings { get; set; }
    public List<string> Errors { get; set; } = new();
}
```

## Correctness Properties

_A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees._

### Property 1: Variant Grouping Completeness

_For any_ set of variants in the database, grouping by ProductId SHALL result in every variant appearing in exactly one group, and the sum of variant counts across all groups SHALL equal the total variant count.
**Validates: Requirements 1.1, 1.2**

### Property 2: Single Variant Products Display

_For any_ product with exactly one variant, the grouping service SHALL return that product without a group wrapper (VariantCount = 1, displayed as single product).
**Validates: Requirements 1.3**

### Property 3: Orphan Variant Detection

_For any_ variant with null or invalid ProductId, the system SHALL flag it with HasOrphanVariants = true and include it in the orphan variants list.
**Validates: Requirements 1.4**

### Property 4: Duplicate Detection Consistency

_For any_ set of products, running duplicate detection multiple times with the same threshold SHALL produce identical duplicate groups.
**Validates: Requirements 2.1**

### Property 5: Merge Data Integrity

_For any_ merge operation, the total count of order lines and stock movements before merge SHALL equal the count after merge (no data loss).
**Validates: Requirements 2.3, 2.4**

### Property 6: Active Order Protection

_For any_ product with active orders (status != SHIPPED, CANCELLED), deletion SHALL be prevented and the system SHALL return the list of blocking orders.
**Validates: Requirements 2.5**

### Property 7: Multi-Variant Order Persistence

_For any_ sales order with N variants, saving the order SHALL create exactly N order lines, each with correct variant-specific attributes.
**Validates: Requirements 3.1, 3.2**

### Property 8: Invoice Line Completeness

_For any_ sales order synced to Luca, the generated invoice SHALL contain exactly one DetayList item per order line, with matching SKU and quantity.
**Validates: Requirements 3.3, 7.1**

### Property 9: BOM Calculation Accuracy

_For any_ order line with quantity Q and BOM component with ratio R, the required material quantity SHALL equal Q \* R.
**Validates: Requirements 4.1**

### Property 10: Shortage Detection Correctness

_For any_ material requirement where RequiredQuantity > CurrentStock, the system SHALL flag it as a shortage with ShortageQuantity = RequiredQuantity - CurrentStock.
**Validates: Requirements 4.2, 4.3**

### Property 11: Production Stock Movement Direction

_For any_ completed manufacturing order, raw material stock SHALL decrease and finished product stock SHALL increase by the correct quantities.
**Validates: Requirements 5.1, 5.2**

### Property 12: SKU Format Validation

_For any_ SKU string, validation SHALL return IsValid = true only if it matches the pattern: ^[A-Z0-9]+-[A-Z0-9]+-[A-Z0-9]+$ (PRODUCT-VARIANT-ATTRIBUTE).
**Validates: Requirements 6.1, 6.2**

### Property 13: SKU Rename Cascade

_For any_ SKU rename operation, all related records (order lines, stock movements, Luca mappings) SHALL be updated to the new SKU value.
**Validates: Requirements 6.4**

### Property 14: Invoice Description Contains Variant Attributes

_For any_ invoice line generated from a variant, the Aciklama (description) field SHALL contain the variant's attribute values (color, size, etc.).
**Validates: Requirements 7.2**

## Error Handling

### Retry Strategy

```csharp
// Luca API çağrıları için retry policy
private static readonly AsyncRetryPolicy _retryPolicy = Policy
    .Handle<HttpRequestException>()
    .Or<TimeoutException>()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
        onRetry: (exception, delay, attempt, context) =>
        {
            // Log retry attempt
        });
```

### Error Codes

| Code    | Description              | Action                                              |
| ------- | ------------------------ | --------------------------------------------------- |
| VAR001  | Orphan variant detected  | Display warning, allow manual assignment            |
| VAR002  | Duplicate merge conflict | Show conflicting records, require manual resolution |
| BOM001  | BOM component not found  | Skip BOM calculation, log warning                   |
| BOM002  | Insufficient stock       | Highlight shortage, suggest PO                      |
| SKU001  | Invalid SKU format       | Show validation error with example                  |
| SKU002  | SKU already exists       | Prevent rename, show existing product               |
| SYNC001 | Luca sync failed         | Retry with backoff, notify after 3 failures         |

## Testing Strategy

### Property-Based Testing Library

- **Library**: FsCheck for .NET
- **Minimum Iterations**: 100 per property test

### Unit Tests

- VariantGroupingService: Test grouping logic with various product/variant combinations
- DeduplicationService: Test fuzzy matching algorithm accuracy
- BOMService: Test calculation with different BOM structures
- SKUValidationService: Test regex pattern matching

### Property-Based Tests

Each correctness property will be implemented as a property-based test using FsCheck:

```csharp
// Example: Property 1 - Variant Grouping Completeness
[Property]
public Property VariantGrouping_AllVariantsAppearExactlyOnce()
{
    return Prop.ForAll(
        Arb.From<List<VariantDto>>(),
        variants =>
        {
            var groups = _service.GroupVariantsByProductAsync(variants).Result;
            var totalInGroups = groups.Sum(g => g.VariantCount);
            return totalInGroups == variants.Count;
        });
}
```

### Integration Tests

- End-to-end order creation with multiple variants
- Luca invoice sync verification
- BOM requirement calculation with real data
- SKU rename cascade verification
