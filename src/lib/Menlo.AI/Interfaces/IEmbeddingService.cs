namespace Menlo.AI.Interfaces;

/// <summary>
/// Text embedding service for vector generation
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generate embeddings for text input
    /// </summary>
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate embeddings for multiple text inputs
    /// </summary>
    Task<float[][]> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);
}
