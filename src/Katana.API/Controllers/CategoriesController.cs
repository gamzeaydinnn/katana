using Katana.Core.DTOs;
using Katana.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Katana.Business.Interfaces;
using Katana.Core.Enums;
using Katana.Infrastructure.Logging;

namespace Katana.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CategoriesController> _logger;
    private readonly ILoggingService _loggingService;
    private readonly IAuditService _auditService;

    public CategoriesController(ICategoryService categoryService, ILogger<CategoriesController> logger, ILoggingService loggingService, IAuditService auditService)
    {
        _categoryService = categoryService;
        _logger = logger;
        _loggingService = loggingService;
        _auditService = auditService;
    }

    [HttpGet]
    [AllowAnonymous] 
    public async Task<IActionResult> GetAll()
        => Ok(await _categoryService.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _categoryService.GetByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _categoryService.CreateAsync(dto);
            _auditService.LogCreate("Category", result.Id.ToString(), User?.Identity?.Name ?? "system", $"Name: {result.Name}");
            _loggingService.LogInfo($"Category created: {result.Name}", User?.Identity?.Name, null, LogCategory.UserAction);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _categoryService.UpdateAsync(id, dto);
            _auditService.LogUpdate("Category", id.ToString(), User?.Identity?.Name ?? "system", null, $"Updated: {result.Name}");
            _loggingService.LogInfo($"Category updated: {id}", User?.Identity?.Name, null, LogCategory.UserAction);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var success = await _categoryService.DeleteAsync(id);
            if (!success) return NotFound();
            _auditService.LogDelete("Category", id.ToString(), User?.Identity?.Name ?? "system", null);
            _loggingService.LogInfo($"Category deleted: {id}", User?.Identity?.Name, null, LogCategory.UserAction);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPut("{id}/activate")]
    public async Task<IActionResult> Activate(int id)
    {
        var ok = await _categoryService.ActivateAsync(id);
        if (!ok) return NotFound();
        _loggingService.LogInfo($"Category activated: {id}", User?.Identity?.Name, null, LogCategory.UserAction);
        return NoContent();
    }

    [HttpPut("{id}/deactivate")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var ok = await _categoryService.DeactivateAsync(id);
        if (!ok) return NotFound();
        _loggingService.LogInfo($"Category deactivated: {id}", User?.Identity?.Name, null, LogCategory.UserAction);
        return NoContent();
    }
}
