using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Katana.API.Controllers
{
    [ApiController]
    [Route("api/luca")]
    public class LucaCompatibilityController : ControllerBase
    {
        private readonly ISyncService _syncService;

        public LucaCompatibilityController(ISyncService syncService)
        {
            _syncService = syncService;
        }

        // Compatibility endpoint for legacy clients/scripts that call /api/luca/sync-products
        [HttpPost("sync-products")]
        public async Task<IActionResult> SyncProductsCompat([FromBody] SyncOptionsDto? options)
        {
            try
            {
                var opts = options ?? new SyncOptionsDto();
                var result = await _syncService.SyncProductsToLucaAsync(null, opts);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Server error while syncing products to Luca (compat)", error = ex.Message });
            }
        }
    }
}
