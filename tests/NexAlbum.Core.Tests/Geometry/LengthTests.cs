using FluentAssertions;
using NexAlbum.Core.Geometry;
using Xunit;

namespace NexAlbum.Core.Tests.Geometry;

public class LengthTests
{
    [Theory]
    [InlineData(10, LengthUnit.Millimeter, 10)]
    [InlineData(3, LengthUnit.Centimeter, 30)]
    [InlineData(1, LengthUnit.Inch, 25.4)]
    public void ToMmConvertsPhysicalUnits(double value, LengthUnit unit, double expectedMm)
    {
        Length.ToMm(value, unit).Should().BeApproximately(expectedMm, 1e-9);
        Length.FromMm(expectedMm, unit).Should().BeApproximately(value, 1e-9);
    }

    [Fact]
    public void PixelConversionUsesDpi()
    {
        Length.ToMm(300, LengthUnit.Pixel, 300).Should().BeApproximately(25.4, 1e-9);
        Length.FromMm(25.4, LengthUnit.Pixel, 300).Should().BeApproximately(300, 1e-9);
    }

    [Fact]
    public void PixelConversionWithoutDpiIsRejectedInsteadOfGuessed()
    {
        var act = () => Length.ToMm(100, LengthUnit.Pixel);
        act.Should().Throw<ArgumentException>().WithMessage("*DPI*");

        var back = () => Length.FromMm(100, LengthUnit.Pixel);
        back.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void NonFiniteValuesAreRejected(double value)
    {
        var act = () => Length.ToMm(value, LengthUnit.Millimeter);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UnknownUnitIsRejected()
    {
        var act = () => Length.ToMm(1, (LengthUnit)99);
        act.Should().Throw<ArgumentOutOfRangeException>();

        var back = () => Length.FromMm(1, (LengthUnit)99);
        back.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void EffectiveDpiIsPixelsPerInch()
    {
        // 3000 px, 254 mm (10 inç) üzerine yayılırsa 300 DPI.
        Length.EffectiveDpi(3000, 254).Should().BeApproximately(300, 1e-9);
    }

    [Fact]
    public void EffectiveDpiRequiresPositiveLength()
    {
        var act = () => Length.EffectiveDpi(100, 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void NearlyEqualUsesTolerance()
    {
        Length.NearlyEqual(1, 1 + 1e-9).Should().BeTrue();
        Length.NearlyEqual(1, 1.001).Should().BeFalse();
    }
}
