using Menlo.AI.Configuration;
using Menlo.AI.Interfaces;
using Menlo.AI.Services;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenAI;
using System.ClientModel;
using System.ClientModel.Primitives;

namespace Menlo.AI.Extensions;

/// <summary>
/// Extension methods for registering Menlo AI services
/// </summary>
public static class ServiceCollectionExtensions
{
    // llama.cpp ignores the key unless it was started with --api-key, but the OpenAI client requires one.
    private const string NoApiKey = "no-key";

    // A day is far beyond any real call and keeps the timeout (plus margin) within what CancelAfter accepts.
    private static readonly TimeSpan MaxVisionTimeout = TimeSpan.FromDays(1);
    private static readonly TimeSpan VisionNetworkTimeoutMargin = TimeSpan.FromMinutes(1);

    extension(IHostApplicationBuilder builder)
    {
        /// <summary>
        /// Add Menlo AI services, talking to the OpenAI-compatible llama.cpp server configured in <c>Menlo:AI</c>.
        /// </summary>
        /// <remarks>
        /// The OpenAI client uses its own HTTP transport, not <c>IHttpClientFactory</c>, so the service defaults'
        /// resilience handler (and its short attempt timeout) does not apply to model calls. Register a
        /// <see cref="PipelineTransport"/> to replace the transport, for example with a stub in tests.
        /// </remarks>
        public IHostApplicationBuilder AddMenloAi()
        {
            builder.Services
                .AddOptions<AiSettings>()
                .Bind(builder.Configuration.GetSection(AiSettings.SectionName))
                .Validate(settings => settings.Endpoint is not null, $"{AiSettings.SectionName}:Endpoint is required.")
                .Validate(
                    settings => settings.Endpoint is null || (settings.Endpoint.IsAbsoluteUri && settings.Endpoint.Scheme is "http" or "https"),
                    $"{AiSettings.SectionName}:Endpoint must be an absolute http or https URL.")
                .Validate(settings => !string.IsNullOrWhiteSpace(settings.ChatModel), $"{AiSettings.SectionName}:ChatModel is required.")
                .Validate(settings => !string.IsNullOrWhiteSpace(settings.VisionModel), $"{AiSettings.SectionName}:VisionModel is required.")
                .Validate(
                    settings => settings.VisionTimeout > TimeSpan.Zero && settings.VisionTimeout <= MaxVisionTimeout,
                    $"{AiSettings.SectionName}:VisionTimeout must be positive and at most {MaxVisionTimeout}.")
                .ValidateOnStart();

            builder.Services.AddSingleton<IChatClient>(sp =>
            {
                AiSettings settings = sp.GetRequiredService<IOptions<AiSettings>>().Value;
                return CreateClient(sp, settings, _ => { }).GetChatClient(settings.ChatModel).AsIChatClient();
            });
            builder.Services.AddSingleton<AIAgent>(sp => sp.GetRequiredService<IChatClient>().AsAIAgent());

            // Vision calls can take minutes, so they get their own client: the SDK's 100 s network timeout would cut them
            // off, and its retries would repeat a call that already ran to the timeout. VisionService enforces the timeout;
            // the network timeout sits a margin above it, so it never fires first.
            builder.Services.AddKeyedSingleton<AIAgent>(VisionService.AgentKey, (sp, _) =>
            {
                AiSettings settings = sp.GetRequiredService<IOptions<AiSettings>>().Value;
                OpenAIClient client = CreateClient(sp, settings, options =>
                {
                    options.NetworkTimeout = settings.VisionTimeout + VisionNetworkTimeoutMargin;
                    options.RetryPolicy = new ClientRetryPolicy(maxRetries: 0);
                });
                return client.GetChatClient(settings.VisionModel).AsIChatClient().AsAIAgent();
            });

            builder.Services.AddScoped<IChatService, ChatService>();
            builder.Services.AddScoped<IVisionService, VisionService>();

            return builder;
        }
    }

    private static OpenAIClient CreateClient(IServiceProvider sp, AiSettings settings, Action<OpenAIClientOptions> configure)
    {
        OpenAIClientOptions clientOptions = new() { Endpoint = settings.Endpoint };
        if (sp.GetService<PipelineTransport>() is { } transport)
        {
            clientOptions.Transport = transport;
        }

        configure(clientOptions);
        return new OpenAIClient(new ApiKeyCredential(string.IsNullOrWhiteSpace(settings.ApiKey) ? NoApiKey : settings.ApiKey), clientOptions);
    }
}
