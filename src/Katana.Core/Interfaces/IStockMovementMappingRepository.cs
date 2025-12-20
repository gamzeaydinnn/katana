using System.Threading.Tasks;

namespace Katana.Core.Interfaces;

/// <summary>
/// Katana stok hareketi ID'lerini Luca kodlarına eşleştirme repository'si
/// Depo (Location) ve Stok (Variant/Product) mapping'leri
/// </summary>
public interface IStockMovementMappingRepository
{
    /// <summary>
    /// Katana Location ID'sini Luca Depo Kodu'na çevirir
    /// </summary>
    Task<string?> GetLucaDepoKoduByLocationIdAsync(int locationId);

    /// <summary>
    /// Katana Variant ID'sini Luca Stok Kodu'na çevirir
    /// </summary>
    Task<string?> GetLucaStokKoduByVariantIdAsync(int variantId);

    /// <summary>
    /// Katana Product ID'sini Luca Stok Kodu'na çevirir
    /// </summary>
    Task<string?> GetLucaStokKoduByProductIdAsync(int productId);

    /// <summary>
    /// Luca'da oluşturulan Transfer ID'sini kaydeder
    /// </summary>
    Task SaveLucaTransferIdAsync(int katanaTransferId, long lucaTransferId);

    /// <summary>
    /// Luca'da oluşturulan DSH (Adjustment) ID'sini kaydeder
    /// </summary>
    Task SaveLucaAdjustmentIdAsync(int katanaAdjustmentId, long lucaDshId);

    /// <summary>
    /// Tüm depo mapping'lerini getirir
    /// </summary>
    Task<Dictionary<int, string>> GetAllLocationMappingsAsync();
}
