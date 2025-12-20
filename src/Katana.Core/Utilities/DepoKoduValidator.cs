using System.Text.RegularExpressions;

namespace Katana.Core.Utilities;

/// <summary>
/// Depo kodu validation ve normalizasyon
/// </summary>
public static class DepoKoduValidator
{
    private const string DEFAULT_DEPO_KODU = "001";
    private static readonly Regex DepoKoduPattern = new Regex(@"^[A-Z0-9]{1,10}$", RegexOptions.Compiled);

    /// <summary>
    /// Depo kodunun format olarak geçerli olup olmadığını kontrol et
    /// </summary>
    public static bool IsValidFormat(string? depoKodu)
    {
        if (string.IsNullOrWhiteSpace(depoKodu))
        {
            return false;
        }

        // Koza depo kodları genellikle 3 haneli sayısal veya alfanumerik
        // Örn: "001", "002", "MRK", "A01"
        return DepoKoduPattern.IsMatch(depoKodu.Trim().ToUpperInvariant());
    }

    /// <summary>
    /// Depo kodunu normalize et (trim, uppercase)
    /// </summary>
    public static string Normalize(string? depoKodu)
    {
        if (string.IsNullOrWhiteSpace(depoKodu))
        {
            return DEFAULT_DEPO_KODU;
        }

        var normalized = depoKodu.Trim().ToUpperInvariant();

        // Geçerli format değilse default kullan
        if (!IsValidFormat(normalized))
        {
            return DEFAULT_DEPO_KODU;
        }

        return normalized;
    }

    /// <summary>
    /// Depo kodunu validate et ve hata mesajı döndür
    /// </summary>
    public static (bool IsValid, string? ErrorMessage) Validate(string? depoKodu)
    {
        if (string.IsNullOrWhiteSpace(depoKodu))
        {
            return (false, "Depo kodu boş olamaz");
        }

        var normalized = depoKodu.Trim();

        if (normalized.Length > 10)
        {
            return (false, "Depo kodu maksimum 10 karakter olabilir");
        }

        if (!DepoKoduPattern.IsMatch(normalized.ToUpperInvariant()))
        {
            return (false, "Depo kodu sadece harf ve rakam içerebilir");
        }

        return (true, null);
    }

    /// <summary>
    /// Depo kodu boş veya geçersizse exception fırlat
    /// </summary>
    public static void ValidateOrThrow(string? depoKodu, string paramName = "depoKodu")
    {
        var (isValid, errorMessage) = Validate(depoKodu);
        
        if (!isValid)
        {
            throw new ArgumentException(errorMessage, paramName);
        }
    }

    /// <summary>
    /// Depo kodu boş veya geçersizse default değer döndür
    /// </summary>
    public static string GetOrDefault(string? depoKodu, string? defaultValue = null)
    {
        if (string.IsNullOrWhiteSpace(depoKodu))
        {
            return defaultValue ?? DEFAULT_DEPO_KODU;
        }

        var normalized = Normalize(depoKodu);
        
        if (!IsValidFormat(normalized))
        {
            return defaultValue ?? DEFAULT_DEPO_KODU;
        }

        return normalized;
    }
}
