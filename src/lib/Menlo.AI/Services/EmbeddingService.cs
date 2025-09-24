using Microsoft.Extensions.AI;
using Menlo.AI.Interfaces;

namespace Menlo.AI.Services;

internal sealed class EmbeddingService : IEmbeddingService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>>? _embeddingGenerator;

    public EmbeddingService(IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator)
    {
        _embeddingGenerator = embeddingGenerator;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (_embeddingGenerator is null)
            throw new InvalidOperationException("Embedding generator is not configured");

        var embeddings = await _embeddingGenerator.GenerateAsync([text], cancellationToken: cancellationToken);
        return embeddings.FirstOrDefault()?.Vector.ToArray() ?? [];
    }

    public async Task<float[][]> GenerateEmbeddingsAsync(IEnumerable<string> texts,
        CancellationToken cancellationToken = default)
    {
        if (_embeddingGenerator is null)
            throw new InvalidOperationException("Embedding generator is not configured");

        var embeddings = await _embeddingGenerator.GenerateAsync(texts, cancellationToken: cancellationToken);
        return embeddings.Select(e => e.Vector.ToArray()).ToArray();
    }
}
