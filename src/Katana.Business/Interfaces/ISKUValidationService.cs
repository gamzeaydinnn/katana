using Katana.Core.DTOs;

namespace Katana.Business.Interfaces;

/// <summary>
/// SKU doğrulama ve yeniden adlandırma servisi interface'i
/// </summary>
public interface ISKUValidationService
{
    /// <summary>
    /// SKU formatını doğrular
    /// </summary>
    SKUValidationResult ValidateSKU(string sku);

    /// <summary>
    /// SKU değişikliğini tüm ilişkili kayıtlara uygular
    /// </summary>
    Task<SKURenameResult> RenameSKUAsync(string oldSku, string newSku);

    /// <summary>
    /// Toplu SKU değişikliği önizlemesi
    /// </summary>
    Task<List<SKURenamePreview>> PreviewBulkRenameAsync(List<SKURenameRequest> requests);

    /// <summary>
    /// Toplu SKU değişikliği uygular
    /// </summary>
    Task<BulkSKURenameResult> ExecuteBulkRenameAsync(List<SKURenameRequest> requests);
}
