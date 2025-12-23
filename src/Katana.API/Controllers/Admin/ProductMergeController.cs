using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Katana.API.Controllers.Admin;

/// <summary>
/// Controller for product merge operations
/// </summary>
[ApiController]
[Route("api/admin/product-merge")]
[Authorize(Roles = "Admin")]
public class ProductMergeController : ControllerBase
{
    private readonly IProductMergeService _mergeService;
    private readonly ILogger<ProductMergeController> _logger;

    public ProductMergeController(
        IProductMergeService mergeService,
        ILogger<ProductMergeController> logger)
    {
        _mergeService = mergeService;
        _logger = logger;
    }

    /// <summary>
    /// Previews the impact of a merge operation
    /// Requirements: 4.1
    /// </summary>
    [HttpPost("preview")]
    [ProducesResponseType(typeof(MergePreview), StatusCodes.Status200OK)]
    public async Task<ActionResult<MergePreview>> PreviewMerge([FromBody] MergeRequest request)
    {
        try
        {
            _logger.LogInformation("Previewing merge for canonical product {CanonicalProductId}", request.CanonicalProductId);
            var preview = await _mergeService.PreviewMergeAsync(request);
            return Ok(preview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing merge");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Executes a merge operation
    /// Requirements: 5.1
    /// </summary>
    [HttpPost("execute")]
    [ProducesResponseType(typeof(MergeResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MergeResult>> ExecuteMerge([FromBody] MergeRequest request)
    {
        try
        {
            var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            
            _logger.LogWarning("Executing merge for canonical product {CanonicalProductId} by user {UserId}", 
                request.CanonicalProductId, adminUserId);

            var result = await _mergeService.ExecuteMergeAsync(request, adminUserId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing merge");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Rolls back a merge operation
    /// Requirements: 7.5
    /// </summary>
    [HttpPost("rollback/{mergeHistoryId}")]
    [ProducesResponseType(typeof(MergeResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MergeResult>> RollbackMerge(int mergeHistoryId)
    {
        try
        {
            var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            
            _logger.LogWarning("Rolling back merge {MergeHistoryId} by user {UserId}", 
                mergeHistoryId, adminUserId);

            var result = await _mergeService.RollbackMergeAsync(mergeHistoryId, adminUserId);

            if (!result.Success)
            {
                if (result.Errors.Any(e => e.Contains("not found")))
                {
                    return NotFound(result);
                }
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling back merge");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Marks a group as keep separate
    /// Requirements: 6.1
    /// </summary>
    [HttpPost("keep-separate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkGroupAsKeepSeparate([FromBody] KeepSeparateRequest request)
    {
        try
        {
            var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            
            _logger.LogInformation("Marking group {ProductName} as keep separate by user {UserId}", 
                request.ProductName, adminUserId);

            await _mergeService.MarkGroupAsKeepSeparateAsync(request.ProductName, request.Reason, adminUserId);

            return Ok(new { message = "Group marked as keep separate" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking group as keep separate");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Removes keep separate flag
    /// Requirements: 6.5
    /// </summary>
    [HttpDelete("keep-separate/{productName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveKeepSeparateFlag(string productName)
    {
        try
        {
            var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            
            _logger.LogInformation("Removing keep separate flag for {ProductName} by user {UserId}", 
                productName, adminUserId);

            await _mergeService.RemoveKeepSeparateFlagAsync(productName, adminUserId);

            return Ok(new { message = "Keep separate flag removed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing keep separate flag");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

/// <summary>
/// Request to mark a group as keep separate
/// </summary>
public class KeepSeparateRequest
{
    public string ProductName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
