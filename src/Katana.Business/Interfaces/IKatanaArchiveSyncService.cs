using Katana.Core.DTOs;

namespace Katana.Business.Interfaces;

/// <summary>
/// Service for synchronizing product archives between local database and Katana API.
/// Archives products in Katana that don't exist in local database.
/// </summary>
public interface IKatanaArchiveSyncService
{
    /// <summary>
    /// Finds products in Katana that don't exist in local database and archives them.
    /// </summary>
    /// <param name="previewOnly">If true, returns preview without making changes</param>
    /// <returns>Result containing archived products and any errors</returns>
    Task<ArchiveSyncResult> SyncArchiveAsync(bool previewOnly = false);
    
    /// <summary>
    /// Gets a preview of products that would be archived without making any changes.
    /// </summary>
    /// <returns>List of products that would be archived</returns>
    Task<List<ProductArchivePreview>> GetArchivePreviewAsync();
}
