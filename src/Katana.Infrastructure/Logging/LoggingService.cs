using Katana.Business.Interfaces;
using Katana.Core.Enums;
using Katana.Data.Context;
using Katana.Data.Models;
using Microsoft.Extensions.Logging;

namespace Katana.Infrastructure.Logging;

public class LoggingService : ILoggingService
{
    private readonly ILogger<LoggingService> _logger;
    private readonly IntegrationDbContext? _context;

    public LoggingService(ILogger<LoggingService> logger, IntegrationDbContext? context = null)
    {
        _logger = logger;
        _context = context;
    }

    public void LogInfo(string message, string? user = null, string? contextData = null, LogCategory? category = null)
    {
        _logger.LogInformation("[{Category}] [{User}] {Message} | Context: {Context}", 
            category?.ToString() ?? "System", user ?? "System", message, contextData ?? "N/A");
        TryLogToDatabase("Info", message, null, user, contextData, category);
    }

    public void LogWarning(string message, string? user = null, string? contextData = null, LogCategory? category = null)
    {
        _logger.LogWarning("[{Category}] [{User}] {Message} | Context: {Context}", 
            category?.ToString() ?? "System", user ?? "System", message, contextData ?? "N/A");
        TryLogToDatabase("Warning", message, null, user, contextData, category);
    }

    public void LogError(string message, Exception? ex = null, string? user = null, string? contextData = null, LogCategory? category = null)
    {
        _logger.LogError(ex, "[{Category}] [{User}] {Message} | Context: {Context}", 
            category?.ToString() ?? "System", user ?? "System", message, contextData ?? "N/A");
        TryLogToDatabase("Error", message, ex, user, contextData, category);
    }

    private void TryLogToDatabase(string level, string message, Exception? ex, string? user, string? contextData, LogCategory? category)
    {
        if (_context == null) return;

        try
        {
            _context.ErrorLogs.Add(new ErrorLog
            {
                IntegrationName = "System",
                Message = ex?.Message ?? message,
                StackTrace = ex?.StackTrace,
                CreatedAt = DateTime.UtcNow,
                Level = level,
                Category = category?.ToString(),
                User = user,
                ContextData = contextData
            });
            _context.SaveChanges();
        }
        catch
        {
            // Veritabanı yoksa sadece dosya/konsola yaz
        }
    }
}
