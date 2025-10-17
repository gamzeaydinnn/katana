/*ErrorHandlerService (Application) — hataların business-level ele alınması; mevcut hata middleware’i bunu kullanmalı.*/
using Katana.Business.Interfaces;
using Katana.Data.Context;
using Katana.Data.Models;
using Microsoft.EntityFrameworkCore;
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
            _logger.LogError(ex, "Error in operation {OperationName}", operationName);
            _context.ErrorLogs.Add(new ErrorLog
            {
                Message = ex.Message,
                StackTrace = ex.StackTrace,
                Operation = operationName,
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
            _logger.LogError(ex, "Error in operation {OperationName}", operationName);
            _context.ErrorLogs.Add(new ErrorLog
            {
                Message = ex.Message,
                StackTrace = ex.StackTrace,
                Operation = operationName,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
    }
}
