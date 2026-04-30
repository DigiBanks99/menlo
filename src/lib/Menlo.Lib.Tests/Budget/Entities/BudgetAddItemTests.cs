using CSharpFunctionalExtensions;
using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.Enums;
using Menlo.Lib.Budget.Errors;
using Menlo.Lib.Budget.Events;
using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;
using Shouldly;

namespace Menlo.Lib.Tests.Budget.Entities;

/// <summary>
/// Tests for the Budget.AddItem method.
/// </summary>
public sealed class BudgetAddItemTests
{
    // -------------------------------------------------------------------------
    // Budget.AddItem — Valid scenarios
    // -------------------------------------------------------------------------

    [Fact]
    public void GivenLeafCategory_WhenAddItemWithValidData_ThenSucceeds()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetCategoryId leafId) =
            CreateBudgetWithLeafCategory();

        // Act
        Result<BudgetItem, BudgetError> result = budget.AddItem(
            leafId, 1, BudgetFlow.Expense, CreateMoney(500m), CreatePayerSplit(), CreateAttributionSplit());

        // Assert
        ItShouldSucceed(result);
    }

    [Fact]
    public void GivenLeafCategory_WhenAddItem_ThenItemHasCorrectProperties()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetCategoryId leafId) =
            CreateBudgetWithLeafCategory();
        Money amount = CreateMoney(750m);
        PayerSplit payerSplit = CreatePayerSplit();
        AttributionSplit attributionSplit = CreateAttributionSplit();

        // Act
        Result<BudgetItem, BudgetError> result = budget.AddItem(
            leafId, 3, BudgetFlow.Expense, amount, payerSplit, attributionSplit);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveCorrectCategoryId(result, leafId);
        ItShouldHaveCorrectMonth(result, 3);
        ItShouldHaveCorrectBudgetFlow(result, BudgetFlow.Expense);
        ItShouldHaveCorrectPlannedAmount(result, amount);
        ItShouldHaveCorrectPayerSplit(result, payerSplit);
        ItShouldHaveCorrectAttributionSplit(result, attributionSplit);
        ItShouldBeManualOverride(result);
    }

    [Fact]
    public void GivenLeafCategory_WhenAddItem_ThenBudgetItemPlannedEventEmitted()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetCategoryId leafId) =
            CreateBudgetWithLeafCategory();
        Money amount = CreateMoney(200m);

        // Act
        Result<BudgetItem, BudgetError> result = budget.AddItem(
            leafId, 6, BudgetFlow.Expense, amount, CreatePayerSplit(), CreateAttributionSplit());

        // Assert
        ItShouldSucceed(result);
        ItShouldRaiseBudgetItemPlannedEvent(budget, result.Value, leafId, 6, amount);
    }

    [Fact]
    public void GivenBothFlowCategory_WhenAddExpenseItem_ThenSucceeds()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetCategoryId leafId) =
            CreateBudgetWithLeafCategory(BudgetFlow.Both);

        // Act
        Result<BudgetItem, BudgetError> result = budget.AddItem(
            leafId, 1, BudgetFlow.Expense, CreateMoney(100m), CreatePayerSplit(), CreateAttributionSplit());

        // Assert
        ItShouldSucceed(result);
    }

    [Fact]
    public void GivenBothFlowCategory_WhenAddIncomeItem_ThenSucceeds()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetCategoryId leafId) =
            CreateBudgetWithLeafCategory(BudgetFlow.Both);

        // Act
        Result<BudgetItem, BudgetError> result = budget.AddItem(
            leafId, 1, BudgetFlow.Income, CreateMoney(100m), CreatePayerSplit(), CreateAttributionSplit());

        // Assert
        ItShouldSucceed(result);
    }

    [Fact]
    public void GivenDeletedItemForMonth_WhenAddNew_ThenSucceeds()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetCategoryId leafId) =
            CreateBudgetWithLeafCategory();

        // Add an item and then soft-delete it
        BudgetItem existingItem = budget.AddItem(
            leafId, 1, BudgetFlow.Expense, CreateMoney(100m), CreatePayerSplit(), CreateAttributionSplit()).Value;
        existingItem.Delete(CreateSoftDeleteStampFactory());

        // Act
        Result<BudgetItem, BudgetError> result = budget.AddItem(
            leafId, 1, BudgetFlow.Expense, CreateMoney(200m), CreatePayerSplit(), CreateAttributionSplit());

        // Assert
        ItShouldSucceed(result);
    }

    [Fact]
    public void GivenValidData_WhenAddItemForEachMonth_ThenAll12Succeed()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetCategoryId leafId) =
            CreateBudgetWithLeafCategory();

        // Act & Assert
        for (int month = 1; month <= 12; month++)
        {
            Result<BudgetItem, BudgetError> result = budget.AddItem(
                leafId, month, BudgetFlow.Expense, CreateMoney(100m), CreatePayerSplit(), CreateAttributionSplit());
            ItShouldSucceed(result);
        }

        budget.Items.Count.ShouldBe(12);
    }

    // -------------------------------------------------------------------------
    // Budget.AddItem — Invalid scenarios
    // -------------------------------------------------------------------------

    [Fact]
    public void GivenInvalidMonth0_WhenAddItem_ThenReturnsInvalidMonthError()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetCategoryId leafId) =
            CreateBudgetWithLeafCategory();

        // Act
        Result<BudgetItem, BudgetError> result = budget.AddItem(
            leafId, 0, BudgetFlow.Expense, CreateMoney(100m), CreatePayerSplit(), CreateAttributionSplit());

        // Assert
        ItShouldFail(result);
        ItShouldBeErrorOfType<InvalidMonthError>(result);
    }

    [Fact]
    public void GivenInvalidMonth13_WhenAddItem_ThenReturnsInvalidMonthError()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetCategoryId leafId) =
            CreateBudgetWithLeafCategory();

        // Act
        Result<BudgetItem, BudgetError> result = budget.AddItem(
            leafId, 13, BudgetFlow.Expense, CreateMoney(100m), CreatePayerSplit(), CreateAttributionSplit());

        // Assert
        ItShouldFail(result);
        ItShouldBeErrorOfType<InvalidMonthError>(result);
    }

    [Fact]
    public void GivenNonExistentCategory_WhenAddItem_ThenReturnsCategoryNotFoundError()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, _) = CreateBudgetWithLeafCategory();
        BudgetCategoryId nonExistentId = BudgetCategoryId.NewId();

        // Act
        Result<BudgetItem, BudgetError> result = budget.AddItem(
            nonExistentId, 1, BudgetFlow.Expense, CreateMoney(100m), CreatePayerSplit(), CreateAttributionSplit());

        // Assert
        ItShouldFail(result);
        ItShouldBeErrorOfType<CategoryNotFoundError>(result);
    }

    [Fact]
    public void GivenNonLeafCategory_WhenAddItem_ThenReturnsNonLeafCategoryError()
    {
        // Arrange — use the root category (which has a child)
        (Menlo.Lib.Budget.Entities.Budget budget, _, BudgetCategoryId rootId) =
            CreateBudgetWithLeafCategoryAndRoot();

        // Act
        Result<BudgetItem, BudgetError> result = budget.AddItem(
            rootId, 1, BudgetFlow.Expense, CreateMoney(100m), CreatePayerSplit(), CreateAttributionSplit());

        // Assert
        ItShouldFail(result);
        ItShouldBeErrorOfType<NonLeafCategoryError>(result);
    }

    [Fact]
    public void GivenExpenseOnlyCategory_WhenAddIncomeItem_ThenReturnsInvalidBudgetFlowError()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetCategoryId leafId) =
            CreateBudgetWithLeafCategory(BudgetFlow.Expense);

        // Act
        Result<BudgetItem, BudgetError> result = budget.AddItem(
            leafId, 1, BudgetFlow.Income, CreateMoney(100m), CreatePayerSplit(), CreateAttributionSplit());

        // Assert
        ItShouldFail(result);
        ItShouldBeErrorOfType<InvalidBudgetFlowError>(result);
    }

    [Fact]
    public void GivenIncomeOnlyCategory_WhenAddExpenseItem_ThenReturnsInvalidBudgetFlowError()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetCategoryId leafId) =
            CreateBudgetWithLeafCategory(BudgetFlow.Income);

        // Act
        Result<BudgetItem, BudgetError> result = budget.AddItem(
            leafId, 1, BudgetFlow.Expense, CreateMoney(100m), CreatePayerSplit(), CreateAttributionSplit());

        // Assert
        ItShouldFail(result);
        ItShouldBeErrorOfType<InvalidBudgetFlowError>(result);
    }

    [Fact]
    public void GivenBudgetFlowBoth_WhenAddItemWithBothFlow_ThenReturnsInvalidBudgetFlowError()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetCategoryId leafId) =
            CreateBudgetWithLeafCategory(BudgetFlow.Both);

        // Act
        Result<BudgetItem, BudgetError> result = budget.AddItem(
            leafId, 1, BudgetFlow.Both, CreateMoney(100m), CreatePayerSplit(), CreateAttributionSplit());

        // Assert
        ItShouldFail(result);
        ItShouldBeErrorOfType<InvalidBudgetFlowError>(result);
    }

    [Fact]
    public void GivenExistingItemForMonth_WhenAddDuplicate_ThenReturnsDuplicateError()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetCategoryId leafId) =
            CreateBudgetWithLeafCategory();
        budget.AddItem(leafId, 1, BudgetFlow.Expense, CreateMoney(100m), CreatePayerSplit(), CreateAttributionSplit());

        // Act
        Result<BudgetItem, BudgetError> result = budget.AddItem(
            leafId, 1, BudgetFlow.Expense, CreateMoney(200m), CreatePayerSplit(), CreateAttributionSplit());

        // Assert
        ItShouldFail(result);
        ItShouldBeErrorOfType<DuplicateBudgetItemError>(result);
    }

    // =========================================================================
    // Setup helpers
    // =========================================================================

    private static (Menlo.Lib.Budget.Entities.Budget Budget, BudgetCategoryId LeafCategoryId)
        CreateBudgetWithLeafCategory(BudgetFlow categoryFlow = BudgetFlow.Expense)
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
            budget.AddCategory("Electricity", categoryFlow, parentId: root.Id);
        CategoryNode leaf = leafResult.Value;

        return (budget, leaf.Id);
    }

    private static (Menlo.Lib.Budget.Entities.Budget Budget, BudgetCategoryId LeafCategoryId, BudgetCategoryId RootCategoryId)
        CreateBudgetWithLeafCategoryAndRoot(BudgetFlow categoryFlow = BudgetFlow.Expense)
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
            budget.AddCategory("Electricity", categoryFlow, parentId: root.Id);
        CategoryNode leaf = leafResult.Value;

        return (budget, leaf.Id, root.Id);
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

    private static void ItShouldSucceed(Result<BudgetItem, BudgetError> result)
    {
        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.ToString() : string.Empty);
    }

    private static void ItShouldFail(Result<BudgetItem, BudgetError> result)
    {
        result.IsFailure.ShouldBeTrue();
    }

    private static void ItShouldBeErrorOfType<TError>(Result<BudgetItem, BudgetError> result)
        where TError : BudgetError
    {
        result.Error.ShouldBeOfType<TError>();
    }

    private static void ItShouldHaveCorrectCategoryId(Result<BudgetItem, BudgetError> result, BudgetCategoryId expected)
    {
        result.Value.CategoryId.ShouldBe(expected);
    }

    private static void ItShouldHaveCorrectMonth(Result<BudgetItem, BudgetError> result, int expected)
    {
        result.Value.Month.ShouldBe(expected);
    }

    private static void ItShouldHaveCorrectBudgetFlow(Result<BudgetItem, BudgetError> result, BudgetFlow expected)
    {
        result.Value.BudgetFlow.ShouldBe(expected);
    }

    private static void ItShouldHaveCorrectPlannedAmount(Result<BudgetItem, BudgetError> result, Money expected)
    {
        result.Value.PlannedAmount.ShouldBe(expected);
    }

    private static void ItShouldHaveCorrectPayerSplit(Result<BudgetItem, BudgetError> result, PayerSplit expected)
    {
        result.Value.PayerSplit.ShouldBe(expected);
    }

    private static void ItShouldHaveCorrectAttributionSplit(Result<BudgetItem, BudgetError> result, AttributionSplit expected)
    {
        result.Value.AttributionSplit.ShouldBe(expected);
    }

    private static void ItShouldBeManualOverride(Result<BudgetItem, BudgetError> result)
    {
        result.Value.IsManualOverride.ShouldBeTrue();
    }

    private static void ItShouldRaiseBudgetItemPlannedEvent(
        Menlo.Lib.Budget.Entities.Budget budget,
        BudgetItem item,
        BudgetCategoryId categoryId,
        int month,
        Money plannedAmount)
    {
        IDomainEvent domainEvent = budget.DomainEvents.Last();
        domainEvent.ShouldBeOfType<BudgetItemPlannedEvent>();
        BudgetItemPlannedEvent evt = (BudgetItemPlannedEvent)domainEvent;
        evt.BudgetId.ShouldBe(budget.Id);
        evt.BudgetItemId.ShouldBe(item.Id);
        evt.CategoryId.ShouldBe(categoryId);
        evt.Month.ShouldBe(month);
        evt.PlannedAmount.ShouldBe(plannedAmount);
    }
}
