namespace Katana.Core.Enums;

/// <summary>
/// Log kategorileri - Logların hangi modülden geldiğini belirtir
/// </summary>
public enum LogCategory
{
    /// <summary>Kimlik doğrulama ve yetkilendirme işlemleri</summary>
    Authentication,
    
    /// <summary>Senkronizasyon işlemleri (Katana/Luca)</summary>
    Sync,
    
    /// <summary>Dış API çağrıları (Katana/Luca API)</summary>
    ExternalAPI,
    
    /// <summary>Kullanıcı aksiyonları (CRUD işlemleri)</summary>
    UserAction,
    
    /// <summary>Sistem hataları ve kritik durumlar</summary>
    System,
    
    /// <summary>Veritabanı işlemleri</summary>
    Database,
    
    /// <summary>İş mantığı ve validasyon</summary>
    Business
}
