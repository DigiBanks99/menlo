using Menlo.Application.Budget;
using Menlo.Application.Tests.Fixtures;
using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.Enums;
using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BudgetAggregate = Menlo.Lib.Budget.Entities.Budget;

namespace Menlo.Application.Tests.Budget;

[ClassDataSource<PersistenceFixture>(Shared = SharedType.PerTestSession)]
[NotInParallel("Persistence")]
public sealed class BudgetItemPersistenceIntegrationTests(PersistenceFixture fixture)
{
    [Test]
    public async Task GivenItemRealizedAndSpent_WhenReloaded_ThenRealizedAndSpentAmountsRoundTrip()
    {
        (BudgetId budgetId, BudgetItemId itemId) = await PersistBudgetWithItemAsync(item =>
        {
            item.Budget.RealizeItem(item.ItemId, Money.Create(1450m, "ZAR").Value).IsSuccess.ShouldBeTrue();
            item.Budget.RecordItemSpent(item.ItemId, Money.Create(1500m, "ZAR").Value).IsSuccess.ShouldBeTrue();
        });

        BudgetItem loaded = await LoadItemAsync(budgetId, itemId);

        ItShouldHaveTheRealizedAmount();
        ItShouldHaveTheSpentAmount();

        void ItShouldHaveTheRealizedAmount() => loaded.RealizedAmount.ShouldBe(Money.Create(1450m, "ZAR").Value);

        void ItShouldHaveTheSpentAmount() => loaded.SpentAmount.ShouldBe(Money.Create(1500m, "ZAR").Value);
    }

    [Test]
    public async Task GivenItemNeverRealizedOrSpent_WhenReloaded_ThenRealizedAndSpentAmountsAreNull()
    {
        (BudgetId budgetId, BudgetItemId itemId) = await PersistBudgetWithItemAsync(_ => { });

        BudgetItem loaded = await LoadItemAsync(budgetId, itemId);

        ItShouldHaveNoRealizedAmount();
        ItShouldHaveNoSpentAmount();

        void ItShouldHaveNoRealizedAmount() => loaded.RealizedAmount.ShouldBeNull();

        void ItShouldHaveNoSpentAmount() => loaded.SpentAmount.ShouldBeNull();
    }

    private async Task<(BudgetId BudgetId, BudgetItemId ItemId)> PersistBudgetWithItemAsync(
        Action<(BudgetAggregate Budget, BudgetItemId ItemId)> recordAmounts)
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        IBudgetContext ctx = scope.ServiceProvider.GetRequiredService<IBudgetContext>();

        BudgetAggregate budget = BudgetAggregate.Create(
            HouseholdId.NewId(),
            2033,
            scope.ServiceProvider.GetRequiredService<IAuditStampFactory>()).Value;

        CategoryNode category = budget.AddCategory("Housing", BudgetFlow.Expense).Value;
        ctx.CanonicalCategories.Add(CanonicalCategory.Create(category.CanonicalCategoryId, category.Name.Value));

        BudgetItem item = budget.AddItem(
            category.Id,
            1,
            BudgetFlow.Expense,
            Money.Create(1400m, "ZAR").Value,
            PayerSplit.Create([new PayerAllocation(UserId.NewId(), 100)]).Value,
            AttributionSplit.Create([new AttributionAllocation(Attribution.Main, 100)]).Value).Value;

        recordAmounts((budget, item.Id));

        ctx.Budgets.Add(budget);
        await ctx.SaveChangesAsync(TestContext.Current!.Execution.CancellationToken);

        return (budget.Id, item.Id);
    }

    private async Task<BudgetItem> LoadItemAsync(BudgetId budgetId, BudgetItemId itemId)
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        IBudgetContext ctx = scope.ServiceProvider.GetRequiredService<IBudgetContext>();

        BudgetAggregate? budget = await ctx.Budgets
            .Include(b => b.Items)
            .AsNoTracking()
            .SingleOrDefaultAsync(b => b.Id == budgetId, TestContext.Current!.Execution.CancellationToken);

        budget.ShouldNotBeNull();
        return budget.Items.Single(i => i.Id == itemId);
    }
}
