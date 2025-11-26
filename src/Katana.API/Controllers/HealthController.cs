
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Katana.API.Controllers;

[ApiController]
[Route("api")] 
[AllowAnonymous] 
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    [HttpGet("health")] 
    public IActionResult GetHealth()
    {
        
        _logger.LogInformation("API Health check successful.");
        
        return Ok(new 
        {
            status = "Healthy",
            checkedAt = DateTime.UtcNow
        });
    }
}
