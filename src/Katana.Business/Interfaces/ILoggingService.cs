using Katana.Core.Enums;

namespace Katana.Business.Interfaces;
//Amaç: Servisler veya işlemler, doğrudan Serilog ya da ILogger kullanmasın; onun yerine bir arayüzden geçsin.
public interface ILoggingService
{
    void LogInfo(string message, string? user = null, string? contextData = null, LogCategory? category = null);
    void LogWarning(string message, string? user = null, string? contextData = null, LogCategory? category = null);
    void LogError(string message, Exception? ex = null, string? user = null, string? contextData = null, LogCategory? category = null);
    Task LogAuditAsync(string user, string action, string entityType, string entityId, string? oldValue = null, string? newValue = null);
}
