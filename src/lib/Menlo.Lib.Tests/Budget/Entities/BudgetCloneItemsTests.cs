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
/// Tests for CloneForYear budget item cloning behaviour.
/// </summary>
public sealed class BudgetCloneItemsTests
{
    [Fact]
    public void GivenSourceWithItems_WhenCloning_ThenItemsAreCopied()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget source, BudgetCategoryId leafId) =
            CreateBudgetWithLeafAndItems(months: [1, 2, 3]);

        // Act
        Result<Menlo.Lib.Budget.Entities.Budget, BudgetError> result =
            Menlo.Lib.Budget.Entities.Budget.CloneForYear(source, 2025, CreateAuditStampFactory());

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(3);
    }

    [Fact]
    public void GivenSourceWithItems_WhenCloning_ThenItemsHaveNewIds()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget source, _) = CreateBudgetWithLeafAndItems(months: [1]);

        // Act
        Result<Menlo.Lib.Budget.Entities.Budget, BudgetError> result =
            Menlo.Lib.Budget.Entities.Budget.CloneForYear(source, 2025, CreateAuditStampFactory());

        // Assert
        result.IsSuccess.ShouldBeTrue();
        BudgetItem cloned = result.Value.Items.First();
        BudgetItem original = source.Items.First();
        cloned.Id.ShouldNotBe(original.Id);
    }

    [Fact]
    public void GivenSourceWithItems_WhenCloning_ThenPlannedAmountPreserved()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget source, _) = CreateBudgetWithLeafAndItems(months: [6]);

        // Act
        Result<Menlo.Lib.Budget.Entities.Budget, BudgetError> result =
            Menlo.Lib.Budget.Entities.Budget.CloneForYear(source, 2025, CreateAuditStampFactory());

        // Assert
        BudgetItem cloned = result.Value.Items.First();
        cloned.PlannedAmount.Amount.ShouldBe(500m);
        cloned.PlannedAmount.Currency.ShouldBe("ZAR");
    }

    [Fact]
    public void GivenSourceWithItems_WhenCloning_ThenMonthAndFlowPreserved()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget source, _) = CreateBudgetWithLeafAndItems(months: [7]);

        // Act
        Result<Menlo.Lib.Budget.Entities.Budget, BudgetError> result =
            Menlo.Lib.Budget.Entities.Budget.CloneForYear(source, 2025, CreateAuditStampFactory());

        // Assert
        BudgetItem cloned = result.Value.Items.First();
        cloned.Month.ShouldBe(7);
        cloned.BudgetFlow.ShouldBe(BudgetFlow.Expense);
    }

    [Fact]
    public void GivenSourceWithItems_WhenCloning_ThenPayerAndAttributionSplitsPreserved()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget source, _) = CreateBudgetWithLeafAndItems(months: [1]);

        // Act
        Result<Menlo.Lib.Budget.Entities.Budget, BudgetError> result =
            Menlo.Lib.Budget.Entities.Budget.CloneForYear(source, 2025, CreateAuditStampFactory());

        // Assert
        BudgetItem cloned = result.Value.Items.First();
        cloned.PayerSplit.Allocations.Count.ShouldBe(1);
        cloned.PayerSplit.Allocations[0].Percent.ShouldBe(100);
        cloned.AttributionSplit.Allocations.Count.ShouldBe(1);
        cloned.AttributionSplit.Allocations[0].Percent.ShouldBe(100);
    }

    [Fact]
    public void GivenSourceWithRealizedItems_WhenCloning_ThenRealizedAndSpentAreNull()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget source, BudgetCategoryId leafId) =
            CreateBudgetWithLeafAndItems(months: [1]);
        // Realize and spend the source item
        BudgetItem sourceItem = source.Items.First();
        source.RealizeItem(sourceItem.Id, CreateMoney(480m));
        source.RecordItemSpent(sourceItem.Id, CreateMoney(480m));

        // Act
        Result<Menlo.Lib.Budget.Entities.Budget, BudgetError> result =
            Menlo.Lib.Budget.Entities.Budget.CloneForYear(source, 2025, CreateAuditStampFactory());

        // Assert
        BudgetItem cloned = result.Value.Items.First();
        cloned.RealizedAmount.ShouldBeNull();
        cloned.SpentAmount.ShouldBeNull();
    }

    [Fact]
    public void GivenSourceWithItems_WhenCloning_ThenAdjustmentRuleIsNullAndManualOverrideTrue()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget source, _) = CreateBudgetWithLeafAndItems(months: [1]);

        // Act
        Result<Menlo.Lib.Budget.Entities.Budget, BudgetError> result =
            Menlo.Lib.Budget.Entities.Budget.CloneForYear(source, 2025, CreateAuditStampFactory());

        // Assert
        BudgetItem cloned = result.Value.Items.First();
        cloned.AdjustmentRuleId.ShouldBeNull();
        cloned.IsManualOverride.ShouldBeTrue();
    }

    [Fact]
    public void GivenSourceWithItems_WhenCloning_ThenCategoryIdsMappedCorrectly()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget source, BudgetCategoryId sourceLeafId) =
            CreateBudgetWithLeafAndItems(months: [1]);

        // Act
        Result<Menlo.Lib.Budget.Entities.Budget, BudgetError> result =
            Menlo.Lib.Budget.Entities.Budget.CloneForYear(source, 2025, CreateAuditStampFactory());

        // Assert
        BudgetItem cloned = result.Value.Items.First();
        // Cloned item category should be different from source (new IDs)
        cloned.CategoryId.ShouldNotBe(sourceLeafId);
        // But should reference a category that exists in the new budget
        result.Value.Categories.ShouldContain(c => c.Id == cloned.CategoryId);
    }

    [Fact]
    public void GivenSourceWithItems_WhenCloning_ThenBudgetItemPlannedEventsEmitted()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget source, _) =
            CreateBudgetWithLeafAndItems(months: [1, 2, 3]);

        // Act
        Result<Menlo.Lib.Budget.Entities.Budget, BudgetError> result =
            Menlo.Lib.Budget.Entities.Budget.CloneForYear(source, 2025, CreateAuditStampFactory());

        // Assert
        IReadOnlyCollection<IDomainEvent> events = result.Value.DomainEvents;
        events.OfType<BudgetItemPlannedEvent>().Count().ShouldBe(3);
    }

    [Fact]
    public void GivenSourceWithDeletedItems_WhenCloning_ThenDeletedItemsNotCloned()
    {
        // Arrange
        (Menlo.Lib.Budget.Entities.Budget source, BudgetCategoryId leafId) =
            CreateBudgetWithLeafAndItems(months: [1, 2]);
        // Delete one item
        BudgetItem toDelete = source.Items.First(i => i.Month == 2);
        source.DeleteItem(toDelete.Id, CreateSoftDeleteStampFactory());

        // Act
        Result<Menlo.Lib.Budget.Entities.Budget, BudgetError> result =
            Menlo.Lib.Budget.Entities.Budget.CloneForYear(source, 2025, CreateAuditStampFactory());

        // Assert
        result.Value.Items.Count.ShouldBe(1);
        result.Value.Items.First().Month.ShouldBe(1);
    }

    [Fact]
    public void GivenSourceWithNoItems_WhenCloning_ThenNewBudgetHasNoItems()
    {
        // Arrange
        IAuditStampFactory auditFactory = CreateAuditStampFactory();
        Menlo.Lib.Budget.Entities.Budget source =
            Menlo.Lib.Budget.Entities.Budget.Create(HouseholdId.NewId(), 2024, auditFactory).Value;
        source.AddCategory("Expenses", BudgetFlow.Expense);

        // Act
        Result<Menlo.Lib.Budget.Entities.Budget, BudgetError> result =
            Menlo.Lib.Budget.Entities.Budget.CloneForYear(source, 2025, auditFactory);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(0);
    }

    [Fact]
    public void GivenExistingCloneForYearTests_WhenCloning_ThenCategoriesStillWork()
    {
        // Verify backward compat: categories are still cloned correctly
        IAuditStampFactory auditFactory = CreateAuditStampFactory();
        Menlo.Lib.Budget.Entities.Budget source =
            Menlo.Lib.Budget.Entities.Budget.Create(HouseholdId.NewId(), 2024, auditFactory).Value;
        source.AddCategory("Income", BudgetFlow.Income);
        CategoryNode root = source.AddCategory("Expenses", BudgetFlow.Expense).Value;
        source.AddCategory("Utilities", BudgetFlow.Expense, parentId: root.Id);

        Result<Menlo.Lib.Budget.Entities.Budget, BudgetError> result =
            Menlo.Lib.Budget.Entities.Budget.CloneForYear(source, 2025, auditFactory);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Categories.Count.ShouldBe(3);
    }

    // =========================================================================
    // Setup helpers
    // =========================================================================

    private static (Menlo.Lib.Budget.Entities.Budget Budget, BudgetCategoryId LeafCategoryId)
        CreateBudgetWithLeafAndItems(int[] months)
    {
        IAuditStampFactory auditFactory = CreateAuditStampFactory();
        Menlo.Lib.Budget.Entities.Budget budget =
            Menlo.Lib.Budget.Entities.Budget.Create(HouseholdId.NewId(), 2024, auditFactory).Value;

        CategoryNode root = budget.AddCategory("Expenses", BudgetFlow.Expense).Value;
        CategoryNode leaf = budget.AddCategory("Electricity", BudgetFlow.Expense, parentId: root.Id).Value;

        foreach (int month in months)
        {
            budget.AddItem(leaf.Id, month, BudgetFlow.Expense, CreateMoney(500m),
                CreatePayerSplit(), CreateAttributionSplit());
        }

        return (budget, leaf.Id);
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
        public AuditStamp CreateStamp() => stamp;
    }

    private static IAuditStampFactory CreateAuditStampFactory() =>
        new FakeAuditStampFactory(new AuditStamp(UserId.NewId(), DateTimeOffset.UtcNow));

    private sealed class FakeSoftDeleteStampFactory(SoftDeleteStamp stamp) : ISoftDeleteStampFactory
    {
        public SoftDeleteStamp CreateStamp() => stamp;
    }

    private static ISoftDeleteStampFactory CreateSoftDeleteStampFactory() =>
        new FakeSoftDeleteStampFactory(new SoftDeleteStamp(UserId.NewId(), DateTimeOffset.UtcNow));
}
