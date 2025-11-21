using System.Net.Http;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Katana.Infrastructure.Logging;

/// <summary>
/// DelegatingHandler that logs raw HTTP requests/responses (headers, cookies, bodies) for diagnostics.
/// Yazılan loglar repo kökündeki logs klasörüne düşer; uzun yanıtlar kesilir.
/// </summary>
public class HttpDebugLoggingHandler : DelegatingHandler
{
    private readonly ILogger<HttpDebugLoggingHandler> _logger;
    private readonly string _logFilePath;

    public HttpDebugLoggingHandler(ILogger<HttpDebugLoggingHandler> logger)
    {
        _logger = logger;

        var baseDir = Directory.GetCurrentDirectory();
        var logsDir = Path.Combine(baseDir, "logs");
        Directory.CreateDirectory(logsDir);

        _logFilePath = Path.Combine(logsDir, $"http-traffic-{DateTime.UtcNow:yyyyMMdd-HHmmss}.log");
        _logger.LogInformation("HTTP traffic will be logged to: {LogFile}", _logFilePath);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var requestId = Guid.NewGuid().ToString("N")[..8];
        var logEntry = new StringBuilder();

        logEntry.AppendLine($"\n{'=',-80}");
        logEntry.AppendLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] REQUEST {requestId}");
        logEntry.AppendLine($"{'=',-80}");

        await LogRequestAsync(request, logEntry);

        var startTime = DateTime.UtcNow;
        HttpResponseMessage? response = null;
        Exception? exception = null;

        try
        {
            response = await base.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            exception = ex;
            throw;
        }
        finally
        {
            var duration = DateTime.UtcNow - startTime;

            if (response != null)
            {
                await LogResponseAsync(response, logEntry, duration);
            }
            else if (exception != null)
            {
                LogException(exception, logEntry, duration);
            }

            await File.AppendAllTextAsync(_logFilePath, logEntry.ToString());
            _logger.LogDebug(logEntry.ToString());
        }

        return response!;
    }

    private async Task LogRequestAsync(HttpRequestMessage request, StringBuilder log)
    {
        log.AppendLine($"{request.Method} {request.RequestUri}");
        log.AppendLine();

        log.AppendLine("REQUEST HEADERS:");
        foreach (var header in request.Headers)
        {
            log.AppendLine($"  {header.Key}: {string.Join(", ", header.Value)}");
        }
        if (request.Content != null)
        {
            foreach (var header in request.Content.Headers)
            {
                log.AppendLine($"  {header.Key}: {string.Join(", ", header.Value)}");
            }
        }
        log.AppendLine();

        if (request.Headers.TryGetValues("Cookie", out var cookies))
        {
            log.AppendLine("COOKIES:");
            foreach (var cookie in cookies)
            {
                log.AppendLine($"  {cookie}");
            }
            log.AppendLine();
        }

        if (request.Content != null)
        {
            log.AppendLine("REQUEST BODY:");
            var body = await ReadContentSafelyAsync(request.Content);
            log.AppendLine(body);
            log.AppendLine();
        }
    }

    private async Task LogResponseAsync(HttpResponseMessage response, StringBuilder log, TimeSpan duration)
    {
        log.AppendLine($"\n{'-',-80}");
        log.AppendLine($"RESPONSE (Duration: {duration.TotalMilliseconds:F2}ms)");
        log.AppendLine($"{'-',-80}");
        log.AppendLine($"HTTP/{response.Version} {(int)response.StatusCode} {response.ReasonPhrase}");
        log.AppendLine();

        log.AppendLine("RESPONSE HEADERS:");
        foreach (var header in response.Headers)
        {
            log.AppendLine($"  {header.Key}: {string.Join(", ", header.Value)}");
        }
        if (response.Content != null)
        {
            foreach (var header in response.Content.Headers)
            {
                log.AppendLine($"  {header.Key}: {string.Join(", ", header.Value)}");
            }
        }
        log.AppendLine();

        if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            log.AppendLine("SET-COOKIE:");
            foreach (var cookie in setCookies)
            {
                log.AppendLine($"  {cookie}");
            }
            log.AppendLine();
        }

        if (response.Content != null)
        {
            log.AppendLine("RESPONSE BODY (UTF-8):");
            var bodyUtf8 = await ReadContentSafelyAsync(response.Content);
            log.AppendLine(bodyUtf8);
            log.AppendLine();

            if (ContainsTurkishCharacters(bodyUtf8) || bodyUtf8.Contains("Ã") || bodyUtf8.Contains("Ä"))
            {
                try
                {
                    var bytes = await response.Content.ReadAsByteArrayAsync();
                    var body1254 = Encoding.GetEncoding(1254).GetString(bytes);
                    if (body1254 != bodyUtf8)
                    {
                        log.AppendLine("RESPONSE BODY (Windows-1254):");
                        log.AppendLine(body1254);
                        log.AppendLine();
                    }
                }
                catch (Exception ex)
                {
                    log.AppendLine($"[Failed to decode as Windows-1254: {ex.Message}]");
                }
            }
        }
    }

    private void LogException(Exception exception, StringBuilder log, TimeSpan duration)
    {
        log.AppendLine($"\n{'-',-80}");
        log.AppendLine($"EXCEPTION (Duration: {duration.TotalMilliseconds:F2}ms)");
        log.AppendLine($"{'-',-80}");
        log.AppendLine(exception.ToString());
        log.AppendLine();
    }

    private async Task<string> ReadContentSafelyAsync(HttpContent content)
    {
        try
        {
            var body = await content.ReadAsStringAsync();

            if (IsJson(content))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(body);
                    return System.Text.Json.JsonSerializer.Serialize(doc.RootElement,
                        new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                }
                catch { }
            }

            const int maxLength = 10000;
            if (body.Length > maxLength)
            {
                return body.Substring(0, maxLength) + $"\n... [TRUNCATED, total length: {body.Length}]";
            }

            return body;
        }
        catch (Exception ex)
        {
            return $"[Failed to read content: {ex.Message}]";
        }
    }

    private bool IsJson(HttpContent content)
    {
        var contentType = content.Headers.ContentType?.MediaType;
        return contentType?.Contains("json", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    private bool ContainsTurkishCharacters(string text)
    {
        char[] turkishChars = { 'ğ', 'Ğ', 'ü', 'Ü', 'ş', 'Ş', 'ı', 'İ', 'ö', 'Ö', 'ç', 'Ç' };
        return turkishChars.Any(text.Contains);
    }
}

public static class HttpDebugLoggingExtensions
{
    public static IHttpClientBuilder AddHttpDebugLogging(this IHttpClientBuilder builder)
    {
        return builder.AddHttpMessageHandler<HttpDebugLoggingHandler>();
    }
}
