using System.Text;

namespace Menlo.Api.Receipts.Photos;

/// <summary>
/// The kinds of image a receipt photo may be sent as.
/// </summary>
internal enum ImageKind
{
    Jpeg,
    Png,
    WebP,
    Heic,
}

/// <summary>
/// Tells what kind of image a file is from its first bytes. The file name and content type a browser sends are never
/// trusted: a phone's photo is often HEIC with a .jpg name, and anything else could be anything.
/// </summary>
internal static class ImageSniffer
{
    /// <summary>The bytes needed to tell every kind apart.</summary>
    public const int HeaderLength = 12;

    private static ReadOnlySpan<byte> JpegMagic => [0xFF, 0xD8, 0xFF];

    private static ReadOnlySpan<byte> PngMagic => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    // HEIF brands a phone writes for a still photo: HEVC-coded (heic, heix), sequences of them, and the generic image brand
    private static readonly string[] HeicBrands = ["heic", "heix", "heim", "heis", "hevc", "hevx", "hevm", "hevs", "mif1", "msf1"];

    /// <summary>
    /// Returns the kind of image the bytes start with, or null when they aren't one Menlo takes.
    /// </summary>
    public static ImageKind? Sniff(ReadOnlySpan<byte> header)
    {
        if (header.StartsWith(JpegMagic))
        {
            return ImageKind.Jpeg;
        }

        if (header.StartsWith(PngMagic))
        {
            return ImageKind.Png;
        }

        if (header.Length < HeaderLength)
        {
            return null;
        }

        if (Ascii(header[..4]) == "RIFF" && Ascii(header[8..12]) == "WEBP")
        {
            return ImageKind.WebP;
        }

        if (Ascii(header[4..8]) == "ftyp" && HeicBrands.Contains(Ascii(header[8..12])))
        {
            return ImageKind.Heic;
        }

        return null;
    }

    private static string Ascii(ReadOnlySpan<byte> bytes) => Encoding.ASCII.GetString(bytes);
}
