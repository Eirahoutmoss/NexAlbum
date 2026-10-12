using FluentAssertions;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Layout;
using Xunit;

namespace NexAlbum.Core.Tests.Layout;

public class LayoutPrimitivesTests
{
    [Fact]
    public void RandomMatchesSplitMix64ReferenceSoSeedsStayStableAcrossVersions()
    {
        var zero = new DeterministicRandom(0);
        zero.NextUInt64().Should().Be(0xE220A8397B1DCDAFUL);
        zero.NextUInt64().Should().Be(0x6E789E6AA1B965F4UL);
        zero.NextUInt64().Should().Be(0x06C45D188009454FUL);

        var answer = new DeterministicRandom(42);
        answer.NextUInt64().Should().Be(0xBDD732262FEB6E95UL);
    }

    [Fact]
    public void RandomStaysInRange()
    {
        var random = new DeterministicRandom(7);
        for (var i = 0; i < 1000; i++)
        {
            random.Next(3).Should().BeInRange(0, 2);
            random.NextDouble().Should().BeGreaterThanOrEqualTo(0).And.BeLessThan(1);
        }

        FluentActions.Invoking(() => random.Next(0)).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SeedIsDerivedStablyFromProjectId()
    {
        LayoutSeed.FromProjectId(TestData.ProjectId).Should().Be(0x41D4A6CE334A92A7UL);
        LayoutSeed.FromProjectId(Guid.Parse("00000000-0000-0000-0000-000000000001"))
            .Should().NotBe(LayoutSeed.FromProjectId(TestData.ProjectId));
    }

    [Theory]
    [InlineData(6000, 4000, PageOrientation.Landscape)]
    [InlineData(4000, 6000, PageOrientation.Portrait)]
    [InlineData(1000, 1000, PageOrientation.Square)]
    [InlineData(1150, 1000, PageOrientation.Square)]
    [InlineData(1160, 1000, PageOrientation.Landscape)]
    [InlineData(870, 1000, PageOrientation.Square)]
    [InlineData(860, 1000, PageOrientation.Portrait)]
    public void ClassifiesOrientationWithDecision10Thresholds(double w, double h, PageOrientation expected)
    {
        PhotoClassifier.Classify(w, h).Should().Be(expected);
    }

    [Theory]
    [InlineData(100, ResolutionClass.Low)]
    [InlineData(200, ResolutionClass.Medium)]
    [InlineData(300, ResolutionClass.High)]
    [InlineData(350, ResolutionClass.High)]
    [InlineData(400, ResolutionClass.VeryHigh)]
    public void ClassifiesResolution(double dpi, ResolutionClass expected)
    {
        PhotoClassifier.ClassifyDpi(dpi).Should().Be(expected);
    }

    [Fact]
    public void ClassificationUsesExifOrientedSize()
    {
        var c = PhotoClassifier.Classify(TestData.Asset(orientation: 6), 3);
        c.Orientation.Should().Be(PageOrientation.Portrait);
        c.Index.Should().Be(3);
        c.AspectRatio.Should().BeApproximately(4000.0 / 6000, 1e-9);
        c.Favorite.Should().BeFalse();
    }

    [Fact]
    public void OptionsValidate()
    {
        FluentActions.Invoking(() => new LayoutOptions { MinPhotosPerPage = 0 }).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => new LayoutOptions { TargetPageCount = 0 }).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => new LayoutOptions { PreferredCategories = null! }).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => new LayoutOptions { AllowedTemplateIds = null! }).Should().Throw<ArgumentNullException>();
        new LayoutOptions().Mode.Should().Be(LayoutMode.Balanced);
    }
}
