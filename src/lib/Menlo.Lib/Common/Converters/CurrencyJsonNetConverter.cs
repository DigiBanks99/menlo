using Newtonsoft.Json;

namespace Menlo.Common.Converters;

internal sealed class CurrencyJsonNetConverter : JsonConverter<Currency>
{
    public override void WriteJson(JsonWriter writer, Currency? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteValue(value.Code);
    }

    public override Currency? ReadJson(
        JsonReader reader,
        Type objectType,
        Currency? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        return reader.TokenType switch
        {
            JsonToken.Null => null,
            JsonToken.String => Currency.FromCode(reader.Value?.ToString() ?? Currency.Zar.Code),
            JsonToken.StartObject => ReadEmptyObject(reader),
            _ => Currency.Zar
        };
    }

    private static Currency ReadEmptyObject(JsonReader reader)
    {
        // Read the start object token
        reader.Read();
        // Ensure the next token is the end object token
        if (reader.TokenType == JsonToken.EndObject)
        {
            return Currency.Zar;
        }
        throw new JsonSerializationException($"Unexpected token '{reader.Value}' when reading Currency.");
    }
}
