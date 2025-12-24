using Katana.Core.DTOs;

namespace Katana.Business.Interfaces;

/// <summary>
/// Service for validating product merge operations
/// </summary>
public interface IMergeValidationService
{
    /// <summary>
    /// Validates a merge request before execution
    /// </summary>
    Task<ValidationResult> ValidateMergeRequestAsync(MergeRequest request);

    /// <summary>
    /// Checks if canonical product exists and is active
    /// </summary>
    Task<bool> CanonicalProductExistsAsync(int productId);

    /// <summary>
    /// Checks for circular BOM references
    /// </summary>
    Task<bool> HasCircularBOMReferencesAsync(int canonicalProductId, List<int> productIdsToMerge);

    /// <summary>
    /// Checks if product is in a pending merge
    /// </summary>
    Task<bool> IsProductInPendingMergeAsync(int productId);
}

/// <summary>
/// Result of validation
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}
