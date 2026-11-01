using Menlo.Application.Common;
using Menlo.Lib.Common.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using TUnit.Core.Interfaces;

namespace Menlo.Application.Tests.Fixtures;

public sealed class InterceptorPersistenceFixture : IAsyncInitializer, IAsyncDisposable
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17")
        .Build();

    private IHost _host = null!;

    public IServiceProvider Services => _host.Services;

    public string ConnectionString => $"{_container.GetConnectionString()};SSL Mode=Disable";

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Production
        });

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:menlo"] = ConnectionString,
            ["Menlo:SkipMigration"] = "true"
        });

        builder.AddMenloApplication();
        builder.Services.AddScoped<IAuditStampFactory, TestAuditStampFactory>();
        builder.Services.AddScoped<ISoftDeleteStampFactory, TestSoftDeleteStampFactory>();

        _host = builder.Build();
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
}
