using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Katana.Core.DTOs;
using Katana.Business.DTOs;
using Katana.Business.Interfaces;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Katana.Core.Entities;

namespace Katana.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    private readonly IAuditService _auditService;
    private readonly IntegrationDbContext _context;

    public AuthController(IConfiguration configuration, ILogger<AuthController> logger, IAuditService auditService, IntegrationDbContext context)
    {
        _configuration = configuration;
        _logger = logger;
        _auditService = auditService;
        _context = context;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == loginRequest.Username && u.IsActive);
        
        if (user == null)
        {
            _logger.LogWarning("Failed login attempt for user '{Username}' - user not found.", loginRequest.Username);
            return Unauthorized(new { message = "Invalid username or password." });
        }

        using var sha = SHA256.Create();
        var hash = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(loginRequest.Password)));
        
        if (user.PasswordHash != hash)
        {
            _logger.LogWarning("Failed login attempt for user '{Username}' - incorrect password.", loginRequest.Username);
            return Unauthorized(new { message = "Invalid username or password." });
        }

        _logger.LogInformation("User '{Username}' successfully logged in.", loginRequest.Username);
        
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
        _auditService.LogLogin(loginRequest.Username, ipAddress, userAgent);
        
        var token = GenerateJwtToken(loginRequest.Username, user.Role);
        return Ok(new LoginResponse(token));
    }

    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
        {
            return Unauthorized(new { message = "User not authenticated." });
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            return Unauthorized(new { message = "User not found." });
        }

        using var sha = SHA256.Create();
        var currentHash = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(request.CurrentPassword)));
        
        if (user.PasswordHash != currentHash)
        {
            _logger.LogWarning("Failed password change attempt for user '{Username}': Incorrect current password.", username);
            return BadRequest(new { message = "Mevcut şifre yanlış." });
        }

        if (request.NewPassword.Length < 6)
        {
            return BadRequest(new { message = "Yeni şifre en az 6 karakter olmalı." });
        }

        var newHash = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(request.NewPassword)));
        user.PasswordHash = newHash;
        user.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Password changed successfully for user '{Username}'.", username);
        
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
        _auditService.LogPasswordChange(username, ipAddress, userAgent);

        return Ok(new { message = "Şifre başarıyla değiştirildi." });
    }

    private string GenerateJwtToken(string username, string role)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"] 
            ?? throw new InvalidOperationException("JWT Key not configured.")));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(ClaimTypes.Name, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, role)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(5),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}
