using CSharpFunctionalExtensions;
using Menlo.Lib.Auth.Abstractions;
using Menlo.Lib.Auth.Errors;
using Menlo.Lib.Auth.Models;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Testcontainers.PostgreSql;
using TUnit.Core.Interfaces;

namespace Menlo.Api.Tests.Budget;

public sealed class BudgetApiFixture : IAsyncInitializer, IAsyncDisposable
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17")
        .Build();

    private BudgetTestWebApplicationFactory _factory = null!;

    /// <summary>
    /// The household behind <see cref="CreateClient"/> and <see cref="CreateAntiforgeryClientAsync"/>. Every test
    /// that uses those clients shares it, so only use them for requests that don't write household data.
    /// </summary>
    public HouseholdId SharedHouseholdId { get; } = HouseholdId.NewId();

    public string ConnectionString { get; private set; } = string.Empty;

    public HttpClient CreateClient() => _factory.CreateClient();

    /// <summary>
    /// Creates an API host on the shared database for one test, signed in to a household of its own
    /// (<see cref="BudgetTestWebApplicationFactory.HouseholdId"/>), so no test sees another's data, whatever order they
    /// run in. Pass <paramref name="householdId"/> only when two hosts in one test must share a household.
    /// </summary>
    public BudgetTestWebApplicationFactory CreateIsolatedFactory(
        HouseholdId? householdId = null,
        bool budgetItemsEnabled = false,
        bool simulateUnauthenticated = false) =>
        new(householdId ?? HouseholdId.NewId())
        {
            MenloConnectionString = ConnectionString,
            SkipMigration = true,
            SimulateUnauthenticated = simulateUnauthenticated,
            ConfigurationOverrides = new Dictionary<string, string?>
            {
                ["Features:Budget"] = "true",
                ["Features:BudgetItems"] = budgetItemsEnabled ? "true" : "false"
            }
        };

    public Task<HttpClient> CreateAntiforgeryClientAsync(CancellationToken cancellationToken = default) =>
        _factory.CreateAntiforgeryClientAsync(cancellationToken: cancellationToken);

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        ConnectionString = $"{_container.GetConnectionString()};SSL Mode=Disable";

        _factory = new BudgetTestWebApplicationFactory(SharedHouseholdId)
        {
            MenloConnectionString = ConnectionString,
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
    /// <summary>
    /// Gets the household the signed-in test user belongs to.
    /// </summary>
    public HouseholdId HouseholdId { get; } = householdId;

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
                    new UserContext(Menlo.Lib.Common.ValueObjects.UserId.NewId(), HouseholdId)));

            services.AddScoped(_ => mock);
        });
    }
}
