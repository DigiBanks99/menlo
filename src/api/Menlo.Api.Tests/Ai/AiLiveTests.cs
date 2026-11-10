using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Menlo.Api.Tests.Ai;

/// <summary>
/// Calls a real llama-server. Opt-in: set <c>MENLO_AI_LIVE_ENDPOINT</c> to its base URL including <c>/v1</c>, for
/// example the Aspire <c>llama</c> resource's URL. Skipped otherwise, so CI never needs a running model.
/// </summary>
[SkipUnlessLiveModel]
public sealed class AiLiveTests
{
    [Test]
    public async Task TheModelServerIsRunning()
    {
        await using TestWebApplicationFactory factory = new()
        {
            UseMockAiServices = false,
            ConfigurationOverrides = new Dictionary<string, string?>
            {
                ["Menlo:AI:Endpoint"] = SkipUnlessLiveModelAttribute.Endpoint
            }
        };
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/ai/health", TestContext.Current!.Execution.CancellationToken);
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current!.Execution.CancellationToken);

        ItShouldSucceed();
        ItShouldReportHealthy();
        ItShouldIncludeTheModelsAnswer();

        void ItShouldSucceed() => response.StatusCode.ShouldBe(HttpStatusCode.OK);

        void ItShouldReportHealthy() => body.GetProperty("status").GetString().ShouldBe("Healthy");

        void ItShouldIncludeTheModelsAnswer() => body.GetProperty("response").GetString().ShouldNotBeNullOrWhiteSpace();
    }
}

internal sealed class SkipUnlessLiveModelAttribute() : SkipAttribute("Set MENLO_AI_LIVE_ENDPOINT to run against a real llama-server")
{
    public static string? Endpoint => Environment.GetEnvironmentVariable("MENLO_AI_LIVE_ENDPOINT");

    public override Task<bool> ShouldSkip(TestRegisteredContext context) =>
        Task.FromResult(string.IsNullOrWhiteSpace(Endpoint));
}
