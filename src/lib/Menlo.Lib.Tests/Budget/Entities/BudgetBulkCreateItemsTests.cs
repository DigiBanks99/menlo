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
/// Tests for the Budget.BulkCreateItems method.
/// </summary>
public sealed class BudgetBulkCreateItemsTests
{
    // -------------------------------------------------------------------------
    // Budget.BulkCreateItems — Valid scenarios
    // -------------------------------------------------------------------------

    [Fact]
    public void GivenEmptyCategory_WhenBulkCreateItems_ThenCreates12Items()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetCategoryId leafId) =
            CreateBudgetWithLeafCategory();
        Money amount = CreateMoney(500m);
        PayerSplit payerSplit = CreatePayerSplit();
        AttributionSplit attributionSplit = CreateAttributionSplit();

        // Act
        Result<IReadOnlyList<BudgetItem>, BudgetError> result = budget.BulkCreateItems(
            leafId, BudgetFlow.Expense, amount, payerSplit, attributionSplit);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveCreated12Items(result);
        ItShouldHaveAllMonthsCovered(result);
        ItShouldHaveCorrectAmountOnAllItems(result, amount);
        ItShouldHaveCorrectPayerSplitOnAllItems(result, payerSplit);
        ItShouldHaveCorrectAttributionSplitOnAllItems(result, attributionSplit);
    }

    [Fact]
    public void GivenEmptyCategory_WhenBulkCreateItems_ThenAllItemsHaveCorrectBudgetFlow()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetCategoryId leafId) =
            CreateBudgetWithLeafCategory();

        // Act
        Result<IReadOnlyList<BudgetItem>, BudgetError> result = budget.BulkCreateItems(
            leafId, BudgetFlow.Expense, CreateMoney(300m), CreatePayerSplit(), CreateAttributionSplit());

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveCorrectBudgetFlowOnAllItems(result, BudgetFlow.Expense);
    }

    [Fact]
    public void GivenEmptyCategory_WhenBulkCreateItems_ThenEmits12BudgetItemPlannedEvents()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetCategoryId leafId) =
            CreateBudgetWithLeafCategory();
        Money amount = CreateMoney(200m);

        // Act
        Result<IReadOnlyList<BudgetItem>, BudgetError> result = budget.BulkCreateItems(
            leafId, BudgetFlow.Expense, amount, CreatePayerSplit(), CreateAttributionSplit());

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveEmitted12PlannedEvents(budget);
    }

    [Fact]
    public void GivenExistingItemForMonth3_WhenBulkCreateItems_ThenCreates11Items()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetCategoryId leafId) =
            CreateBudgetWithLeafCategory();
        budget.AddItem(leafId, 3, BudgetFlow.Expense, CreateMoney(100m), CreatePayerSplit(), CreateAttributionSplit());

        // Act
        Result<IReadOnlyList<BudgetItem>, BudgetError> result = budget.BulkCreateItems(
            leafId, BudgetFlow.Expense, CreateMoney(500m), CreatePayerSplit(), CreateAttributionSplit());

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveCreated11Items(result);
        ItShouldNotContainMonth(result, 3);
    }

    // -------------------------------------------------------------------------
    // Budget.BulkCreateItems — Invalid scenarios
    // -------------------------------------------------------------------------

    [Fact]
    public void GivenNonLeafCategory_WhenBulkCreateItems_ThenReturnsNonLeafCategoryError()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, _, BudgetCategoryId rootId) =
            CreateBudgetWithLeafCategoryAndRoot();

        // Act
        Result<IReadOnlyList<BudgetItem>, BudgetError> result = budget.BulkCreateItems(
            rootId, BudgetFlow.Expense, CreateMoney(100m), CreatePayerSplit(), CreateAttributionSplit());

        // Assert
        ItShouldFail(result);
        ItShouldBeErrorOfType<NonLeafCategoryError>(result);
    }

    [Fact]
    public void GivenNonExistentCategory_WhenBulkCreateItems_ThenReturnsCategoryNotFoundError()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, _) = CreateBudgetWithLeafCategory();
        BudgetCategoryId nonExistentId = BudgetCategoryId.NewId();

        // Act
        Result<IReadOnlyList<BudgetItem>, BudgetError> result = budget.BulkCreateItems(
            nonExistentId, BudgetFlow.Expense, CreateMoney(100m), CreatePayerSplit(), CreateAttributionSplit());

        // Assert
        ItShouldFail(result);
        ItShouldBeErrorOfType<CategoryNotFoundError>(result);
    }

    [Fact]
    public void GivenIncomeCategoryAndExpenseFlow_WhenBulkCreateItems_ThenReturnsInvalidBudgetFlowError()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetCategoryId leafId) =
            CreateBudgetWithLeafCategory(BudgetFlow.Income);

        // Act
        Result<IReadOnlyList<BudgetItem>, BudgetError> result = budget.BulkCreateItems(
            leafId, BudgetFlow.Expense, CreateMoney(100m), CreatePayerSplit(), CreateAttributionSplit());

        // Assert
        ItShouldFail(result);
        ItShouldBeErrorOfType<InvalidBudgetFlowError>(result);
    }

    [Fact]
    public void GivenBudgetFlowBoth_WhenBulkCreateItems_ThenReturnsInvalidBudgetFlowError()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetCategoryId leafId) =
            CreateBudgetWithLeafCategory(BudgetFlow.Both);

        // Act
        Result<IReadOnlyList<BudgetItem>, BudgetError> result = budget.BulkCreateItems(
            leafId, BudgetFlow.Both, CreateMoney(100m), CreatePayerSplit(), CreateAttributionSplit());

        // Assert
        ItShouldFail(result);
        ItShouldBeErrorOfType<InvalidBudgetFlowError>(result);
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

        Result<CategoryNode, BudgetError> rootResult =
            budget.AddCategory("Expenses", BudgetFlow.Expense);
        CategoryNode root = rootResult.Value;

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

        Result<CategoryNode, BudgetError> rootResult =
            budget.AddCategory("Expenses", BudgetFlow.Expense);
        CategoryNode root = rootResult.Value;

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

    // =========================================================================
    // Assertion helpers
    // =========================================================================

    private static void ItShouldSucceed(Result<IReadOnlyList<BudgetItem>, BudgetError> result)
    {
        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.ToString() : string.Empty);
    }

    private static void ItShouldFail(Result<IReadOnlyList<BudgetItem>, BudgetError> result)
    {
        result.IsFailure.ShouldBeTrue();
    }

    private static void ItShouldBeErrorOfType<TError>(Result<IReadOnlyList<BudgetItem>, BudgetError> result)
        where TError : BudgetError
    {
        result.Error.ShouldBeOfType<TError>();
    }

    private static void ItShouldHaveCreated12Items(Result<IReadOnlyList<BudgetItem>, BudgetError> result)
    {
        result.Value.Count.ShouldBe(12);
    }

    private static void ItShouldHaveCreated11Items(Result<IReadOnlyList<BudgetItem>, BudgetError> result)
    {
        result.Value.Count.ShouldBe(11);
    }

    private static void ItShouldHaveAllMonthsCovered(Result<IReadOnlyList<BudgetItem>, BudgetError> result)
    {
        HashSet<int> months = result.Value.Select(i => i.Month).ToHashSet();
        for (int month = 1; month <= 12; month++)
        {
            months.ShouldContain(month);
        }
    }

    private static void ItShouldHaveCorrectBudgetFlowOnAllItems(
        Result<IReadOnlyList<BudgetItem>, BudgetError> result,
        BudgetFlow expected)
    {
        foreach (BudgetItem item in result.Value)
        {
            item.BudgetFlow.ShouldBe(expected);
        }
    }

    private static void ItShouldHaveCorrectAmountOnAllItems(
        Result<IReadOnlyList<BudgetItem>, BudgetError> result,
        Money expected)
    {
        foreach (BudgetItem item in result.Value)
        {
            item.PlannedAmount.ShouldBe(expected);
        }
    }

    private static void ItShouldHaveCorrectPayerSplitOnAllItems(
        Result<IReadOnlyList<BudgetItem>, BudgetError> result,
        PayerSplit expected)
    {
        foreach (BudgetItem item in result.Value)
        {
            item.PayerSplit.ShouldBe(expected);
        }
    }

    private static void ItShouldHaveCorrectAttributionSplitOnAllItems(
        Result<IReadOnlyList<BudgetItem>, BudgetError> result,
        AttributionSplit expected)
    {
        foreach (BudgetItem item in result.Value)
        {
            item.AttributionSplit.ShouldBe(expected);
        }
    }

    private static void ItShouldHaveEmitted12PlannedEvents(Menlo.Lib.Budget.Entities.Budget budget)
    {
        int plannedEventCount = budget.DomainEvents.OfType<BudgetItemPlannedEvent>().Count();
        plannedEventCount.ShouldBe(12);
    }

    private static void ItShouldNotContainMonth(Result<IReadOnlyList<BudgetItem>, BudgetError> result, int month)
    {
        result.Value.ShouldAllBe(i => i.Month != month);
    }
}
