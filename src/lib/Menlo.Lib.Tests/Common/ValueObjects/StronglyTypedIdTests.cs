using Menlo.Lib.Common.ValueObjects;
using Shouldly;

namespace Menlo.Lib.Tests.Common.ValueObjects;

/// <summary>
/// Tests for strongly-typed ID pattern using readonly record structs.
/// TC-01: Strongly-Typed Id Non-Interchangeability
/// </summary>
public sealed class StronglyTypedIdTests
{
    private readonly record struct BudgetId(Guid Value)
    {
        public static BudgetId New() => new(Guid.NewGuid());
    }

    [Fact]
    public void GivenTwoDifferentStronglyTypedIds_WhenComparing()
    {
        // Arrange
        BudgetId budgetId = BudgetId.New();
        UserId userId = UserId.NewId();

        // Act
        // This test verifies compile-time non-interchangeability
        // The following line would not compile:
        // BudgetId wrongAssignment = userId; // Compile error

        // Assert
        ItShouldHaveDifferentTypes(budgetId, userId);
    }

    private static void ItShouldHaveDifferentTypes(BudgetId budgetId, UserId userId)
    {
        budgetId.GetType().ShouldNotBe(userId.GetType());
    }

    [Fact]
    public void GivenBudgetId_WhenCreatingNew()
    {
        // Arrange & Act
        BudgetId id1 = BudgetId.New();
        BudgetId id2 = BudgetId.New();

        // Assert
        ItShouldReturnUniqueValues(id1, id2);
    }

    private static void ItShouldReturnUniqueValues(BudgetId id1, BudgetId id2)
    {
        id1.ShouldNotBe(id2);
        id1.Value.ShouldNotBe(Guid.Empty);
        id2.Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void GivenUserId_WhenCreatingNew()
    {
        // Arrange & Act
        UserId id1 = UserId.NewId();
        UserId id2 = UserId.NewId();

        // Assert
        ItShouldReturnUniqueUserIds(id1, id2);
    }

    private static void ItShouldReturnUniqueUserIds(UserId id1, UserId id2)
    {
        id1.ShouldNotBe(id2);
        id1.Value.ShouldNotBe(Guid.Empty);
        id2.Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void GivenSameGuidValue_WhenCreatingStronglyTypedIds()
    {
        // Arrange
        Guid value = Guid.NewGuid();

        // Act
        BudgetId id1 = new(value);
        BudgetId id2 = new(value);

        // Assert
        ItShouldHaveEqualIds(id1, id2, value);
    }

    private static void ItShouldHaveEqualIds(BudgetId id1, BudgetId id2, Guid value)
    {
        id1.ShouldBe(id2);
        id1.Value.ShouldBe(value);
    }

    [Fact]
    public void GivenUserId_WhenConvertingToString()
    {
        // Arrange
        Guid value = Guid.NewGuid();
        UserId userId = new(value);

        // Act
        string result = userId.ToString();

        // Assert
        ItShouldReturnGuidString(result, value);
    }

    private static void ItShouldReturnGuidString(string result, Guid value)
    {
        result.ShouldBe(value.ToString());
    }
}
