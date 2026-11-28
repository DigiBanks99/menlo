using Menlo.Application.Common;
using Menlo.Application.Tests.Fixtures;
using Menlo.Lib.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace Menlo.Application.Tests.Common;

/// <summary>
/// Tests the full migration lifecycle including rollback, exercising InitialCreate.Down().
/// Uses its own container to avoid interfering with the shared Persistence fixture.
/// </summary>
public sealed class MigrationTests : IAsyncDisposable
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17")
        .Build();

    private IHost _host = null!;

    [Before(Test)]
    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Production
        });

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:menlo"] = $"{_container.GetConnectionString()};SSL Mode=Disable"
        });

        builder.AddMenloApplication();
        builder.Services.AddScoped<IAuditStampFactory, TestAuditStampFactory>();
        builder.Services.AddScoped<ISoftDeleteStampFactory, TestSoftDeleteStampFactory>();

        _host = builder.Build();

        using IServiceScope scope = _host.Services.CreateScope();
        await scope.ServiceProvider
            .GetRequiredService<MenloDbContext>()
            .Database.MigrateAsync(TestContext.Current!.Execution.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        // _host is null when setup failed before building it; don't hide that failure behind a NullReferenceException.
        if (_host is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else
        {
            _host?.Dispose();
        }
        await _container.DisposeAsync();
    }

    [Test]
    public async Task GivenMigratedDatabase_WhenRolledBackToBaseline_ThenMigrationsTableIsEmpty()
    {
        using IServiceScope scope = _host.Services.CreateScope();
        MenloDbContext ctx = scope.ServiceProvider.GetRequiredService<MenloDbContext>();
        IMigrator migrator = ctx.Database.GetService<IMigrator>()!;

        // Act - rollback all migrations (exercises InitialCreate.Down())
        await migrator.MigrateAsync("0", TestContext.Current!.Execution.CancellationToken);

        // Assert - no applied migrations remain
        IEnumerable<string> applied = await ctx.Database.GetAppliedMigrationsAsync(TestContext.Current!.Execution.CancellationToken);
        ItShouldHaveNoAppliedMigrations(applied);
    }

    [Test]
    public async Task GivenHouseholdScopedData_WhenUpgradingToRemoveHouseholds_ThenOnlyUsersSurvive()
    {
        CancellationToken ct = TestContext.Current!.Execution.CancellationToken;
        using IServiceScope scope = _host.Services.CreateScope();
        MenloDbContext ctx = scope.ServiceProvider.GetRequiredService<MenloDbContext>();
        IMigrator migrator = ctx.Database.GetService<IMigrator>()!;

        await migrator.MigrateAsync(PreviousMigration, ct);
        Guid userId = await SeedHouseholdScopedDataAsync(ctx, ct);

        await migrator.MigrateAsync(RemoveHouseholdsMigration, ct);

        await ItShouldHaveKeptTheUser(ctx, userId, ct);
        await ItShouldHaveDroppedEveryHouseholdScopedRow(ctx, ct);
        await ItShouldHaveDroppedTheHouseholdSchema(ctx, ct);
    }

    [Test]
    public async Task GivenAnEmptyDatabase_WhenMigrated_ThenNoShippedBudgetTablesAreLeft()
    {
        CancellationToken ct = TestContext.Current!.Execution.CancellationToken;
        using IServiceScope scope = _host.Services.CreateScope();
        MenloDbContext ctx = scope.ServiceProvider.GetRequiredService<MenloDbContext>();

        await ItShouldHaveNoShippedBudgetTablesOrSchema(ctx, ct);
    }

    [Test]
    public async Task GivenAnEmptyDatabase_WhenMigrated_ThenTheBudgetsTableKeepsOneBudgetPerYear()
    {
        CancellationToken ct = TestContext.Current!.Execution.CancellationToken;
        using IServiceScope scope = _host.Services.CreateScope();
        MenloDbContext ctx = scope.ServiceProvider.GetRequiredService<MenloDbContext>();

        List<string> columns = await ctx.Database
            .SqlQuery<string>($@"SELECT column_name AS ""Value"" FROM information_schema.columns
                WHERE table_schema = 'budget' AND table_name = 'budgets' ORDER BY column_name")
            .ToListAsync(ct);
        long uniqueYearIndexes = await ctx.Database
            .SqlQuery<long>($@"SELECT count(*) AS ""Value"" FROM pg_indexes
                WHERE schemaname = 'budget' AND tablename = 'budgets' AND indexname = 'ix_budgets_year'
                  AND indexdef LIKE 'CREATE UNIQUE INDEX%(year)'")
            .SingleAsync(ct);

        ItShouldHaveTheBudgetColumns();
        ItShouldHaveAUniqueIndexOnTheYear();

        void ItShouldHaveTheBudgetColumns() => columns.ShouldBe(
            ["created_at", "created_by", "id", "modified_at", "modified_by", "year"]);

        void ItShouldHaveAUniqueIndexOnTheYear() => uniqueYearIndexes.ShouldBe(1);
    }

    [Test]
    public async Task GivenAMigratedDatabase_WhenRolledBackBeforeAddBudgets_ThenTheBudgetSchemaIsGone()
    {
        CancellationToken ct = TestContext.Current!.Execution.CancellationToken;
        using IServiceScope scope = _host.Services.CreateScope();
        MenloDbContext ctx = scope.ServiceProvider.GetRequiredService<MenloDbContext>();
        IMigrator migrator = ctx.Database.GetService<IMigrator>()!;

        await migrator.MigrateAsync(RemoveShippedBudgetMigration, ct);
        long schemas = await ctx.Database
            .SqlQuery<long>($@"SELECT count(*) AS ""Value"" FROM information_schema.schemata WHERE schema_name = 'budget'")
            .SingleAsync(ct);

        ItShouldHaveDroppedTheBudgetSchema();

        void ItShouldHaveDroppedTheBudgetSchema() => schemas.ShouldBe(0);
    }

    [Test]
    public async Task GivenBudgetRows_WhenUpgradingToRemoveTheShippedBudget_ThenTheBudgetTablesAreDroppedAndUsersSurvive()
    {
        CancellationToken ct = TestContext.Current!.Execution.CancellationToken;
        using IServiceScope scope = _host.Services.CreateScope();
        MenloDbContext ctx = scope.ServiceProvider.GetRequiredService<MenloDbContext>();
        IMigrator migrator = ctx.Database.GetService<IMigrator>()!;

        await migrator.MigrateAsync(RemoveHouseholdsMigration, ct);
        Guid userId = await SeedShippedBudgetAsync(ctx, ct);

        await migrator.MigrateAsync(RemoveShippedBudgetMigration, ct);

        await ItShouldHaveKeptTheUser(ctx, userId, ct);
        await ItShouldHaveNoShippedBudgetTablesOrSchema(ctx, ct);
    }

    private const string PreviousMigration = "20261104121202_MapBudgetItemRealizedAndSpentAmounts";
    private const string RemoveHouseholdsMigration = "20261115112130_RemoveHouseholds";
    private const string RemoveShippedBudgetMigration = "20261128141016_RemoveShippedBudget";

    // A user and a budget with a category and an item, in the schema the shipped budget left behind
    private static async Task<Guid> SeedShippedBudgetAsync(MenloDbContext ctx, CancellationToken ct)
    {
        Guid userId = Guid.NewGuid();
        Guid budgetId = Guid.NewGuid();
        Guid canonicalId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();

        await ctx.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO shared.users (id, external_id, email, display_name, is_deleted) VALUES ({userId}, 'migration-user', 'migration@menlo.test', 'Migration User', false)", ct);
        await ctx.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO budget_schema.budgets (id, year, status, is_deleted) VALUES ({budgetId}, 2026, 'Active', false)", ct);
        await ctx.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO budget_schema.canonical_categories (id, name) VALUES ({canonicalId}, 'Housing')", ct);
        await ctx.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO budget_schema.budget_categories (id, budget_id, canonical_category_id, name, budget_flow, is_deleted) VALUES ({categoryId}, {budgetId}, {canonicalId}, 'Housing', 'Expense', false)", ct);
        await ctx.Database.ExecuteSqlInterpolatedAsync($@"INSERT INTO budget_schema.budget_items (id, budget_id, category_id, month, budget_flow, payer_split, attribution_split, planned_amount, planned_currency, is_manual_override, is_deleted)
            VALUES ({Guid.NewGuid()}, {budgetId}, {categoryId}, 1, 'Expense', '[]', '[]', 100, 'ZAR', false, false)", ct);

        return userId;
    }

    // The new budget has a budgets table of its own, in the budget schema, so only the shipped budget's schema and the
    // tables only it had are checked
    private static async Task ItShouldHaveNoShippedBudgetTablesOrSchema(MenloDbContext ctx, CancellationToken ct)
    {
        long tables = await ctx.Database
            .SqlQuery<long>($@"SELECT count(*) AS ""Value"" FROM information_schema.tables
                WHERE table_schema = 'budget_schema'
                   OR table_name IN ('budget_categories', 'canonical_categories', 'budget_items')")
            .SingleAsync(ct);
        long schemas = await ctx.Database
            .SqlQuery<long>($@"SELECT count(*) AS ""Value"" FROM information_schema.schemata WHERE schema_name = 'budget_schema'")
            .SingleAsync(ct);

        tables.ShouldBe(0);
        schemas.ShouldBe(0);
    }

    private static async Task<Guid> SeedHouseholdScopedDataAsync(MenloDbContext ctx, CancellationToken ct)
    {
        Guid householdId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        Guid budgetId = Guid.NewGuid();
        Guid canonicalId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();

        await ctx.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO shared.households (id, name, is_deleted) VALUES ({householdId}, 'Test', false)", ct);
        await ctx.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO shared.users (id, external_id, email, display_name, household_id, is_deleted) VALUES ({userId}, 'migration-user', 'migration@menlo.test', 'Migration User', {householdId}, false)", ct);
        await ctx.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO shared.onboarding_states (id, user_id, created_at, updated_at) VALUES ({Guid.NewGuid()}, {userId}, now(), now())", ct);
        await ctx.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO budget_schema.budgets (id, household_id, year, status, is_deleted) VALUES ({budgetId}, {householdId}, 2026, 'Draft', false)", ct);
        await ctx.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO budget_schema.canonical_categories (id, name) VALUES ({canonicalId}, 'Housing')", ct);
        await ctx.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO budget_schema.budget_categories (id, budget_id, canonical_category_id, name, budget_flow, is_deleted) VALUES ({categoryId}, {budgetId}, {canonicalId}, 'Housing', 'Expense', false)", ct);
        await ctx.Database.ExecuteSqlInterpolatedAsync($@"INSERT INTO budget_schema.budget_items (id, budget_id, category_id, month, budget_flow, payer_split, attribution_split, planned_amount, planned_currency, is_manual_override, is_deleted)
            VALUES ({Guid.NewGuid()}, {budgetId}, {categoryId}, 1, 'Expense', '[]', '[]', 100, 'ZAR', false, false)", ct);

        return userId;
    }

    private static async Task ItShouldHaveKeptTheUser(MenloDbContext ctx, Guid userId, CancellationToken ct)
    {
        long users = await ctx.Database
            .SqlQuery<long>($"SELECT count(*) AS \"Value\" FROM shared.users WHERE id = {userId} AND external_id = 'migration-user' AND email = 'migration@menlo.test'")
            .SingleAsync(ct);
        users.ShouldBe(1);
    }

    private static async Task ItShouldHaveDroppedEveryHouseholdScopedRow(MenloDbContext ctx, CancellationToken ct)
    {
        foreach (string table in new[] { "budget_items", "budget_categories", "canonical_categories", "budgets" })
        {
            long rows = await ctx.Database
                .SqlQueryRaw<long>("SELECT count(*) AS \"Value\" FROM budget_schema.\"" + table + "\"")
                .SingleAsync(ct);
            rows.ShouldBe(0, $"budget_schema.{table} should be empty");
        }
    }

    private static async Task ItShouldHaveDroppedTheHouseholdSchema(MenloDbContext ctx, CancellationToken ct)
    {
        long leftovers = await ctx.Database
            .SqlQuery<long>($@"SELECT count(*) AS ""Value"" FROM information_schema.columns
                WHERE column_name = 'household_id' AND table_schema IN ('shared', 'budget_schema')")
            .SingleAsync(ct);
        long tables = await ctx.Database
            .SqlQuery<long>($@"SELECT count(*) AS ""Value"" FROM information_schema.tables
                WHERE table_schema = 'shared' AND table_name IN ('households', 'onboarding_states')")
            .SingleAsync(ct);

        leftovers.ShouldBe(0);
        tables.ShouldBe(0);
    }

    private static void ItShouldHaveNoAppliedMigrations(IEnumerable<string> applied)
    {
        applied.ShouldBeEmpty("All migrations should have been rolled back");
    }
}
