using Menlo.Application.Common;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using TUnit.Core.Interfaces;

namespace Menlo.Api.Tests.Integration.Onboarding;

public sealed class OnboardingApiFixture : IAsyncInitializer, IAsyncDisposable
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17")
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public IServiceProvider Services { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = $"{_container.GetConnectionString()};SSL Mode=Disable";

        HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Production
        });

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:menlo"] = ConnectionString
        });

        builder.AddMenloApplication();
        builder.Services.AddScoped<IAuditStampFactory, TestOnboardingApiAuditStampFactory>();
        builder.Services.AddScoped<ISoftDeleteStampFactory, TestOnboardingApiSoftDeleteStampFactory>();

        Services = builder.Services.BuildServiceProvider();

        using IServiceScope scope = Services.CreateScope();
        MenloDbContext db = scope.ServiceProvider.GetRequiredService<MenloDbContext>();
        await db.Database.MigrateAsync();

        await using TestWebApplicationFactory factory = CreateFactory();
        using HttpClient client = factory.CreateClient();
        _ = await client.GetAsync("/health");
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

    public TestWebApplicationFactory CreateFactory(
        string? userId = null,
        string? email = null,
        string? displayName = null,
        bool simulateUnauthenticated = false) =>
        new()
        {
            MenloConnectionString = ConnectionString,
            SkipMigration = false,
            UserId = userId ?? Guid.NewGuid().ToString(),
            UserEmail = email ?? $"user-{Guid.NewGuid():N}@menlo.test",
            UserDisplayName = displayName ?? $"User {Guid.NewGuid():N}",
            SimulateUnauthenticated = simulateUnauthenticated
        };
}

internal sealed class TestOnboardingApiAuditStampFactory : IAuditStampFactory
{
    public AuditStamp CreateStamp() => new(UserId.NewId(), DateTimeOffset.UtcNow);
}

internal sealed class TestOnboardingApiSoftDeleteStampFactory : ISoftDeleteStampFactory
{
    public SoftDeleteStamp CreateStamp() => new(UserId.NewId(), DateTimeOffset.UtcNow);
}
