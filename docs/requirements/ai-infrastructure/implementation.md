# Ollama AI Infrastructure Setup Implementation Guide

This document outlines the step-by-step process for integrating Ollama with Semantic Kernel and .NET Aspire to provide local AI processing capabilities in the Menlo Home Management solution.

## Prerequisites

- Review the [Architecture Document](../../explanations/architecture-document.md) for AI infrastructure requirements.
- Review the [Concepts & Terminology](../../explanations/concepts-and-terminology.md) for the "Blueberry Muffin" AI integration philosophy.
- .NET 9 SDK or later installed.
- Docker Desktop or Podman for container orchestration.

## Windows setup (native, no containers) — winget + PowerShell

If you prefer running Ollama directly on Windows (outside of .NET Aspire containers), follow these steps. This is useful for quick local development, laptop demos, or when you don’t want to run Docker.

### A. Install required tools

```powershell
# 1) Update winget sources (optional but recommended)
winget source update

# 2) Install Ollama (official package)
winget install --id Ollama.Ollama --silent --accept-package-agreements --accept-source-agreements

# 3) (Optional) Install Docker Desktop for Open WebUI or container scenarios
winget install --id Docker.DockerDesktop --silent --accept-package-agreements --accept-source-agreements

# 4) (Optional) Install Git if not present
winget install --id Git.Git --silent --accept-package-agreements --accept-source-agreements
```

Verify installation:

```powershell
ollama --version
```

### B. Start or check the Ollama service

The Windows installer registers a background service. Confirm and start it:

```powershell
# Check service state
Get-Service -Name "Ollama" -ErrorAction SilentlyContinue

# Start (if not running)
Start-Service -Name "Ollama"

# Set to start automatically on boot
Set-Service -Name "Ollama" -StartupType Automatic
```

By default the Ollama API listens on http://localhost:11434.

### C. Open firewall (if needed)

Most setups won’t require this for localhost access. If you’re calling from other devices on your LAN and explicitly bind externally, add a firewall rule:

```powershell
New-NetFirewallRule -DisplayName "Ollama 11434" -Direction Inbound -Protocol TCP -LocalPort 11434 -Action Allow
```

### D. Download models

Pull the models used by Menlo. If a specific model variant isn’t available on your machine, use the alternative development-friendly models noted below.

```powershell
# Preferred text model (small, efficient). If unavailable, see alternative below.
ollama pull phi4:latest  # or a small variant like phi4:mini when available on your setup

# Preferred vision model (for handwriting and images). If unavailable, use llava as an alternative.
ollama pull phi4-vision:latest  # if not present on your catalog, use llava:latest

# Alternatives for constrained/dev environments
ollama pull phi3.5:3.8b   # lightweight text model (~2GB)
ollama pull llava:latest   # common community vision model
```

List available models to confirm downloads:

```powershell
ollama list
```

### E. Quick verification (PowerShell)

```powershell
# 1) Check tags via API
Invoke-RestMethod -Uri http://localhost:11434/api/tags -Method Get | ConvertTo-Json -Depth 5

# 2) Test a text generation call (replace model with the one you pulled)
$body = @{ model = "phi4"; prompt = "Say hello from Menlo"; stream = $false } | ConvertTo-Json
Invoke-RestMethod -Uri http://localhost:11434/api/generate -Method Post -Body $body -ContentType "application/json"

# 3) (Optional) Test a vision call with an image (Base64)
$imgPath = "C:\\path\\to\\image.jpg"
$imgBytes = [Convert]::ToBase64String([IO.File]::ReadAllBytes($imgPath))
$vbody = @{ model = "phi4-vision"; prompt = "Describe this image"; images = @($imgBytes); stream = $false } | ConvertTo-Json -Depth 5
Invoke-RestMethod -Uri http://localhost:11434/api/generate -Method Post -Body $vbody -ContentType "application/json"
```

ℹ️ Gotcha: Model names vary per catalog
> If `phi4`/`phi4-vision` are not available through your Ollama index, use `phi3.5:3.8b` for text and `llava:latest` for vision during development. In container/Aspire mode, models are bootstrapped using the AppHost configuration and may reference provider-specific names.

### F. Optional: Open WebUI on Windows (connect to native Ollama)

```powershell
# Requires Docker Desktop
docker run -d --name open-webui -p 3000:8080 `
    -e OLLAMA_BASE_URL=http://host.docker.internal:11434 `
    ghcr.io/open-webui/open-webui:main

# Then open http://localhost:3000 and point it at the Ollama endpoint if needed
```

### G. Optional: GPU acceleration on Windows

- NVIDIA: Ensure recent NVIDIA drivers and CUDA runtime. Ollama can use CUDA when available.
- Set environment variables (service restart required after changes):

```powershell
# Use more GPU layers (tune per VRAM)
setx OLLAMA_GPU_LAYERS -1

# Parallel requests (adjust for your CPU/GPU)
setx OLLAMA_NUM_PARALLEL 2

# Keep models warm in memory
setx OLLAMA_KEEP_ALIVE 1h
```

Restart the Ollama service to apply environment changes:

```powershell
Restart-Service -Name "Ollama"
```

### H. Point Menlo API to native Ollama (when not using Aspire)

If you’re running the API without the Aspire AppHost, configure the Ollama endpoint explicitly:

```csharp
// In AddMenloAi (manual configuration path)
services.AddOllamaChatCompletion(
        modelId: "phi4",  // or your chosen local model id
        endpoint: new Uri("http://localhost:11434"));
```

Add an appsetting for manual configuration if needed:

```json
{
    "ConnectionStrings": {
        "ollama": "http://localhost:11434"
    }
}
```

This keeps development flexible: you can switch between native Windows Ollama and containerized Aspire Ollama with minimal code changes.

## Steps

### 1. Add AI NuGet Packages ✅

Add the required AI and Semantic Kernel packages:

```sh
# Core Semantic Kernel
dotnet add src/api/Menlo.Api/Menlo.Api.csproj package Microsoft.SemanticKernel

# Ollama connector (experimental)
dotnet add src/api/Menlo.Api/Menlo.Api.csproj package Microsoft.SemanticKernel.Connectors.Ollama --prerelease

# Aspire Community Toolkit for Ollama hosting
dotnet add src/api/Menlo.AppHost/Menlo.AppHost.csproj package CommunityToolkit.Aspire.Hosting.Ollama

# Aspire Community Toolkit for Ollama client integration  
dotnet add src/api/Menlo.Api/Menlo.Api.csproj package CommunityToolkit.Aspire.Ollama
```

### 2. Configure Ollama Models in AppHost ✅

Update `AppHost.cs` in `Menlo.AppHost` to include Ollama container with automated model bootstrapping:

```csharp
using CommunityToolkit.Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL
IResourceBuilder<PostgresServerResource> postgres = builder
    .AddPostgres("postgres")
    .WithLifetime(ContainerLifetime.Persistent);
IResourceBuilder<PostgresDatabaseResource> postgresDb = postgres
    .AddDatabase("postgresdb");

// Add Ollama with automatic model bootstrapping and persistent storage
var ollama = builder.AddOllama("ollama")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("ollama-models")  // Persist models across container restarts
    .WithOpenWebUI();  // Optional: Add Open WebUI for model testing

// Add Phi-4-mini for text processing (downloads automatically on startup)
var phi4Mini = ollama.AddModel("phi4-mini", "microsoft/phi-4:latest");

// Add Phi-4-vision for image/handwriting recognition (downloads automatically on startup)
var phi4Vision = ollama.AddModel("phi4-vision", "microsoft/phi-4-vision:latest");

// Alternative: Use Hugging Face models directly
// var llamaModel = ollama.AddHuggingFaceModel("llama", "bartowski/Llama-3.2-1B-Instruct-GGUF:IQ4_XS");

// Optional: GPU support (uncomment if you have GPU available)
// var ollama = builder.AddOllama("ollama")
//     .WithDataVolume("ollama-models")
//     .WithContainerRuntimeArgs("--device", "nvidia.com/gpu=all");

// Add API project with model references
var api = builder.AddProject<Projects.Menlo_Api>("menlo-api")
    .WithReference(postgres)
    .WithReference(phi4Mini)
    .WithReference(phi4Vision)
    .WaitFor(postgres)
    .WaitFor(phi4Mini)
    .WaitFor(phi4Vision);

builder.Build().Run();
```

**Automatic Model Bootstrapping Features:**

- **Auto-download**: Models download automatically when Ollama container starts
- **Progress tracking**: Download progress visible in .NET Aspire dashboard with real-time status
- **Health checks**: Ollama server and individual models have separate health checks
  - Server health: Verifies Ollama API is running and accessible
  - Model health: Models marked unhealthy until download completes (prevents premature API calls)
- **Persistent storage**: Models cached in Docker volumes for faster subsequent startups
- **GPU support**: Optional GPU acceleration for faster inference (Docker/Podman compatible)
- **Open WebUI**: Optional web interface for testing models directly in browser
- **Hugging Face integration**: Direct support for Hugging Face model hub models

**Important Notes:**

- **Keep AppHost running**: Model downloads will be cancelled if AppHost stops during download
- **Volume persistence**: Without `WithDataVolume()`, models re-download on each container restart
- **Model size**: Phi-4 models are 7-14GB each - ensure adequate disk space and bandwidth
- **First startup**: Initial startup may take 10-30 minutes depending on internet speed

### 2.1. Understanding Ollama Bootstrapping Process ✅

The Aspire Community Toolkit provides sophisticated Ollama bootstrapping capabilities. Here's how the process works:

**Startup Sequence:**

1. **Container Launch**: Aspire starts the Ollama container from `docker.io/ollama/ollama`
2. **Health Check**: Initial health check verifies Ollama API is responding
3. **Model Download**: Configured models download automatically in parallel
4. **Volume Persistence**: Models are cached to `ollama-models` volume for future startups
5. **Service Ready**: Models marked healthy when fully downloaded and loaded

**Monitoring Download Progress:**

```bash
# View in Aspire Dashboard
# Navigate to http://localhost:15888 (or configured port)
# Check the "State" column for download progress

# Manual verification via Ollama API
curl http://localhost:11434/api/tags
# Should list downloaded models when complete
```

**Bootstrapping Configuration Options:**

```csharp
// Basic configuration with auto-download
var ollama = builder.AddOllama("ollama")
    .WithDataVolume("ollama-models");

// Advanced configuration with custom settings
var ollama = builder.AddOllama("ollama")
    .WithDataVolume("ollama-models")
    .WithEnvironment("OLLAMA_KEEP_ALIVE", "5m")        // Keep models in memory
    .WithEnvironment("OLLAMA_HOST", "0.0.0.0")         // Bind to all interfaces
    .WithEnvironment("OLLAMA_NUM_PARALLEL", "2")       // Parallel requests
    .WithEnvironment("OLLAMA_MAX_LOADED_MODELS", "3")  // Memory management
    .WithOpenWebUI();  // Optional web interface

// GPU-optimized configuration
var ollama = builder.AddOllama("ollama")
    .WithDataVolume("ollama-models")
    .WithContainerRuntimeArgs("--gpus=all")  // Docker GPU support
    .WithEnvironment("OLLAMA_GPU_LAYERS", "32");  // GPU acceleration layers

// Production-ready configuration with resource limits
var ollama = builder.AddOllama("ollama")
    .WithDataVolume("ollama-models")
    .WithEnvironment("OLLAMA_KEEP_ALIVE", "24h")       // Keep models loaded longer
    .WithEnvironment("OLLAMA_MAX_LOADED_MODELS", "5")  // Multiple model support
    .WithMemoryLimit("8GB")                            // Container memory limit
    .WithCpuLimit(4.0);                               // CPU core limit
```

**Model Management:**

```csharp
// Standard Ollama Hub models
var phi4 = ollama.AddModel("phi4-mini", "microsoft/phi-4:latest");
var llama = ollama.AddModel("llama", "llama3.2:latest");

// Hugging Face models (alternative approach)
var customModel = ollama.AddHuggingFaceModel("custom", "bartowski/Llama-3.2-1B-Instruct-GGUF:IQ4_XS");

// Multiple model configurations for different use cases
var textProcessor = ollama.AddModel("text-processor", "phi3.5:latest");
var visionProcessor = ollama.AddModel("vision-processor", "llava:latest");
var coder = ollama.AddModel("code-assistant", "codellama:latest");
```

**Troubleshooting Common Issues:**

| Problem                    | Symptoms                                | Solution                                                                     |
| -------------------------- | --------------------------------------- | ---------------------------------------------------------------------------- |
| **Models not downloading** | Health checks failing, empty model list | Ensure AppHost stays running during download                                 |
| **Slow download speeds**   | Extended startup times                  | Use smaller models (e.g., `phi3.5:3.8b` vs `phi3.5:latest`)                  |
| **Out of disk space**      | Container startup failures              | Monitor disk usage - Phi-4 models require 7-14GB each                        |
| **Memory issues**          | Container restarts, OOM errors          | Add memory limits: `.WithMemoryLimit("8GB")`                                 |
| **GPU not detected**       | CPU-only inference despite GPU          | Verify Docker Desktop GPU support, use correct runtime args                  |
| **Port conflicts**         | Connection refused errors               | Check if port 11434 is available, use `.WithHttpEndpoint()` for custom ports |
| **Health check timeouts**  | Services marked unhealthy               | Increase health check timeout or adjust model count                          |

**Performance Optimization:**

```csharp
// Development: Fast startup, smaller models
var ollama = builder.AddOllama("ollama")
    .WithDataVolume("ollama-models");
var phi3Mini = ollama.AddModel("phi3-mini", "phi3.5:3.8b");  // Smaller variant

// Production: Balanced performance and capability
var ollama = builder.AddOllama("ollama")
    .WithDataVolume("ollama-models")
    .WithEnvironment("OLLAMA_KEEP_ALIVE", "1h")
    .WithEnvironment("OLLAMA_NUM_PARALLEL", "4")
    .WithMemoryLimit("16GB");
var phi4 = ollama.AddModel("phi4", "microsoft/phi-4:latest");

// High-performance: GPU acceleration, multiple models
var ollama = builder.AddOllama("ollama")
    .WithDataVolume("ollama-models")
    .WithContainerRuntimeArgs("--gpus=all")
    .WithEnvironment("OLLAMA_GPU_LAYERS", "-1")  // Use all GPU layers
    .WithEnvironment("OLLAMA_KEEP_ALIVE", "24h")
    .WithMemoryLimit("32GB");
```

**Validation Steps:**

```bash
# 1. Verify container is running
docker ps | grep ollama

# 2. Check model availability
curl http://localhost:11434/api/tags

# 3. Test model inference
curl http://localhost:11434/api/generate -d '{
  "model": "phi4-mini",
  "prompt": "Hello, how are you?",
  "stream": false
}'

# 4. Monitor resource usage
docker stats ollama

# 5. Check logs for issues
docker logs ollama
```

### 3. Create AI Service Infrastructure ✅

Create `AiServiceCollectionExtensions.cs` in `Menlo.Api/src/Ai/`:

```csharp
using Microsoft.SemanticKernel;
using CommunityToolkit.Aspire.OllamaSharp;

#pragma warning disable SKEXP0070 // Ollama connector is experimental

namespace Menlo.Api.Ai;

public static class AiServiceCollectionExtensions
{
    public static IServiceCollection AddMenloAi(this IServiceCollection services, IConfiguration configuration)
    {
        // Register Semantic Kernel with Ollama
        services.AddKernel();
        
        // Add Ollama client API integration (preferred method with Aspire)
        services.AddOllamaClientApi("phi4-mini");  // Uses model reference from AppHost
        
        // Alternative: Manual Ollama chat completion configuration
        // services.AddOllamaChatCompletion(
        //     modelId: "phi4-mini",
        //     endpoint: new Uri(configuration.GetConnectionString("ollama") ?? "http://localhost:11434"));

        // Register multiple models for different use cases (optional)
        services.AddKeyedOllamaClientApi("text-analysis");  // For general text processing
        services.AddKeyedOllamaClientApi("vision");         // For image/handwriting analysis

        // Add AI service abstractions
        services.AddScoped<ITextAnalysisService, TextAnalysisService>();
        services.AddScoped<IListInterpretationService, ListInterpretationService>();
        services.AddScoped<ICostEstimationService, CostEstimationService>();
        services.AddScoped<ICategorizationService, CategorizationService>();
        
        // Add caching for AI responses
        services.AddMemoryCache();
        services.AddScoped<IAiResponseCache, AiResponseCache>();
        
        // Add fallback patterns
        services.Configure<AiServiceOptions>(configuration.GetSection("AiService"));
        services.AddScoped<IAiFallbackHandler, AiFallbackHandler>();

        return services;
    }
}

public class AiServiceOptions
{
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
    public bool EnableCaching { get; set; } = true;
    public bool EnableFallback { get; set; } = true;
    public string DefaultModel { get; set; } = "phi4-mini";
    public string VisionModel { get; set; } = "phi4-vision";
}
```

### 3.1. Understanding Model Reference Integration ✅

The Aspire Community Toolkit creates seamless integration between AppHost model configuration and API client usage:

**AppHost to Client Model Mapping:**

```csharp
// In AppHost.cs
var phi4Mini = ollama.AddModel("phi4-mini", "microsoft/phi-4:latest");
var phi4Vision = ollama.AddModel("phi4-vision", "microsoft/phi-4-vision:latest");

var api = builder.AddProject<Projects.Menlo_Api>("menlo-api")
    .WithReference(phi4Mini)      // Creates connection string "phi4-mini"
    .WithReference(phi4Vision);   // Creates connection string "phi4-vision"

// In API service registration
services.AddOllamaClientApi("phi4-mini");  // Uses model reference from AppHost
```

**Connection String Generation:**

When you use `.WithReference(phi4Mini)`, Aspire automatically generates connection strings:

```json
{
  "ConnectionStrings": {
    "phi4-mini": "http://ollama:11434",
    "phi4-vision": "http://ollama:11434",
    "ollama": "http://ollama:11434"
  }
}
```

**Multi-Model Service Usage:**

```csharp
public class ExampleAiService
{
    private readonly IOllamaClientApi _textModel;
    private readonly IOllamaClientApi _visionModel;

    public ExampleAiService(
        [FromKeyedServices("text-analysis")] IOllamaClientApi textModel,
        [FromKeyedServices("vision")] IOllamaClientApi visionModel)
    {
        _textModel = textModel;
        _visionModel = visionModel;
    }

    public async Task<string> ProcessTextAsync(string input)
    {
        // Uses phi4-mini model via text-analysis key
        var response = await _textModel.GenerateCompletion(input);
        return response.Response;
    }

    public async Task<string> ProcessImageAsync(byte[] imageData)
    {
        // Uses phi4-vision model via vision key
        var response = await _visionModel.GenerateCompletion(
            prompt: "Describe this image",
            images: new[] { Convert.ToBase64String(imageData) });
        return response.Response;
    }
}
```

**Configuration Validation:**

Add health checks to ensure models are properly configured:

```csharp
// In AddMenloAi extension
services.AddHealthChecks()
    .AddCheck<OllamaHealthCheck>("ollama-text-model", tags: new[] { "ai", "models" })
    .AddCheck<OllamaVisionHealthCheck>("ollama-vision-model", tags: new[] { "ai", "models" });
```

### 4. Create AI Service Abstractions ✅

Create AI service interfaces in `Menlo.Api/src/Ai/Abstractions/`:

```csharp
using CSharpFunctionalExtensions;

namespace Menlo.Api.Ai.Abstractions;

// Core text analysis service
public interface ITextAnalysisService
{
    Task<Result<TextAnalysisResult>> AnalyzeTextAsync(string text, CancellationToken cancellationToken = default);
}

// Planning list interpretation from photos/text
public interface IListInterpretationService  
{
    Task<Result<ListInterpretationResult>> InterpretListAsync(string listText, ListContext context, CancellationToken cancellationToken = default);
}

// Cost estimation for list items
public interface ICostEstimationService
{
    Task<Result<CostEstimationResult>> EstimateCostsAsync(IEnumerable<string> items, EstimationContext context, CancellationToken cancellationToken = default);
}

// Transaction and item categorization
public interface ICategorizationService
{
    Task<Result<CategorizationResult>> CategorizeItemsAsync(IEnumerable<string> items, CategorizationContext context, CancellationToken cancellationToken = default);
}

// AI response caching
public interface IAiResponseCache
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}

// Fallback handling when AI services fail
public interface IAiFallbackHandler
{
    Task<Result<T>> ExecuteWithFallbackAsync<T>(Func<Task<Result<T>>> aiOperation, Func<Task<Result<T>>> fallbackOperation, CancellationToken cancellationToken = default);
}
```

Create result models in `Menlo.Api/src/Ai/Models/`:

```csharp
namespace Menlo.Api.Ai.Models;

public record TextAnalysisResult(
    string ProcessedText,
    float ConfidenceScore,
    IReadOnlyList<string> ExtractedItems,
    IReadOnlyDictionary<string, string> Metadata);

public record ListInterpretationResult(
    IReadOnlyList<InterpretedItem> Items,
    float OverallConfidence,
    string ListType,
    IReadOnlyDictionary<string, string> Context);

public record InterpretedItem(
    string OriginalText,
    string CleanedText,
    string? SuggestedCategory,
    float ConfidenceScore);

public record CostEstimationResult(
    IReadOnlyList<ItemCostEstimate> Estimates,
    decimal TotalEstimatedCost,
    string Currency,
    float OverallConfidence);

public record ItemCostEstimate(
    string Item,
    decimal EstimatedCost,
    decimal MinCost,
    decimal MaxCost,
    float Confidence,
    string? ReasoningNotes);

public record CategorizationResult(
    IReadOnlyList<CategorizedItem> CategorizedItems,
    float OverallConfidence,
    IReadOnlyList<string> SuggestedNewCategories);

public record CategorizedItem(
    string Item,
    string SuggestedCategory,
    float Confidence,
    IReadOnlyList<string> AlternativeCategories);

// Context objects
public record ListContext(
    string ListType,
    DateOnly? TargetDate,
    string? BudgetCategory,
    IReadOnlyDictionary<string, object> AdditionalContext);

public record EstimationContext(
    string Currency,
    string Location,
    DateOnly EstimationDate,
    string? BudgetCategory);

public record CategorizationContext(
    IReadOnlyList<string> ExistingCategories,
    string Domain,
    IReadOnlyDictionary<string, string> UserPreferences);
```

### 5. Implement AI Services ✅

Create `TextAnalysisService.cs` in `Menlo.Api/src/Ai/Services/`:

```csharp
using CSharpFunctionalExtensions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Menlo.Api.Ai.Abstractions;
using Menlo.Api.Ai.Models;

namespace Menlo.Api.Ai.Services;

public class TextAnalysisService : ITextAnalysisService
{
    private readonly Kernel _kernel;
    private readonly IAiResponseCache _cache;
    private readonly ILogger<TextAnalysisService> _logger;

    public TextAnalysisService(Kernel kernel, IAiResponseCache cache, ILogger<TextAnalysisService> logger)
    {
        _kernel = kernel;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<TextAnalysisResult>> AnalyzeTextAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            string cacheKey = $"text_analysis_{text.GetHashCode()}";
            
            // Check cache first
            var cachedResult = await _cache.GetAsync<TextAnalysisResult>(cacheKey, cancellationToken);
            if (cachedResult != null)
            {
                _logger.LogDebug("Returning cached text analysis result");
                return Result.Success(cachedResult);
            }

            // Get chat completion service
            var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
            
            string prompt = $"""
                Analyze the following text and extract individual items from what appears to be a planning list:
                
                Text: {text}
                
                Please respond with a JSON object containing:
                - processedText: cleaned version of the input
                - confidenceScore: float between 0-1 indicating analysis confidence
                - extractedItems: array of individual items found
                - metadata: object with any additional context
                
                Focus on identifying discrete items, quantities, and any implicit categories.
                """;

            var result = await chatCompletion.GetChatMessageContentAsync(prompt, cancellationToken: cancellationToken);
            
            // Parse AI response (simplified - would need proper JSON parsing)
            var analysisResult = ParseTextAnalysisResponse(result.Content ?? string.Empty, text);
            
            // Cache the result
            await _cache.SetAsync(cacheKey, analysisResult, TimeSpan.FromHours(1), cancellationToken);
            
            return Result.Success(analysisResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze text: {Text}", text);
            return Result.Failure<TextAnalysisResult>($"Text analysis failed: {ex.Message}");
        }
    }
    
    private static TextAnalysisResult ParseTextAnalysisResponse(string aiResponse, string originalText)
    {
        // Simplified parsing - in real implementation, use System.Text.Json
        // For now, return a basic result
        var items = originalText.Split(['\n', ','], StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Trim())
            .Where(item => !string.IsNullOrEmpty(item))
            .ToList();
            
        return new TextAnalysisResult(
            ProcessedText: originalText.Trim(),
            ConfidenceScore: 0.8f,
            ExtractedItems: items,
            Metadata: new Dictionary<string, string>
            {
                { "ItemCount", items.Count.ToString() },
                { "ProcessedAt", DateTime.UtcNow.ToString("O") }
            });
    }
}
```

Create `AiResponseCache.cs` in `Menlo.Api/src/Ai/Services/`:

```csharp
using Microsoft.Extensions.Caching.Memory;
using Menlo.Api.Ai.Abstractions;
using System.Text.Json;

namespace Menlo.Api.Ai.Services;

public class AiResponseCache : IAiResponseCache
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<AiResponseCache> _logger;

    public AiResponseCache(IMemoryCache cache, ILogger<AiResponseCache> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = _cache.Get<T>(key);
            _logger.LogDebug("Cache {Status} for key: {Key}", result != null ? "HIT" : "MISS", key);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get cached value for key: {Key}", key);
            return Task.FromResult(default(T));
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new MemoryCacheEntryOptions();
            if (expiry.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiry.Value;
            }
            else
            {
                options.SlidingExpiration = TimeSpan.FromMinutes(30);
            }

            _cache.Set(key, value, options);
            _logger.LogDebug("Cached value for key: {Key} with expiry: {Expiry}", key, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache value for key: {Key}", key);
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _cache.Remove(key);
            _logger.LogDebug("Removed cached value for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove cached value for key: {Key}", key);
        }

        return Task.CompletedTask;
    }
}
```

### 6. Create AI Integration Tests ✅

Create `AiServiceTests.cs` in `Menlo.Api.IntegrationTests/`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Menlo.Api.Ai.Abstractions;

namespace Menlo.Api.IntegrationTests;

public class AiServiceTests : IClassFixture<MenloWebApplicationFactory>
{
    private readonly MenloWebApplicationFactory _webApplicationFactory;

    public AiServiceTests(MenloWebApplicationFactory webApplicationFactory)
    {
        _webApplicationFactory = webApplicationFactory;
    }

    [Fact]
    public async Task GivenValidText_WhenAnalyzingText()
    {
        // Arrange
        using var scope = _webApplicationFactory.Services.CreateScope();
        var textAnalysisService = scope.ServiceProvider.GetRequiredService<ITextAnalysisService>();
        
        const string testText = """
            Groceries for this week:
            - Milk (2 litres)
            - Bread (brown)
            - Apples (1kg)
            - Chicken breast
            """;

        // Act
        var result = await textAnalysisService.AnalyzeTextAsync(testText);

        // Assert
        ItShouldSuccessfullyAnalyzeText(result);
    }

    [Fact]
    public async Task GivenOllamaServiceUnavailable_WhenAnalyzingText()
    {
        // Arrange
        using var factory = _webApplicationFactory.WithOllamaUnavailable();
        using var scope = factory.Services.CreateScope();
        var textAnalysisService = scope.ServiceProvider.GetRequiredService<ITextAnalysisService>();

        // Act
        var result = await textAnalysisService.AnalyzeTextAsync("Test text");

        // Assert
        ItShouldHandleServiceFailureGracefully(result);
    }

    [Fact]
    public async Task GivenPreviouslyAnalyzedText_WhenAnalyzingAgain()
    {
        // Arrange
        using var scope = _webApplicationFactory.Services.CreateScope();
        var textAnalysisService = scope.ServiceProvider.GetRequiredService<ITextAnalysisService>();
        const string testText = "Test grocery list";

        // Act - First call
        await textAnalysisService.AnalyzeTextAsync(testText);
        
        // Act - Second call (should use cache)
        var result = await textAnalysisService.AnalyzeTextAsync(testText);

        // Assert
        ItShouldReturnCachedResult(result);
    }

    private static void ItShouldSuccessfullyAnalyzeText(CSharpFunctionalExtensions.Result<Menlo.Api.Ai.Models.TextAnalysisResult> result)
    {
        result.IsSuccess.ShouldBeTrue();
        result.Value.ExtractedItems.ShouldNotBeEmpty();
        result.Value.ConfidenceScore.ShouldBeGreaterThan(0);
        result.Value.ProcessedText.ShouldNotBeNullOrEmpty();
    }

    private static void ItShouldHandleServiceFailureGracefully(CSharpFunctionalExtensions.Result<Menlo.Api.Ai.Models.TextAnalysisResult> result)
    {
        // Should either succeed with fallback or fail gracefully
        if (result.IsFailure)
        {
            result.Error.ShouldNotBeNullOrEmpty();
        }
    }

    private static void ItShouldReturnCachedResult(CSharpFunctionalExtensions.Result<Menlo.Api.Ai.Models.TextAnalysisResult> result)
    {
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }
}
```

### 7. Update Program.cs and Configuration ✅

Update `Program.cs` in `Menlo.Api`:

```csharp
using Menlo.Api.Ai;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddOpenApi("menlo-api");
builder.AddPersistence();

// Add AI services
builder.Services.AddMenloAi(builder.Configuration);

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// ...existing code...

app.Run();
```

Update `appsettings.json` to include AI configuration:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.SemanticKernel": "Warning"
    }
  },
  "AllowedHosts": "*",
  "RunMigrationsOnStartup": false,
  "AiService": {
    "TimeoutSeconds": 30,
    "MaxRetries": 3,
    "EnableCaching": true,
    "EnableFallback": true
  }
}
```

### 8. Create AI Fallback Patterns ✅

Create `AiFallbackHandler.cs` in `Menlo.Api/src/Ai/Services/`:

```csharp
using CSharpFunctionalExtensions;
using Menlo.Api.Ai.Abstractions;
using Microsoft.Extensions.Options;

namespace Menlo.Api.Ai.Services;

public class AiFallbackHandler : IAiFallbackHandler
{
    private readonly AiServiceOptions _options;
    private readonly ILogger<AiFallbackHandler> _logger;

    public AiFallbackHandler(IOptions<AiServiceOptions> options, ILogger<AiFallbackHandler> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<T>> ExecuteWithFallbackAsync<T>(
        Func<Task<Result<T>>> aiOperation, 
        Func<Task<Result<T>>> fallbackOperation, 
        CancellationToken cancellationToken = default)
    {
        if (!_options.EnableFallback)
        {
            return await aiOperation();
        }

        try
        {
            // Try AI operation first
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.TimeoutSeconds));
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
            
            var aiResult = await aiOperation();
            
            if (aiResult.IsSuccess)
            {
                return aiResult;
            }

            _logger.LogWarning("AI operation failed: {Error}. Attempting fallback.", aiResult.Error);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw; // Don't handle user cancellation
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI operation threw exception. Attempting fallback.");
        }

        // Execute fallback
        try
        {
            _logger.LogInformation("Executing fallback operation");
            return await fallbackOperation();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallback operation also failed");
            return Result.Failure<T>($"Both AI and fallback operations failed. Last error: {ex.Message}");
        }
    }
}
```

### 9. Documentation & Validation ✅

**AI Architecture Overview:**

- **Local Processing**: All AI processing happens locally via Ollama for privacy and cost control
- **Model Selection**: Phi-4-mini for text processing, Phi-4-vision for image analysis (Microsoft models)
- **Semantic Kernel Integration**: Provides abstraction layer for AI services
- **Caching Strategy**: In-memory caching for AI responses to improve performance
- **Fallback Patterns**: Graceful degradation when AI services are unavailable

**Service Responsibilities:**

| Service                   | Purpose                              | Models Used |
| ------------------------- | ------------------------------------ | ----------- |
| TextAnalysisService       | Extract items from handwritten lists | phi4-mini   |
| ListInterpretationService | Understand list context and intent   | phi4-mini   |
| CostEstimationService     | Estimate costs for list items        | phi4-mini   |
| CategorizationService     | Categorize items for budget mapping  | phi4-mini   |

**Blueberry Muffin Integration:**

- **Embedded Intelligence**: AI services are injected into domain services, not exposed as separate endpoints
- **Non-Intrusive Enhancement**: AI operates in background, providing suggestions without interrupting workflows
- **Contextual Awareness**: AI services understand family patterns and cross-domain relationships
- **Natural Integration**: Users interact with enhanced planning/budget features, not "AI features"

**Performance Considerations:**

- **Response Times**: Target <2 seconds for AI responses via local processing
- **Caching**: 1-hour cache for text analysis, 30-minute sliding cache for other operations
- **Fallback**: Rule-based fallbacks when AI services fail
- **Resource Management**: Ollama container with volume persistence for models

**Testing Strategy:**

- **Integration Tests**: Test AI services with actual Ollama integration
- **Fallback Testing**: Verify graceful degradation when services unavailable
- **Cache Testing**: Validate caching behavior and performance improvements
- **Error Handling**: Test timeout scenarios and retry logic

ℹ️ **Gotcha: Ollama Model Downloads**
> The first time you start the AppHost, Ollama will download the specified models (phi4-mini ~7GB, phi4-vision ~8GB). This can take significant time depending on internet connection. Consider pre-downloading models or using smaller alternatives for development.

ℹ️ **Gotcha: Experimental Packages**
> The Semantic Kernel Ollama connector is experimental and requires `#pragma warning disable SKEXP0070`. Be prepared for potential breaking changes in future releases.

ℹ️ **Gotcha: Local AI Performance**
> Local AI inference performance depends heavily on available system resources (CPU, RAM, GPU). Consider providing configuration options for model selection based on available hardware.

---

For more details, see the [Implementation Roadmap](../../requirements/implementation-roadmap.md) and [Concepts & Terminology](../../explanations/concepts-and-terminology.md).

### 2.2. Ollama Bootstrapping Timeline & Expectations ✅

Understanding the bootstrapping timeline helps set proper expectations for development and deployment:

**First Startup (Fresh Environment):**

```bash
┌─ AppHost Start ─────────────────────────────────────────────────────┐
│ 0:00  ✓ Container launch (Ollama service)                           │
│ 0:05  ✓ Health check passes (API available)                         │
│ 0:06  ⏳ Model download begins (phi4-mini: ~7GB)                    │
│ 0:15  ⏳ Model download 50% complete                                │
│ 0:25  ✓ Model download complete, loading into memory                │
│ 0:26  ✓ Model marked healthy, ready for inference                   │
│ 0:27  ⏳ Second model download begins (phi4-vision: ~14GB)          │
│ 0:45  ✓ All models healthy and ready                                │
└─────────────────────────────────────────────────────────────────────┘
```

**Subsequent Startups (With Persistent Volume):**

```bash
┌─ AppHost Start ─────────────────────────────────────────────────────┐
│ 0:00  ✓ Container launch (Ollama service)                           │
│ 0:03  ✓ Health check passes (API available)                         │
│ 0:04  ✓ Models found in cache, loading from volume                  │
│ 0:08  ✓ All models loaded and ready                                 │
└─────────────────────────────────────────────────────────────────────┘
```

**Resource Requirements:**

| Model            | Download Size | Memory Usage | VRAM (GPU) | CPU Cores |
| ---------------- | ------------- | ------------ | ---------- | --------- |
| **phi-4-mini**   | ~7GB          | 4-6GB        | 3-4GB      | 2-4 cores |
| **phi-4-vision** | ~14GB         | 8-12GB       | 6-8GB      | 4-8 cores |
| **phi-3.5:3.8b** | ~2.2GB        | 2-3GB        | 1-2GB      | 1-2 cores |
| **llama3.2:1b**  | ~1.3GB        | 1-2GB        | 0.5-1GB    | 1 core    |

**Development Recommendations:**

```csharp
// Development: Start with smaller, faster models
var ollama = builder.AddOllama("ollama")
    .WithDataVolume("ollama-models");

// Use smaller model variants for faster development iteration
var phi3Mini = ollama.AddModel("phi3-mini", "phi3.5:3.8b");  // ~2GB vs ~7GB

// Production: Full-featured models when needed
var phi4 = ollama.AddModel("phi4", "microsoft/phi-4:latest");  // Full capabilities
```

**Docker Resource Configuration:**

```json
// .devcontainer/devcontainer.json or Docker Desktop settings
{
  "runArgs": [
    "--memory=16g",      // Ensure enough memory for models
    "--cpus=8",          // Sufficient CPU cores
    "--shm-size=2g"      // Shared memory for model loading
  ]
}
```

**Monitoring Best Practices:**

```csharp
// Add structured logging for model bootstrap tracking
services.Configure<LoggerFilterOptions>(options =>
{
    options.AddFilter("Microsoft.Extensions.Hosting", LogLevel.Information);
    options.AddFilter("Aspire.Hosting", LogLevel.Information);
    options.AddFilter("CommunityToolkit.Aspire", LogLevel.Debug);  // Show download progress
});
```
