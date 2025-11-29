using System.ComponentModel.DataAnnotations;

namespace Katana.Core.Entities;
public class Supplier
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Code { get; set; }

    [MaxLength(50)]
    public string? TaxNo { get; set; }

    [MaxLength(100)]
    public string? ContactName { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(300)]
    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public virtual ICollection<SupplierPrice> PriceList { get; set; } = new List<SupplierPrice>();
}
