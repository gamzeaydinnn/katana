using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Katana.API.Controllers;

/// <summary>
/// SKU doğrulama ve yeniden adlandırma API endpoints
/// </summary>
[Authorize]
[ApiController]
[Route("api/sku")]
public class SKUController : ControllerBase
{
    private readonly ISKUValidationService _skuService;
    private readonly ILogger<SKUController> _logger;

    public SKUController(
        ISKUValidationService skuService,
        ILogger<SKUController> logger)
    {
        _skuService = skuService;
        _logger = logger;
    }

    /// <summary>
    /// SKU formatını doğrular
    /// </summary>
    [HttpPost("validate")]
    public ActionResult<SKUValidationResult> ValidateSKU([FromBody] SKUValidateRequest request)
    {
        try
        {
            var result = _skuService.ValidateSKU(request.SKU);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating SKU {SKU}", request.SKU);
            return StatusCode(500, new { message = "SKU doğrulama hatası", error = ex.Message });
        }
    }

    /// <summary>
    /// SKU'yu yeniden adlandırır
    /// </summary>
    [HttpPost("rename")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SKURenameResult>> RenameSKU([FromBody] SKURenameRequest request)
    {
        try
        {
            var result = await _skuService.RenameSKUAsync(request.OldSKU, request.NewSKU);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renaming SKU from {OldSKU} to {NewSKU}", request.OldSKU, request.NewSKU);
            return StatusCode(500, new { message = "SKU yeniden adlandırma hatası", error = ex.Message });
        }
    }

    /// <summary>
    /// Toplu SKU değişikliği önizlemesi
    /// </summary>
    [HttpPost("bulk-rename/preview")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<SKURenamePreview>>> PreviewBulkRename([FromBody] BulkSKURenamePreviewRequest request)
    {
        try
        {
            var previews = await _skuService.PreviewBulkRenameAsync(request.Renames);
            return Ok(previews);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating bulk rename preview");
            return StatusCode(500, new { message = "Önizleme oluşturma hatası", error = ex.Message });
        }
    }

    /// <summary>
    /// Toplu SKU değişikliği uygular
    /// </summary>
    [HttpPost("bulk-rename")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BulkSKURenameResult>> ExecuteBulkRename([FromBody] List<SKURenameRequest> requests)
    {
        try
        {
            var result = await _skuService.ExecuteBulkRenameAsync(requests);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing bulk rename");
            return StatusCode(500, new { message = "Toplu yeniden adlandırma hatası", error = ex.Message });
        }
    }
}
