using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Katana.API.Controllers;

/// <summary>
/// BOM (Bill of Materials) API endpoints
/// </summary>
[Authorize]
[ApiController]
[Route("api/bom")]
public class BOMController : ControllerBase
{
    private readonly IBOMService _bomService;
    private readonly ILogger<BOMController> _logger;

    public BOMController(
        IBOMService bomService,
        ILogger<BOMController> logger)
    {
        _bomService = bomService;
        _logger = logger;
    }

    /// <summary>
    /// Sipariş için BOM gereksinimlerini hesaplar
    /// </summary>
    [HttpGet("order/{orderId}/requirements")]
    public async Task<ActionResult<BOMRequirementResult>> GetOrderRequirements(int orderId)
    {
        try
        {
            var result = await _bomService.CalculateBOMRequirementsAsync(orderId);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating BOM requirements for order {OrderId}", orderId);
            return StatusCode(500, new { message = "BOM hesaplama hatası", error = ex.Message });
        }
    }

    /// <summary>
    /// Ürün için BOM bileşenlerini getirir
    /// </summary>
    [HttpGet("product/{variantId}/components")]
    public async Task<ActionResult<List<BOMComponent>>> GetProductComponents(long variantId)
    {
        try
        {
            var components = await _bomService.GetBOMComponentsAsync(variantId);
            return Ok(components);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting BOM components for variant {VariantId}", variantId);
            return StatusCode(500, new { message = "BOM bileşenleri getirme hatası", error = ex.Message });
        }
    }

    /// <summary>
    /// Ürünün BOM'u olup olmadığını kontrol eder
    /// </summary>
    [HttpGet("product/{variantId}/has-bom")]
    public async Task<ActionResult<bool>> HasBOM(long variantId)
    {
        try
        {
            var hasBom = await _bomService.HasBOMAsync(variantId);
            return Ok(new { variantId, hasBom });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking BOM for variant {VariantId}", variantId);
            return StatusCode(500, new { message = "BOM kontrol hatası", error = ex.Message });
        }
    }

    /// <summary>
    /// Sipariş için stok eksikliklerini tespit eder
    /// </summary>
    [HttpGet("order/{orderId}/shortages")]
    public async Task<ActionResult<List<StockShortage>>> GetOrderShortages(int orderId)
    {
        try
        {
            var requirements = await _bomService.CalculateBOMRequirementsAsync(orderId);
            return Ok(requirements.Shortages);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting shortages for order {OrderId}", orderId);
            return StatusCode(500, new { message = "Stok eksikliği tespit hatası", error = ex.Message });
        }
    }
}
