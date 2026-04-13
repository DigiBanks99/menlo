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

        var tableExists = await db.Database
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
}
