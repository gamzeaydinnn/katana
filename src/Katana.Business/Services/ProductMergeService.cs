using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Data.Context;
using Katana.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Katana.Business.Services;

/// <summary>
/// Service for managing product merge operations
/// </summary>
public class ProductMergeService : IProductMergeService
{
    private readonly IntegrationDbContext _context;
    private readonly IMergeValidationService _validationService;
    private readonly IKatanaService _katanaService;
    private readonly ILogger<ProductMergeService> _logger;

    public ProductMergeService(
        IntegrationDbContext context,
        IMergeValidationService validationService,
        IKatanaService katanaService,
        ILogger<ProductMergeService> logger)
    {
        _context = context;
        _validationService = validationService;
        _katanaService = katanaService;
        _logger = logger;
    }

    /// <summary>
    /// Previews the impact of a merge operation
    /// Requirements: 4.1, 4.2, 4.3, 4.4, 4.5
    /// </summary>
    public async Task<MergePreview> PreviewMergeAsync(MergeRequest request)
    {
        _logger.LogInformation("Previewing merge for canonical product {CanonicalProductId}", request.CanonicalProductId);

        var preview = new MergePreview { CanProceed = true };

        // Get canonical product
        var canonicalProduct = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.CanonicalProductId);

        if (canonicalProduct == null)
        {
            preview.CanProceed = false;
            preview.CriticalWarnings.Add("Canonical product not found");
            return preview;
        }

        preview.CanonicalProduct = new ProductSummary
        {
            Id = canonicalProduct.Id,
            Name = canonicalProduct.Name,
            SKU = canonicalProduct.SKU ?? string.Empty,
            Code = canonicalProduct.SKU ?? string.Empty,
            CategoryName = canonicalProduct.Category ?? string.Empty,
            IsActive = canonicalProduct.IsActive
        };

        // Get products to merge
        var productsToMerge = await _context.Products
            .Where(p => request.ProductIdsToMerge.Contains(p.Id))
            .ToListAsync();

        preview.ProductsToMerge = productsToMerge.Select(p => new ProductSummary
        {
            Id = p.Id,
            Name = p.Name,
            SKU = p.SKU ?? string.Empty,
            Code = p.SKU ?? string.Empty,
            CategoryName = p.Category ?? string.Empty,
            IsActive = p.IsActive
        }).ToList();

        // Count references to update (Req 4.1, 4.2, 4.3)
        // TODO: SalesOrder.ProductId property doesn't exist yet - needs to be implemented via Lines
        preview.SalesOrdersToUpdate = 0; // await _context.SalesOrders.CountAsync(so => request.ProductIdsToMerge.Contains(so.ProductId));

        // TODO: IntegrationDbContext.BOMs DbSet doesn't exist yet
        preview.BOMsToUpdate = 0; // await _context.BOMs.CountAsync(b => request.ProductIdsToMerge.Contains(b.ProductId) || request.ProductIdsToMerge.Contains(b.ComponentProductId));

        preview.StockMovementsToUpdate = await _context.StockMovements
            .CountAsync(sm => request.ProductIdsToMerge.Contains(sm.ProductId));

        // Check for active sales orders (Req 4.4)
        // TODO: SalesOrder.ProductId property doesn't exist yet
        var hasActiveSalesOrders = false; // await _context.SalesOrders.AnyAsync(so => request.ProductIdsToMerge.Contains(so.ProductId) && so.Status == "Active");

        if (hasActiveSalesOrders)
        {
            preview.Warnings.Add("Some products have active sales orders that will be updated");
        }

        // Check for unique data (Req 4.5)
        foreach (var product in productsToMerge)
        {
            if (!string.IsNullOrEmpty(product.SKU) && product.SKU != canonicalProduct.SKU)
            {
                preview.CriticalWarnings.Add($"Product {product.Id} has unique SKU '{product.SKU}' that will be lost");
            }
            if (!string.IsNullOrEmpty(product.SKU) && product.SKU != canonicalProduct.SKU)
            {
                preview.CriticalWarnings.Add($"Product {product.Id} has unique code '{product.SKU}' that will be lost");
            }
        }

        return preview;
    }

    /// <summary>
    /// Executes a merge operation
    /// Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 7.1
    /// </summary>
    public async Task<MergeResult> ExecuteMergeAsync(MergeRequest request, string adminUserId)
    {
        _logger.LogInformation("Executing merge for canonical product {CanonicalProductId}", request.CanonicalProductId);

        var result = new MergeResult { Success = false };

        // Validate request (Req 5.1)
        var validation = await _validationService.ValidateMergeRequestAsync(request);
        if (!validation.IsValid)
        {
            result.Errors = validation.Errors;
            return result;
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var canonicalProduct = await _context.Products.FindAsync(request.CanonicalProductId);
            if (canonicalProduct == null)
            {
                result.Errors.Add("Canonical product not found");
                return result;
            }

            // Update sales order references (Req 5.2)
            if (request.UpdateSalesOrders)
            // TODO: Update sales order references - SalesOrder.ProductId doesn't exist yet
            // if (request.UpdateSalesOrders)
            // {
            //     var salesOrders = await _context.SalesOrders
            //         .Where(so => request.ProductIdsToMerge.Contains(so.ProductId))
            //         .ToListAsync();
            //     foreach (var so in salesOrders)
            //     {
            //         so.ProductId = request.CanonicalProductId;
            //         result.SalesOrdersUpdated++;
            //     }
            // }

            // TODO: Update BOM references - IntegrationDbContext.BOMs doesn't exist yet
            // if (request.UpdateBOMs)
            // {
            //     var boms = await _context.BOMs
            //         .Where(b => request.ProductIdsToMerge.Contains(b.ProductId) || 
            //                    request.ProductIdsToMerge.Contains(b.ComponentProductId))
            //         .ToListAsync();
            //     foreach (var bom in boms)
            //     {
            //         if (request.ProductIdsToMerge.Contains(bom.ProductId))
            //         {
            //             bom.ProductId = request.CanonicalProductId;
            //         }
            //         if (request.ProductIdsToMerge.Contains(bom.ComponentProductId))
            //         {
            //             bom.ComponentProductId = request.CanonicalProductId;
            //         }
            //         result.BOMsUpdated++;
            //     }
            // }

            // Update stock movement references (Req 5.4)
            if (request.UpdateStockMovements)
            {
                var stockMovements = await _context.StockMovements
                    .Where(sm => request.ProductIdsToMerge.Contains(sm.ProductId))
                    .ToListAsync();

                foreach (var sm in stockMovements)
                {
                    sm.ProductId = request.CanonicalProductId;
                    result.StockMovementsUpdated++;
                }
            }

            // Mark products as inactive (Req 5.5)
            var productsToInactivate = await _context.Products
                .Where(p => request.ProductIdsToMerge.Contains(p.Id))
                .ToListAsync();

            foreach (var product in productsToInactivate)
            {
                product.IsActive = false;
                result.ProductsInactivated++;
                
                // Delete from Katana API if KatanaProductId exists (Req 11.2)
                if (product.KatanaProductId.HasValue && product.KatanaProductId.Value > 0)
                {
                    try
                    {
                        _logger.LogInformation("Deleting product {ProductId} (Katana ID: {KatanaProductId}) from Katana API", 
                            product.Id, product.KatanaProductId.Value);
                        
                        var deleted = await _katanaService.DeleteProductAsync(product.KatanaProductId.Value);
                        
                        if (deleted)
                        {
                            _logger.LogInformation("Successfully deleted product {ProductId} from Katana API", product.Id);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to delete product {ProductId} from Katana API - product may not exist", product.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error deleting product {ProductId} from Katana API - continuing with merge", product.Id);
                        // Don't fail the entire merge if Katana API deletion fails
                    }
                }
            }

            // Create merge history entry (Req 7.1)
            var mergeHistory = new MergeHistory
            {
                CanonicalProductId = request.CanonicalProductId,
                CanonicalProductName = canonicalProduct.Name,
                CanonicalProductSKU = canonicalProduct.SKU ?? string.Empty,
                MergedProductIds = request.ProductIdsToMerge,
                SalesOrdersUpdated = result.SalesOrdersUpdated,
                BOMsUpdated = result.BOMsUpdated,
                StockMovementsUpdated = result.StockMovementsUpdated,
                AdminUserId = adminUserId,
                AdminUserName = adminUserId, // TODO: Get actual user name
                Status = MergeStatus.Completed,
                Reason = request.Reason
            };

            _context.MergeHistories.Add(mergeHistory);
            await _context.SaveChangesAsync();

            result.MergeHistoryId = mergeHistory.Id;
            result.Success = true;
            result.CompletedAt = DateTime.UtcNow;

            await transaction.CommitAsync();

            _logger.LogInformation("Merge completed successfully. History ID: {HistoryId}", result.MergeHistoryId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error executing merge");
            result.Errors.Add($"Error executing merge: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Rolls back a merge operation
    /// Requirements: 7.5
    /// </summary>
    public async Task<MergeResult> RollbackMergeAsync(int mergeHistoryId, string adminUserId)
    {
        _logger.LogInformation("Rolling back merge {MergeHistoryId}", mergeHistoryId);

        var result = new MergeResult { Success = false };

        var mergeHistory = await _context.MergeHistories.FindAsync(mergeHistoryId);
        if (mergeHistory == null)
        {
            result.Errors.Add("Merge history not found");
            return result;
        }

        if (mergeHistory.Status != MergeStatus.Completed)
        {
            result.Errors.Add("Only completed merges can be rolled back");
            return result;
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Reactivate merged products
            var products = await _context.Products
                .Where(p => mergeHistory.MergedProductIds.Contains(p.Id))
                .ToListAsync();

            foreach (var product in products)
            {
                product.IsActive = true;
            }

            // Update merge history status
            mergeHistory.Status = MergeStatus.RolledBack;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            result.Success = true;
            result.CompletedAt = DateTime.UtcNow;

            _logger.LogInformation("Merge rollback completed successfully");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error rolling back merge");
            result.Errors.Add($"Error rolling back merge: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Marks a group as keep separate
    /// Requirements: 6.1, 6.3
    /// </summary>
    public async Task MarkGroupAsKeepSeparateAsync(string productName, string reason, string adminUserId)
    {
        _logger.LogInformation("Marking group {ProductName} as keep separate", productName);

        var existing = await _context.KeepSeparateGroups
            .FirstOrDefaultAsync(k => k.ProductName == productName && k.RemovedAt == null);

        if (existing != null)
        {
            _logger.LogWarning("Group {ProductName} is already marked as keep separate", productName);
            return;
        }

        var keepSeparate = new KeepSeparateGroup
        {
            ProductName = productName,
            Reason = reason,
            CreatedBy = adminUserId
        };

        _context.KeepSeparateGroups.Add(keepSeparate);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Group {ProductName} marked as keep separate", productName);
    }

    /// <summary>
    /// Removes keep separate flag
    /// Requirements: 6.5
    /// </summary>
    public async Task RemoveKeepSeparateFlagAsync(string productName, string adminUserId)
    {
        _logger.LogInformation("Removing keep separate flag for {ProductName}", productName);

        var keepSeparate = await _context.KeepSeparateGroups
            .FirstOrDefaultAsync(k => k.ProductName == productName && k.RemovedAt == null);

        if (keepSeparate == null)
        {
            _logger.LogWarning("Keep separate flag not found for {ProductName}", productName);
            return;
        }

        keepSeparate.RemovedAt = DateTime.UtcNow;
        keepSeparate.RemovedBy = adminUserId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Keep separate flag removed for {ProductName}", productName);
    }
}
