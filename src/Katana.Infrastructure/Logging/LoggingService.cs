using Katana.Business.Interfaces;
using Katana.Core.Enums;
using Katana.Data.Context;
using Katana.Data.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Katana.Infrastructure.Logging;

public class LoggingService : ILoggingService
{
    private readonly ILogger<LoggingService> _logger;
    private readonly IntegrationDbContext? _context;
    private readonly IConfiguration _config;
    private readonly bool _persistToDb;

    public LoggingService(ILogger<LoggingService> logger, IConfiguration config, IntegrationDbContext? context = null)
    {
        _logger = logger;
        _config = config;
        _context = context;

        // Yapılandırma anahtarını oku (varsayılan: true)
        // Not: appsettings uses "LoggingOptions" section
        _persistToDb = _config.GetValue<bool>("LoggingOptions:PersistToDatabase", true);
    }

    public void LogInfo(string message, string? user = null, string? contextData = null, LogCategory? category = null)
    {
        // Türkçe log mesajı
        _logger.LogInformation("[{Category}] [{User}] {Message} | Bağlam: {Context}", 
            category?.ToString() ?? "Sistem", user ?? "Sistem", message, contextData ?? "Yok");
        TryLogToDatabase("Info", message, null, user, contextData, category);
    }

    public void LogWarning(string message, string? user = null, string? contextData = null, LogCategory? category = null)
    {
        // Türkçe log mesajı
        _logger.LogWarning("[{Category}] [{User}] {Message} | Bağlam: {Context}", 
            category?.ToString() ?? "Sistem", user ?? "Sistem", message, contextData ?? "Yok");
        TryLogToDatabase("Warning", message, null, user, contextData, category);
    }

    public void LogError(string message, Exception? ex = null, string? user = null, string? contextData = null, LogCategory? category = null)
    {
        // Türkçe log mesajı
        _logger.LogError(ex, "[{Category}] [{User}] {Message} | Bağlam: {Context}", 
            category?.ToString() ?? "Sistem", user ?? "Sistem", message, contextData ?? "Yok");
        TryLogToDatabase("Error", message, ex, user, contextData, category);
    }

    private void TryLogToDatabase(string level, string message, Exception? ex, string? user, string? contextData, LogCategory? category)
    {
        if (!_persistToDb) return; // toggle disabled
        if (_context == null) return;

        // Only persist Warning and Error to DB to reduce volume
        if (!string.Equals(level, "Warning", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(level, "Error", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

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
            // If DB is not available, just continue
        }
    }
}
