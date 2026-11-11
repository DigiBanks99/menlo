namespace Menlo.AI.Interfaces;

/// <summary>
/// Vision service: reads images, such as photos of hand-written notes, with the vision model.
/// </summary>
/// <remarks>
/// Accepted media types are <c>image/jpeg</c>, <c>image/png</c> and <c>image/webp</c>. A call can take minutes on the
/// home server, so run it as background work. A call that runs past <c>Menlo:AI:VisionTimeout</c> is cancelled and
/// throws <see cref="TimeoutException"/>.
/// </remarks>
public interface IVisionService
{
    /// <summary>
    /// Ask the vision model about an image.
    /// </summary>
    /// <exception cref="ArgumentException">The image is empty, its media type is not supported, or the prompt is blank.</exception>
    Task<string> AnalyzeImageAsync(byte[] imageData, string mediaType, string prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Transcribe the hand-written text in an image.
    /// </summary>
    /// <exception cref="ArgumentException">The image is empty or its media type is not supported.</exception>
    Task<string> ExtractTextAsync(byte[] imageData, string mediaType, CancellationToken cancellationToken = default);
}
