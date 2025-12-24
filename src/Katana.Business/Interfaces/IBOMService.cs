using Katana.Core.DTOs;

namespace Katana.Business.Interfaces;

/// <summary>
/// BOM (Bill of Materials) servisi interface'i
/// </summary>
public interface IBOMService
{
    /// <summary>
    /// Sipariş için BOM gereksinimlerini hesaplar
    /// </summary>
    Task<BOMRequirementResult> CalculateBOMRequirementsAsync(int salesOrderId);

    /// <summary>
    /// Ürün için BOM bileşenlerini getirir
    /// </summary>
    Task<List<BOMComponent>> GetBOMComponentsAsync(long variantId);

    /// <summary>
    /// Stok eksikliklerini tespit eder
    /// </summary>
    Task<List<StockShortage>> DetectShortagesAsync(BOMRequirementResult requirements);

    /// <summary>
    /// Ürünün BOM'u olup olmadığını kontrol eder
    /// </summary>
    Task<bool> HasBOMAsync(long variantId);
}
