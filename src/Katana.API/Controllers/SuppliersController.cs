using Katana.Core.DTOs;
using Katana.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Katana.Business.Validators;
using Katana.Business.Interfaces;
using Katana.Infrastructure.Logging;
using Katana.Core.Enums;

namespace Katana.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _supplierService;
    private readonly ILoggingService _loggingService;
    private readonly IAuditService _auditService;

    public SuppliersController(
        ISupplierService supplierService,
        ILoggingService loggingService,
        IAuditService auditService)
    {
        _supplierService = supplierService;
        _loggingService = loggingService;
        _auditService = auditService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll() => Ok(await _supplierService.GetAllAsync());

    [HttpPost("import-from-katana")]
    public async Task<IActionResult> ImportFromKatana(CancellationToken ct)
        => Ok(await _supplierService.ImportFromKatanaAsync(ct));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var supplier = await _supplierService.GetByIdAsync(id);
        return supplier == null ? NotFound() : Ok(supplier);
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Create([FromBody] CreateSupplierDto dto)
    {
        var errors = new List<string>();
        if (!DataValidator.IsNotEmpty(dto.Name)) errors.Add("Name is required.");
        if (!string.IsNullOrWhiteSpace(dto.Email) && !DataValidator.IsValidEmail(dto.Email)) errors.Add("Invalid email format.");
        if (errors.Any()) return BadRequest(new { errors });

        var supplier = await _supplierService.CreateAsync(dto);
        _auditService.LogCreate("Supplier", supplier.Id.ToString(), User?.Identity?.Name ?? "system", $"Name: {supplier.Name}");
        _loggingService.LogInfo($"Supplier created: {supplier.Id}", User?.Identity?.Name, null, LogCategory.UserAction);
        return CreatedAtAction(nameof(GetById), new { id = supplier.Id }, supplier);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSupplierDto dto)
    {
        var errors = new List<string>();
        if (!DataValidator.IsNotEmpty(dto.Name)) errors.Add("Name is required.");
        if (!string.IsNullOrWhiteSpace(dto.Email) && !DataValidator.IsValidEmail(dto.Email)) errors.Add("Invalid email format.");
        if (errors.Any()) return BadRequest(new { errors });

        try
        {
            var supplier = await _supplierService.UpdateAsync(id, dto);
            _auditService.LogUpdate("Supplier", id.ToString(), User?.Identity?.Name ?? "system", null, $"Updated: {supplier.Name}");
            _loggingService.LogInfo($"Supplier updated: {id}", User?.Identity?.Name, null, LogCategory.UserAction);
            return Ok(supplier);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _supplierService.DeleteAsync(id);
            if (!result) return NotFound();
            _auditService.LogDelete("Supplier", id.ToString(), User?.Identity?.Name ?? "system", null);
            _loggingService.LogInfo($"Supplier deleted: {id}", User?.Identity?.Name, null, LogCategory.UserAction);
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
        var success = await _supplierService.ActivateAsync(id);
        if (!success) return NotFound();
        _loggingService.LogInfo($"Supplier activated: {id}", User?.Identity?.Name, null, LogCategory.UserAction);
        return NoContent();
    }

    [HttpPut("{id}/deactivate")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var success = await _supplierService.DeactivateAsync(id);
        if (!success) return NotFound();
        _loggingService.LogInfo($"Supplier deactivated: {id}", User?.Identity?.Name, null, LogCategory.UserAction);
        return NoContent();
    }
}
