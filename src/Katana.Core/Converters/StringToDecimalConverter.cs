using System.Text.Json;
using System.Text.Json.Serialization;

namespace Katana.Core.Converters;

/// <summary>
/// JSON converter that handles decimal values that may come as strings from Katana API.
/// Katana API sometimes returns numeric values as strings (e.g., "123.45" instead of 123.45).
/// </summary>
public class StringToDecimalConverter : JsonConverter<decimal?>
{
    public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                return reader.GetDecimal();
            case JsonTokenType.String:
                var stringValue = reader.GetString();
                if (string.IsNullOrWhiteSpace(stringValue))
                    return null;
                if (decimal.TryParse(stringValue, System.Globalization.NumberStyles.Any, 
                    System.Globalization.CultureInfo.InvariantCulture, out var result))
                    return result;
                return null;
            case JsonTokenType.Null:
                return null;
            default:
                return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteNumberValue(value.Value);
        else
            writer.WriteNullValue();
    }
}

/// <summary>
/// Non-nullable version for required decimal fields
/// </summary>
public class StringToDecimalNonNullableConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                return reader.GetDecimal();
            case JsonTokenType.String:
                var stringValue = reader.GetString();
                if (decimal.TryParse(stringValue, System.Globalization.NumberStyles.Any, 
                    System.Globalization.CultureInfo.InvariantCulture, out var result))
                    return result;
                return 0m;
            default:
                return 0m;
        }
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}
