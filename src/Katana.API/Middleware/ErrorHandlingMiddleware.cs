using System.Net;
using System.Text.Json;
using Katana.Business.Interfaces;

namespace Katana.API.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
            catch (Exception ex)
        {
            var user = context.User?.Identity?.Name ?? "Anonymous";
            var path = context.Request.Path;
            _logger.LogError(ex, "Unhandled exception at {Path}", path);
            try
            {
                // Resolve the scoped logging service from the request services to avoid capturing scoped services in middleware ctor
                var loggingService = context.RequestServices.GetService(typeof(Katana.Business.Interfaces.ILoggingService)) as Katana.Business.Interfaces.ILoggingService;
                loggingService?.LogError($"Unhandled exception at {path}", ex, user, $"Method: {context.Request.Method}");
            }
            catch
            {
                // ignore failures during error reporting to avoid secondary exceptions
            }
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Response başlamışsa header set edilemez
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.ContentType = "application/json";
        
        var response = new ErrorResponse
        {
            Message = "An error occurred while processing your request.",
            Details = exception.Message
        };

        switch (exception)
        {
            case ArgumentNullException:
            case ArgumentException:
                response.Message = "Invalid request parameters.";
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;
            
            case UnauthorizedAccessException:
                response.Message = "Unauthorized access.";
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                break;
            
            case KeyNotFoundException:
                response.Message = "The requested resource was not found.";
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                break;
            
            case InvalidOperationException:
                response.Message = "The operation is not valid in the current state.";
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                break;
            
            case TimeoutException:
                response.Message = "The request timed out.";
                context.Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                break;
            
            default:
                response.Message = "An internal server error occurred.";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                break;
        }

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

