using CSharpFunctionalExtensions;
using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.Enums;
using Menlo.Lib.Budget.Errors;
using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;
using Shouldly;

namespace Menlo.Lib.Tests.Budget.Entities;

/// <summary>
/// Tests for the Budget.DeleteItem method.
/// </summary>
public sealed class BudgetDeleteItemTests
{
    // =========================================================================
    // Budget.DeleteItem — Valid scenarios
    // =========================================================================

    [Fact]
    public void DeleteItem_WithExistingItem()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetItem item) = CreateBudgetWithItem();
        ISoftDeleteStampFactory stampFactory = CreateSoftDeleteStampFactory();

        // Act
        UnitResult<BudgetError> result = budget.DeleteItem(item.Id, stampFactory);

        // Assert
        ItShouldSucceed(result);
        ItShouldMarkItemAsDeleted(item);
        ItShouldSetDeletedAtAndDeletedBy(item);
    }

    [Fact]
    public void DeleteItem_WithNonExistentItemId()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, _) = CreateBudgetWithItem();
        BudgetItemId nonExistentId = BudgetItemId.NewId();
        ISoftDeleteStampFactory stampFactory = CreateSoftDeleteStampFactory();

        // Act
        UnitResult<BudgetError> result = budget.DeleteItem(nonExistentId, stampFactory);

        // Assert
        ItShouldFail(result);
        ItShouldBeErrorOfType<BudgetItemNotFoundError>(result);
    }

    [Fact]
    public void DeleteItem_WithAlreadyDeletedItem()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetItem item) = CreateBudgetWithItem();
        ISoftDeleteStampFactory stampFactory = CreateSoftDeleteStampFactory();
        budget.DeleteItem(item.Id, stampFactory); // first delete

        // Act
        UnitResult<BudgetError> result = budget.DeleteItem(item.Id, stampFactory);

        // Assert
        ItShouldFail(result);
        ItShouldBeErrorOfType<BudgetItemNotFoundError>(result);
    }

    [Fact]
    public void DeleteItem_AllowsAddItemForSameCategoryAndMonth()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetItem item, BudgetCategoryId categoryId) =
            CreateBudgetWithItemAndCategory();
        ISoftDeleteStampFactory stampFactory = CreateSoftDeleteStampFactory();
        int month = item.Month;

        // Delete the existing item
        budget.DeleteItem(item.Id, stampFactory);

        // Act — add a new item for the same category and month
        Result<BudgetItem, BudgetError> result = budget.AddItem(
            categoryId, month, BudgetFlow.Expense, CreateMoney(600m), CreatePayerSplit(), CreateAttributionSplit());

        // Assert
        ItShouldSucceedAddItem(result);
        ItShouldHaveMonth(result, month);
    }

    // =========================================================================
    // Setup helpers
    // =========================================================================

    private static (Menlo.Lib.Budget.Entities.Budget Budget, BudgetItem Item) CreateBudgetWithItem()
    {
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetItem item, _) = CreateBudgetWithItemAndCategory();
        return (budget, item);
    }

    private static (Menlo.Lib.Budget.Entities.Budget Budget, BudgetItem Item, BudgetCategoryId CategoryId) CreateBudgetWithItemAndCategory()
    {
        IAuditStampFactory auditFactory = CreateAuditStampFactory();
        Menlo.Lib.Budget.Entities.Budget budget =
            Menlo.Lib.Budget.Entities.Budget.Create(HouseholdId.NewId(), 2025, auditFactory).Value;

        // Add a root category
        Result<CategoryNode, BudgetError> rootResult =
            budget.AddCategory("Expenses", BudgetFlow.Expense);
        CategoryNode root = rootResult.Value;

        // Add a leaf category under root
        Result<CategoryNode, BudgetError> leafResult =
            budget.AddCategory("Electricity", BudgetFlow.Expense, parentId: root.Id);
        CategoryNode leaf = leafResult.Value;

        // Add an item for month 3
        Result<BudgetItem, BudgetError> itemResult = budget.AddItem(
            leaf.Id, 3, BudgetFlow.Expense, CreateMoney(500m), CreatePayerSplit(), CreateAttributionSplit());

        return (budget, itemResult.Value, leaf.Id);
    }

    private static Money CreateMoney(decimal amount) =>
        Money.Create(amount, "ZAR").Value;

    private static PayerSplit CreatePayerSplit()
    {
        IReadOnlyList<PayerAllocation> allocations = [new PayerAllocation(UserId.NewId(), 100)];
        return PayerSplit.Create(allocations).Value;
    }

    private static AttributionSplit CreateAttributionSplit()
    {
        IReadOnlyList<AttributionAllocation> allocations =
            [new AttributionAllocation(Attribution.Main, 100)];
        return AttributionSplit.Create(allocations).Value;
    }

    private sealed class FakeAuditStampFactory(AuditStamp stamp) : IAuditStampFactory
    {
        private readonly AuditStamp _stamp = stamp;
        public AuditStamp CreateStamp() => _stamp;
    }

    private static IAuditStampFactory CreateAuditStampFactory()
    {
        UserId actorId = UserId.NewId();
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        return new FakeAuditStampFactory(new AuditStamp(actorId, timestamp));
    }

    private sealed class FakeSoftDeleteStampFactory(SoftDeleteStamp stamp) : ISoftDeleteStampFactory
    {
        private readonly SoftDeleteStamp _stamp = stamp;
        public SoftDeleteStamp CreateStamp() => _stamp;
    }

    private static ISoftDeleteStampFactory CreateSoftDeleteStampFactory()
    {
        UserId actorId = UserId.NewId();
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        return new FakeSoftDeleteStampFactory(new SoftDeleteStamp(actorId, timestamp));
    }

    // =========================================================================
    // Assertion helpers
    // =========================================================================

    private static void ItShouldSucceed(UnitResult<BudgetError> result)
    {
        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.ToString() : string.Empty);
    }

    private static void ItShouldFail(UnitResult<BudgetError> result)
    {
        result.IsFailure.ShouldBeTrue();
    }

    private static void ItShouldBeErrorOfType<TError>(UnitResult<BudgetError> result)
        where TError : BudgetError
    {
        result.Error.ShouldBeOfType<TError>();
    }

    private static void ItShouldMarkItemAsDeleted(BudgetItem item)
    {
        item.IsDeleted.ShouldBeTrue();
    }

    private static void ItShouldSetDeletedAtAndDeletedBy(BudgetItem item)
    {
        item.DeletedAt.ShouldNotBeNull();
        item.DeletedBy.ShouldNotBeNull();
    }

    private static void ItShouldSucceedAddItem(Result<BudgetItem, BudgetError> result)
    {
        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.ToString() : string.Empty);
    }

    private static void ItShouldHaveMonth(Result<BudgetItem, BudgetError> result, int expectedMonth)
    {
        result.Value.Month.ShouldBe(expectedMonth);
    }
}
