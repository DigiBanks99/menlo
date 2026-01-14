using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Menlo.AI.Configuration;
using Menlo.AI.Interfaces;
using Menlo.AI.Services;

#pragma warning disable SKEXP0070 // Ollama connector is experimental

namespace Menlo.AI.Extensions;

/// <summary>
/// Extension methods for registering Menlo AI services
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        /// <summary>
        /// Add Menlo AI services to the service collection
        /// </summary>
        public IServiceCollection AddMenloAi()
        {
            // Configure AI settings
            builder.Services.Configure<AiSettings>(builder.Configuration.GetSection(AiSettings.SectionName));

            // Register AI services
            builder.Services.AddScoped<IChatService, ChatService>();
            builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
            builder.Services.AddScoped<IVisionService, VisionService>();

            // Register Semantic Kernel
            builder.Services.AddKernel();

            return builder.Services;
        }

        /// <summary>
        /// Add Menlo AI services with Aspire Ollama integration
        /// </summary>
        public IHostApplicationBuilder AddMenloAiWithAspire()
        {
            // Configure AI settings
            builder.Services.Configure<AiSettings>(builder.Configuration.GetSection(AiSettings.SectionName));

            // Register AI services
            builder.Services.AddScoped<IChatService, ChatService>();
            builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
            builder.Services.AddScoped<IVisionService, VisionService>();

            // Register Semantic Kernel with Ollama connection from Aspire configuration
            builder.Services.AddKernel();
            builder.Services.AddOllamaChatClient("text");
            builder.Services.AddOllamaEmbeddingGenerator("text");

            return builder;
        }
    }
}
