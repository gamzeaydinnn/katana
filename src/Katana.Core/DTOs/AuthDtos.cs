
using System;
using System.Text.Json.Serialization;

namespace Katana.Business.DTOs
{
    
    
    
    public record LoginRequest(string Username, string Password);

    
    
    
    
    public record LoginResponse(
        [property: JsonPropertyName("token")] string Token
    );

    
    
    
    public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
}
