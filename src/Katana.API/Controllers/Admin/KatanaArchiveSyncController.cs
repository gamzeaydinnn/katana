using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Katana.API.Controllers.Admin;

/// <summary>
/// Controller for synchronizing product archives between local database and Katana API.
/// Archives products in Katana that don't exist in local database.
/// </summary>
[ApiController]
[Route("api/admin/katana-archive-sync")]
[Authorize(Roles = "Admin,Manager")]
public class KatanaArchiveSyncController : ControllerBase
{
    private readonly IKatanaArchiveSyncService _archiveSyncService;
    private readonly ILogger<KatanaArchiveSyncController> _logger;

    public KatanaArchiveSyncController(
        IKatanaArchiveSyncService archiveSyncService,
        ILogger<KatanaArchiveSyncController> logger)
    {
        _archiveSyncService = archiveSyncService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a preview of products that would be archived without making any changes.
    /// </summary>
    /// <returns>List of products that would be archived</returns>
    [HttpGet("preview")]
    [ProducesResponseType(typeof(List<ProductArchivePreview>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<ProductArchivePreview>>> GetPreview()
    {
        try
        {
            _logger.LogInformation("Archive preview requested by user: {User}", User.Identity?.Name);
            
            var preview = await _archiveSyncService.GetArchivePreviewAsync();
            
            _logger.LogInformation("Archive preview completed: {Count} products to archive", preview.Count);
            
            return Ok(preview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting archive preview");
            return StatusCode(500, new { error = "Failed to get archive preview", message = ex.Message });
        }
    }

    /// <summary>
    /// Executes the archive sync operation.
    /// </summary>
    /// <param name="previewOnly">If true, returns preview without making changes</param>
    /// <returns>Result of the sync operation</returns>
    [HttpPost("sync")]
    [ProducesResponseType(typeof(ArchiveSyncResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ArchiveSyncResult>> SyncArchive([FromQuery] bool previewOnly = false)
    {
        try
        {
            _logger.LogInformation(
                "Archive sync requested by user: {User}, previewOnly: {PreviewOnly}", 
                User.Identity?.Name, previewOnly);
            
            var result = await _archiveSyncService.SyncArchiveAsync(previewOnly);
            
            _logger.LogInformation(
                "Archive sync completed: Success={Success}, Failed={Failed}, Duration={Duration}s",
                result.ArchivedSuccessfully, result.ArchiveFailed, result.DurationSeconds);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing archive sync");
            return StatusCode(500, new { error = "Failed to execute archive sync", message = ex.Message });
        }
    }

    /// <summary>
    /// Gets summary statistics about the archive sync status.
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetStatus()
    {
        try
        {
            var preview = await _archiveSyncService.GetArchivePreviewAsync();
            
            return Ok(new
            {
                productsToArchive = preview.Count,
                alreadyArchived = preview.Count(p => p.IsAlreadyArchived),
                pendingArchive = preview.Count(p => !p.IsAlreadyArchived),
                checkedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting archive status");
            return StatusCode(500, new { error = "Failed to get archive status", message = ex.Message });
        }
    }
}
