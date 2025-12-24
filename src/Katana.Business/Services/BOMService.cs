using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Katana.Business.Services;

/// <summary>
/// BOM (Bill of Materials) servisi implementasyonu
/// </summary>
public class BOMService : IBOMService
{
    private readonly IntegrationDbContext _context;
    private readonly ILogger<BOMService> _logger;

    public BOMService(
        IntegrationDbContext context,
        ILogger<BOMService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<BOMRequirementResult> CalculateBOMRequirementsAsync(int salesOrderId)
    {
        _logger.LogInformation("Calculating BOM requirements for order {OrderId}", salesOrderId);

        var order = await _context.SalesOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == salesOrderId);

        if (order == null)
        {
            throw new ArgumentException($"Sales order not found: {salesOrderId}");
        }

        var result = new BOMRequirementResult
        {
            SalesOrderId = salesOrderId,
            OrderNo = order.OrderNo,
            OrderDate = order.OrderCreatedDate ?? DateTime.UtcNow
        };

        var totalMaterials = new Dictionary<long, MaterialRequirement>();

        foreach (var line in order.Lines)
        {
            var lineRequirement = await CalculateLineRequirementAsync(line);
            result.LineRequirements.Add(lineRequirement);

            // Aggregate total materials
            foreach (var material in lineRequirement.Materials)
            {
                if (totalMaterials.TryGetValue(material.ComponentVariantId, out var existing))
                {
                    existing.RequiredQuantity += material.RequiredQuantity;
                    existing.TotalCost = (existing.TotalCost ?? 0) + (material.TotalCost ?? 0);
                }
                else
                {
                    totalMaterials[material.ComponentVariantId] = new MaterialRequirement
                    {
                        ComponentVariantId = material.ComponentVariantId,
                        ComponentSKU = material.ComponentSKU,
                        ComponentName = material.ComponentName,
                        RequiredQuantity = material.RequiredQuantity,
                        CurrentStock = material.CurrentStock,
                        AvailableStock = material.AvailableStock,
                        Unit = material.Unit,
                        UnitCost = material.UnitCost,
                        TotalCost = material.TotalCost,
                        BOMRatio = material.BOMRatio
                    };
                }
            }
        }

        result.TotalMaterialRequirements = totalMaterials.Values.ToList();


        // Detect shortages
        result.Shortages = await DetectShortagesAsync(result);
        result.HasShortages = result.Shortages.Any();

        // Calculate total estimated cost
        result.TotalEstimatedCost = result.TotalMaterialRequirements
            .Sum(m => m.TotalCost ?? 0);

        _logger.LogInformation(
            "BOM calculation complete for order {OrderId}: {LineCount} lines, {MaterialCount} materials, {ShortageCount} shortages",
            salesOrderId, result.LineRequirements.Count, result.TotalMaterialRequirements.Count, result.Shortages.Count);

        return result;
    }

    /// <inheritdoc />
    public async Task<List<BOMComponent>> GetBOMComponentsAsync(long variantId)
    {
        _logger.LogDebug("Getting BOM components for variant {VariantId}", variantId);

        var bomEntries = await _context.BillOfMaterials
            .Include(b => b.Material)
            .Where(b => b.ProductId == variantId)
            .ToListAsync();

        return bomEntries.Select(b => new BOMComponent
        {
            ComponentId = b.Id,
            ParentVariantId = b.ProductId,
            ComponentVariantId = b.MaterialId,
            ComponentSKU = b.Material?.SKU ?? string.Empty,
            ComponentName = b.Material?.Name ?? string.Empty,
            Quantity = b.Quantity,
            Unit = b.Unit,
            UnitCost = b.Material?.PurchasePrice,
            IsActive = true,
            SortOrder = null,
            Notes = null
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<List<StockShortage>> DetectShortagesAsync(BOMRequirementResult requirements)
    {
        _logger.LogDebug("Detecting shortages for {MaterialCount} materials", 
            requirements.TotalMaterialRequirements.Count);

        var shortages = new List<StockShortage>();

        foreach (var material in requirements.TotalMaterialRequirements)
        {
            // Get current stock
            var product = await _context.Products
                .Include(p => p.StockMovements)
                .FirstOrDefaultAsync(p => p.Id == material.ComponentVariantId);

            var currentStock = product?.Stock ?? 0;
            material.CurrentStock = currentStock;
            material.AvailableStock = currentStock;
            material.ShortageQuantity = Math.Max(0, material.RequiredQuantity - currentStock);

            if (material.ShortageQuantity > 0)
            {
                shortages.Add(new StockShortage
                {
                    VariantId = material.ComponentVariantId,
                    SKU = material.ComponentSKU,
                    ProductName = material.ComponentName,
                    Required = material.RequiredQuantity,
                    Available = currentStock,
                    Shortage = material.ShortageQuantity,
                    SuggestPurchaseOrder = true,
                    EstimatedPurchaseCost = material.UnitCost * material.ShortageQuantity
                });
            }
        }

        return shortages;
    }

    /// <inheritdoc />
    public async Task<bool> HasBOMAsync(long variantId)
    {
        return await _context.BillOfMaterials
            .AnyAsync(b => b.ProductId == variantId);
    }

    private async Task<BOMLineRequirement> CalculateLineRequirementAsync(SalesOrderLine line)
    {
        var requirement = new BOMLineRequirement
        {
            OrderLineId = line.Id,
            VariantId = line.VariantId,
            SKU = line.SKU,
            ProductName = line.ProductName ?? line.SKU,
            OrderedQuantity = line.Quantity
        };

        // Get BOM components
        var bomComponents = await GetBOMComponentsAsync(line.VariantId);
        requirement.HasBOM = bomComponents.Any();

        if (!requirement.HasBOM)
        {
            return requirement;
        }

        foreach (var component in bomComponents)
        {
            var requiredQty = component.Quantity * line.Quantity;
            var unitCost = component.UnitCost ?? 0;

            requirement.Materials.Add(new MaterialRequirement
            {
                ComponentVariantId = component.ComponentVariantId,
                ComponentSKU = component.ComponentSKU,
                ComponentName = component.ComponentName,
                RequiredQuantity = requiredQty,
                Unit = component.Unit,
                UnitCost = unitCost,
                TotalCost = unitCost * requiredQty,
                BOMRatio = component.Quantity
            });
        }

        requirement.EstimatedLineCost = requirement.Materials.Sum(m => m.TotalCost ?? 0);

        return requirement;
    }
}
