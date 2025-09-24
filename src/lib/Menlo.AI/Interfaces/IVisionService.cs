namespace Menlo.AI.Interfaces;

/// <summary>
/// Vision service for image analysis and OCR
/// </summary>
public interface IVisionService
{
    /// <summary>
    /// Analyze image with text prompt
    /// </summary>
    Task<string> AnalyzeImageAsync(byte[] imageData, string prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extract text from image (OCR)
    /// </summary>
    Task<string> ExtractTextAsync(byte[] imageData, CancellationToken cancellationToken = default);
}
