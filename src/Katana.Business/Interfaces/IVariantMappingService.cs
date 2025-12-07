using Katana.Core.Entities;

namespace Katana.Business.Interfaces;

public interface IVariantMappingService
{
    Task<VariantMapping?> GetMappingAsync(long variantId);
    Task<VariantMapping> CreateOrUpdateAsync(long variantId, int productId, string sku, int? productVariantId = null);
}
