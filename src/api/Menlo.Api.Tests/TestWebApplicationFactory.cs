using Menlo.AI.Interfaces;
using Menlo.Api.Tests.Fixtures;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            config.AddInMemoryCollection();
        });

        builder.ConfigureServices(services =>
        {
            // Remove the AI service registrations that depend on Aspire
            RemoveAiServices(services);

            // Add mock AI services
            AddMockAiServices(services);

            // Override authentication with test handler (authenticated by default)
            services
                .AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            // Configure the test handler with default authenticated user
            services.Configure<TestAuthHandlerOptions>(options =>
            {
                options.Roles = UserRoles;
                options.SimulateUnauthenticated = SimulateUnauthenticated;
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
