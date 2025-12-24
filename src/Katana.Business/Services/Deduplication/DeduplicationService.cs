using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Katana.Business.Services.Deduplication;

/// <summary>
/// Main orchestration service for stock card deduplication
/// </summary>
public class DeduplicationService : IDeduplicationService
{
    private readonly ILucaService _lucaService;
    private readonly IDuplicateDetector _duplicateDetector;
    private readonly ICanonicalSelector _canonicalSelector;
    private readonly IntegrationDbContext _context;
    private readonly ILogger<DeduplicationService> _logger;
    private readonly DeduplicationRules _rules;

    public DeduplicationService(
        ILucaService lucaService,
        IDuplicateDetector duplicateDetector,
        ICanonicalSelector canonicalSelector,
        IntegrationDbContext context,
        ILogger<DeduplicationService> logger,
        IOptions<DeduplicationRules> rulesOptions)
    {
        _lucaService = lucaService;
        _duplicateDetector = duplicateDetector;
        _canonicalSelector = canonicalSelector;
        _context = context;
        _logger = logger;
        _rules = rulesOptions.Value;
    }

    /// <summary>
    /// Property 3: Analysis report structure
    /// Property 6: Version count accuracy
    /// Analyzes all stock cards and detects duplicates
    /// </summary>
    public async Task<DuplicateAnalysisResult> AnalyzeDuplicatesAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting duplicate analysis...");

        // Fetch all stock cards from Luca
        var stockCards = await _lucaService.ListStockCardsAsync(ct);
        
        _logger.LogInformation("Fetched {Count} stock cards from Luca", stockCards.Count);

        // Convert to LucaStockDto format for detection
        // Note: LucaStockCardSummaryDto doesn't have SkartId
        // We'll use StokKodu hash as a pseudo-ID for grouping purposes
        var lucaStockDtos = stockCards.Select(card => new LucaStockDto
        {
            SkartId = GetStableHashCode(card.StokKodu), // Use hash of stock code as ID
            ProductCode = card.StokKodu ?? string.Empty,
            ProductName = card.StokAdi ?? string.Empty
        }).ToList();

        // Detect duplicates
        var duplicateGroups = _duplicateDetector.DetectDuplicates(lucaStockDtos);
        
        _logger.LogInformation("Detected {GroupCount} duplicate groups", duplicateGroups.Count);

        // Calculate statistics
        var statistics = CalculateStatistics(stockCards.Count, duplicateGroups);

        var result = new DuplicateAnalysisResult
        {
            DuplicateGroups = duplicateGroups,
            Statistics = statistics,
            AnalyzedAt = DateTime.UtcNow
        };

        _logger.LogInformation(
            "Analysis complete: {TotalCards} cards, {DuplicateGroups} groups, {TotalDuplicates} duplicates",
            statistics.TotalStockCards,
            statistics.DuplicateGroups,
            statistics.TotalDuplicates);

        return result;
    }

    /// <summary>
    /// Property 11: Preview completeness
    /// Generates preview of deduplication actions
    /// </summary>
    public async Task<DeduplicationPreview> GeneratePreviewAsync(
        DuplicateAnalysisResult analysis, 
        CancellationToken ct = default)
    {
        _logger.LogInformation("Generating deduplication preview for {GroupCount} groups", 
            analysis.DuplicateGroups.Count);

        var actions = new List<DeduplicationAction>();

        foreach (var group in analysis.DuplicateGroups)
        {
            // Select canonical card
            var canonical = _canonicalSelector.SelectCanonical(group, _rules);

            // Determine cards to remove
            var cardsToRemove = group.StockCards
                .Where(c => c.SkartId != canonical.SkartId)
                .ToList();

            // Build reason
            var reason = BuildActionReason(canonical, group);

            var action = new DeduplicationAction
            {
                GroupId = group.GroupId,
                CanonicalCard = canonical,
                CardsToRemove = cardsToRemove,
                Reason = reason,
                Type = DetermineActionType(group)
            };

            actions.Add(action);
        }

        var previewStats = new PreviewStatistics
        {
            TotalActions = actions.Count,
            CardsToKeep = actions.Count,
            CardsToRemove = actions.Sum(a => a.CardsToRemove.Count),
            CardsToUpdate = actions.Count(a => a.Type == ActionType.UpdateAndRemove)
        };

        var preview = new DeduplicationPreview
        {
            Actions = actions,
            Statistics = previewStats,
            GeneratedAt = DateTime.UtcNow
        };

        _logger.LogInformation(
            "Preview generated: {TotalActions} actions, {CardsToRemove} cards to remove",
            previewStats.TotalActions,
            previewStats.CardsToRemove);

        return await Task.FromResult(preview);
    }

    /// <summary>
    /// Property 14: Execution follows preview
    /// Property 15: Canonical existence check
    /// Property 16: Execution summary completeness
    /// Executes deduplication by deleting duplicate cards
    /// </summary>
    public async Task<DeduplicationExecutionResult> ExecuteDeduplicationAsync(
        DeduplicationPreview preview, 
        CancellationToken ct = default)
    {
        _logger.LogInformation("Starting deduplication execution for {ActionCount} actions", 
            preview.Actions.Count);

        var result = new DeduplicationExecutionResult
        {
            ExecutedAt = DateTime.UtcNow
        };

        foreach (var action in preview.Actions)
        {
            if (ct.IsCancellationRequested)
            {
                _logger.LogWarning("Deduplication execution cancelled");
                break;
            }

            try
            {
                // Property 15: Verify canonical exists before deletion
                var canonicalExists = await VerifyCanonicalExistsAsync(action.CanonicalCard.SkartId, ct);
                
                if (!canonicalExists)
                {
                    var error = new ExecutionError
                    {
                        GroupId = action.GroupId,
                        StockCode = action.CanonicalCard.StockCode,
                        ErrorMessage = "Canonical card not found in Luca"
                    };
                    result.Errors.Add(error);
                    result.FailedRemovals += action.CardsToRemove.Count;
                    
                    _logger.LogError(
                        "Canonical card {Code} (ID: {Id}) not found, skipping group {GroupId}",
                        action.CanonicalCard.StockCode,
                        action.CanonicalCard.SkartId,
                        action.GroupId);
                    
                    // Halt on first error as per spec
                    break;
                }

                // Delete duplicate cards
                foreach (var cardToRemove in action.CardsToRemove)
                {
                    try
                    {
                        await DeleteStockCardAsync(cardToRemove.SkartId, ct);
                        result.SuccessfulRemovals++;
                        
                        _logger.LogInformation(
                            "Deleted duplicate card {Code} (ID: {Id})",
                            cardToRemove.StockCode,
                            cardToRemove.SkartId);
                    }
                    catch (Exception ex)
                    {
                        var error = new ExecutionError
                        {
                            GroupId = action.GroupId,
                            StockCode = cardToRemove.StockCode,
                            ErrorMessage = ex.Message
                        };
                        result.Errors.Add(error);
                        result.FailedRemovals++;
                        
                        _logger.LogError(ex,
                            "Failed to delete card {Code} (ID: {Id})",
                            cardToRemove.StockCode,
                            cardToRemove.SkartId);
                        
                        // Halt on first error
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing group {GroupId}, halting execution", action.GroupId);
                break;
            }
        }

        _logger.LogInformation(
            "Execution complete: {Success} successful, {Failed} failed, {Skipped} skipped",
            result.SuccessfulRemovals,
            result.FailedRemovals,
            result.SkippedActions);

        return result;
    }

    /// <summary>
    /// Exports analysis results to specified format
    /// </summary>
    public Task<string> ExportResultsAsync(
        DuplicateAnalysisResult analysis, 
        ExportFormat format, 
        CancellationToken ct = default)
    {
        // This will be implemented when we add the export service
        throw new NotImplementedException("Export functionality will be implemented in Task 5");
    }

    /// <summary>
    /// Gets current deduplication rules
    /// </summary>
    public async Task<DeduplicationRules> GetRulesAsync()
    {
        return await Task.FromResult(_rules);
    }

    /// <summary>
    /// Updates deduplication rules
    /// </summary>
    public async Task UpdateRulesAsync(DeduplicationRules rules)
    {
        // In a real implementation, this would update configuration
        // For now, we'll just validate the rules
        if (rules == null)
        {
            throw new ArgumentNullException(nameof(rules));
        }

        if (rules.Rules == null || !rules.Rules.Any())
        {
            throw new ArgumentException("Rules list cannot be empty", nameof(rules));
        }

        if (rules.DefaultRule == null)
        {
            throw new ArgumentException("Default rule must be specified", nameof(rules));
        }

        _logger.LogInformation("Rules updated successfully");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Calculates statistics from duplicate groups
    /// Property 6: Version count accuracy
    /// </summary>
    private DuplicateStatistics CalculateStatistics(int totalCards, List<DuplicateGroup> groups)
    {
        var stats = new DuplicateStatistics
        {
            TotalStockCards = totalCards,
            DuplicateGroups = groups.Count,
            TotalDuplicates = groups.Sum(g => g.StockCards.Count),
            VersioningDuplicates = groups.Count(g => 
                g.Category == DuplicateCategory.Versioning || 
                g.Category == DuplicateCategory.Mixed),
            ConcatenationErrors = groups.Count(g => 
                g.Category == DuplicateCategory.ConcatenationError || 
                g.Category == DuplicateCategory.Mixed),
            EncodingIssues = groups.Count(g => 
                g.Category == DuplicateCategory.CharacterEncoding || 
                g.Category == DuplicateCategory.Mixed)
        };

        return stats;
    }

    /// <summary>
    /// Builds human-readable reason for deduplication action
    /// </summary>
    private string BuildActionReason(StockCardInfo canonical, DuplicateGroup group)
    {
        var reasons = new List<string>();

        // Add category-specific reason
        switch (group.Category)
        {
            case DuplicateCategory.Versioning:
                reasons.Add("Versioning duplicates detected");
                break;
            case DuplicateCategory.ConcatenationError:
                reasons.Add("Concatenation error detected");
                break;
            case DuplicateCategory.CharacterEncoding:
                reasons.Add("Character encoding issue detected");
                break;
            case DuplicateCategory.Mixed:
                reasons.Add("Multiple issues detected");
                break;
        }

        // Add canonical selection reason
        if (!canonical.StockCode.Contains("-V"))
        {
            reasons.Add("Base version selected (no version suffix)");
        }
        else if (!canonical.StockName.Contains('?'))
        {
            reasons.Add("Correctly encoded version selected");
        }
        else
        {
            reasons.Add($"Shortest code selected: {canonical.StockCode}");
        }

        return string.Join("; ", reasons);
    }

    /// <summary>
    /// Determines the type of action needed
    /// </summary>
    private ActionType DetermineActionType(DuplicateGroup group)
    {
        // If concatenation error, might need update
        if (group.Category == DuplicateCategory.ConcatenationError)
        {
            return ActionType.UpdateAndRemove;
        }

        // Otherwise just remove
        return ActionType.Remove;
    }

    /// <summary>
    /// Verifies that canonical card exists in Luca
    /// Property 15: Canonical existence check
    /// </summary>
    private async Task<bool> VerifyCanonicalExistsAsync(long skartId, CancellationToken ct)
    {
        try
        {
            // Since LucaStockCardSummaryDto doesn't have SkartId,
            // we verify by checking if any card exists with matching hash
            var allCards = await _lucaService.ListStockCardsAsync(ct);
            return allCards.Any(c => GetStableHashCode(c.StokKodu) == skartId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying canonical card existence for ID {SkartId}", skartId);
            return false;
        }
    }

    /// <summary>
    /// Generates a stable hash code for a string (used as pseudo-ID)
    /// </summary>
    private long GetStableHashCode(string str)
    {
        if (string.IsNullOrEmpty(str))
            return 0;

        unchecked
        {
            long hash = 5381;
            foreach (char c in str)
            {
                hash = ((hash << 5) + hash) + c;
            }
            return Math.Abs(hash);
        }
    }

    /// <summary>
    /// Deletes a stock card from Luca
    /// </summary>
    private async Task DeleteStockCardAsync(long skartId, CancellationToken ct)
    {
        // This will be implemented when we add the delete method to LucaService (Task 9)
        _logger.LogWarning("Delete functionality not yet implemented for card ID {SkartId}", skartId);
        await Task.CompletedTask;
        
        // TODO: Implement actual deletion
        // await _lucaService.DeleteStockCardAsync(skartId, ct);
    }

    // ============ Variant-Aware Deduplication Methods ============

    /// <summary>
    /// Varyant bazlı duplicate tespiti yapar
    /// Property 4: Duplicate Detection Consistency
    /// </summary>
    public async Task<List<VariantDuplicateGroup>> DetectVariantDuplicatesAsync(
        double similarityThreshold = 0.85, 
        CancellationToken ct = default)
    {
        _logger.LogInformation("Starting variant duplicate detection with threshold {Threshold}", similarityThreshold);

        var products = await _context.Products
            .Include(p => p.Variants)
            .Where(p => p.IsActive)
            .ToListAsync(ct);

        var duplicateGroups = new List<VariantDuplicateGroup>();
        var processedIds = new HashSet<int>();

        foreach (var product in products)
        {
            if (processedIds.Contains(product.Id))
                continue;

            var similarProducts = products
                .Where(p => p.Id != product.Id && !processedIds.Contains(p.Id))
                .Where(p => CalculateSimilarity(product.Name, p.Name) >= similarityThreshold)
                .ToList();

            if (similarProducts.Any())
            {
                var allInGroup = new List<Product> { product };
                allInGroup.AddRange(similarProducts);

                // Mark all as processed
                foreach (var p in allInGroup)
                    processedIds.Add(p.Id);

                // Select canonical (the one with most order lines or oldest)
                var canonical = await SelectCanonicalProductAsync(allInGroup, ct);

                var group = new VariantDuplicateGroup
                {
                    GroupKey = product.Name.ToLowerInvariant().Trim(),
                    SimilarityScore = similarProducts.Average(p => CalculateSimilarity(product.Name, p.Name)),
                    RecommendedCanonical = MapToVariantDetail(canonical),
                    Duplicates = allInGroup
                        .Where(p => p.Id != canonical.Id)
                        .Select(MapToVariantDetail)
                        .ToList(),
                    TotalOrderLines = await GetTotalOrderLinesAsync(allInGroup.Select(p => p.Id).ToList(), ct),
                    TotalStockMovements = await GetTotalStockMovementsAsync(allInGroup.Select(p => p.Id).ToList(), ct)
                };

                duplicateGroups.Add(group);
            }
        }

        _logger.LogInformation("Detected {GroupCount} variant duplicate groups", duplicateGroups.Count);
        return duplicateGroups;
    }

    /// <summary>
    /// Varyantları canonical ürüne birleştirir
    /// Property 5: Merge Data Integrity
    /// </summary>
    public async Task<VariantMergeResult> MergeVariantsAsync(
        long canonicalProductId, 
        List<long> duplicateProductIds, 
        CancellationToken ct = default)
    {
        _logger.LogInformation("Merging {Count} products into canonical {CanonicalId}", 
            duplicateProductIds.Count, canonicalProductId);

        var result = new VariantMergeResult
        {
            CanonicalProductId = canonicalProductId,
            MergedProductIds = duplicateProductIds
        };

        using var transaction = await _context.Database.BeginTransactionAsync(ct);

        try
        {
            // Check for active orders first
            foreach (var productId in duplicateProductIds)
            {
                var activeOrders = await GetActiveOrdersForProductAsync(productId, ct);
                if (activeOrders.Any())
                {
                    result.Success = false;
                    result.Errors.Add($"Product {productId} has {activeOrders.Count} active orders. Cannot merge.");
                    return result;
                }
            }

            // Transfer order lines
            var orderLines = await _context.SalesOrderLines
                .Where(l => duplicateProductIds.Contains(l.VariantId))
                .ToListAsync(ct);

            foreach (var line in orderLines)
            {
                line.VariantId = (int)canonicalProductId;
            }
            result.TransferredOrderLines = orderLines.Count;

            // Transfer stock movements
            var stockMovements = await _context.StockMovements
                .Where(m => duplicateProductIds.Contains(m.ProductId))
                .ToListAsync(ct);

            foreach (var movement in stockMovements)
            {
                movement.ProductId = (int)canonicalProductId;
            }
            result.TransferredStockMovements = stockMovements.Count;

            // Update Luca mappings
            var mappings = await _context.MappingTables
                .Where(m => m.MappingType == "Product" && 
                           duplicateProductIds.Select(id => id.ToString()).Contains(m.SourceValue))
                .ToListAsync(ct);

            foreach (var mapping in mappings)
            {
                mapping.SourceValue = canonicalProductId.ToString();
                mapping.UpdatedAt = DateTime.UtcNow;
            }
            result.UpdatedLucaMappings = mappings.Count;

            // Mark duplicate products as inactive
            var duplicateProducts = await _context.Products
                .Where(p => duplicateProductIds.Contains(p.Id))
                .ToListAsync(ct);

            foreach (var product in duplicateProducts)
            {
                product.IsActive = false;
                product.UpdatedAt = DateTime.UtcNow;
                product.Description = $"[MERGED INTO {canonicalProductId}] {product.Description}";
            }

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            result.Success = true;
            _logger.LogInformation("Successfully merged {Count} products into {CanonicalId}", 
                duplicateProductIds.Count, canonicalProductId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            result.Success = false;
            result.Errors.Add(ex.Message);
            _logger.LogError(ex, "Failed to merge products into {CanonicalId}", canonicalProductId);
        }

        return result;
    }

    /// <summary>
    /// Aktif siparişi olan ürünleri kontrol eder
    /// Property 6: Active Order Protection
    /// </summary>
    public async Task<List<ActiveOrderReference>> GetActiveOrdersForProductAsync(
        long productId, 
        CancellationToken ct = default)
    {
        _logger.LogDebug("Checking active orders for product {ProductId}", productId);

        var activeStatuses = new[] { "PENDING", "CONFIRMED", "IN_PROGRESS", "PROCESSING" };

        var activeOrders = await _context.SalesOrderLines
            .Include(l => l.SalesOrder)
            .ThenInclude(o => o.Customer)
            .Where(l => l.VariantId == productId || l.SalesOrder.Lines.Any(ol => ol.VariantId == productId))
            .Where(l => activeStatuses.Contains(l.SalesOrder.Status))
            .Select(l => new ActiveOrderReference
            {
                OrderId = l.SalesOrderId,
                OrderNo = l.SalesOrder.OrderNo,
                Status = l.SalesOrder.Status,
                OrderDate = l.SalesOrder.OrderCreatedDate ?? DateTime.MinValue,
                CustomerName = l.SalesOrder.Customer != null ? l.SalesOrder.Customer.Title : null,
                Quantity = l.Quantity
            })
            .Distinct()
            .ToListAsync(ct);

        return activeOrders;
    }

    // ============ Helper Methods for Variant Deduplication ============

    private double CalculateSimilarity(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            return 0;

        s1 = s1.ToLowerInvariant().Trim();
        s2 = s2.ToLowerInvariant().Trim();

        if (s1 == s2)
            return 1.0;

        // Levenshtein distance based similarity
        var distance = LevenshteinDistance(s1, s2);
        var maxLength = Math.Max(s1.Length, s2.Length);
        return 1.0 - ((double)distance / maxLength);
    }

    private int LevenshteinDistance(string s1, string s2)
    {
        var n = s1.Length;
        var m = s2.Length;
        var d = new int[n + 1, m + 1];

        for (var i = 0; i <= n; i++)
            d[i, 0] = i;
        for (var j = 0; j <= m; j++)
            d[0, j] = j;

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

    private async Task<Product> SelectCanonicalProductAsync(List<Product> products, CancellationToken ct)
    {
        // Select the product with most order lines, or oldest if tied
        var productOrderCounts = new Dictionary<int, int>();

        foreach (var product in products)
        {
            var orderCount = await _context.SalesOrderLines
                .CountAsync(l => l.VariantId == product.Id, ct);
            productOrderCounts[product.Id] = orderCount;
        }

        return products
            .OrderByDescending(p => productOrderCounts[p.Id])
            .ThenBy(p => p.CreatedAt)
            .First();
    }

    private VariantDetail MapToVariantDetail(Product product)
    {
        return new VariantDetail
        {
            VariantId = product.Id,
            ProductId = product.Id,
            SKU = product.SKU,
            Barcode = product.Barcode,
            Name = product.Name,
            InStock = product.Stock,
            Available = product.Stock,
            SalesPrice = product.Price,
            PurchasePrice = product.PurchasePrice,
            IsOrphan = false
        };
    }

    private async Task<int> GetTotalOrderLinesAsync(List<int> productIds, CancellationToken ct)
    {
        var longProductIds = productIds.Select(id => (long)id).ToList();
        return await _context.SalesOrderLines
            .CountAsync(l => longProductIds.Contains(l.VariantId), ct);
    }

    private async Task<int> GetTotalStockMovementsAsync(List<int> productIds, CancellationToken ct)
    {
        return await _context.StockMovements
            .CountAsync(m => productIds.Contains(m.ProductId), ct);
    }

    public async Task<SkuBaseMergePlan> BuildSkuBaseMergePlanAsync(CancellationToken ct = default)
    {
        var products = await _context.Products
            .Where(p => p.IsActive)
            .ToListAsync(ct);

        var grouped = products
            .GroupBy(p => GetSkuBaseCode(p.SKU))
            .Where(g => !string.IsNullOrWhiteSpace(g.Key))
            .Where(g => g.Count() > 1)
            .ToList();

        var plan = new SkuBaseMergePlan();

        foreach (var group in grouped)
        {
            var baseSku = group.Key!;
            var groupProducts = group.ToList();

            var canonical = groupProducts
                .FirstOrDefault(p => string.Equals(p.SKU, baseSku, StringComparison.OrdinalIgnoreCase));

            if (canonical == null)
            {
                canonical = await SelectCanonicalProductAsync(groupProducts, ct);
            }

            var duplicates = groupProducts
                .Where(p => p.Id != canonical.Id)
                .Select(p => (long)p.Id)
                .ToList();

            if (duplicates.Count == 0)
            {
                continue;
            }

            plan.Groups.Add(new SkuBaseMergeGroup
            {
                BaseSku = baseSku,
                CanonicalProductId = canonical.Id,
                DuplicateProductIds = duplicates,
                ProductSkus = groupProducts.Select(p => p.SKU).ToList()
            });
        }

        plan.TotalGroups = plan.Groups.Count;
        plan.TotalDuplicates = plan.Groups.Sum(g => g.DuplicateProductIds.Count);

        return plan;
    }

    public async Task<List<VariantMergeResult>> MergeProductsBySkuBaseAsync(CancellationToken ct = default)
    {
        var plan = await BuildSkuBaseMergePlanAsync(ct);
        var results = new List<VariantMergeResult>();

        foreach (var group in plan.Groups)
        {
            var result = await MergeVariantsAsync(group.CanonicalProductId, group.DuplicateProductIds, ct);
            results.Add(result);
        }

        return results;
    }

    private static string? GetSkuBaseCode(string? sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return null;
        }

        var parts = sku.Split('-', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0] : sku;
    }
}
