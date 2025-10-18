namespace Katana.Core.Enums;

/// <summary>
/// Verinin hangi kaynaktan geldiğini belirtir.
/// </summary>
public enum DataSource
{
    /// <summary>
    /// Kaynak belirtilmemiş veya bilinmiyor.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Katana MRP/ERP sisteminden gelen veriler.
    /// </summary>
    Katana = 1,

    /// <summary>
    /// Luca muhasebe/finans sisteminden gelen veriler.
    /// </summary>
    Luca = 2,

    /// <summary>
    /// Sistemin kendisi tarafından üretilen (ör. manuel giriş, admin işlemi) veriler.
    /// </summary>
    Internal = 3,

    /// <summary>
    /// Dış API veya üçüncü parti bir kaynaktan gelen veriler (ör. e-fatura, e-ticaret entegrasyonu).
    /// </summary>
    External = 4
}
