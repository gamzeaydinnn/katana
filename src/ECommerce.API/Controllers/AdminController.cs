using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IAdminService adminService, ILogger<AdminController> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    [HttpGet("sync-status")]
    public async Task<ActionResult<List<SyncStatusDto>>> GetSyncStatuses()
    {
        return Ok(await _adminService.GetSyncStatusesAsync());
    }

    [HttpGet("error-logs")]
    public async Task<ActionResult<List<ErrorLogDto>>> GetErrorLogs([FromQuery]int page = 1, [FromQuery]int pageSize = 50)
    {
        return Ok(await _adminService.GetErrorLogsAsync(page, pageSize));
    }

    [HttpPost("manual-sync")]
    public async Task<ActionResult> ManualSync([FromBody] ManualSyncRequest request)
    {
        var result = await _adminService.RunManualSyncAsync(request);
        if(result) return Ok(new { message = "Manual sync started" });
        return BadRequest(new { message = "Failed to start manual sync" });
    }

    [HttpGet("sync-reports")]
    public async Task<ActionResult<SyncReportDto>> GetSyncReport([FromQuery] string integrationName)
    {
        return Ok(await _adminService.GetSyncReportAsync(integrationName));
    }
}
