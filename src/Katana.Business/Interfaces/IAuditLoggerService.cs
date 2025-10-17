
using Katana.Core.Entities;

namespace Katana.Business.Interfaces;

/// <summary>
/// Sistemde yapılan önemli işlemleri (Create, Update, Delete) kayıt altına almak için kullanılır.
/// </summary>
public interface IAuditLoggerService
{
    /// <summary>
    /// Belirli bir kullanıcı tarafından yapılan işlemi kaydeder.
    /// </summary>
    Task LogAsync(
        string actionType,       // "CREATE", "UPDATE", "DELETE" gibi
        string entityName,       // "Mapping", "Invoice", "Product" vb.
        string? entityId,        // Etkilenen kaydın ID’si
        string? performedBy,     // İşlemi yapan kişi veya sistem (örn. "API", "SyncJob")
        string? oldValues = null,
        string? newValues = null,
        string? description = null);
        Task LogAsync(string actionType, string entityName, int? entityId, string? details, string? performedBy = null);
        Task<List<Katana.Data.Models.AuditLog>> GetAllAsync(int page = 1, int pageSize = 50);
        Task<List<Katana.Data.Models.AuditLog>> GetByEntityAsync(string entityName, int entityId);
   
}
