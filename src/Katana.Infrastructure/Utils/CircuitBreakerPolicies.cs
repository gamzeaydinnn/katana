using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;

namespace Katana.Infrastructure.Utils;

/// <summary>
/// Merkezi Circuit Breaker policy'leri.
/// Luca/Katana API down olduğunda cascade failure'ı önler.
/// </summary>
public static class CircuitBreakerPolicies
{
    private static ILogger? _logger;
    
    /// <summary>
    /// Luca API için Circuit Breaker
    /// 5 ardışık hata sonrası 2 dakika devre kesilir
    /// </summary>
    public static AsyncCircuitBreakerPolicy LucaCircuitBreaker { get; } = Policy
        .Handle<HttpRequestException>()
        .Or<TimeoutException>()
        .Or<TaskCanceledException>()
        .CircuitBreakerAsync(
            exceptionsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromMinutes(2),
            onBreak: (exception, duration) =>
            {
                _logger?.LogWarning(
                    "Luca Circuit OPEN - API down. Breaking for {Duration}. Error: {Error}",
                    duration, exception.Message);
            },
            onReset: () =>
            {
                _logger?.LogInformation("Luca Circuit CLOSED - API recovered");
            },
            onHalfOpen: () =>
            {
                _logger?.LogInformation("Luca Circuit HALF-OPEN - Testing API...");
            });

    /// <summary>
    /// Katana API için Circuit Breaker
    /// 5 ardışık hata sonrası 2 dakika devre kesilir
    /// </summary>
    public static AsyncCircuitBreakerPolicy KatanaCircuitBreaker { get; } = Policy
        .Handle<HttpRequestException>()
        .Or<TimeoutException>()
        .Or<TaskCanceledException>()
        .CircuitBreakerAsync(
            exceptionsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromMinutes(2),
            onBreak: (exception, duration) =>
            {
                _logger?.LogWarning(
                    "Katana Circuit OPEN - API down. Breaking for {Duration}. Error: {Error}",
                    duration, exception.Message);
            },
            onReset: () =>
            {
                _logger?.LogInformation("Katana Circuit CLOSED - API recovered");
            },
            onHalfOpen: () =>
            {
                _logger?.LogInformation("Katana Circuit HALF-OPEN - Testing API...");
            });

    /// <summary>
    /// Logger'ı ayarla (Program.cs'de çağrılmalı)
    /// </summary>
    public static void ConfigureLogger(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Circuit durumunu kontrol et
    /// </summary>
    public static CircuitState GetLucaCircuitState() => LucaCircuitBreaker.CircuitState;
    public static CircuitState GetKatanaCircuitState() => KatanaCircuitBreaker.CircuitState;

    /// <summary>
    /// Circuit durumlarını döndür (health check için)
    /// </summary>
    public static object GetCircuitStates() => new
    {
        Luca = new
        {
            State = LucaCircuitBreaker.CircuitState.ToString(),
            IsOpen = LucaCircuitBreaker.CircuitState == CircuitState.Open
        },
        Katana = new
        {
            State = KatanaCircuitBreaker.CircuitState.ToString(),
            IsOpen = KatanaCircuitBreaker.CircuitState == CircuitState.Open
        }
    };
}
