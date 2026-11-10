using Menlo.AI.Configuration;
using Menlo.AI.Interfaces;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;

namespace Menlo.AI.Services;

public sealed class ChatService(AIAgent agent, IOptions<AiSettings> settings) : IChatService
{
    private readonly ChatClientAgentRunOptions _runOptions = new(LlamaChatOptions.Create(settings.Value.EnableThinking));

    public async Task<string> GetResponseAsync(string prompt, CancellationToken cancellationToken = default)
    {
        AgentResponse response = await agent.RunAsync(prompt, options: _runOptions, cancellationToken: cancellationToken);
        return response.Text ?? string.Empty;
    }

    public async IAsyncEnumerable<string> GetStreamingResponseAsync(string prompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(prompt, options: _runOptions, cancellationToken: cancellationToken))
        {
            yield return update.Text ?? string.Empty;
        }
    }
}
