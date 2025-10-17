//filtreleme, kısıtlama, senkronize edilecek veri türleri seçimi
//request payload’
namespace Katana.Core.DTOs;

    /// <summary>
    /// Genel senkronizasyon talebi DTO'su.
    /// Manuel veya planlı senkronizasyon işlemleri için parametreleri taşır.
    /// </summary>
    public class SyncRequestDto
    {
        /// <summary>
        /// Senkronize edilecek veri türü (örnek: STOCK, INVOICE, CUSTOMER, ALL)
        /// </summary>
        public string SyncType { get; set; } = "ALL";

        /// <summary>
        /// Belirli bir tarihten itibaren senkronizasyon yapmak için kullanılır.
        /// Örneğin sadece son 7 günün verisini senkronize etmek için.
        /// </summary>
        public DateTime? FromDate { get; set; }

        /// <summary>
        /// Manuel tetikleme işlemini başlatan kullanıcı.
        /// Log’lama amacıyla kullanılabilir.
        /// </summary>
        public string? TriggeredBy { get; set; }

        /// <summary>
        /// Yalnızca aktif kayıtların senkronize edilip edilmeyeceği.
        /// </summary>
        public bool OnlyActiveRecords { get; set; } = true;

        /// <summary>
        /// Paralel senkronizasyon işlemi (örneğin stok ve müşteri aynı anda) çalıştırılsın mı?
        /// </summary>
        public bool RunInParallel { get; set; } = false;
    }

