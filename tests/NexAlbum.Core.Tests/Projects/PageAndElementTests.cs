using FluentAssertions;
using NexAlbum.Core.Common;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Projects;
using Xunit;

namespace NexAlbum.Core.Tests.Projects;

public class PageAndElementTests
{
    [Fact]
    public void ElementsCanBeAddedReplacedAndRemoved()
    {
        var page = TestData.Page("p")
            .AddElement(TestData.Slot("s1"))
            .AddElement(TestData.Text("t1"));

        page.Elements.Should().HaveCount(2);
        page.Slots.Should().ContainSingle();
        page.FindElement("t1").Should().BeOfType<TextElement>();
        page.FindElement("yok").Should().BeNull();

        var replaced = page.ReplaceElement(TestData.Slot("s1", width: 100));
        ((Slot)replaced.Elements[0]).Bounds.Width.Should().Be(100);

        replaced.RemoveElement("s1").Elements.Should().ContainSingle(e => e.Id == "t1");
        FluentActions.Invoking(() => page.RemoveElement("yok")).Should().Throw<KeyNotFoundException>();
        FluentActions.Invoking(() => page.ReplaceElement(TestData.Slot("yok"))).Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void DuplicateElementIdsAreRejected()
    {
        var page = TestData.Page("p", TestData.Slot("s1"));
        FluentActions.Invoking(() => page.AddElement(TestData.Text("s1"))).Should().Throw<ArgumentException>().WithMessage("*s1*");
    }

    [Fact]
    public void NullElementsAreRejected()
    {
        FluentActions.Invoking(() => TestData.Page("p") with { Elements = [null!] }).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => TestData.Page("p") with { Elements = null! }).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void DrawOrderFollowsZIndexAndKeepsInsertionOrderForTies()
    {
        var page = TestData.Page(
            "p",
            TestData.Slot("top") with { ZIndex = 5 },
            new BackgroundElement { Id = "bg" },
            TestData.Slot("a"),
            TestData.Slot("b"));

        page.ElementsInDrawOrder.Select(e => e.Id).Should().Equal("bg", "a", "b", "top");
    }

    [Fact]
    public void BackgroundElementDefaultsBelowEverything()
    {
        new BackgroundElement { Id = "bg" }.ZIndex.Should().Be(-100);
        new BackgroundElement { Id = "bg", ZIndex = 3 }.ZIndex.Should().Be(3);
    }

    [Fact]
    public void SlotRejectsEmptyBoundsAndInvalidValues()
    {
        FluentActions.Invoking(() => TestData.Slot(width: 0)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => TestData.Slot() with { MinDpi = 0 }).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => TestData.Slot() with { CornerRadiusMm = -1 }).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => TestData.Slot() with { Id = "" }).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SlotKnowsWhetherItIsEmpty()
    {
        TestData.Slot().IsEmpty.Should().BeTrue();
        TestData.Slot(assetId: "a").IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void SlotKeepsStyling()
    {
        var slot = TestData.Slot() with
        {
            Shape = SlotShape.Rounded,
            CornerRadiusMm = 4,
            Border = new BorderSpec(1, RgbaColor.Black),
            Shadow = new ShadowSpec(1, 1, 2, RgbaColor.Black),
            TargetDpi = 300,
            CropMode = CropMode.Contain,
        };

        slot.Border!.WidthMm.Should().Be(1);
        slot.Border.Color.Should().Be(RgbaColor.Black);
        slot.Shadow!.BlurMm.Should().Be(2);
        FluentActions.Invoking(() => new BorderSpec(-1, RgbaColor.Black)).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void PlacementIsNonDestructiveAndValidated()
    {
        var p = new PhotoPlacement { AssetId = "a", OffsetX = 0.5, OffsetY = -0.5, Scale = 1.2, Rotation = 90 };
        p.Crop.Should().Be(NormalizedRect.Full);
        p.Adjustments.IsNone.Should().BeTrue();

        FluentActions.Invoking(() => p with { Scale = 0 }).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => p with { OffsetX = 2 }).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => p with { Rotation = double.NaN }).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => p with { AssetId = " " }).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => p with { Adjustments = null! }).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AdjustmentsAreBounded()
    {
        var adj = new ImageAdjustments { Brightness = 0.2, Contrast = -0.1, Saturation = 1 };
        adj.IsNone.Should().BeFalse();
        FluentActions.Invoking(() => adj with { Brightness = 1.5 }).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void TextElementValidatesItsParts()
    {
        var text = TestData.Text() with { Alignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Middle, LineHeight = 1.2, LetterSpacing = 0.5 };
        text.Font.Weight.Should().Be(400);
        text.Font.Size.Should().Be(12);
        text.Font.Color.Should().Be(RgbaColor.Black);

        FluentActions.Invoking(() => text with { Content = null! }).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => text with { LineHeight = 0 }).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => text with { Bounds = new RectMm(0, 0, 0, 0) }).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => new FontSpec { Family = "Inter", Size = 0 }).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => new FontSpec { Family = "Inter", Weight = 0 }).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => new FontSpec { Family = "" }).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void BackgroundFillDefaultsAndValidation()
    {
        BackgroundFill.White.Color.Should().Be(RgbaColor.White);
        BackgroundFill.Solid(RgbaColor.Black).Color.Should().Be(RgbaColor.Black);
        var gradient = new BackgroundFill { Mode = BackgroundMode.Gradient, Gradient = new GradientSpec(RgbaColor.White, RgbaColor.Black, 45) };
        gradient.Gradient!.AngleDegrees.Should().Be(45);
        FluentActions.Invoking(() => new BackgroundFill { Opacity = 1.1 }).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ClearPlacementsLeavesOtherSlotsAlone()
    {
        var page = TestData.Page("p", TestData.Slot("s1", assetId: "a"), TestData.Slot("s2", assetId: "b"), TestData.Text());
        var cleared = page.ClearPlacementsOf("a");
        cleared.Slots.Select(s => s.Placement?.AssetId).Should().Equal(null, "b");
        cleared.UsesAsset("a").Should().BeFalse();
        cleared.UsesAsset("b").Should().BeTrue();
    }
}
