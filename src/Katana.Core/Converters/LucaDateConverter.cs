using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Katana.Core.Converters;

/// <summary>
/// Luca API için özel tarih converter'ı
/// DateTime değerlerini dd/MM/yyyy formatında serialize eder
/// </summary>
public class LucaDateConverter : JsonConverter<DateTime>
{
    private const string DateFormat = "dd/MM/yyyy";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateString = reader.GetString();
        if (string.IsNullOrWhiteSpace(dateString))
        {
            return DateTime.MinValue;
        }

        if (DateTime.TryParseExact(dateString, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }

        // Fallback: Try parsing with default parser
        if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fallbackDate))
        {
            return fallbackDate;
        }

        throw new JsonException($"Unable to parse date: {dateString}");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(DateFormat, CultureInfo.InvariantCulture));
    }
}

/// <summary>
/// Nullable DateTime için Luca API converter'ı
/// </summary>
public class LucaNullableDateConverter : JsonConverter<DateTime?>
{
    private const string DateFormat = "dd/MM/yyyy";

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateString = reader.GetString();
        if (string.IsNullOrWhiteSpace(dateString))
        {
            return null;
        }

        if (DateTime.TryParseExact(dateString, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }

        // Fallback: Try parsing with default parser
        if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fallbackDate))
        {
            return fallbackDate;
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString(DateFormat, CultureInfo.InvariantCulture));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
