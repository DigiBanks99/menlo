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
/// Tests for the Budget.UpdateItem method.
/// </summary>
public sealed class BudgetUpdateItemTests
{
    // -------------------------------------------------------------------------
    // Budget.UpdateItem — Valid scenarios
    // -------------------------------------------------------------------------

    [Fact]
    public void UpdateItem_WithPlannedAmountOnly()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetItem item) = CreateBudgetWithItem();
        Money originalPlanned = item.PlannedAmount;
        Money newPlanned = CreateMoney(999m);
        int eventCountBefore = budget.DomainEvents.Count;

        // Act
        Result<BudgetItem, BudgetError> result = budget.UpdateItem(item.Id, plannedAmount: newPlanned);

        // Assert
        ItShouldSucceed(result);
        ItShouldHavePlannedAmount(result, newPlanned);
        ItShouldHaveRealizedAmountUnchanged(result, item.RealizedAmount);
        ItShouldHaveSpentAmountUnchanged(result, item.SpentAmount);
        ItShouldHaveEmittedCorrectionEvent(budget, item.Id, "PlannedAmount", originalPlanned, newPlanned, eventCountBefore);
    }

    [Fact]
    public void UpdateItem_WithRealizedAmountOnly()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetItem item) = CreateBudgetWithItem();
        Money newRealized = CreateMoney(450m);
        int eventCountBefore = budget.DomainEvents.Count;

        // Act
        Result<BudgetItem, BudgetError> result = budget.UpdateItem(item.Id, realizedAmount: newRealized);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveRealizedAmount(result, newRealized);
        ItShouldHavePlannedAmountUnchanged(result, item.PlannedAmount);
        ItShouldHaveEmittedCorrectionEvent(budget, item.Id, "RealizedAmount", Money.Zero("ZAR"), newRealized, eventCountBefore);
    }

    [Fact]
    public void UpdateItem_WithSpentAmountOnly()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetItem item) = CreateBudgetWithItem();
        Money newSpent = CreateMoney(300m);
        int eventCountBefore = budget.DomainEvents.Count;

        // Act
        Result<BudgetItem, BudgetError> result = budget.UpdateItem(item.Id, spentAmount: newSpent);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveSpentAmount(result, newSpent);
        ItShouldHavePlannedAmountUnchanged(result, item.PlannedAmount);
        ItShouldHaveEmittedCorrectionEvent(budget, item.Id, "SpentAmount", Money.Zero("ZAR"), newSpent, eventCountBefore);
    }

    [Fact]
    public void UpdateItem_WithPayerSplitOnly()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetItem item) = CreateBudgetWithItem();
        PayerSplit newPayerSplit = CreateAlternatePayerSplit();
        int eventCountBefore = budget.DomainEvents.Count;

        // Act
        Result<BudgetItem, BudgetError> result = budget.UpdateItem(item.Id, payerSplit: newPayerSplit);

        // Assert
        ItShouldSucceed(result);
        ItShouldHavePayerSplit(result, newPayerSplit);
        ItShouldNotHaveEmittedNewEvents(budget, eventCountBefore);
    }

    [Fact]
    public void UpdateItem_WithAttributionSplitOnly()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetItem item) = CreateBudgetWithItem();
        AttributionSplit newAttributionSplit = CreateAlternateAttributionSplit();
        int eventCountBefore = budget.DomainEvents.Count;

        // Act
        Result<BudgetItem, BudgetError> result = budget.UpdateItem(item.Id, attributionSplit: newAttributionSplit);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveAttributionSplit(result, newAttributionSplit);
        ItShouldNotHaveEmittedNewEvents(budget, eventCountBefore);
    }

    [Fact]
    public void UpdateItem_WithMultipleFieldsAtOnce()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetItem item) = CreateBudgetWithItem();
        Money originalPlanned = item.PlannedAmount;
        Money newPlanned = CreateMoney(800m);
        Money newRealized = CreateMoney(750m);
        Money newSpent = CreateMoney(700m);
        PayerSplit newPayerSplit = CreateAlternatePayerSplit();
        AttributionSplit newAttributionSplit = CreateAlternateAttributionSplit();
        int eventCountBefore = budget.DomainEvents.Count;

        // Act
        Result<BudgetItem, BudgetError> result = budget.UpdateItem(
            item.Id,
            plannedAmount: newPlanned,
            realizedAmount: newRealized,
            spentAmount: newSpent,
            payerSplit: newPayerSplit,
            attributionSplit: newAttributionSplit);

        // Assert
        ItShouldSucceed(result);
        ItShouldHavePlannedAmount(result, newPlanned);
        ItShouldHaveRealizedAmount(result, newRealized);
        ItShouldHaveSpentAmount(result, newSpent);
        ItShouldHavePayerSplit(result, newPayerSplit);
        ItShouldHaveAttributionSplit(result, newAttributionSplit);
        ItShouldHaveEmittedExactlyNCorrectionEvents(budget, eventCountBefore, 3);
    }

    [Fact]
    public void UpdateItem_WithSameValuesAsExisting_NoOpNoEvents()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetItem item) = CreateBudgetWithItem();
        Money samePlanned = item.PlannedAmount;
        int eventCountBefore = budget.DomainEvents.Count;

        // Act
        Result<BudgetItem, BudgetError> result = budget.UpdateItem(item.Id, plannedAmount: samePlanned);

        // Assert
        ItShouldSucceed(result);
        ItShouldHavePlannedAmount(result, samePlanned);
        ItShouldNotHaveEmittedNewEvents(budget, eventCountBefore);
    }

    [Fact]
    public void UpdateItem_WithAllNullParams_ReturnsItemUnchanged()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetItem item) = CreateBudgetWithItem();
        Money originalPlanned = item.PlannedAmount;
        PayerSplit originalPayerSplit = item.PayerSplit;
        AttributionSplit originalAttributionSplit = item.AttributionSplit;
        int eventCountBefore = budget.DomainEvents.Count;

        // Act
        Result<BudgetItem, BudgetError> result = budget.UpdateItem(item.Id);

        // Assert
        ItShouldSucceed(result);
        ItShouldHavePlannedAmount(result, originalPlanned);
        ItShouldHavePayerSplit(result, originalPayerSplit);
        ItShouldHaveAttributionSplit(result, originalAttributionSplit);
        ItShouldNotHaveEmittedNewEvents(budget, eventCountBefore);
    }

    [Fact]
    public void UpdateItem_CorrectionEventContainsOldAndNewAmounts()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetItem item) = CreateBudgetWithItem();
        Money oldPlanned = item.PlannedAmount;
        Money newPlanned = CreateMoney(1234m);

        // Act
        Result<BudgetItem, BudgetError> result = budget.UpdateItem(item.Id, plannedAmount: newPlanned);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveCorrectionEventWithAmounts(budget, "PlannedAmount", oldPlanned, newPlanned);
    }

    [Fact]
    public void UpdateItem_RealizedAmountFromNull_OldAmountIsZero()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetItem item) = CreateBudgetWithItem();
        item.RealizedAmount.ShouldBeNull(); // precondition: no realized amount yet
        Money newRealized = CreateMoney(600m);

        // Act
        Result<BudgetItem, BudgetError> result = budget.UpdateItem(item.Id, realizedAmount: newRealized);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveCorrectionEventWithAmounts(budget, "RealizedAmount", Money.Zero("ZAR"), newRealized);
    }

    // -------------------------------------------------------------------------
    // Budget.UpdateItem — Error scenarios
    // -------------------------------------------------------------------------

    [Fact]
    public void UpdateItem_WithNonExistentItemId_ReturnsBudgetItemNotFoundError()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, _) = CreateBudgetWithItem();
        BudgetItemId nonExistentId = BudgetItemId.NewId();

        // Act
        Result<BudgetItem, BudgetError> result = budget.UpdateItem(nonExistentId, plannedAmount: CreateMoney(100m));

        // Assert
        ItShouldFail(result);
        ItShouldBeErrorOfType<BudgetItemNotFoundError>(result);
    }

    [Fact]
    public void UpdateItem_WithDeletedItem_ReturnsBudgetItemNotFoundError()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget budget, BudgetItem item) = CreateBudgetWithItem();
        item.Delete(CreateSoftDeleteStampFactory());

        // Act
        Result<BudgetItem, BudgetError> result = budget.UpdateItem(item.Id, plannedAmount: CreateMoney(100m));

        // Assert
        ItShouldFail(result);
        ItShouldBeErrorOfType<BudgetItemNotFoundError>(result);
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

    private static PayerSplit CreateAlternatePayerSplit()
    {
        UserId user1 = UserId.NewId();
        UserId user2 = UserId.NewId();
        IReadOnlyList<PayerAllocation> allocations =
            [new PayerAllocation(user1, 60), new PayerAllocation(user2, 40)];
        return PayerSplit.Create(allocations).Value;
    }

    private static AttributionSplit CreateAttributionSplit()
    {
        IReadOnlyList<AttributionAllocation> allocations =
            [new AttributionAllocation(Attribution.Main, 100)];
        return AttributionSplit.Create(allocations).Value;
    }

    private static AttributionSplit CreateAlternateAttributionSplit()
    {
        IReadOnlyList<AttributionAllocation> allocations =
            [new AttributionAllocation(Attribution.Main, 50), new AttributionAllocation(Attribution.Rental, 50)];
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

    private static void ItShouldHavePlannedAmount(Result<BudgetItem, BudgetError> result, Money expected)
    {
        result.Value.PlannedAmount.ShouldBe(expected);
    }

    private static void ItShouldHavePlannedAmountUnchanged(Result<BudgetItem, BudgetError> result, Money expected)
    {
        result.Value.PlannedAmount.ShouldBe(expected);
    }

    private static void ItShouldHaveRealizedAmount(Result<BudgetItem, BudgetError> result, Money expected)
    {
        result.Value.RealizedAmount.ShouldBe(expected);
    }

    private static void ItShouldHaveRealizedAmountUnchanged(Result<BudgetItem, BudgetError> result, Money? expected)
    {
        result.Value.RealizedAmount.ShouldBe(expected);
    }

    private static void ItShouldHaveSpentAmount(Result<BudgetItem, BudgetError> result, Money expected)
    {
        result.Value.SpentAmount.ShouldBe(expected);
    }

    private static void ItShouldHaveSpentAmountUnchanged(Result<BudgetItem, BudgetError> result, Money? expected)
    {
        result.Value.SpentAmount.ShouldBe(expected);
    }

    private static void ItShouldHavePayerSplit(Result<BudgetItem, BudgetError> result, PayerSplit expected)
    {
        result.Value.PayerSplit.ShouldBe(expected);
    }

    private static void ItShouldHaveAttributionSplit(Result<BudgetItem, BudgetError> result, AttributionSplit expected)
    {
        result.Value.AttributionSplit.ShouldBe(expected);
    }

    private static void ItShouldHaveEmittedCorrectionEvent(
        Menlo.Lib.Budget.Entities.Budget budget,
        BudgetItemId itemId,
        string field,
        Money oldAmount,
        Money newAmount,
        int eventCountBefore)
    {
        IReadOnlyList<IDomainEvent> newEvents = budget.DomainEvents.Skip(eventCountBefore).ToList();
        newEvents.Count.ShouldBe(1);
        BudgetItemCorrectedEvent evt = newEvents[0].ShouldBeOfType<BudgetItemCorrectedEvent>();
        evt.BudgetId.ShouldBe(budget.Id);
        evt.BudgetItemId.ShouldBe(itemId);
        evt.Field.ShouldBe(field);
        evt.OldAmount.ShouldBe(oldAmount);
        evt.NewAmount.ShouldBe(newAmount);
    }

    private static void ItShouldHaveEmittedExactlyNCorrectionEvents(
        Menlo.Lib.Budget.Entities.Budget budget,
        int eventCountBefore,
        int expectedCount)
    {
        IReadOnlyList<IDomainEvent> newEvents = budget.DomainEvents.Skip(eventCountBefore).ToList();
        newEvents.Count.ShouldBe(expectedCount);
        newEvents.ShouldAllBe(e => e is BudgetItemCorrectedEvent);
    }

    private static void ItShouldNotHaveEmittedNewEvents(
        Menlo.Lib.Budget.Entities.Budget budget,
        int eventCountBefore)
    {
        budget.DomainEvents.Count.ShouldBe(eventCountBefore);
    }

    private static void ItShouldHaveCorrectionEventWithAmounts(
        Menlo.Lib.Budget.Entities.Budget budget,
        string field,
        Money expectedOldAmount,
        Money expectedNewAmount)
    {
        IDomainEvent lastEvent = budget.DomainEvents.Last();
        BudgetItemCorrectedEvent evt = lastEvent.ShouldBeOfType<BudgetItemCorrectedEvent>();
        evt.Field.ShouldBe(field);
        evt.OldAmount.ShouldBe(expectedOldAmount);
        evt.NewAmount.ShouldBe(expectedNewAmount);
    }
}
