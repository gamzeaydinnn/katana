// Katana.API/Controllers/DTOs/MappingRequestDto.cs
using System.ComponentModel.DataAnnotations;

namespace Katana.API.Controllers.DTOs
{
    public class MappingRequestDto
    {
        
        [Required]
        public string SourceCode { get; set; } = null!;

        // Hedef sistemdeki kod
        [Required]
        public string TargetCode { get; set; } = null!;

        // Eşleştirme tipi 
        [Required]
        public string MappingType { get; set; } = null!; 
    }
}