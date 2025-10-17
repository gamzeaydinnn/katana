/*AuditLoggerService (Application) — audit kayıtlarının oluşturulması ve 
DB’ye yazılması (senin IAuditLogger var ama implementasyon yok).*/
using Katana.Business.Interfaces;
using Katana.Data.Context;
using Katana.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Katana.Business.Services
{
    public class AuditLoggerService : IAuditLoggerService
    {
        private readonly IntegrationDbContext _context;
        private readonly ILogger<AuditLoggerService> _logger;

        public AuditLoggerService(IntegrationDbContext context, ILogger<AuditLoggerService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Veritabanına audit kaydı ekler.
        /// </summary>
        public async Task LogAsync(string actionType, string entityName, int? entityId, string? details, string? performedBy = null)
        {
            try
            {
                var audit = new AuditLog
                {
                    ActionType = actionType,
                    EntityName = entityName,
                    EntityId = entityId,
                    Details = details,
                    PerformedBy = performedBy ?? "System",
                    CreatedAt = DateTime.UtcNow
                };

                _context.AuditLogs.Add(audit);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Audit log created: {ActionType} {EntityName}#{EntityId} by {User}", 
                    actionType, entityName, entityId, performedBy ?? "System");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing audit log for {EntityName}#{EntityId}", entityName, entityId);
            }
        }

        /// <summary>
        /// Audit loglarını getirir.
        /// </summary>
        public async Task<List<AuditLog>> GetAllAsync(int page = 1, int pageSize = 50)
        {
            return await _context.AuditLogs
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Belirli bir entity’e ait logları getirir.
        /// </summary>
        public async Task<List<AuditLog>> GetByEntityAsync(string entityName, int entityId)
        {
            return await _context.AuditLogs
                .Where(a => a.EntityName == entityName && a.EntityId == entityId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public Task LogAsync(string actionType, string entityName, string? entityId, string? performedBy, string? oldValues = null, string? newValues = null, string? description = null)
        {
            throw new NotImplementedException();
        }
    }
}
