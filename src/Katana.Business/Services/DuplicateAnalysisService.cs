using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Katana.Business.Services;

/// <summary>
/// Service for analyzing and managing product duplicates
/// </summary>
public class DuplicateAnalysisService : IDuplicateAnalysisService
{
    private readonly IntegrationDbContext _context;
    private readonly ILogger<DuplicateAnalysisService> _logger;

    public DuplicateAnalysisService(
        IntegrationDbContext context,
        ILogger<DuplicateAnalysisService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Analyzes all products and returns duplicate groups
    /// Requirements: 1.1, 1.2, 1.4, 1.5, 11.1, 11.2
    /// </summary>
    public async Task<List<ProductDuplicateGroup>> AnalyzeDuplicatesAsync()
    {
        _logger.LogInformation("Starting duplicate analysis");

        // Get all products (Category is a string property, not a navigation)
        var products = await _context.Products
            .Where(p => p.IsActive)
            .ToListAsync();

        // Group products by name AND KatanaOrderId
        var groupedProducts = new List<ProductDuplicateGroup>();
        
        // First, group by KatanaOrderId (Req 11.1, 11.2)
        var orderGroups = products
            .Where(p => p.KatanaOrderId.HasValue)
            .GroupBy(p => p.KatanaOrderId!.Value)
            .Where(g => g.Count() > 1) // Only groups with multiple products
            .Select(g => new ProductDuplicateGroup
            {
                ProductName = $"{g.First().Name} (Sipariş #{g.Key})",
                Count = g.Count(),
                KatanaOrderId = g.Key,
                Products = g.Select(p => new ProductSummary
                {
                    Id = p.Id,
                    Name = p.Name,
                    SKU = p.SKU ?? string.Empty,
                    Code = p.SKU ?? string.Empty,
                    CategoryName = p.Category ?? string.Empty,
                    IsActive = p.IsActive,
                    KatanaOrderId = p.KatanaOrderId
                }).ToList()
            })
            .ToList();
        
        groupedProducts.AddRange(orderGroups);
        
        // Then, group by name (for products without KatanaOrderId or different orders)
        var nameGroups = products
            .Where(p => !p.KatanaOrderId.HasValue || 
                       !orderGroups.Any(og => og.KatanaOrderId == p.KatanaOrderId))
            .GroupBy(p => p.Name)
            .Where(g => g.Count() > 1) // Exclude single-product groups (Req 1.4)
            .Select(g => new ProductDuplicateGroup
            {
                ProductName = g.Key,
                Count = g.Count(),
                Products = g.Select(p => new ProductSummary
                {
                    Id = p.Id,
                    Name = p.Name,
                    SKU = p.SKU ?? string.Empty,
                    Code = p.SKU ?? string.Empty,
                    CategoryName = p.Category ?? string.Empty,
                    IsActive = p.IsActive,
                    KatanaOrderId = p.KatanaOrderId
                }).ToList()
            })
            .ToList();
        
        groupedProducts.AddRange(nameGroups);
        
        // Sort by count descending (Req 1.5)
        groupedProducts = groupedProducts.OrderByDescending(g => g.Count).ToList();

        // Calculate reference counts for each product
        foreach (var group in groupedProducts)
        {
            foreach (var product in group.Products)
            {
                await CalculateReferenceCountsAsync(product);
                product.IsSuggestedCanonical = DetermineCanonicalSuggestion(group.Products, product, group.KatanaOrderId);
            }
        }

        // Check for keep separate flags
        var keepSeparateGroups = await _context.KeepSeparateGroups
            .Where(k => k.RemovedAt == null)
            .ToListAsync();

        foreach (var group in groupedProducts)
        {
            var keepSeparate = keepSeparateGroups.FirstOrDefault(k => k.ProductName == group.ProductName);
            if (keepSeparate != null)
            {
                group.IsKeepSeparate = true;
                group.KeepSeparateReason = keepSeparate.Reason;
                group.KeepSeparateDate = keepSeparate.CreatedAt;
                group.KeepSeparateBy = keepSeparate.CreatedBy;
            }
        }

        _logger.LogInformation("Duplicate analysis completed. Found {Count} duplicate groups", groupedProducts.Count);

        return groupedProducts;
    }

    /// <summary>
    /// Gets detailed information about a specific duplicate group
    /// Requirements: 2.1, 2.4, 2.5, 3.2, 3.3
    /// </summary>
    public async Task<ProductDuplicateGroup?> GetDuplicateGroupDetailAsync(string productName)
    {
        _logger.LogInformation("Getting duplicate group detail for {ProductName}", productName);

        var products = await _context.Products
            .Where(p => p.Name == productName && p.IsActive)
            .ToListAsync();

        if (products.Count <= 1)
        {
            return null;
        }

        var group = new ProductDuplicateGroup
        {
            ProductName = productName,
            Count = products.Count,
            Products = products.Select(p => new ProductSummary
            {
                Id = p.Id,
                Name = p.Name,
                SKU = p.SKU ?? string.Empty,
                Code = p.SKU ?? string.Empty,
                CategoryName = p.Category ?? string.Empty,
                IsActive = p.IsActive
            }).ToList()
        };

        // Calculate reference counts
        foreach (var product in group.Products)
        {
            await CalculateReferenceCountsAsync(product);
            product.IsSuggestedCanonical = DetermineCanonicalSuggestion(group.Products, product, null);
        }

        // Check keep separate flag
        var keepSeparate = await _context.KeepSeparateGroups
            .FirstOrDefaultAsync(k => k.ProductName == productName && k.RemovedAt == null);

        if (keepSeparate != null)
        {
            group.IsKeepSeparate = true;
            group.KeepSeparateReason = keepSeparate.Reason;
            group.KeepSeparateDate = keepSeparate.CreatedAt;
            group.KeepSeparateBy = keepSeparate.CreatedBy;
        }

        return group;
    }

    /// <summary>
    /// Filters duplicate groups based on criteria
    /// Requirements: 8.1, 8.2, 8.3, 8.4
    /// </summary>
    public async Task<List<ProductDuplicateGroup>> FilterDuplicateGroupsAsync(DuplicateFilterCriteria criteria)
    {
        _logger.LogInformation("Filtering duplicate groups with criteria");

        var groups = await AnalyzeDuplicatesAsync();

        // Apply category filter (Req 8.1)
        if (!string.IsNullOrEmpty(criteria.CategoryName))
        {
            groups = groups.Where(g => g.Products.Any(p => p.CategoryName == criteria.CategoryName)).ToList();
        }

        // Apply minimum count filter (Req 8.2)
        if (criteria.MinimumCount.HasValue)
        {
            groups = groups.Where(g => g.Count >= criteria.MinimumCount.Value).ToList();
        }

        // Apply name pattern search (Req 8.3)
        if (!string.IsNullOrEmpty(criteria.NamePattern))
        {
            groups = groups.Where(g => g.ProductName.Contains(criteria.NamePattern, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // Apply SKU pattern search (Req 8.4)
        if (!string.IsNullOrEmpty(criteria.SkuPattern))
        {
            groups = groups.Where(g => g.Products.Any(p => p.SKU.Contains(criteria.SkuPattern, StringComparison.OrdinalIgnoreCase))).ToList();
        }

        return groups;
    }

    /// <summary>
    /// Exports duplicate analysis results to CSV
    /// Requirements: 9.1, 9.2, 9.3, 9.4
    /// </summary>
    public async Task<byte[]> ExportDuplicateAnalysisAsync(List<ProductDuplicateGroup> groups)
    {
        _logger.LogInformation("Exporting duplicate analysis to CSV");

        var csv = new StringBuilder();

        // Add header with metadata (Req 9.4)
        csv.AppendLine($"# Duplicate Analysis Export");
        csv.AppendLine($"# Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        csv.AppendLine($"# Total Groups: {groups.Count}");
        csv.AppendLine();

        // Add column headers (Req 9.2)
        csv.AppendLine("Product Name,Product ID,SKU,Code,Category,Sales Order Count,BOM Count,Stock Movement Count,Is Suggested Canonical,Is Active");

        // Add data rows (Req 9.1, 9.2, 9.3)
        foreach (var group in groups)
        {
            foreach (var product in group.Products)
            {
                csv.AppendLine($"\"{product.Name}\",{product.Id},\"{product.SKU}\",\"{product.Code}\",\"{product.CategoryName}\",{product.SalesOrderCount},{product.BOMCount},{product.StockMovementCount},{product.IsSuggestedCanonical},{product.IsActive}");
            }
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    /// <summary>
    /// Calculates reference counts for a product
    /// Requirements: 2.4, 2.5
    /// </summary>
    private async Task CalculateReferenceCountsAsync(ProductSummary product)
    {
        // TODO: Count sales orders - SalesOrder.ProductId doesn't exist yet
        product.SalesOrderCount = 0; // await _context.SalesOrders.CountAsync(so => so.ProductId == product.Id);

        // TODO: Get sales order ID if exists
        product.SalesOrderId = null; // var salesOrder = await _context.SalesOrders.Where(so => so.ProductId == product.Id).FirstOrDefaultAsync(); salesOrder?.Id;

        // TODO: Count BOMs - IntegrationDbContext.BOMs doesn't exist yet
        product.BOMCount = 0; // await _context.BOMs.CountAsync(b => b.ProductId == product.Id || b.ComponentProductId == product.Id);

        // Count stock movements (Req 2.5)
        product.StockMovementCount = await _context.StockMovements
            .CountAsync(sm => sm.ProductId == product.Id);
    }

    /// <summary>
    /// Determines if a product should be suggested as canonical
    /// Requirements: 3.2, 3.3, 11.5
    /// </summary>
    private bool DetermineCanonicalSuggestion(List<ProductSummary> products, ProductSummary product, long? katanaOrderId)
    {
        // Priority 0: For products with same KatanaOrderId, select first created (Req 11.5)
        if (katanaOrderId.HasValue)
        {
            var firstProduct = products
                .OrderBy(p => p.Id) // Assuming lower ID = created first
                .First();
            return product.Id == firstProduct.Id;
        }
        
        // Priority 1: Products with active sales orders (Req 3.2)
        var hasActiveSalesOrders = product.SalesOrderCount > 0;
        var maxSalesOrders = products.Max(p => p.SalesOrderCount);

        if (hasActiveSalesOrders && product.SalesOrderCount == maxSalesOrders)
        {
            return true;
        }

        // Priority 2: Products with most complete data (Req 3.3)
        var completenessScore = CalculateCompletenessScore(product);
        var maxCompleteness = products.Max(p => CalculateCompletenessScore(p));

        return completenessScore == maxCompleteness && completenessScore > 0;
    }

    /// <summary>
    /// Calculates data completeness score for a product
    /// </summary>
    private int CalculateCompletenessScore(ProductSummary product)
    {
        int score = 0;

        if (!string.IsNullOrEmpty(product.SKU)) score++;
        if (!string.IsNullOrEmpty(product.Code)) score++;
        if (!string.IsNullOrEmpty(product.CategoryName)) score++;
        if (product.SalesOrderCount > 0) score++;
        if (product.BOMCount > 0) score++;
        if (product.StockMovementCount > 0) score++;

        return score;
    }
}
