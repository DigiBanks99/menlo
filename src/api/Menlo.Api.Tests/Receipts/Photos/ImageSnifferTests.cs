using Menlo.Api.Receipts.Photos;
using System.Text;

namespace Menlo.Api.Tests.Receipts.Photos;

public sealed class ImageSnifferTests
{
    [Test]
    public void AJpegIsSent()
    {
        ImageKind? kind = ImageSniffer.Sniff(TestPhotos.Jpeg());

        ItShouldBeAJpeg();

        void ItShouldBeAJpeg() => kind.ShouldBe(ImageKind.Jpeg);
    }

    [Test]
    public void APngIsSent()
    {
        ImageKind? kind = ImageSniffer.Sniff(TestPhotos.Png());

        ItShouldBeAPng();

        void ItShouldBeAPng() => kind.ShouldBe(ImageKind.Png);
    }

    [Test]
    public void AWebPIsSent()
    {
        ImageKind? kind = ImageSniffer.Sniff(TestPhotos.WebP());

        ItShouldBeAWebP();

        void ItShouldBeAWebP() => kind.ShouldBe(ImageKind.WebP);
    }

    [Test]
    public void AHeicFromAPhoneIsSent()
    {
        ImageKind? kind = ImageSniffer.Sniff(TestPhotos.Heic());

        ItShouldBeAHeic();

        void ItShouldBeAHeic() => kind.ShouldBe(ImageKind.Heic);
    }

    [Test]
    [Arguments("heix")]
    [Arguments("mif1")]
    [Arguments("hevc")]
    public void AHeicWithAnotherBrandIsSent(string brand)
    {
        byte[] header = [0, 0, 0, 24, .. Encoding.ASCII.GetBytes("ftyp"), .. Encoding.ASCII.GetBytes(brand)];

        ImageKind? kind = ImageSniffer.Sniff(header);

        ItShouldBeAHeic();

        void ItShouldBeAHeic() => kind.ShouldBe(ImageKind.Heic);
    }

    [Test]
    [Arguments("%PDF-1.7 a document")]
    [Arguments("GIF89a an animation")]
    [Arguments("Just some text, honest")]
    [Arguments("....ftypmp42 a video")]
    [Arguments("RIFF....WAVE a sound")]
    [Arguments("RIFF")]
    public void AFileThatIsntAPhotoIsSent(string content)
    {
        ImageKind? kind = ImageSniffer.Sniff(Encoding.ASCII.GetBytes(content));

        ItShouldNotBeAPhoto();

        void ItShouldNotBeAPhoto() => kind.ShouldBeNull();
    }

    [Test]
    public void AnEmptyFileIsSent()
    {
        ImageKind? kind = ImageSniffer.Sniff([]);

        ItShouldNotBeAPhoto();

        void ItShouldNotBeAPhoto() => kind.ShouldBeNull();
    }
}
