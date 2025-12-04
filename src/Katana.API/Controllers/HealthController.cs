using Katana.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Katana.API.Controllers;

[ApiController]
[Route("api/health")] 
[AllowAnonymous] 
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;
    private readonly IKatanaService _katanaService;
    private readonly ILucaService _lucaService;

    public HealthController(
        ILogger<HealthController> logger,
        IKatanaService katanaService,
        ILucaService lucaService)
    {
        _logger = logger;
        _katanaService = katanaService;
        _lucaService = lucaService;
    }

    /// <summary>
    /// Genel API sağlık durumu
    /// </summary>
    [HttpGet] 
    public IActionResult GetHealth()
    {
        _logger.LogDebug("API Health check");
        
        return Ok(new 
        {
            status = "Healthy",
            service = "Katana.API",
            checkedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Katana API bağlantı durumu
    /// </summary>
    [HttpGet("katana")]
    public async Task<IActionResult> CheckKatanaHealth()
    {
        var result = new ExternalServiceHealthResult { Service = "Katana API" };
        
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var isHealthy = await _katanaService.TestConnectionAsync();
            sw.Stop();

            result.IsHealthy = isHealthy;
            result.ResponseTimeMs = sw.ElapsedMilliseconds;
            result.Message = isHealthy ? "Connection successful" : "Connection failed";
            result.CheckedAt = DateTime.UtcNow;

            if (!isHealthy)
            {
                _logger.LogWarning("Katana API health check failed");
                return StatusCode(503, result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Katana API health check error");
            result.IsHealthy = false;
            result.Message = $"Error: {ex.Message}";
            result.CheckedAt = DateTime.UtcNow;
            return StatusCode(503, result);
        }
    }

    /// <summary>
    /// Luca API bağlantı durumu
    /// </summary>
    [HttpGet("luca")]
    public async Task<IActionResult> CheckLucaHealth()
    {
        var result = new ExternalServiceHealthResult { Service = "Luca API" };
        
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var isHealthy = await _lucaService.TestConnectionAsync();
            sw.Stop();

            result.IsHealthy = isHealthy;
            result.ResponseTimeMs = sw.ElapsedMilliseconds;
            result.Message = isHealthy ? "Connection successful" : "Connection failed";
            result.CheckedAt = DateTime.UtcNow;

            if (!isHealthy)
            {
                _logger.LogWarning("Luca API health check failed");
                return StatusCode(503, result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Luca API health check error");
            result.IsHealthy = false;
            result.Message = $"Error: {ex.Message}";
            result.CheckedAt = DateTime.UtcNow;
            return StatusCode(503, result);
        }
    }

    /// <summary>
    /// Tüm servislerin durumu
    /// </summary>
    [HttpGet("all")]
    public async Task<IActionResult> CheckAllServices()
    {
        var katanaTask = CheckServiceHealthAsync("Katana", () => _katanaService.TestConnectionAsync());
        var lucaTask = CheckServiceHealthAsync("Luca", () => _lucaService.TestConnectionAsync());

        await Task.WhenAll(katanaTask, lucaTask);

        var results = new
        {
            status = katanaTask.Result.IsHealthy && lucaTask.Result.IsHealthy ? "Healthy" : "Degraded",
            checkedAt = DateTime.UtcNow,
            services = new[] { katanaTask.Result, lucaTask.Result }
        };

        var statusCode = results.status == "Healthy" ? 200 : 503;
        return StatusCode(statusCode, results);
    }

    private async Task<ExternalServiceHealthResult> CheckServiceHealthAsync(string serviceName, Func<Task<bool>> healthCheck)
    {
        var result = new ExternalServiceHealthResult { Service = serviceName };
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            result.IsHealthy = await healthCheck();
            sw.Stop();
            result.ResponseTimeMs = sw.ElapsedMilliseconds;
            result.Message = result.IsHealthy ? "OK" : "Failed";
        }
        catch (Exception ex)
        {
            result.IsHealthy = false;
            result.Message = ex.Message;
        }
        result.CheckedAt = DateTime.UtcNow;
        return result;
    }
}

public class ExternalServiceHealthResult
{
    public string Service { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public string Message { get; set; } = string.Empty;
    public long ResponseTimeMs { get; set; }
    public DateTime CheckedAt { get; set; }
}

/// <summary>
/// Circuit Breaker durumu için ek controller
/// </summary>
[ApiController]
[Route("api/health/circuit")]
[AllowAnonymous]
public class CircuitBreakerHealthController : ControllerBase
{
    /// <summary>
    /// Circuit Breaker durumlarını döndür
    /// </summary>
    [HttpGet]
    public IActionResult GetCircuitStates()
    {
        var lucaState = Katana.Business.UseCases.Sync.OrderInvoiceSyncService.LucaCircuitState;
        
        return Ok(new
        {
            checkedAt = DateTime.UtcNow,
            circuits = new
            {
                luca = new
                {
                    state = lucaState.ToString(),
                    isOpen = lucaState == Polly.CircuitBreaker.CircuitState.Open,
                    description = GetCircuitDescription(lucaState)
                }
            }
        });
    }

    private static string GetCircuitDescription(Polly.CircuitBreaker.CircuitState state) => state switch
    {
        Polly.CircuitBreaker.CircuitState.Closed => "Normal - API çalışıyor",
        Polly.CircuitBreaker.CircuitState.Open => "Açık - API erişilemez, istekler engelleniyor",
        Polly.CircuitBreaker.CircuitState.HalfOpen => "Yarı Açık - API test ediliyor",
        Polly.CircuitBreaker.CircuitState.Isolated => "İzole - Manuel olarak devre dışı",
        _ => "Bilinmiyor"
    };
}
