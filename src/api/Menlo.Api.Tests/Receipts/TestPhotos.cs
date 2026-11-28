using ImageMagick;
using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace Menlo.Api.Tests.Receipts;

/// <summary>
/// Photos to send as receipts, made on the fly. ImageMagick can't write HEIC, so the HEIC photo is a sample file:
/// <c>rainbow-451x461.heic</c> from libheif's test data (see <c>Samples/README.md</c>).
/// </summary>
internal static class TestPhotos
{
    public const uint HeicWidth = 451;
    public const uint HeicHeight = 461;

    public static byte[] Jpeg(uint width = 40, uint height = 20, ushort? exifOrientation = null)
    {
        using MagickImage image = new(MagickColors.SkyBlue, width, height);
        if (exifOrientation is { } orientation)
        {
            ExifProfile exif = new();
            exif.SetValue(ExifTag.Orientation, orientation);
            image.SetProfile(exif);

            // ImageMagick writes its own orientation into the EXIF profile, so set that too
            image.Orientation = (Orientation)orientation;
        }

        return image.ToByteArray(MagickFormat.Jpeg);
    }

    public static byte[] Png(uint width = 40, uint height = 20)
    {
        using MagickImage image = new(MagickColors.Orange, width, height);
        return image.ToByteArray(MagickFormat.Png);
    }

    /// <summary>
    /// A blank PNG of any size, written by hand at 1 bit a pixel, so even a huge one is only kilobytes and is still a
    /// whole image ImageMagick could decode.
    /// </summary>
    public static byte[] BlankPng(uint width, uint height)
    {
        using MemoryStream png = new();
        png.Write([0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A]);

        // Width, height, bit depth 1, greyscale, deflate, no filter method, not interlaced
        byte[] header = new byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(0, 4), width);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4, 4), height);
        header[8] = 1;
        WriteChunk(png, "IHDR", header);

        // Each row is a filter byte then the row's bits, all zero
        using MemoryStream rows = new();
        using (ZLibStream zlib = new(rows, CompressionLevel.Optimal, leaveOpen: true))
        {
            byte[] row = new byte[1 + ((width + 7) / 8)];
            for (uint y = 0; y < height; y++)
            {
                zlib.Write(row);
            }
        }

        WriteChunk(png, "IDAT", rows.ToArray());
        WriteChunk(png, "IEND", []);
        return png.ToArray();
    }

    public static byte[] WebP(uint width = 40, uint height = 20)
    {
        using MagickImage image = new(MagickColors.Green, width, height);
        return image.ToByteArray(MagickFormat.WebP);
    }

    public static byte[] Heic() =>
        File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Receipts", "Samples", "rainbow-451x461.heic"));

    public static MagickImageInfo Describe(byte[] photo) => new(photo);

    // A PNG chunk: the data's length, the type, the data, then a CRC of the type and data
    private static void WriteChunk(Stream png, string type, byte[] data)
    {
        byte[] typeAndData = [.. Encoding.ASCII.GetBytes(type), .. data];
        Span<byte> number = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(number, (uint)data.Length);
        png.Write(number);
        png.Write(typeAndData);
        BinaryPrimitives.WriteUInt32BigEndian(number, Crc32(typeAndData));
        png.Write(number);
    }

    // The CRC-32 PNG uses (ISO 3309)
    private static uint Crc32(ReadOnlySpan<byte> bytes)
    {
        uint crc = 0xFFFFFFFF;
        foreach (byte b in bytes)
        {
            crc ^= b;
            for (int bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
            }
        }

        return ~crc;
    }
}
