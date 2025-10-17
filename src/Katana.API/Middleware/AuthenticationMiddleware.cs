using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Katana.API.Middleware;

public static class JwtTokenHelper
{
    public static string GenerateToken(string secretKey, string issuer, string audience, 
        Dictionary<string, string> claims, int expirationMinutes = 60)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenClaims = claims.Select(c => new Claim(c.Key, c.Value)).ToList();
        tokenClaims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
        tokenClaims.Add(new Claim(JwtRegisteredClaimNames.Iat, 
            new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: tokenClaims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static ClaimsPrincipal? ValidateToken(string token, string secretKey, string issuer, string audience)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var tokenHandler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    public static Dictionary<string, string> GetClaimsFromToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(token);
            
            return jsonToken.Claims.ToDictionary(c => c.Type, c => c.Value);
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }

    public static bool IsTokenExpired(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(token);
            
            return jsonToken.ValidTo < DateTime.UtcNow;
        }
        catch
        {
            return true;
        }
    }

    public static string GenerateApiKey(int length = 32)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}

