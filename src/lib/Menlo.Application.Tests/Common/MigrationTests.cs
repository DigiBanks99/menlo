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
public sealed class MigrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17")
        .Build();

    private IHost _host = null!;

    public async ValueTask InitializeAsync()
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
            .Database.MigrateAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_host is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else
        {
            _host.Dispose();
        }
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task GivenMigratedDatabase_WhenRolledBackToBaseline_ThenMigrationsTableIsEmpty()
    {
        using IServiceScope scope = _host.Services.CreateScope();
        MenloDbContext ctx = scope.ServiceProvider.GetRequiredService<MenloDbContext>();
        IMigrator migrator = ctx.Database.GetService<IMigrator>()!;

        // Act - rollback all migrations (exercises InitialCreate.Down())
        await migrator.MigrateAsync("0", TestContext.Current.CancellationToken);

        // Assert - no applied migrations remain
        IEnumerable<string> applied = await ctx.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);
        ItShouldHaveNoAppliedMigrations(applied);
    }

    private static void ItShouldHaveNoAppliedMigrations(IEnumerable<string> applied)
    {
        applied.ShouldBeEmpty("All migrations should have been rolled back");
    }
}
