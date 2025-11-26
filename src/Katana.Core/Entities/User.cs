using System.ComponentModel.DataAnnotations;

namespace Katana.Core.Entities;

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = "Staff";

    public bool IsActive { get; set; } = true;

    public string Email { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
