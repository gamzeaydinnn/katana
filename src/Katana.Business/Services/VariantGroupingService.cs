using System.Text.Json;
using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Katana.Business.Services;

public class VariantGroupingService : IVariantGroupingService
{
    private readonly IntegrationDbContext _context;

    public VariantGroupingService(IntegrationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductVariantGroup>> GroupVariantsByProductAsync()
    {
        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.Variants)
            .ToListAsync();

        return products
            .Select(BuildGroup)
            .OrderBy(g => g.ProductName)
            .ToList();
    }

    public async Task<ProductVariantGroup?> GetVariantGroupAsync(long productId)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == productId);

        return product == null ? null : BuildGroup(product);
    }

    public async Task<List<VariantDetail>> GetOrphanVariantsAsync()
    {
        var orphans = await _context.ProductVariants
            .AsNoTracking()
            .Where(v => !_context.Products.Any(p => p.Id == v.ProductId))
            .ToListAsync();

        return orphans
            .Select(MapOrphanVariant)
            .OrderBy(v => v.SKU)
            .ToList();
    }

    public async Task<VariantDetail?> GetVariantDetailAsync(long variantId)
    {
        var variant = await _context.ProductVariants
            .AsNoTracking()
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == variantId);

        if (variant != null)
        {
            return MapVariant(variant, variant.Product, 0m, false);
        }

        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == variantId);

        return product == null ? null : MapStandaloneProduct(product);
    }

    public async Task<List<ProductVariantGroup>> SearchVariantGroupsAsync(string searchTerm)
    {
        var term = searchTerm?.Trim() ?? string.Empty;

        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Variants)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(term))
        {
            query = query.Where(p => p.Name.Contains(term) || p.SKU.Contains(term));
        }

        var products = await query.ToListAsync();

        return products
            .Select(BuildGroup)
            .OrderBy(g => g.ProductName)
            .ToList();
    }

    private ProductVariantGroup BuildGroup(Product product)
    {
        var variants = product.Variants?.ToList() ?? new List<ProductVariant>();
        List<VariantDetail> variantDetails;

        if (variants.Count == 0)
        {
            variantDetails = new List<VariantDetail> { MapStandaloneProduct(product) };
        }
        else
        {
            var perVariantStock = variants.Count == 1 ? product.Stock : 0m;
            variantDetails = variants
                .Select(v => MapVariant(v, product, perVariantStock, false))
                .ToList();
        }

        var totalStock = (decimal)product.Stock;

        return new ProductVariantGroup
        {
            ProductId = product.Id,
            ProductName = product.Name,
            ProductSKU = product.SKU,
            VariantCount = variantDetails.Count,
            Variants = variantDetails,
            TotalStock = totalStock,
            TotalAvailable = totalStock,
            TotalCommitted = 0m,
            HasOrphanVariants = false
        };
    }

    private static VariantDetail MapStandaloneProduct(Product product)
    {
        var stock = (decimal)product.Stock;

        return new VariantDetail
        {
            VariantId = product.Id,
            ProductId = product.Id,
            SKU = product.SKU,
            Barcode = product.Barcode,
            Name = product.Name,
            Attributes = new List<VariantAttribute>(),
            InStock = stock,
            Available = stock,
            Committed = 0m,
            SalesPrice = product.Price,
            PurchasePrice = product.PurchasePrice,
            Unit = null,
            IsOrphan = false,
            LastSyncedAt = null
        };
    }

    private static VariantDetail MapVariant(ProductVariant variant, Product product, decimal stock, bool isOrphan)
    {
        var salesPrice = variant.Price != 0 ? variant.Price : product.Price;

        return new VariantDetail
        {
            VariantId = variant.Id,
            ProductId = isOrphan ? null : product.Id,
            SKU = variant.SKU,
            Barcode = variant.Barcode,
            Name = product.Name,
            Attributes = ParseAttributes(variant.Attributes),
            InStock = stock,
            Available = stock,
            Committed = 0m,
            SalesPrice = salesPrice,
            PurchasePrice = product.PurchasePrice,
            Unit = null,
            IsOrphan = isOrphan,
            LastSyncedAt = null
        };
    }

    private static VariantDetail MapOrphanVariant(ProductVariant variant)
    {
        return new VariantDetail
        {
            VariantId = variant.Id,
            ProductId = null,
            SKU = variant.SKU,
            Barcode = variant.Barcode,
            Name = null,
            Attributes = ParseAttributes(variant.Attributes),
            InStock = 0m,
            Available = 0m,
            Committed = 0m,
            SalesPrice = variant.Price,
            PurchasePrice = null,
            Unit = null,
            IsOrphan = true,
            LastSyncedAt = null
        };
    }

    private static List<VariantAttribute> ParseAttributes(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new List<VariantAttribute>();
        }

        var trimmed = raw.Trim();

        if (trimmed.StartsWith("{") || trimmed.StartsWith("["))
        {
            try
            {
                using var document = JsonDocument.Parse(trimmed);
                if (document.RootElement.ValueKind == JsonValueKind.Array)
                {
                    var parsed = new List<VariantAttribute>();
                    foreach (var element in document.RootElement.EnumerateArray())
                    {
                        if (element.ValueKind == JsonValueKind.Object)
                        {
                            var name = element.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
                            var value = element.TryGetProperty("value", out var valueProp) ? valueProp.GetString() : null;
                            var code = element.TryGetProperty("code", out var codeProp) ? codeProp.GetString() : null;

                            if (!string.IsNullOrWhiteSpace(name) || !string.IsNullOrWhiteSpace(value))
                            {
                                parsed.Add(new VariantAttribute
                                {
                                    Name = name ?? "Attribute",
                                    Value = value ?? string.Empty,
                                    Code = code
                                });
                            }
                        }
                        else if (element.ValueKind == JsonValueKind.String)
                        {
                            var value = element.GetString();
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                parsed.Add(new VariantAttribute { Name = "Attribute", Value = value });
                            }
                        }
                    }

                    if (parsed.Count > 0)
                    {
                        return parsed;
                    }
                }
                else if (document.RootElement.ValueKind == JsonValueKind.Object)
                {
                    var parsed = new List<VariantAttribute>();
                    foreach (var property in document.RootElement.EnumerateObject())
                    {
                        var value = property.Value.ToString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            parsed.Add(new VariantAttribute { Name = property.Name, Value = value });
                        }
                    }

                    if (parsed.Count > 0)
                    {
                        return parsed;
                    }
                }
            }
            catch (JsonException)
            {
                // Ignore invalid JSON and fall back to delimiter parsing.
            }
        }

        var separators = new[] { ';', '|', ',' };
        var parts = trimmed.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        var result = new List<VariantAttribute>();

        foreach (var part in parts)
        {
            var token = part.Trim();
            if (token.Length == 0)
            {
                continue;
            }

            var delimiterIndex = token.IndexOf(':');
            if (delimiterIndex < 0)
            {
                delimiterIndex = token.IndexOf('=');
            }

            if (delimiterIndex > 0)
            {
                var name = token[..delimiterIndex].Trim();
                var value = token[(delimiterIndex + 1)..].Trim();
                result.Add(new VariantAttribute
                {
                    Name = string.IsNullOrWhiteSpace(name) ? "Attribute" : name,
                    Value = value
                });
            }
            else
            {
                result.Add(new VariantAttribute { Name = "Attribute", Value = token });
            }
        }

        return result;
    }
}
