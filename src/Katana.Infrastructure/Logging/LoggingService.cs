using Katana.Business.Interfaces;
using Katana.Data.Context;
using Katana.Data.Models;
using Microsoft.Extensions.Logging;

namespace Katana.Infrastructure.Logging;

public class LoggingService : ILoggingService
{
    private readonly ILogger<LoggingService> _logger;
    private readonly IntegrationDbContext _context;

    public LoggingService(ILogger<LoggingService> logger, IntegrationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public void LogInfo(string message)
    {
        _logger.LogInformation(message);
    }

    public void LogWarning(string message)
    {
        _logger.LogWarning(message);
    }

    public void LogError(string message, Exception? ex = null)
    {
        _logger.LogError(ex, message);

        // Veritabanına da yazalım
        var errorLog = new ErrorLog
        {
            IntegrationName = "System",
            Message = ex?.Message ?? message,
            StackTrace = ex?.StackTrace,
            CreatedAt = DateTime.UtcNow
        };

        _context.ErrorLogs.Add(errorLog);
        _context.SaveChanges();
    }
}
