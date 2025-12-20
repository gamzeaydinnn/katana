using System;
using System.Globalization;
using System.Text;

namespace Katana.Core.Helpers;

/// <summary>
/// Stok kartı kodu (KartKodu/SKU) normalizasyonu için yardımcı sınıf.
/// Cache lookup, payload oluşturma ve duplicate kontrolü için tek bir canonical form sağlar.
/// </summary>
public static class KartKoduHelper
{
    /// <summary>
    /// KartKodu'nu canonical forma dönüştürür.
    /// Bu fonksiyon:
    /// - Cache build sırasında (Koza'dan gelen kartKodu değerleri)
    /// - Cache lookup sırasında (existence check)
    /// - Payload oluşturma sırasında (outgoing kartKodu)
    /// aynı şekilde kullanılmalıdır.
    /// </summary>
    /// <param name="kartKodu">Ham kart kodu</param>
    /// <returns>Canonical form (uppercase, trimmed, normalized)</returns>
    public static string CanonicalizeKartKodu(string? kartKodu)
    {
        if (string.IsNullOrWhiteSpace(kartKodu))
            return string.Empty;

        var result = kartKodu.Trim();

        // 1. Unicode normalization (FormKC - Compatibility Composition)
        result = result.Normalize(NormalizationForm.FormKC);

        // 2. Ø/ø karakterlerini O'ya çevir (Skandinav karakteri)
        result = result
            .Replace("Ø", "O")
            .Replace("ø", "o");

        // 3. Türkçe karakterleri ASCII karşılıklarına çevir
        result = NormalizeTurkishChars(result);

        // 4. Diğer diacritics'leri kaldır (é→e, ñ→n, etc.)
        result = RemoveDiacritics(result);

        // 5. Birden fazla boşluğu tek boşluğa indir
        result = CollapseSpaces(result);

        // 6. Uppercase (culture-invariant)
        result = result.ToUpperInvariant();

        return result;
    }

    /// <summary>
    /// Payload için kullanılacak kartKodu değerini döndürür.
    /// CanonicalizeKartKodu ile aynı normalizasyonu uygular.
    /// </summary>
    public static string NormalizeForPayload(string? kartKodu)
    {
        return CanonicalizeKartKodu(kartKodu);
    }

    /// <summary>
    /// Cache key için kullanılacak değeri döndürür.
    /// CanonicalizeKartKodu ile aynı normalizasyonu uygular.
    /// </summary>
    public static string NormalizeForCacheKey(string? kartKodu)
    {
        return CanonicalizeKartKodu(kartKodu);
    }

    /// <summary>
    /// Türkçe karakterleri ASCII karşılıklarına çevirir.
    /// İ→I, ı→I, Ş→S, ş→S, Ğ→G, ğ→G, Ü→U, ü→U, Ö→O, ö→O, Ç→C, ç→C
    /// </summary>
    private static string NormalizeTurkishChars(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var sb = new StringBuilder(input.Length);
        foreach (var c in input)
        {
            sb.Append(c switch
            {
                'İ' => 'I',
                'ı' => 'I',
                'Ş' => 'S',
                'ş' => 'S',
                'Ğ' => 'G',
                'ğ' => 'G',
                'Ü' => 'U',
                'ü' => 'U',
                'Ö' => 'O',
                'ö' => 'O',
                'Ç' => 'C',
                'ç' => 'C',
                _ => c
            });
        }
        return sb.ToString();
    }

    /// <summary>
    /// Diacritics (aksanlar) kaldırır: é→e, ñ→n, ä→a, etc.
    /// </summary>
    private static string RemoveDiacritics(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Birden fazla ardışık boşluğu tek boşluğa indirger.
    /// </summary>
    private static string CollapseSpaces(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var parts = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", parts);
    }
}
