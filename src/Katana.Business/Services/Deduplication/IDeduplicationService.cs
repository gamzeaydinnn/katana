using Katana.Core.DTOs;

namespace Katana.Business.Services.Deduplication;

/// <summary>
/// Main orchestration service for stock card deduplication
/// </summary>
public interface IDeduplicationService
{
    /// <summary>
    /// Analyzes all stock cards in Luca and detects duplicates
    /// Property 3: Analysis report structure
    /// </summary>
    Task<DuplicateAnalysisResult> AnalyzeDuplicatesAsync(CancellationToken ct = default);

    /// <summary>
    /// Generates a preview of deduplication actions based on analysis
    /// Property 11: Preview completeness
    /// </summary>
    Task<DeduplicationPreview> GeneratePreviewAsync(DuplicateAnalysisResult analysis, CancellationToken ct = default);

    /// <summary>
    /// Executes the deduplication plan by deleting duplicate stock cards
    /// Property 14: Execution follows preview
    /// Property 15: Canonical existence check
    /// Property 16: Execution summary completeness
    /// </summary>
    Task<DeduplicationExecutionResult> ExecuteDeduplicationAsync(DeduplicationPreview preview, CancellationToken ct = default);

    /// <summary>
    /// Exports analysis results to specified format
    /// </summary>
    Task<string> ExportResultsAsync(DuplicateAnalysisResult analysis, ExportFormat format, CancellationToken ct = default);

    /// <summary>
    /// Gets current deduplication rules configuration
    /// </summary>
    Task<DeduplicationRules> GetRulesAsync();

    /// <summary>
    /// Updates deduplication rules configuration
    /// </summary>
    Task UpdateRulesAsync(DeduplicationRules rules);

    // ============ Variant-Aware Deduplication Methods ============

    /// <summary>
    /// Varyant bazlı duplicate tespiti yapar
    /// Property 4: Duplicate Detection Consistency
    /// </summary>
    Task<List<VariantDuplicateGroup>> DetectVariantDuplicatesAsync(double similarityThreshold = 0.85, CancellationToken ct = default);

    /// <summary>
    /// Varyantları canonical ürüne birleştirir
    /// Property 5: Merge Data Integrity
    /// </summary>
    Task<VariantMergeResult> MergeVariantsAsync(long canonicalProductId, List<long> duplicateProductIds, CancellationToken ct = default);

    /// <summary>
    /// Aktif siparişi olan ürünleri kontrol eder
    /// Property 6: Active Order Protection
    /// </summary>
    Task<List<ActiveOrderReference>> GetActiveOrdersForProductAsync(long productId, CancellationToken ct = default);

    /// <summary>
    /// SKU base koduna gore birlestirme plani olusturur
    /// </summary>
    Task<SkuBaseMergePlan> BuildSkuBaseMergePlanAsync(CancellationToken ct = default);

    /// <summary>
    /// SKU base koduna gore urunleri birlestirir
    /// </summary>
    Task<List<VariantMergeResult>> MergeProductsBySkuBaseAsync(CancellationToken ct = default);
}
