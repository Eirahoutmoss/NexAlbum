using FluentAssertions;
using NexAlbum.Core.Geometry;
using Xunit;

namespace NexAlbum.Core.Tests.Geometry;

public class ShapeTests
{
    [Theory]
    [InlineData(300, 300, PageOrientation.Square)]
    [InlineData(300, 200, PageOrientation.Landscape)]
    [InlineData(200, 300, PageOrientation.Portrait)]
    public void SizeDerivesOrientation(double width, double height, PageOrientation expected)
    {
        new SizeMm(width, height).Orientation.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(10, -1)]
    [InlineData(double.NaN, 10)]
    public void SizeMustBePositive(double width, double height)
    {
        var act = () => new SizeMm(width, height);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SizeAspectRatio()
    {
        new SizeMm(300, 200).AspectRatio.Should().BeApproximately(1.5, 1e-9);
    }

    [Fact]
    public void RectEdgesAndEmptiness()
    {
        var r = new RectMm(10, 20, 30, 40);
        r.Right.Should().Be(40);
        r.Bottom.Should().Be(60);
        r.IsEmpty.Should().BeFalse();
        new RectMm(0, 0, 0, 10).IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void RectRejectsNegativeSize()
    {
        var act = () => new RectMm(0, 0, -1, 1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RectAllowsNegativePositionForBleedArea()
    {
        var r = new RectMm(-3, -3, 306, 306);
        r.X.Should().Be(-3);
    }

    [Fact]
    public void ContainsIncludesTouchingEdges()
    {
        var page = new RectMm(0, 0, 300, 300);
        page.Contains(new RectMm(0, 0, 300, 300)).Should().BeTrue();
        page.Contains(new RectMm(10, 10, 50, 50)).Should().BeTrue();
        page.Contains(new RectMm(260, 10, 50, 50)).Should().BeFalse();
    }

    [Fact]
    public void IntersectsIgnoresMereEdgeContact()
    {
        var a = new RectMm(0, 0, 100, 100);
        a.Intersects(new RectMm(50, 50, 100, 100)).Should().BeTrue();
        a.Intersects(new RectMm(100, 0, 50, 50)).Should().BeFalse();
        a.Intersects(new RectMm(200, 200, 10, 10)).Should().BeFalse();
    }

    [Fact]
    public void DeflateAndInflateAreInverse()
    {
        var page = RectMm.FromSize(new SizeMm(300, 300));
        var edges = new EdgesMm(1, 2, 3, 4);

        var safe = page.Deflate(edges);
        safe.Should().Be(new RectMm(4, 1, 294, 296));
        safe.Inflate(edges).Should().Be(page);
    }

    [Fact]
    public void DeflateNeverProducesNegativeSize()
    {
        var small = new RectMm(0, 0, 4, 4);
        var result = small.Deflate(EdgesMm.Uniform(5));
        result.Width.Should().Be(0);
        result.Height.Should().Be(0);
    }

    [Fact]
    public void EdgesSummary()
    {
        var e = new EdgesMm(1, 2, 3, 4);
        e.Min.Should().Be(1);
        e.Max.Should().Be(4);
        e.IsUniform.Should().BeFalse();
        EdgesMm.Uniform(3).IsUniform.Should().BeTrue();
    }

    [Fact]
    public void EdgesRejectNegative()
    {
        var act = () => new EdgesMm(1, -1, 1, 1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void NormalizedRectFullIsWholeImage()
    {
        NormalizedRect.Full.Should().Be(new NormalizedRect(0, 0, 1, 1));
    }

    [Theory]
    [InlineData(0.5, 0, 0.6, 1)]
    [InlineData(0, 0, 0, 1)]
    [InlineData(-0.1, 0, 0.5, 0.5)]
    public void NormalizedRectMustStayInsideImage(double x, double y, double w, double h)
    {
        var act = () => new NormalizedRect(x, y, w, h);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void PercentRectResolvesAgainstArea()
    {
        var rect = new PercentRect(50, 0, 50, 50);
        var resolved = rect.Resolve(new RectMm(10, 10, 200, 100));
        resolved.Should().Be(new RectMm(110, 10, 100, 50));
    }

    [Theory]
    [InlineData(60, 0, 50, 50)]
    [InlineData(0, 0, 0, 10)]
    [InlineData(0, 0, 101, 10)]
    public void PercentRectMustStayInsideArea(double x, double y, double w, double h)
    {
        var act = () => new PercentRect(x, y, w, h);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
