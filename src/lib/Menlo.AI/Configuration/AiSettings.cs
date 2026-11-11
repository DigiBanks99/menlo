namespace Menlo.AI.Configuration;

/// <summary>
/// Configuration settings for AI services, bound from <c>Menlo:AI</c>.
/// </summary>
public sealed class AiSettings
{
    public const string SectionName = "Menlo:AI";

    /// <summary>
    /// Base URL of the OpenAI-compatible llama.cpp server, including <c>/v1</c>. Required.
    /// </summary>
    public Uri? Endpoint { get; init; }

    /// <summary>
    /// Model name the server serves for chat. Matches a section name in the llama.cpp preset.
    /// </summary>
    public string ChatModel { get; init; } = "qwen3.5-4b";

    /// <summary>
    /// Optional API key for the server. llama.cpp needs none unless started with <c>--api-key</c>.
    /// </summary>
    public string? ApiKey { get; init; }

    /// <summary>
    /// Let the model think before answering. Off by default: each request asks the chat template not to think.
    /// </summary>
    public bool EnableThinking { get; init; }

    /// <summary>
    /// Model name the server serves for vision. A separate key from <see cref="ChatModel"/> so vision can move to another
    /// model later; the model needs its image projector enabled.
    /// </summary>
    public string VisionModel { get; init; } = "qwen3.5-4b";

    /// <summary>
    /// How long one vision call may take before it is cancelled. A photo can take minutes on CPU, so the default matches
    /// llama-server's 600 s <c>--timeout</c> on the home server.
    /// </summary>
    public TimeSpan VisionTimeout { get; init; } = TimeSpan.FromMinutes(10);
}
