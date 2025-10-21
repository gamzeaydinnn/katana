/*Admin paneli için kullanıcı girişi ve yetkilendirme işlemlerini yapacak.

Amacı: Arayüze güvenli erişimi sağlamak.

Sorumlulukları:

Kullanıcı adı/şifre ile giriş yapıp JWT token üreten bir endpoint (POST /api/auth/login).*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Katana.Core.DTOs;
using Katana.Business.DTOs;
using Katana.Business.Interfaces;


namespace Katana.API.Controllers;

/// <summary>
/// Admin paneli için kullanıcı girişi ve yetkilendirme işlemlerini yönetir.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // Bu controller'a token olmadan erişilebilmelidir.
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    private readonly IAuditService _auditService;

    public AuthController(IConfiguration configuration, ILogger<AuthController> logger, IAuditService auditService)
    {
        _configuration = configuration;
        _logger = logger;
        _auditService = auditService;
    }

    /// <summary>
    /// Kullanıcı adı ve şifre ile giriş yaparak JWT token alır.
    /// </summary>
    /// <param name="loginRequest">Kullanıcı adı ve şifre bilgisi.</param>
    /// <returns>Başarılı girişte JWT token, aksi halde Unauthorized döner.</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] LoginRequest loginRequest)
    {
        // Gerçek bir uygulamada bu bilgiler veritabanından, hash'lenmiş olarak kontrol edilmelidir.
        // Bu örnekte, kolaylık olması için appsettings.json dosyasından okunmaktadır.
        var adminUsername = _configuration["AuthSettings:AdminUsername"];
        var adminPassword = _configuration["AuthSettings:AdminPassword"];

        if (string.IsNullOrEmpty(adminUsername) || string.IsNullOrEmpty(adminPassword))
        {
            _logger.LogCritical("Authentication settings (AdminUsername/AdminPassword) are not configured.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Authentication is not configured on the server.");
        }

        if (loginRequest.Username == adminUsername && loginRequest.Password == adminPassword)
        {
            _logger.LogInformation("Admin user '{Username}' successfully logged in.", loginRequest.Username);
            
            // Audit log: Login işlemini kaydet
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
            _auditService.LogLogin(loginRequest.Username, ipAddress, userAgent);
            
            var token = GenerateJwtToken(loginRequest.Username);
            return Ok(new LoginResponse(token));
        }

        _logger.LogWarning("Failed login attempt for user '{Username}'.", loginRequest.Username);
        // Frontend hatayı err.response.data.message olarak okuyor
        return Unauthorized(new { message = "Invalid username or password." });
    }

    private string GenerateJwtToken(string username)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"] 
            ?? throw new InvalidOperationException("JWT Key not configured.")));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(ClaimTypes.Name, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            // Rol eklemek isterseniz:
            // new Claim(ClaimTypes.Role, "Admin")
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(8), // Token geçerlilik süresi
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}

