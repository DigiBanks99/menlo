using Menlo.AI.Configuration;
using Menlo.AI.Extensions;
using Menlo.AI.Interfaces;
using Menlo.AI.Services;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;
using System.Text.Json.Nodes;

namespace Menlo.AI.Tests.Services;

public sealed class VisionServiceTests
{
    // Not a real PNG: the stub server never looks inside the image.
    private static readonly byte[] Image = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x01, 0x02];

    private readonly StubLlamaServer _llama = new();

    [Test]
    public async Task SomeoneAsksForTheHandwrittenText()
    {
        using IHost host = BuildHost(new()
        {
            ["Menlo:AI:Endpoint"] = "http://llama.test/v1",
            ["Menlo:AI:ChatModel"] = "chat-model",
            ["Menlo:AI:VisionModel"] = "vision-model"
        });

        string text = await UseVisionAsync(host, vision => vision.ExtractTextAsync(Image, "image/png"));
        JsonObject body = _llama.Requests.Single().Body;
        JsonArray parts = body["messages"]!.AsArray().Single()!["content"]!.AsArray();

        ItShouldAskTheVisionModel();
        ItShouldSendOneUserMessage();
        ItShouldAskForATranscription();
        ItShouldSendTheImageWithItsMediaType();
        ItShouldReturnTheModelsText();

        void ItShouldAskTheVisionModel() => body["model"]!.GetValue<string>().ShouldBe("vision-model");

        void ItShouldSendOneUserMessage() =>
            body["messages"]!.AsArray().Single()!["role"]!.GetValue<string>().ShouldBe("user");

        void ItShouldAskForATranscription() => parts.ShouldSatisfyAllConditions(
            () => parts.Count.ShouldBe(2),
            () => parts[0]!["type"]!.GetValue<string>().ShouldBe("text"),
            () => parts[0]!["text"]!.GetValue<string>().ShouldBe(VisionService.TranscribePrompt));

        void ItShouldSendTheImageWithItsMediaType() => parts.ShouldSatisfyAllConditions(
            () => parts[1]!["type"]!.GetValue<string>().ShouldBe("image_url"),
            () => parts[1]!["image_url"]!["url"]!.GetValue<string>()
                .ShouldBe($"data:image/png;base64,{Convert.ToBase64String(Image)}"));

        void ItShouldReturnTheModelsText() => text.ShouldBe("AI service is working");
    }

    [Test]
    [Arguments("image/jpeg")]
    [Arguments("image/webp")]
    [Arguments("IMAGE/PNG")]
    public async Task SomeoneAsksAboutAnImage(string mediaType)
    {
        using IHost host = BuildHost(new() { ["Menlo:AI:Endpoint"] = "http://llama.test/v1" });

        await UseVisionAsync(host, vision => vision.AnalyzeImageAsync(Image, mediaType, "Which shop is this receipt from?"));
        JsonArray parts = _llama.Requests.Single().Body["messages"]!.AsArray().Single()!["content"]!.AsArray();

        ItShouldPassThePromptThrough();
        ItShouldSendTheImageAsThatMediaType();
        ItShouldUseTheDefaultVisionModel();

        void ItShouldPassThePromptThrough() =>
            parts[0]!["text"]!.GetValue<string>().ShouldBe("Which shop is this receipt from?");

        void ItShouldSendTheImageAsThatMediaType() => parts[1]!["image_url"]!["url"]!.GetValue<string>()
            .ShouldStartWith($"data:{mediaType.ToLowerInvariant()};base64,", Case.Sensitive);

        void ItShouldUseTheDefaultVisionModel() =>
            _llama.Requests.Single().Body["model"]!.GetValue<string>().ShouldBe("qwen3.5-4b");
    }

    [Test]
    public async Task TheImageIsEmpty()
    {
        using IHost host = BuildHost(new() { ["Menlo:AI:Endpoint"] = "http://llama.test/v1" });

        Exception? exception = await Catch.ExceptionAsync(() => UseVisionAsync(host, vision => vision.ExtractTextAsync([], "image/png")));

        ItShouldBeRefused();
        ItShouldNotAskTheModel();

        void ItShouldBeRefused() => exception.ShouldBeOfType<ArgumentException>().ParamName.ShouldBe("imageData");

        void ItShouldNotAskTheModel() => _llama.Requests.ShouldBeEmpty();
    }

    [Test]
    [Arguments("image/gif")]
    [Arguments("application/pdf")]
    [Arguments("")]
    public async Task TheImageIsNotASupportedType(string mediaType)
    {
        using IHost host = BuildHost(new() { ["Menlo:AI:Endpoint"] = "http://llama.test/v1" });

        Exception? exception = await Catch.ExceptionAsync(() => UseVisionAsync(host, vision => vision.ExtractTextAsync(Image, mediaType)));

        ItShouldBeRefused();
        ItShouldNotAskTheModel();

        void ItShouldBeRefused() => exception.ShouldBeOfType<ArgumentException>().ParamName.ShouldBe("mediaType");

        void ItShouldNotAskTheModel() => _llama.Requests.ShouldBeEmpty();
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    public async Task ThePromptIsBlank(string prompt)
    {
        using IHost host = BuildHost(new() { ["Menlo:AI:Endpoint"] = "http://llama.test/v1" });

        Exception? exception = await Catch.ExceptionAsync(() => UseVisionAsync(host, vision => vision.AnalyzeImageAsync(Image, "image/png", prompt)));

        ItShouldBeRefused();
        ItShouldNotAskTheModel();

        void ItShouldBeRefused() => exception.ShouldBeOfType<ArgumentException>().ParamName.ShouldBe("prompt");

        void ItShouldNotAskTheModel() => _llama.Requests.ShouldBeEmpty();
    }

    [Test]
    public async Task ThinkingIsLeftAtItsDefault()
    {
        using IHost host = BuildHost(new() { ["Menlo:AI:Endpoint"] = "http://llama.test/v1" });

        await UseVisionAsync(host, vision => vision.ExtractTextAsync(Image, "image/png"));
        JsonObject body = _llama.Requests.Single().Body;

        ItShouldAskTheModelNotToThink();

        void ItShouldAskTheModelNotToThink() =>
            body["chat_template_kwargs"]!["enable_thinking"]!.GetValue<bool>().ShouldBeFalse();
    }

    [Test]
    public async Task ThinkingIsEnabled()
    {
        using IHost host = BuildHost(new()
        {
            ["Menlo:AI:Endpoint"] = "http://llama.test/v1",
            ["Menlo:AI:EnableThinking"] = "true"
        });

        await UseVisionAsync(host, vision => vision.ExtractTextAsync(Image, "image/png"));
        JsonObject body = _llama.Requests.Single().Body;

        ItShouldLeaveTheChatTemplateAlone();

        void ItShouldLeaveTheChatTemplateAlone() => body.ContainsKey("chat_template_kwargs").ShouldBeFalse();
    }

    [Test]
    public async Task TheModelServerFails()
    {
        _llama.FailWith = HttpStatusCode.ServiceUnavailable;
        using IHost host = BuildHost(new() { ["Menlo:AI:Endpoint"] = "http://llama.test/v1" });

        Exception? exception = await Catch.ExceptionAsync(() => UseVisionAsync(host, vision => vision.ExtractTextAsync(Image, "image/png")));

        ItShouldSurfaceTheFailure();
        ItShouldNotRetry();

        void ItShouldSurfaceTheFailure() => exception.ShouldBeOfType<ClientResultException>().Status.ShouldBe(503);

        void ItShouldNotRetry() => _llama.Requests.Count.ShouldBe(1);
    }

    [Test]
    public async Task TheModelTakesTooLong()
    {
        HangingModel model = new();
        VisionService vision = model.CreateVisionService(TimeSpan.FromMilliseconds(100));

        Exception? exception = await Catch.ExceptionAsync(() => vision.ExtractTextAsync(Image, "image/png"));

        ItShouldReportATimeout();
        ItShouldStopWaitingForTheModel();

        void ItShouldReportATimeout() => exception.ShouldBeOfType<TimeoutException>()
            .InnerException.ShouldBeAssignableTo<OperationCanceledException>();

        void ItShouldStopWaitingForTheModel() => model.Token!.Value.IsCancellationRequested.ShouldBeTrue();
    }

    [Test]
    public async Task TheCallerGivesUpFirst()
    {
        HangingModel model = new();
        VisionService vision = model.CreateVisionService(TimeSpan.FromMinutes(10));
        using CancellationTokenSource caller = new(TimeSpan.FromMilliseconds(100));

        Exception? exception = await Catch.ExceptionAsync(() => vision.ExtractTextAsync(Image, "image/png", caller.Token));

        ItShouldReportACancellationNotATimeout();
        ItShouldStopWaitingForTheModel();

        void ItShouldReportACancellationNotATimeout() => exception.ShouldBeAssignableTo<OperationCanceledException>();

        void ItShouldStopWaitingForTheModel() => model.Token!.Value.IsCancellationRequested.ShouldBeTrue();
    }

    /// <summary>A model that never answers, only stopping when its call is cancelled.</summary>
    private sealed class HangingModel
    {
        private readonly IChatClient _chatClient = Substitute.For<IChatClient>();

        public HangingModel() => _chatClient
            .GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                Token = call.Arg<CancellationToken>();
                await Task.Delay(Timeout.InfiniteTimeSpan, Token.Value);
                return new ChatResponse();
            });

        /// <summary>The cancellation token the model was called with.</summary>
        public CancellationToken? Token { get; private set; }

        public VisionService CreateVisionService(TimeSpan timeout) => new(_chatClient.AsAIAgent(), Options.Create(new AiSettings
        {
            Endpoint = new Uri("http://llama.test/v1"),
            VisionTimeout = timeout
        }));
    }

    private IHost BuildHost(Dictionary<string, string?> configuration)
    {
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Configuration.AddInMemoryCollection(configuration);
        builder.AddMenloAi();
        builder.Services.AddSingleton<PipelineTransport>(new HttpClientPipelineTransport(new HttpClient(_llama)));
        return builder.Build();
    }

    private static async Task<T> UseVisionAsync<T>(IHost host, Func<IVisionService, Task<T>> use)
    {
        using IServiceScope scope = host.Services.CreateScope();
        return await use(scope.ServiceProvider.GetRequiredService<IVisionService>());
    }
}
