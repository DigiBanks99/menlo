using Menlo.Lib.Receipts.Entities;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace Menlo.Api.Receipts;

/// <summary>
/// The photos of a receipt and the budget view it was sent from, as a member sent them.
/// </summary>
/// <param name="Photos">The photos, as sent, in order. Only the first <see cref="Receipt.MaxPhotos"/> are kept.</param>
/// <param name="PhotosSent">How many photos were sent, kept or not.</param>
/// <param name="BudgetView">The budget view it was sent from: a path in Menlo, or null.</param>
public sealed record ReceiptUpload(IReadOnlyList<byte[]> Photos, int PhotosSent, string? BudgetView)
{
    /// <summary>The form field for the budget view the receipt was sent from.</summary>
    public const string BudgetViewField = "budgetView";

    /// <summary>The largest request a receipt may be sent in: three large phone photos, with room to spare.</summary>
    public const long MaxRequestBytes = 64L * 1024 * 1024;

    private const int MaxBudgetViewLength = 500;

    /// <summary>
    /// Reads the photos straight from the multipart request body, so ASP.NET never buffers them to a temporary file.
    /// Files past the third are counted but not kept. Empty file fields (an input left blank) are ignored.
    /// </summary>
    /// <returns>The upload, or null when the request isn't <c>multipart/form-data</c>.</returns>
    public static async Task<ReceiptUpload?> ReadAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (!MediaTypeHeaderValue.TryParse(request.ContentType, out MediaTypeHeaderValue? mediaType)
            || !mediaType.MediaType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase)
            || HeaderUtilities.RemoveQuotes(mediaType.Boundary).Value is not { Length: > 0 } boundary)
        {
            return null;
        }

        MultipartReader reader = new(boundary, request.Body);
        List<byte[]> photos = [];
        int photosSent = 0;
        string? budgetView = null;

        while (await reader.ReadNextSectionAsync(cancellationToken) is { } section)
        {
            if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out ContentDispositionHeaderValue? disposition))
            {
                continue;
            }

            if (disposition.IsFileDisposition())
            {
                // A file input left blank still sends a part, with an empty file name
                if (HeaderUtilities.RemoveQuotes(disposition.FileName).Length == 0 && disposition.FileNameStar.Length == 0)
                {
                    continue;
                }

                photosSent++;
                if (photos.Count < Receipt.MaxPhotos)
                {
                    photos.Add(await ReadAllAsync(section.Body, cancellationToken));
                }
            }
            else if (disposition.IsFormDisposition()
                && string.Equals(HeaderUtilities.RemoveQuotes(disposition.Name).Value, BudgetViewField, StringComparison.Ordinal))
            {
                using StreamReader text = new(section.Body);
                budgetView = LocalPath(await text.ReadToEndAsync(cancellationToken));
            }
        }

        return new ReceiptUpload(photos, photosSent, budgetView);
    }

    // Only a path within Menlo is kept, so closing a receipt can never send someone to another site. A control
    // character is refused anywhere in it: a browser drops tabs and line breaks from a URL, so "/\t/evil.example"
    // would become "//evil.example".
    private static string? LocalPath(string value)
    {
        string path = value.Trim();
        return path.Length is > 0 and <= MaxBudgetViewLength
            && path[0] == '/'
            && !path.Any(char.IsControl)
            && !path.StartsWith("//", StringComparison.Ordinal)
            && !path.StartsWith("/\\", StringComparison.Ordinal)
            ? path
            : null;
    }

    private static async Task<byte[]> ReadAllAsync(Stream body, CancellationToken cancellationToken)
    {
        using MemoryStream buffer = new();
        await body.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }
}
