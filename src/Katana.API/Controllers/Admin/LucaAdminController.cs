using Microsoft.AspNetCore.Mvc;
using Katana.Business.Services;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Katana.API.Controllers.Admin;

[ApiController]
public class LucaAdminController : ControllerBase
{
    private readonly LucaProductCacheService _cache;
    private readonly IntegrationDbContext _db;

    public LucaAdminController(LucaProductCacheService cache, IntegrationDbContext db)
    {
        _cache = cache;
        _db = db;
    }

    [HttpPost("api/adminpanel/luca/sync-products")]
    public async Task<IActionResult> SyncProducts()
    {
        var count = await _cache.RefreshProductsFromKozaAsync();
        return Ok(new { success = true, updated = count });
    }

    [HttpGet("api/adminpanel/luca/products")]
    public async Task<IActionResult> GetCachedProducts()
    {
        var result = await _cache.GetAllCachedAsync();
        return Ok(result);
    }

    [HttpGet("api/adminpanel/luca/mapping/products")]
    public async Task<IActionResult> GetProductMappingCandidates()
    {
        var katana = await _db.Products
            .Select(p => new { p.Id, SKU = p.SKU, Name = p.Name })
            .ToListAsync();

        var luca = await _db.LucaProducts
            .Select(p => new { p.Id, Code = p.LucaCode, Name = p.LucaName })
            .ToListAsync();

        return Ok(new { katanaProducts = katana, lucaProducts = luca });
    }
}
