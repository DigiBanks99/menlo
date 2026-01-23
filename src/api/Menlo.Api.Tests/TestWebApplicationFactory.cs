using System.Data.Common;
using Menlo.AI.Interfaces;
using Menlo.Api.Persistence.Data;
using Menlo.Api.Tests.Fixtures;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using NSubstitute;

namespace Menlo.Api.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// Gets the roles to assign to the test user.
    /// </summary>
    public string[] UserRoles { get; init; } = ["Menlo.User"];

    /// <summary>
    /// Gets whether to simulate an unauthenticated user.
    /// </summary>
    public bool SimulateUnauthenticated { get; init; }

    /// <summary>
    /// Configures the web host for testing.
    /// This includes setting up test authentication and mock AI services.
    /// </summary>
    /// <param name="builder">The web host builder.</param>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Dictionary<string, string?> hostConfig = new()
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Production"
        };

        builder.UseConfiguration(new ConfigurationBuilder()
            .AddInMemoryCollection(hostConfig)
            .Build());

        // Add test configuration values for auth
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
                ["AzureAd:TenantId"] = "00000000-0000-0000-0000-000000000000",
                ["AzureAd:ClientId"] = "00000000-0000-0000-0000-000000000001",
                ["AzureAd:ClientSecret"] = "test-client-secret",
                ["AzureAd:CookieDomain"] = "localhost"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the AI service registrations that depend on Aspire
            RemoveAiServices(services);

            // Add mock AI services
            AddMockAiServices(services);

            // Remove the real database and all EF Core services
            List<ServiceDescriptor> efDescriptors = services
                .Where(d => d.ServiceType.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") == true
                    || d.ServiceType.Namespace?.StartsWith("Npgsql") == true
                    || d.ServiceType == typeof(MenloDbContext)
                    || d.ServiceType == typeof(DbContextOptions<MenloDbContext>)
                    || d.ServiceType == typeof(DbConnection))
                .ToList();

            foreach (ServiceDescriptor descriptor in efDescriptors)
            {
                services.Remove(descriptor);
            }

            // Create a shared SQLite in-memory connection that stays open for the test
            SqliteConnection connection = new("DataSource=:memory:");
            connection.Open();
            services.AddSingleton<DbConnection>(connection);

            // Add SQLite database for tests (supports complex types unlike InMemory provider)
            services.AddDbContext<MenloDbContext>((sp, options) =>
            {
                DbConnection conn = sp.GetRequiredService<DbConnection>();
                options.UseSqlite(conn);
            });

            // Ensure the database schema is created
            using ServiceProvider sp = services.BuildServiceProvider();
            using IServiceScope scope = sp.CreateScope();
            MenloDbContext db = scope.ServiceProvider.GetRequiredService<MenloDbContext>();
            db.Database.EnsureCreated();

            // Add the test authentication scheme
            services
                .AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            // Override the default authentication scheme to use our test handler
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                options.DefaultScheme = TestAuthHandler.SchemeName;
            });

            // Configure the test handler with default authenticated user
            services.Configure<TestAuthHandlerOptions>(options =>
            {
                options.Roles = UserRoles;
                options.SimulateUnauthenticated = SimulateUnauthenticated;
            });

            // Configure OpenIdConnect options for tests
            // Use Configure to override values and provide mock OIDC configuration to avoid metadata fetching
            services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.RequireHttpsMetadata = false;
                options.Authority = "https://login.microsoftonline.com/00000000-0000-0000-0000-000000000000/v2.0";
                options.ClientId = "00000000-0000-0000-0000-000000000001";
                options.ClientSecret = "test-client-secret";

                // Provide mock OIDC configuration to avoid fetching metadata from Authority
                options.Configuration = new OpenIdConnectConfiguration
                {
                    AuthorizationEndpoint = "https://login.microsoftonline.com/00000000-0000-0000-0000-000000000000/oauth2/v2.0/authorize",
                    TokenEndpoint = "https://login.microsoftonline.com/00000000-0000-0000-0000-000000000000/oauth2/v2.0/token",
                    EndSessionEndpoint = "https://login.microsoftonline.com/00000000-0000-0000-0000-000000000000/oauth2/v2.0/logout",
                    Issuer = "https://login.microsoftonline.com/00000000-0000-0000-0000-000000000000/v2.0"
                };
            });
        });
    }

    private static void RemoveAiServices(IServiceCollection services)
    {
        List<ServiceDescriptor> descriptors =
         [
            .. services
            .Where(d => d.ServiceType == typeof(IChatService)
                || d.ServiceType == typeof(IEmbeddingService)
                || d.ServiceType == typeof(IVisionService))
        ];

        foreach (ServiceDescriptor descriptor in descriptors)
        {
            services.Remove(descriptor);
        }
    }

    private static void AddMockAiServices(IServiceCollection services)
    {
        IChatService mockChatService = Substitute.For<IChatService>();
        mockChatService
        .GetResponseAsync(Arg.Any<string>())
            .Returns(Task.FromResult("AI service is working"));

        IEmbeddingService mockEmbeddingService = Substitute.For<IEmbeddingService>();
        IVisionService mockVisionService = Substitute.For<IVisionService>();

        services.AddSingleton(mockChatService);
        services.AddSingleton(mockEmbeddingService);
        services.AddSingleton(mockVisionService);
    }
}
