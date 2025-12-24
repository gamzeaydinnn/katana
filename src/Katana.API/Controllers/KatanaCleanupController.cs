using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Katana.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class KatanaCleanupController : ControllerBase
{
    private readonly IKatanaCleanupService _cleanupService;
    private readonly ILogger<KatanaCleanupController> _logger;

    public KatanaCleanupController(
        IKatanaCleanupService cleanupService,
        ILogger<KatanaCleanupController> logger)
    {
        _cleanupService = cleanupService;
        _logger = logger;
    }

    /// <summary>
    /// Analyze order products sent to Katana
    /// </summary>
    [HttpGet("analyze")]
    [ProducesResponseType(typeof(OrderProductAnalysisResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<OrderProductAnalysisResult>> AnalyzeOrderProducts()
    {
        try
        {
            _logger.LogInformation("Admin requested order product analysis");
            var result = await _cleanupService.AnalyzeOrderProductsAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing order products");
            return StatusCode(500, new { error = "Failed to analyze order products", details = ex.Message });
        }
    }

    /// <summary>
    /// Delete products from Katana
    /// </summary>
    [HttpPost("delete-from-katana")]
    [ProducesResponseType(typeof(KatanaCleanupResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<KatanaCleanupResult>> DeleteFromKatana([FromBody] KatanaCleanupRequest request)
    {
        if (request == null || request.Skus == null || !request.Skus.Any())
        {
            return BadRequest(new { error = "SKU list is required" });
        }

        try
        {
            _logger.LogInformation(
                "Admin requested Katana cleanup for {Count} SKUs (DryRun: {DryRun})",
                request.Skus.Count,
                request.DryRun);

            var result = await _cleanupService.DeleteFromKatanaAsync(request.Skus, request.DryRun);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting products from Katana");
            return StatusCode(500, new { error = "Failed to delete products from Katana", details = ex.Message });
        }
    }

    /// <summary>
    /// Reset order approvals
    /// </summary>
    [HttpPost("reset-orders")]
    [ProducesResponseType(typeof(ResetResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResetResult>> ResetOrders([FromBody] ResetOrdersRequest request)
    {
        try
        {
            _logger.LogInformation("Admin requested order reset (DryRun: {DryRun})", request?.DryRun ?? true);

            var result = await _cleanupService.ResetOrderApprovalsAsync(request?.DryRun ?? true);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting orders");
            return StatusCode(500, new { error = "Failed to reset orders", details = ex.Message });
        }
    }

    /// <summary>
    /// Create database backup
    /// </summary>
    [HttpPost("backup")]
    [ProducesResponseType(typeof(BackupResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BackupResult>> CreateBackup()
    {
        try
        {
            _logger.LogInformation("Admin requested database backup");
            var result = await _cleanupService.CreateBackupAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating backup");
            return StatusCode(500, new { error = "Failed to create backup", details = ex.Message });
        }
    }

    /// <summary>
    /// Rollback to a previous backup
    /// </summary>
    [HttpPost("rollback")]
    [ProducesResponseType(typeof(RollbackResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RollbackResult>> Rollback([FromBody] RollbackRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.BackupId))
        {
            return BadRequest(new { error = "Backup ID is required" });
        }

        try
        {
            _logger.LogInformation("Admin requested rollback to backup {BackupId}", request.BackupId);
            var result = await _cleanupService.RollbackAsync(request.BackupId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling back to backup {BackupId}", request.BackupId);
            return StatusCode(500, new { error = "Failed to rollback", details = ex.Message });
        }
    }

    /// <summary>
    /// Generate cleanup report
    /// </summary>
    [HttpGet("report")]
    [ProducesResponseType(typeof(CleanupReport), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CleanupReport>> GetReport()
    {
        try
        {
            _logger.LogInformation("Admin requested cleanup report");
            var result = await _cleanupService.GenerateCleanupReportAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating cleanup report");
            return StatusCode(500, new { error = "Failed to generate report", details = ex.Message });
        }
    }

    /// <summary>
    /// Find duplicate orders in Katana
    /// </summary>
    [HttpGet("duplicate-orders")]
    [ProducesResponseType(typeof(Dictionary<string, List<long>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Dictionary<string, List<long>>>> FindDuplicateOrders([FromQuery] DateTime? fromDate = null)
    {
        try
        {
            _logger.LogInformation("Admin requested duplicate order search from {FromDate}", fromDate ?? DateTime.UtcNow.AddDays(-30));
            var result = await _cleanupService.FindDuplicateOrdersAsync(fromDate);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding duplicate orders");
            return StatusCode(500, new { error = "Failed to find duplicate orders", details = ex.Message });
        }
    }

    /// <summary>
    /// Cancel duplicate orders in Katana
    /// </summary>
    [HttpPost("cancel-duplicate-orders")]
    [ProducesResponseType(typeof(KatanaOrderCleanupResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<KatanaOrderCleanupResult>> CancelDuplicateOrders([FromBody] CancelDuplicateOrdersRequest request)
    {
        if (request == null || request.KatanaOrderIds == null || !request.KatanaOrderIds.Any())
        {
            return BadRequest(new { error = "Katana order ID list is required" });
        }

        try
        {
            _logger.LogInformation(
                "Admin requested duplicate order cancellation for {Count} orders (DryRun: {DryRun})",
                request.KatanaOrderIds.Count,
                request.DryRun);

            var result = await _cleanupService.CancelDuplicateOrdersAsync(request.KatanaOrderIds, request.DryRun);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling duplicate orders");
            return StatusCode(500, new { error = "Failed to cancel duplicate orders", details = ex.Message });
        }
    }
}

// Request DTOs
public class KatanaCleanupRequest
{
    public List<string> Skus { get; set; } = new();
    public bool DryRun { get; set; } = true;
}

public class ResetOrdersRequest
{
    public bool DryRun { get; set; } = true;
}

public class RollbackRequest
{
    public string BackupId { get; set; } = string.Empty;
}

public class CancelDuplicateOrdersRequest
{
    public List<long> KatanaOrderIds { get; set; } = new();
    public bool DryRun { get; set; } = true;
}
