using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CatalogCart.Web.Api.Json;


public sealed class FlexibleDecimalConverter : JsonConverter<decimal?>
{
    public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetDecimal(),
            JsonTokenType.String => decimal.TryParse(reader.GetString(), NumberStyles.Number,
                CultureInfo.InvariantCulture, out var value) ? value : null,
            _ => SkipAndReturnNull<decimal>(ref reader),
        };

    public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
    {
        if (value is null) writer.WriteNullValue();
        else writer.WriteNumberValue(value.Value);
    }

    internal static T? SkipAndReturnNull<T>(ref Utf8JsonReader reader) where T : struct
    {
        reader.Skip();
        return null;
    }
}

public sealed class FlexibleIntConverter : JsonConverter<int?>
{
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.Number when reader.TryGetInt32(out var whole) => whole,
            JsonTokenType.Number => (int)reader.GetDouble(),
            JsonTokenType.String => decimal.TryParse(reader.GetString(), NumberStyles.Number,
                CultureInfo.InvariantCulture, out var value) ? (int)value : null,
            _ => FlexibleDecimalConverter.SkipAndReturnNull<int>(ref reader),
        };

    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        if (value is null) writer.WriteNullValue();
        else writer.WriteNumberValue(value.Value);
    }
}

public sealed class FlexibleStringConverter : JsonConverter<string?>
{
    // A string is kept; a number becomes its text; anything else (object, array, bool) is skipped as null.
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return reader.GetString();
            case JsonTokenType.Number:
                return reader.GetDecimal().ToString(CultureInfo.InvariantCulture);
            default:
                reader.Skip();
                return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value is null) writer.WriteNullValue();
        else writer.WriteStringValue(value);
    }
}

public sealed class FlexibleBoolConverter : JsonConverter<bool?>
{
    public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Number => reader.GetDouble() != 0,
            JsonTokenType.String => reader.GetString()?.Trim().ToLowerInvariant() switch
            {
                "true" or "1" or "yes" or "instock" => true,
                "false" or "0" or "no" or "outofstock" => false,
                _ => null,
            },
            _ => FlexibleDecimalConverter.SkipAndReturnNull<bool>(ref reader),
        };

    public override void Write(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
    {
        if (value is null) writer.WriteNullValue();
        else writer.WriteBooleanValue(value.Value);
    }
}
