using Katana.Core.Entities;

namespace Katana.Core.Interfaces;

public interface IVariantMappingRepository
{
    Task<VariantMapping?> GetByVariantIdAsync(long variantId);
    Task<VariantMapping> UpsertAsync(long variantId, int productId, string sku, int? productVariantId = null);
}
