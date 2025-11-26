// Katana.API/Controllers/DTOs/SyncRequestDto.cs
using System.ComponentModel.DataAnnotations;

namespace Katana.API.Controllers.DTOs
{
    public class SyncRequestDto
    {
        // Hangi tür senkronizasyon yapılacağını belirtir 
        [Required]
        public string SyncType { get; set; } = null!; 

        // Hangi tarihten itibaren veri çekileceği 
        public DateTime? StartDate { get; set; } 

        // Eğer tek bir seferlik çalıştırma isteniyorsa
        public bool RunOnce { get; set; } = true;
    }
}