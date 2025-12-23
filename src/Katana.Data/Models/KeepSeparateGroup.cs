using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Katana.Data.Models;

/// <summary>
/// Represents a product group that should be kept separate from merge operations
/// </summary>
[Table("keep_separate_groups")]
public class KeepSeparateGroup
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("product_name")]
    [MaxLength(500)]
    public string ProductName { get; set; } = string.Empty;

    [Required]
    [Column("reason")]
    public string Reason { get; set; } = string.Empty;

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("created_by")]
    [MaxLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    [Column("removed_at")]
    public DateTime? RemovedAt { get; set; }

    [Column("removed_by")]
    [MaxLength(100)]
    public string? RemovedBy { get; set; }
}
