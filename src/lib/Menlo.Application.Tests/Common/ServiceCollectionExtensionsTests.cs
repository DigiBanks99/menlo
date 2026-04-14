using Menlo.Application.Common;
using Menlo.Application.Tests.Fixtures;
using Menlo.Lib.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Menlo.Application.Tests.Common;

[Collection("Persistence")]
public sealed class ServiceCollectionExtensionsTests(PersistenceFixture fixture)
{
    [Fact]
    public async Task GivenSkipMigrationTrue_WhenMigrateDatabaseAsync_ThenReturnsWithoutMigrating()
    {
        string connectionString = fixture.Services
            .GetRequiredService<IConfiguration>().GetConnectionString("menlo")!;

        HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Production
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:menlo"] = connectionString,
            ["Menlo:SkipMigration"] = "true"
        });
        builder.AddMenloApplication();
        builder.Services.AddScoped<IAuditStampFactory, TestAuditStampFactory>();
        builder.Services.AddScoped<ISoftDeleteStampFactory, TestSoftDeleteStampFactory>();
        using IHost host = builder.Build();

        // Act - should return early without migrating (no throw = pass)
        await host.MigrateDatabaseAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GivenValidConnection_WhenMigrateDatabaseAsync_ThenMigratesSuccessfully()
    {
        string connectionString = fixture.Services
            .GetRequiredService<IConfiguration>().GetConnectionString("menlo")!;

        HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Production
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:menlo"] = connectionString
        });
        builder.AddMenloApplication();
        builder.Services.AddScoped<IAuditStampFactory, TestAuditStampFactory>();
        builder.Services.AddScoped<ISoftDeleteStampFactory, TestSoftDeleteStampFactory>();
        using IHost host = builder.Build();

        // Act - idempotent migration (db already migrated by fixture, so this just verifies the path)
        await host.MigrateDatabaseAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public void GivenDevelopmentEnvironment_WhenResolvingDbContext_ThenConnectionStringHasSslMode()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Development
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:menlo"] = "Host=localhost;Database=test;Username=test;Password=test"
        });
        builder.AddMenloApplication();
        builder.Services.AddScoped<IAuditStampFactory, TestAuditStampFactory>();
        builder.Services.AddScoped<ISoftDeleteStampFactory, TestSoftDeleteStampFactory>();
        using IHost host = builder.Build();
        using IServiceScope scope = host.Services.CreateScope();

        // Act - resolving MenloDbContext triggers the AddDbContext lambda
        MenloDbContext dbContext = scope.ServiceProvider.GetRequiredService<MenloDbContext>();

        // Assert - connection string should have SSL mode required
        string? connectionString = dbContext.Database.GetConnectionString();
        ItShouldHaveSslModeRequired(connectionString);
    }

    private static void ItShouldHaveSslModeRequired(string? connectionString)
    {
        connectionString.ShouldNotBeNull();
        connectionString.ShouldContain("SSL Mode=Require", Case.Insensitive);
    }
}
