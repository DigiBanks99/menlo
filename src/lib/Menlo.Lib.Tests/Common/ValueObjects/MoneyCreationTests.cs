using CSharpFunctionalExtensions;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.Errors;
using Menlo.Lib.Common.ValueObjects;
using Shouldly;

namespace Menlo.Lib.Tests.Common.ValueObjects;

/// <summary>
/// Tests for Money Value Object creation and validation.
/// Test Cases: TC-MON-001 through TC-MON-005
/// </summary>
public sealed class MoneyCreationTests
{
    /// <summary>
    /// TC-MON-001: Create Money with valid amount and currency
    /// </summary>
    [Fact]
    public void GivenValidAmountAndCurrency_WhenCreatingMoney()
    {
        // Arrange
        decimal amount = 100.50m;
        string currency = "ZAR";

        // Act
        Result<Money, Error> result = Money.Create(amount, currency);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveCorrectAmount(result, amount);
        ItShouldHaveCorrectCurrency(result, currency);
    }

    /// <summary>
    /// TC-MON-002: Create Money with negative amount (allowed for budgeting)
    /// </summary>
    [Fact]
    public void GivenNegativeAmount_WhenCreatingMoney()
    {
        // Arrange
        decimal amount = -50.25m;
        string currency = "USD";

        // Act
        Result<Money, Error> result = Money.Create(amount, currency);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveCorrectAmount(result, amount);
    }

    /// <summary>
    /// TC-MON-003: Create Money with zero amount
    /// </summary>
    [Fact]
    public void GivenZeroAmount_WhenCreatingMoney()
    {
        // Arrange
        string currency = "EUR";

        // Act
        Result<Money, Error> result = Money.Create(0m, currency);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveCorrectAmount(result, 0m);
    }

    /// <summary>
    /// TC-MON-003: Create Money.Zero helper
    /// </summary>
    [Fact]
    public void GivenCurrency_WhenCreatingZeroMoney()
    {
        // Arrange
        string currency = "GBP";

        // Act
        Money money = Money.Zero(currency);

        // Assert
        ItShouldHaveZeroAmount(money);
        ItShouldHaveUpperCaseCurrency(money, currency);
    }

    /// <summary>
    /// TC-MON-004: Reject empty or null currency
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenEmptyOrNullCurrency_WhenCreatingMoney(string currency)
    {
        // Arrange
        decimal amount = 100m;

        // Act
        Result<Money, Error> result = Money.Create(amount, currency);

        // Assert
        ItShouldFailWithEmptyCurrencyError(result);
    }

    /// <summary>
    /// TC-MON-004: Reject null currency
    /// </summary>
    [Fact]
    public void GivenNullCurrency_WhenCreatingMoney()
    {
        // Arrange
        decimal amount = 100m;
        string currency = null!;

        // Act
        Result<Money, Error> result = Money.Create(amount, currency);

        // Assert
        ItShouldFailWithEmptyCurrencyError(result);
    }

    /// <summary>
    /// TC-MON-005: Currency should be uppercase
    /// </summary>
    [Fact]
    public void GivenLowercaseCurrency_WhenCreatingMoney()
    {
        // Arrange
        decimal amount = 100m;
        string currency = "usd";

        // Act
        Result<Money, Error> result = Money.Create(amount, currency);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveUpperCaseCurrency(result.Value, "USD");
    }

    /// <summary>
    /// TC-MON-005: Amount should be rounded to 2 decimal places
    /// </summary>
    [Theory]
    [InlineData(100.125, 100.12)]  // Rounds down (banker's rounding)
    [InlineData(100.135, 100.14)]  // Rounds up
    [InlineData(100.115, 100.12)]  // Rounds to even (banker's rounding)
    public void GivenAmountWithMoreThanTwoDecimals_WhenCreatingMoney(decimal input, decimal expected)
    {
        // Arrange
        string currency = "ZAR";

        // Act
        Result<Money, Error> result = Money.Create(input, currency);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveCorrectAmount(result, expected);
    }

    /// <summary>
    /// TC-MON-005: Test Money equality
    /// </summary>
    [Fact]
    public void GivenSameAmountAndCurrency_WhenComparingMoney()
    {
        // Arrange
        Money money1 = Money.Create(100.00m, "ZAR").Value;
        Money money2 = Money.Create(100.00m, "ZAR").Value;

        // Act & Assert
        ItShouldBeEqual(money1, money2);
    }

    /// <summary>
    /// TC-MON-005: Test Money inequality with different amounts
    /// </summary>
    [Fact]
    public void GivenDifferentAmounts_WhenComparingMoney()
    {
        // Arrange
        Money money1 = Money.Create(100.00m, "ZAR").Value;
        Money money2 = Money.Create(200.00m, "ZAR").Value;

        // Act & Assert
        ItShouldNotBeEqual(money1, money2);
    }

    /// <summary>
    /// TC-MON-005: Test Money inequality with different currencies
    /// </summary>
    [Fact]
    public void GivenDifferentCurrencies_WhenComparingMoney()
    {
        // Arrange
        Money money1 = Money.Create(100.00m, "ZAR").Value;
        Money money2 = Money.Create(100.00m, "USD").Value;

        // Act & Assert
        ItShouldNotBeEqual(money1, money2);
    }

    // Assertion Helpers

    private static void ItShouldSucceed(Result<Money, Error> result)
    {
        result.IsSuccess.ShouldBeTrue();
    }

    private static void ItShouldHaveCorrectAmount(Result<Money, Error> result, decimal expectedAmount)
    {
        result.Value.Amount.ShouldBe(expectedAmount);
    }

    private static void ItShouldHaveCorrectCurrency(Result<Money, Error> result, string expectedCurrency)
    {
        result.Value.Currency.ShouldBe(expectedCurrency.ToUpperInvariant());
    }

    private static void ItShouldHaveZeroAmount(Money money)
    {
        money.Amount.ShouldBe(0m);
    }

    private static void ItShouldHaveUpperCaseCurrency(Money money, string expectedCurrency)
    {
        money.Currency.ShouldBe(expectedCurrency.ToUpperInvariant());
    }

    private static void ItShouldFailWithEmptyCurrencyError(Result<Money, Error> result)
    {
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<EmptyCurrencyError>();
        result.Error.Code.ShouldBe("MONEY_000");
    }

    private static void ItShouldBeEqual(Money money1, Money money2)
    {
        money1.ShouldBe(money2);
        (money1 == money2).ShouldBeTrue();
    }

    private static void ItShouldNotBeEqual(Money money1, Money money2)
    {
        money1.ShouldNotBe(money2);
        (money1 != money2).ShouldBeTrue();
    }
}
