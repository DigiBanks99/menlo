using CSharpFunctionalExtensions;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.Errors;
using Menlo.Lib.Common.ValueObjects;
using Shouldly;

namespace Menlo.Lib.Tests.Common.ValueObjects;

/// <summary>
/// Tests for Money arithmetic operations.
/// Test Cases: TC-MON-006 through TC-MON-010
/// </summary>
public sealed class MoneyArithmeticTests
{
    /// <summary>
    /// TC-MON-006: Add Money with same currency
    /// </summary>
    [Fact]
    public void GivenMoneyWithSameCurrency_WhenAdding()
    {
        // Arrange
        Money money1 = Money.Create(10.00m, "ZAR").Value;
        Money money2 = Money.Create(20.00m, "ZAR").Value;

        // Act
        Result<Money, Error> result = money1.Add(money2);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveAmount(result, 30.00m);
        ItShouldHaveCurrency(result, "ZAR");
    }

    /// <summary>
    /// TC-MON-006: Reject addition with currency mismatch
    /// </summary>
    [Fact]
    public void GivenMoneyWithDifferentCurrency_WhenAdding()
    {
        // Arrange
        Money money1 = Money.Create(10.00m, "ZAR").Value;
        Money money2 = Money.Create(20.00m, "USD").Value;

        // Act
        Result<Money, Error> result = money1.Add(money2);

        // Assert
        ItShouldFailWithCurrencyMismatchError(result, "ZAR", "USD");
    }

    /// <summary>
    /// TC-MON-007: Subtract Money with same currency
    /// </summary>
    [Fact]
    public void GivenMoneyWithSameCurrency_WhenSubtracting()
    {
        // Arrange
        Money money1 = Money.Create(50.00m, "EUR").Value;
        Money money2 = Money.Create(20.00m, "EUR").Value;

        // Act
        Result<Money, Error> result = money1.Subtract(money2);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveAmount(result, 30.00m);
        ItShouldHaveCurrency(result, "EUR");
    }

    /// <summary>
    /// TC-MON-007: Allow negative result from subtraction
    /// </summary>
    [Fact]
    public void GivenSmallerMinuend_WhenSubtracting()
    {
        // Arrange
        Money money1 = Money.Create(10.00m, "GBP").Value;
        Money money2 = Money.Create(20.00m, "GBP").Value;

        // Act
        Result<Money, Error> result = money1.Subtract(money2);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveAmount(result, -10.00m);
    }

    /// <summary>
    /// TC-MON-007: Reject subtraction with currency mismatch
    /// </summary>
    [Fact]
    public void GivenMoneyWithDifferentCurrency_WhenSubtracting()
    {
        // Arrange
        Money money1 = Money.Create(50.00m, "ZAR").Value;
        Money money2 = Money.Create(20.00m, "USD").Value;

        // Act
        Result<Money, Error> result = money1.Subtract(money2);

        // Assert
        ItShouldFailWithCurrencyMismatchError(result, "ZAR", "USD");
    }

    /// <summary>
    /// TC-MON-008: Multiply Money by factor
    /// </summary>
    [Fact]
    public void GivenMoney_WhenMultiplyingByFactor()
    {
        // Arrange
        Money money = Money.Create(10.00m, "ZAR").Value;
        decimal factor = 2.5m;

        // Act
        Money result = money.Multiply(factor);

        // Assert
        ItShouldHaveAmountAfterMultiply(result, 25.00m);
        ItShouldHaveCurrencyAfterMultiply(result, "ZAR");
    }

    /// <summary>
    /// TC-MON-008: Multiply by negative factor
    /// </summary>
    [Fact]
    public void GivenMoney_WhenMultiplyingByNegativeFactor()
    {
        // Arrange
        Money money = Money.Create(10.00m, "USD").Value;
        decimal factor = -3m;

        // Act
        Money result = money.Multiply(factor);

        // Assert
        ItShouldHaveAmountAfterMultiply(result, -30.00m);
    }

    /// <summary>
    /// TC-MON-008: Multiply by zero
    /// </summary>
    [Fact]
    public void GivenMoney_WhenMultiplyingByZero()
    {
        // Arrange
        Money money = Money.Create(100.00m, "EUR").Value;

        // Act
        Money result = money.Multiply(0);

        // Assert
        ItShouldHaveAmountAfterMultiply(result, 0m);
    }

    /// <summary>
    /// TC-MON-009: Divide Money by divisor
    /// </summary>
    [Fact]
    public void GivenMoney_WhenDividingByDivisor()
    {
        // Arrange
        Money money = Money.Create(100.00m, "ZAR").Value;
        decimal divisor = 4m;

        // Act
        Result<Money, Error> result = money.Divide(divisor);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveAmount(result, 25.00m);
        ItShouldHaveCurrency(result, "ZAR");
    }

    /// <summary>
    /// TC-MON-009: Round division result to 2 decimal places
    /// </summary>
    [Fact]
    public void GivenMoney_WhenDividingWithRemainder()
    {
        // Arrange
        Money money = Money.Create(10.00m, "USD").Value;
        decimal divisor = 3m;

        // Act
        Result<Money, Error> result = money.Divide(divisor);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveAmount(result, 3.33m);
    }

    /// <summary>
    /// TC-MON-010: Reject division by zero
    /// </summary>
    [Fact]
    public void GivenMoney_WhenDividingByZero()
    {
        // Arrange
        Money money = Money.Create(100.00m, "GBP").Value;

        // Act
        Result<Money, Error> result = money.Divide(0);

        // Assert
        ItShouldFailWithDivisionByZeroError(result);
    }

    // Assertion Helpers

    private static void ItShouldSucceed(Result<Money, Error> result)
    {
        result.IsSuccess.ShouldBeTrue();
    }

    private static void ItShouldHaveAmount(Result<Money, Error> result, decimal expectedAmount)
    {
        result.Value.Amount.ShouldBe(expectedAmount);
    }

    private static void ItShouldHaveCurrency(Result<Money, Error> result, string expectedCurrency)
    {
        result.Value.Currency.ShouldBe(expectedCurrency);
    }

    private static void ItShouldHaveAmountAfterMultiply(Money money, decimal expectedAmount)
    {
        money.Amount.ShouldBe(expectedAmount);
    }

    private static void ItShouldHaveCurrencyAfterMultiply(Money money, string expectedCurrency)
    {
        money.Currency.ShouldBe(expectedCurrency);
    }

    private static void ItShouldFailWithCurrencyMismatchError(Result<Money, Error> result, string expected, string actual)
    {
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<CurrencyMismatchError>();
        result.Error.Code.ShouldBe("MONEY_001");
        
        CurrencyMismatchError error = (CurrencyMismatchError)result.Error;
        error.ExpectedCurrency.ShouldBe(expected);
        error.ActualCurrency.ShouldBe(actual);
    }

    private static void ItShouldFailWithDivisionByZeroError(Result<Money, Error> result)
    {
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<DivisionByZeroError>();
        result.Error.Code.ShouldBe("MONEY_002");
    }
}
