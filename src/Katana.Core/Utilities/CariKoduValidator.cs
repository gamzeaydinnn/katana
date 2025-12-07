using System.Text.RegularExpressions;

namespace Katana.Core.Utilities;

/// <summary>
/// Cari kodu validation ve normalizasyon
/// ÖNEMLI: Leading zero kaybını önlemek için string olarak sakla!
/// </summary>
public static class CariKoduValidator
{
    private const string DEFAULT_CUSTOMER_PREFIX = "CK";
    private const string DEFAULT_SUPPLIER_PREFIX = "TED";
    
    // Cari kodları alfanumerik + tire olabilir: "CK-123", "000.00000001", "MUS-001"
    private static readonly Regex CariKoduPattern = new Regex(@"^[A-Z0-9\.\-]{1,50}$", RegexOptions.Compiled);

    /// <summary>
    /// Cari kodunun format olarak geçerli olup olmadığını kontrol et
    /// </summary>
    public static bool IsValidFormat(string? cariKodu)
    {
        if (string.IsNullOrWhiteSpace(cariKodu))
        {
            return false;
        }

        return CariKoduPattern.IsMatch(cariKodu.Trim().ToUpperInvariant());
    }

    /// <summary>
    /// Cari kodunu normalize et (trim, uppercase)
    /// ÖNEMLI: Leading zero'ları korur!
    /// </summary>
    public static string Normalize(string? cariKodu)
    {
        if (string.IsNullOrWhiteSpace(cariKodu))
        {
            return string.Empty;
        }

        // Trim yap ama leading zero'ları koru
        var normalized = cariKodu.Trim().ToUpperInvariant();

        return normalized;
    }

    /// <summary>
    /// Cari kodunu validate et ve hata mesajı döndür
    /// </summary>
    public static (bool IsValid, string? ErrorMessage) Validate(string? cariKodu)
    {
        if (string.IsNullOrWhiteSpace(cariKodu))
        {
            return (false, "Cari kodu boş olamaz");
        }

        var normalized = cariKodu.Trim();

        if (normalized.Length > 50)
        {
            return (false, "Cari kodu maksimum 50 karakter olabilir");
        }

        if (!CariKoduPattern.IsMatch(normalized.ToUpperInvariant()))
        {
            return (false, "Cari kodu sadece harf, rakam, nokta ve tire içerebilir");
        }

        return (true, null);
    }

    /// <summary>
    /// Cari kodu boş veya geçersizse exception fırlat
    /// </summary>
    public static void ValidateOrThrow(string? cariKodu, string paramName = "cariKodu")
    {
        var (isValid, errorMessage) = Validate(cariKodu);
        
        if (!isValid)
        {
            throw new ArgumentException(errorMessage, paramName);
        }
    }

    /// <summary>
    /// Customer için default cari kodu oluştur
    /// </summary>
    public static string GenerateCustomerCode(int customerId)
    {
        return $"{DEFAULT_CUSTOMER_PREFIX}-{customerId}";
    }

    /// <summary>
    /// Supplier için default cari kodu oluştur
    /// </summary>
    public static string GenerateSupplierCode(string supplierId)
    {
        return $"{DEFAULT_SUPPLIER_PREFIX}-{supplierId}";
    }

    /// <summary>
    /// Kategorili kod formatını kontrol et (örn: "000.00000001")
    /// </summary>
    public static bool IsKategoriliKod(string? cariKodu)
    {
        if (string.IsNullOrWhiteSpace(cariKodu))
        {
            return false;
        }

        // Kategorili kod formatı: XXX.XXXXXXXX (3 hane nokta 8 hane)
        var pattern = new Regex(@"^\d{3}\.\d{8}$");
        return pattern.IsMatch(cariKodu.Trim());
    }

    /// <summary>
    /// Leading zero'ları koru - string olarak sakla uyarısı
    /// </summary>
    public static string PreserveLeadingZeros(string? cariKodu)
    {
        if (string.IsNullOrWhiteSpace(cariKodu))
        {
            return string.Empty;
        }

        // Trim yap ama leading zero'ları koru
        // ÖNEMLI: int'e çevirme, string olarak sakla!
        return cariKodu.Trim();
    }

    /// <summary>
    /// Cari kodu karşılaştırması (case-insensitive, leading zero aware)
    /// </summary>
    public static bool AreEqual(string? code1, string? code2)
    {
        if (string.IsNullOrWhiteSpace(code1) && string.IsNullOrWhiteSpace(code2))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(code1) || string.IsNullOrWhiteSpace(code2))
        {
            return false;
        }

        // Case-insensitive karşılaştırma
        // Leading zero'lar korunur
        return string.Equals(
            code1.Trim(), 
            code2.Trim(), 
            StringComparison.OrdinalIgnoreCase);
    }
}
