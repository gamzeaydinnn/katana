using Katana.Core.DTOs;
using Katana.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Katana.Business.Interfaces;
using Katana.Infrastructure.Logging;
using Katana.Core.Enums;

namespace Katana.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Manager")] // tüm endpoint’leri varsayılan olarak Admin ve Manager görebilir
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILoggingService _loggingService;
    private readonly IAuditService _auditService;

    public UsersController(IUserService userService, ILoggingService loggingService, IAuditService auditService)
    {
        _userService = userService;
        _loggingService = loggingService;
        _auditService = auditService;
    }

    /// <summary>
    /// Tüm kullanıcıları getirir.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAllAsync();
        _loggingService.LogInfo("Users listed", User?.Identity?.Name, null, LogCategory.UserAction);
        return Ok(users);
    }

    /// <summary>
    /// Belirli bir kullanıcıyı ID’ye göre getirir.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        return user == null ? NotFound(new { message = $"Kullanıcı bulunamadı: {id}" }) : Ok(user);
    }

    /// <summary>
    /// Yeni bir kullanıcı oluşturur.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")] // yalnızca admin kullanıcı oluşturabilir
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userService.CreateAsync(dto);
        _auditService.LogCreate("User", user.Id.ToString(), User?.Identity?.Name ?? "system", $"Username: {user.Username}");
        _loggingService.LogInfo($"User created: {user.Username}", User?.Identity?.Name, null, LogCategory.UserAction);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    /// <summary>
    /// Kullanıcıyı siler.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _userService.DeleteAsync(id);
        if (success)
        {
            _auditService.LogDelete("User", id.ToString(), User?.Identity?.Name ?? "system", null);
            _loggingService.LogInfo($"User deleted: {id}", User?.Identity?.Name, null, LogCategory.UserAction);
            return NoContent();
        }
        return NotFound(new { message = $"Kullanıcı bulunamadı: {id}" });
    }

    /// <summary>
    /// Kullanıcının rolünü günceller.
    /// </summary>
    [HttpPut("{id}/role")]
    [Authorize(Roles = "Admin")] // sadece Admin rol değiştirebilir
    public async Task<IActionResult> UpdateRole(int id, [FromBody] string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return BadRequest(new { error = "Rol boş olamaz." });

        var success = await _userService.UpdateRoleAsync(id, role);
        if (success)
        {
            _auditService.LogUpdate("User", id.ToString(), User?.Identity?.Name ?? "system", null, $"Role: {role}");
            _loggingService.LogInfo($"User role updated: {id}", User?.Identity?.Name, null, LogCategory.UserAction);
            return NoContent();
        }
        return NotFound(new { message = $"Kullanıcı bulunamadı: {id}" });
    }

    /// <summary>
    /// Kullanıcı bilgilerini günceller.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")] // sadece Admin kullanıcı güncelleyebilir
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var user = await _userService.UpdateAsync(id, dto);
            _auditService.LogUpdate("User", id.ToString(), User?.Identity?.Name ?? "system", null, $"Updated: {user.Username}");
            _loggingService.LogInfo($"User updated: {id}", User?.Identity?.Name, null, LogCategory.UserAction);
            return Ok(user);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            // duplicate username/email gibi durumlar
            return Conflict(new { error = ex.Message });
        }
    }
}
