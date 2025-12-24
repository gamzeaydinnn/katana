using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Katana.Business.Services;

/// <summary>
/// Service for validating product merge operations
/// </summary>
public class MergeValidationService : IMergeValidationService
{
    private readonly IntegrationDbContext _context;
    private readonly ILogger<MergeValidationService> _logger;

    public MergeValidationService(
        IntegrationDbContext context,
        ILogger<MergeValidationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Validates a merge request before execution
    /// Requirements: 10.1, 10.2, 10.3, 10.4, 10.5
    /// </summary>
    public async Task<ValidationResult> ValidateMergeRequestAsync(MergeRequest request)
    {
        var result = new ValidationResult { IsValid = true };

        // Validate canonical product exists and is active (Req 10.1)
        if (!await CanonicalProductExistsAsync(request.CanonicalProductId))
        {
            result.IsValid = false;
            result.Errors.Add($"Canonical product with ID {request.CanonicalProductId} does not exist or is inactive");
        }

        // Validate all products to merge exist (Req 10.2)
        foreach (var productId in request.ProductIdsToMerge)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                result.IsValid = false;
                result.Errors.Add($"Product with ID {productId} does not exist");
            }
        }

        // Check for circular BOM references (Req 10.3)
        if (await HasCircularBOMReferencesAsync(request.CanonicalProductId, request.ProductIdsToMerge))
        {
            result.IsValid = false;
            result.Errors.Add("Merge would create circular BOM references");
        }

        // Check for pending merges (Req 10.4)
        if (await IsProductInPendingMergeAsync(request.CanonicalProductId))
        {
            result.IsValid = false;
            result.Errors.Add($"Canonical product {request.CanonicalProductId} is already in a pending merge");
        }

        foreach (var productId in request.ProductIdsToMerge)
        {
            if (await IsProductInPendingMergeAsync(productId))
            {
                result.IsValid = false;
                result.Errors.Add($"Product {productId} is already in a pending merge");
            }
        }

        return result;
    }

    /// <summary>
    /// Checks if canonical product exists and is active
    /// Requirements: 10.1
    /// </summary>
    public async Task<bool> CanonicalProductExistsAsync(int productId)
    {
        var product = await _context.Products.FindAsync(productId);
        return product != null && product.IsActive;
    }

    /// <summary>
    /// Checks for circular BOM references
    /// Requirements: 10.3
    /// </summary>
    public async Task<bool> HasCircularBOMReferencesAsync(int canonicalProductId, List<int> productIdsToMerge)
    {
        // TODO: IntegrationDbContext.BOMs doesn't exist yet
        return false;
        
        // Get all BOMs where canonical product is a component
        // var canonicalAsBOMs = await _context.BOMs
        //     .Where(b => b.ComponentProductId == canonicalProductId)
        //     .Select(b => b.ProductId)
        //     .ToListAsync();

        // // Check if any of the products to merge would create a circular reference
        // foreach (var productId in productIdsToMerge)
        // {
        //     if (canonicalAsBOMs.Contains(productId))
        //     {
        //         return true; // Circular reference detected
        //     }

        //     // Check transitive dependencies
        //     var transitiveCheck = await CheckTransitiveBOMDependency(productId, canonicalProductId, new HashSet<int>());
        //     if (transitiveCheck)
        //     {
        //         return true;
        //     }
        // }

        // return false;
    }

    /// <summary>
    /// Recursively checks for transitive BOM dependencies
    /// </summary>
    private async Task<bool> CheckTransitiveBOMDependency(int productId, int targetId, HashSet<int> visited)
    {
        // TODO: IntegrationDbContext.BOMs doesn't exist yet
        return false;
        
        // if (visited.Contains(productId))
        // {
        //     return false; // Already visited
        // }

        // visited.Add(productId);

        // var components = await _context.BOMs
        //     .Where(b => b.ProductId == productId)
        //     .Select(b => b.ComponentProductId)
        //     .ToListAsync();

        // if (components.Contains(targetId))
        // {
        //     return true; // Found circular reference
        // }

        // foreach (var componentId in components)
        // {
        //     if (await CheckTransitiveBOMDependency(componentId, targetId, visited))
        //     {
        //         return true;
        //     }
        // }

        // return false;
    }

    /// <summary>
    /// Checks if product is in a pending merge
    /// Requirements: 10.4
    /// </summary>
    public async Task<bool> IsProductInPendingMergeAsync(int productId)
    {
        // Check if product is in any pending merge history
        var pendingMerge = await _context.MergeHistories
            .Where(m => m.Status == MergeStatus.Pending)
            .Where(m => m.CanonicalProductId == productId || m.MergedProductIds.Contains(productId))
            .AnyAsync();

        return pendingMerge;
    }
}
