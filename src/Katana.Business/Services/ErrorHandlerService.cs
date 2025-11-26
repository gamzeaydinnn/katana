




using Katana.Business.Interfaces;
using Katana.Core.Enums;
using Katana.Data.Context;
using Katana.Data.Models;
using Microsoft.Extensions.Logging;

namespace Katana.Business.Services;

public class ErrorHandlerService : IErrorHandler
{
    private readonly IntegrationDbContext _context;
    private readonly ILogger<ErrorHandlerService> _logger;

    public ErrorHandlerService(IntegrationDbContext context, ILogger<ErrorHandlerService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<T?> ExecuteWithHandlingAsync<T>(Func<Task<T>> operation, string operationName)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            var errorType = ClassifyError(ex);

            _logger.LogError(ex, "Error in operation {OperationName}", operationName);
            _context.ErrorLogs.Add(new ErrorLog
            {
                IntegrationName = "Katana-Luca",
                Message = ex.Message,
                StackTrace = ex.StackTrace,
                Operation = operationName,
                ErrorType = errorType,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return default;
        }
    }

    public async Task ExecuteWithHandlingAsync(Func<Task> operation, string operationName)
    {
        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            var errorType = ClassifyError(ex);

            _logger.LogError(ex, "Error in operation {OperationName}", operationName);
            _context.ErrorLogs.Add(new ErrorLog
            {
                IntegrationName = "Katana-Luca",
                Message = ex.Message,
                StackTrace = ex.StackTrace,
                Operation = operationName,
                ErrorType = errorType,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }
    }

    
    
    
    private ErrorType ClassifyError(Exception ex)
    {
        var msg = ex.Message.ToLowerInvariant();

        if (msg.Contains("timeout"))
            return ErrorType.Database;
        if (msg.Contains("unauthorized") || msg.Contains("forbidden"))
            return ErrorType.Security;
        if (msg.Contains("validation") || msg.Contains("invalid"))
            return ErrorType.Validation;
        if (msg.Contains("api") || msg.Contains("http"))
            return ErrorType.Api;
        if (msg.Contains("sync") || msg.Contains("etl"))
            return ErrorType.Sync;

        return ErrorType.Unknown;
    }
}
