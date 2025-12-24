using Katana.Business.Services.Deduplication;
using Katana.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Katana.API.Controllers.Admin;

/// <summary>
/// Controller for stock card deduplication operations
/// </summary>
[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "Admin")]
public class DeduplicationController : ControllerBase
{
    private readonly IDeduplicationService _deduplicationService;
    private readonly ILogger<DeduplicationController> _logger;

    public DeduplicationController(
        IDeduplicationService deduplicationService,
        ILogger<DeduplicationController> logger)
    {
        _deduplicationService = deduplicationService;
        _logger = logger;
    }

    /// <summary>
    /// Analyzes all stock cards and detects duplicates
    /// </summary>
    /// <returns>Analysis result with duplicate groups and statistics</returns>
    [HttpPost("analyze")]
    [ProducesResponseType(typeof(DuplicateAnalysisResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DuplicateAnalysisResult>> AnalyzeDuplicates(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Starting duplicate analysis requested by {User}", User.Identity?.Name);

            var result = await _deduplicationService.AnalyzeDuplicatesAsync(ct);

            _logger.LogInformation(
                "Analysis completed: {Groups} duplicate groups found with {Total} total duplicates",
                result.Statistics.DuplicateGroups,
                result.Statistics.TotalDuplicates);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during duplicate analysis");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Analysis Failed",
                Detail = ex.Message,
                Status = 500
            });
        }
    }

    /// <summary>
    /// Generates a preview of deduplication actions
    /// </summary>
    /// <param name="analysis">Analysis result from analyze endpoint</param>
    /// <returns>Preview with actions to be taken</returns>
    [HttpPost("preview")]
    [ProducesResponseType(typeof(DeduplicationPreview), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DeduplicationPreview>> GeneratePreview(
        [FromBody] DuplicateAnalysisResult analysis,
        CancellationToken ct)
    {
        try
        {
            if (analysis == null || analysis.DuplicateGroups == null)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Analysis",
                    Detail = "Analysis result is required",
                    Status = 400
                });
            }

            _logger.LogInformation(
                "Generating preview for {Groups} duplicate groups requested by {User}",
                analysis.DuplicateGroups.Count,
                User.Identity?.Name);

            var preview = await _deduplicationService.GeneratePreviewAsync(analysis, ct);

            _logger.LogInformation(
                "Preview generated: {Actions} actions, {ToRemove} cards to remove",
                preview.Statistics.TotalActions,
                preview.Statistics.CardsToRemove);

            return Ok(preview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating preview");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Preview Generation Failed",
                Detail = ex.Message,
                Status = 500
            });
        }
    }

    /// <summary>
    /// Executes the deduplication plan
    /// </summary>
    /// <param name="preview">Preview from preview endpoint</param>
    /// <returns>Execution result with success/failure counts</returns>
    [HttpPost("execute")]
    [ProducesResponseType(typeof(DeduplicationExecutionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DeduplicationExecutionResult>> ExecuteDeduplication(
        [FromBody] DeduplicationPreview preview,
        CancellationToken ct)
    {
        try
        {
            if (preview == null || preview.Actions == null)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Preview",
                    Detail = "Preview is required",
                    Status = 400
                });
            }

            _logger.LogWarning(
                "DEDUPLICATION EXECUTION started by {User} - {Actions} actions, {ToRemove} cards to remove",
                User.Identity?.Name,
                preview.Actions.Count,
                preview.Statistics.CardsToRemove);

            var result = await _deduplicationService.ExecuteDeduplicationAsync(preview, ct);

            _logger.LogWarning(
                "DEDUPLICATION EXECUTION completed: {Success} successful, {Failed} failed, {Errors} errors",
                result.SuccessfulRemovals,
                result.FailedRemovals,
                result.Errors.Count);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing deduplication");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Execution Failed",
                Detail = ex.Message,
                Status = 500
            });
        }
    }

    /// <summary>
    /// Exports analysis results in specified format
    /// </summary>
    /// <param name="format">Export format (json or csv)</param>
    /// <returns>Exported data as string</returns>
    [HttpGet("export")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status501NotImplemented)]
    public Task<IActionResult> ExportResults(
        [FromQuery] ExportFormat format,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "Export requested in {Format} format by {User}",
                format,
                User.Identity?.Name);

            // For now, return not implemented
            // This will be implemented in Task 5
            return Task.FromResult<IActionResult>(StatusCode(501, new ProblemDetails
            {
                Title = "Not Implemented",
                Detail = "Export functionality will be available soon",
                Status = 501
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting results");
            return Task.FromResult<IActionResult>(StatusCode(500, new ProblemDetails
            {
                Title = "Export Failed",
                Detail = ex.Message,
                Status = 500
            }));
        }
    }

    /// <summary>
    /// Gets current deduplication rules configuration
    /// </summary>
    /// <returns>Current rules</returns>
    [HttpGet("rules")]
    [ProducesResponseType(typeof(DeduplicationRules), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DeduplicationRules>> GetRules()
    {
        try
        {
            _logger.LogInformation("Rules requested by {User}", User.Identity?.Name);

            var rules = await _deduplicationService.GetRulesAsync();

            return Ok(rules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rules");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Failed to Get Rules",
                Detail = ex.Message,
                Status = 500
            });
        }
    }

    /// <summary>
    /// Updates deduplication rules configuration
    /// </summary>
    /// <param name="rules">New rules configuration</param>
    /// <returns>Success status</returns>
    [HttpPut("rules")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateRules([FromBody] DeduplicationRules rules)
    {
        try
        {
            if (rules == null)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Rules",
                    Detail = "Rules configuration is required",
                    Status = 400
                });
            }

            _logger.LogWarning(
                "Rules update requested by {User}",
                User.Identity?.Name);

            await _deduplicationService.UpdateRulesAsync(rules);

            _logger.LogInformation("Rules updated successfully");

            return Ok(new { message = "Rules updated successfully" });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid rules configuration");
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Rules",
                Detail = ex.Message,
                Status = 400
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating rules");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Failed to Update Rules",
                Detail = ex.Message,
                Status = 500
            });
        }
    }

    /// <summary>
    /// Merges duplicate products by SKU base code
    /// </summary>
    [HttpPost("merge-by-sku-base")]
    [ProducesResponseType(typeof(SkuBaseMergePlan), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(List<VariantMergeResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> MergeBySkuBase([FromQuery] bool dryRun = true, CancellationToken ct = default)
    {
        try
        {
            _logger.LogWarning("SKU base merge requested by {User}, DryRun={DryRun}", User.Identity?.Name, dryRun);

            if (dryRun)
            {
                var plan = await _deduplicationService.BuildSkuBaseMergePlanAsync(ct);
                return Ok(plan);
            }

            var results = await _deduplicationService.MergeProductsBySkuBaseAsync(ct);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging products by SKU base");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Merge Failed",
                Detail = ex.Message,
                Status = 500
            });
        }
    }
}
