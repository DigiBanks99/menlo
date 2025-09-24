namespace Menlo.AI.Configuration;

/// <summary>
/// Configuration settings for AI services
/// </summary>
public sealed class AiSettings
{
    public const string SectionName = "Menlo:AI";

    /// <summary>
    /// Ollama connection name for Aspire integration
    /// </summary>
    public string OllamaConnectionName { get; init; } = "ollama";

    /// <summary>
    /// Text model name for chat completion
    /// </summary>
    public string TextModelName { get; init; } = "phi4-mini";

    /// <summary>
    /// Vision model name for image analysis
    /// </summary>
    public string VisionModelName { get; init; } = "phi4-vision";

    /// <summary>
    /// Embedding model name for vector generation
    /// </summary>
    public string EmbeddingModelName { get; init; } = "phi4-mini";

    /// <summary>
    /// Enable health checks for AI services
    /// </summary>
    public bool EnableHealthChecks { get; init; } = true;

    /// <summary>
    /// Enable telemetry and logging for AI operations
    /// </summary>
    public bool EnableTelemetry { get; init; } = true;
}
