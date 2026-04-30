using CSharpFunctionalExtensions;
using Menlo.Lib.Budget.Errors;
using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.ValueObjects;
using Shouldly;

namespace Menlo.Lib.Tests.Budget.ValueObjects;

/// <summary>
/// Tests for the PayerSplit value object.
/// </summary>
public sealed class PayerSplitTests
{
    // -------------------------------------------------------------------------
    // PayerSplit.Create — Valid scenarios
    // -------------------------------------------------------------------------

    [Fact]
    public void GivenSingleAllocationAt100Percent_WhenCreate_ThenSucceeds()
    {
        // Arrange
        UserId userId = UserId.NewId();
        IReadOnlyList<PayerAllocation> allocations = [new PayerAllocation(userId, 100)];

        // Act
        Result<PayerSplit, BudgetError> result = PayerSplit.Create(allocations);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveAllocationCount(result, 1);
    }

    [Fact]
    public void GivenMultipleAllocations_WhenSumIs100_ThenSucceeds()
    {
        // Arrange
        IReadOnlyList<PayerAllocation> allocations =
        [
            new PayerAllocation(UserId.NewId(), 60),
            new PayerAllocation(UserId.NewId(), 40),
        ];

        // Act
        Result<PayerSplit, BudgetError> result = PayerSplit.Create(allocations);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveAllocationCount(result, 2);
    }

    // -------------------------------------------------------------------------
    // PayerSplit.Create — Invalid scenarios
    // -------------------------------------------------------------------------

    [Fact]
    public void GivenEmptyAllocations_WhenCreate_ThenReturnsError()
    {
        // Arrange
        IReadOnlyList<PayerAllocation> allocations = [];

        // Act
        Result<PayerSplit, BudgetError> result = PayerSplit.Create(allocations);

        // Assert
        ItShouldFail(result);
        ItShouldBeInvalidPayerSplitError(result);
    }

    [Fact]
    public void GivenNullAllocations_WhenCreate_ThenReturnsError()
    {
        // Act
        Result<PayerSplit, BudgetError> result = PayerSplit.Create(null!);

        // Assert
        ItShouldFail(result);
        ItShouldBeInvalidPayerSplitError(result);
    }

    [Fact]
    public void GivenNegativePercent_WhenCreate_ThenReturnsError()
    {
        // Arrange
        IReadOnlyList<PayerAllocation> allocations =
        [
            new PayerAllocation(UserId.NewId(), -10),
            new PayerAllocation(UserId.NewId(), 110),
        ];

        // Act
        Result<PayerSplit, BudgetError> result = PayerSplit.Create(allocations);

        // Assert
        ItShouldFail(result);
        ItShouldBeInvalidPayerSplitError(result);
    }

    [Fact]
    public void GivenZeroPercent_WhenCreate_ThenReturnsError()
    {
        // Arrange
        IReadOnlyList<PayerAllocation> allocations =
        [
            new PayerAllocation(UserId.NewId(), 0),
            new PayerAllocation(UserId.NewId(), 100),
        ];

        // Act
        Result<PayerSplit, BudgetError> result = PayerSplit.Create(allocations);

        // Assert
        ItShouldFail(result);
        ItShouldBeInvalidPayerSplitError(result);
    }

    [Theory]
    [InlineData(50, 40)] // Under 100
    [InlineData(60, 50)] // Over 100
    public void GivenSumNot100_WhenCreate_ThenReturnsError(int percent1, int percent2)
    {
        // Arrange
        IReadOnlyList<PayerAllocation> allocations =
        [
            new PayerAllocation(UserId.NewId(), percent1),
            new PayerAllocation(UserId.NewId(), percent2),
        ];

        // Act
        Result<PayerSplit, BudgetError> result = PayerSplit.Create(allocations);

        // Assert
        ItShouldFail(result);
        ItShouldBeInvalidPayerSplitError(result);
    }

    [Fact]
    public void GivenDuplicateUserIds_WhenCreate_ThenReturnsError()
    {
        // Arrange
        UserId sameUser = UserId.NewId();
        IReadOnlyList<PayerAllocation> allocations =
        [
            new PayerAllocation(sameUser, 50),
            new PayerAllocation(sameUser, 50),
        ];

        // Act
        Result<PayerSplit, BudgetError> result = PayerSplit.Create(allocations);

        // Assert
        ItShouldFail(result);
        ItShouldBeInvalidPayerSplitError(result);
    }

    // -------------------------------------------------------------------------
    // PayerSplit — Equality
    // -------------------------------------------------------------------------

    [Fact]
    public void GivenValidSplit_WhenCheckEquality_ThenEqualSplitsAreEqual()
    {
        // Arrange
        UserId user1 = UserId.NewId();
        UserId user2 = UserId.NewId();
        IReadOnlyList<PayerAllocation> allocations =
        [
            new PayerAllocation(user1, 70),
            new PayerAllocation(user2, 30),
        ];

        // Act
        PayerSplit split1 = PayerSplit.Create(allocations).Value;
        PayerSplit split2 = PayerSplit.Create(allocations).Value;

        // Assert
        ItShouldBeEqual(split1, split2);
    }

    // =========================================================================
    // Assertion helpers
    // =========================================================================

    private static void ItShouldSucceed(Result<PayerSplit, BudgetError> result)
    {
        result.IsSuccess.ShouldBeTrue();
    }

    private static void ItShouldFail(Result<PayerSplit, BudgetError> result)
    {
        result.IsFailure.ShouldBeTrue();
    }

    private static void ItShouldHaveAllocationCount(Result<PayerSplit, BudgetError> result, int expected)
    {
        result.Value.Allocations.Count.ShouldBe(expected);
    }

    private static void ItShouldBeInvalidPayerSplitError(Result<PayerSplit, BudgetError> result)
    {
        result.Error.ShouldBeOfType<InvalidPayerSplitError>();
    }

    private static void ItShouldBeEqual(PayerSplit split1, PayerSplit split2)
    {
        split1.Equals(split2).ShouldBeTrue();
        split1.GetHashCode().ShouldBe(split2.GetHashCode());
    }
}
