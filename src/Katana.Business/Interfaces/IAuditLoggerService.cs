
namespace Katana.Business.Interfaces;


public interface IAuditLoggerService
{

    Task LogAsync(
        string actionType,       
        string entityName,       
        string? entityId,       
        string? performedBy,     
        string? oldValues = null,
        string? newValues = null,
        string? description = null);
        Task LogAsync(string actionType, string entityName, int? entityId, string? details, string? performedBy = null);
        Task<List<Katana.Core.Entities.AuditLog>> GetAllAsync(int page = 1, int pageSize = 50);
        Task<List<Katana.Core.Entities.AuditLog>> GetByEntityAsync(string entityName, int entityId);
   
}
