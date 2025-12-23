using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Katana.Business.Services;

/// <summary>
/// SKU doğrulama ve yeniden adlandırma servisi implementasyonu
/// </summary>
public class SKUValidationService : ISKUValidationService
{
    private readonly IntegrationDbContext _context;
    private readonly ILogger<SKUValidationService> _logger;

    // SKU format pattern: PRODUCT-VARIANT-ATTRIBUTE (e.g., TSHIRT-RED-M)
    private static readonly Regex SKUPattern = new(
        @"^[A-Z0-9]+-[A-Z0-9]+-[A-Z0-9]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Relaxed pattern for validation (at least product code)
    private static readonly Regex RelaxedSKUPattern = new(
        @"^[A-Z0-9]+(-[A-Z0-9]+)*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public SKUValidationService(
        IntegrationDbContext context,
        ILogger<SKUValidationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public SKUValidationResult ValidateSKU(string sku)
    {
        var result = new SKUValidationResult
        {
            SKU = sku
        };

        if (string.IsNullOrWhiteSpace(sku))
        {
            result.IsValid = false;
            result.ErrorMessage = "SKU boş olamaz";
            result.ValidationErrors.Add("SKU boş olamaz");
            return result;
        }

        var normalizedSku = sku.Trim().ToUpperInvariant();


        // Check strict format
        if (SKUPattern.IsMatch(normalizedSku))
        {
            result.IsValid = true;
            result.ParsedComponents = ParseSKUComponents(normalizedSku);
            return result;
        }

        // Check relaxed format
        if (RelaxedSKUPattern.IsMatch(normalizedSku))
        {
            result.IsValid = true;
            result.ParsedComponents = ParseSKUComponents(normalizedSku);
            result.SuggestedFormat = GenerateSuggestedFormat(normalizedSku);
            
            if (result.SuggestedFormat != normalizedSku)
            {
                result.ValidationErrors.Add($"SKU formatı önerilen formata uymuyor. Önerilen: {result.SuggestedFormat}");
            }
            return result;
        }

        // Invalid format
        result.IsValid = false;
        result.ErrorMessage = "SKU formatı geçersiz. Beklenen format: PRODUCT-VARIANT-ATTRIBUTE (örn: TSHIRT-RED-M)";
        result.ValidationErrors.Add(result.ErrorMessage);
        result.SuggestedFormat = "PRODUCT-VARIANT-ATTRIBUTE";

        return result;
    }

    /// <inheritdoc />
    public async Task<SKURenameResult> RenameSKUAsync(string oldSku, string newSku)
    {
        _logger.LogInformation("Renaming SKU from {OldSKU} to {NewSKU}", oldSku, newSku);

        var result = new SKURenameResult
        {
            OldSKU = oldSku,
            NewSKU = newSku
        };

        // Validate new SKU
        var validation = ValidateSKU(newSku);
        if (!validation.IsValid)
        {
            result.Success = false;
            result.Errors.AddRange(validation.ValidationErrors);
            return result;
        }

        // Check if new SKU already exists
        var existingProduct = await _context.Products
            .FirstOrDefaultAsync(p => p.SKU == newSku && p.IsActive);

        if (existingProduct != null)
        {
            result.Success = false;
            result.Errors.Add($"SKU '{newSku}' zaten mevcut (Product ID: {existingProduct.Id})");
            return result;
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1. Update Products table
            var products = await _context.Products
                .Where(p => p.SKU == oldSku)
                .ToListAsync();

            foreach (var product in products)
            {
                product.SKU = newSku;
                product.UpdatedAt = DateTime.UtcNow;
            }
            result.UpdatedProducts = products.Count;

            // 2. Update SalesOrderLines table
            var orderLines = await _context.SalesOrderLines
                .Where(l => l.SKU == oldSku)
                .ToListAsync();

            foreach (var line in orderLines)
            {
                line.SKU = newSku;
                line.UpdatedAt = DateTime.UtcNow;
            }
            result.UpdatedOrderLines = orderLines.Count;

            // 3. Update StockMovements table
            var stockMovements = await _context.StockMovements
                .Where(m => m.ProductSku == oldSku)
                .ToListAsync();

            foreach (var movement in stockMovements)
            {
                movement.ProductSku = newSku;
            }
            result.UpdatedStockMovements = stockMovements.Count;

            // 4. Update MappingTables (Luca mappings)
            var mappings = await _context.MappingTables
                .Where(m => m.MappingType == "Product" && m.SourceValue == oldSku)
                .ToListAsync();

            foreach (var mapping in mappings)
            {
                mapping.SourceValue = newSku;
                mapping.UpdatedAt = DateTime.UtcNow;
            }
            result.UpdatedLucaMappings = mappings.Count;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            result.Success = true;
            result.CompletedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "SKU rename completed: {OldSKU} -> {NewSKU}. Products: {Products}, OrderLines: {OrderLines}, StockMovements: {StockMovements}, Mappings: {Mappings}",
                oldSku, newSku, result.UpdatedProducts, result.UpdatedOrderLines, result.UpdatedStockMovements, result.UpdatedLucaMappings);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            result.Success = false;
            result.Errors.Add(ex.Message);
            _logger.LogError(ex, "Failed to rename SKU from {OldSKU} to {NewSKU}", oldSku, newSku);
        }

        return result;
    }


    /// <inheritdoc />
    public async Task<List<SKURenamePreview>> PreviewBulkRenameAsync(List<SKURenameRequest> requests)
    {
        _logger.LogInformation("Generating preview for {Count} SKU renames", requests.Count);

        var previews = new List<SKURenamePreview>();

        foreach (var request in requests)
        {
            var preview = new SKURenamePreview
            {
                OldSKU = request.OldSKU,
                NewSKU = request.NewSKU
            };

            // Validate new SKU
            var validation = ValidateSKU(request.NewSKU);
            preview.IsValid = validation.IsValid;
            preview.ValidationError = validation.ErrorMessage;

            if (!validation.IsValid)
            {
                previews.Add(preview);
                continue;
            }

            // Check for conflicts
            var existingProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.SKU == request.NewSKU && p.IsActive);

            if (existingProduct != null)
            {
                preview.HasConflict = true;
                preview.ConflictMessage = $"SKU '{request.NewSKU}' zaten mevcut (Product ID: {existingProduct.Id})";
                preview.IsValid = false;
            }

            // Count affected records
            preview.AffectedProducts = await _context.Products
                .CountAsync(p => p.SKU == request.OldSKU);

            preview.AffectedOrderLines = await _context.SalesOrderLines
                .CountAsync(l => l.SKU == request.OldSKU);

            preview.AffectedStockMovements = await _context.StockMovements
                .CountAsync(m => m.ProductSku == request.OldSKU);

            preview.AffectedLucaMappings = await _context.MappingTables
                .CountAsync(m => m.MappingType == "Product" && m.SourceValue == request.OldSKU);

            previews.Add(preview);
        }

        return previews;
    }

    /// <inheritdoc />
    public async Task<BulkSKURenameResult> ExecuteBulkRenameAsync(List<SKURenameRequest> requests)
    {
        _logger.LogInformation("Executing bulk rename for {Count} SKUs", requests.Count);

        var result = new BulkSKURenameResult
        {
            TotalRequested = requests.Count,
            StartedAt = DateTime.UtcNow
        };

        foreach (var request in requests)
        {
            var renameResult = await RenameSKUAsync(request.OldSKU, request.NewSKU);
            result.Results.Add(renameResult);

            if (renameResult.Success)
            {
                result.SuccessCount++;
            }
            else
            {
                result.FailedCount++;
            }
        }

        result.Success = result.FailedCount == 0;
        result.CompletedAt = DateTime.UtcNow;

        _logger.LogInformation(
            "Bulk rename completed: {Success}/{Total} successful",
            result.SuccessCount, result.TotalRequested);

        return result;
    }

    private SKUComponents ParseSKUComponents(string sku)
    {
        var parts = sku.Split('-');
        return new SKUComponents
        {
            ProductCode = parts.Length > 0 ? parts[0] : string.Empty,
            VariantCode = parts.Length > 1 ? parts[1] : null,
            AttributeCode = parts.Length > 2 ? parts[2] : null
        };
    }

    private string GenerateSuggestedFormat(string sku)
    {
        var parts = sku.Split('-');
        
        if (parts.Length == 1)
        {
            return $"{parts[0]}-VAR-ATTR";
        }
        else if (parts.Length == 2)
        {
            return $"{parts[0]}-{parts[1]}-ATTR";
        }
        
        return sku;
    }
}
