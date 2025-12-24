# Design Document: Auto Stock Card Creation on Order Approval

## Overview

Bu tasarım, satış siparişi onaylandığında sipariş satırlarındaki ürünler için Luca/Koza sisteminde otomatik stok kartı oluşturma özelliğini tanımlar. Mevcut `ApproveOrder` metodundaki stok kartı mantığı iyileştirilecek ve daha güvenilir hale getirilecektir.

### Mevcut Durum Analizi

Mevcut kodda (`SalesOrdersController.ApproveOrder`) stok kartı kontrolü ve oluşturma mantığı zaten var:

- `FindStockCardBySkuAsync` ile SKU kontrolü yapılıyor
- `UpsertStockCardAsync` ile stok kartı oluşturuluyor
- Sonuçlar `stockCardCreationResults` listesinde toplanıyor

Ancak log'lara bakınca Luca API'den "Beklenmedik bir hata oluştu" HTML response dönüyor. Bu, muhtemelen:

1. Session timeout sonrası retry mekanizmasının düzgün çalışmaması
2. Stok kartı oluşturma request'inin eksik/hatalı veri içermesi
3. Fatura oluşturma sırasında stok kartı ID'lerinin doğru eşlenmemesi

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    SalesOrdersController                         │
│                      ApproveOrder(id)                            │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                 StockCardPreparationService                      │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ PrepareStockCardsForOrderAsync(order)                    │   │
│  │   - For each line: CheckAndCreateStockCard()             │   │
│  │   - Returns: StockCardPreparationResult                  │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                        ILucaService                              │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ FindStockCardBySkuAsync(sku)                             │   │
│  │ UpsertStockCardAsync(request)                            │   │
│  │ CreateSalesOrderInvoiceAsync(order, depoKodu)            │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Luca/Koza API                               │
│  - ListeleStkKart.do (stok kartı arama)                         │
│  - EkleStkWsKart.do (stok kartı oluşturma)                      │
│  - EkleFtrWsFaturaBaslik.do (fatura oluşturma)                  │
└─────────────────────────────────────────────────────────────────┘
```

## Components and Interfaces

### 1. StockCardPreparationService (Yeni)

Stok kartı hazırlama işlemlerini merkezi bir serviste toplar.

```csharp
public interface IStockCardPreparationService
{
    Task<StockCardPreparationResult> PrepareStockCardsForOrderAsync(
        SalesOrder order,
        CancellationToken ct = default);
}

public class StockCardPreparationResult
{
    public bool AllSucceeded { get; set; }
    public int TotalLines { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; } // Already exists
    public List<StockCardOperationResult> Results { get; set; }
}

public class StockCardOperationResult
{
    public string SKU { get; set; }
    public string ProductName { get; set; }
    public string Action { get; set; } // "exists", "created", "failed", "skipped"
    public long? SkartId { get; set; }
    public string Message { get; set; }
    public string Error { get; set; }
}
```

### 2. Enhanced ApproveOrder Flow

```csharp
[HttpPost("{id}/approve")]
public async Task<ActionResult> ApproveOrder(int id)
{
    // 1. Load and validate order
    // 2. Check duplicate approval
    // 3. Validate lines

    // 4. ✅ NEW: Prepare stock cards BEFORE Katana/Luca operations
    var stockCardResult = await _stockCardPreparationService
        .PrepareStockCardsForOrderAsync(order);

    // 5. Send to Katana (if new order)
    // 6. Update database

    // 7. Send to Luca (with guaranteed stock cards)
    // 8. Return response with stock card results
}
```

### 3. LucaCreateStokKartiRequest Mapping

```csharp
public static LucaCreateStokKartiRequest MapFromOrderLine(SalesOrderLine line)
{
    return new LucaCreateStokKartiRequest
    {
        KartKodu = CleanSpecialChars(line.SKU),
        KartAdi = CleanSpecialChars(line.ProductName ?? line.SKU),
        KartTuru = 1, // Stok kartı
        KartTipi = 1,
        KartAlisKdvOran = CalculateKdvRate(line.TaxRate),
        KartSatisKdvOran = CalculateKdvRate(line.TaxRate),
        OlcumBirimiId = 1, // ADET (default)
        BaslangicTarihi = DateTime.Now.ToString("dd/MM/yyyy"),
        Barkod = CleanSpecialChars(line.SKU),
        SatilabilirFlag = 1,
        SatinAlinabilirFlag = 1,
        MaliyetHesaplanacakFlag = true
    };
}

private static double CalculateKdvRate(decimal? taxRate)
{
    if (!taxRate.HasValue) return 0.20; // Default %20
    // TaxRate yüzde olarak geliyorsa (20) → 0.20'ye çevir
    return taxRate.Value > 1 ? (double)taxRate.Value / 100.0 : (double)taxRate.Value;
}

private static string CleanSpecialChars(string input)
{
    if (string.IsNullOrEmpty(input)) return input;
    return input
        .Replace("Ø", "O")
        .Replace("ø", "o")
        .Trim();
}
```

## Data Models

### StockCardPreparationResult

```csharp
public class StockCardPreparationResult
{
    public bool AllSucceeded { get; set; }
    public int TotalLines { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<StockCardOperationResult> Results { get; set; } = new();

    public bool HasCriticalFailures => FailedCount > 0 && SuccessCount == 0;
}

public class StockCardOperationResult
{
    public string SKU { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // "exists", "created", "failed", "skipped"
    public long? SkartId { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
```

## Correctness Properties

_A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees._

### Property 1: Stock Card Check Coverage

_For any_ sales order with N lines, when approval is triggered, the system should check exactly N SKUs for existing stock cards in Luca.
**Validates: Requirements 1.1**

### Property 2: Stock Card Creation for Missing SKUs

_For any_ SKU that does not exist in Luca, when stock card preparation runs, the system should attempt to create a new stock card with that SKU as kartKodu.
**Validates: Requirements 1.2, 3.1**

### Property 3: Error Isolation

_For any_ set of SKUs where one fails to create, the system should still process all remaining SKUs and return individual results for each.
**Validates: Requirements 1.3, 4.1**

### Property 4: Idempotency - No Duplicate Creation

_For any_ SKU that already exists in Luca (FindStockCardBySkuAsync returns non-null), the system should NOT call UpsertStockCardAsync for that SKU.
**Validates: Requirements 1.5, 5.2**

### Property 5: Data Mapping Correctness

_For any_ order line, the created stock card request should have:

- kartKodu = line.SKU (with special chars cleaned)
- kartAdi = line.ProductName (with special chars cleaned)
- kartTuru = 1
- OlcumBirimiId = 1 (default)
  **Validates: Requirements 3.1, 3.2, 3.4, 3.5**

### Property 6: KDV Rate Calculation

_For any_ order line with TaxRate T, the stock card KDV rate should be:

- If T > 1: T / 100 (percentage to decimal)
- If T <= 1: T (already decimal)
- If T is null: 0.20 (default)
  **Validates: Requirements 3.3**

### Property 7: Response Completeness

_For any_ approval operation, the response should contain a stockCardResults array with exactly one entry per processed SKU, each containing: sku, action, and either skartId or error.
**Validates: Requirements 2.5, 4.5**

### Property 8: Duplicate Error Handling

_For any_ UpsertStockCardAsync call that returns a duplicate error, the system should treat it as success (action = "exists") rather than failure.
**Validates: Requirements 5.3, 5.5**

## Error Handling

### Session Timeout Recovery

```csharp
// LucaService.SendWithAuthRetryAsync already handles this:
// 1. Detect HTML response (session expired)
// 2. Call ForceSessionRefreshAsync()
// 3. Retry the request
// 4. After 3 attempts, throw InvalidOperationException
```

### Error Categories

| Error Type                            | Action           | Result               |
| ------------------------------------- | ---------------- | -------------------- |
| SKU empty/null                        | Skip             | action = "skipped"   |
| FindStockCardBySkuAsync returns value | Skip creation    | action = "exists"    |
| UpsertStockCardAsync success          | Continue         | action = "created"   |
| UpsertStockCardAsync duplicate error  | Treat as success | action = "exists"    |
| UpsertStockCardAsync other error      | Log and continue | action = "failed"    |
| All SKUs failed                       | Return warning   | AllSucceeded = false |

### Retry Strategy

```csharp
// Per-SKU retry (already in UpsertStockCardAsync):
// - 3 attempts with exponential backoff
// - Session refresh between attempts
// - Final failure logged and returned
```

## Testing Strategy

### Unit Tests

1. **StockCardPreparationService Tests**

   - Test with empty order lines
   - Test with single line
   - Test with multiple lines
   - Test with mixed results (some exist, some created, some failed)

2. **Mapping Tests**
   - Test KDV rate calculation with various inputs
   - Test special character cleaning
   - Test null/empty handling

### Property-Based Tests

Using xUnit + FsCheck for property-based testing:

1. **Property 1 Test**: Generate random orders, verify FindStockCardBySkuAsync call count equals line count
2. **Property 3 Test**: Generate orders with mock failures, verify all SKUs are processed
3. **Property 6 Test**: Generate random TaxRate values, verify KDV calculation is correct

### Integration Tests

1. **End-to-End Approval Flow**

   - Create order with new SKUs
   - Approve order
   - Verify stock cards created in Luca
   - Verify invoice created in Luca

2. **Idempotency Test**
   - Approve order once
   - Approve same order again
   - Verify no duplicate stock cards
