using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Katana.API.Controllers.Admin;

/// <summary>
/// Controller for merge history operations
/// </summary>
[ApiController]
[Route("api/admin/merge-history")]
[Authorize(Roles = "Admin")]
public class MergeHistoryController : ControllerBase
{
    private readonly IMergeHistoryService _historyService;
    private readonly ILogger<MergeHistoryController> _logger;

    public MergeHistoryController(
        IMergeHistoryService historyService,
        ILogger<MergeHistoryController> logger)
    {
        _historyService = historyService;
        _logger = logger;
    }

    /// <summary>
    /// Gets merge history with optional filters
    /// Requirements: 7.2, 7.3
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<MergeHistoryEntry>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MergeHistoryEntry>>> GetMergeHistory(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? adminUserId,
        [FromQuery] MergeStatus? status)
    {
        try
        {
            _logger.LogInformation("Getting merge history");

            var filter = new MergeHistoryFilter
            {
                StartDate = startDate,
                EndDate = endDate,
                AdminUserId = adminUserId,
                Status = status
            };

            var history = await _historyService.GetMergeHistoryAsync(filter);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting merge history");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets detailed information about a specific merge history entry
    /// Requirements: 7.2
    /// </summary>
    [HttpGet("{mergeHistoryId}")]
    [ProducesResponseType(typeof(MergeHistoryEntry), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MergeHistoryEntry>> GetMergeHistoryDetail(int mergeHistoryId)
    {
        try
        {
            _logger.LogInformation("Getting merge history detail for {HistoryId}", mergeHistoryId);

            var history = await _historyService.GetMergeHistoryDetailAsync(mergeHistoryId);

            if (history == null)
            {
                return NotFound(new { error = "Merge history not found" });
            }

            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting merge history detail");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
