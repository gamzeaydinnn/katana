
using System.ComponentModel.DataAnnotations;

namespace Katana.API.Controllers.DTOs
{
    public class MappingRequestDto
    {
        
        [Required]
        public string SourceCode { get; set; } = null!;

        
        [Required]
        public string TargetCode { get; set; } = null!;

        
        [Required]
        public string MappingType { get; set; } = null!; 
    }
}