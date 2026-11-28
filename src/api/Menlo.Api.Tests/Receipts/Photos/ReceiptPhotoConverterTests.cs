using ImageMagick;
using Menlo.Api.Receipts.Photos;

namespace Menlo.Api.Tests.Receipts.Photos;

public sealed class ReceiptPhotoConverterTests
{
    [Test]
    public void AHeicFromAPhoneIsConverted()
    {
        byte[]? jpeg = ReceiptPhotoConverter.ToJpeg(TestPhotos.Heic());

        ItShouldBeAJpeg();
        ItShouldKeepItsSize();

        void ItShouldBeAJpeg() => ImageSniffer.Sniff(jpeg.ShouldNotBeNull()).ShouldBe(ImageKind.Jpeg);

        void ItShouldKeepItsSize()
        {
            MagickImageInfo info = TestPhotos.Describe(jpeg!);
            info.Width.ShouldBe(TestPhotos.HeicWidth);
            info.Height.ShouldBe(TestPhotos.HeicHeight);
        }
    }

    [Test]
    [Arguments("PNG")]
    [Arguments("WebP")]
    public void APhotoInAnotherFormatIsConverted(string format)
    {
        byte[] photo = format == "PNG" ? TestPhotos.Png() : TestPhotos.WebP();

        byte[]? jpeg = ReceiptPhotoConverter.ToJpeg(photo);

        ItShouldBeAJpeg();

        void ItShouldBeAJpeg() => ImageSniffer.Sniff(jpeg.ShouldNotBeNull()).ShouldBe(ImageKind.Jpeg);
    }

    [Test]
    public void APhotoTakenWithThePhoneTurnedIsConverted()
    {
        // Orientation 6: the camera stored it on its side, and it shows turned 90 degrees clockwise
        byte[] photo = TestPhotos.Jpeg(width: 200, height: 100, exifOrientation: 6);

        byte[]? jpeg = ReceiptPhotoConverter.ToJpeg(photo);

        ItShouldBeTurnedTheRightWayUp();
        ItShouldDropTheOrientationAndOtherMetadata();

        void ItShouldBeTurnedTheRightWayUp()
        {
            MagickImageInfo info = TestPhotos.Describe(jpeg.ShouldNotBeNull());
            info.Width.ShouldBe(100u);
            info.Height.ShouldBe(200u);
        }

        void ItShouldDropTheOrientationAndOtherMetadata()
        {
            using MagickImage image = new(jpeg!);
            image.GetExifProfile().ShouldBeNull();
        }
    }

    [Test]
    public void ALargePhotoIsConverted()
    {
        byte[] photo = TestPhotos.Png(width: 3000, height: 2000);

        byte[]? jpeg = ReceiptPhotoConverter.ToJpeg(photo);

        ItShouldBeScaledDownToAboutFourMegapixels();
        ItShouldKeepItsShape();

        void ItShouldBeScaledDownToAboutFourMegapixels()
        {
            MagickImageInfo info = TestPhotos.Describe(jpeg.ShouldNotBeNull());
            ((long)info.Width * info.Height).ShouldBeInRange(3_900_000, ReceiptPhotoConverter.TargetPixels);
        }

        void ItShouldKeepItsShape()
        {
            MagickImageInfo info = TestPhotos.Describe(jpeg!);
            ((double)info.Width / info.Height).ShouldBe(1.5, tolerance: 0.01);
        }
    }

    [Test]
    public void AHugePhotoIsSent()
    {
        // 144 megapixels in a few kilobytes, and within ImageMagick's own width and height limits
        byte[] photo = TestPhotos.BlankPng(width: 12_000, height: 12_000);
        MagickImageInfo claimed = TestPhotos.Describe(photo);

        byte[]? jpeg = ReceiptPhotoConverter.ToJpeg(photo);

        ItShouldBeMoreThanTheLimit();
        ItShouldBeRefused();

        void ItShouldBeMoreThanTheLimit() =>
            ((long)claimed.Width * claimed.Height).ShouldBeGreaterThan(ReceiptPhotoConverter.MaxSourcePixels);

        void ItShouldBeRefused() => jpeg.ShouldBeNull();
    }

    [Test]
    public void ASmallPhotoIsConverted()
    {
        byte[] photo = TestPhotos.Jpeg(width: 1200, height: 1600);

        byte[]? jpeg = ReceiptPhotoConverter.ToJpeg(photo);

        ItShouldKeepItsSize();

        void ItShouldKeepItsSize()
        {
            MagickImageInfo info = TestPhotos.Describe(jpeg.ShouldNotBeNull());
            info.Width.ShouldBe(1200u);
            info.Height.ShouldBe(1600u);
        }
    }

    [Test]
    public void AFileThatOnlyStartsLikeAPhotoIsSent()
    {
        byte[] photo = [0xFF, 0xD8, 0xFF, 0xE0, .. "not really a JPEG"u8.ToArray()];

        byte[]? jpeg = ReceiptPhotoConverter.ToJpeg(photo);

        ItShouldBeRefused();

        void ItShouldBeRefused() => jpeg.ShouldBeNull();
    }

    [Test]
    public void AFileThatIsntAPhotoIsSent()
    {
        byte[]? jpeg = ReceiptPhotoConverter.ToJpeg("%PDF-1.7 a document"u8.ToArray());

        ItShouldBeRefused();

        void ItShouldBeRefused() => jpeg.ShouldBeNull();
    }
}
