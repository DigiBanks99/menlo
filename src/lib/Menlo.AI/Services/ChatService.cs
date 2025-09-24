using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Menlo.AI.Interfaces;

namespace Menlo.AI.Services;

public sealed class ChatService : IChatService
{
    private readonly Kernel _kernel;
    private readonly IChatClient? _chatClient;

    public ChatService(Kernel kernel, IChatClient? chatClient = null)
    {
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _chatClient = chatClient;
    }

    public async Task<string> GetResponseAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (_chatClient is not null)
        {
            var response = await _chatClient.GetResponseAsync(prompt, cancellationToken: cancellationToken);
            return response.Text ?? string.Empty;
        }

        var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
        return result.GetValue<string>() ?? string.Empty;
    }

    public async IAsyncEnumerable<string> GetStreamingResponseAsync(string prompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_chatClient is not null)
        {
            await foreach (var item in _chatClient.GetStreamingResponseAsync(prompt, cancellationToken: cancellationToken))
            {
                yield return item.Text ?? string.Empty;
            }
        }
        else
        {
            // For Semantic Kernel, we'll return the complete response as a single item
            var result = await GetResponseAsync(prompt, cancellationToken);
            yield return result;
        }
    }
}
