using System.Net;
using System.Text;
using System.Text.Json.Nodes;

namespace Menlo.AI.Tests;

/// <summary>
/// Stands in for llama-server's OpenAI-compatible API: records each request and answers with a fixed chat completion.
/// </summary>
internal sealed class StubLlamaServer : HttpMessageHandler
{
    private const string Completion = """
        {"id":"chatcmpl-1","object":"chat.completion","created":0,"model":"qwen3.5-4b",
         "choices":[{"index":0,"finish_reason":"stop","message":{"role":"assistant","content":"AI service is working"}}]}
        """;

    public List<(Uri Uri, JsonObject Body)> Requests { get; } = [];

    /// <summary>The <c>Authorization</c> header of each request, in order.</summary>
    public List<string?> Authorizations { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string body = request.Content is null ? "{}" : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add((request.RequestUri!, JsonNode.Parse(body)!.AsObject()));
        Authorizations.Add(request.Headers.Authorization?.ToString());

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(Completion, Encoding.UTF8, "application/json")
        };
    }
}
