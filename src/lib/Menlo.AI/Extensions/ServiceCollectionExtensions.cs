using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
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
    /// <summary>
    /// Add Menlo AI services to the service collection
    /// </summary>
    public static IServiceCollection AddMenloAI(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure AI settings
        services.Configure<AiSettings>(configuration.GetSection(AiSettings.SectionName));

        // Register AI services
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IEmbeddingService, EmbeddingService>();
        services.AddScoped<IVisionService, VisionService>();

        // Register Semantic Kernel
        services.AddKernel();

        return services;
    }

    /// <summary>
    /// Add Menlo AI services with Aspire Ollama integration
    /// </summary>
    public static IServiceCollection AddMenloAIWithAspire(this IServiceCollection services,
        IHostApplicationBuilder builder)
    {
        var configuration = builder.Configuration;

        // Configure AI settings
        services.Configure<AiSettings>(configuration.GetSection(AiSettings.SectionName));

        // Register AI services
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IEmbeddingService, EmbeddingService>();
        services.AddScoped<IVisionService, VisionService>();

        // Register Semantic Kernel with Ollama connection from Aspire configuration
        services.AddKernel();
        services.AddOllamaChatClient("text");
        services.AddOllamaEmbeddingGenerator("text");

        return services;
    }
}
