using Newtonsoft.Json;

namespace Menlo.Common.Converters;

internal sealed class LiterMeasurementJsonNetConverter : JsonConverter<LiterMeasurement>
{
    public override void WriteJson(JsonWriter writer, LiterMeasurement? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteValue(value.Value);
    }

    public override LiterMeasurement? ReadJson(
        JsonReader reader,
        Type objectType,
        LiterMeasurement? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        return reader.TokenType switch
        {
            JsonToken.Null => null,
            JsonToken.Float or JsonToken.Integer => new LiterMeasurement(Convert.ToDecimal(reader.Value)),
            JsonToken.String when decimal.TryParse(reader.Value as string, out decimal result) =>
                new LiterMeasurement(result),
            JsonToken.StartObject => ReadEmptyObject(reader),
            _ => existingValue ?? LiterMeasurement.Zero
        };
    }

    private static LiterMeasurement ReadEmptyObject(JsonReader reader)
    {
        // Read the start object token
        reader.Read();
        // Ensure the next token is the end object token
        if (reader.TokenType == JsonToken.EndObject)
        {
            return LiterMeasurement.Zero;
        }
        throw new JsonSerializationException($"Unexpected token '{reader.Value}' when reading LiterMeasurement.");
    }
}
