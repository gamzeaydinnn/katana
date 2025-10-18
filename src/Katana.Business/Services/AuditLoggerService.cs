/* AuditLoggerService (Application)
 * Uygulama içinde yapılan önemli işlemleri (CREATE, UPDATE, DELETE) kaydeder.
 * Veriler AuditLog tablosuna yazılır. Servis; Sync, Mapping, Admin veya API işlemlerinde izleme sağlar.
 */

using Katana.Business.Interfaces;
using Katana.Core.Entities;
using Katana.Data.Context;
using Katana.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Katana.Business.Services;

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
    /// Veritabanına temel audit kaydı ekler (integer ID üzerinden).
    /// </summary>
    public async Task LogAsync(
        string actionType,
        string entityName,
        int? entityId,
        string? details,
        string? performedBy = null)
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
                CreatedAt = DateTime.UtcNow,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(audit);
            await _context.SaveChangesAsync();

            _logger.LogInformation("📝 Audit log created: {ActionType} {EntityName}#{EntityId} by {User}",
                actionType, entityName, entityId, performedBy ?? "System");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error writing audit log for {EntityName}#{EntityId}", entityName, entityId);
        }
    }

    /// <summary>
    /// Değişiklik öncesi/sonrası bilgilerini içeren gelişmiş audit kaydı oluşturur.
    /// </summary>
    public async Task LogAsync(
        string actionType,
        string entityName,
        string? entityId,
        string? performedBy,
        string? oldValues = null,
        string? newValues = null,
        string? description = null)
    {
        try
        {
            var audit = new AuditLog
            {
                ActionType = actionType,
                EntityName = entityName,
                EntityId = int.TryParse(entityId, out var id) ? id : null,
                Details = description ?? $"Change detected in {entityName}",
                PerformedBy = performedBy ?? "System",
                Changes = BuildChangeSummary(oldValues, newValues),
                CreatedAt = DateTime.UtcNow,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(audit);
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ Detailed audit created for {EntityName}#{EntityId} ({ActionType})",
                entityName, entityId, actionType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating detailed audit log for {EntityName}", entityName);
        }
    }

    /// <summary>
    /// Tüm audit kayıtlarını sayfalama ile getirir.
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
    /// Belirli bir entity'e ait audit kayıtlarını döndürür.
    /// </summary>
    public async Task<List<AuditLog>> GetByEntityAsync(string entityName, int entityId)
    {
        return await _context.AuditLogs
            .Where(a => a.EntityName == entityName && a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Eski ve yeni değerleri özet bir metne dönüştürür (opsiyonel).
    /// </summary>
    private string? BuildChangeSummary(string? oldValues, string? newValues)
    {
        if (string.IsNullOrEmpty(oldValues) && string.IsNullOrEmpty(newValues))
            return null;

        return $"Old: {Truncate(oldValues)} | New: {Truncate(newValues)}";
    }

    /// <summary>
    /// Uzun değerleri keser (DB aşımı olmaması için).
    /// </summary>
    private string Truncate(string? value, int maxLength = 500)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Length > maxLength ? value.Substring(0, maxLength) + "..." : value;
    }
}
