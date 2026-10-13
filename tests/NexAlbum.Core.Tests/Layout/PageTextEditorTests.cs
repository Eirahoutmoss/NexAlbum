using FluentAssertions;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Layout;
using NexAlbum.Core.Projects;
using Xunit;

namespace NexAlbum.Core.Tests.Layout;

public class PageTextEditorTests
{
    private static readonly RectMm Page = new(0, 0, 300, 300);
    private static readonly RectMm Canvas = new(-3, -3, 306, 306);
    private static readonly DateTimeOffset Later = TestData.Now.AddMinutes(5);

    [Fact]
    public void PresetsAreCenteredInsideThePageAboveSlots()
    {
        var page = TestData.Page("page-001", TestData.Slot("s1") with { ZIndex = 3 }, TestData.Text("text-001"));

        var (updated, text) = PageTextEditor.Add(page, TextPresets.Find("title")!, Page, "Ayşe & Mert", Later);

        text.Id.Should().Be("text-002", "ilk boş kimlik");
        text.Content.Should().Be("Ayşe & Mert");
        text.Font.Family.Should().Be("Great Vibes");
        text.ZIndex.Should().BeGreaterThanOrEqualTo(PageTextEditor.TextLayer);
        (text.Bounds.X + (text.Bounds.Width / 2)).Should().BeApproximately(150, 1e-6, "yatayda ortalanır");
        text.Bounds.Y.Should().BeGreaterThanOrEqualTo(0);
        text.Bounds.Bottom.Should().BeLessThanOrEqualTo(300);
        updated.FindElement("text-002").Should().Be(text);
        updated.UpdatedAt.Should().Be(Later);
    }

    [Fact]
    public void EveryPresetFitsOnASmallPage()
    {
        var small = new RectMm(0, 0, 100, 70);
        foreach (var preset in TextPresets.All)
        {
            var text = preset.Create("t", null, small, PageTextEditor.TextLayer);
            text.Content.Should().Be(preset.SampleText);
            text.Bounds.X.Should().BeGreaterThanOrEqualTo(0, preset.Id);
            text.Bounds.Y.Should().BeGreaterThanOrEqualTo(0, preset.Id);
            text.Bounds.Right.Should().BeLessThanOrEqualTo(100.0001, preset.Id);
        }
    }

    [Fact]
    public void UpdateMoveResizeAndRemove()
    {
        var page = TestData.Page("page-001", TestData.Text("text-001", x: 20, y: 20, width: 100, height: 15));

        var bigger = PageTextEditor.Update(page, "text-001", t => t with { Font = t.Font with { Size = 24 } }, Later);
        ((TextElement)bigger.FindElement("text-001")!).Font.Size.Should().Be(24);
        PageTextEditor.Update(page, "text-001", t => t, Later).Should().BeSameAs(page, "değişiklik yoksa aynı sayfa");

        var moved = PageTextEditor.Move(page, "text-001", 500, -50, Canvas, Later);
        var b = ((TextElement)moved.FindElement("text-001")!).Bounds;
        b.X.Should().Be(Canvas.Right - 100, "tuvalin içinde kalır");
        b.Y.Should().Be(Canvas.Y);
        PageTextEditor.Move(page, "text-001", 20, 20, Canvas, Later).Should().BeSameAs(page);

        var narrow = PageTextEditor.Resize(page, "text-001", 60, Canvas, Later);
        var n = ((TextElement)narrow.FindElement("text-001")!).Bounds;
        n.Width.Should().Be(60);
        (n.X + (n.Width / 2)).Should().BeApproximately(70, 1e-6, "orta korunur");

        var wide = PageTextEditor.Resize(page, "text-001", 160, Canvas, Later);
        var w = ((TextElement)wide.FindElement("text-001")!).Bounds;
        w.Width.Should().Be(160);
        w.X.Should().Be(Canvas.X, "ortadan genişleyen kutu tuvalin soluna taşarsa içeri itilir");

        PageTextEditor.Remove(page, "text-001", Later).Elements.Should().BeEmpty();
        var missing = () => PageTextEditor.Remove(page, "yok", Later);
        missing.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void FitHeightFollowsLinesAndLineHeight()
    {
        var text = TestData.Text() with { Font = new FontSpec { Family = "Inter", Size = 12 }, LineHeight = 1.5 };
        var fitted = PageTextEditor.FitHeight(text, 3);
        fitted.Bounds.Height.Should().BeApproximately(3 * 12 * TextPreset.MmPerPoint * 1.5, 1e-9);
        fitted.Bounds.Y.Should().Be(text.Bounds.Y);
    }
}
