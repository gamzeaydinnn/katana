using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Katana.API.Middleware;

/// <summary>
/// Admin paneli API'larına gelen isteklerde JWT token'ını doğrulayacak olan middleware.
/// </summary>
public class AuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthMiddleware> _logger;
    private readonly IConfiguration _configuration;

    public AuthMiddleware(RequestDelegate next, ILogger<AuthMiddleware> logger, IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Kimlik doğrulama gerektirmeyen yolları (path) atla.
        // Login, Swagger ve Health check endpoint'leri herkese açık olmalı.
        if (context.Request.Path.StartsWithSegments("/api/auth/login") ||
            context.Request.Path.StartsWithSegments("/swagger") ||
            context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        // Authorization header'ını al
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

        if (token != null)
        {
            await AttachUserToContext(context, token);
        }
        
        // Eğer context.User set edilmemişse ve endpoint [Authorize] attribute'u taşıyorsa,
        // ASP.NET Core'un kendi mekanizması 401 Unauthorized döndürecektir.
        // Bu yüzden burada manuel bir engelleme yapmaya gerek kalmıyor.
        await _next(context);
    }

    private async Task AttachUserToContext(HttpContext context, string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] 
                ?? throw new InvalidOperationException("JWT Key not configured."));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                // Zaman aşımı kontrolü
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            // Token'ı doğrula ve principal (kimlik) bilgilerini al
            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

            // Kullanıcı kimliğini isteğin context'ine ekle.
            // Bu sayede [Authorize] attribute'u ve controller içindeki User nesnesi çalışır.
            context.User = principal;
        }
        catch (Exception ex)
        {
            // Token doğrulama başarısız olursa (geçersiz, süresi dolmuş vb.)
            // loglama yapabiliriz ancak context'e kullanıcı eklemeyiz.
            _logger.LogWarning(ex, "JWT token validation failed.");
            // Hata fırlatmaya veya response'u direkt değiştirmeye gerek yok,
            // [Authorize] attribute'u context.User boş olduğu için zaten 401 döndürecektir.
            await HandleUnauthorizedAsync(context, $"JWT Token Validation Failed: {ex.Message}");
        }
    }

    private static async Task HandleUnauthorizedAsync(HttpContext context, string message)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        context.Response.ContentType = "application/json";

        var response = new
        {
            Message = "Unauthorized",
            Details = message,
            Timestamp = DateTime.UtcNow
        };

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}
