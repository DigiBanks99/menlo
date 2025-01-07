using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Menlo.Common.Converters;

[TestSubject(typeof(CurrencyJsonNetConverter))]
public class CurrencyJsonNetConverterTest
{
    private readonly CurrencyJsonNetConverter _converter = new();
    private Currency? ConvertJson(string json) =>
        JsonConvert.DeserializeObject<Currency>(json, _converter);

    [Fact]
    public void WriteJson_ShouldWriteJsonCorrectly()
    {
        Currency currency = Currency.Aud;
        string json = JsonConvert.SerializeObject(currency, _converter);

        json.ShouldBe("\"AUD\"");
    }

    [Fact]
    public void ReadJson_ShouldReturnCurrency()
    {
        const string json = "\"AUD\"";
        Currency? currency = ConvertJson(json);

        currency.ShouldNotBeNull().ShouldBe(Currency.Aud);
    }

    [Fact]
    public void ReadJson_ShouldReturnNull_WhenNull()
    {
        const string json = "null";
        Currency? currency = ConvertJson(json);

        currency.ShouldBeNull();
    }

    [Fact]
    public void ReadJson_ShouldReturnDefaultCurrency_WhenInvalidJson()
    {
        const string json = "true";
        Currency? currency = ConvertJson(json);

        currency.ShouldBe(Currency.Zar);
    }

    [Fact]
    public void ReadJson_ShouldReturnDefaultCurrency_WhenEmptyJson()
    {
        const string json = "{}";
        Currency? currency = ConvertJson(json);

        currency.ShouldBe(Currency.Zar);
    }

    [Fact]
    public void ReadJson_ShouldThrowException_WhenUnexpectedToken()
    {
        const string json = """{"unexpected":"token"}""";
        var exception = Should.Throw<JsonSerializationException>(() => ConvertJson(json));

        exception.Message.ShouldBe("Unexpected token 'unexpected' when reading Currency.");
    }
}
