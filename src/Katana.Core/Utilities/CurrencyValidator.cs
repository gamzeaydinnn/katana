using System;
using System.Collections.Generic;
using System.Linq;

namespace Katana.Core.Utilities;

/// <summary>
/// Validates and normalizes currency codes (ISO 4217)
/// </summary>
public static class CurrencyValidator
{
    private static readonly HashSet<string> SupportedCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "TRY", // Turkish Lira
        "USD", // US Dollar
        "EUR", // Euro
        "GBP", // British Pound
        "CHF", // Swiss Franc
        "JPY", // Japanese Yen
        "CNY", // Chinese Yuan
        "RUB", // Russian Ruble
        "AED", // UAE Dirham
        "SAR"  // Saudi Riyal
    };
    
    /// <summary>
    /// Checks if currency code is valid (ISO 4217 format)
    /// </summary>
    public static bool IsValidCurrency(string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            return false;
        }
        
        var normalized = currency.Trim().ToUpperInvariant();
        return normalized.Length == 3 && SupportedCurrencies.Contains(normalized);
    }
    
    /// <summary>
    /// Normalizes currency code to uppercase ISO 4217 format
    /// </summary>
    public static string NormalizeCurrency(string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            return "TRY"; // Default to Turkish Lira
        }
        
        var normalized = currency.Trim().ToUpperInvariant();
        
        // Handle common variations
        return normalized switch
        {
            "TURKISH LIRA" => "TRY",
            "LIRA" => "TRY",
            "TL" => "TRY",
            "DOLLAR" => "USD",
            "DOLAR" => "USD",
            "EURO" => "EUR",
            "POUND" => "GBP",
            _ => normalized.Length == 3 ? normalized : "TRY"
        };
    }
    
    /// <summary>
    /// Validates currency and returns normalized value or throws exception
    /// </summary>
    public static string Validate(string? currency)
    {
        var normalized = NormalizeCurrency(currency);
        
        if (!IsValidCurrency(normalized))
        {
            throw new ArgumentException($"Invalid currency code: {currency}. Supported currencies: {string.Join(", ", SupportedCurrencies)}");
        }
        
        return normalized;
    }
    
    /// <summary>
    /// Validates currency and throws exception if invalid
    /// </summary>
    public static void ValidateOrThrow(string? currency)
    {
        if (!IsValidCurrency(currency))
        {
            throw new ArgumentException($"Invalid currency code: {currency}. Must be a valid ISO 4217 code (e.g., TRY, USD, EUR)");
        }
    }
    
    /// <summary>
    /// Gets list of supported currency codes
    /// </summary>
    public static IReadOnlyList<string> GetSupportedCurrencies()
    {
        return SupportedCurrencies.OrderBy(c => c).ToList();
    }
    
    /// <summary>
    /// Gets currency or default if invalid
    /// </summary>
    public static string GetOrDefault(string? currency, string defaultCurrency = "TRY")
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            return defaultCurrency;
        }
        
        var normalized = NormalizeCurrency(currency);
        return IsValidCurrency(normalized) ? normalized : defaultCurrency;
    }
}
