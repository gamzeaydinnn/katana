using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Katana.API.Controllers.Admin;

/// <summary>
/// Duplike sipariş temizleme API'si
/// </summary>
[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = "Admin")]
public class DuplicateOrderCleanupController : ControllerBase
{
    private readonly IDuplicateOrderCleanupService _cleanupService;
    private readonly ILogger<DuplicateOrderCleanupController> _logger;

    public DuplicateOrderCleanupController(
        IDuplicateOrderCleanupService cleanupService,
        ILogger<DuplicateOrderCleanupController> logger)
    {
        _cleanupService = cleanupService;
        _logger = logger;
    }

    /// <summary>
    /// Duplike siparişleri analiz et
    /// </summary>
    /// <remarks>
    /// Aynı OrderNo'ya sahip siparişleri tespit eder ve hangisinin tutulacağını belirler.
    /// </remarks>
    [HttpGet("duplicates/analyze")]
    [ProducesResponseType(typeof(DuplicateOrderAnalysisResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DuplicateOrderAnalysisResult>> AnalyzeDuplicates(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Duplicate order analysis requested by {User}", User.Identity?.Name);
            var result = await _cleanupService.AnalyzeDuplicatesAsync(ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze duplicate orders");
            return StatusCode(500, new { error = "Analysis failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Duplike siparişleri temizle
    /// </summary>
    /// <remarks>
    /// dryRun=true (varsayılan) ile önizleme yapılır, dryRun=false ile gerçek silme işlemi yapılır.
    /// </remarks>
    [HttpPost("duplicates/cleanup")]
    [ProducesResponseType(typeof(OrderCleanupResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OrderCleanupResult>> CleanupDuplicates(
        [FromBody] OrderCleanupRequest? request,
        CancellationToken ct)
    {
        try
        {
            var dryRun = request?.DryRun ?? true;
            _logger.LogInformation("Duplicate order cleanup requested by {User}, DryRun={DryRun}",
                User.Identity?.Name, dryRun);

            var result = await _cleanupService.CleanupDuplicatesAsync(dryRun, ct);

            if (!result.Success && !dryRun)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup duplicate orders");
            return StatusCode(500, new { error = "Cleanup failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Bozuk OrderNo'ları analiz et
    /// </summary>
    /// <remarks>
    /// SO-SO-84, SO-SO-SO-56 gibi bozuk formatlı sipariş numaralarını tespit eder.
    /// </remarks>
    [HttpGet("malformed/analyze")]
    [ProducesResponseType(typeof(MalformedOrderAnalysisResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MalformedOrderAnalysisResult>> AnalyzeMalformed(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Malformed order analysis requested by {User}", User.Identity?.Name);
            var result = await _cleanupService.AnalyzeMalformedAsync(ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze malformed orders");
            return StatusCode(500, new { error = "Analysis failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Bozuk OrderNo'ları temizle
    /// </summary>
    /// <remarks>
    /// Bozuk formatlı siparişleri düzeltir: mevcut doğru siparişle birleştirir veya yeniden adlandırır.
    /// dryRun=true (varsayılan) ile önizleme yapılır.
    /// </remarks>
    [HttpPost("malformed/cleanup")]
    [ProducesResponseType(typeof(OrderCleanupResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OrderCleanupResult>> CleanupMalformed(
        [FromBody] OrderCleanupRequest? request,
        CancellationToken ct)
    {
        try
        {
            var dryRun = request?.DryRun ?? true;
            _logger.LogInformation("Malformed order cleanup requested by {User}, DryRun={DryRun}",
                User.Identity?.Name, dryRun);

            var result = await _cleanupService.CleanupMalformedAsync(dryRun, ct);

            if (!result.Success && !dryRun)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup malformed orders");
            return StatusCode(500, new { error = "Cleanup failed", message = ex.Message });
        }
    }

    /// <summary>
    /// OrderNo'nun bozuk format olup olmadığını kontrol et
    /// </summary>
    [HttpGet("check-orderno/{orderNo}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public ActionResult CheckOrderNo(string orderNo)
    {
        var isMalformed = _cleanupService.IsMalformedOrderNo(orderNo);
        var corrected = _cleanupService.ExtractBaseOrderNo(orderNo);

        return Ok(new
        {
            orderNo,
            isMalformed,
            correctedOrderNo = corrected,
            needsCorrection = orderNo != corrected
        });
    }
}
