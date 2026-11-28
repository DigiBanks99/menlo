using Menlo.Application.Budget;
using Menlo.Application.Budget.EntityConfigurations;
using Menlo.Application.Tests.Fixtures;
using Menlo.Lib.Budget.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using BudgetAggregate = Menlo.Lib.Budget.Entities.Budget;

namespace Menlo.Application.Tests.Budget;

[ClassDataSource<PersistenceFixture>(Shared = SharedType.PerTestSession)]
[NotInParallel("Persistence")]
public sealed class BudgetContextIntegrationTests(PersistenceFixture fixture)
{
    private static CancellationToken Cancellation => TestContext.Current!.Execution.CancellationToken;

    [Test]
    public async Task ABudgetIsSaved()
    {
        BudgetAggregate budget = BudgetAggregate.Create(UnusedYear()).Value;

        await SaveAsync(budget);
        BudgetAggregate? stored = await LoadAsync(budget.Year);

        ItShouldBeStoredUnderItsYear();
        ItShouldBeStampedByWhoeverSavedIt();

        void ItShouldBeStoredUnderItsYear() => stored.ShouldNotBeNull().ShouldSatisfyAllConditions(
            b => b.Id.ShouldBe(budget.Id),
            b => b.Year.ShouldBe(budget.Year));

        void ItShouldBeStampedByWhoeverSavedIt() => stored!.ShouldSatisfyAllConditions(
            b => b.CreatedBy.ShouldBe(TestAuditStampFactory.TestUserId),
            b => b.CreatedAt.ShouldNotBeNull(),
            b => b.ModifiedBy.ShouldBe(TestAuditStampFactory.TestUserId),
            b => b.ModifiedAt.ShouldNotBeNull());
    }

    [Test]
    public async Task ASecondBudgetIsSavedForTheSameYear()
    {
        int year = UnusedYear();
        await SaveAsync(BudgetAggregate.Create(year).Value);

        Exception? exception = await CatchAsync(() => SaveAsync(BudgetAggregate.Create(year).Value));

        ItShouldBeRefusedByTheYearIndex();

        void ItShouldBeRefusedByTheYearIndex() => exception
            .ShouldBeOfType<DbUpdateException>()
            .InnerException.ShouldBeOfType<PostgresException>().ShouldSatisfyAllConditions(
                e => e.SqlState.ShouldBe(PostgresErrorCodes.UniqueViolation),
                e => e.ConstraintName.ShouldBe(BudgetEntityTypeConfiguration.YearIndexName));
    }

    [Test]
    public async Task ABudgetIsSavedWithItsCategories()
    {
        BudgetAggregate budget = BudgetAggregate.Create(UnusedYear()).Value;
        Category home = budget.AddCategory("Home", parentId: null).Value;
        Category electricity = budget.AddCategory("Electricity", home.Id).Value;

        await SaveAsync(budget);
        BudgetAggregate? stored = await LoadAsync(budget.Year);

        ItShouldLoadTheCategoriesWithTheBudget();
        ItShouldKeepTheTree();
        ItShouldStampEachCategory();

        void ItShouldLoadTheCategoriesWithTheBudget() => stored.ShouldNotBeNull().Categories
            .Select(c => c.Id).ShouldBe([home.Id, electricity.Id], ignoreOrder: true);

        void ItShouldKeepTheTree() => stored!.Categories.Single(c => c.Id == electricity.Id).ShouldSatisfyAllConditions(
            c => c.Name.ShouldBe("Electricity"),
            c => c.ParentId.ShouldBe(home.Id),
            c => c.BudgetId.ShouldBe(budget.Id));

        void ItShouldStampEachCategory() => stored!.Categories.ShouldAllBe(c =>
            c.CreatedBy == TestAuditStampFactory.TestUserId && c.CreatedAt != null);
    }

    [Test]
    public async Task ACategoryIsAddedToASavedBudget()
    {
        BudgetAggregate budget = BudgetAggregate.Create(UnusedYear()).Value;
        Category home = budget.AddCategory("Home", parentId: null).Value;
        await SaveAsync(budget);

        Category school = await ChangeAsync(budget.Year, b => b.AddCategory("School", parentId: null).Value);
        Category electricity = await ChangeAsync(budget.Year, b => b.AddCategory("Electricity", home.Id).Value);
        BudgetAggregate? stored = await LoadAsync(budget.Year);

        ItShouldStoreTheNewCategories();

        void ItShouldStoreTheNewCategories() => stored.ShouldNotBeNull().Categories
            .Select(c => (c.Id, c.Name, c.ParentId))
            .ShouldBe([(home.Id, "Home", null), (school.Id, "School", null), (electricity.Id, "Electricity", home.Id)], ignoreOrder: true);
    }

    private static int _lastYear = Random.Shared.Next(1000, 5000);

    // A year no other test in the session uses, since the database is shared and holds one budget per year
    private static int UnusedYear() => Interlocked.Increment(ref _lastYear);

    private static async Task<Exception?> CatchAsync(Func<Task> action)
    {
        try
        {
            await action();
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private async Task SaveAsync(BudgetAggregate budget)
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        IBudgetContext context = scope.ServiceProvider.GetRequiredService<IBudgetContext>();
        context.Budgets.Add(budget);
        await context.SaveChangesAsync(Cancellation);
    }

    private async Task<T> ChangeAsync<T>(int year, Func<BudgetAggregate, T> change)
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        IBudgetContext context = scope.ServiceProvider.GetRequiredService<IBudgetContext>();
        BudgetAggregate budget = await context.Budgets.SingleAsync(b => b.Year == year, Cancellation);
        T changed = change(budget);
        await context.SaveChangesAsync(Cancellation);
        return changed;
    }

    private async Task<BudgetAggregate?> LoadAsync(int year)
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        IBudgetContext context = scope.ServiceProvider.GetRequiredService<IBudgetContext>();
        return await context.Budgets.AsNoTracking().SingleOrDefaultAsync(b => b.Year == year, Cancellation);
    }
}
