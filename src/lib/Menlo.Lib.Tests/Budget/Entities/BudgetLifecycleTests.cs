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
/// Tests for the Budget.RealizeItem and Budget.RecordItemSpent lifecycle methods.
/// </summary>
public sealed class BudgetLifecycleTests
{
    // -------------------------------------------------------------------------
    // Budget.RealizeItem — Valid scenarios
    // -------------------------------------------------------------------------

    [Fact]
    public void RealizeItem_SetsRealizedAmount()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetItem item) = CreateBudgetWithItem();
        Money realizedAmount = CreateMoney(450m);

        // Act
        Result<BudgetItem, BudgetError> result = budget.RealizeItem(item.Id, realizedAmount);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveRealizedAmount(result, realizedAmount);
    }

    [Fact]
    public void RealizeItem_EmitsBudgetItemRealizedEvent()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetItem item) = CreateBudgetWithItem();
        Money realizedAmount = CreateMoney(450m);
        int eventCountBefore = budget.DomainEvents.Count;

        // Act
        Result<BudgetItem, BudgetError> result = budget.RealizeItem(item.Id, realizedAmount);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveEmittedRealizedEvent(budget, item.Id, realizedAmount, eventCountBefore);
    }

    [Fact]
    public void RealizeItem_WithNonExistentItemId_ReturnsBudgetItemNotFoundError()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, _) = CreateBudgetWithItem();
        BudgetItemId nonExistentId = BudgetItemId.NewId();

        // Act
        Result<BudgetItem, BudgetError> result = budget.RealizeItem(nonExistentId, CreateMoney(100m));

        // Assert
        ItShouldFail(result);
        ItShouldBeErrorOfType<BudgetItemNotFoundError>(result);
    }

    [Fact]
    public void RealizeItem_WithDeletedItem_ReturnsBudgetItemNotFoundError()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetItem item) = CreateBudgetWithItem();
        item.Delete(CreateSoftDeleteStampFactory());

        // Act
        Result<BudgetItem, BudgetError> result = budget.RealizeItem(item.Id, CreateMoney(100m));

        // Assert
        ItShouldFail(result);
        ItShouldBeErrorOfType<BudgetItemNotFoundError>(result);
    }

    [Fact]
    public void RealizeItem_CalledMultipleTimes_OverwritesPreviousAmount()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetItem item) = CreateBudgetWithItem();
        Money firstAmount = CreateMoney(400m);
        Money correctedAmount = CreateMoney(425m);

        // Act
        budget.RealizeItem(item.Id, firstAmount);
        Result<BudgetItem, BudgetError> result = budget.RealizeItem(item.Id, correctedAmount);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveRealizedAmount(result, correctedAmount);
    }

    // -------------------------------------------------------------------------
    // Budget.RecordItemSpent — Valid scenarios
    // -------------------------------------------------------------------------

    [Fact]
    public void RecordItemSpent_SetsSpentAmount()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetItem item) = CreateBudgetWithItem();
        Money spentAmount = CreateMoney(500m);

        // Act
        Result<BudgetItem, BudgetError> result = budget.RecordItemSpent(item.Id, spentAmount);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveSpentAmount(result, spentAmount);
    }

    [Fact]
    public void RecordItemSpent_EmitsBudgetItemSpentEvent()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetItem item) = CreateBudgetWithItem();
        Money spentAmount = CreateMoney(500m);
        int eventCountBefore = budget.DomainEvents.Count;

        // Act
        Result<BudgetItem, BudgetError> result = budget.RecordItemSpent(item.Id, spentAmount);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveEmittedSpentEvent(budget, item.Id, spentAmount, eventCountBefore);
    }

    [Fact]
    public void RecordItemSpent_WithNonExistentItemId_ReturnsBudgetItemNotFoundError()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, _) = CreateBudgetWithItem();
        BudgetItemId nonExistentId = BudgetItemId.NewId();

        // Act
        Result<BudgetItem, BudgetError> result = budget.RecordItemSpent(nonExistentId, CreateMoney(100m));

        // Assert
        ItShouldFail(result);
        ItShouldBeErrorOfType<BudgetItemNotFoundError>(result);
    }

    [Fact]
    public void RecordItemSpent_WithDeletedItem_ReturnsBudgetItemNotFoundError()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetItem item) = CreateBudgetWithItem();
        item.Delete(CreateSoftDeleteStampFactory());

        // Act
        Result<BudgetItem, BudgetError> result = budget.RecordItemSpent(item.Id, CreateMoney(100m));

        // Assert
        ItShouldFail(result);
        ItShouldBeErrorOfType<BudgetItemNotFoundError>(result);
    }

    [Fact]
    public void RecordItemSpent_WithoutPriorRealization_Succeeds()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetItem item) = CreateBudgetWithItem();
        item.RealizedAmount.ShouldBeNull(); // precondition: no realized amount yet
        Money spentAmount = CreateMoney(500m);

        // Act
        Result<BudgetItem, BudgetError> result = budget.RecordItemSpent(item.Id, spentAmount);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveSpentAmount(result, spentAmount);
        ItShouldHaveNullRealizedAmount(result);
    }

    [Fact]
    public void RecordItemSpent_AfterRealizeItem_BothAmountsSetIndependently()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetItem item) = CreateBudgetWithItem();
        Money realizedAmount = CreateMoney(450m);
        Money spentAmount = CreateMoney(450m);

        // Act
        budget.RealizeItem(item.Id, realizedAmount);
        Result<BudgetItem, BudgetError> result = budget.RecordItemSpent(item.Id, spentAmount);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveRealizedAmount(result, realizedAmount);
        ItShouldHaveSpentAmount(result, spentAmount);
    }

    // =========================================================================
    // Setup helpers
    // =========================================================================

    private static (Menlo.Lib.Budget.Entities.Budget Budget, BudgetItem Item) CreateBudgetWithItem()
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

        // Add an item
        Result<BudgetItem, BudgetError> itemResult = budget.AddItem(
            leaf.Id, 1, BudgetFlow.Expense, CreateMoney(500m), CreatePayerSplit(), CreateAttributionSplit());

        return (budget, itemResult.Value);
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

    private static void ItShouldHaveRealizedAmount(Result<BudgetItem, BudgetError> result, Money expected)
    {
        result.Value.RealizedAmount.ShouldBe(expected);
    }

    private static void ItShouldHaveNullRealizedAmount(Result<BudgetItem, BudgetError> result)
    {
        result.Value.RealizedAmount.ShouldBeNull();
    }

    private static void ItShouldHaveSpentAmount(Result<BudgetItem, BudgetError> result, Money expected)
    {
        result.Value.SpentAmount.ShouldBe(expected);
    }

    private static void ItShouldHaveEmittedRealizedEvent(
        Menlo.Lib.Budget.Entities.Budget budget,
        BudgetItemId itemId,
        Money expectedAmount,
        int eventCountBefore)
    {
        IReadOnlyList<IDomainEvent> newEvents = budget.DomainEvents.Skip(eventCountBefore).ToList();
        newEvents.Count.ShouldBe(1);
        BudgetItemRealizedEvent evt = newEvents[0].ShouldBeOfType<BudgetItemRealizedEvent>();
        evt.BudgetId.ShouldBe(budget.Id);
        evt.BudgetItemId.ShouldBe(itemId);
        evt.RealizedAmount.ShouldBe(expectedAmount);
    }

    private static void ItShouldHaveEmittedSpentEvent(
        Menlo.Lib.Budget.Entities.Budget budget,
        BudgetItemId itemId,
        Money expectedAmount,
        int eventCountBefore)
    {
        IReadOnlyList<IDomainEvent> newEvents = budget.DomainEvents.Skip(eventCountBefore).ToList();
        newEvents.Count.ShouldBe(1);
        BudgetItemSpentEvent evt = newEvents[0].ShouldBeOfType<BudgetItemSpentEvent>();
        evt.BudgetId.ShouldBe(budget.Id);
        evt.BudgetItemId.ShouldBe(itemId);
        evt.SpentAmount.ShouldBe(expectedAmount);
    }
}
