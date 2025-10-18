namespace Katana.Core.Enums;

/// <summary>
/// Hata türlerini sınıflandırmak için kullanılır.
/// </summary>
public enum ErrorType
{
    /// <summary>
    /// Bilinmeyen veya sınıflandırılamayan hata.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Doğrulama hatası (örnek: eksik alan, geçersiz format)
    /// </summary>
    Validation = 1,

    /// <summary>
    /// API çağrılarında (Katana, Luca vb.) oluşan hata.
    /// </summary>
    Api = 2,

    /// <summary>
    /// Veritabanı kaynaklı hatalar.
    /// </summary>
    Database = 3,

    /// <summary>
    /// Senkronizasyon veya ETL işlemlerindeki hata.
    /// </summary>
    Sync = 4,

    /// <summary>
    /// Yetkilendirme, erişim veya güvenlik hatası.
    /// </summary>
    Security = 5,

    /// <summary>
    /// Sistem veya yapılandırma hatası.
    /// </summary>
    System = 6
}
