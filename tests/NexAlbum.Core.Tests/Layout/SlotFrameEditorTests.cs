using FluentAssertions;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Layout;
using NexAlbum.Core.Projects;
using Xunit;

namespace NexAlbum.Core.Tests.Layout;

public class SlotFrameEditorTests
{
    private static readonly RectMm Canvas = new(0, 0, 306, 306);
    private static readonly DateTimeOffset Now = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

    private static AlbumPage Page(Slot slot) => new() { Id = "p1", Elements = [slot] };

    [Fact]
    public void CornerHandleResizesAndKeepsTheOppositeCorner()
    {
        var r = SlotFrameEditor.Resize(new RectMm(50, 50, 100, 80), FrameHandle.TopLeft, -10, -20, Canvas);

        r.Should().Be(new RectMm(40, 30, 110, 100));
    }

    [Fact]
    public void ResizeCannotCollapseOrLeaveTheCanvas()
    {
        SlotFrameEditor.Resize(new RectMm(50, 50, 100, 80), FrameHandle.Right, -500, 0, Canvas).Width.Should().Be(SlotFrameEditor.MinSizeMm);
        SlotFrameEditor.Resize(new RectMm(50, 50, 100, 80), FrameHandle.Bottom, 0, 900, Canvas).Bottom.Should().Be(306);
        SlotFrameEditor.Move(new RectMm(250, 50, 100, 80), 100, -100, Canvas).Should().Be(new RectMm(206, 0, 100, 80));
    }

    [Fact]
    public void ShiftKeepsTheAspectRatio()
    {
        var r = SlotFrameEditor.Resize(new RectMm(50, 50, 100, 50), FrameHandle.BottomRight, 20, 0, Canvas, keepAspect: true);

        r.Width.Should().BeApproximately(120, 1e-9);
        r.Height.Should().BeApproximately(60, 1e-9);
        r.X.Should().Be(50);
        r.Y.Should().Be(50);
    }

    [Fact]
    public void CornersComeFromTheShape()
    {
        SlotFrameEditor.Corners(new Slot { Id = "s", Bounds = new RectMm(0, 0, 10, 10) }).Should().HaveCount(4);
        SlotFrameEditor.Corners(new Slot { Id = "s", Bounds = new RectMm(0, 0, 10, 10), Shape = SlotShape.Circle }).Should().BeNull();
        SlotFrameEditor.Corners(new Slot { Id = "s", Bounds = new RectMm(0, 0, 10, 10), CornerRadiusMm = 2 }).Should().BeNull();
        SlotFrameEditor.Corners(new Slot { Id = "s", Bounds = new RectMm(0, 0, 10, 10), Shape = SlotShape.Triangle }).Should().HaveCount(3);
    }

    [Fact]
    public void MovingACornerKeepsTheShapeConvex()
    {
        UnitPoint[] square = [new(0, 0), new(1, 0), new(1, 1), new(0, 1)];

        var slanted = SlotFrameEditor.MoveCorner(square, 1, 0.8, 0.2);
        slanted.Should().NotBeNull();
        slanted![1].Should().Be(new UnitPoint(0.8, 0.2));

        SlotFrameEditor.MoveCorner(square, 1, 0.2, 0.8).Should().BeNull("içe katlanan köşe dışbükeyliği bozar");
        SlotFrameEditor.MoveCorner(square, 1, 0.01, 0.01).Should().BeNull("komşu köşeyle çakışır");
        SlotFrameEditor.MoveCorner(square, 2, 1.4, 1.2)![2].Should().Be(new UnitPoint(1, 1), "yuvanın dışına çıkamaz");
    }

    [Fact]
    public void SetFrameWritesBoundsAndPolygon()
    {
        var slot = new Slot { Id = "s", Bounds = new RectMm(20, 20, 100, 100), Placement = new PhotoPlacement { AssetId = "a" } };
        UnitPoint[] trapezoid = [new(0.2, 0), new(0.8, 0), new(1, 1), new(0, 1)];

        var page = SlotFrameEditor.SetFrame(Page(slot), "s", new RectMm(30, 25, 120, 90), trapezoid, Now);

        var changed = page.Slots.Single();
        changed.Bounds.Should().Be(new RectMm(30, 25, 120, 90));
        changed.Shape.Should().Be(SlotShape.Polygon);
        changed.Polygon.Should().Equal(trapezoid);
        changed.Placement.Should().Be(slot.Placement, "fotoğraf yerinde kalır");
        page.UpdatedAt.Should().Be(Now);

        var back = SlotFrameEditor.SetFrame(page, "s", changed.Bounds, [new(0, 0), new(1, 0), new(1, 1), new(0, 1)], Now).Slots.Single();
        back.Shape.Should().Be(SlotShape.Rectangle, "tam dikdörtgen düz dikdörtgen olarak saklanır");
        back.Polygon.Should().BeNull();
    }

    [Fact]
    public void UnchangedFrameReturnsTheSamePage()
    {
        var page = Page(new Slot { Id = "s", Bounds = new RectMm(20, 20, 100, 100) });

        SlotFrameEditor.SetFrame(page, "s", new RectMm(20, 20, 100, 100), null, Now).Should().BeSameAs(page);
    }

    [Fact]
    public void InvalidFramesAreRejected()
    {
        var page = Page(new Slot { Id = "s", Bounds = new RectMm(20, 20, 100, 100) });

        FluentActions.Invoking(() => SlotFrameEditor.SetFrame(page, "s", new RectMm(20, 20, 2, 100), null, Now)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => SlotFrameEditor.SetFrame(page, "s", new RectMm(20, 20, 50, 50), [new(0, 0), new(1, 1), new(1, 0), new(0, 1)], Now))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => SlotFrameEditor.SetFrame(page, "yok", new RectMm(20, 20, 50, 50), null, Now)).Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void SingleEdgeSnapsToThePageCenter()
    {
        var (value, guides) = SnapGuides.SnapLine(151.4, vertical: true, new RectMm(3, 3, 300, 300), null, [], tolerance: 2);

        value.Should().BeApproximately(153, 1e-9);
        guides.Should().ContainSingle().Which.Kind.Should().Be(SnapKind.PageCenter);
    }
}
