using Menlo.Common.Converters;
using Newtonsoft.Json;
using System.Globalization;

namespace Menlo.Common;

public sealed record SimpleMoney(decimal Amount, string Ccy)
{
    public Money ToMoney() => new(Amount, Currency.FromCode(Ccy));
};

public sealed class Money(decimal amount, Currency ccy)
{
    private Money(decimal amount, string ccy) : this(amount, Currency.FromCode(ccy)) { }

    public decimal Amount { get; init; } = amount;

    [JsonConverter(typeof(CurrencyJsonNetConverter))]
    public Currency Ccy { get; init; } = ccy;

    public override string ToString()
    {
        return ToString(CultureInfo.InvariantCulture);
    }

    public string ToString(CultureInfo culture)
    {
        CultureInfo effectiveCulture = CultureInfo.CreateSpecificCulture(culture.Name);
        effectiveCulture.NumberFormat.CurrencySymbol = Ccy.Symbol;
        if (effectiveCulture.Name == "en-ZA")
        {
            effectiveCulture.NumberFormat.NumberDecimalSeparator = ",";
            effectiveCulture.NumberFormat.NumberGroupSeparator = " ";
            effectiveCulture.NumberFormat.CurrencyDecimalSeparator = ",";
            effectiveCulture.NumberFormat.CurrencyGroupSeparator = " ";
        }
        return Amount.ToString("C", effectiveCulture);
    }
}
