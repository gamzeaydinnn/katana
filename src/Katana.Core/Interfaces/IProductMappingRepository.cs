using Katana.Core.Entities;

namespace Katana.Core.Interfaces;

/// <summary>
/// Katana ürünleri ile Luca stok kartları arasındaki eşleştirme veritabanı işlemleri
/// </summary>
public interface IProductMappingRepository
{
    /// <summary>
    /// Katana ürün ID'sine göre aktif mapping'i getirir
    /// </summary>
    Task<ProductLucaMapping?> GetActiveMappingByProductIdAsync(string katanaProductId);
    
    /// <summary>
    /// Katana ürün ID'sine göre tüm versiyonları getirir
    /// </summary>
    Task<List<ProductLucaMapping>> GetAllVersionsByProductIdAsync(string katanaProductId);
    
    /// <summary>
    /// Luca stok koduna göre mapping'i getirir
    /// </summary>
    Task<ProductLucaMapping?> GetByLucaStockCodeAsync(string lucaStockCode);
    
    /// <summary>
    /// Yeni mapping oluşturur
    /// </summary>
    Task<ProductLucaMapping> CreateAsync(ProductLucaMapping mapping);
    
    /// <summary>
    /// Mapping'i günceller
    /// </summary>
    Task UpdateAsync(ProductLucaMapping mapping);
    
    /// <summary>
    /// Eski versiyonları pasif yapar
    /// </summary>
    Task DeactivateOldVersionsAsync(string katanaProductId, int currentMappingId);
    
    /// <summary>
    /// Sync durumunu SYNCED olarak işaretler
    /// </summary>
    Task MarkAsSyncedAsync(int mappingId, long lucaStockId);
    
    /// <summary>
    /// Sync durumunu FAILED olarak işaretler
    /// </summary>
    Task MarkAsSyncFailedAsync(int mappingId, string errorMessage);
    
    /// <summary>
    /// PENDING durumundaki tüm mapping'leri getirir
    /// </summary>
    Task<List<ProductLucaMapping>> GetPendingMappingsAsync();
    
    /// <summary>
    /// FAILED durumundaki tüm mapping'leri getirir
    /// </summary>
    Task<List<ProductLucaMapping>> GetFailedMappingsAsync();
    
    /// <summary>
    /// Tüm aktif mapping'leri getirir
    /// </summary>
    Task<List<ProductLucaMapping>> GetAllActiveMappingsAsync();
}
