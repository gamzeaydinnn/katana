using System.ComponentModel.DataAnnotations;
using Katana.Core.Enums;

namespace Katana.Data.Models;

/// <summary>
/// Katana ↔ Luca senkronizasyon süreçleri için log kaydı tutar.
/// Her bir senkronizasyon çalışmasının genel sonuç özetini içerir.
/// </summary>
public class IntegrationLog
{
    public int Id { get; set; }

    /// <summary>
    /// Hangi tür senkronizasyonun çalıştığı (STOCK, INVOICE, CUSTOMER vb.)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string SyncType { get; set; } = string.Empty;

    /// <summary>
    /// Senkronizasyonun mevcut durumu (Pending, Running, Success, Failed, Cancelled)
    /// </summary>
    [Required]
    public SyncStatus Status { get; set; } = SyncStatus.Pending;

    /// <summary>
    /// Verinin kaynağını belirtir (Katana, Luca, Internal, External)
    /// </summary>
    public DataSource Source { get; set; } = DataSource.Katana;

    /// <summary>
    /// İşlemin başlatıldığı zaman.
    /// </summary>
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// İşlemin tamamlandığı zaman.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Toplam işlem süresi.
    /// </summary>
    public TimeSpan? Duration => EndTime?.Subtract(StartTime);

    /// <summary>
    /// İşlenen toplam kayıt sayısı.
    /// </summary>
    public int ProcessedRecords { get; set; }

    /// <summary>
    /// Başarılı kayıt sayısı.
    /// </summary>
    public int SuccessfulRecords { get; set; }

    /// <summary>
    /// Hatalı kayıt sayısı.
    /// </summary>
    public int FailedRecordsCount { get; set; }

    /// <summary>
    /// Hata mesajı (varsa).
    /// </summary>
    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// İşlemi başlatan kullanıcı veya sistem adı.
    /// </summary>
    [MaxLength(100)]
    public string? TriggeredBy { get; set; }

    /// <summary>
    /// İşlemin detaylarını JSON formatında tutar.
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Hatalı senkronize edilen kayıtların detayları.
    /// </summary>
    public virtual ICollection<FailedSyncRecord> FailedRecords { get; set; } = new List<FailedSyncRecord>();
}
