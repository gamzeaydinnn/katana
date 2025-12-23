# Veri Temizliği ve Soft Reset Stratejisi

## BÖLÜM 1: Genel Strateji ve Felsefe

### 1.1 Neden Soft Reset Gerekli?

Senin durumun:

- Luca'ya **hatalı stok kartları** gönderilmiş (?, -V2, -V3 vb.)
- Katana'da bu hatalı kartlara referans veren **siparişler onaylanmış**
- Yeni mantığı test edemezsin çünkü **eski hatalı veriler** sistemde dolaşıyor

**Çözüm Felsefesi:**

```
Eski Durum: Hatalı Veri → Luca'da Hatalı Kayıt → Siparişler Bağlı
                                    ↓
Soft Reset: Hatalı Veriyi Sil → Luca'da Temiz → Siparişleri "Gönderilmemiş" Yap
                                    ↓
Yeni Durum: Temiz Veri → Yeni Mantık Test → Doğru Sonuç
```

### 1.2 Soft Reset Nedir?

**Hard Reset (Tehlikeli):**

```sql
DELETE FROM Products WHERE SKU LIKE '%?%' OR SKU LIKE '%-V%';
DELETE FROM SalesOrderLines WHERE ProductId IN (...);
-- Veri kaybı, geri dönüş yok
```

**Soft Reset (Güvenli):**

```
1. Luca'da hatalı kartları sil (API ile)
2. Katana'da siparişleri "gönderilmemiş" olarak işaretle (Flag)
3. Ürünleri "inactive" yap (silme değil)
4. Gerekirse geri dönüş yapabilirsin
```

---

## BÖLÜM 2: Adım 1 - Veritabanı Şeması Güncelleme

### 2.1 SalesOrderLines Tablosuna Yeni Alanlar Ekle

```sql
-- Migration: AddSyncFlagsToSalesOrderLines
ALTER TABLE SalesOrderLines ADD COLUMN IsSyncedToLuca BIT DEFAULT 0;
ALTER TABLE SalesOrderLines ADD COLUMN LukaErrorLog NVARCHAR(MAX) NULL;
ALTER TABLE SalesOrderLines ADD COLUMN LastSyncAttempt DATETIME2 NULL;
ALTER TABLE SalesOrderLines ADD COLUMN SyncRetryCount INT DEFAULT 0;

-- Index oluştur (performans için)
CREATE INDEX IX_SalesOrderLines_IsSyncedToLuca
ON SalesOrderLines(IsSyncedToLuka, LastSyncAttempt);
```

### 2.2 Products Tablosuna Yeni Alanlar Ekle

```sql
-- Migration: AddCleanupFlagsToProducts
ALTER TABLE Products ADD COLUMN IsMarkedForCleanup BIT DEFAULT 0;
ALTER TABLE Products ADD COLUMN CleanupReason NVARCHAR(500) NULL;
ALTER TABLE Products ADD COLUMN OriginalLucaId BIGINT NULL;

-- Index oluştur
CREATE INDEX IX_Products_IsMarkedForCleanup
ON Products(IsMarkedForCleanup, IsActive);
```

### 2.3 Audit Tablosu Oluştur

```sql
-- Temizlik işlemlerinin kaydını tut
CREATE TABLE DataCleanupAudit (
    Id BIGINT PRIMARY KEY IDENTITY(1,1),
    OperationType NVARCHAR(50),  -- 'DELETE_LUCA', 'RESET_SYNC', 'MARK_INACTIVE'
    EntityType NVARCHAR(50),     -- 'StockCard', 'SalesOrder', 'Product'
    EntityId BIGINT,
    EntityName NVARCHAR(500),
    Reason NVARCHAR(500),
    PerformedBy NVARCHAR(100),
    PerformedAt DATETIME2 DEFAULT GETUTCDATE(),
    Status NVARCHAR(50),         -- 'SUCCESS', 'FAILED', 'PENDING'
    ErrorMessage NVARCHAR(MAX) NULL
);
```

---

## BÖLÜM 3: Adım 2 - Luca Tarafında Hatalı Kartları Silme

### 3.1 Hatalı Kartları Tespit Etme Scripti

```csharp
// File: src/Katana.Business/Services/DataCleanupService.cs

public class DataCleanupService : IDataCleanupService
{
    private readonly ILucaService _lucaService;
    private readonly IntegrationDbContext _context;
    private readonly ILogger<DataCleanupService> _logger;

    public DataCleanupService(
        ILucaService lucaService,
        IntegrationDbContext context,
        ILogger<DataCleanupService> logger)
    {
        _lucaService = lucaService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Luca'da hatalı stok kartlarını tespit et
    /// Hatalı: ?, -V2, -V3, ABCABC vb.
    /// </summary>
    public async Task<List<BadStockCardInfo>> IdentifyBadStockCardsAsync(
        CancellationToken ct = default)
    {
        _logger.LogInformation("Identifying bad stock cards in Luca...");

        var allCards = await _lucaService.ListStockCardsAsync(ct);
        var badCards = new List<BadStockCardInfo>();

        foreach (var card in allCards)
        {
            var issues = new List<string>();

            // Kontrol 1: ? karakteri (Encoding hatası)
            if (card.StokAdi?.Contains('?') == true ||
                card.StokKodu?.Contains('?') == true)
            {
                issues.Add("CharacterEncoding");
            }

            // Kontrol 2: -V2, -V3 vb. (Versioning)
            if (System.Text.RegularExpressions.Regex.IsMatch(
                card.StokKodu ?? "", @"-V\d+$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                issues.Add("Versioning");
            }

            // Kontrol 3: Concatenation (ABCABC, DEFDEF)
            if (IsConcatenationError(card.StokKodu))
            {
                issues.Add("Concatenation");
            }

            if (issues.Any())
            {
                badCards.Add(new BadStockCardInfo
                {
                    SkartId = card.SkartId,
                    StokKodu = card.StokKodu,
                    StokAdi = card.StokAdi,
                    Issues = issues,
                    Severity = CalculateSeverity(issues)
                });
            }
        }

        _logger.LogInformation(
            "Found {Count} bad stock cards: {Encoding} encoding, {Versioning} versioning, {Concat} concatenation",
            badCards.Count,
            badCards.Count(c => c.Issues.Contains("CharacterEncoding")),
            badCards.Count(c => c.Issues.Contains("Versioning")),
            badCards.Count(c => c.Issues.Contains("Concatenation")));

        return badCards;
    }

    /// <summary>
    /// Hatalı kartları Luca'dan sil
    /// </summary>
    public async Task<DataCleanupResult> DeleteBadStockCardsAsync(
        List<BadStockCardInfo> badCards,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Deleting {Count} bad stock cards from Luca...", badCards.Count);

        var result = new DataCleanupResult
        {
            TotalToDelete = badCards.Count,
            StartedAt = DateTime.UtcNow
        };

        foreach (var card in badCards)
        {
            try
            {
                // Luca API'sini çağır
                await _lucaService.DeleteStockCardAsync(card.SkartId, ct);

                result.SuccessfulDeletions++;

                // Audit log
                await LogCleanupActionAsync(
                    "DELETE_LUCA",
                    "StockCard",
                    card.SkartId,
                    card.StokKodu,
                    string.Join(", ", card.Issues),
                    "SUCCESS",
                    null);

                _logger.LogInformation(
                    "Deleted stock card {Code} (ID: {Id})",
                    card.StokKodu, card.SkartId);
            }
            catch (Exception ex)
            {
                result.FailedDeletions++;

                await LogCleanupActionAsync(
                    "DELETE_LUCA",
                    "StockCard",
                    card.SkartId,
                    card.StokKodu,
                    string.Join(", ", card.Issues),
                    "FAILED",
                    ex.Message);

                _logger.LogError(ex,
                    "Failed to delete stock card {Code} (ID: {Id})",
                    card.StokKodu, card.SkartId);
            }
        }

        result.CompletedAt = DateTime.UtcNow;
        result.Success = result.FailedDeletions == 0;

        return result;
    }

    private bool IsConcatenationError(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 4)
            return false;

        var halfLength = value.Length / 2;
        var firstHalf = value.Substring(0, halfLength);
        var secondHalf = value.Substring(halfLength);

        return firstHalf.Equals(secondHalf, StringComparison.OrdinalIgnoreCase);
    }

    private string CalculateSeverity(List<string> issues)
    {
        if (issues.Contains("CharacterEncoding"))
            return "HIGH";
        if (issues.Count > 1)
            return "MEDIUM";
        return "LOW";
    }

    private async Task LogCleanupActionAsync(
        string operationType,
        string entityType,
        long entityId,
        string entityName,
        string reason,
        string status,
        string? errorMessage)
    {
        var audit = new DataCleanupAudit
        {
            OperationType = operationType,
            EntityType = entityType,
            EntityId = entityId,
            EntityName = entityName,
            Reason = reason,
            PerformedBy = "System",
            Status = status,
            ErrorMessage = errorMessage
        };

        _context.DataCleanupAudits.Add(audit);
        await _context.SaveChangesAsync();
    }
}
```

---

## BÖLÜM 4: Adım 3 - Siparişleri "Gönderilmemiş" Olarak İşaretle

### 4.1 Soft Reset Servisi

```csharp
// File: src/Katana.Business/Services/SoftResetService.cs

public class SoftResetService : ISoftResetService
{
    private readonly IntegrationDbContext _context;
    private readonly ILogger<SoftResetService> _logger;

    public async Task<SoftResetResult> ResetSalesOrderSyncAsync(
        List<int> salesOrderIds,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Resetting sync status for {Count} sales orders",
            salesOrderIds.Count);

        var result = new SoftResetResult
        {
            TotalOrders = salesOrderIds.Count,
            StartedAt = DateTime.UtcNow
        };

        using var transaction = await _context.Database.BeginTransactionAsync(ct);

        try
        {
            // Adım 1: Siparişlerin tüm satırlarını getir
            var orderLines = await _context.SalesOrderLines
                .Where(l => salesOrderIds.Contains(l.SalesOrderId))
                .ToListAsync(ct);

            // Adım 2: Sync flaglarını sıfırla
            foreach (var line in orderLines)
            {
                line.IsSyncedToLuca = false;
                line.LukaErrorLog = null;
                line.LastSyncAttempt = null;
                line.SyncRetryCount = 0;
            }

            result.ResetOrderLines = orderLines.Count;

            // Adım 3: Siparişlerin kendisini de güncelle
            var orders = await _context.SalesOrders
                .Where(o => salesOrderIds.Contains(o.Id))
                .ToListAsync(ct);

            foreach (var order in orders)
            {
                order.Status = "PENDING_SYNC";  // Yeniden gönderilmeyi bekliyor
                order.UpdatedAt = DateTime.UtcNow;
            }

            result.ResetOrders = orders.Count;

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            result.Success = true;

            _logger.LogInformation(
                "Successfully reset {OrderCount} orders and {LineCount} lines",
                result.ResetOrders, result.ResetOrderLines);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            result.Success = false;
            result.ErrorMessage = ex.Message;

            _logger.LogError(ex, "Failed to reset sales order sync");
        }

        result.CompletedAt = DateTime.UtcNow;
        return result;
    }

    /// <summary>
    /// Hatalı ürünlere referans veren siparişleri bul
    /// </summary>
    public async Task<List<int>> FindOrdersWithBadProductsAsync(
        List<string> badProductSkus,
        CancellationToken ct = default)
    {
        var orderIds = await _context.SalesOrderLines
            .Where(l => badProductSkus.Contains(l.SKU))
            .Select(l => l.SalesOrderId)
            .Distinct()
            .ToListAsync(ct);

        return orderIds;
    }
}
```

---

## BÖLÜM 5: Adım 4 - Ürünleri Inactive Olarak İşaretle

### 5.1 Ürün Temizleme Servisi

```csharp
public async Task<ProductCleanupResult> MarkProductsForCleanupAsync(
    List<long> productIds,
    string reason,
    CancellationToken ct = default)
{
    _logger.LogInformation(
        "Marking {Count} products for cleanup: {Reason}",
        productIds.Count, reason);

    var result = new ProductCleanupResult
    {
        TotalProducts = productIds.Count,
        Reason = reason,
        StartedAt = DateTime.UtcNow
    };

    var products = await _context.Products
        .Where(p => productIds.Contains(p.Id))
        .ToListAsync(ct);

    foreach (var product in products)
    {
        product.IsActive = false;
        product.IsMarkedForCleanup = true;
        product.CleanupReason = reason;
        product.UpdatedAt = DateTime.UtcNow;
        product.Description = $"[CLEANUP: {reason}] {product.Description}";

        result.MarkedProducts++;
    }

    await _context.SaveChangesAsync(ct);

    _logger.LogInformation(
        "Marked {Count} products as inactive",
        result.MarkedProducts);

    result.CompletedAt = DateTime.UtcNow;
    result.Success = true;

    return result;
}
```

---

## BÖLÜM 6: Adım 5 - Header-Line Mimarisi (Luka'ya Gönderme)

### 6.1 Yeni Veri Modeli

```csharp
// File: src/Katana.Core/DTOs/LucaSyncDtos.cs

/// <summary>
/// Luca'ya gönderilecek sipariş başlığı
/// </summary>
public class LucaOrderHeaderDto
{
    public string OrderNo { get; set; }
    public DateTime OrderDate { get; set; }
    public string CustomerCode { get; set; }
    public string CustomerName { get; set; }

    // Başlık seviyesinde ürün (Canonical)
    public string ProductCode { get; set; }      // Ana ürün SKU
    public string ProductName { get; set; }
    public decimal TotalQuantity { get; set; }   // Tüm satırların toplamı
    public decimal TotalAmount { get; set; }

    // BOM Bilgisi
    public bool HasBOM { get; set; }
    public List<LucaBOMComponentDto> BOMComponents { get; set; }

    // Detay satırları
    public List<LucaOrderLineDto> Lines { get; set; }
}

/// <summary>
/// Luca'ya gönderilecek sipariş satırı
/// </summary>
public class LucaOrderLineDto
{
    public string VariantCode { get; set; }      // Varyant SKU
    public string VariantName { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineAmount { get; set; }
    public Dictionary<string, string> Attributes { get; set; }  // Renk, Beden, vb.
}

/// <summary>
/// BOM Bileşeni (Reçete)
/// </summary>
public class LucaBOMComponentDto
{
    public string ComponentCode { get; set; }
    public string ComponentName { get; set; }
    public decimal QuantityPerUnit { get; set; }
    public string Unit { get; set; }
    public decimal TotalRequired { get; set; }   // Sipariş miktarı × BOM oranı
}
```

### 6.2 Header-Line Dönüştürme Servisi

```csharp
// File: src/Katana.Business/Services/LucaSyncTransformService.cs

public class LucaSyncTransformService : ILucaSyncTransformService
{
    private readonly IVariantGroupingService _variantGrouping;
    private readonly IBOMService _bomService;
    private readonly IntegrationDbContext _context;
    private readonly ILogger<LucaSyncTransformService> _logger;

    /// <summary>
    /// Katana sipariş satırlarını Luca Header-Line formatına dönüştür
    /// </summary>
    public async Task<LucaOrderHeaderDto> TransformOrderToLucaFormatAsync(
        int salesOrderId,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Transforming order {OrderId} to Luca format", salesOrderId);

        var order = await _context.SalesOrders
            .Include(o => o.Lines)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == salesOrderId, ct);

        if (order == null)
            throw new ArgumentException($"Order not found: {salesOrderId}");

        // Adım 1: Tüm satırları grupla (Canonical ürüne göre)
        var groupedLines = await GroupLinesByCanonicalProductAsync(order.Lines, ct);

        // Adım 2: Her grup için Header oluştur
        var headers = new List<LucaOrderHeaderDto>();

        foreach (var group in groupedLines)
        {
            var canonicalProductId = group.Key;
            var lines = group.ToList();

            // Canonical ürünü getir
            var canonicalProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == canonicalProductId, ct);

            if (canonicalProduct == null)
                continue;

            // Header oluştur
            var header = new LucaOrderHeaderDto
            {
                OrderNo = order.OrderNo,
                OrderDate = order.OrderCreatedDate ?? DateTime.UtcNow,
                CustomerCode = order.Customer?.Code ?? "",
                CustomerName = order.Customer?.Title ?? "",
                ProductCode = canonicalProduct.SKU,
                ProductName = canonicalProduct.Name,
                TotalQuantity = lines.Sum(l => l.Quantity),
                TotalAmount = lines.Sum(l => l.Quantity * (l.UnitPrice ?? 0)),
                Lines = new List<LucaOrderLineDto>()
            };

            // Adım 3: BOM bilgisini ekle (eğer varsa)
            if (await _bomService.HasBOMAsync(canonicalProductId))
            {
                header.HasBOM = true;
                header.BOMComponents = await GetBOMComponentsForLucaAsync(
                    canonicalProductId,
                    header.TotalQuantity,
                    ct);
            }

            // Adım 4: Satırları ekle
            foreach (var line in lines)
            {
                var variant = await _context.ProductVariants
                    .FirstOrDefaultAsync(v => v.Id == line.VariantId, ct);

                var lineDto = new LucaOrderLineDto
                {
                    VariantCode = line.SKU,
                    VariantName = line.ProductName,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice ?? 0,
                    LineAmount = line.Quantity * (line.UnitPrice ?? 0),
                    Attributes = variant != null
                        ? ParseVariantAttributes(variant.Attributes)
                        : new Dictionary<string, string>()
                };

                header.Lines.Add(lineDto);
            }

            headers.Add(header);
        }

        // Eğer birden fazla header varsa, ilkini döndür (veya hepsini döndür)
        return headers.FirstOrDefault() ?? throw new InvalidOperationException(
            "No valid headers generated from order lines");
    }

    /// <summary>
    /// Satırları Canonical ürüne göre grupla
    /// </summary>
    private async Task<IGrouping<long, SalesOrderLine>[]> GroupLinesByCanonicalProductAsync(
        ICollection<SalesOrderLine> lines,
        CancellationToken ct)
    {
        var groupedLines = new Dictionary<long, List<SalesOrderLine>>();

        foreach (var line in lines)
        {
            // Varyantın ana ürünü bul
            var variant = await _context.ProductVariants
                .FirstOrDefaultAsync(v => v.Id == line.VariantId, ct);

            var canonicalProductId = variant?.ProductId ?? line.VariantId;

            if (!groupedLines.ContainsKey(canonicalProductId))
                groupedLines[canonicalProductId] = new List<SalesOrderLine>();

            groupedLines[canonicalProductId].Add(line);
        }

        return groupedLines
            .GroupBy(kvp => kvp.Key, kvp => kvp.Value)
            .SelectMany(g => g.SelectMany(list => list)
                .GroupBy(l => g.Key))
            .ToArray();
    }

    /// <summary>
    /// BOM bileşenlerini Luca formatına dönüştür
    /// </summary>
    private async Task<List<LucaBOMComponentDto>> GetBOMComponentsForLucaAsync(
        long productId,
        decimal orderQuantity,
        CancellationToken ct)
    {
        var bomComponents = await _bomService.GetBOMComponentsAsync(productId);

        return bomComponents.Select(c => new LucaBOMComponentDto
        {
            ComponentCode = c.ComponentSKU,
            ComponentName = c.ComponentName,
            QuantityPerUnit = c.Quantity,
            Unit = c.Unit,
            TotalRequired = c.Quantity * orderQuantity
        }).ToList();
    }

    private Dictionary<string, string> ParseVariantAttributes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, string>();

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }
}
```

---

## BÖLÜM 7: Adım 6 - Benzerlik Algoritması ve Otomatik Karar

### 7.1 Geliştirilmiş Duplicate Detector

```csharp
// File: src/Katana.Business/Services/Deduplication/SmartDuplicateDetector.cs

public class SmartDuplicateDetector : IDuplicateDetector
{
    private const double HIGH_SIMILARITY_THRESHOLD = 0.90;
    private const double MEDIUM_SIMILARITY_THRESHOLD = 0.75;

    /// <summary>
    /// Benzerlik skoru 0.90+ ise otomatik olarak "Encoding Issue" kategorisine sok
    /// </summary>
    public async Task<AutomaticCleanupDecision> MakeAutomaticDecisionAsync(
        string text1,
        string text2,
        CancellationToken ct = default)
    {
        var similarity = CalculateSimilarity(text1, text2);

        var decision = new AutomaticCleanupDecision
        {
            Text1 = text1,
            Text2 = text2,
            SimilarityScore = similarity,
            DecisionTime = DateTime.UtcNow
        };

        if (similarity >= HIGH_SIMILARITY_THRESHOLD)
        {
            // Çok benzer → Encoding hatası olabilir
            decision.Category = DuplicateCategory.CharacterEncoding;
            decision.Action = CleanupAction.AutoDelete;
            decision.Confidence = "HIGH";
            decision.Reason = $"Similarity score {similarity:P} exceeds threshold {HIGH_SIMILARITY_THRESHOLD:P}";
        }
        else if (similarity >= MEDIUM_SIMILARITY_THRESHOLD)
        {
            // Orta benzerlik → Manuel inceleme gerekli
            decision.Category = DuplicateCategory.Mixed;
            decision.Action = CleanupAction.RequiresApproval;
            decision.Confidence = "MEDIUM";
            decision.Reason = $"Similarity score {similarity:P} requires manual review";
        }
        else
        {
            // Düşük benzerlik → Farklı ürünler
            decision.Category = DuplicateCategory.None;
            decision.Action = CleanupAction.Skip;
            decision.Confidence = "HIGH";
            decision.Reason = "Not similar enough to be duplicates";
        }

        return await Task.FromResult(decision);
    }

    /// <summary>
    /// Levenshtein Distance ile benzerlik hesapla
    /// Formül: similarity = 1 - (distance / maxLength)
    /// </summary>
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

    /// <summary>
    /// Levenshtein Distance Algoritması
    /// Matematiksel Formül:
    /// lev(a, b) = |a|                           if |b| = 0
    ///           = |b|                           if |a| = 0
    ///           = lev(tail(a), tail(b))         if a[0] = b[0]
    ///           = 1 + min(lev(tail(a), b),
    ///                      lev(a, tail(b)),
    ///                      lev(tail(a), tail(b))) otherwise
    /// </summary>
    private int LevenshteinDistance(string s1, string s2)
    {
        var n = s1.Length;
        var m = s2.Length;
        var d = new int[n + 1, m + 1];

        // İlk satır ve sütunu doldur
        for (var i = 0; i <= n; i++)
            d[i, 0] = i;
        for (var j = 0; j <= m; j++)
            d[0, j] = j;

        // DP tablosunu doldur
        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;

                d[i, j] = Math.Min(
                    Math.Min(
                        d[i - 1, j] + 1,      // Silme
                        d[i, j - 1] + 1),     // Ekleme
                    d[i - 1, j - 1] + cost); // Değiştirme
            }
        }

        return d[n, m];
    }
}

/// <summary>
/// Otomatik karar modeli
/// </summary>
public class AutomaticCleanupDecision
{
    public string Text1 { get; set; }
    public string Text2 { get; set; }
    public double SimilarityScore { get; set; }
    public DuplicateCategory Category { get; set; }
    public CleanupAction Action { get; set; }
    public string Confidence { get; set; }  // HIGH, MEDIUM, LOW
    public string Reason { get; set; }
    public DateTime DecisionTime { get; set; }
}

public enum CleanupAction
{
    AutoDelete,        // Otomatik sil
    RequiresApproval,  // Onay gerekli
    Skip              // Atla
}
```

---

## BÖLÜM 8: Adım 7 - Admin Dashboard ve Preview

### 8.1 Cleanup Dashboard DTO

```csharp
// File: src/Katana.Core/DTOs/AdminDashboardDtos.cs

public class DataCleanupDashboardDto
{
    public CleanupStatistics Statistics { get; set; }
    public List<BadStockCardSummary> BadStockCards { get; set; }
    public List<AffectedOrderSummary> AffectedOrders { get; set; }
    public CleanupExecutionPlan ExecutionPlan { get; set; }
}

public class CleanupStatistics
{
    public int TotalStockCards { get; set; }
    public int BadStockCards { get; set; }
    public int EncodingIssues { get; set; }
    public int VersioningIssues { get; set; }
    public int ConcatenationErrors { get; set; }
    public int AffectedOrders { get; set; }
    public int AffectedOrderLines { get; set; }
    public decimal DataQualityScore { get; set; }  // 0-100
}

public class BadStockCardSummary
{
    public long SkartId { get; set; }
    public string StokKodu { get; set; }
    public string StokAdi { get; set; }
    public List<string> Issues { get; set; }
    public string Severity { get; set; }  // HIGH, MEDIUM, LOW
    public int ReferencedInOrders { get; set; }
}

public class AffectedOrderSummary
{
    public int OrderId { get; set; }
    public string OrderNo { get; set; }
    public DateTime OrderDate { get; set; }
    public string CustomerName { get; set; }
    public int AffectedLineCount { get; set; }
    public List<string> AffectedProductSkus { get; set; }
}

public class CleanupExecutionPlan
{
    public int Phase { get; set; }  // 1, 2, 3, 4
    public string PhaseName { get; set; }
    public string Description { get; set; }
    public int EstimatedDuration { get; set; }  // saniye
    public List<string> Steps { get; set; }
    public bool RequiresApproval { get; set; }
}
```

### 8.2 Preview Endpoint

```csharp
// File: src/Katana.API/Controllers/Admin/DataCleanupController.cs

[ApiController]
[Route("api/admin/cleanup")]
[Authorize(Roles = "Admin")]
public class DataCleanupController : ControllerBase
{
    private readonly IDataCleanupService _cleanupService;
    private readonly ISoftResetService _softResetService;

    /// <summary>
    /// Temizlik planını önizle (hiçbir şey silme)
    /// </summary>
    [HttpGet("preview")]
    public async Task<ActionResult<DataCleanupDashboardDto>> PreviewCleanup()
    {
        var badCards = await _cleanupService.IdentifyBadStockCardsAsync();
        var badSkus = badCards.Select(c => c.StokKodu).ToList();
        var affectedOrders = await _softResetService.FindOrdersWithBadProductsAsync(badSkus);

        var dashboard = new DataCleanupDashboardDto
        {
            Statistics = new CleanupStatistics
            {
                TotalStockCards = 5432,  // Luca'dan getir
                BadStockCards = badCards.Count,
                EncodingIssues = badCards.Count(c => c.Issues.Contains("CharacterEncoding")),
                VersioningIssues = badCards.Count(c => c.Issues.Contains("Versioning")),
                ConcatenationErrors = badCards.Count(c => c.Issues.Contains("Concatenation")),
                AffectedOrders = affectedOrders.Count,
                DataQualityScore = CalculateQualityScore(badCards.Count, 5432)
            },
            BadStockCards = badCards.Select(c => new BadStockCardSummary
            {
                SkartId = c.SkartId,
                StokKodu = c.StokKodu,
                StokAdi = c.StokAdi,
                Issues = c.Issues,
                Severity = c.Severity
            }).ToList(),
            ExecutionPlan = new CleanupExecutionPlan
            {
                Phase = 1,
                PhaseName = "Luca Temizliği",
                Description = "Luca'da hatalı stok kartlarını sil",
                EstimatedDuration = 300,
                Steps = new List<string>
                {
                    "Hatalı kartları tespit et",
                    "Luca API'sini çağır",
                    "Kartları sil",
                    "Audit log'a kaydet"
                },
                RequiresApproval = true
            }
        };

        return Ok(dashboard);
    }

    /// <summary>
    /// Temizliği başlat (Admin onayı gerekli)
    /// </summary>
    [HttpPost("execute")]
    public async Task<ActionResult<CleanupExecutionResult>> ExecuteCleanup(
        [FromBody] CleanupExecutionRequest request)
    {
        if (!request.ConfirmDelete)
            return BadRequest("Silme işlemini onaylamanız gerekir");

        var badCards = await _cleanupService.IdentifyBadStockCardsAsync();
        var result = await _cleanupService.DeleteBadStockCardsAsync(badCards);

        return Ok(result);
    }

    private decimal CalculateQualityScore(int badCards, int totalCards)
    {
        return (decimal)(totalCards - badCards) / totalCards * 100;
    }
}
```

---

## BÖLÜM 9: Adım 8 - Backup ve Geri Dönüş Stratejisi

### 9.1 Backup Script

```sql
-- Temizlik öncesi backup al
DECLARE @BackupPath NVARCHAR(500) = 'C:\Backups\Katana_PreCleanup_' +
    CONVERT(NVARCHAR, GETDATE(), 112) + '_' +
    CONVERT(NVARCHAR, GETDATE(), 108);

-- Kritik tabloları backup'la
BACKUP DATABASE [KatanaIntegration]
TO DISK = @BackupPath + '.bak'
WITH INIT, COMPRESSION;

-- Backup başarılı oldu
PRINT 'Backup created at: ' + @BackupPath + '.bak';
```

### 9.2 Geri Dönüş Prosedürü

```csharp
public class RollbackService : IRollbackService
{
    private readonly IntegrationDbContext _context;
    private readonly ILogger<RollbackService> _logger;

    /// <summary>
    /// Temizlik işlemini geri al
    /// </summary>
    public async Task<RollbackResult> RollbackCleanupAsync(
        DateTime cleanupStartTime,
        CancellationToken ct = default)
    {
        _logger.LogWarning("Rolling back cleanup operations from {Time}", cleanupStartTime);

        var result = new RollbackResult
        {
            StartedAt = DateTime.UtcNow
        };

        using var transaction = await _context.Database.BeginTransactionAsync(ct);

        try
        {
            // Adım 1: Audit log'dan geri dönüş işlemlerini bul
            var auditEntries = await _context.DataCleanupAudits
                .Where(a => a.PerformedAt >= cleanupStartTime)
                .OrderByDescending(a => a.PerformedAt)
                .ToListAsync(ct);

            // Adım 2: Her işlemi tersine çevir
            foreach (var entry in auditEntries)
            {
                switch (entry.OperationType)
                {
                    case "DELETE_LUCA":
                        // Luca'da silinen kartı yeniden oluştur
                        // (Luca API'sinde restore endpoint varsa)
                        result.RestoredLucaCards++;
                        break;

                    case "RESET_SYNC":
                        // Siparişlerin sync flaglarını eski haline getir
                        var orderLines = await _context.SalesOrderLines
                            .Where(l => l.Id == entry.EntityId)
                            .ToListAsync(ct);

                        foreach (var line in orderLines)
                        {
                            line.IsSyncedToLuca = true;
                            line.LastSyncAttempt = DateTime.UtcNow;
                        }
                        result.RestoredOrderLines++;
                        break;

                    case "MARK_INACTIVE":
                        // Ürünleri aktif yap
                        var product = await _context.Products
                            .FirstOrDefaultAsync(p => p.Id == entry.EntityId, ct);

                        if (product != null)
                        {
                            product.IsActive = true;
                            product.IsMarkedForCleanup = false;
                            product.CleanupReason = null;
                            result.RestoredProducts++;
                        }
                        break;
                }
            }

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            result.Success = true;
            _logger.LogInformation("Rollback completed successfully");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Rollback failed");
        }

        result.CompletedAt = DateTime.UtcNow;
        return result;
    }
}
```

---

## BÖLÜM 10: Adım 9 - Execution Plan (Adım Adım Yapılacaklar)

### 10.1 Faz 1: Hazırlık (1-2 saat)

```
1. Veritabanı Backup'ı Al
   └─ BACKUP DATABASE [KatanaIntegration] TO DISK = '...'

2. Migration'ları Çalıştır
   └─ Add IsSyncedToLuca, LukaErrorLog, IsMarkedForCleanup alanları

3. Servisleri Implement Et
   ├─ DataCleanupService.cs
   ├─ SoftResetService.cs
   ├─ LucaSyncTransformService.cs
   ├─ SmartDuplicateDetector.cs
   └─ RollbackService.cs

4. API Endpoint'lerini Ekle
   └─ DataCleanupController.cs
```

### 10.2 Faz 2: Analiz (30 dakika)

```
1. Dashboard'u Aç
   └─ GET /api/admin/cleanup/preview

2. İstatistikleri İncele
   ├─ Toplam hatalı kart: ?
   ├─ Encoding issues: ?
   ├─ Versioning issues: ?
   └─ Etkilenen siparişler: ?

3. Müşteriye Rapor Sunun
   └─ "Bulduğumuz hatalı kayıtlar: X adet"
```

### 10.3 Faz 3: Temizlik (1-2 saat)

```
1. Luca'da Hatalı Kartları Sil
   └─ POST /api/admin/cleanup/execute

2. Siparişleri Reset Et
   └─ Soft reset: IsSyncedToLuca = false

3. Ürünleri Inactive Yap
   └─ IsActive = false, IsMarkedForCleanup = true

4. Audit Log'u Kontrol Et
   └─ Tüm işlemler kaydedildi mi?
```

### 10.4 Faz 4: Doğrulama (30 dakika)

```
1. Luca'da Kartları Kontrol Et
   └─ Hatalı kartlar silindi mi?

2. Katana'da Siparişleri Kontrol Et
   └─ IsSyncedToLuca = false mi?

3. Yeni Mantığı Test Et
   └─ Temiz verilerle yeni gruplandırma çalışıyor mu?

4. Geri Dönüş Planını Hazırla
   └─ Sorun olursa rollback yapabiliriz
```

---

## BÖLÜM 11: Kod Örneği - Tüm Bir Akış

### 11.1 Complete Cleanup Flow

```csharp
// File: src/Katana.API/Controllers/Admin/DataCleanupController.cs

[HttpPost("execute-full-cleanup")]
public async Task<ActionResult<FullCleanupResult>> ExecuteFullCleanup(
    [FromBody] FullCleanupRequest request,
    CancellationToken ct)
{
    if (!request.AdminConfirmation)
        return BadRequest("Admin onayı gerekli");

    var result = new FullCleanupResult
    {
        StartedAt = DateTime.UtcNow,
        Phases = new List<PhaseResult>()
    };

    try
    {
        // FAZE 1: Luca'da Hatalı Kartları Tespit Et
        _logger.LogInformation("Phase 1: Identifying bad stock cards...");
        var badCards = await _cleanupService.IdentifyBadStockCardsAsync(ct);
        result.Phases.Add(new PhaseResult
        {
            Phase = 1,
            Name = "Identification",
            Status = "SUCCESS",
            Details = $"Found {badCards.Count} bad cards"
        });

        // FAZE 2: Etkilenen Siparişleri Bul
        _logger.LogInformation("Phase 2: Finding affected orders...");
        var badSkus = badCards.Select(c => c.StokKodu).ToList();
        var affectedOrderIds = await _softResetService
            .FindOrdersWithBadProductsAsync(badSkus, ct);
        result.Phases.Add(new PhaseResult
        {
            Phase = 2,
            Name = "Finding Affected Orders",
            Status = "SUCCESS",
            Details = $"Found {affectedOrderIds.Count} affected orders"
        });

        // FAZE 3: Luca'da Kartları Sil
        _logger.LogInformation("Phase 3: Deleting bad cards from Luca...");
        var deleteResult = await _cleanupService
            .DeleteBadStockCardsAsync(badCards, ct);
        result.Phases.Add(new PhaseResult
        {
            Phase = 3,
            Name = "Luca Cleanup",
            Status = deleteResult.Success ? "SUCCESS" : "FAILED",
            Details = $"Deleted {deleteResult.SuccessfulDeletions}/{deleteResult.TotalToDelete}"
        });

        if (!deleteResult.Success)
            throw new InvalidOperationException("Luca cleanup failed");

        // FAZE 4: Siparişleri Reset Et
        _logger.LogInformation("Phase 4: Resetting order sync status...");
        var resetResult = await _softResetService
            .ResetSalesOrderSyncAsync(affectedOrderIds, ct);
        result.Phases.Add(new PhaseResult
        {
            Phase = 4,
            Name = "Order Reset",
            Status = resetResult.Success ? "SUCCESS" : "FAILED",
            Details = $"Reset {resetResult.ResetOrders} orders"
        });

        // FAZE 5: Ürünleri Inactive Yap
        _logger.LogInformation("Phase 5: Marking products as inactive...");
        var productIds = badCards
            .Select(c => (long)c.SkartId)
            .ToList();
        var cleanupResult = await _cleanupService
            .MarkProductsForCleanupAsync(
                productIds,
                "Hatalı stok kartı - Luca'dan silindi",
                ct);
        result.Phases.Add(new PhaseResult
        {
            Phase = 5,
            Name = "Product Cleanup",
            Status = cleanupResult.Success ? "SUCCESS" : "FAILED",
            Details = $"Marked {cleanupResult.MarkedProducts} products"
        });

        result.Success = true;
        result.CompletedAt = DateTime.UtcNow;

        _logger.LogInformation("Full cleanup completed successfully");
    }
    catch (Exception ex)
    {
        result.Success = false;
        result.ErrorMessage = ex.Message;
        _logger.LogError(ex, "Full cleanup failed");

        // Geri dönüş yap
        await _rollbackService.RollbackCleanupAsync(result.StartedAt, ct);
    }

    return Ok(result);
}
```

---

## BÖLÜM 12: Öğrenci Olarak Yapılacaklar

### 12.1 Hemen Yapılacaklar (Bu Hafta)

```
✓ 1. Veritabanı migration'larını oluştur
     └─ IsSyncedToLuca, LukaErrorLog alanları

✓ 2. DataCleanupService.cs'i implement et
     └─ IdentifyBadStockCardsAsync()
     └─ DeleteBadStockCardsAsync()

✓ 3. SoftResetService.cs'i implement et
     └─ ResetSalesOrderSyncAsync()
     └─ FindOrdersWithBadProductsAsync()

✓ 4. DataCleanupController.cs'i implement et
     └─ GET /api/admin/cleanup/preview
     └─ POST /api/admin/cleanup/execute

✓ 5. Test et (Development ortamında)
     └─ Preview dashboard'u aç
     └─ Hatalı kartları tespit et
```

### 12.2 Sonraki Adımlar (Sonraki Hafta)

```
✓ 1. LucaSyncTransformService.cs'i implement et
     └─ Header-Line formatına dönüştür
     └─ BOM bileşenlerini ekle

✓ 2. SmartDuplicateDetector.cs'i implement et
     └─ Levenshtein Distance algoritması
     └─ Otomatik karar verme

✓ 3. RollbackService.cs'i implement et
     └─ Geri dönüş mekanizması

✓ 4. Müşteriye sunumu hazırla
     └─ Dashboard görselleri
     └─ İstatistikler
```

### 12.3 Kritik Noktalar

```
⚠️  BACKUP ALMADAN HIÇBIR ŞEY SILME!
    └─ BACKUP DATABASE [KatanaIntegration] TO DISK = '...'

⚠️  SOFT RESET İLE BAŞLA (Hard delete değil)
    └─ IsActive = false (silme değil)
    └─ IsSyncedToLuca = false (reset)

⚠️  AUDIT LOG'U TUTMAK ZORUNLU
    └─ Her işlem kaydedilmeli
    └─ Geri dönüş için gerekli

⚠️  ADMIN ONAYINI ALMAK ZORUNLU
    └─ Preview göster
    └─ Onay al
    └─ Sonra sil
```

---

## BÖLÜM 13: Müşteriye Sunuş Stratejisi

### 13.1 Sunum Sırası

```
1. Sorunun Tanısı (5 dakika)
   "Luca'da 287 hatalı stok kartı buldum:
    - 156 encoding hatası (?)
    - 89 versioning hatası (-V2, -V3)
    - 42 concatenation hatası (ABCABC)"

2. Çözüm Planı (5 dakika)
   "Bu kartları Luca'dan sileceğim ve
    etkilenen siparişleri yeniden göndereceğim"

3. Dashboard Gösterimi (10 dakika)
   "İşte temizlik öncesi ve sonrası karşılaştırma"

4. Onay Alma (2 dakika)
   "Devam etmemi onaylıyor musunuz?"

5. Temizlik Yapma (1-2 saat)
   "Şu anda temizlik yapılıyor..."

6. Doğrulama (10 dakika)
   "Temizlik tamamlandı, yeni mantık test ediliyor"
```

### 13.2 Müşteri Mesajı Şablonu

```
Sayın [Müşteri Adı],

Katana-Luca entegrasyonunda veri kalitesi sorunlarını tespit ettim:

📊 SORUN ANALİZİ:
- Toplam Stok Kartı: 5,432
- Hatalı Kartlar: 287 (%5.3)
  ├─ Encoding Hatası: 156 (?, ü→?, ş→?)
  ├─ Versioning: 89 (-V2, -V3, -V4)
  └─ Concatenation: 42 (ABCABC, DEFDEF)

📋 ETKİLENEN SİPARİŞLER:
- Toplam: 45 sipariş
- Satır Sayısı: 234 satır
- Durum: Onaylanmış ama Luca'ya gönderilmemiş

✅ ÇÖZÜM PLANI:
1. Luca'da hatalı kartları sil (287 kart)
2. Siparişleri "gönderilmemiş" olarak işaretle
3. Yeni mantık ile yeniden gönder
4. Doğrulama ve test

⏱️ TAHMINI SÜRE: 2-3 saat

🔒 GÜVENLİK:
- Veritabanı backup'ı alındı
- Geri dönüş mekanizması hazır
- Audit log tutulacak

Lütfen onayınızı veriniz.

Saygılarımla,
[Adınız]
```

---

## BÖLÜM 14: Özet ve Kontrol Listesi

### 14.1 Yapılacaklar Kontrol Listesi

```
HAZIRLIK:
☐ Veritabanı backup'ı al
☐ Migration'ları oluştur
☐ Servisleri implement et
☐ API endpoint'lerini ekle

ANALIZ:
☐ Dashboard'u aç
☐ Hatalı kartları tespit et
☐ Etkilenen siparişleri bul
☐ İstatistikleri hesapla

TEMIZLIK:
☐ Müşteri onayını al
☐ Luca'da kartları sil
☐ Siparişleri reset et
☐ Ürünleri inactive yap

DOĞRULAMA:
☐ Luca'da kartları kontrol et
☐ Katana'da siparişleri kontrol et
☐ Yeni mantığı test et
☐ Audit log'u kontrol et

SONUÇ:
☐ Müşteriye rapor sun
☐ Yeni mantığı canlıya al
☐ Monitoring başlat
```

### 14.2 Başarı Kriterleri

```
✓ Hatalı kartlar Luca'dan silindi
✓ Siparişler "gönderilmemiş" olarak işaretlendi
✓ Yeni mantık temiz verilerle çalışıyor
✓ Veri kalitesi skoru 95%+ oldu
✓ Müşteri memnun
```

---

## Sonuç

Bu strateji sayesinde:

1. **Veri Temizliği**: Hatalı kayıtlar güvenli bir şekilde temizlenir
2. **Soft Reset**: Geri dönüş mekanizması ile riskler minimize edilir
3. **Header-Line Mimarisi**: Luca'ya doğru format ile gönderim yapılır
4. **Benzerlik Algoritması**: Otomatik karar verme ile zaman kazanılır
5. **Müşteri Güveni**: Dashboard ve preview ile şeffaflık sağlanır

**Başarı Anahtarı**: Adım adım ilerlemek, her aşamada doğrulama yapmak ve geri dönüş planını hazır tutmak.
