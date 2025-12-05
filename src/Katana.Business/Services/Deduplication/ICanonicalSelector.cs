using Katana.Core.DTOs;

namespace Katana.Business.Services.Deduplication;

/// <summary>
/// Service for selecting the canonical (preferred) stock card from a duplicate group
/// </summary>
public interface ICanonicalSelector
{
    /// <summary>
    /// Selects the canonical stock card from a duplicate group based on configured rules
    /// Property 12: Canonical selection by rules
    /// Property 13: Default canonical selection
    /// </summary>
    /// <param name="group">Duplicate group to select from</param>
    /// <param name="rules">Deduplication rules configuration</param>
    /// <returns>The selected canonical stock card</returns>
    StockCardInfo SelectCanonical(DuplicateGroup group, DeduplicationRules rules);
}
