using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Katana.API.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class DataCorrectionController : ControllerBase
{
    private readonly IDataCorrectionService _correctionService;
    private readonly ILogger<DataCorrectionController> _logger;

    public DataCorrectionController(
        IDataCorrectionService correctionService,
        ILogger<DataCorrectionController> logger)
    {
        _correctionService = correctionService;
        _logger = logger;
    }

    /// <summary>
    /// Compare Katana and Luca products - show differences
    /// </summary>
    [HttpGet("compare/products")]
    public async Task<IActionResult> CompareProducts()
    {
        try
        {
            var comparison = await _correctionService.CompareKatanaAndLucaProductsAsync();
            return Ok(new { data = comparison, count = comparison.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compare products");
            return StatusCode(500, new { error = "Karşılaştırma başarısız", details = ex.Message });
        }
    }

    /// <summary>
    /// Get pending corrections waiting for approval
    /// </summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingCorrections()
    {
        try
        {
            var corrections = await _correctionService.GetPendingCorrectionsAsync();
            return Ok(new { data = corrections, count = corrections.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pending corrections");
            return StatusCode(500, new { error = "Düzeltmeler yüklenemedi", details = ex.Message });
        }
    }

    /// <summary>
    /// Create a new data correction
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateCorrection([FromBody] CreateCorrectionDto dto)
    {
        try
        {
            var userId = User?.Identity?.Name ?? "system";
            var correction = await _correctionService.CreateCorrectionAsync(dto, userId);
            return Ok(correction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create correction");
            return StatusCode(500, new { error = "Düzeltme oluşturulamadı", details = ex.Message });
        }
    }

    /// <summary>
    /// Approve a correction (Admin only)
    /// </summary>
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> ApproveCorrection(int id)
    {
        try
        {
            var userId = User?.Identity?.Name ?? "system";
            var result = await _correctionService.ApproveCorrectionAsync(id, userId);
            
            if (!result)
                return NotFound(new { error = "Düzeltme bulunamadı" });
            
            return Ok(new { message = "Düzeltme onaylandı" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to approve correction {CorrectionId}", id);
            return StatusCode(500, new { error = "Onaylama başarısız", details = ex.Message });
        }
    }

    /// <summary>
    /// Reject and delete a correction
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> RejectCorrection(int id)
    {
        try
        {
            var userId = User?.Identity?.Name ?? "system";
            var result = await _correctionService.RejectCorrectionAsync(id, userId);
            
            if (!result)
                return NotFound(new { error = "Düzeltme bulunamadı" });
            
            return Ok(new { message = "Düzeltme reddedildi" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reject correction {CorrectionId}", id);
            return StatusCode(500, new { error = "Reddetme başarısız", details = ex.Message });
        }
    }

    /// <summary>
    /// Apply approved correction to Luca
    /// </summary>
    [HttpPost("{id}/apply-to-luca")]
    public async Task<IActionResult> ApplyToLuca(int id)
    {
        try
        {
            var result = await _correctionService.ApplyCorrectionToLucaAsync(id);
            
            if (!result)
                return BadRequest(new { error = "Düzeltme uygulanamadı. Onaylandığından emin olun." });
            
            return Ok(new { message = "Düzeltme Luca'ya uygulandı" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply correction {CorrectionId} to Luca", id);
            return StatusCode(500, new { error = "Uygulama başarısız", details = ex.Message });
        }
    }

    /// <summary>
    /// Apply approved correction to Katana (if supported)
    /// </summary>
    [HttpPost("{id}/apply-to-katana")]
    public async Task<IActionResult> ApplyToKatana(int id)
    {
        try
        {
            var result = await _correctionService.ApplyCorrectionToKatanaAsync(id);
            
            if (!result)
                return BadRequest(new { error = "Katana API write henüz desteklenmiyor" });
            
            return Ok(new { message = "Düzeltme Katana'ya uygulandı" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply correction {CorrectionId} to Katana", id);
            return StatusCode(500, new { error = "Uygulama başarısız", details = ex.Message });
        }
    }
}
