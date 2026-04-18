using CSharpFunctionalExtensions;
using Menlo.Lib.Auth.Abstractions;
using Menlo.Lib.Auth.Errors;
using Menlo.Lib.Auth.Models;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Testcontainers.PostgreSql;

namespace Menlo.Api.Tests.Budget;

[CollectionDefinition("Budget")]
public sealed class BudgetCollection : ICollectionFixture<BudgetApiFixture>;

public sealed class BudgetApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17")
        .Build();

    private BudgetTestWebApplicationFactory _factory = null!;

    public static readonly HouseholdId TestHouseholdId =
        new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

    public HttpClient CreateClient() => _factory.CreateClient();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        _factory = new BudgetTestWebApplicationFactory(TestHouseholdId)
        {
            MenloConnectionString = $"{_container.GetConnectionString()};SSL Mode=Disable",
            SkipMigration = false,
            ConfigurationOverrides = new Dictionary<string, string?>
            {
                ["Features:Budget"] = "true"
            }
        };

        // Warm up — this triggers EF Core migrations
        _ = _factory.CreateClient();
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _container.DisposeAsync();
    }
}

/// <summary>
/// Extends <see cref="TestWebApplicationFactory"/> to override
/// <see cref="IUserContextProvider"/> with an NSubstitute mock that always
/// returns a successful <see cref="UserContext"/> backed by the given
/// <paramref name="householdId"/>.
/// </summary>
public sealed class BudgetTestWebApplicationFactory(HouseholdId householdId)
    : TestWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            // Remove the real IUserContextProvider registered by the API
            ServiceDescriptor? descriptor = services
                .FirstOrDefault(d => d.ServiceType == typeof(IUserContextProvider));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            IUserContextProvider mock = Substitute.For<IUserContextProvider>();
            mock.GetUserContextAsync(Arg.Any<CancellationToken>())
                .Returns(Result.Success<UserContext, AuthError>(
                    new UserContext(UserId.NewId(), householdId)));

            services.AddScoped(_ => mock);
        });
    }
}
