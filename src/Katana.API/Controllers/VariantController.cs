using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Katana.API.Controllers;

/// <summary>
/// Varyant gruplama API endpoints
/// </summary>
[Authorize]
[ApiController]
[Route("api/variants")]
public class VariantController : ControllerBase
{
    private readonly IVariantGroupingService _variantService;
    private readonly ILogger<VariantController> _logger;

    public VariantController(
        IVariantGroupingService variantService,
        ILogger<VariantController> logger)
    {
        _variantService = variantService;
        _logger = logger;
    }

    /// <summary>
    /// Tüm varyantları ana ürün altında gruplandırılmış olarak getirir
    /// </summary>
    [HttpGet("grouped")]
    public async Task<ActionResult<List<ProductVariantGroup>>> GetGroupedVariants()
    {
        try
        {
            var groups = await _variantService.GroupVariantsByProductAsync();
            return Ok(groups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting grouped variants");
            return StatusCode(500, new { message = "Varyant gruplama hatası", error = ex.Message });
        }
    }

    /// <summary>
    /// Orphan (ana ürünsüz) varyantları getirir
    /// </summary>
    [HttpGet("orphans")]
    public async Task<ActionResult<List<VariantDetail>>> GetOrphanVariants()
    {
        try
        {
            var orphans = await _variantService.GetOrphanVariantsAsync();
            return Ok(orphans);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orphan variants");
            return StatusCode(500, new { message = "Orphan varyant getirme hatası", error = ex.Message });
        }
    }

    /// <summary>
    /// Belirli bir ürünün varyantlarını getirir
    /// </summary>
    [HttpGet("product/{productId}")]
    public async Task<ActionResult<ProductVariantGroup>> GetProductVariants(long productId)
    {
        try
        {
            var group = await _variantService.GetVariantGroupAsync(productId);
            
            if (group == null)
            {
                return NotFound(new { message = $"Ürün bulunamadı: {productId}" });
            }
            
            return Ok(group);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting variants for product {ProductId}", productId);
            return StatusCode(500, new { message = "Varyant getirme hatası", error = ex.Message });
        }
    }

    /// <summary>
    /// Varyant detayını getirir
    /// </summary>
    [HttpGet("{variantId}")]
    public async Task<ActionResult<VariantDetail>> GetVariantDetail(long variantId)
    {
        try
        {
            var variant = await _variantService.GetVariantDetailAsync(variantId);
            
            if (variant == null)
            {
                return NotFound(new { message = $"Varyant bulunamadı: {variantId}" });
            }
            
            return Ok(variant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting variant detail {VariantId}", variantId);
            return StatusCode(500, new { message = "Varyant detay getirme hatası", error = ex.Message });
        }
    }

    /// <summary>
    /// Varyant gruplarını arar
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<List<ProductVariantGroup>>> SearchVariants([FromQuery] string? q)
    {
        try
        {
            var groups = await _variantService.SearchVariantGroupsAsync(q ?? string.Empty);
            return Ok(groups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching variants with query {Query}", q);
            return StatusCode(500, new { message = "Varyant arama hatası", error = ex.Message });
        }
    }
}
