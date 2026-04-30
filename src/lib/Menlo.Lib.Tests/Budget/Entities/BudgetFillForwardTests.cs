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
/// Tests for the Budget.FillForward method.
/// </summary>
public sealed class BudgetFillForwardTests
{
    // -------------------------------------------------------------------------
    // Budget.FillForward — Valid scenarios
    // -------------------------------------------------------------------------

    [Fact]
    public void FillForward_FromMonth1CreatesAll12Items()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetCategoryId leafId) =
            CreateBudgetWithLeafCategory();
        Money amount = CreateMoney(500m);
        PayerSplit payerSplit = CreatePayerSplit();
        AttributionSplit attributionSplit = CreateAttributionSplit();

        // Act
        Result<IReadOnlyList<BudgetItem>, BudgetError> result = budget.FillForward(
            leafId, 1, BudgetFlow.Expense, amount, payerSplit, attributionSplit);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveItemCount(result, 12);
        ItShouldCoverMonthsFromTo(result, 1, 12);
        ItShouldHaveCorrectAmountOnAllItems(result, amount);
    }

    [Fact]
    public void FillForward_FromMonth4Creates9Items()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetCategoryId leafId) =
            CreateBudgetWithLeafCategory();
        Money amount = CreateMoney(300m);
        PayerSplit payerSplit = CreatePayerSplit();
        AttributionSplit attributionSplit = CreateAttributionSplit();

        // Act
        Result<IReadOnlyList<BudgetItem>, BudgetError> result = budget.FillForward(
            leafId, 4, BudgetFlow.Expense, amount, payerSplit, attributionSplit);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveItemCount(result, 9);
        ItShouldCoverMonthsFromTo(result, 4, 12);
    }

    [Fact]
    public void FillForward_UpdatesExistingItemsPlannedAmount()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetCategoryId leafId) =
            CreateBudgetWithLeafCategory();
        PayerSplit payerSplit = CreatePayerSplit();
        AttributionSplit attributionSplit = CreateAttributionSplit();

        budget.AddItem(leafId, 4, BudgetFlow.Expense, CreateMoney(100m), payerSplit, attributionSplit);
        budget.AddItem(leafId, 5, BudgetFlow.Expense, CreateMoney(100m), payerSplit, attributionSplit);
        budget.AddItem(leafId, 6, BudgetFlow.Expense, CreateMoney(100m), payerSplit, attributionSplit);

        Money newAmount = CreateMoney(500m);

        // Act
        Result<IReadOnlyList<BudgetItem>, BudgetError> result = budget.FillForward(
            leafId, 4, BudgetFlow.Expense, newAmount, payerSplit, attributionSplit);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveItemCount(result, 9);
        ItShouldHaveCorrectAmountOnAllItems(result, newAmount);
    }

    [Fact]
    public void FillForward_EmitsBudgetItemPlannedEventForNewItems()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetCategoryId leafId) =
            CreateBudgetWithLeafCategory();
        budget.ClearDomainEvents();

        Money amount = CreateMoney(200m);
        PayerSplit payerSplit = CreatePayerSplit();
        AttributionSplit attributionSplit = CreateAttributionSplit();

        // Act
        Result<IReadOnlyList<BudgetItem>, BudgetError> result = budget.FillForward(
            leafId, 1, BudgetFlow.Expense, amount, payerSplit, attributionSplit);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveEmittedPlannedEvents(budget, 12);
    }

    [Fact]
    public void FillForward_EmitsBudgetItemCorrectedEventForUpdatedItems()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetCategoryId leafId) =
            CreateBudgetWithLeafCategory();
        PayerSplit payerSplit = CreatePayerSplit();
        AttributionSplit attributionSplit = CreateAttributionSplit();

        budget.AddItem(leafId, 4, BudgetFlow.Expense, CreateMoney(100m), payerSplit, attributionSplit);
        budget.AddItem(leafId, 5, BudgetFlow.Expense, CreateMoney(100m), payerSplit, attributionSplit);
        budget.AddItem(leafId, 6, BudgetFlow.Expense, CreateMoney(100m), payerSplit, attributionSplit);
        budget.ClearDomainEvents();

        Money newAmount = CreateMoney(500m);

        // Act
        Result<IReadOnlyList<BudgetItem>, BudgetError> result = budget.FillForward(
            leafId, 4, BudgetFlow.Expense, newAmount, payerSplit, attributionSplit);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveEmittedCorrectedEvents(budget, 3);
    }

    [Fact]
    public void FillForward_DoesNotEmitCorrectedEventWhenAmountUnchanged()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetCategoryId leafId) =
            CreateBudgetWithLeafCategory();
        PayerSplit payerSplit = CreatePayerSplit();
        AttributionSplit attributionSplit = CreateAttributionSplit();
        Money amount = CreateMoney(500m);

        budget.AddItem(leafId, 4, BudgetFlow.Expense, amount, payerSplit, attributionSplit);
        budget.ClearDomainEvents();

        // Act
        Result<IReadOnlyList<BudgetItem>, BudgetError> result = budget.FillForward(
            leafId, 4, BudgetFlow.Expense, amount, payerSplit, attributionSplit);

        // Assert
        ItShouldSucceed(result);
        ItShouldNotHaveEmittedCorrectedEvents(budget);
    }

    [Fact]
    public void FillForward_PropagatesSplitsToExistingItems()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetCategoryId leafId) =
            CreateBudgetWithLeafCategory();
        PayerSplit originalPayerSplit = CreatePayerSplit();
        AttributionSplit originalAttributionSplit = CreateAttributionSplit();
        Money amount = CreateMoney(300m);

        budget.AddItem(leafId, 4, BudgetFlow.Expense, amount, originalPayerSplit, originalAttributionSplit);

        PayerSplit newPayerSplit = CreatePayerSplit();
        AttributionSplit newAttributionSplit = CreateAttributionSplit();

        // Act
        Result<IReadOnlyList<BudgetItem>, BudgetError> result = budget.FillForward(
            leafId, 4, BudgetFlow.Expense, amount, newPayerSplit, newAttributionSplit);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveCorrectPayerSplitOnAllItems(result, newPayerSplit);
        ItShouldHaveCorrectAttributionSplitOnAllItems(result, newAttributionSplit);
    }

    [Fact]
    public void FillForward_CreatesNewItemsWithProvidedSplits()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetCategoryId leafId) =
            CreateBudgetWithLeafCategory();
        Money amount = CreateMoney(400m);
        PayerSplit payerSplit = CreatePayerSplit();
        AttributionSplit attributionSplit = CreateAttributionSplit();

        // Act
        Result<IReadOnlyList<BudgetItem>, BudgetError> result = budget.FillForward(
            leafId, 1, BudgetFlow.Expense, amount, payerSplit, attributionSplit);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveCorrectPayerSplitOnAllItems(result, payerSplit);
        ItShouldHaveCorrectAttributionSplitOnAllItems(result, attributionSplit);
    }

    // -------------------------------------------------------------------------
    // Budget.FillForward — Invalid scenarios
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void FillForward_WithInvalidMonthReturnsInvalidMonthError(int invalidMonth)
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetCategoryId leafId) =
            CreateBudgetWithLeafCategory();

        // Act
        Result<IReadOnlyList<BudgetItem>, BudgetError> result = budget.FillForward(
            leafId, invalidMonth, BudgetFlow.Expense, CreateMoney(100m), CreatePayerSplit(), CreateAttributionSplit());

        // Assert
        ItShouldFail(result);
        ItShouldBeErrorOfType<InvalidMonthError>(result);
    }

    [Fact]
    public void FillForward_WithNonExistentCategoryReturnsCategoryNotFoundError()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, _) = CreateBudgetWithLeafCategory();
        BudgetCategoryId nonExistentId = BudgetCategoryId.NewId();

        // Act
        Result<IReadOnlyList<BudgetItem>, BudgetError> result = budget.FillForward(
            nonExistentId, 1, BudgetFlow.Expense, CreateMoney(100m), CreatePayerSplit(), CreateAttributionSplit());

        // Assert
        ItShouldFail(result);
        ItShouldBeErrorOfType<CategoryNotFoundError>(result);
    }

    [Fact]
    public void FillForward_WithNonLeafCategoryReturnsNonLeafCategoryError()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, _, BudgetCategoryId rootId) =
            CreateBudgetWithLeafCategoryAndRoot();

        // Act
        Result<IReadOnlyList<BudgetItem>, BudgetError> result = budget.FillForward(
            rootId, 1, BudgetFlow.Expense, CreateMoney(100m), CreatePayerSplit(), CreateAttributionSplit());

        // Assert
        ItShouldFail(result);
        ItShouldBeErrorOfType<NonLeafCategoryError>(result);
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

    private static void ItShouldHaveItemCount(Result<IReadOnlyList<BudgetItem>, BudgetError> result, int count)
    {
        result.Value.Count.ShouldBe(count);
    }

    private static void ItShouldCoverMonthsFromTo(Result<IReadOnlyList<BudgetItem>, BudgetError> result, int from, int to)
    {
        HashSet<int> months = result.Value.Select(i => i.Month).ToHashSet();
        for (int month = from; month <= to; month++)
        {
            months.ShouldContain(month);
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

    private static void ItShouldHaveEmittedPlannedEvents(Menlo.Lib.Budget.Entities.Budget budget, int count)
    {
        int plannedEventCount = budget.DomainEvents.OfType<BudgetItemPlannedEvent>().Count();
        plannedEventCount.ShouldBe(count);
    }

    private static void ItShouldHaveEmittedCorrectedEvents(Menlo.Lib.Budget.Entities.Budget budget, int count)
    {
        int correctedEventCount = budget.DomainEvents.OfType<BudgetItemCorrectedEvent>().Count();
        correctedEventCount.ShouldBe(count);
    }

    private static void ItShouldNotHaveEmittedCorrectedEvents(Menlo.Lib.Budget.Entities.Budget budget)
    {
        int correctedEventCount = budget.DomainEvents.OfType<BudgetItemCorrectedEvent>().Count();
        correctedEventCount.ShouldBe(0);
    }
}
