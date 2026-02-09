using CSharpFunctionalExtensions;
using Menlo.Lib.Budget.Errors;
using Menlo.Lib.Budget.ValueObjects;
using Shouldly;

namespace Menlo.Lib.Tests.BudgetAggregateMinimum.ValueObjects;

/// <summary>
/// Tests for BudgetPeriod value object.
/// </summary>
public sealed class BudgetPeriodTests
{
    [Fact]
    public void GivenValidYearAndMonth_WhenCreatingBudgetPeriod()
    {
        // Arrange
        int year = 2024;
        int month = 6;

        // Act
        Result<BudgetPeriod, BudgetError> result = BudgetPeriod.Create(year, month);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveYear(result, year);
        ItShouldHaveMonth(result, month);
    }

    [Theory]
    [InlineData(1900, 1)]
    [InlineData(2100, 12)]
    [InlineData(2024, 6)]
    public void GivenValidBoundaryValues_WhenCreatingBudgetPeriod(int year, int month)
    {
        // Arrange & Act
        Result<BudgetPeriod, BudgetError> result = BudgetPeriod.Create(year, month);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveYear(result, year);
        ItShouldHaveMonth(result, month);
    }

    [Theory]
    [InlineData(1899)]
    [InlineData(2101)]
    [InlineData(1500)]
    [InlineData(3000)]
    public void GivenInvalidYear_WhenCreatingBudgetPeriod(int year)
    {
        // Arrange
        int month = 6;

        // Act
        Result<BudgetPeriod, BudgetError> result = BudgetPeriod.Create(year, month);

        // Assert
        ItShouldFail(result);
        ItShouldBeInvalidPeriodError(result);
        ItShouldHaveReasonContaining(result, "Year");
        ItShouldHaveReasonContaining(result, year.ToString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    [InlineData(100)]
    public void GivenInvalidMonth_WhenCreatingBudgetPeriod(int month)
    {
        // Arrange
        int year = 2024;

        // Act
        Result<BudgetPeriod, BudgetError> result = BudgetPeriod.Create(year, month);

        // Assert
        ItShouldFail(result);
        ItShouldBeInvalidPeriodError(result);
        ItShouldHaveReasonContaining(result, "Month");
        ItShouldHaveReasonContaining(result, month.ToString());
    }

    [Fact]
    public void GivenValidBudgetPeriod_WhenCallingToString()
    {
        // Arrange
        Result<BudgetPeriod, BudgetError> periodResult = BudgetPeriod.Create(2024, 6);
        BudgetPeriod period = periodResult.Value;

        // Act
        string result = period.ToString();

        // Assert
        result.ShouldBe("2024-06");
    }

    [Theory]
    [InlineData(2024, 1, "2024-01")]
    [InlineData(2024, 12, "2024-12")]
    [InlineData(1900, 1, "1900-01")]
    [InlineData(2100, 12, "2100-12")]
    public void GivenVariousPeriods_WhenCallingToString(int year, int month, string expected)
    {
        // Arrange
        Result<BudgetPeriod, BudgetError> periodResult = BudgetPeriod.Create(year, month);
        BudgetPeriod period = periodResult.Value;

        // Act
        string result = period.ToString();

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void GivenTwoIdenticalPeriods_WhenComparing()
    {
        // Arrange
        Result<BudgetPeriod, BudgetError> period1Result = BudgetPeriod.Create(2024, 6);
        Result<BudgetPeriod, BudgetError> period2Result = BudgetPeriod.Create(2024, 6);
        BudgetPeriod period1 = period1Result.Value;
        BudgetPeriod period2 = period2Result.Value;

        // Act & Assert
        ItShouldBeEqual(period1, period2);
    }

    [Fact]
    public void GivenTwoDifferentPeriods_WhenComparing()
    {
        // Arrange
        Result<BudgetPeriod, BudgetError> period1Result = BudgetPeriod.Create(2024, 6);
        Result<BudgetPeriod, BudgetError> period2Result = BudgetPeriod.Create(2024, 7);
        BudgetPeriod period1 = period1Result.Value;
        BudgetPeriod period2 = period2Result.Value;

        // Act & Assert
        ItShouldNotBeEqual(period1, period2);
    }

    [Fact]
    public void GivenPeriodsWithDifferentYears_WhenComparing()
    {
        // Arrange
        Result<BudgetPeriod, BudgetError> period1Result = BudgetPeriod.Create(2024, 6);
        Result<BudgetPeriod, BudgetError> period2Result = BudgetPeriod.Create(2025, 6);
        BudgetPeriod period1 = period1Result.Value;
        BudgetPeriod period2 = period2Result.Value;

        // Act & Assert
        ItShouldNotBeEqual(period1, period2);
    }

    // Assertion Helpers

    private static void ItShouldSucceed(Result<BudgetPeriod, BudgetError> result)
    {
        result.IsSuccess.ShouldBeTrue();
    }

    private static void ItShouldFail(Result<BudgetPeriod, BudgetError> result)
    {
        result.IsFailure.ShouldBeTrue();
    }

    private static void ItShouldHaveYear(Result<BudgetPeriod, BudgetError> result, int expectedYear)
    {
        result.Value.Year.ShouldBe(expectedYear);
    }

    private static void ItShouldHaveMonth(Result<BudgetPeriod, BudgetError> result, int expectedMonth)
    {
        result.Value.Month.ShouldBe(expectedMonth);
    }

    private static void ItShouldBeInvalidPeriodError(Result<BudgetPeriod, BudgetError> result)
    {
        result.Error.ShouldBeOfType<InvalidPeriodError>();
    }

    private static void ItShouldHaveReasonContaining(Result<BudgetPeriod, BudgetError> result, string expectedSubstring)
    {
        InvalidPeriodError error = (InvalidPeriodError)result.Error;
        error.Reason.ShouldContain(expectedSubstring);
    }

    private static void ItShouldBeEqual(BudgetPeriod period1, BudgetPeriod period2)
    {
        period1.ShouldBe(period2);
        (period1 == period2).ShouldBeTrue();
    }

    private static void ItShouldNotBeEqual(BudgetPeriod period1, BudgetPeriod period2)
    {
        period1.ShouldNotBe(period2);
        (period1 != period2).ShouldBeTrue();
    }
}
