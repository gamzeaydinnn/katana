namespace Katana.Business.Interfaces;

/// <summary>
/// Kullanıcı aksiyonlarını kaydetmek için audit log servisi
/// </summary>
public interface IAuditService
{
    /// CREATE işlemini logla
    void LogCreate(string entityName, string entityId, string performedBy, string? details = null);
    /// UPDATE işlemini logla

    void LogUpdate(string entityName, string entityId, string performedBy, string? changes = null, string? details = null);

    /// DELETE işlemini logla
    void LogDelete(string entityName, string entityId, string performedBy, string? details = null);
    
    /// SYNC işlemini logla

    void LogSync(string syncType, string performedBy, string? details = null);
    
    /// LOGIN işlemini logla

    void LogLogin(string username, string? ipAddress = null, string? userAgent = null);
    

    /// PASSWORD CHANGE işlemini logla

    void LogPasswordChange(string username, string? ipAddress = null, string? userAgent = null);
    

    /// Özel aksiyon logla
   
    void LogAction(string actionType, string entityName, string? entityId, string performedBy, string? details = null);
}
