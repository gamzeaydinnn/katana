using Katana.Core.DTOs;

namespace Katana.Business.Interfaces;

/// <summary>
/// Varyant gruplama servisi interface'i
/// </summary>
public interface IVariantGroupingService
{
    /// <summary>
    /// Tüm varyantları ana ürün altında gruplar
    /// </summary>
    Task<List<ProductVariantGroup>> GroupVariantsByProductAsync();

    /// <summary>
    /// Belirli bir ürünün varyantlarını getirir
    /// </summary>
    Task<ProductVariantGroup?> GetVariantGroupAsync(long productId);

    /// <summary>
    /// Orphan (ana ürünsüz) varyantları tespit eder
    /// </summary>
    Task<List<VariantDetail>> GetOrphanVariantsAsync();

    /// <summary>
    /// Varyant detayını getirir
    /// </summary>
    Task<VariantDetail?> GetVariantDetailAsync(long variantId);

    /// <summary>
    /// Ürün adına göre varyant gruplarını arar
    /// </summary>
    Task<List<ProductVariantGroup>> SearchVariantGroupsAsync(string searchTerm);
}
