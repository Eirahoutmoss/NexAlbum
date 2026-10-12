using FluentAssertions;
using NexAlbum.Core.Projects;
using Xunit;

namespace NexAlbum.Core.Tests.Projects;

public class PhotoAssetTests
{
    [Theory]
    [InlineData(null, 6000, 4000)]
    [InlineData(1, 6000, 4000)]
    [InlineData(3, 6000, 4000)]
    [InlineData(6, 4000, 6000)]
    [InlineData(8, 4000, 6000)]
    public void DisplaySizeHonoursExifOrientation(int? orientation, int width, int height)
    {
        var asset = TestData.Asset(orientation: orientation);
        asset.DisplayWidth.Should().Be(width);
        asset.DisplayHeight.Should().Be(height);
        asset.DisplayAspectRatio.Should().BeApproximately((double)width / height, 1e-9);
    }

    [Fact]
    public void InvalidMetadataIsRejected()
    {
        var asset = TestData.Asset();
        FluentActions.Invoking(() => asset with { ExifOrientation = 9 }).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => asset with { PixelWidth = 0 }).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => asset with { FileSize = -1 }).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => asset with { Iso = 0 }).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => asset with { FocalLength = -50 }).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => asset with { SourcePath = "" }).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => asset with { Tags = null! }).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ReferenceIsTheDefaultSource()
    {
        TestData.Asset().SourceType.Should().Be(AssetSourceType.Reference);
    }

    [Fact]
    public void ThumbnailValidatesDimensions()
    {
        var thumb = new ThumbnailInfo("thumbnails/a.jpg", 256, 171);
        thumb.Path.Should().Be("thumbnails/a.jpg");
        thumb.Width.Should().Be(256);
        thumb.Height.Should().Be(171);
        FluentActions.Invoking(() => new ThumbnailInfo("", 1, 1)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => new ThumbnailInfo("a", 0, 1)).Should().Throw<ArgumentOutOfRangeException>();
    }
}
