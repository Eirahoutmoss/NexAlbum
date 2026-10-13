using FluentAssertions;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Layout;
using NexAlbum.Core.Projects;
using Xunit;

namespace NexAlbum.Core.Tests.Layout;

public class PlacementPositioningTests
{
    private static readonly RectMm Area = new(0, 0, 100, 100);
    private const double Wide = 1.5;

    /// <summary>Kapla modunda kare yuvadaki 1,5 oranlı fotoğrafın dikdörtgeni (ImageFit kuralı: merkez = yuva merkezi + kaydırma × yuva).</summary>
    private static RectMm Rect(PhotoPlacement p)
    {
        var width = 150 * p.Scale;
        var height = 100 * p.Scale;
        return new RectMm(50 + (p.OffsetX * 100) - (width / 2), 50 + (p.OffsetY * 100) - (height / 2), width, height);
    }

    /// <summary>Yuva içindeki bir noktanın fotoğrafta denk geldiği yer (fotoğraf genişliğinin oranı, 0–1).</summary>
    private static double PhotoXAt(PhotoPlacement p, double slotX)
    {
        var rect = Rect(p);
        return (slotX - rect.X) / rect.Width;
    }

    [Fact]
    public void ZoomKeepsThePointUnderTheCursorInPlace()
    {
        var start = new PhotoPlacement { AssetId = "a" };
        var before = PhotoXAt(start, 80);

        var zoomed = PageEditor.ZoomAt(start, Area, Wide, CropMode.Cover, 2, anchorX: 0.3, anchorY: 0);

        zoomed.Scale.Should().Be(2);
        PhotoXAt(zoomed, 80).Should().BeApproximately(before, 1e-9, "imlecin altındaki ayrıntı yerinde kalır");

        var back = PageEditor.ZoomAt(zoomed, Area, Wide, CropMode.Cover, 0.1, 0.3, 0);
        back.Scale.Should().Be(PageEditor.MinScale);
        back.OffsetY.Should().Be(0);
        Math.Abs(back.OffsetX).Should().BeLessThanOrEqualTo(0.25, "kapla modunda boşluk kalmaz (1,5 oranlı fotoğraf, kare yuva)");
    }

    [Fact]
    public void NudgeMovesWithinTheLimits()
    {
        var start = new PhotoPlacement { AssetId = "a" };
        var right = PageEditor.Nudge(start, Area, Wide, CropMode.Cover, 0.1, 0.1);
        right.OffsetX.Should().BeApproximately(0.1, 1e-9);
        right.OffsetY.Should().Be(0, "dikeyde taşma yok: fotoğraf yuvanın yüksekliğinde");

        PageEditor.Nudge(start, Area, Wide, CropMode.Cover, 5, 0).OffsetX.Should().BeApproximately(0.25, 1e-9);
    }

    [Fact]
    public void AlignShowsTheChosenPartOfThePhoto()
    {
        var zoomed = new PhotoPlacement { AssetId = "a", Scale = 2 };

        var left = PageEditor.Align(zoomed, Area, Wide, CropMode.Cover, -1, -1);
        var rect = Rect(left);
        rect.X.Should().BeApproximately(0, 1e-9, "fotoğrafın sol kenarı yuvanın soluna oturur");
        rect.Y.Should().BeApproximately(0, 1e-9);
        left.Scale.Should().Be(2, "yakınlaştırma korunur");

        var bottomRight = Rect(PageEditor.Align(zoomed, Area, Wide, CropMode.Cover, 1, 1));
        bottomRight.Right.Should().BeApproximately(100, 1e-9);
        bottomRight.Bottom.Should().BeApproximately(100, 1e-9);

        var center = PageEditor.Align(left, Area, Wide, CropMode.Cover, 0, 0);
        center.OffsetX.Should().Be(0);
        center.OffsetY.Should().Be(0);
    }
}
