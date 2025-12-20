using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Katana.Core.Entities;

[Table("VariantMappings")]
public class VariantMapping
{
    public int Id { get; set; }

    public long KatanaVariantId { get; set; }

    public int ProductId { get; set; }

    public int? ProductVariantId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Sku { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
