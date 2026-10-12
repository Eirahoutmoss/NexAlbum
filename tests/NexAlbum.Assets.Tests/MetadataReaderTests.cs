using FluentAssertions;
using NexAlbum.Assets.Metadata;
using Xunit;

namespace NexAlbum.Assets.Tests;

public class MetadataReaderTests
{
    private static readonly MetadataExtractorReader Reader = new();

    [Fact]
    public void ReadsJpegSizeAndExif()
    {
        using var stream = new MemoryStream(Fixtures.Jpeg);
        var meta = Reader.Read(stream);

        meta.Should().NotBeNull();
        meta!.Width.Should().Be(6000);
        meta.Height.Should().Be(4000);
        meta.Orientation.Should().Be(6);
        meta.CameraModel.Should().Be("TestCam");
        meta.Lens.Should().Be("TestLens");
        meta.Iso.Should().Be(400);
        meta.FocalLength.Should().Be(50);
        meta.CapturedAt.Should().Be(new DateTimeOffset(2024, 9, 15, 14, 30, 0, TimeSpan.FromHours(3)));
    }

    [Fact]
    public void ReadsPngSizeWithoutInventingExif()
    {
        using var stream = new MemoryStream(Fixtures.Png);
        var meta = Reader.Read(stream);

        meta.Should().NotBeNull();
        meta!.Width.Should().Be(1200);
        meta.Height.Should().Be(1800);
        meta.Orientation.Should().BeNull();
        meta.CapturedAt.Should().BeNull();
        meta.CameraModel.Should().BeNull();
        meta.Iso.Should().BeNull();
    }

    [Fact]
    public void UnknownContentYieldsNull()
    {
        using var stream = new MemoryStream("bu bir fotoğraf değil"u8.ToArray());
        Reader.Read(stream).Should().BeNull();
    }
}
