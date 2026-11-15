using Menlo.Application.Common;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using TUnit.Core.Interfaces;

namespace Menlo.Api.Tests.Integration.Users;

public sealed class UserPersistenceFixture : IAsyncInitializer, IAsyncDisposable
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17")
        .Build();

    public IServiceProvider Services { get; private set; } = null!;

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
        builder.Services.AddScoped<IAuditStampFactory, TestUserAuditStampFactory>();
        builder.Services.AddScoped<ISoftDeleteStampFactory, TestUserSoftDeleteStampFactory>();

        Services = builder.Services.BuildServiceProvider();

        using IServiceScope scope = Services.CreateScope();
        MenloDbContext db = scope.ServiceProvider.GetRequiredService<MenloDbContext>();
        await db.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        switch (Services)
        {
            case IAsyncDisposable asyncDisposable:
                await asyncDisposable.DisposeAsync();
                break;
            case IDisposable disposable:
                disposable.Dispose();
                break;
        }

        await _container.DisposeAsync();
    }
}

internal sealed class TestUserAuditStampFactory : IAuditStampFactory
{
    public AuditStamp CreateStamp() => new(UserId.NewId(), DateTimeOffset.UtcNow);
}

internal sealed class TestUserSoftDeleteStampFactory : ISoftDeleteStampFactory
{
    public SoftDeleteStamp CreateStamp() => new(UserId.NewId(), DateTimeOffset.UtcNow);
}
