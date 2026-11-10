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
                .ValidateOnStart();

            builder.Services.AddSingleton<IChatClient>(sp =>
            {
                AiSettings settings = sp.GetRequiredService<IOptions<AiSettings>>().Value;
                OpenAIClientOptions clientOptions = new() { Endpoint = settings.Endpoint };
                if (sp.GetService<PipelineTransport>() is { } transport)
                {
                    clientOptions.Transport = transport;
                }

                OpenAIClient client = new(new ApiKeyCredential(string.IsNullOrWhiteSpace(settings.ApiKey) ? NoApiKey : settings.ApiKey), clientOptions);
                return client.GetChatClient(settings.ChatModel).AsIChatClient();
            });
            builder.Services.AddSingleton<AIAgent>(sp => sp.GetRequiredService<IChatClient>().AsAIAgent());

            builder.Services.AddScoped<IChatService, ChatService>();
            builder.Services.AddScoped<IVisionService, VisionService>();

            return builder;
        }
    }
}
