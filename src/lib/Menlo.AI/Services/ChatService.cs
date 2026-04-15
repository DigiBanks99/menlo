using Menlo.AI.Interfaces;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using System.Runtime.CompilerServices;

namespace Menlo.AI.Services;

public sealed class ChatService(Kernel kernel, IChatClient? chatClient = null) : IChatService
{
    private readonly Kernel _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
    private readonly IChatClient? _chatClient = chatClient;

    public async Task<string> GetResponseAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (_chatClient is not null)
        {
            ChatResponse response = await _chatClient.GetResponseAsync(prompt, cancellationToken: cancellationToken);
            return response.Text ?? string.Empty;
        }

        FunctionResult result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
        return result.GetValue<string>() ?? string.Empty;
    }

    public async IAsyncEnumerable<string> GetStreamingResponseAsync(string prompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_chatClient is not null)
        {
            await foreach (ChatResponseUpdate item in _chatClient.GetStreamingResponseAsync(prompt, cancellationToken: cancellationToken))
            {
                yield return item.Text ?? string.Empty;
            }
        }
        else
        {
            // For Semantic Kernel, we'll return the complete response as a single item
            string result = await GetResponseAsync(prompt, cancellationToken);
            yield return result;
        }
    }
}


