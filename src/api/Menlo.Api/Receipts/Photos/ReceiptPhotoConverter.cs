using ImageMagick;

namespace Menlo.Api.Receipts.Photos;

/// <summary>
/// Turns a photo as a phone sends it into the JPEG Menlo keeps: HEIC is converted, the EXIF orientation is applied
/// (the model server ignores it), it is scaled down to about 4 megapixels, and its metadata, location included, is
/// dropped.
/// </summary>
internal static class ReceiptPhotoConverter
{
    /// <summary>About the most pixels a kept photo has: plenty to read a slip, and small enough to send to the model.</summary>
    public const long TargetPixels = 4_000_000;

    /// <summary>The most pixels a photo may have before it's converted, so a crafted file can't exhaust memory.</summary>
    public const long MaxSourcePixels = 100_000_000;

    /// <summary>The widest or tallest photo ImageMagick may open: wider than any phone camera takes.</summary>
    public const uint MaxSourceSide = 16_384;

    private const uint JpegQuality = 85;

    /// <summary>
    /// Sets ImageMagick's own limits for the whole process, as a second line behind the size check in
    /// <see cref="ToJpeg"/>: a photo wider or taller than <see cref="MaxSourceSide"/> isn't opened, and one with more
    /// than <see cref="MaxSourcePixels"/> isn't held in memory.
    /// </summary>
    public static void LimitResources()
    {
        ResourceLimits.Width = MaxSourceSide;
        ResourceLimits.Height = MaxSourceSide;
        ResourceLimits.Area = MaxSourcePixels;
    }

    /// <summary>
    /// Converts the photo, or returns null when it isn't an image Menlo takes or can't be decoded.
    /// </summary>
    public static byte[]? ToJpeg(byte[] photo)
    {
        if (ImageSniffer.Sniff(photo) is not { } kind)
        {
            return null;
        }

        // Decode only as the sniffed format, so no other ImageMagick coder ever sees the bytes
        MagickReadSettings settings = new()
        {
            Format = kind switch
            {
                ImageKind.Jpeg => MagickFormat.Jpeg,
                ImageKind.Png => MagickFormat.Png,
                ImageKind.WebP => MagickFormat.WebP,
                _ => MagickFormat.Heic,
            }
        };

        try
        {
            MagickImageInfo info = new(photo, settings);
            if ((long)info.Width * info.Height > MaxSourcePixels)
            {
                return null;
            }

            using MagickImage image = new(photo, settings);
            image.AutoOrient();
            ScaleDown(image);
            image.Strip();
            image.Quality = JpegQuality;
            return image.ToByteArray(MagickFormat.Jpeg);
        }
        catch (MagickException)
        {
            return null;
        }
    }

    private static void ScaleDown(MagickImage image)
    {
        long pixels = (long)image.Width * image.Height;
        if (pixels <= TargetPixels)
        {
            return;
        }

        double scale = Math.Sqrt((double)TargetPixels / pixels);
        image.Resize(
            Math.Max(1, (uint)Math.Floor(image.Width * scale)),
            Math.Max(1, (uint)Math.Floor(image.Height * scale)));
    }
}
