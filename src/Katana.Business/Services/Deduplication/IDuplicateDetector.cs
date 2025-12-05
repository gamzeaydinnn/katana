using Katana.Core.DTOs;

namespace Katana.Business.Services.Deduplication;

/// <summary>
/// Service for detecting and categorizing duplicate stock cards
/// </summary>
public interface IDuplicateDetector
{
    /// <summary>
    /// Detects duplicate stock cards by grouping cards with identical names
    /// </summary>
    /// <param name="stockCards">List of stock cards to analyze</param>
    /// <returns>List of duplicate groups</returns>
    List<DuplicateGroup> DetectDuplicates(List<LucaStockDto> stockCards);

    /// <summary>
    /// Categorizes a duplicate group based on the type of duplication issue
    /// </summary>
    /// <param name="group">Duplicate group to categorize</param>
    /// <returns>Category of the duplicate (Versioning, ConcatenationError, CharacterEncoding, or Mixed)</returns>
    DuplicateCategory CategorizeDuplicate(DuplicateGroup group);
}
