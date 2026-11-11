using Menlo.AI.Configuration;
using Menlo.AI.Interfaces;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Menlo.AI.Services;

/// <summary>
/// Sends an image and a prompt to the vision model as one user message. Prompts and images are not logged.
/// </summary>
internal sealed class VisionService(
    [FromKeyedServices(VisionService.AgentKey)] AIAgent agent,
    IOptions<AiSettings> settings) : IVisionService
{
    /// <summary>The service key of the vision model's agent.</summary>
    public const string AgentKey = "Menlo.AI.Vision";

    internal const string TranscribePrompt =
        "Transcribe the hand-written text in this image exactly as written. Reply with the text only, keeping its " +
        "line breaks. Write [illegible] for any word you cannot read.";

    private static readonly string[] SupportedMediaTypes = ["image/jpeg", "image/png", "image/webp"];

    private readonly ChatClientAgentRunOptions _runOptions = new(LlamaChatOptions.Create(settings.Value.EnableThinking));
    private readonly TimeSpan _timeout = settings.Value.VisionTimeout;

    public Task<string> AnalyzeImageAsync(byte[] imageData, string mediaType, string prompt,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        return AskAsync(imageData, mediaType, prompt, cancellationToken);
    }

    public Task<string> ExtractTextAsync(byte[] imageData, string mediaType, CancellationToken cancellationToken = default) =>
        AskAsync(imageData, mediaType, TranscribePrompt, cancellationToken);

    private async Task<string> AskAsync(byte[] imageData, string mediaType, string prompt, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(imageData);
        ArgumentNullException.ThrowIfNull(mediaType);
        if (imageData.Length == 0)
        {
            throw new ArgumentException("The image is empty.", nameof(imageData));
        }

        if (!SupportedMediaTypes.Contains(mediaType, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"The media type '{mediaType}' is not supported. Use one of: {string.Join(", ", SupportedMediaTypes)}.",
                nameof(mediaType));
        }

        ChatMessage message = new(ChatRole.User, [new TextContent(prompt), new DataContent(imageData, mediaType.ToLowerInvariant())]);

        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_timeout);

        try
        {
            AgentResponse response = await agent.RunAsync(message, options: _runOptions, cancellationToken: timeout.Token);
            return response.Text ?? string.Empty;
        }
        catch (OperationCanceledException ex) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            // Tell a timeout apart from the caller cancelling, which would otherwise look the same.
            throw new TimeoutException($"The vision model did not answer within {_timeout}.", ex);
        }
    }
}
