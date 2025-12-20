using System;
using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

/// <summary>
/// Login request DTO
/// </summary>
public record LoginRequest(string Username, string Password);

/// <summary>
/// Login response DTO
/// </summary>
public record LoginResponse(
    [property: JsonPropertyName("token")] string Token
);

/// <summary>
/// Change password request DTO
/// </summary>
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
