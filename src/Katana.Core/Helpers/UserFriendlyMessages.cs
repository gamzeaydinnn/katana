namespace Katana.Core.Helpers;

/// <summary>
/// Teknik hata mesajlarını kullanıcı dostu mesajlara çevirir
/// </summary>
public static class UserFriendlyMessages
{
    private static readonly Dictionary<string, (string Title, string Description, string Suggestion)> ErrorMappings = new()
    {
        // Login/Oturum Hataları
        ["login olunmalı"] = ("Oturum Süresi Doldu", "Luca sistemine olan bağlantı süresi doldu.", "Lütfen 'Bağlantıyı Yenile' butonuna tıklayın veya birkaç dakika sonra tekrar deneyin."),
        ["code=1001"] = ("Oturum Süresi Doldu", "Luca sistemine olan bağlantı süresi doldu.", "Lütfen 'Bağlantıyı Yenile' butonuna tıklayın."),
        ["unauthorized"] = ("Yetkilendirme Hatası", "Bu işlem için yetkiniz bulunmuyor.", "Lütfen sistem yöneticinize başvurun."),
        ["forbidden"] = ("Erişim Engellendi", "Bu alana erişim izniniz yok.", "Lütfen sistem yöneticinize başvurun."),
        
        // Bağlantı Hataları
        ["timeout"] = ("Bağlantı Zaman Aşımı", "Sunucu yanıt vermedi.", "İnternet bağlantınızı kontrol edin ve tekrar deneyin."),
        ["connection refused"] = ("Bağlantı Kurulamadı", "Sunucuya bağlanılamıyor.", "Lütfen birkaç dakika sonra tekrar deneyin."),
        ["network"] = ("Ağ Hatası", "İnternet bağlantısında sorun var.", "İnternet bağlantınızı kontrol edin."),
        ["ssl"] = ("Güvenli Bağlantı Hatası", "Güvenli bağlantı kurulamadı.", "Lütfen sistem yöneticinize başvurun."),
        
        // Veri Hataları
        ["duplicate"] = ("Kayıt Zaten Mevcut", "Bu kayıt daha önce oluşturulmuş.", "Mevcut kaydı güncelleyebilirsiniz."),
        ["kart kodu daha önce kullanılmış"] = ("Ürün Zaten Kayıtlı", "Bu ürün kodu Luca'da zaten mevcut.", "Ürün zaten aktarılmış, güncelleme yapabilirsiniz."),
        ["not found"] = ("Kayıt Bulunamadı", "Aranan kayıt sistemde mevcut değil.", "Kayıt silinmiş olabilir veya henüz oluşturulmamış olabilir."),
        ["validation"] = ("Geçersiz Veri", "Girilen bilgilerde hata var.", "Lütfen bilgileri kontrol edip tekrar deneyin."),
        ["required"] = ("Eksik Bilgi", "Zorunlu alanlar doldurulmamış.", "Lütfen tüm zorunlu alanları doldurun."),
        
        // Sistem Hataları
        ["internal server"] = ("Sistem Hatası", "Beklenmeyen bir hata oluştu.", "Lütfen birkaç dakika sonra tekrar deneyin."),
        ["database"] = ("Veritabanı Hatası", "Veri kaydedilemedi.", "Lütfen tekrar deneyin veya destek ekibine başvurun."),
        ["snapshot"] = ("Geçici Hata", "İşlem sırasında geçici bir sorun oluştu.", "Sayfa yenilenerek tekrar deneyin."),
        
        // Katana Hataları
        ["katana api"] = ("Katana Bağlantı Hatası", "Katana sistemine bağlanılamadı.", "Katana API ayarlarını kontrol edin."),
        ["rate limit"] = ("Çok Fazla İstek", "Kısa sürede çok fazla istek gönderildi.", "Lütfen birkaç dakika bekleyip tekrar deneyin."),
        
        // Luca Hataları
        ["luca"] = ("Luca Bağlantı Hatası", "Luca sistemine bağlanılamadı.", "Luca oturum bilgilerini kontrol edin."),
    };

    /// <summary>
    /// Teknik hata mesajını kullanıcı dostu mesaja çevirir
    /// </summary>
    public static UserFriendlyError TranslateError(string? technicalMessage, string? errorCode = null)
    {
        if (string.IsNullOrWhiteSpace(technicalMessage))
        {
            return new UserFriendlyError
            {
                Title = "Bilinmeyen Hata",
                Description = "Bir hata oluştu ancak detayı belirlenemedi.",
                Suggestion = "Lütfen sayfayı yenileyip tekrar deneyin.",
                TechnicalDetails = technicalMessage
            };
        }

        var lowerMessage = technicalMessage.ToLowerInvariant();
        
        foreach (var mapping in ErrorMappings)
        {
            if (lowerMessage.Contains(mapping.Key))
            {
                return new UserFriendlyError
                {
                    Title = mapping.Value.Title,
                    Description = mapping.Value.Description,
                    Suggestion = mapping.Value.Suggestion,
                    TechnicalDetails = technicalMessage,
                    ErrorCode = errorCode
                };
            }
        }

        // Varsayılan mesaj
        return new UserFriendlyError
        {
            Title = "İşlem Başarısız",
            Description = "İşlem sırasında bir hata oluştu.",
            Suggestion = "Lütfen tekrar deneyin. Sorun devam ederse destek ekibine başvurun.",
            TechnicalDetails = technicalMessage,
            ErrorCode = errorCode
        };
    }

    /// <summary>
    /// Hata koduna göre kısa açıklama döner
    /// </summary>
    public static string GetShortDescription(string? technicalMessage)
    {
        var error = TranslateError(technicalMessage);
        return error.Title;
    }
}

public class UserFriendlyError
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Suggestion { get; set; } = "";
    public string? TechnicalDetails { get; set; }
    public string? ErrorCode { get; set; }
}
