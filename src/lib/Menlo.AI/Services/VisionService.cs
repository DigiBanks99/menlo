using Menlo.AI.Interfaces;

namespace Menlo.AI.Services;

internal sealed class VisionService : IVisionService
{
    // Placeholder implementation - will be implemented in future phases
    public Task<string> AnalyzeImageAsync(byte[] imageData, string prompt, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Vision service will be implemented in future phases");
    }

    public Task<string> ExtractTextAsync(byte[] imageData, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Vision service will be implemented in future phases");
    }
}
