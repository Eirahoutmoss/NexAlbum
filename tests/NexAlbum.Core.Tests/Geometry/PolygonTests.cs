using FluentAssertions;
using NexAlbum.Core.Geometry;
using Xunit;

namespace NexAlbum.Core.Tests.Geometry;

public class PolygonTests
{
    private static readonly PointMm[] Square = [new(0, 0), new(100, 0), new(100, 100), new(0, 100)];

    [Fact]
    public void InsetMovesEveryEdgeByTheSameDistance()
    {
        Polygon.Offset(Square, 10).Should().Equal(new PointMm(10, 10), new PointMm(90, 10), new PointMm(90, 90), new PointMm(10, 90));
        Polygon.Offset(Square, -5).Should().Equal(new PointMm(-5, -5), new PointMm(105, -5), new PointMm(105, 105), new PointMm(-5, 105));
    }

    [Fact]
    public void InsetDoesNotDependOnWindingOrder()
    {
        PointMm[] clockwise = [new(0, 0), new(100, 0), new(0, 100)];
        PointMm[] counter = [new(0, 0), new(0, 100), new(100, 0)];
        var a = Polygon.Offset(clockwise, 4);
        var b = Polygon.Offset(counter, 4);
        Polygon.Bounds(a).Should().Be(Polygon.Bounds(b));
        Math.Abs(Polygon.SignedArea(a)).Should().BeApproximately(Math.Abs(Polygon.SignedArea(b)), 1e-9);
    }

    [Fact]
    public void DiagonalEdgeIsMovedPerpendicularly()
    {
        // Yalnızca hipotenüs (1. kenar: (100,0) → (0,100)) 5 mm içe: yeni doğru x + y = 100 - 5√2.
        PointMm[] triangle = [new(0, 0), new(100, 0), new(0, 100)];
        var inset = Polygon.Offset(triangle, i => i == 1 ? 5 : 0);
        inset.Should().HaveCount(3);
        foreach (var p in inset.Where(p => p.X > 1e-9 || p.Y > 1e-9))
        {
            (p.X + p.Y).Should().BeApproximately(100 - (5 * Math.Sqrt(2)), 1e-9);
        }
    }

    [Fact]
    public void CollapsingInsetReturnsEmpty()
    {
        Polygon.Offset(Square, 60).Should().BeEmpty("çokgen ters döner");
        Polygon.Offset([new PointMm(0, 0), new PointMm(1, 0)], 0.1).Should().BeEmpty();
    }

    [Fact]
    public void ConvexityIsChecked()
    {
        Polygon.IsConvex([new UnitPoint(0, 0), new UnitPoint(1, 0), new UnitPoint(1, 1), new UnitPoint(0, 1)]).Should().BeTrue();
        Polygon.IsConvex([new UnitPoint(0, 0), new UnitPoint(1, 1), new UnitPoint(1, 0), new UnitPoint(0, 1)]).Should().BeFalse("kendini kesen kelebek");
        Polygon.IsConvex([new UnitPoint(0, 0), new UnitPoint(0.5, 0.5), new UnitPoint(1, 1)]).Should().BeFalse("doğrusal köşeler alan oluşturmaz");
        Polygon.IsConvex([new UnitPoint(0, 0), new UnitPoint(1, 0), new UnitPoint(0.5, 0.2), new UnitPoint(1, 1), new UnitPoint(0, 1)]).Should().BeFalse("içbükey");
    }

    [Fact]
    public void MapAndNormalizeAreInverse()
    {
        var rect = new RectMm(10, 20, 200, 100);
        UnitPoint[] unit = [new(0, 0), new(1, 0.5), new(0.25, 1)];
        var mapped = Polygon.Map(unit, rect);
        mapped[1].Should().Be(new PointMm(210, 70));
        Polygon.Normalize(mapped, rect).Should().Equal(unit);
        Polygon.Bounds(mapped).Should().Be(new RectMm(10, 20, 200, 100));
    }

    [Fact]
    public void UnitPointsStayInRange()
    {
        FluentActions.Invoking(() => new UnitPoint(1.2, 0)).Should().Throw<ArgumentOutOfRangeException>();
        UnitPoint.Clamped(1.0000001, -0.0000001).Should().Be(new UnitPoint(1, 0));
    }
}
