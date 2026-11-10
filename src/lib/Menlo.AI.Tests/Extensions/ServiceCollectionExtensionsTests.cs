using Menlo.AI.Configuration;
using Menlo.AI.Extensions;
using Menlo.AI.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.ClientModel.Primitives;
using System.Text.Json.Nodes;

namespace Menlo.AI.Tests.Extensions;

public sealed class ServiceCollectionExtensionsTests
{
    private readonly StubLlamaServer _llama = new();

    [Test]
    public async Task TheApiAsksTheConfiguredModelServer()
    {
        using IHost host = BuildHost(new()
        {
            ["Menlo:AI:Endpoint"] = "http://llama.test:8080/v1",
            ["Menlo:AI:ChatModel"] = "household-model"
        });
        AiSettings settings = host.Services.GetRequiredService<IOptions<AiSettings>>().Value;

        string answer = await AskAsync(host);
        (Uri uri, JsonObject body) = _llama.Requests.Single();

        ItShouldBindTheSettingsFromConfiguration();
        ItShouldCallTheConfiguredEndpoint();
        ItShouldAskForTheConfiguredModel();
        ItShouldReturnTheServersAnswer();

        void ItShouldBindTheSettingsFromConfiguration() => settings.ShouldSatisfyAllConditions(
            () => settings.Endpoint.ShouldBe(new Uri("http://llama.test:8080/v1")),
            () => settings.ChatModel.ShouldBe("household-model"));

        void ItShouldCallTheConfiguredEndpoint() => uri.ShouldBe(new Uri("http://llama.test:8080/v1/chat/completions"));

        void ItShouldAskForTheConfiguredModel() => body["model"]!.GetValue<string>().ShouldBe("household-model");

        void ItShouldReturnTheServersAnswer() => answer.ShouldBe("AI service is working");
    }

    [Test]
    public async Task ThinkingIsLeftAtItsDefault()
    {
        using IHost host = BuildHost(new() { ["Menlo:AI:Endpoint"] = "http://llama.test/v1" });

        await AskAsync(host);
        JsonObject body = _llama.Requests.Single().Body;

        ItShouldAskTheModelNotToThink();
        ItShouldUseTheDefaultModel();

        void ItShouldAskTheModelNotToThink() =>
            body["chat_template_kwargs"]!["enable_thinking"]!.GetValue<bool>().ShouldBeFalse();

        void ItShouldUseTheDefaultModel() => body["model"]!.GetValue<string>().ShouldBe("qwen3.5-4b");
    }

    [Test]
    public async Task ThinkingIsEnabled()
    {
        using IHost host = BuildHost(new()
        {
            ["Menlo:AI:Endpoint"] = "http://llama.test/v1",
            ["Menlo:AI:EnableThinking"] = "true"
        });

        await AskAsync(host);
        JsonObject body = _llama.Requests.Single().Body;

        ItShouldLeaveTheChatTemplateAlone();

        void ItShouldLeaveTheChatTemplateAlone() => body.ContainsKey("chat_template_kwargs").ShouldBeFalse();
    }

    [Test]
    public async Task NoEndpointIsConfigured()
    {
        using IHost host = BuildHost([]);

        Exception? exception = await Catch.ExceptionAsync(() => host.StartAsync());

        ItShouldRefuseToStart();

        void ItShouldRefuseToStart() => exception
            .ShouldBeOfType<OptionsValidationException>()
            .Message.ShouldContain("Menlo:AI:Endpoint is required.");
    }

    [Test]
    [Arguments("llama:8080/v1")]
    [Arguments("ftp://llama/v1")]
    public async Task TheEndpointIsNotAWebAddress(string endpoint)
    {
        using IHost host = BuildHost(new() { ["Menlo:AI:Endpoint"] = endpoint });

        Exception? exception = await Catch.ExceptionAsync(() => host.StartAsync());

        ItShouldRefuseToStart();

        void ItShouldRefuseToStart() => exception
            .ShouldBeOfType<OptionsValidationException>()
            .Message.ShouldContain("Menlo:AI:Endpoint must be an absolute http or https URL.");
    }

    [Test]
    public async Task TheApiKeyIsBlank()
    {
        using IHost host = BuildHost(new()
        {
            ["Menlo:AI:Endpoint"] = "http://llama.test/v1",
            ["Menlo:AI:ApiKey"] = ""
        });

        string answer = await AskAsync(host);

        ItShouldStillAskTheModel();

        void ItShouldStillAskTheModel() => answer.ShouldBe("AI service is working");
    }

    [Test]
    public async Task TheApiKeyIsConfigured()
    {
        using IHost host = BuildHost(new()
        {
            ["Menlo:AI:Endpoint"] = "http://llama.test/v1",
            ["Menlo:AI:ApiKey"] = "llama-secret"
        });

        await AskAsync(host);
        string? authorization = _llama.Authorizations.Single();

        ItShouldSendTheKeyAsABearerToken();

        void ItShouldSendTheKeyAsABearerToken() => authorization.ShouldBe("Bearer llama-secret");
    }

    private IHost BuildHost(Dictionary<string, string?> configuration)
    {
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Configuration.AddInMemoryCollection(configuration);
        builder.AddMenloAi();
        builder.Services.AddSingleton<PipelineTransport>(new HttpClientPipelineTransport(new HttpClient(_llama)));
        return builder.Build();
    }

    private static async Task<string> AskAsync(IHost host)
    {
        using IServiceScope scope = host.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IChatService>().GetResponseAsync("Hello");
    }
}
