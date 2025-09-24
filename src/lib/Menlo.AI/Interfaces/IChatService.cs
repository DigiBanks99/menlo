using Microsoft.Extensions.AI;

namespace Menlo.AI.Interfaces;

/// <summary>
/// Core chat service interface for AI text generation
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Generate a chat completion response
    /// </summary>
    Task<string> GetResponseAsync(string prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate a streaming chat completion response
    /// </summary>
    IAsyncEnumerable<string> GetStreamingResponseAsync(string prompt, CancellationToken cancellationToken = default);
}
