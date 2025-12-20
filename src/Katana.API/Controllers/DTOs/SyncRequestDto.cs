
using System.ComponentModel.DataAnnotations;

namespace Katana.API.Controllers.DTOs
{
    public class SyncRequestDto
    {
        
        [Required]
        public string SyncType { get; set; } = null!; 

        
        public DateTime? StartDate { get; set; } 

        
        public bool RunOnce { get; set; } = true;
    }
}