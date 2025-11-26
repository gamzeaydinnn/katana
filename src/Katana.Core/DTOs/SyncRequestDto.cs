

namespace Katana.Core.DTOs;

    
    
    
    
    public class SyncRequestDto
    {
        
        
        
        public string SyncType { get; set; } = "ALL";

        
        
        
        
        public DateTime? FromDate { get; set; }

        
        
        
        
        public string? TriggeredBy { get; set; }

        
        
        
        public bool OnlyActiveRecords { get; set; } = true;

        
        
        
        public bool RunInParallel { get; set; } = false;
    }

