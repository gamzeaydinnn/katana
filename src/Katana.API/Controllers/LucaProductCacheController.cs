using Microsoft.AspNetCore.Mvc;
using Katana.Business.Services;
using System.Threading.Tasks;

namespace Katana.API.Controllers;

[ApiController]
[Route("api/luca/cache")]
public class LucaProductCacheController : ControllerBase
{
    private readonly LucaProductCacheService _cache;

    public LucaProductCacheController(LucaProductCacheService cache)
    {
        _cache = cache;
    }

    [HttpPost("sync-products")]
    public async Task<IActionResult> SyncProducts()
    {
        var count = await _cache.RefreshProductsFromKozaAsync();
        return Ok(new { success = true, updated = count });
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetCachedProducts()
    {
        var result = await _cache.GetAllCachedAsync();
        return Ok(result);
    }
}
