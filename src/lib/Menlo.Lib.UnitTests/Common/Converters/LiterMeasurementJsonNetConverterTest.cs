using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Menlo.Common.Converters;

[TestSubject(typeof(LiterMeasurementJsonNetConverter))]
public class LiterMeasurementJsonNetConverterTest
{
    private readonly LiterMeasurementJsonNetConverter _converter = new();
    private LiterMeasurement? ConvertJson(string json) =>
        JsonConvert.DeserializeObject<LiterMeasurement>(json, _converter);

    [Fact]
    public void WriteJson_ShouldWriteJsonCorrectly()
    {
        LiterMeasurement measurement = new(123.45m);
        string json = JsonConvert.SerializeObject(measurement, _converter);

        json.ShouldBe("123.45");
    }

    [Fact]
    public void ReadJson_ShouldReturnLiterMeasurement()
    {
        const string json = "123.45";
        LiterMeasurement? measurement = ConvertJson(json);

        measurement.ShouldNotBeNull().Value.ShouldBe(123.45m);
    }

    [Fact]
    public void ReadJson_ShouldReturnNull_WhenNull()
    {
        const string json = "null";
        LiterMeasurement? measurement = ConvertJson(json);

        measurement.ShouldBeNull();
    }

    [Fact]
    public void ReadJson_ShouldReturnDefaultLiterMeasurement_WhenInvalidJson()
    {
        const string json = "true";
        LiterMeasurement? measurement = ConvertJson(json);

        measurement.ShouldNotBeNull().Value.ShouldBe(decimal.Zero);
        measurement.ShouldBe(LiterMeasurement.Zero);
    }

    [Fact]
    public void ReadJson_ShouldReturnDefaultLiterMeasurement_WhenEmptyJson()
    {
        const string json = "{}";
        LiterMeasurement? measurement = ConvertJson(json);

        measurement.ShouldNotBeNull().Value.ShouldBe(decimal.Zero);
        measurement.ShouldBe(LiterMeasurement.Zero);
    }

    [Fact]
    public void ReadJson_ShouldThrowException_WhenUnexpectedToken()
    {
        const string json = """{"unexpected":"token"}""";
        var exception = Should.Throw<JsonSerializationException>(() => ConvertJson(json));

        exception.Message.ShouldBe("Unexpected token 'unexpected' when reading LiterMeasurement.");
    }
}
