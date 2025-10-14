using System.Net;
using System.Text.Json;

namespace Katana.API.Middleware;

public class AuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthMiddleware> _logger;
    private readonly IConfiguration _configuration;
    private const string ApiKeyHeaderName = "X-API-Key";

    public AuthMiddleware(RequestDelegate next, ILogger<AuthMiddleware> logger, IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip auth for health checks and swagger
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/swagger"))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            await HandleUnauthorizedAsync(context, "API Key missing");
            return;
        }

        var apiKey = _configuration["ApiSettings:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("API Key not configured in settings");
            await _next(context);
            return;
        }

        if (!apiKey.Equals(extractedApiKey))
        {
            await HandleUnauthorizedAsync(context, "Invalid API Key");
            return;
        }

        await _next(context);
    }

    private static async Task HandleUnauthorizedAsync(HttpContext context, string message)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        context.Response.ContentType = "application/json";

        var response = new
        {
            Message = "Unauthorized",
            Details = message,
            Timestamp = DateTime.UtcNow
        };

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}

