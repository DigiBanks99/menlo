using CSharpFunctionalExtensions;
using Menlo.Lib.Budget.Enums;
using Menlo.Lib.Budget.Errors;
using Menlo.Lib.Budget.ValueObjects;
using Shouldly;

namespace Menlo.Lib.Tests.Budget.ValueObjects;

/// <summary>
/// Tests for the AttributionSplit value object.
/// </summary>
public sealed class AttributionSplitTests
{
    // -------------------------------------------------------------------------
    // AttributionSplit.Create — Valid scenarios
    // -------------------------------------------------------------------------

    [Fact]
    public void GivenSingleAttribution_WhenCreate_ThenSucceeds()
    {
        // Arrange
        IReadOnlyList<AttributionAllocation> allocations =
            [new AttributionAllocation(Attribution.Main, 100)];

        // Act
        Result<AttributionSplit, BudgetError> result = AttributionSplit.Create(allocations);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveAllocationCount(result, 1);
    }

    [Fact]
    public void GivenMultipleAttributions_WhenSumIs100_ThenSucceeds()
    {
        // Arrange
        IReadOnlyList<AttributionAllocation> allocations =
        [
            new AttributionAllocation(Attribution.Main, 60),
            new AttributionAllocation(Attribution.Rental, 40),
        ];

        // Act
        Result<AttributionSplit, BudgetError> result = AttributionSplit.Create(allocations);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveAllocationCount(result, 2);
    }

    // -------------------------------------------------------------------------
    // AttributionSplit.Create — Invalid scenarios
    // -------------------------------------------------------------------------

    [Fact]
    public void GivenEmptyAllocations_WhenCreate_ThenReturnsError()
    {
        // Arrange
        IReadOnlyList<AttributionAllocation> allocations = [];

        // Act
        Result<AttributionSplit, BudgetError> result = AttributionSplit.Create(allocations);

        // Assert
        ItShouldFail(result);
        ItShouldBeInvalidAttributionSplitError(result);
    }

    [Fact]
    public void GivenNullAllocations_WhenCreate_ThenReturnsError()
    {
        // Act
        Result<AttributionSplit, BudgetError> result = AttributionSplit.Create(null!);

        // Assert
        ItShouldFail(result);
        ItShouldBeInvalidAttributionSplitError(result);
    }

    [Fact]
    public void GivenNegativePercent_WhenCreate_ThenReturnsError()
    {
        // Arrange
        IReadOnlyList<AttributionAllocation> allocations =
        [
            new AttributionAllocation(Attribution.Main, -10),
            new AttributionAllocation(Attribution.Rental, 110),
        ];

        // Act
        Result<AttributionSplit, BudgetError> result = AttributionSplit.Create(allocations);

        // Assert
        ItShouldFail(result);
        ItShouldBeInvalidAttributionSplitError(result);
    }

    [Fact]
    public void GivenZeroPercent_WhenCreate_ThenReturnsError()
    {
        // Arrange
        IReadOnlyList<AttributionAllocation> allocations =
        [
            new AttributionAllocation(Attribution.Main, 0),
            new AttributionAllocation(Attribution.Rental, 100),
        ];

        // Act
        Result<AttributionSplit, BudgetError> result = AttributionSplit.Create(allocations);

        // Assert
        ItShouldFail(result);
        ItShouldBeInvalidAttributionSplitError(result);
    }

    [Theory]
    [InlineData(50, 40)] // Under 100
    [InlineData(60, 50)] // Over 100
    public void GivenSumNot100_WhenCreate_ThenReturnsError(int percent1, int percent2)
    {
        // Arrange
        IReadOnlyList<AttributionAllocation> allocations =
        [
            new AttributionAllocation(Attribution.Main, percent1),
            new AttributionAllocation(Attribution.Rental, percent2),
        ];

        // Act
        Result<AttributionSplit, BudgetError> result = AttributionSplit.Create(allocations);

        // Assert
        ItShouldFail(result);
        ItShouldBeInvalidAttributionSplitError(result);
    }

    [Fact]
    public void GivenDuplicateAttributions_WhenCreate_ThenReturnsError()
    {
        // Arrange
        IReadOnlyList<AttributionAllocation> allocations =
        [
            new AttributionAllocation(Attribution.Main, 50),
            new AttributionAllocation(Attribution.Main, 50),
        ];

        // Act
        Result<AttributionSplit, BudgetError> result = AttributionSplit.Create(allocations);

        // Assert
        ItShouldFail(result);
        ItShouldBeInvalidAttributionSplitError(result);
    }

    // -------------------------------------------------------------------------
    // AttributionSplit — Equality
    // -------------------------------------------------------------------------

    [Fact]
    public void GivenValidSplit_WhenCheckEquality_ThenEqualSplitsAreEqual()
    {
        // Arrange
        IReadOnlyList<AttributionAllocation> allocations =
        [
            new AttributionAllocation(Attribution.Main, 70),
            new AttributionAllocation(Attribution.Rental, 30),
        ];

        // Act
        AttributionSplit split1 = AttributionSplit.Create(allocations).Value;
        AttributionSplit split2 = AttributionSplit.Create(allocations).Value;

        // Assert
        ItShouldBeEqual(split1, split2);
    }

    // =========================================================================
    // Assertion helpers
    // =========================================================================

    private static void ItShouldSucceed(Result<AttributionSplit, BudgetError> result)
    {
        result.IsSuccess.ShouldBeTrue();
    }

    private static void ItShouldFail(Result<AttributionSplit, BudgetError> result)
    {
        result.IsFailure.ShouldBeTrue();
    }

    private static void ItShouldHaveAllocationCount(Result<AttributionSplit, BudgetError> result, int expected)
    {
        result.Value.Allocations.Count.ShouldBe(expected);
    }

    private static void ItShouldBeInvalidAttributionSplitError(Result<AttributionSplit, BudgetError> result)
    {
        result.Error.ShouldBeOfType<InvalidAttributionSplitError>();
    }

    private static void ItShouldBeEqual(AttributionSplit split1, AttributionSplit split2)
    {
        split1.Equals(split2).ShouldBeTrue();
        split1.GetHashCode().ShouldBe(split2.GetHashCode());
    }
}
