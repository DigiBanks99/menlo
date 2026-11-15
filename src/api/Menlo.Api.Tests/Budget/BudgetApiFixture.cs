using CSharpFunctionalExtensions;
using Menlo.Lib.Auth.Abstractions;
using Menlo.Lib.Auth.Errors;
using Menlo.Lib.Auth.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NSubstitute;
using Testcontainers.PostgreSql;
using TUnit.Core.Interfaces;

namespace Menlo.Api.Tests.Budget;

public sealed class BudgetApiFixture : IAsyncInitializer, IAsyncDisposable
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17")
        .Build();

    private BudgetTestWebApplicationFactory _factory = null!;

    public string ConnectionString { get; private set; } = string.Empty;

    public HttpClient CreateClient() => _factory.CreateClient();

    /// <summary>
    /// Creates an API host on the shared database for one test. Budgets are unique per year across the whole
    /// database, so the budget tables are emptied first (the budget tests run one at a time) and no test sees
    /// another's data, whatever order they run in. Pass <paramref name="keepData"/> when a second host in the same
    /// test must see what the first one wrote, and <paramref name="asDifferentMember"/> to sign in as another member.
    /// </summary>
    public BudgetTestWebApplicationFactory CreateIsolatedFactory(
        bool budgetItemsEnabled = false,
        bool simulateUnauthenticated = false,
        bool keepData = false,
        bool asDifferentMember = false,
        bool useRealStampFactories = false)
    {
        if (!keepData)
        {
            ResetBudgetData();
        }

        return new()
        {
            MenloConnectionString = ConnectionString,
            SkipMigration = true,
            UseRealUserProvisioning = true,
            UseRealStampFactories = useRealStampFactories,
            SimulateUnauthenticated = simulateUnauthenticated,
            UserId = asDifferentMember ? Guid.NewGuid().ToString() : Menlo.Api.Tests.Fixtures.TestAuthHandler.DefaultUserId,
            UserEmail = asDifferentMember ? $"member-{Guid.NewGuid():N}@menlo.test" : Menlo.Api.Tests.Fixtures.TestAuthHandler.DefaultEmail,
            ConfigurationOverrides = new Dictionary<string, string?>
            {
                ["Features:Budget"] = "true",
                ["Features:BudgetItems"] = budgetItemsEnabled ? "true" : "false"
            }
        };
    }

    private void ResetBudgetData()
    {
        using NpgsqlConnection connection = new(ConnectionString);
        connection.Open();
        using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = "TRUNCATE budget_schema.budget_items, budget_schema.budget_categories, "
            + "budget_schema.canonical_categories, budget_schema.budgets CASCADE";
        command.ExecuteNonQuery();
    }

    public Task<HttpClient> CreateAntiforgeryClientAsync(CancellationToken cancellationToken = default) =>
        _factory.CreateAntiforgeryClientAsync(cancellationToken: cancellationToken);

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        ConnectionString = $"{_container.GetConnectionString()};SSL Mode=Disable";

        _factory = new BudgetTestWebApplicationFactory()
        {
            MenloConnectionString = ConnectionString,
            SkipMigration = false,
            UseRealUserProvisioning = true,
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
/// returns a successful <see cref="UserContext"/>.
/// </summary>
public sealed class BudgetTestWebApplicationFactory
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
                    new UserContext(Menlo.Lib.Common.ValueObjects.UserId.NewId())));

            services.AddScoped(_ => mock);
        });
    }
}
