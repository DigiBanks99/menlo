using JetBrains.Annotations;
using System.Globalization;

namespace Menlo.Common;

[TestSubject(typeof(Money))]
public class MoneyTests
{
    [Theory]
    [InlineData("en-US", 1234.56, "AUD", "$1,234.56")]
    [InlineData("en-ZA", 1234.56, "AUD", "$1 234,56")]
    [InlineData("en-GB", 1234.56, "AUD", "$1,234.56")]
    [InlineData("af-ZA", 1234.56, "AUD", "$1 234,56")]
    [InlineData("en-US", 1234.56, "CAD", "$1,234.56")]
    [InlineData("en-ZA", 1234.56, "CAD", "$1 234,56")]
    [InlineData("en-GB", 1234.56, "CAD", "$1,234.56")]
    [InlineData("af-ZA", 1234.56, "CAD", "$1 234,56")]
    [InlineData("en-US", 1234.56, "EUR", "€1,234.56")]
    [InlineData("en-ZA", 1234.56, "EUR", "€1 234,56")]
    [InlineData("en-GB", 1234.56, "EUR", "€1,234.56")]
    [InlineData("af-ZA", 1234.56, "EUR", "€1 234,56")]
    [InlineData("en-US", 1234.56, "USD", "$1,234.56")]
    [InlineData("en-ZA", 1234.56, "USD", "$1 234,56")]
    [InlineData("en-GB", 1234.56, "USD", "$1,234.56")]
    [InlineData("af-ZA", 1234.56, "USD", "$1 234,56")]
    [InlineData("en-US", 1234.56, "ZAR", "R1,234.56")]
    [InlineData("en-ZA", 1234.56, "ZAR", "R1 234,56")]
    [InlineData("en-GB", 1234.56, "ZAR", "R1,234.56")]
    [InlineData("af-ZA", 1234.56, "ZAR", "R1 234,56")]
    public void ToString_Should_FormatTheCurrencyByCulture(string culture, decimal value, string ccy, string expected)
    {
        Money money = new(value, Currency.FromCode(ccy));

        money.ToString(CultureInfo.GetCultureInfo(culture)).ShouldBe(expected);
    }

    [Theory]
    [InlineData(1234.56, "AUD", "$1,234.56")]
    [InlineData(1234.56, "CAD", "$1,234.56")]
    [InlineData(1234.56, "EUR", "€1,234.56")]
    [InlineData(1234.56, "USD", "$1,234.56")]
    [InlineData(1234.56, "ZAR", "R1,234.56")]
    public void ToString_Should_FormatTheCurrencyByInvariantCulture(decimal value, string ccy, string expected)
    {
        Money money = new(value, Currency.FromCode(ccy));

        money.ToString().ShouldBe(expected);
    }
}
