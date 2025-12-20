using Katana.Business.Interfaces;
using Katana.Core.Entities;
using Katana.Data.Context;
using Microsoft.Extensions.Logging;

namespace Katana.Infrastructure.Logging;

public class AuditService : IAuditService
{
    private readonly ILogger<AuditService> _logger;
    private readonly IntegrationDbContext? _context;

    public AuditService(ILogger<AuditService> logger, IntegrationDbContext? context = null)
    {
        _logger = logger;
        _context = context;
    }

    public void LogCreate(string entityName, string entityId, string performedBy, string? details = null)
    {
        _logger.LogInformation("[AUDIT] CREATE {Entity} #{Id} by {User}", entityName, entityId, performedBy);
        TryLogToDatabase("CREATE", entityName, entityId, performedBy, details);
    }

    public void LogUpdate(string entityName, string entityId, string performedBy, string? changes = null, string? details = null)
    {
        _logger.LogInformation("[AUDIT] UPDATE {Entity} #{Id} by {User}", entityName, entityId, performedBy);
        TryLogToDatabase("UPDATE", entityName, entityId, performedBy, details, changes);
    }

    public void LogDelete(string entityName, string entityId, string performedBy, string? details = null)
    {
        _logger.LogInformation("[AUDIT] DELETE {Entity} #{Id} by {User}", entityName, entityId, performedBy);
        TryLogToDatabase("DELETE", entityName, entityId, performedBy, details);
    }

    public void LogSync(string syncType, string performedBy, string? details = null)
    {
        _logger.LogInformation("[AUDIT] SYNC {Type} by {User}", syncType, performedBy);
        TryLogToDatabase("SYNC", syncType, null, performedBy, details);
    }

    public void LogLogin(string username, string? ipAddress = null, string? userAgent = null)
    {
        _logger.LogInformation("[AUDIT] LOGIN {User} from {IP}", username, ipAddress ?? "Unknown");
        TryLogToDatabase("LOGIN", "User", null, username, $"IP: {ipAddress}", null, ipAddress, userAgent);
    }

    public void LogPasswordChange(string username, string? ipAddress = null, string? userAgent = null)
    {
        _logger.LogInformation("[AUDIT] PASSWORD_CHANGE {User} from {IP}", username, ipAddress ?? "Unknown");
        TryLogToDatabase("PASSWORD_CHANGE", "User", null, username, $"IP: {ipAddress}", null, ipAddress, userAgent);
    }

    public void LogAction(string actionType, string entityName, string? entityId, string performedBy, string? details = null)
    {
        _logger.LogInformation("[AUDIT] {Action} {Entity} #{Id} by {User}", actionType, entityName, entityId ?? "N/A", performedBy);
        TryLogToDatabase(actionType, entityName, entityId, performedBy, details);
    }

    private void TryLogToDatabase(string actionType, string entityName, string? entityId, string performedBy, 
        string? details = null, string? changes = null, string? ipAddress = null, string? userAgent = null)
    {
        if (_context == null) return;

        try
        {
            _context.AuditLogs.Add(new AuditLog
            {
                ActionType = actionType,
                EntityName = entityName,
                EntityId = entityId,
                PerformedBy = performedBy,
                Timestamp = DateTime.UtcNow,
                Details = details,
                Changes = changes,
                IpAddress = ipAddress,
                UserAgent = userAgent
            });
            _context.SaveChanges();
        }
        catch
        {
            
        }
    }
}
