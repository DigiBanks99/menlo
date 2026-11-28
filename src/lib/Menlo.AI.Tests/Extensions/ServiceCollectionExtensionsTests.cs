using Menlo.AI.Configuration;
using Menlo.AI.Extensions;
using Menlo.AI.Interfaces;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
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
    [Arguments("VisionModel", " ", "Menlo:AI:VisionModel is required.")]
    [Arguments("VisionTimeout", "00:00:00", "Menlo:AI:VisionTimeout must be positive and at most 1.00:00:00.")]
    [Arguments("VisionTimeout", "-00:00:01", "Menlo:AI:VisionTimeout must be positive and at most 1.00:00:00.")]
    [Arguments("VisionTimeout", "30.00:00:00", "Menlo:AI:VisionTimeout must be positive and at most 1.00:00:00.")]
    public async Task AVisionSettingIsInvalid(string key, string value, string message)
    {
        using IHost host = BuildHost(new()
        {
            ["Menlo:AI:Endpoint"] = "http://llama.test/v1",
            [$"Menlo:AI:{key}"] = value
        });

        Exception? exception = await Catch.ExceptionAsync(() => host.StartAsync());

        ItShouldRefuseToStart();

        void ItShouldRefuseToStart() => exception
            .ShouldBeOfType<OptionsValidationException>()
            .Message.ShouldContain(message);
    }

    [Test]
    public void TheVisionSettingsAreConfigured()
    {
        using IHost host = BuildHost(new()
        {
            ["Menlo:AI:Endpoint"] = "http://llama.test/v1",
            ["Menlo:AI:VisionModel"] = "household-vision",
            ["Menlo:AI:VisionTimeout"] = "00:05:00"
        });

        AiSettings settings = host.Services.GetRequiredService<IOptions<AiSettings>>().Value;

        ItShouldBindThem();

        void ItShouldBindThem() => settings.ShouldSatisfyAllConditions(
            () => settings.VisionModel.ShouldBe("household-vision"),
            () => settings.VisionTimeout.ShouldBe(TimeSpan.FromMinutes(5)));
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

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments(" ")]
    public async Task NoDecisionModelIsConfigured(string? decisionModel)
    {
        using IHost host = BuildHost(new()
        {
            ["Menlo:AI:Endpoint"] = "http://llama.test/v1",
            ["Menlo:AI:ChatModel"] = "household-model",
            ["Menlo:AI:DecisionModel"] = decisionModel
        });
        AiSettings settings = host.Services.GetRequiredService<IOptions<AiSettings>>().Value;

        Exception? startup = await Catch.ExceptionAsync(() => host.StartAsync());
        string answer = await DecideAsync(host);
        (Uri uri, JsonObject body) = _llama.Requests.Single();

        ItShouldStart();
        ItShouldFallBackToTheChatModel();
        ItShouldAskTheChatModel();
        ItShouldCallTheConfiguredEndpoint();
        ItShouldReturnTheServersAnswer();

        void ItShouldStart() => startup.ShouldBeNull();

        void ItShouldFallBackToTheChatModel() => settings.ResolvedDecisionModel.ShouldBe("household-model");

        void ItShouldAskTheChatModel() => body["model"]!.GetValue<string>().ShouldBe("household-model");

        void ItShouldCallTheConfiguredEndpoint() => uri.ShouldBe(new Uri("http://llama.test/v1/chat/completions"));

        void ItShouldReturnTheServersAnswer() => answer.ShouldBe("AI service is working");
    }

    [Test]
    public async Task ADecisionModelIsConfigured()
    {
        using IHost host = BuildHost(new()
        {
            ["Menlo:AI:Endpoint"] = "http://llama.test/v1",
            ["Menlo:AI:ChatModel"] = "household-model",
            ["Menlo:AI:DecisionModel"] = "household-decision"
        });
        AiSettings settings = host.Services.GetRequiredService<IOptions<AiSettings>>().Value;

        await DecideAsync(host);
        await AskAsync(host);
        string[] models = [.. _llama.Requests.Select(request => request.Body["model"]!.GetValue<string>())];

        ItShouldBindIt();
        ItShouldAskTheDecisionModelForDecisions();
        ItShouldLeaveChatOnTheChatModel();

        void ItShouldBindIt() => settings.ShouldSatisfyAllConditions(
            () => settings.DecisionModel.ShouldBe("household-decision"),
            () => settings.ResolvedDecisionModel.ShouldBe("household-decision"));

        void ItShouldAskTheDecisionModelForDecisions() => models[0].ShouldBe("household-decision");

        void ItShouldLeaveChatOnTheChatModel() => models[1].ShouldBe("household-model");
    }

    [Test]
    public async Task ADecisionIsAskedWithTheCallersOptions()
    {
        using IHost host = BuildHost(new() { ["Menlo:AI:Endpoint"] = "http://llama.test/v1" });
        ChatOptions options = new()
        {
            Temperature = 0.25f,
            RawRepresentationFactory = _ => new ChatCompletionOptions { EndUserId = "budget-agent" }
        };

        await DecideAsync(host, options);
        JsonObject body = _llama.Requests.Single().Body;

        ItShouldAskTheModelNotToThink();
        ItShouldKeepTheCallersOptions();
        ItShouldKeepTheCallersRequestOptions();

        void ItShouldAskTheModelNotToThink() =>
            body["chat_template_kwargs"]!["enable_thinking"]!.GetValue<bool>().ShouldBeFalse();

        void ItShouldKeepTheCallersOptions() => body["temperature"]!.GetValue<float>().ShouldBe(0.25f);

        void ItShouldKeepTheCallersRequestOptions() => body["user"]!.GetValue<string>().ShouldBe("budget-agent");
    }

    [Test]
    public async Task ADecisionIsAskedWithRequestOptionsOfAnotherType()
    {
        using IHost host = BuildHost(new() { ["Menlo:AI:Endpoint"] = "http://llama.test/v1" });
        ChatOptions options = new() { RawRepresentationFactory = _ => new object() };

        Exception? decision = await Catch.ExceptionAsync(() => DecideAsync(host, options));

        ItShouldRefuseThem();
        ItShouldSayWhatItNeeds();
        ItShouldNotCallTheModel();

        void ItShouldRefuseThem() => decision.ShouldBeOfType<InvalidOperationException>();

        void ItShouldSayWhatItNeeds() => decision!.Message.ShouldContain(typeof(ChatCompletionOptions).FullName!);

        void ItShouldNotCallTheModel() => _llama.Requests.ShouldBeEmpty();
    }

    [Test]
    public async Task ADecisionIsAskedWithoutOptions()
    {
        using IHost host = BuildHost(new() { ["Menlo:AI:Endpoint"] = "http://llama.test/v1" });

        await DecideAsync(host);
        JsonObject body = _llama.Requests.Single().Body;

        ItShouldAskTheModelNotToThink();

        void ItShouldAskTheModelNotToThink() =>
            body["chat_template_kwargs"]!["enable_thinking"]!.GetValue<bool>().ShouldBeFalse();
    }

    [Test]
    public async Task ADecisionIsAskedWithThinkingEnabled()
    {
        using IHost host = BuildHost(new()
        {
            ["Menlo:AI:Endpoint"] = "http://llama.test/v1",
            ["Menlo:AI:EnableThinking"] = "true"
        });

        await DecideAsync(host);
        JsonObject body = _llama.Requests.Single().Body;

        ItShouldLeaveTheChatTemplateAlone();

        void ItShouldLeaveTheChatTemplateAlone() => body.ContainsKey("chat_template_kwargs").ShouldBeFalse();
    }

    private static async Task<string> DecideAsync(IHost host, ChatOptions? options = null)
    {
        IChatClient client = host.Services.GetRequiredKeyedService<IChatClient>(AiClientKeys.Decision);
        ChatResponse response = await client.GetResponseAsync("Rank these", options);
        return response.Text;
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
