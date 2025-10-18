using Katana.Core.DTOs;
using Katana.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Katana.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Manager")] // tüm endpoint’leri varsayılan olarak Admin ve Manager görebilir
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Tüm kullanıcıları getirir.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAllAsync();
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
        return success ? NoContent() : NotFound(new { message = $"Kullanıcı bulunamadı: {id}" });
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
        return success ? NoContent() : NotFound(new { message = $"Kullanıcı bulunamadı: {id}" });
    }
}
