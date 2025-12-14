using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace Katana.Infrastructure.Utils;
public class RateLimitHandler : DelegatingHandler
{
    private readonly ILogger<RateLimitHandler> _logger;
    private const int DefaultRetrySeconds = 1;
    private const int MaxRetryAttempts = 3;

    public RateLimitHandler(ILogger<RateLimitHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // 🔥 Request gönderilmeden ÖNCE Content-Type'ı kontrol et ve charset'i kaldır
        if (request.Content?.Headers.ContentType != null)
        {
            var contentType = request.Content.Headers.ContentType;
            
            // Charset varsa KALDIR
            if (!string.IsNullOrEmpty(contentType.CharSet))
            {
                _logger.LogDebug("Removing charset from Content-Type: {ContentType}", contentType);
                contentType.CharSet = null;
            }
        }
        
        string? bufferedContent = null;
        if (request.Content != null)
        {
            bufferedContent = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        for (var attempt = 1; attempt <= MaxRetryAttempts; attempt++)
        {
            var req = CloneRequest(request, bufferedContent);
            
            // Clone'da da charset'i temizle
            if (req.Content?.Headers.ContentType != null)
            {
                req.Content.Headers.ContentType.CharSet = null;
            }

            var response = await base.SendAsync(req, cancellationToken);
            LogRateHeaders(response);

            if (response.StatusCode != System.Net.HttpStatusCode.TooManyRequests || attempt == MaxRetryAttempts)
            {
                return response;
            }

            var retrySeconds = GetRetryAfterSeconds(response) ?? DefaultRetrySeconds;
            _logger.LogWarning("429 Too Many Requests received. Waiting {Seconds}s before retry {Attempt}/{Max}.", retrySeconds, attempt + 1, MaxRetryAttempts);

            await Task.Delay(TimeSpan.FromSeconds(retrySeconds), cancellationToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private static HttpRequestMessage CloneRequest(HttpRequestMessage request, string? bufferedContent)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version
        };

        // 🔥 ByteArrayContent kullan - StringContent charset ekliyor!
        if (bufferedContent != null)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(bufferedContent);
            clone.Content = new ByteArrayContent(bytes);
            
            // Content-Type'ı charset OLMADAN ayarla
            var mediaType = request.Content?.Headers.ContentType?.MediaType ?? "application/json";
            clone.Content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        }

        // Request headers'ı kopyala
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Content headers'ı kopyala (Content-Type hariç - zaten ayarladık)
        if (request.Content != null && clone.Content != null)
        {
            foreach (var header in request.Content.Headers)
            {
                // Content-Type'ı atlayalım çünkü zaten charset olmadan ayarladık
                if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                    continue;
                    
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }

    private double? GetRetryAfterSeconds(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Retry-After", out var retryValues))
        {
            var retryValue = retryValues.FirstOrDefault();
            if (double.TryParse(retryValue, out var seconds))
                return seconds;

            if (DateTimeOffset.TryParse(retryValue, out var retryDate))
                return Math.Max(0, (retryDate - DateTimeOffset.UtcNow).TotalSeconds);
        }

        return null;
    }

    private void LogRateHeaders(HttpResponseMessage response)
    {
        var limit = GetHeader(response.Headers, "X-RateLimit-Limit");
        var remaining = GetHeader(response.Headers, "X-RateLimit-Remaining");
        var reset = GetHeader(response.Headers, "X-RateLimit-Reset");

        if (limit != null || remaining != null || reset != null)
        {
            _logger.LogDebug("RateLimit headers: limit={Limit}, remaining={Remaining}, reset={Reset}", limit ?? "?", remaining ?? "?", reset ?? "?");
        }
    }

    private static string? GetHeader(HttpResponseHeaders headers, string name)
    {
        return headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;
    }
}
