using FluentAssertions;
using NexAlbum.Core.Common;
using Xunit;

namespace NexAlbum.Core.Tests.Geometry;

public class RgbaColorTests
{
    [Theory]
    [InlineData("#FFFFFF", 255, 255, 255, 255)]
    [InlineData("#f5f5f5", 245, 245, 245, 255)]
    [InlineData("#33333380", 51, 51, 51, 128)]
    public void ParsesHex(string hex, int r, int g, int b, int a)
    {
        RgbaColor.Parse(hex).Should().Be(new RgbaColor((byte)r, (byte)g, (byte)b, (byte)a));
    }

    [Theory]
    [InlineData("FFFFFF")]
    [InlineData("#FFF")]
    [InlineData("#GGGGGG")]
    [InlineData("#FFFFFFGG")]
    [InlineData("")]
    public void RejectsInvalidHex(string hex)
    {
        RgbaColor.TryParse(hex, out _).Should().BeFalse();
        var act = () => RgbaColor.Parse(hex);
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void NullIsNotAColor()
    {
        RgbaColor.TryParse(null, out _).Should().BeFalse();
    }

    [Fact]
    public void HexRoundTripsAndOmitsOpaqueAlpha()
    {
        RgbaColor.White.ToHex().Should().Be("#FFFFFF");
        new RgbaColor(1, 2, 3, 4).ToHex().Should().Be("#01020304");
        RgbaColor.Parse(new RgbaColor(10, 20, 30, 40).ToString()).Should().Be(new RgbaColor(10, 20, 30, 40));
    }

    [Fact]
    public void HexIsCultureIndependent()
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("tr-TR");
            RgbaColor.Parse("#ABCDEF").ToHex().Should().Be("#ABCDEF");
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }
}
