namespace Katana.Data.Models
{
    /// <summary>
    /// Kullanıcı aksiyonlarını izlemek için audit log tablosu
    /// </summary>
    public class AuditLog
    {
        public int Id { get; set; }
        
        /// <summary>İşlem tipi: CREATE, UPDATE, DELETE, SYNC, LOGIN</summary>
        public string ActionType { get; set; } = string.Empty;
        
        /// <summary>Etkilenen entity adı: Product, Invoice, Customer vb.</summary>
        public string EntityName { get; set; } = string.Empty;
        
        /// <summary>Etkilenen kaydın ID'si</summary>
        public string? EntityId { get; set; }
        
        /// <summary>İşlemi yapan kullanıcı</summary>
        public string PerformedBy { get; set; } = string.Empty;
        
        /// <summary>İşlem tarihi</summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        /// <summary>İşlem detayları (JSON veya açıklama)</summary>
        public string? Details { get; set; }
        
        /// <summary>Değişiklikler (eski ve yeni değerler)</summary>
        public string? Changes { get; set; }
        
        /// <summary>IP Adresi</summary>
        public string? IpAddress { get; set; }
        
        /// <summary>User Agent</summary>
        public string? UserAgent { get; set; }
    }
}
