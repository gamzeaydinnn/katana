// Katana.API/Controllers/HealthController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Katana.API.Controllers;

[ApiController]
[Route("api")] // Route'u 'api' olarak bırakalım ki, sadece /api/Health adresinden erişilebilsin.
[AllowAnonymous] // Token olmadan erişilebilir
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    [HttpGet("Health")] // API adresi: /api/Health
    public IActionResult GetHealth()
    {
        // Basitçe API'nizin çalıştığını gösterir. DB veya harici bağlantı kontrolü burada yapılmaz.
        _logger.LogInformation("API Health check successful.");
        
        return Ok(new 
        {
            status = "Healthy",
            checkedAt = DateTime.UtcNow
        });
    }
}