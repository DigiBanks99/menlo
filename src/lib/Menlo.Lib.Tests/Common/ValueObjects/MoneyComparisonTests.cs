using Menlo.Lib.Common.ValueObjects;
using Shouldly;

namespace Menlo.Lib.Tests.Common.ValueObjects;

/// <summary>
/// Tests for Money comparison operations.
/// Test Cases: TC-MON-014 through TC-MON-017
/// </summary>
public sealed class MoneyComparisonTests
{
    /// <summary>
    /// TC-MON-014: Compare Money with same currency
    /// </summary>
    [Fact]
    public void GivenMoneyWithSameCurrency_WhenComparing()
    {
        // Arrange
        Money money1 = Money.Create(10.00m, "ZAR").Value;
        Money money2 = Money.Create(20.00m, "ZAR").Value;

        // Act & Assert
        ItShouldBeLessThan(money1, money2);
        ItShouldBeGreaterThan(money2, money1);
    }

    /// <summary>
    /// TC-MON-014: Compare equal Money
    /// </summary>
    [Fact]
    public void GivenEqualMoney_WhenComparing()
    {
        // Arrange
        Money money1 = Money.Create(50.00m, "USD").Value;
        Money money2 = Money.Create(50.00m, "USD").Value;

        // Act & Assert
        ItShouldBeEqual(money1, money2);
        ItShouldBeLessThanOrEqual(money1, money2);
        ItShouldBeGreaterThanOrEqual(money1, money2);
    }

    /// <summary>
    /// TC-MON-015: Throw exception when comparing different currencies
    /// </summary>
    [Fact]
    public void GivenMoneyWithDifferentCurrency_WhenComparing()
    {
        // Arrange
        Money money1 = Money.Create(10.00m, "ZAR").Value;
        Money money2 = Money.Create(10.00m, "USD").Value;

        // Act & Assert
        ItShouldThrowArgumentException(() => money1.CompareTo(money2));
        ItShouldThrowArgumentException(() => { bool _ = money1 < money2; });
        ItShouldThrowArgumentException(() => { bool _ = money1 > money2; });
    }

    /// <summary>
    /// TC-MON-016: Test less than operator
    /// </summary>
    [Fact]
    public void GivenSmallerMoney_WhenUsingLessThanOperator()
    {
        // Arrange
        Money money1 = Money.Create(10.00m, "EUR").Value;
        Money money2 = Money.Create(20.00m, "EUR").Value;

        // Act & Assert
        ItShouldBeTrueForLessThan(money1, money2);
        ItShouldBeFalseForLessThan(money2, money1);
    }

    /// <summary>
    /// TC-MON-016: Test greater than operator
    /// </summary>
    [Fact]
    public void GivenLargerMoney_WhenUsingGreaterThanOperator()
    {
        // Arrange
        Money money1 = Money.Create(100.00m, "GBP").Value;
        Money money2 = Money.Create(50.00m, "GBP").Value;

        // Act & Assert
        ItShouldBeTrueForGreaterThan(money1, money2);
        ItShouldBeFalseForGreaterThan(money2, money1);
    }

    /// <summary>
    /// TC-MON-017: Test less than or equal operator
    /// </summary>
    [Fact]
    public void GivenMoney_WhenUsingLessThanOrEqualOperator()
    {
        // Arrange
        Money money1 = Money.Create(10.00m, "ZAR").Value;
        Money money2 = Money.Create(20.00m, "ZAR").Value;
        Money money3 = Money.Create(10.00m, "ZAR").Value;

        // Act & Assert
        ItShouldBeTrueForLessThanOrEqual(money1, money2);
        ItShouldBeTrueForLessThanOrEqual(money1, money3);
        ItShouldBeFalseForLessThanOrEqual(money2, money1);
    }

    /// <summary>
    /// TC-MON-017: Test greater than or equal operator
    /// </summary>
    [Fact]
    public void GivenMoney_WhenUsingGreaterThanOrEqualOperator()
    {
        // Arrange
        Money money1 = Money.Create(50.00m, "USD").Value;
        Money money2 = Money.Create(30.00m, "USD").Value;
        Money money3 = Money.Create(50.00m, "USD").Value;

        // Act & Assert
        ItShouldBeTrueForGreaterThanOrEqual(money1, money2);
        ItShouldBeTrueForGreaterThanOrEqual(money1, money3);
        ItShouldBeFalseForGreaterThanOrEqual(money2, money1);
    }

    // Assertion Helpers

    private static void ItShouldBeLessThan(Money money1, Money money2)
    {
        money1.CompareTo(money2).ShouldBeLessThan(0);
    }

    private static void ItShouldBeGreaterThan(Money money1, Money money2)
    {
        money1.CompareTo(money2).ShouldBeGreaterThan(0);
    }

    private static void ItShouldBeEqual(Money money1, Money money2)
    {
        money1.CompareTo(money2).ShouldBe(0);
        (money1 == money2).ShouldBeTrue();
    }

    private static void ItShouldBeLessThanOrEqual(Money money1, Money money2)
    {
        (money1 <= money2).ShouldBeTrue();
    }

    private static void ItShouldBeGreaterThanOrEqual(Money money1, Money money2)
    {
        (money1 >= money2).ShouldBeTrue();
    }

    private static void ItShouldBeTrueForLessThan(Money money1, Money money2)
    {
        (money1 < money2).ShouldBeTrue();
    }

    private static void ItShouldBeFalseForLessThan(Money money1, Money money2)
    {
        (money1 < money2).ShouldBeFalse();
    }

    private static void ItShouldBeTrueForGreaterThan(Money money1, Money money2)
    {
        (money1 > money2).ShouldBeTrue();
    }

    private static void ItShouldBeFalseForGreaterThan(Money money1, Money money2)
    {
        (money1 > money2).ShouldBeFalse();
    }

    private static void ItShouldBeTrueForLessThanOrEqual(Money money1, Money money2)
    {
        (money1 <= money2).ShouldBeTrue();
    }

    private static void ItShouldBeFalseForLessThanOrEqual(Money money1, Money money2)
    {
        (money1 <= money2).ShouldBeFalse();
    }

    private static void ItShouldBeTrueForGreaterThanOrEqual(Money money1, Money money2)
    {
        (money1 >= money2).ShouldBeTrue();
    }

    private static void ItShouldBeFalseForGreaterThanOrEqual(Money money1, Money money2)
    {
        (money1 >= money2).ShouldBeFalse();
    }

    private static void ItShouldThrowArgumentException(Action action)
    {
        Should.Throw<ArgumentException>(action);
    }
}
