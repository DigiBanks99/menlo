using Menlo.Application.Auth;
using Menlo.Application.Common;
using Menlo.Application.Tests.Fixtures;
using Menlo.Lib.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Menlo.Application.Tests.Common;

[ClassDataSource<PersistenceFixture>(Shared = SharedType.PerTestSession)]
[NotInParallel("Persistence")]
public sealed class ServiceCollectionExtensionsTests(PersistenceFixture fixture)
{
    [Test]
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
        await host.MigrateDatabaseAsync(TestContext.Current!.Execution.CancellationToken);
    }

    [Test]
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
        await host.MigrateDatabaseAsync(TestContext.Current!.Execution.CancellationToken);
    }

    [Test]
    public void GivenDevelopmentEnvironment_WhenResolvingDbContext_ThenConnectionStringDisablesSsl()
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

        // Assert - local development should not require SSL
        string? connectionString = dbContext.Database.GetConnectionString();
        ItShouldHaveSslModeDisabled(connectionString);
    }

    [Test]
    public void GivenProductionEnvironment_WhenResolvingDbContext_ThenConnectionStringRequiresSsl()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Production
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:menlo"] = "Host=menlo-db.example.com;Database=test;Username=test;Password=test"
        });
        builder.AddMenloApplication();
        builder.Services.AddScoped<IAuditStampFactory, TestAuditStampFactory>();
        builder.Services.AddScoped<ISoftDeleteStampFactory, TestSoftDeleteStampFactory>();
        using IHost host = builder.Build();
        using IServiceScope scope = host.Services.CreateScope();

        MenloDbContext dbContext = scope.ServiceProvider.GetRequiredService<MenloDbContext>();

        string? connectionString = dbContext.Database.GetConnectionString();
        ItShouldHaveSslModeRequired(connectionString);
    }

    [Test]
    public void GivenExplicitSslMode_WhenResolvingDbContext_ThenConfiguredSslModeIsPreserved()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Production
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:menlo"] = "Host=localhost;Database=test;Username=test;Password=test;SSL Mode=Disable"
        });
        builder.AddMenloApplication();
        builder.Services.AddScoped<IAuditStampFactory, TestAuditStampFactory>();
        builder.Services.AddScoped<ISoftDeleteStampFactory, TestSoftDeleteStampFactory>();
        using IHost host = builder.Build();
        using IServiceScope scope = host.Services.CreateScope();

        MenloDbContext dbContext = scope.ServiceProvider.GetRequiredService<MenloDbContext>();

        string? connectionString = dbContext.Database.GetConnectionString();
        ItShouldHaveSslModeDisabled(connectionString);
    }

    [Test]
    public void GivenMissingConnectionString_WhenResolvingDbContext_ThenThrowsInvalidOperationException()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Production
        });
        // No connection string configured
        builder.AddMenloApplication();
        builder.Services.AddScoped<IAuditStampFactory, TestAuditStampFactory>();
        builder.Services.AddScoped<ISoftDeleteStampFactory, TestSoftDeleteStampFactory>();
        using IHost host = builder.Build();
        using IServiceScope scope = host.Services.CreateScope();

        Action act = () => scope.ServiceProvider.GetRequiredService<MenloDbContext>();

        act.ShouldThrow<InvalidOperationException>();
    }

    [Test]
    public void GivenValidConfiguration_WhenResolvingIUserContext_ThenReturnsScopedMenloDbContext()
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
        using IServiceScope scope = host.Services.CreateScope();

        IUserContext userContext = scope.ServiceProvider.GetRequiredService<IUserContext>();
        MenloDbContext dbContext = scope.ServiceProvider.GetRequiredService<MenloDbContext>();

        userContext.ShouldBe(dbContext);
    }

    private static void ItShouldHaveSslModeRequired(string? connectionString)
    {
        connectionString.ShouldNotBeNull();
        connectionString.ShouldContain("SSL Mode=Require", Case.Insensitive);
    }

    private static void ItShouldHaveSslModeDisabled(string? connectionString)
    {
        connectionString.ShouldNotBeNull();
        connectionString.ShouldContain("SSL Mode=Disable", Case.Insensitive);
    }
}
