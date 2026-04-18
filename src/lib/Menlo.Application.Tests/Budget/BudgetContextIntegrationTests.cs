using Menlo.Application.Budget;
using Menlo.Application.Tests.Fixtures;
using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BudgetAggregate = Menlo.Lib.Budget.Entities.Budget;

namespace Menlo.Application.Tests.Budget;

[Collection("Persistence")]
public sealed class BudgetContextIntegrationTests(PersistenceFixture fixture)
{
    [Fact]
    public async Task SaveChangesAsync_WithNewBudget_PersistsBudgetCorrectly()
    {
        BudgetAggregate budget = await CreateAndPersistBudgetAsync();

        BudgetAggregate? persisted = await LoadBudgetAsync(budget.Id);

        ItShouldHavePersistedBudget(persisted, budget);
        ItShouldHavePersistedAuditFields(persisted);
    }

    [Fact]
    public async Task SaveChangesAsync_WithBudgetAndCategories_PersistsCategoriesCorrectly()
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        IBudgetContext ctx = scope.ServiceProvider.GetRequiredService<IBudgetContext>();

        BudgetAggregate budget = BudgetAggregate.Create(
            HouseholdId.NewId(),
            2030,
            scope.ServiceProvider.GetRequiredService<IAuditStampFactory>()).Value;

        budget.AddCategory("Income").Value.ToString();
        budget.AddCategory("Expenses");

        ctx.Budgets.Add(budget);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        using IServiceScope readScope = fixture.Services.CreateScope();
        IBudgetContext readCtx = readScope.ServiceProvider.GetRequiredService<IBudgetContext>();
        BudgetAggregate? loaded = await readCtx.Budgets
            .Include(b => b.Categories)
            .AsNoTracking()
            .SingleOrDefaultAsync(b => b.Id == budget.Id, TestContext.Current.CancellationToken);

        loaded.ShouldNotBeNull();
        loaded.Categories.Count.ShouldBe(2);
        loaded.Categories.Any(c => c.Name.Value == "Income").ShouldBeTrue();
        loaded.Categories.Any(c => c.Name.Value == "Expenses").ShouldBeTrue();
    }

    [Fact]
    public async Task SaveChangesAsync_DuplicateHouseholdYear_ThrowsException()
    {
        HouseholdId householdId = HouseholdId.NewId();

        await using (AsyncServiceScope scope = fixture.Services.CreateAsyncScope())
        {
            IBudgetContext ctx = scope.ServiceProvider.GetRequiredService<IBudgetContext>();
            BudgetAggregate budget = BudgetAggregate.Create(
                householdId,
                2031,
                scope.ServiceProvider.GetRequiredService<IAuditStampFactory>()).Value;
            ctx.Budgets.Add(budget);
            await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using AsyncServiceScope scope2 = fixture.Services.CreateAsyncScope();
        IBudgetContext ctx2 = scope2.ServiceProvider.GetRequiredService<IBudgetContext>();
        BudgetAggregate duplicate = BudgetAggregate.Create(
            householdId,
            2031,
            scope2.ServiceProvider.GetRequiredService<IAuditStampFactory>()).Value;
        ctx2.Budgets.Add(duplicate);

        Func<Task> act = () => ctx2.SaveChangesAsync(TestContext.Current.CancellationToken);
        await act.ShouldThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Query_WithSoftDeletedBudget_FiltersSoftDeletedBudgets()
    {
        BudgetAggregate budget = await CreateAndPersistBudgetAsync(year: 2032);

        using (IServiceScope scope = fixture.Services.CreateScope())
        {
            IBudgetContext ctx = scope.ServiceProvider.GetRequiredService<IBudgetContext>();
            BudgetAggregate? toDelete = await ctx.Budgets.SingleOrDefaultAsync(
                b => b.Id == budget.Id, TestContext.Current.CancellationToken);
            toDelete.ShouldNotBeNull();

            toDelete.Delete(scope.ServiceProvider.GetRequiredService<ISoftDeleteStampFactory>());
            await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using IServiceScope readScope = fixture.Services.CreateScope();
        IBudgetContext readCtx = readScope.ServiceProvider.GetRequiredService<IBudgetContext>();

        BudgetAggregate? found = await readCtx.Budgets
            .AsNoTracking()
            .SingleOrDefaultAsync(b => b.Id == budget.Id, TestContext.Current.CancellationToken);
        found.ShouldBeNull();

        BudgetAggregate? deleted = await readCtx.Budgets
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(b => b.Id == budget.Id, TestContext.Current.CancellationToken);
        deleted.ShouldNotBeNull();
        deleted.IsDeleted.ShouldBeTrue();
        deleted.DeletedAt.ShouldNotBeNull();
        deleted.DeletedBy.ShouldNotBeNull();
    }

    private async Task<BudgetAggregate> CreateAndPersistBudgetAsync(int year = 2029)
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        IBudgetContext ctx = scope.ServiceProvider.GetRequiredService<IBudgetContext>();
        BudgetAggregate budget = BudgetAggregate.Create(
            HouseholdId.NewId(),
            year,
            scope.ServiceProvider.GetRequiredService<IAuditStampFactory>()).Value;

        ctx.Budgets.Add(budget);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        return budget;
    }

    private async Task<BudgetAggregate?> LoadBudgetAsync(BudgetId budgetId)
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        IBudgetContext ctx = scope.ServiceProvider.GetRequiredService<IBudgetContext>();
        return await ctx.Budgets
            .AsNoTracking()
            .SingleOrDefaultAsync(b => b.Id == budgetId, TestContext.Current.CancellationToken);
    }

    private static void ItShouldHavePersistedBudget(BudgetAggregate? persisted, BudgetAggregate budget)
    {
        persisted.ShouldNotBeNull();
        persisted.Id.ShouldBe(budget.Id);
        persisted.HouseholdId.ShouldBe(budget.HouseholdId);
        persisted.Year.ShouldBe(budget.Year);
        persisted.Status.ShouldBe(budget.Status);
    }

    private static void ItShouldHavePersistedAuditFields(BudgetAggregate? persisted)
    {
        persisted.ShouldNotBeNull();
        persisted.CreatedBy.ShouldBe(TestAuditStampFactory.TestUserId);
        persisted.CreatedAt.ShouldNotBeNull();
        persisted.ModifiedBy.ShouldBe(TestAuditStampFactory.TestUserId);
        persisted.ModifiedAt.ShouldNotBeNull();
    }
}
