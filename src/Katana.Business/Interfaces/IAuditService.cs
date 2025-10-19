namespace Katana.Business.Interfaces;

/// <summary>
/// Kullanıcı aksiyonlarını kaydetmek için audit log servisi
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// CREATE işlemini logla
    /// </summary>
    void LogCreate(string entityName, string entityId, string performedBy, string? details = null);
    
    /// <summary>
    /// UPDATE işlemini logla
    /// </summary>
    void LogUpdate(string entityName, string entityId, string performedBy, string? changes = null, string? details = null);
    
    /// <summary>
    /// DELETE işlemini logla
    /// </summary>
    void LogDelete(string entityName, string entityId, string performedBy, string? details = null);
    
    /// <summary>
    /// SYNC işlemini logla
    /// </summary>
    void LogSync(string syncType, string performedBy, string? details = null);
    
    /// <summary>
    /// LOGIN işlemini logla
    /// </summary>
    void LogLogin(string username, string? ipAddress = null, string? userAgent = null);
    
    /// <summary>
    /// Özel aksiyon logla
    /// </summary>
    void LogAction(string actionType, string entityName, string? entityId, string performedBy, string? details = null);
}
