using JetBrains.Annotations;

namespace Menlo.Common;

[TestSubject(typeof(Currency))]
public class CurrencyTests
{
    public static TheoryData<string, Currency> TestData = new()
    {
        { "ZAR", Currency.Zar },
        { "USD", Currency.Usd },
        { "EUR", Currency.Eur },
        { "GBP", Currency.Gbp },
        { "AUD", Currency.Aud },
        { "CAD", Currency.Cad },
        { "JPY", Currency.Jpy },
        { "CNY", Currency.Cny },
        { "INR", Currency.Inr }
    };

    [Theory]
    [MemberData(nameof(TestData))]
    public void FromCode_ShouldReturnTheCorrectCurrency(string currencyCode, Currency expectedCurrency)
    {
        Currency actualCurrency = Currency.FromCode(currencyCode);

        actualCurrency.ShouldBe(expectedCurrency);
    }
}
