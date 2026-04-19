using Menlo.Application.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;

namespace Menlo.Api.Tests;

public sealed class ProgramStartupTests : TestFixture
{
    [Fact]
    public async Task CreateClient_WithStartupMigrationEnabled()
    {
        await using PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17")
            .Build();
        await container.StartAsync(TestContext.Current.CancellationToken);

        using TestWebApplicationFactory factory = new()
        {
            MenloConnectionString = $"{container.GetConnectionString()};SSL Mode=Disable",
            SkipMigration = false
        };
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/ai/health", TestContext.Current.CancellationToken);
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        MenloDbContext db = scope.ServiceProvider.GetRequiredService<MenloDbContext>();
        IEnumerable<string> appliedMigrations = await db.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);

        ItShouldHaveSucceeded(response);
        ItShouldContainInitialCreateMigration(appliedMigrations);
    }

    [Fact]
    public void CreateClient_WithMissingConnectionString()
    {
        using TestWebApplicationFactory factory = new()
        {
            MenloConnectionString = null,
            SkipMigration = false
        };

        Action act = () =>
        {
            using HttpClient client = factory.CreateClient();
            _ = client.GetAsync("/api/ai/health", TestContext.Current.CancellationToken).GetAwaiter().GetResult();
        };

        InvalidOperationException exception = act.ShouldThrow<InvalidOperationException>();

        ItShouldMentionMissingConnectionString(exception);
    }

    [Fact]
    public void CreateClient_WithInvalidAuthenticationConfiguration()
    {
        using TestWebApplicationFactory factory = new()
        {
            ConfigurationOverrides = new Dictionary<string, string?>
            {
                ["AzureAd:Instance"] = string.Empty,
                ["AzureAd:TenantId"] = string.Empty,
                ["AzureAd:ClientId"] = string.Empty,
                ["AzureAd:ClientSecret"] = string.Empty
            }
        };

        Action act = () =>
        {
            using HttpClient client = factory.CreateClient();
            _ = client.GetAsync("/auth/user", TestContext.Current.CancellationToken).GetAwaiter().GetResult();
        };

        OptionsValidationException exception = act.ShouldThrow<OptionsValidationException>();

        ItShouldIncludeAuthenticationValidationFailures(exception);
    }

    private static void ItShouldContainInitialCreateMigration(IEnumerable<string> appliedMigrations)
    {
        appliedMigrations.ShouldContain(migration => migration.Contains("InitialCreate"),
            "Application startup should apply the existing EF Core migrations.");
    }

    private static void ItShouldMentionMissingConnectionString(InvalidOperationException exception)
    {
        exception.Message.ShouldContain("Connection string 'menlo' is not configured.");
    }

    private static void ItShouldIncludeAuthenticationValidationFailures(OptionsValidationException exception)
    {
        exception.Failures.ShouldContain("Instance is required.");
        exception.Failures.ShouldContain("TenantId is required.");
        exception.Failures.ShouldContain("ClientId is required.");
        exception.Failures.ShouldContain("Either ClientSecret or ClientCertificates is required.");
    }
}
