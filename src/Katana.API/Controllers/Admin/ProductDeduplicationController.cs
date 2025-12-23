using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Katana.API.Controllers.Admin;

/// <summary>
/// Controller for product name deduplication operations
/// </summary>
[ApiController]
[Route("api/admin/product-deduplication")]
[Authorize(Roles = "Admin")]
public class ProductDeduplicationController : ControllerBase
{
    private readonly IDuplicateAnalysisService _analysisService;
    private readonly ILogger<ProductDeduplicationController> _logger;

    public ProductDeduplicationController(
        IDuplicateAnalysisService analysisService,
        ILogger<ProductDeduplicationController> logger)
    {
        _analysisService = analysisService;
        _logger = logger;
    }

    /// <summary>
    /// Analyzes all products and returns duplicate groups
    /// Requirements: 1.1
    /// </summary>
    [HttpGet("analyze")]
    [ProducesResponseType(typeof(List<ProductDuplicateGroup>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ProductDuplicateGroup>>> AnalyzeDuplicates()
    {
        try
        {
            _logger.LogInformation("Analyzing product duplicates requested by {User}", User.Identity?.Name);
            var groups = await _analysisService.AnalyzeDuplicatesAsync();
            return Ok(groups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing duplicates");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets detailed information about a specific duplicate group
    /// Requirements: 2.1
    /// </summary>
    [HttpGet("groups/{productName}")]
    [ProducesResponseType(typeof(ProductDuplicateGroup), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDuplicateGroup>> GetDuplicateGroupDetail(string productName)
    {
        try
        {
            _logger.LogInformation("Getting duplicate group detail for {ProductName}", productName);
            var group = await _analysisService.GetDuplicateGroupDetailAsync(productName);
            
            if (group == null)
            {
                return NotFound(new { error = "Duplicate group not found" });
            }

            return Ok(group);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting duplicate group detail");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Filters duplicate groups based on criteria
    /// Requirements: 8.1
    /// </summary>
    [HttpPost("filter")]
    [ProducesResponseType(typeof(List<ProductDuplicateGroup>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ProductDuplicateGroup>>> FilterDuplicateGroups(
        [FromBody] DuplicateFilterCriteria criteria)
    {
        try
        {
            _logger.LogInformation("Filtering duplicate groups");
            var groups = await _analysisService.FilterDuplicateGroupsAsync(criteria);
            return Ok(groups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error filtering duplicate groups");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Exports duplicate analysis results to CSV
    /// Requirements: 9.1
    /// </summary>
    [HttpPost("export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportDuplicateAnalysis([FromBody] List<ProductDuplicateGroup> groups)
    {
        try
        {
            _logger.LogInformation("Exporting duplicate analysis");
            var csvBytes = await _analysisService.ExportDuplicateAnalysisAsync(groups);
            return File(csvBytes, "text/csv", $"duplicate-analysis-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting duplicate analysis");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
