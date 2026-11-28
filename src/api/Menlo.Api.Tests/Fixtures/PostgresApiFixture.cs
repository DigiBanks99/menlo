using CSharpFunctionalExtensions;
using Menlo.Lib.Auth.Abstractions;
using Menlo.Lib.Auth.Errors;
using Menlo.Lib.Auth.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Testcontainers.PostgreSql;
using TUnit.Core.Interfaces;

namespace Menlo.Api.Tests.Fixtures;

/// <summary>
/// One migrated PostgreSQL database for the test session, for tests that run the whole app against a real database.
/// </summary>
public sealed class PostgresApiFixture : IAsyncInitializer, IAsyncDisposable
{
    /// <summary>
    /// Set this environment variable to a PostgreSQL connection string to run against that server and not start a
    /// container, for machines where Testcontainers can not reach a container engine. Point it at a database
    /// that holds nothing else.
    /// </summary>
    public const string ExternalDatabaseVariable = "MENLO_TEST_POSTGRES";

    private static readonly string? ExternalConnectionString =
        Environment.GetEnvironmentVariable(ExternalDatabaseVariable) is { Length: > 0 } value ? value : null;

    private readonly PostgreSqlContainer? _container = ExternalConnectionString is null
        ? new PostgreSqlBuilder("postgres:17").Build()
        : null;

    private PostgresTestWebApplicationFactory _factory = null!;

    public string ConnectionString { get; private set; } = string.Empty;

    /// <summary>
    /// Creates an API host on the shared, already migrated database for one test, signed in as the test user.
    /// </summary>
    public PostgresTestWebApplicationFactory CreateFactory(IReadOnlyDictionary<string, string?>? configuration = null) =>
        new()
        {
            MenloConnectionString = ConnectionString,
            SkipMigration = true,
            UseRealUserProvisioning = true,
            UserRoles = ["Menlo.User"],
            ConfigurationOverrides = configuration ?? new Dictionary<string, string?>()
        };

    public async Task InitializeAsync()
    {
        if (_container is null)
        {
            ConnectionString = ExternalConnectionString!;
        }
        else
        {
            await _container.StartAsync();
            ConnectionString = $"{_container.GetConnectionString()};SSL Mode=Disable";
        }

        _factory = new PostgresTestWebApplicationFactory
        {
            MenloConnectionString = ConnectionString,
            SkipMigration = false,
            UseRealUserProvisioning = true
        };

        // Warm up: this runs the EF Core migrations
        _ = _factory.CreateClient();
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}

/// <summary>
/// Extends <see cref="TestWebApplicationFactory"/> to override
/// <see cref="IUserContextProvider"/> with an NSubstitute mock that always
/// returns a successful <see cref="UserContext"/>.
/// </summary>
public sealed class PostgresTestWebApplicationFactory
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
