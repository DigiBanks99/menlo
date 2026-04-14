using Menlo.Application.Common.Interceptors;
using Menlo.Application.Tests.TestHelpers;
using Menlo.Lib.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace Menlo.Application.Tests.Fixtures;

public sealed class InterceptorFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .Build();

    private IHost _host = null!;

    public IServiceProvider Services => _host.Services;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Production
        });

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:menlo"] = _container.GetConnectionString()
        });

        builder.Services.AddScoped<IAuditStampFactory, TestAuditStampFactory>();
        builder.Services.AddScoped<ISoftDeleteStampFactory, TestSoftDeleteStampFactory>();
        builder.Services.AddScoped<AuditingInterceptor>();
        builder.Services.AddScoped<SoftDeleteInterceptor>();
        builder.Services.AddDbContext<TestMenloDbContext>((sp, options) =>
        {
            string cs = sp.GetRequiredService<IConfiguration>().GetConnectionString("menlo")!;
            options.UseNpgsql(cs)
                   .UseSnakeCaseNamingConvention()
                   .AddInterceptors(
                       sp.GetRequiredService<AuditingInterceptor>(),
                       sp.GetRequiredService<SoftDeleteInterceptor>());
        });

        _host = builder.Build();

        using IServiceScope scope = _host.Services.CreateScope();
        TestMenloDbContext ctx = scope.ServiceProvider.GetRequiredService<TestMenloDbContext>();
        await ctx.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        _host.Dispose();
        await _container.DisposeAsync();
    }
}

[CollectionDefinition("Interceptors")]
public sealed class InterceptorCollection : ICollectionFixture<InterceptorFixture>;

