# Katana Duplicate Consolidation Stratejisi

## 🎯 Hedef

Katana'da tekrar eden ürünleri temizle:

1. Siparişleri onaylanmamış yap
2. Tekrar eden ürünleri sil
3. Siparişleri yeniden onayla (temiz verilerle)

---

## 📊 Sorun Analizi

### Tekrar Eden Ürün Türleri

```
1. Aynı SKU'ya sahip ürünler
   ├─ TSHIRT-RED-M (ID: 1001)
   ├─ TSHIRT-RED-M (ID: 1002) ← Duplicate
   └─ TSHIRT-RED-M (ID: 1003) ← Duplicate

2. Benzer adlı ürünler (Levenshtein > 0.90)
   ├─ ÜRÜN-KIRMIZI (ID: 2001)
   └─ ÜR?N-KIRMIZI (ID: 2002) ← Duplicate

3. Varyant karışıklığı
   ├─ TSHIRT (Ana ürün, ID: 3001)
   ├─ TSHIRT-RED (Varyant, ID: 3002)
   └─ TSHIRT-RED (Varyant, ID: 3003) ← Duplicate
```

### Etkilenen Veriler

```
SalesOrders
├─ Status: APPROVED (onaylanmış)
├─ Lines: Tekrar eden ürünlere referans
└─ Invoices: Oluşturulmuş

SalesOrderLines
├─ VariantId: Tekrar eden ürün ID'si
├─ SKU: Tekrar eden SKU
└─ Quantity: Sipariş miktarı

StockMovements
├─ ProductId: Tekrar eden ürün ID'si
├─ Type: IN/OUT
└─ Quantity: Hareket miktarı

OrderInvoices
├─ SalesOrderId: Sipariş ID'si
├─ InvoiceNo: Fatura numarası
└─ Status: SYNCED/PENDING
```

---

## 🔧 Çözüm Stratejisi (5 Aşama)

### AŞAMA 1: Tekrar Eden Ürünleri Tespit Et

```csharp
// File: src/Katana.Business/Services/DuplicateConsolidationService.cs

public class DuplicateConsolidationService : IDuplicateConsolidationService
{
    private readonly IntegrationDbContext _context;
    private readonly ILogger<DuplicateConsolidationService> _logger;

    /// <summary>
    /// Katana'da tekrar eden ürünleri tespit et
    /// </summary>
    public async Task<DuplicateProductAnalysis> AnalyzeDuplicateProductsAsync(
        CancellationToken ct = default)
    {
        _logger.LogInformation("Analyzing duplicate products in Katana...");

        var analysis = new DuplicateProductAnalysis
        {
            AnalyzedAt = DateTime.UtcNow
        };

        // Adım 1: Aynı SKU'ya sahip ürünleri bul
        var skuDuplicates = await _context.Products
            .Where(p => p.IsActive)
            .GroupBy(p => p.SKU)
            .Where(g => g.Count() > 1)
            .ToListAsync(ct);

        foreach (var group in skuDuplicates)
        {
            var products = group.ToList();
            var canonical = products.OrderBy(p => p.CreatedAt).First();
            var duplicates = products.Where(p => p.Id != canonical.Id).ToList();

            analysis.SKUDuplicates.Add(new DuplicateProductGroup
            {
                GroupKey = group.Key,
                CanonicalProduct = MapToProductInfo(canonical),
                DuplicateProducts = duplicates.Select(MapToProductInfo).ToList(),
                Type = DuplicateType.SameSKU
            });
        }

        // Adım 2: Benzer adlı ürünleri bul (Levenshtein > 0.90)
        var allProducts = await _context.Products
            .Where(p => p.IsActive)
            .ToListAsync(ct);

        var similarGroups = FindSimilarProducts(allProducts, 0.90);
        foreach (var group in similarGroups)
        {
            analysis.SimilarNameDuplicates.Add(group);
        }

        // Adım 3: İstatistikleri hesapla
        analysis.TotalDuplicateProducts = analysis.SKUDuplicates
            .Sum(g => g.DuplicateProducts.Count) +
            analysis.SimilarNameDuplicates
            .Sum(g => g.DuplicateProducts.Count);

        // Adım 4: Etkilenen siparişleri bul
        var affectedOrderIds = new HashSet<int>();
        foreach (var group in analysis.SKUDuplicates.Concat(analysis.SimilarNameDuplicates))
        {
            var duplicateIds = group.DuplicateProducts
                .Select(p => p.ProductId)
                .ToList();

            var orderIds = await _context.SalesOrderLines
                .Where(l => duplicateIds.Contains(l.VariantId))
                .Select(l => l.SalesOrderId)
                .Distinct()
                .ToListAsync(ct);

            foreach (var orderId in orderIds)
                affectedOrderIds.Add(orderId);
        }

        analysis.AffectedOrders = affectedOrderIds.Count;

        _logger.LogInformation(
            "Found {DuplicateCount} duplicate products affecting {OrderCount} orders",
            analysis.TotalDuplicateProducts,
            analysis.AffectedOrders);

        return analysis;
    }

    private List<DuplicateProductGroup> FindSimilarProducts(
        List<Product> products,
        double threshold)
    {
        var groups = new List<DuplicateProductGroup>();
        var processed = new HashSet<long>();

        foreach (var product in products)
        {
            if (processed.Contains(product.Id))
                continue;

            var similar = products
                .Where(p => p.Id != product.Id && !processed.Contains(p.Id))
                .Where(p => CalculateSimilarity(product.Name, p.Name) >= threshold)
                .ToList();

            if (similar.Any())
            {
                var allInGroup = new List<Product> { product };
                allInGroup.AddRange(similar);

                foreach (var p in allInGroup)
                    processed.Add(p.Id);

                var canonical = allInGroup.OrderBy(p => p.CreatedAt).First();

                groups.Add(new DuplicateProductGroup
                {
                    GroupKey = product.Name.ToLowerInvariant(),
                    CanonicalProduct = MapToProductInfo(canonical),
                    DuplicateProducts = allInGroup
                        .Where(p => p.Id != canonical.Id)
                        .Select(MapToProductInfo)
                        .ToList(),
                    Type = DuplicateType.SimilarName,
                    SimilarityScore = similar.Average(p =>
                        CalculateSimilarity(product.Name, p.Name))
                });
            }
        }

        return groups;
    }

    private double CalculateSimilarity(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            return 0;

        s1 = s1.ToLowerInvariant().Trim();
        s2 = s2.ToLowerInvariant().Trim();

        if (s1 == s2)
            return 1.0;

        var distance = LevenshteinDistance(s1, s2);
        var maxLength = Math.Max(s1.Length, s2.Length);
        return 1.0 - ((double)distance / maxLength);
    }

    private int LevenshteinDistance(string s1, string s2)
    {
        var n = s1.Length;
        var m = s2.Length;
        var d = new int[n + 1, m + 1];

        for (var i = 0; i <= n; i++) d[i, 0] = i;
        for (var j = 0; j <= m; j++) d[0, j] = j;

        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }

    private ProductInfo MapToProductInfo(Product product)
    {
        return new ProductInfo
        {
            ProductId = product.Id,
            SKU = product.SKU,
            Name = product.Name,
            CreatedAt = product.CreatedAt,
            Stock = product.Stock
        };
    }
}
```

---

### AŞAMA 2: Siparişleri Onaylanmamış Yap

```csharp
/// <summary>
/// Etkilenen siparişleri onaylanmamış yap
/// </summary>
public async Task<OrderResetResult> ResetAffectedOrdersAsync(
    List<int> orderIds,
    CancellationToken ct = default)
{
    _logger.LogInformation("Resetting {Count} orders to unapproved status", orderIds.Count);

    var result = new OrderResetResult
    {
        TotalOrders = orderIds.Count,
        StartedAt = DateTime.UtcNow
    };

    using var transaction = await _context.Database.BeginTransactionAsync(ct);

    try
    {
        // Adım 1: Siparişleri getir
        var orders = await _context.SalesOrders
            .Include(o => o.Lines)
            .Where(o => orderIds.Contains(o.Id))
            .ToListAsync(ct);

        // Adım 2: Siparişleri onaylanmamış yap
        foreach (var order in orders)
        {
            order.Status = "PENDING";  // Onaylanmamış
            order.ApprovedAt = null;
            order.ApprovedBy = null;
            order.UpdatedAt = DateTime.UtcNow;
            result.ResetOrders++;
        }

        // Adım 3: Faturalarını sil
        var invoices = await _context.OrderInvoices
            .Where(i => orderIds.Contains(i.SalesOrderId))
            .ToListAsync(ct);

        _context.OrderInvoices.RemoveRange(invoices);
        result.DeletedInvoices = invoices.Count;

        // Adım 4: Stok hareketlerini sil
        var stockMovements = await _context.StockMovements
            .Where(m => orderIds.Contains(m.OrderId ?? 0))
            .ToListAsync(ct);

        _context.StockMovements.RemoveRange(stockMovements);
        result.DeletedStockMovements = stockMovements.Count;

        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        result.Success = true;

        _logger.LogInformation(
            "Reset complete: {Orders} orders, {Invoices} invoices, {Movements} stock movements",
            result.ResetOrders, result.DeletedInvoices, result.DeletedStockMovements);
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync(ct);
        result.Success = false;
        result.ErrorMessage = ex.Message;
        _logger.LogError(ex, "Failed to reset orders");
    }

    result.CompletedAt = DateTime.UtcNow;
    return result;
}
```

---

### AŞAMA 3: Tekrar Eden Ürünleri Sil

```csharp
/// <summary>
/// Tekrar eden ürünleri sil
/// </summary>
public async Task<ProductDeletionResult> DeleteDuplicateProductsAsync(
    List<DuplicateProductGroup> duplicateGroups,
    CancellationToken ct = default)
{
    _logger.LogInformation("Deleting {Count} duplicate products",
        duplicateGroups.Sum(g => g.DuplicateProducts.Count));

    var result = new ProductDeletionResult
    {
        TotalToDelete = duplicateGroups.Sum(g => g.DuplicateProducts.Count),
        StartedAt = DateTime.UtcNow
    };

    using var transaction = await _context.Database.BeginTransactionAsync(ct);

    try
    {
        foreach (var group in duplicateGroups)
        {
            foreach (var duplicate in group.DuplicateProducts)
            {
                // Adım 1: Ürünü getir
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == duplicate.ProductId, ct);

                if (product == null)
                    continue;

                // Adım 2: Varyantlarını sil
                var variants = await _context.ProductVariants
                    .Where(v => v.ProductId == product.Id)
                    .ToListAsync(ct);

                _context.ProductVariants.RemoveRange(variants);
                result.DeletedVariants += variants.Count;

                // Adım 3: Ürünü sil
                _context.Products.Remove(product);
                result.DeletedProducts++;

                _logger.LogInformation(
                    "Deleted duplicate product {SKU} (ID: {Id})",
                    product.SKU, product.Id);
            }
        }

        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        result.Success = true;

        _logger.LogInformation(
            "Deletion complete: {Products} products, {Variants} variants",
            result.DeletedProducts, result.DeletedVariants);
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync(ct);
        result.Success = false;
        result.ErrorMessage = ex.Message;
        _logger.LogError(ex, "Failed to delete duplicate products");
    }

    result.CompletedAt = DateTime.UtcNow;
    return result;
}
```

---

### AŞAMA 4: Siparişleri Yeniden Onayla

```csharp
/// <summary>
/// Siparişleri yeniden onayla (temiz verilerle)
/// </summary>
public async Task<OrderReapprovalResult> ReapproveOrdersAsync(
    List<int> orderIds,
    string approvedBy,
    CancellationToken ct = default)
{
    _logger.LogInformation("Reapproving {Count} orders", orderIds.Count);

    var result = new OrderReapprovalResult
    {
        TotalOrders = orderIds.Count,
        StartedAt = DateTime.UtcNow
    };

    using var transaction = await _context.Database.BeginTransactionAsync(ct);

    try
    {
        // Adım 1: Siparişleri getir
        var orders = await _context.SalesOrders
            .Include(o => o.Lines)
            .Where(o => orderIds.Contains(o.Id))
            .ToListAsync(ct);

        // Adım 2: Siparişleri onayla
        foreach (var order in orders)
        {
            // Siparişin tüm satırlarının geçerli ürünlere referans verdiğini kontrol et
            var invalidLines = new List<SalesOrderLine>();

            foreach (var line in order.Lines)
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == line.VariantId, ct);

                if (product == null)
                {
                    invalidLines.Add(line);
                }
            }

            if (invalidLines.Any())
            {
                result.FailedOrders++;
                result.Errors.Add($"Order {order.OrderNo} has invalid product references");
                continue;
            }

            // Siparişi onayla
            order.Status = "APPROVED";
            order.ApprovedAt = DateTime.UtcNow;
            order.ApprovedBy = approvedBy;
            order.UpdatedAt = DateTime.UtcNow;
            result.ApprovedOrders++;

            _logger.LogInformation("Reapproved order {OrderNo}", order.OrderNo);
        }

        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        result.Success = result.FailedOrders == 0;

        _logger.LogInformation(
            "Reapproval complete: {Approved} approved, {Failed} failed",
            result.ApprovedOrders, result.FailedOrders);
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync(ct);
        result.Success = false;
        result.ErrorMessage = ex.Message;
        _logger.LogError(ex, "Failed to reapprove orders");
    }

    result.CompletedAt = DateTime.UtcNow;
    return result;
}
```

---

### AŞAMA 5: Consolidation Orchestration

```csharp
/// <summary>
/// Tüm consolidation işlemini yönet
/// </summary>
public async Task<ConsolidationExecutionResult> ExecuteFullConsolidationAsync(
    string approvedBy,
    CancellationToken ct = default)
{
    _logger.LogInformation("Starting full duplicate consolidation...");

    var result = new ConsolidationExecutionResult
    {
        StartedAt = DateTime.UtcNow,
        Phases = new List<PhaseResult>()
    };

    try
    {
        // FAZE 1: Tekrar eden ürünleri tespit et
        _logger.LogInformation("Phase 1: Analyzing duplicate products...");
        var analysis = await AnalyzeDuplicateProductsAsync(ct);
        result.Phases.Add(new PhaseResult
        {
            Phase = 1,
            Name = "Duplicate Analysis",
            Status = "SUCCESS",
            Details = $"Found {analysis.TotalDuplicateProducts} duplicates affecting {analysis.AffectedOrders} orders"
        });

        if (analysis.TotalDuplicateProducts == 0)
        {
            result.Success = true;
            result.Message = "No duplicate products found";
            return result;
        }

        // FAZE 2: Siparişleri onaylanmamış yap
        _logger.LogInformation("Phase 2: Resetting orders...");
        var affectedOrderIds = await GetAffectedOrderIdsAsync(analysis, ct);
        var resetResult = await ResetAffectedOrdersAsync(affectedOrderIds, ct);
        result.Phases.Add(new PhaseResult
        {
            Phase = 2,
            Name = "Order Reset",
            Status = resetResult.Success ? "SUCCESS" : "FAILED",
            Details = $"Reset {resetResult.ResetOrders} orders, deleted {resetResult.DeletedInvoices} invoices"
        });

        if (!resetResult.Success)
            throw new InvalidOperationException("Order reset failed");

        // FAZE 3: Tekrar eden ürünleri sil
        _logger.LogInformation("Phase 3: Deleting duplicate products...");
        var allDuplicates = analysis.SKUDuplicates
            .Concat(analysis.SimilarNameDuplicates)
            .ToList();
        var deleteResult = await DeleteDuplicateProductsAsync(allDuplicates, ct);
        result.Phases.Add(new PhaseResult
        {
            Phase = 3,
            Name = "Product Deletion",
            Status = deleteResult.Success ? "SUCCESS" : "FAILED",
            Details = $"Deleted {deleteResult.DeletedProducts} products and {deleteResult.DeletedVariants} variants"
        });

        if (!deleteResult.Success)
            throw new InvalidOperationException("Product deletion failed");

        // FAZE 4: Siparişleri yeniden onayla
        _logger.LogInformation("Phase 4: Reapproving orders...");
        var reapprovalResult = await ReapproveOrdersAsync(affectedOrderIds, approvedBy, ct);
        result.Phases.Add(new PhaseResult
        {
            Phase = 4,
            Name = "Order Reapproval",
            Status = reapprovalResult.Success ? "SUCCESS" : "FAILED",
            Details = $"Reapproved {reapprovalResult.ApprovedOrders} orders"
        });

        if (!reapprovalResult.Success)
            throw new InvalidOperationException("Order reapproval failed");

        result.Success = true;
        result.Message = "Consolidation completed successfully";

        _logger.LogInformation("Full consolidation completed successfully");
    }
    catch (Exception ex)
    {
        result.Success = false;
        result.ErrorMessage = ex.Message;
        _logger.LogError(ex, "Full consolidation failed");
    }

    result.CompletedAt = DateTime.UtcNow;
    return result;
}

private async Task<List<int>> GetAffectedOrderIdsAsync(
    DuplicateProductAnalysis analysis,
    CancellationToken ct)
{
    var duplicateIds = analysis.SKUDuplicates
        .Concat(analysis.SimilarNameDuplicates)
        .SelectMany(g => g.DuplicateProducts)
        .Select(p => p.ProductId)
        .ToList();

    var orderIds = await _context.SalesOrderLines
        .Where(l => duplicateIds.Contains(l.VariantId))
        .Select(l => l.SalesOrderId)
        .Distinct()
        .ToListAsync(ct);

    return orderIds;
}
```

---

## 📋 DTO Modelleri

```csharp
// File: src/Katana.Core/DTOs/ConsolidationDtos.cs

public class DuplicateProductAnalysis
{
    public DateTime AnalyzedAt { get; set; }
    public List<DuplicateProductGroup> SKUDuplicates { get; set; } = new();
    public List<DuplicateProductGroup> SimilarNameDuplicates { get; set; } = new();
    public int TotalDuplicateProducts { get; set; }
    public int AffectedOrders { get; set; }
}

public class DuplicateProductGroup
{
    public string GroupKey { get; set; }
    public ProductInfo CanonicalProduct { get; set; }
    public List<ProductInfo> DuplicateProducts { get; set; } = new();
    public DuplicateType Type { get; set; }
    public double SimilarityScore { get; set; }
}

public class ProductInfo
{
    public long ProductId { get; set; }
    public string SKU { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal Stock { get; set; }
}

public class OrderResetResult
{
    public bool Success { get; set; }
    public int TotalOrders { get; set; }
    public int ResetOrders { get; set; }
    public int DeletedInvoices { get; set; }
    public int DeletedStockMovements { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
}

public class ProductDeletionResult
{
    public bool Success { get; set; }
    public int TotalToDelete { get; set; }
    public int DeletedProducts { get; set; }
    public int DeletedVariants { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
}

public class OrderReapprovalResult
{
    public bool Success { get; set; }
    public int TotalOrders { get; set; }
    public int ApprovedOrders { get; set; }
    public int FailedOrders { get; set; }
    public List<string> Errors { get; set; } = new();
    public string ErrorMessage { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
}

public class ConsolidationExecutionResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public string ErrorMessage { get; set; }
    public List<PhaseResult> Phases { get; set; } = new();
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
}

public class PhaseResult
{
    public int Phase { get; set; }
    public string Name { get; set; }
    public string Status { get; set; }  // SUCCESS, FAILED
    public string Details { get; set; }
}

public enum DuplicateType
{
    SameSKU,
    SimilarName,
    VariantDuplicate
}
```

---

## 🔌 API Endpoint

```csharp
// File: src/Katana.API/Controllers/Admin/ConsolidationController.cs

[ApiController]
[Route("api/admin/consolidation")]
[Authorize(Roles = "Admin")]
public class ConsolidationController : ControllerBase
{
    private readonly IDuplicateConsolidationService _consolidationService;

    /// <summary>
    /// Tekrar eden ürünleri analiz et (preview)
    /// </summary>
    [HttpGet("analyze")]
    public async Task<ActionResult<DuplicateProductAnalysis>> AnalyzeDuplicates()
    {
        var analysis = await _consolidationService.AnalyzeDuplicateProductsAsync();
        return Ok(analysis);
    }

    /// <summary>
    /// Consolidation'ı başlat
    /// </summary>
    [HttpPost("execute")]
    public async Task<ActionResult<ConsolidationExecutionResult>> ExecuteConsolidation(
        [FromBody] ConsolidationRequest request)
    {
        if (!request.ConfirmDelete)
            return BadRequest("Silme işlemini onaylamanız gerekir");

        var result = await _consolidationService
            .ExecuteFullConsolidationAsync(User.Identity.Name);

        return Ok(result);
    }
}

public class ConsolidationRequest
{
    public bool ConfirmDelete { get; set; }
}
```

---

## 📊 Execution Plan

### Gün 1: Hazırlık (1 saat)

```
☐ DuplicateConsolidationService.cs implement et
☐ DTO modelleri oluştur
☐ API endpoint'lerini ekle
☐ Test et
```

### Gün 2: Analiz (30 dakika)

```
☐ GET /api/admin/consolidation/analyze çağır
☐ Tekrar eden ürünleri incele
☐ Etkilenen siparişleri kontrol et
☐ Müşteriye rapor sun
```

### Gün 3: Consolidation (1 saat)

```
☐ Backup al
☐ POST /api/admin/consolidation/execute çağır
☐ İşlemi izle
☐ Tamamlanmasını bekle
```

### Gün 4: Doğrulama (30 dakika)

```
☐ Tekrar eden ürünler silindi mi?
☐ Siparişler onaylanmış mı?
☐ Stok hareketleri doğru mu?
☐ Faturalar oluşturuldu mu?
```

---

## ✅ Başarı Kriterleri

```
✓ Tekrar eden ürünler silindi
✓ Siparişler onaylanmamış yapıldı
✓ Faturalar silindi
✓ Stok hareketleri silindi
✓ Siparişler yeniden onaylandı
✓ Veri bütünlüğü korundu
✓ Müşteri memnun
```

---

## 🚀 Hızlı Başlangıç

```bash
# 1. Analiz yap
GET /api/admin/consolidation/analyze

# 2. Sonuçları incele
# Kaç tekrar eden ürün?
# Kaç sipariş etkilendi?

# 3. Consolidation başlat
POST /api/admin/consolidation/execute
Body: { "confirmDelete": true }

# 4. Tamamlanmasını bekle
# Tüm aşamalar başarılı mı?

# 5. Doğrulama yap
# Veriler temiz mi?
```

---

## 💡 Önemli Notlar

```
1. Backup al (BACKUP DATABASE)
2. Analiz yap (preview)
3. Müşteri onayını al
4. Consolidation başlat
5. Doğrulama yap
6. Müşteriye rapor sun
```

**Başarılar!** 🚀
