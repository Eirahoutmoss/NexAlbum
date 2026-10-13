using FluentAssertions;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Layout;
using Xunit;

namespace NexAlbum.Core.Tests.Layout;

public class SnapGuidesTests
{
    private static readonly RectMm Page = new(3, 3, 300, 300);
    private static readonly RectMm Safe = new(13, 13, 280, 280);

    [Fact]
    public void BoxSnapsToThePageCenterOnBothAxes()
    {
        // 100×20 kutu; ortası (152,1 ; 154,4) — sayfa ortası 153.
        var result = SnapGuides.Snap(new RectMm(102.1, 144.4, 100, 20), Page, Safe, [], tolerance: 2);

        result.X.Should().BeApproximately(103, 1e-9);
        result.Y.Should().BeApproximately(143, 1e-9);
        result.Guides.Should().BeEquivalentTo(new[] { new SnapGuide(true, 153, SnapKind.PageCenter), new SnapGuide(false, 153, SnapKind.PageCenter) });
    }

    [Fact]
    public void EdgesSnapToSafeAreaAndOtherElements()
    {
        var photo = new RectMm(40, 60, 120, 80);
        var result = SnapGuides.Snap(new RectMm(14.2, 141, 60, 10), Page, Safe, [photo], tolerance: 2);

        result.X.Should().BeApproximately(13, 1e-9, "sol kenar güvenli alana");
        result.Y.Should().BeApproximately(140, 1e-9, "üst kenar fotoğrafın altına");
        result.Guides.Should().Contain(new SnapGuide(true, 13, SnapKind.SafeArea)).And.Contain(new SnapGuide(false, 140, SnapKind.Element));
    }

    [Fact]
    public void NothingSnapsOutsideTheToleranceOrWithoutASafeArea()
    {
        var free = SnapGuides.Snap(new RectMm(50, 70, 30, 10), Page, null, [], tolerance: 2);
        free.X.Should().Be(50);
        free.Y.Should().Be(70);
        free.Guides.Should().BeEmpty();

        SnapGuides.Snap(new RectMm(14.2, 70, 30, 10), Page, safeArea: null, [], tolerance: 2).X.Should().Be(14.2, "profilde güvenli alan yoksa hedef de yok");
    }

    [Fact]
    public void TheNearestTargetWinsAndCoincidingTargetsAreAllShown()
    {
        var other = new RectMm(13, 200, 50, 10); // sol kenarı güvenli alanla aynı çizgide
        var result = SnapGuides.Snap(new RectMm(12.5, 100, 40, 10), Page, Safe, [other], tolerance: 5);

        result.X.Should().BeApproximately(13, 1e-9, "0,5 mm uzaktaki hedef, 9,5 mm uzaktaki sayfa kenarından önce gelir");
        result.Guides.Where(g => g.Vertical).Should().ContainSingle("aynı konumdaki hedefler tek çizgi").Which.Kind.Should().Be(SnapKind.SafeArea);
    }
}
