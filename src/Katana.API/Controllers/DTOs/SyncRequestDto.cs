// Katana.API/Controllers/DTOs/SyncRequestDto.cs
using System.ComponentModel.DataAnnotations;

namespace Katana.API.Controllers.DTOs
{
    public class SyncRequestDto
    {
        // Hangi tür senkronizasyon yapılacağını belirtir (Örn: "Stock", "Sales", "Full")
        [Required]
        public string SyncType { get; set; } = null!; 

        // Hangi tarihten itibaren veri çekileceği (KatanaClient için)
        public DateTime? StartDate { get; set; } 

        // Eğer tek bir seferlik çalıştırma isteniyorsa
        public bool RunOnce { get; set; } = true;
    }
}