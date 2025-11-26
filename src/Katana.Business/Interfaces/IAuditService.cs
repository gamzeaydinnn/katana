namespace Katana.Business.Interfaces;




public interface IAuditService
{
    
    void LogCreate(string entityName, string entityId, string performedBy, string? details = null);
    

    void LogUpdate(string entityName, string entityId, string performedBy, string? changes = null, string? details = null);

    
    void LogDelete(string entityName, string entityId, string performedBy, string? details = null);
    
    

    void LogSync(string syncType, string performedBy, string? details = null);
    
    

    void LogLogin(string username, string? ipAddress = null, string? userAgent = null);
    

    

    void LogPasswordChange(string username, string? ipAddress = null, string? userAgent = null);
    

    
   
    void LogAction(string actionType, string entityName, string? entityId, string performedBy, string? details = null);
}
