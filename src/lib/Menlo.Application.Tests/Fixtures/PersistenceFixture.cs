using Menlo.Application.Common;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace Menlo.Application.Tests.Fixtures;

public sealed class PersistenceFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17")
        .Build();

    public IServiceProvider Services { get; private set; } = null!;

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

        Services = builder.Services.BuildServiceProvider();

        using IServiceScope scope = Services.CreateScope();
        await scope.ServiceProvider
            .GetRequiredService<MenloDbContext>()
            .Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}

public sealed class TestAuditStampFactory : IAuditStampFactory
{
    public static readonly UserId TestUserId = UserId.NewId();

    public AuditStamp CreateStamp() => new(TestUserId, DateTimeOffset.UtcNow);
}

public sealed class TestSoftDeleteStampFactory : ISoftDeleteStampFactory
{
    public SoftDeleteStamp CreateStamp() => new(TestAuditStampFactory.TestUserId, DateTimeOffset.UtcNow);
}

[CollectionDefinition("Persistence")]
public sealed class PersistenceCollection : ICollectionFixture<PersistenceFixture>;


