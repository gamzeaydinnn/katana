using System.ComponentModel.DataAnnotations;

namespace Katana.Core.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CreateUserDto
{
    [Required, StringLength(100)]
    public string Username { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;
    [Required]
    public string Role { get; set; } = "Staff";
    public string? Email { get; set; }
}
public class UpdateUserDto
{
    [Required, StringLength(100)]
    public string Username { get; set; } = string.Empty;
    [StringLength(100, MinimumLength = 6)]
    public string? Password { get; set; }   // opsiyonel güncelleme
    [Required]
    public string Role { get; set; } = "Staff";
    public bool IsActive { get; set; } = true;
    public string? Email { get; set; }
}
