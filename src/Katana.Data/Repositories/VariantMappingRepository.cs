using Katana.Core.Entities;
using Katana.Core.Interfaces;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Katana.Data.Repositories;

public class VariantMappingRepository : IVariantMappingRepository
{
    private readonly IntegrationDbContext _context;

    public VariantMappingRepository(IntegrationDbContext context)
    {
        _context = context;
    }

    public async Task<VariantMapping?> GetByVariantIdAsync(long variantId)
    {
        return await _context.VariantMappings
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.KatanaVariantId == variantId);
    }

    public async Task<VariantMapping> UpsertAsync(long variantId, int productId, string sku, int? productVariantId = null)
    {
        var mapping = await _context.VariantMappings
            .FirstOrDefaultAsync(m => m.KatanaVariantId == variantId);

        if (mapping == null)
        {
            mapping = new VariantMapping
            {
                KatanaVariantId = variantId,
                ProductId = productId,
                ProductVariantId = productVariantId,
                Sku = sku,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.VariantMappings.Add(mapping);
        }
        else
        {
            mapping.ProductId = productId;
            mapping.ProductVariantId = productVariantId;
            mapping.Sku = sku;
            mapping.UpdatedAt = DateTime.UtcNow;
            _context.VariantMappings.Update(mapping);
        }

        await _context.SaveChangesAsync();
        return mapping;
    }
}
