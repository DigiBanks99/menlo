namespace Menlo.Ui.Tests.Utilities;

public sealed class UtilityTests
{
    [Test]
    [Arguments(MnlSplitRatio.WideStart, "wide-start")]
    [Arguments(MnlSplitRatio.Even, "even")]
    public void GivenAnEnumValue_WhenTurnedIntoAToken_ThenItIsKebabCase(MnlSplitRatio value, string expected)
    {
        Css.Token(value).ShouldBe(expected);
    }

    [Test]
    public void GivenBlankAndNullNames_WhenClassesAreJoined_ThenTheyAreLeftOut()
    {
        Css.Classes("mnl-button", null, " ", "extra").ShouldBe("mnl-button extra");
    }

    [Test]
    [Arguments(12000, "ZAR", "R\u00a012\u00a0000.00")]
    [Arguments(0.5, "ZAR", "R\u00a00.50")]
    [Arguments(-1234.567, "USD", "$\u00a0-1\u00a0234.57")]
    [Arguments(10, "NZD", "NZD\u00a010.00")]
    public void GivenAnAmount_WhenFormatted_ThenItHasASymbolSpacesAndTwoDecimals(double amount, string currency, string expected)
    {
        MoneyFormat.Format((decimal)amount, currency).ShouldBe(expected);
    }

    [Test]
    public void GivenNoAmount_WhenFormatted_ThenTheFallbackIsUsed()
    {
        MoneyFormat.FormatOrDefault(null, fallback: "Nothing").ShouldBe("Nothing");
        MoneyFormat.FormatOrDefault(5m).ShouldBe("R\u00a05.00");
    }

    [Test]
    [Arguments(1250.5, "1250.5")]
    [Arguments(1250, "1250")]
    [Arguments(0.125, "0.13")]
    public void GivenAnAmount_WhenMadeEditable_ThenItHasNoGroupingOrTrailingZeros(double amount, string expected)
    {
        MoneyFormat.Editable((decimal)amount).ShouldBe(expected);
    }

    [Test]
    [Arguments("1250.50", 1250.5)]
    [Arguments("1 250,50", 1250.5)]
    [Arguments("R 1,250.50", 1250.5)]
    [Arguments("1,250,000", 1250000)]
    [Arguments("-12", -12)]
    public void GivenWhatAPersonTyped_WhenParsed_ThenSpacesSymbolsAndDecimalCommasAreUnderstood(string text, double expected)
    {
        bool parsed = MoneyFormat.TryParse(text, out decimal amount);

        parsed.ShouldBeTrue();
        amount.ShouldBe((decimal)expected);
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    [Arguments("abc")]
    [Arguments("12.5.1")]
    [Arguments(null)]
    public void GivenSomethingThatIsNotANumber_WhenParsed_ThenItIsRejected(string? text)
    {
        MoneyFormat.TryParse(text, out _).ShouldBeFalse();
    }
}
