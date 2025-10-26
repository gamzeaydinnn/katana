// Katana.API/Controllers/DTOs/MappingRequestDto.cs
using System.ComponentModel.DataAnnotations;

namespace Katana.API.Controllers.DTOs
{
    public class MappingRequestDto
    {
        // Kaynak sistemdeki kod (Örn: Katana ürün kodu)
        [Required]
        public string SourceCode { get; set; } = null!;

        // Hedef sistemdeki kod (Örn: Luca stok kodu)
        [Required]
        public string TargetCode { get; set; } = null!;

        // Eşleştirme tipi (Product, Customer, vb.)
        [Required]
        public string MappingType { get; set; } = null!; 
    }
}