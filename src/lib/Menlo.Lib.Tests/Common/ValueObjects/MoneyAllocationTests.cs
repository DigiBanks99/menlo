using CSharpFunctionalExtensions;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.Errors;
using Menlo.Lib.Common.ValueObjects;
using Shouldly;

namespace Menlo.Lib.Tests.Common.ValueObjects;

/// <summary>
/// Tests for Money allocation operations.
/// Test Cases: TC-MON-011 through TC-MON-013
/// </summary>
public sealed class MoneyAllocationTests
{
    /// <summary>
    /// TC-MON-011: Allocate into equal parts
    /// </summary>
    [Fact]
    public void GivenMoney_WhenAllocatingIntoEqualParts()
    {
        // Arrange
        Money money = Money.Create(10.00m, "ZAR").Value;
        int parts = 3;

        // Act
        Result<IReadOnlyList<Money>, Error> result = money.Allocate(parts);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveCorrectNumberOfParts(result, 3);
        ItShouldSumToOriginalAmount(result, money);
    }

    /// <summary>
    /// TC-MON-011: Allocate with remainder distribution
    /// </summary>
    [Fact]
    public void GivenMoney_WhenAllocatingWithRemainder()
    {
        // Arrange
        Money money = Money.Create(10.00m, "USD").Value;
        int parts = 3;

        // Act
        Result<IReadOnlyList<Money>, Error> result = money.Allocate(parts);

        // Assert
        ItShouldSucceed(result);
        ItShouldDistributeRemainder(result, new[] { 3.34m, 3.33m, 3.33m });
    }

    /// <summary>
    /// TC-MON-011: Reject zero or negative parts
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GivenZeroOrNegativeParts_WhenAllocating(int parts)
    {
        // Arrange
        Money money = Money.Create(100.00m, "EUR").Value;

        // Act
        Result<IReadOnlyList<Money>, Error> result = money.Allocate(parts);

        // Assert
        ItShouldFailWithInvalidAllocationError(result);
    }

    /// <summary>
    /// TC-MON-012: Allocate by ratios
    /// </summary>
    [Fact]
    public void GivenMoney_WhenAllocatingByRatios()
    {
        // Arrange
        Money money = Money.Create(100.00m, "ZAR").Value;
        int[] ratios = [1, 2, 3];

        // Act
        Result<IReadOnlyList<Money>, Error> result = money.Allocate(ratios);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveCorrectNumberOfParts(result, 3);
        ItShouldSumToOriginalAmount(result, money);
    }

    /// <summary>
    /// TC-MON-012: Allocate with specific ratio distribution
    /// </summary>
    [Fact]
    public void GivenMoney_WhenAllocatingWithSpecificRatios()
    {
        // Arrange
        Money money = Money.Create(100.00m, "USD").Value;
        int[] ratios = [1, 1, 2];

        // Act
        Result<IReadOnlyList<Money>, Error> result = money.Allocate(ratios);

        // Assert
        ItShouldSucceed(result);
        ItShouldDistributeRemainder(result, new[] { 25.00m, 25.00m, 50.00m });
    }

    /// <summary>
    /// TC-MON-012: Allocate with zero ratios (valid for some parts)
    /// </summary>
    [Fact]
    public void GivenMoney_WhenAllocatingWithZeroRatios()
    {
        // Arrange
        Money money = Money.Create(100.00m, "GBP").Value;
        int[] ratios = [0, 1, 0, 2];

        // Act
        Result<IReadOnlyList<Money>, Error> result = money.Allocate(ratios);

        // Assert
        ItShouldSucceed(result);
        ItShouldDistributeRemainder(result, new[] { 0m, 33.33m, 0m, 66.67m });
    }

    /// <summary>
    /// TC-MON-013: Reject empty ratios
    /// </summary>
    [Fact]
    public void GivenEmptyRatios_WhenAllocating()
    {
        // Arrange
        Money money = Money.Create(100.00m, "EUR").Value;
        int[] ratios = [];

        // Act
        Result<IReadOnlyList<Money>, Error> result = money.Allocate(ratios);

        // Assert
        ItShouldFailWithInvalidAllocationError(result);
    }

    /// <summary>
    /// TC-MON-013: Reject negative ratios
    /// </summary>
    [Fact]
    public void GivenNegativeRatios_WhenAllocating()
    {
        // Arrange
        Money money = Money.Create(100.00m, "ZAR").Value;
        int[] ratios = [1, -1, 2];

        // Act
        Result<IReadOnlyList<Money>, Error> result = money.Allocate(ratios);

        // Assert
        ItShouldFailWithInvalidAllocationError(result);
    }

    /// <summary>
    /// TC-MON-013: Reject all-zero ratios
    /// </summary>
    [Fact]
    public void GivenAllZeroRatios_WhenAllocating()
    {
        // Arrange
        Money money = Money.Create(100.00m, "USD").Value;
        int[] ratios = [0, 0, 0];

        // Act
        Result<IReadOnlyList<Money>, Error> result = money.Allocate(ratios);

        // Assert
        ItShouldFailWithInvalidAllocationError(result);
    }

    // Assertion Helpers

    private static void ItShouldSucceed(Result<IReadOnlyList<Money>, Error> result)
    {
        result.IsSuccess.ShouldBeTrue();
    }

    private static void ItShouldHaveCorrectNumberOfParts(Result<IReadOnlyList<Money>, Error> result, int expectedCount)
    {
        result.Value.Count.ShouldBe(expectedCount);
    }

    private static void ItShouldSumToOriginalAmount(Result<IReadOnlyList<Money>, Error> result, Money original)
    {
        decimal sum = result.Value.Sum(m => m.Amount);
        sum.ShouldBe(original.Amount);
    }

    private static void ItShouldDistributeRemainder(Result<IReadOnlyList<Money>, Error> result, decimal[] expectedAmounts)
    {
        result.Value.Count.ShouldBe(expectedAmounts.Length);

        for (int i = 0; i < expectedAmounts.Length; i++)
        {
            result.Value[i].Amount.ShouldBe(expectedAmounts[i]);
        }
    }

    private static void ItShouldFailWithInvalidAllocationError(Result<IReadOnlyList<Money>, Error> result)
    {
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<InvalidAllocationError>();
        result.Error.Code.ShouldBe("MONEY_003");
    }
}
