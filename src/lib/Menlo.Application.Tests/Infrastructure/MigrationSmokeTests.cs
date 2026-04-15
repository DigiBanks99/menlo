using Menlo.Application.Common;
using Menlo.Application.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Menlo.Application.Tests.Infrastructure;

[Collection("Persistence")]
public sealed class MigrationSmokeTests(PersistenceFixture fixture)
{
    [Fact]
    public async Task GivenFreshDatabase_WhenMigrationRuns_ThenMigrationHistoryTableExists()
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        MenloDbContext db = scope.ServiceProvider.GetRequiredService<Menlo.Application.Common.MenloDbContext>();

        bool tableExists = await db.Database
            .SqlQueryRaw<int>(
                "SELECT 1 FROM information_schema.tables WHERE table_name = '__EFMigrationsHistory'")
            .AnyAsync(TestContext.Current.CancellationToken);

        tableExists.ShouldBeTrue("__EFMigrationsHistory table should exist after migration runs");
    }

    [Fact]
    public async Task GivenFreshDatabase_WhenMigrationRuns_ThenInitialCreateRecordExists()
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        MenloDbContext db = scope.ServiceProvider.GetRequiredService<Menlo.Application.Common.MenloDbContext>();

        IEnumerable<string> migrations = await db.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);

        migrations.ShouldContain(m => m.Contains("InitialCreate"),
            "InitialCreate migration should be recorded in __EFMigrationsHistory");
    }

    [Fact]
    public async Task GivenFreshDatabase_WhenMigrationRuns_ThenSharedUsersTableExists()
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        MenloDbContext db = scope.ServiceProvider.GetRequiredService<Menlo.Application.Common.MenloDbContext>();

        List<string> columnNames = await db.Database
            .SqlQueryRaw<string>(
                """
                SELECT column_name AS "Value"
                FROM information_schema.columns
                WHERE table_schema = 'shared'
                  AND table_name = 'users'
                ORDER BY ordinal_position
                """)
            .ToListAsync(TestContext.Current.CancellationToken);

        ItShouldContainSharedUsersColumns(columnNames);
    }

    private static void ItShouldContainSharedUsersColumns(IReadOnlyCollection<string> columnNames)
    {
        columnNames.ShouldContain("id");
        columnNames.ShouldContain("external_id");
        columnNames.ShouldContain("email");
        columnNames.ShouldContain("display_name");
        columnNames.ShouldContain("last_login_at");
        columnNames.ShouldContain("created_by");
        columnNames.ShouldContain("created_at");
        columnNames.ShouldContain("modified_by");
        columnNames.ShouldContain("modified_at");
    }
}


