namespace Katana.Core.Enums;

/// <summary>
/// Senkronizasyon işlemlerinin durumunu temsil eder.
/// </summary>
public enum SyncStatus
{
    /// <summary>
    /// Senkronizasyon henüz başlatılmadı.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Senkronizasyon şu anda devam ediyor.
    /// </summary>
    Running = 1,

    /// <summary>
    /// Senkronizasyon başarıyla tamamlandı.
    /// </summary>
    Success = 2,

    /// <summary>
    /// Senkronizasyon sırasında hata oluştu.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Senkronizasyon iptal edildi veya yarıda kesildi.
    /// </summary>
    Cancelled = 4
}
