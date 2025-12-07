using System.Globalization;

namespace Katana.Core.Utilities;

/// <summary>
/// Koza tarih formatı yönetimi ve timezone dönüşümleri
/// Koza formatı: dd/MM/yyyy (timezone yok)
/// Katana formatı: ISO8601 (timezone var)
/// </summary>
public static class KozaDateTimeHelper
{
    private const string KOZA_DATE_FORMAT = "dd/MM/yyyy";
    private const string KOZA_DATETIME_FORMAT = "dd/MM/yyyy HH:mm:ss";
    
    // Türkiye timezone (UTC+3)
    private static readonly TimeZoneInfo TurkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");

    /// <summary>
    /// DateTime'ı Koza formatına çevir (dd/MM/yyyy)
    /// </summary>
    public static string ToKozaDateFormat(DateTime dateTime)
    {
        // UTC'den Türkiye saatine çevir
        var turkeyTime = TimeZoneInfo.ConvertTimeFromUtc(dateTime.ToUniversalTime(), TurkeyTimeZone);
        return turkeyTime.ToString(KOZA_DATE_FORMAT, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// DateTime'ı Koza datetime formatına çevir (dd/MM/yyyy HH:mm:ss)
    /// </summary>
    public static string ToKozaDateTimeFormat(DateTime dateTime)
    {
        var turkeyTime = TimeZoneInfo.ConvertTimeFromUtc(dateTime.ToUniversalTime(), TurkeyTimeZone);
        return turkeyTime.ToString(KOZA_DATETIME_FORMAT, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Nullable DateTime'ı Koza formatına çevir
    /// </summary>
    public static string? ToKozaDateFormat(DateTime? dateTime)
    {
        return dateTime.HasValue ? ToKozaDateFormat(dateTime.Value) : null;
    }

    /// <summary>
    /// Koza formatından (dd/MM/yyyy) DateTime'a çevir
    /// </summary>
    public static DateTime FromKozaDateFormat(string kozaDate)
    {
        if (string.IsNullOrWhiteSpace(kozaDate))
        {
            throw new ArgumentException("Koza date cannot be null or empty", nameof(kozaDate));
        }

        var parsed = DateTime.ParseExact(kozaDate, KOZA_DATE_FORMAT, CultureInfo.InvariantCulture);
        
        // Türkiye saatinden UTC'ye çevir
        return TimeZoneInfo.ConvertTimeToUtc(parsed, TurkeyTimeZone);
    }

    /// <summary>
    /// Koza datetime formatından (dd/MM/yyyy HH:mm:ss) DateTime'a çevir
    /// </summary>
    public static DateTime FromKozaDateTimeFormat(string kozaDateTime)
    {
        if (string.IsNullOrWhiteSpace(kozaDateTime))
        {
            throw new ArgumentException("Koza datetime cannot be null or empty", nameof(kozaDateTime));
        }

        var parsed = DateTime.ParseExact(kozaDateTime, KOZA_DATETIME_FORMAT, CultureInfo.InvariantCulture);
        return TimeZoneInfo.ConvertTimeToUtc(parsed, TurkeyTimeZone);
    }

    /// <summary>
    /// ISO8601 string'i Koza formatına çevir
    /// </summary>
    public static string Iso8601ToKozaFormat(string iso8601)
    {
        if (string.IsNullOrWhiteSpace(iso8601))
        {
            throw new ArgumentException("ISO8601 date cannot be null or empty", nameof(iso8601));
        }

        var dateTime = DateTime.Parse(iso8601, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        return ToKozaDateFormat(dateTime);
    }

    /// <summary>
    /// Bugünün tarihini Koza formatında döndür
    /// </summary>
    public static string Today()
    {
        return ToKozaDateFormat(DateTime.UtcNow);
    }

    /// <summary>
    /// Şu anki zamanı Koza datetime formatında döndür
    /// </summary>
    public static string Now()
    {
        return ToKozaDateTimeFormat(DateTime.UtcNow);
    }

    /// <summary>
    /// Tarih aralığı validation
    /// </summary>
    public static bool IsValidDateRange(DateTime startDate, DateTime endDate)
    {
        return startDate <= endDate;
    }

    /// <summary>
    /// Tarih aralığını Koza formatında döndür
    /// </summary>
    public static (string StartDate, string EndDate) ToKozaDateRange(DateTime startDate, DateTime endDate)
    {
        if (!IsValidDateRange(startDate, endDate))
        {
            throw new ArgumentException("Start date must be before or equal to end date");
        }

        return (ToKozaDateFormat(startDate), ToKozaDateFormat(endDate));
    }
}
