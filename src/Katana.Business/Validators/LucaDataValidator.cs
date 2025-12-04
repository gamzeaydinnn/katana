using System.Globalization;

namespace Katana.Business.Validators;

/// <summary>
/// Luca API'ye gönderilecek verilerin format validation'ı
/// </summary>
public static class LucaDataValidator
{
    /// <summary>
    /// Cari kodu validation (null ve boş kontrolü)
    /// </summary>
    public static (bool IsValid, string? ErrorMessage) ValidateCariKodu(string? cariKodu, string fieldName = "Cari Kodu")
    {
        if (string.IsNullOrWhiteSpace(cariKodu))
        {
            return (false, $"{fieldName} boş olamaz. Müşteri/Tedarikçi Luca sisteminde tanımlı olmalı.");
        }

        if (cariKodu.Length > 50)
        {
            return (false, $"{fieldName} 50 karakterden uzun olamaz.");
        }

        return (true, null);
    }

    /// <summary>
    /// Stok kodu validation (null ve boş kontrolü)
    /// </summary>
    public static (bool IsValid, string? ErrorMessage) ValidateStokKodu(string? stokKodu, string productName = "")
    {
        if (string.IsNullOrWhiteSpace(stokKodu))
        {
            var productInfo = !string.IsNullOrEmpty(productName) ? $" ({productName})" : "";
            return (false, $"Stok kodu{productInfo} boş olamaz. Ürün Luca sisteminde tanımlı olmalı.");
        }

        if (stokKodu.Length > 100)
        {
            return (false, $"Stok kodu 100 karakterden uzun olamaz.");
        }

        return (true, null);
    }

    /// <summary>
    /// Decimal değer validation (Luca API 2 ondalık basamak kabul eder)
    /// </summary>
    public static (bool IsValid, string? ErrorMessage) ValidateDecimalPrecision(decimal value, string fieldName = "Değer")
    {
        if (value < 0)
        {
            return (false, $"{fieldName} negatif olamaz.");
        }

        // Luca API genellikle max 2 ondalık basamak kabul eder
        var rounded = Math.Round(value, 2);
        if (value != rounded)
        {
            return (false, $"{fieldName} en fazla 2 ondalık basamak içerebilir. Gönderilecek: {rounded:F2}");
        }

        // Max değer kontrolü (Luca'da overflow önleme)
        if (value > 999999999.99M)
        {
            return (false, $"{fieldName} çok büyük. Maksimum: 999,999,999.99");
        }

        return (true, null);
    }

    /// <summary>
    /// Tarih validation (Luca API geçmiş/gelecek tarih kısıtlamaları)
    /// </summary>
    public static (bool IsValid, string? ErrorMessage) ValidateDate(DateTime? date, string fieldName = "Tarih", bool allowFuture = true)
    {
        if (!date.HasValue)
        {
            return (false, $"{fieldName} boş olamaz.");
        }

        // Çok eski tarih kontrolü (örn: 1900'den önce)
        if (date.Value.Year < 1900)
        {
            return (false, $"{fieldName} 1900 yılından önce olamaz.");
        }

        // Gelecek tarih kontrolü
        if (!allowFuture && date.Value > DateTime.UtcNow)
        {
            return (false, $"{fieldName} gelecek bir tarih olamaz.");
        }

        // Çok ileri gelecek kontrolü (örn: 10 yıldan fazla)
        if (date.Value > DateTime.UtcNow.AddYears(10))
        {
            return (false, $"{fieldName} 10 yıldan fazla ileride olamaz.");
        }

        return (true, null);
    }

    /// <summary>
    /// Para birimi validation
    /// </summary>
    public static (bool IsValid, string? ErrorMessage) ValidateCurrency(string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            return (false, "Para birimi boş olamaz.");
        }

        var validCurrencies = new[] { "TRY", "USD", "EUR", "GBP" };
        if (!validCurrencies.Contains(currency.ToUpper()))
        {
            return (false, $"Geçersiz para birimi: {currency}. Desteklenen: {string.Join(", ", validCurrencies)}");
        }

        return (true, null);
    }

    /// <summary>
    /// Belge numarası validation
    /// </summary>
    public static (bool IsValid, string? ErrorMessage) ValidateDocumentNo(string? documentNo, string fieldName = "Belge No")
    {
        if (string.IsNullOrWhiteSpace(documentNo))
        {
            return (false, $"{fieldName} boş olamaz.");
        }

        if (documentNo.Length > 50)
        {
            return (false, $"{fieldName} 50 karakterden uzun olamaz.");
        }

        // Özel karakterler kontrolü (sadece alfanumerik ve - _ / kabul et)
        if (!System.Text.RegularExpressions.Regex.IsMatch(documentNo, @"^[a-zA-Z0-9\-_/]+$"))
        {
            return (false, $"{fieldName} sadece harf, rakam, tire, alt çizgi ve slash içerebilir.");
        }

        return (true, null);
    }

    /// <summary>
    /// Miktar validation (pozitif ve makul aralıkta olmalı)
    /// </summary>
    public static (bool IsValid, string? ErrorMessage) ValidateQuantity(decimal quantity, string fieldName = "Miktar")
    {
        if (quantity <= 0)
        {
            return (false, $"{fieldName} 0'dan büyük olmalı.");
        }

        if (quantity > 1000000)
        {
            return (false, $"{fieldName} çok büyük (max: 1,000,000).");
        }

        return (true, null);
    }

    /// <summary>
    /// Vergi oranı validation (0-1 arası veya 0-100 arası)
    /// </summary>
    public static (bool IsValid, string? ErrorMessage) ValidateTaxRate(decimal taxRate)
    {
        // 0-1 arası (0.18 = %18) veya 0-100 arası (18 = %18)
        if (taxRate < 0 || (taxRate > 1 && taxRate <= 100))
        {
            return (true, null);
        }

        if (taxRate > 100)
        {
            return (false, "Vergi oranı %100'den fazla olamaz.");
        }

        if (taxRate < 0)
        {
            return (false, "Vergi oranı negatif olamaz.");
        }

        return (true, null);
    }

    /// <summary>
    /// Tüm validationları tek seferde çalıştır ve hataları topla
    /// </summary>
    public static (bool IsValid, List<string> Errors) ValidateAll(params (bool IsValid, string? ErrorMessage)[] validations)
    {
        var errors = new List<string>();

        foreach (var (isValid, errorMessage) in validations)
        {
            if (!isValid && !string.IsNullOrEmpty(errorMessage))
            {
                errors.Add(errorMessage);
            }
        }

        return (errors.Count == 0, errors);
    }
}
