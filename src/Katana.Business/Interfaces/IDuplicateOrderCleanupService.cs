using Katana.Core.DTOs;

namespace Katana.Business.Interfaces;

/// <summary>
/// Duplike sipariş temizleme servisi
/// </summary>
public interface IDuplicateOrderCleanupService
{
    /// <summary>
    /// Duplike siparişleri analiz et
    /// </summary>
    Task<DuplicateOrderAnalysisResult> AnalyzeDuplicatesAsync(CancellationToken ct = default);

    /// <summary>
    /// Duplike siparişleri temizle
    /// </summary>
    Task<OrderCleanupResult> CleanupDuplicatesAsync(bool dryRun = true, CancellationToken ct = default);

    /// <summary>
    /// Bozuk OrderNo'ları analiz et
    /// </summary>
    Task<MalformedOrderAnalysisResult> AnalyzeMalformedAsync(CancellationToken ct = default);

    /// <summary>
    /// Bozuk OrderNo'ları temizle
    /// </summary>
    Task<OrderCleanupResult> CleanupMalformedAsync(bool dryRun = true, CancellationToken ct = default);

    /// <summary>
    /// Bozuk OrderNo'dan doğru formatı çıkar
    /// </summary>
    string ExtractBaseOrderNo(string orderNo);

    /// <summary>
    /// OrderNo'nun bozuk format olup olmadığını kontrol et
    /// </summary>
    bool IsMalformedOrderNo(string orderNo);
}
