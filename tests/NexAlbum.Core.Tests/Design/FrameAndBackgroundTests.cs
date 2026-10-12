using FluentAssertions;
using NexAlbum.Core.Common;
using NexAlbum.Core.Design;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Projects;
using Xunit;

namespace NexAlbum.Core.Tests.Design;

public class FrameAndBackgroundTests
{
    [Fact]
    public void PresetIdsAreUniqueAndStable()
    {
        FramePresets.All.Select(f => f.Id).Should().OnlyHaveUniqueItems();
        FramePresets.All.Should().HaveCountGreaterThanOrEqualTo(12);
        FramePresets.All[0].Should().BeSameAs(FramePresets.None);
        FramePresets.Find("polaroid")!.Name.Should().Be("Polaroid");
        FramePresets.Find(null).Should().BeNull();
        FramePresets.Find("yok").Should().BeNull();
    }

    [Fact]
    public void PolaroidMatScalesWithSlotAndHasThickBottom()
    {
        var slot = TestData.Slot(width: 200, height: 100, assetId: "a");
        var framed = FramePresets.Find("polaroid")!.ApplyTo(slot);

        // Kısa kenar 100 mm → %5 = 5 mm, alt %18 = 18 mm.
        framed.Mat!.Edges.Should().Be(new EdgesMm(5, 5, 18, 5));
        framed.Mat.Color.Should().Be(RgbaColor.White);
        framed.PhotoArea.Should().Be(new RectMm(25, 25, 190, 77));
        framed.Shadow.Should().NotBeNull();
        framed.StyleId.Should().Be("polaroid");
        framed.Placement!.AssetId.Should().Be("a", "çerçeve fotoğrafı değiştirmez");
    }

    [Fact]
    public void StrokeIsAbsoluteAndRadiusIsProportional()
    {
        var small = FramePresets.Find("rounded")!.ApplyTo(TestData.Slot(width: 50, height: 50));
        var large = FramePresets.Find("rounded")!.ApplyTo(TestData.Slot(width: 200, height: 200));
        large.CornerRadiusMm.Should().Be(4 * small.CornerRadiusMm);
        small.Shape.Should().Be(SlotShape.Rounded);

        var gold = FramePresets.Find("gold")!;
        gold.ApplyTo(TestData.Slot(width: 50, height: 50)).Border!.WidthMm.Should().Be(gold.ApplyTo(TestData.Slot(width: 200, height: 200)).Border!.WidthMm);
    }

    [Fact]
    public void NoneClearsAllStyling()
    {
        var styled = FramePresets.Find("rounded-shadow")!.ApplyTo(FramePresets.Find("mat-white")!.ApplyTo(TestData.Slot()));
        var cleared = FramePresets.None.ApplyTo(styled);
        cleared.Border.Should().BeNull();
        cleared.Mat.Should().BeNull();
        cleared.Shadow.Should().BeNull();
        cleared.CornerRadiusMm.Should().Be(0);
        cleared.Shape.Should().Be(SlotShape.Rectangle);
        cleared.StyleId.Should().Be(FramePresets.NoneId);
    }

    [Fact]
    public void OvalUsesEllipseWithoutCornerRadius()
    {
        var oval = FramePresets.Find("oval")!.ApplyTo(TestData.Slot());
        oval.Shape.Should().Be(SlotShape.Circle);
        oval.CornerRadiusMm.Should().Be(0);
    }

    [Fact]
    public void ShadowOpacityBecomesAlpha()
    {
        var framed = new FrameStyle { Id = "x", Name = "X", Shadow = new ShadowStyle(1, 2, 0.5) }.ApplyTo(TestData.Slot());
        framed.Shadow!.Color.A.Should().Be(128);
        framed.Shadow.OffsetXMm.Should().Be(1);
        framed.Shadow.BlurMm.Should().Be(2);
    }

    [Fact]
    public void FrameStyleValidates()
    {
        FluentActions.Invoking(() => new FrameStyle { Id = "x", Name = "X", StrokeWidthMm = -1 }).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => new FrameStyle { Id = "x", Name = "X", CornerRadiusPercent = 60 }).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => new FrameStyle { Id = "", Name = "X" }).Should().Throw<ArgumentException>();
        EdgePercents.Uniform(0).IsZero.Should().BeTrue();
    }

    [Fact]
    public void MatReducesEffectiveResolution()
    {
        var slot = TestData.Slot(width: 254, height: 254, assetId: "a");
        var withMat = slot with { Mat = new MatSpec(EdgesMm.Uniform(63.5), RgbaColor.White) };
        PlacementDpi.Calculate(TestData.Asset(), withMat, withMat.Placement!).Should().BeApproximately(2 * PlacementDpi.Calculate(TestData.Asset(), slot, slot.Placement!), 1e-9);

        var swallowed = slot with { Mat = new MatSpec(EdgesMm.Uniform(200), RgbaColor.White) };
        PlacementDpi.Calculate(TestData.Asset(), swallowed, swallowed.Placement!).Should().Be(0);
    }

    [Fact]
    public void BackgroundPresetsCoverSolidsAndGradients()
    {
        BackgroundPresets.Colors.Should().HaveCountGreaterThanOrEqualTo(12).And.OnlyContain(p => p.Fill.Mode == BackgroundMode.Solid && p.Fill.Color != null);
        BackgroundPresets.Gradients.Should().HaveCountGreaterThanOrEqualTo(8).And.OnlyContain(p => p.Fill.Mode == BackgroundMode.Gradient && p.Fill.Gradient != null);
        BackgroundPresets.All.Select(p => p.Id).Should().OnlyHaveUniqueItems();
        BackgroundPresets.Find("blush")!.Fill.Color.Should().Be(RgbaColor.Parse("#F2DCDB"));
        BackgroundPresets.Find("yok").Should().BeNull();
    }

    [Fact]
    public void DesignSettingsValidateAndResolveFrame()
    {
        new DesignSettings().Frame.Should().BeSameAs(FramePresets.None);
        new DesignSettings { FrameStyleId = "polaroid" }.Frame.Id.Should().Be("polaroid");
        new DesignSettings { FrameStyleId = "silinmiş-stil" }.Frame.Should().BeSameAs(FramePresets.None);
        FluentActions.Invoking(() => new DesignSettings { GapPercent = 25 }).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => new DesignSettings { MarginPercent = -1 }).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => new DesignSettings { FrameStyleId = "" }).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void BackgroundAssetsAreNotPhotos()
    {
        var project = TestData.Project()
            .AddAsset(TestData.Asset("foto"))
            .AddAsset(TestData.Asset("fon") with { Role = AssetRole.Background });
        project.Photos.Select(a => a.Id).Should().Equal("foto");
        project.Backgrounds.Select(a => a.Id).Should().Equal("fon");
        project.UnusedAssets().Select(a => a.Id).Should().Equal("foto");
    }
}
