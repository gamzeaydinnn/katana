using Katana.Business.Interfaces;
using Katana.Core.Entities;
using Katana.Core.Interfaces;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Katana.Business.Services;

public class VariantMappingService : IVariantMappingService
{
    private readonly IVariantMappingRepository _repository;
    private readonly IntegrationDbContext _context;

    public VariantMappingService(
        IVariantMappingRepository repository,
        IntegrationDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public Task<VariantMapping?> GetMappingAsync(long variantId)
        => _repository.GetByVariantIdAsync(variantId);

    public async Task<VariantMapping> CreateOrUpdateAsync(long variantId, int productId, string sku, int? productVariantId = null)
    {
        var resolvedProductId = productId;
        if (resolvedProductId == 0 && !string.IsNullOrWhiteSpace(sku))
        {
            resolvedProductId = await _context.Products
                .Where(p => p.SKU == sku)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();
        }

        var resolvedVariantId = productVariantId;
        if (!resolvedVariantId.HasValue && !string.IsNullOrWhiteSpace(sku))
        {
            resolvedVariantId = await _context.ProductVariants
                .Where(pv => pv.SKU == sku)
                .Select(pv => (int?)pv.Id)
                .FirstOrDefaultAsync();
        }

        return await _repository.UpsertAsync(variantId, resolvedProductId, sku, resolvedVariantId);
    }
}
