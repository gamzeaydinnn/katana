using System;
using System.ComponentModel.DataAnnotations;

namespace Katana.Data.Models;
public class LucaProduct
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string LucaCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    public string LucaName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? LucaCategory { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
