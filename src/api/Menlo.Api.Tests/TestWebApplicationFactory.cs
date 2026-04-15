using Menlo.AI.Interfaces;
using Menlo.Api.Tests.Fixtures;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
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
    /// Gets the connection string to use for the API host. When null, the host starts
    /// without a Menlo database connection string so startup failures can be asserted.
    /// </summary>
    public string? MenloConnectionString { get; init; } = "Host=test;Database=test;Username=test;Password=test";

    /// <summary>
    /// Gets whether startup migrations should be skipped.
    /// </summary>
    public bool SkipMigration { get; init; } = true;

    /// <summary>
    /// Gets configuration overrides to apply after the deterministic test defaults.
    /// </summary>
    public IReadOnlyDictionary<string, string?> ConfigurationOverrides { get; init; } =
        new Dictionary<string, string?>();

    /// <summary>
    /// Configures the web host for testing.
    /// This includes setting up test authentication and mock AI services.
    /// </summary>
    /// <param name="builder">The web host builder.</param>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Dictionary<string, string?> hostConfig = BuildHostConfiguration();

        builder.UseEnvironment("Production");
        builder.UseConfiguration(new ConfigurationBuilder()
            .AddInMemoryCollection(hostConfig)
            .Build());

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection();
        });

        builder.ConfigureServices(services =>
        {
            RemoveAiServices(services);
            AddMockAiServices(services);

            services.AddScoped<IAuditStampFactory, NoOpAuditStampFactory>();
            services.AddScoped<ISoftDeleteStampFactory, NoOpSoftDeleteStampFactory>();

            services.PostConfigureAll<OpenIdConnectOptions>(options =>
            {
                OpenIdConnectConfiguration configuration = new()
                {
                    AuthorizationEndpoint = "https://login.test/authorize",
                    EndSessionEndpoint = "https://login.test/logout",
                    Issuer = "https://login.test/"
                };

                options.RequireHttpsMetadata = false;
                options.Configuration = configuration;
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
            });

            services
                .AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                options.DefaultScheme = TestAuthHandler.SchemeName;
            });

            services.Configure<TestAuthHandlerOptions>(options =>
            {
                options.Roles = UserRoles;
                options.SimulateUnauthenticated = SimulateUnauthenticated;
            });
        });
    }

    private Dictionary<string, string?> BuildHostConfiguration()
    {
        Dictionary<string, string?> hostConfig = new()
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Production",
            ["Menlo:SkipMigration"] = SkipMigration.ToString(),
            ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
            ["AzureAd:TenantId"] = "test-tenant-id",
            ["AzureAd:ClientId"] = "test-client-id",
            ["AzureAd:ClientSecret"] = "test-client-secret",
            ["AzureAd:CookieDomain"] = "localhost"
        };

        if (MenloConnectionString is not null)
        {
            hostConfig["ConnectionStrings:menlo"] = MenloConnectionString;
        }

        foreach ((string key, string? value) in ConfigurationOverrides)
        {
            hostConfig[key] = value;
        }

        return hostConfig;
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

internal sealed class NoOpAuditStampFactory : IAuditStampFactory
{
    public AuditStamp CreateStamp() => new(UserId.NewId(), DateTimeOffset.UtcNow);
}

internal sealed class NoOpSoftDeleteStampFactory : ISoftDeleteStampFactory
{
    public SoftDeleteStamp CreateStamp() => new(UserId.NewId(), DateTimeOffset.UtcNow);
}
