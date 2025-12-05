using Katana.Core.DTOs;
using Katana.Core.Entities;

namespace Katana.Business.Interfaces;

/// <summary>
/// Ürün güncellemesi sonucu
/// </summary>
public class ProductUpdateResult
{
    public bool Success { get; set; }
    
    /// <summary>
    /// Yeni versiyon mu oluşturuldu?
    /// </summary>
    public bool IsNewVersion { get; set; }
    
    /// <summary>
    /// Versiyonlu Luca stok kodu (örn: SKU-V2)
    /// </summary>
    public string LucaStockCode { get; set; } = string.Empty;
    
    public int Version { get; set; }
    public int MappingId { get; set; }
    
    /// <summary>
    /// Luca'ya gönderilmeli mi?
    /// </summary>
    public bool ShouldSendToLuca { get; set; }
    
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Katana → Luca Append-Only Senkronizasyon için mapping servisi.
/// Luca'da güncelleme endpoint'i olmadığından her değişiklikte yeni versiyonlu SKU oluşturulur.
/// </summary>
public interface IProductMappingService
{
    /// <summary>
    /// Ana metod: Ürün güncellemesi kontrolü ve yeni versiyon oluşturma
    /// </summary>
    Task<ProductUpdateResult> HandleProductUpdateAsync(KatanaProductDto product);
    
    /// <summary>
    /// Değişiklik kontrolü
    /// </summary>
    Task<bool> HasProductChangedAsync(ProductLucaMapping activeMapping, KatanaProductDto product);
    
    /// <summary>
    /// Versiyonlu SKU üretimi (örn: SKU-V2, SKU-V3...)
    /// </summary>
    Task<string> GenerateVersionedSkuAsync(string originalSku, int version);
    
    /// <summary>
    /// Sync sonuçlarını güncelle - başarılı
    /// </summary>
    Task MarkAsSyncedAsync(int mappingId, long lucaStockId);
    
    /// <summary>
    /// Sync sonuçlarını güncelle - başarısız
    /// </summary>
    Task MarkAsSyncFailedAsync(int mappingId, string errorMessage);
    
    /// <summary>
    /// Katana ürün ID'sine göre aktif mapping'i getirir
    /// </summary>
    Task<ProductLucaMapping?> GetActiveMappingAsync(string katanaProductId);
    
    /// <summary>
    /// PENDING durumundaki mapping'leri getirir
    /// </summary>
    Task<List<ProductLucaMapping>> GetPendingMappingsAsync();
    
    /// <summary>
    /// FAILED durumundaki mapping'leri getirir
    /// </summary>
    Task<List<ProductLucaMapping>> GetFailedMappingsAsync();
}
