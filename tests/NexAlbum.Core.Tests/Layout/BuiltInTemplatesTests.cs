using FluentAssertions;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Templates;
using Xunit;

namespace NexAlbum.Core.Tests.Layout;

public class BuiltInTemplatesTests
{
    public static TheoryData<double, double> PageSizes() => new() { { 300, 300 }, { 200, 300 }, { 300, 200 }, { 150, 130 } };

    [Fact]
    public void LibraryCoversCommonPageDensities()
    {
        BuiltInTemplates.All.Select(t => t.Id).Should().OnlyHaveUniqueItems();
        BuiltInTemplates.All.Select(t => t.Slots.Count).Distinct().Order().Should().Equal(Enumerable.Range(1, 12));
        BuiltInTemplates.All.Should().HaveCountGreaterThan(200, "kolaj üreticisi geniş bir kütüphane sağlar");
        BuiltInTemplates.All.Should().OnlyContain(t => t.PhotoCount.Min == t.Slots.Count && t.PhotoCount.Max == t.Slots.Count);
        BuiltInTemplates.Find("grid-4")!.Category.Should().Be(TemplateCategory.Grid);
        BuiltInTemplates.Find("yok").Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(PageSizes))]
    public void SlotsStayOnPageAndNeverOverlap(double width, double height)
    {
        var page = RectMm.FromSize(new SizeMm(width, height));
        foreach (var template in BuiltInTemplates.All)
        {
            var slots = template.Resolve(page, gapPercent: 3, marginPercent: 4);
            slots.Should().OnlyContain(s => page.Contains(s.Bounds) && !s.Bounds.IsEmpty, template.Id);
            slots = template.Resolve(page);
            slots.Should().OnlyContain(s => page.Contains(s.Bounds) && !s.Bounds.IsEmpty, template.Id);
            for (var i = 0; i < slots.Count; i++)
            {
                for (var j = i + 1; j < slots.Count; j++)
                {
                    // Çokgen yuvaların sınır kutuları çakışabilir (çapraz bölünmüş iki üçgen); gerçek biçimler çakışmamalı.
                    var overlap = slots[i].Polygon is null && slots[j].Polygon is null
                        ? slots[i].Bounds.Intersects(slots[j].Bounds)
                        : ConvexShapes.Overlap(ConvexShapes.Outline(slots[i]), ConvexShapes.Outline(slots[j]));
                    overlap.Should().BeFalse($"{template.Id}: {slots[i].SlotId} ile {slots[j].SlotId}");
                }
            }
        }
    }

    [Fact]
    public void MarginAndGutterAreAppliedInMillimetres()
    {
        // 300 mm kare: birim 3 mm; kenar %5 = 15 mm, boşluk %2 = 6 mm (her yuvaya 3 mm).
        var slots = BuiltInTemplates.Find("grid-4")!.Resolve(new RectMm(0, 0, 300, 300));
        slots[0].Bounds.Should().Be(new RectMm(15, 15, 132, 132));
        slots[3].Bounds.Should().Be(new RectMm(153, 153, 132, 132));
        (slots[1].Bounds.X - slots[0].Bounds.Right).Should().BeApproximately(6, 1e-9);
    }

    [Fact]
    public void FullBleedTemplateCoversTheWholeArea()
    {
        var area = new RectMm(-3, -3, 306, 306);
        BuiltInTemplates.Find("fullbleed-1")!.Resolve(area).Single().Bounds.Should().Be(area);
    }

    [Fact]
    public void TemplateSlotValidation()
    {
        FluentActions.Invoking(() => new TemplateSlot { Id = "s", Rect = new PercentRect(0, 0, 1, 1), Priority = 0 }).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => new TemplateSlot { Id = "s", Rect = new PercentRect(0, 0, 1, 1), MinAspectRatio = 0 }).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => new TemplateSlot { Id = "s", Rect = new PercentRect(0, 0, 1, 1), RecommendedOrientations = null! }).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => BuiltInTemplates.All[0] with { GutterPercent = 60 }).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => BuiltInTemplates.All[0] with { OuterMarginPercent = -1 }).Should().Throw<ArgumentOutOfRangeException>();
    }
}
