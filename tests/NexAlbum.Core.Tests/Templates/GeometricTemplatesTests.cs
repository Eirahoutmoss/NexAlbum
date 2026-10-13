using FluentAssertions;
using NexAlbum.Core.Common;
using NexAlbum.Core.Design;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Layout;
using NexAlbum.Core.Projects;
using NexAlbum.Core.Templates;
using Xunit;

namespace NexAlbum.Core.Tests.Templates;

/// <summary>Geometrik şablonlar ve biçimli yuvalar (Karar 10 Rev. 1).</summary>
public class GeometricTemplatesTests
{
    private static readonly RectMm Page = new(0, 0, 300, 300);

    [Fact]
    public void CatalogIsRichAndPartOfTheLibrary()
    {
        GeometricTemplates.All.Should().HaveCountGreaterThanOrEqualTo(25);
        GeometricTemplates.All.Should().OnlyContain(t => t.Category == TemplateCategory.Geometric);
        GeometricTemplates.All.Should().OnlyContain(t => t.Slots.Any(s => s.Polygon != null), "her geometrik şablonda en az bir çokgen yuva");
        BuiltInTemplates.All.Should().Contain(GeometricTemplates.All);
        GeometricTemplates.All.Select(t => t.Slots.Count).Distinct().Order().Should().Equal(1, 2, 3, 4, 5, 7);
        BuiltInTemplates.All.Select(t => t.Id).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void DiagonalGapIsAsWideAsStraightGaps()
    {
        // 300 mm: birim 3 mm; boşluk %2 = 6 mm. Hipotenüsler arası dik uzaklık da 6 mm olmalı.
        var slots = BuiltInTemplates.Find("geo-diagonal-2")!.Resolve(Page);
        ConvexShapes.Separation(ConvexShapes.Outline(slots[0]), ConvexShapes.Outline(slots[1])).Should().BeApproximately(6, 1e-6);

        var mixed = BuiltInTemplates.Find("geo-triangles-3")!.Resolve(Page);
        var outlines = mixed.Select(ConvexShapes.Outline).ToList();
        ConvexShapes.Separation(outlines[0], outlines[1]).Should().BeApproximately(6, 1e-6, "karşılıklı üçgenler");
        ConvexShapes.Separation(outlines[1], outlines[2]).Should().BeApproximately(6, 1e-6, "üçgen ile dikdörtgen");
        ConvexShapes.Separation(outlines[2], outlines[3]).Should().BeApproximately(6, 1e-6, "dikdörtgenler");
    }

    [Theory]
    [InlineData(300, 300)]
    [InlineData(300, 200)]
    [InlineData(200, 300)]
    public void ResolvedShapesAreConvexInsideThePageAndApart(double width, double height)
    {
        var page = new RectMm(0, 0, width, height);
        foreach (var template in GeometricTemplates.All)
        {
            var slots = template.Resolve(page, gapPercent: 3, marginPercent: 4);
            var outlines = slots.Select(ConvexShapes.Outline).ToList();
            foreach (var slot in slots)
            {
                page.Contains(slot.Bounds).Should().BeTrue(template.Id);
                if (slot.Polygon is { } polygon)
                {
                    Polygon.IsConvex(polygon).Should().BeTrue($"{template.Id}/{slot.SlotId}");
                }
            }

            for (var i = 0; i < outlines.Count; i++)
            {
                for (var j = i + 1; j < outlines.Count; j++)
                {
                    ConvexShapes.Separation(outlines[i], outlines[j]).Should().BeGreaterThan(0.5, $"{template.Id}: {slots[i].SlotId} ile {slots[j].SlotId} arasında boşluk olmalı");
                }
            }
        }
    }

    [Fact]
    public void FreeStandingShapesDoNotShrink()
    {
        // Tek kemer hiçbir yuvayla kenar paylaşmaz: boşluk değişse de boyutu aynı kalır.
        var arch = BuiltInTemplates.Find("geo-arch-1")!;
        arch.Resolve(Page, gapPercent: 0).Single().Bounds.Should().Be(arch.Resolve(Page, gapPercent: 8).Single().Bounds);
    }

    [Fact]
    public void NonConvexTemplatePolygonIsRejected()
    {
        FluentActions.Invoking(() => new TemplateSlot
        {
            Id = "s",
            Rect = new PercentRect(0, 0, 50, 50),
            Polygon = [new(0, 0), new(1, 0), new(0.5, 0.2), new(1, 1), new(0, 1)],
        }).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void LayoutBuildsPolygonSlotsAndRelayoutFollowsTheGap()
    {
        var project = TestData.Project().AddAssets(Enumerable.Range(1, 5).Select(i => TestData.Asset($"asset-{i:000}")));
        var template = BuiltInTemplates.Find("geo-triangles-3")!;
        var page = PageDesigner.ApplyTemplate(TestData.Page("p1"), template, project, TestData.Now).Page;

        page.Slots.Count(s => s.Shape == SlotShape.Polygon && s.Polygon is { Count: 3 }).Should().Be(2);
        var before = page.Slots.First(s => s.Polygon != null).Bounds;

        var wider = PageDesigner.WithDesign(project, project.Settings.Design with { GapPercent = 6 }).WithPages([page]);
        var relaid = PageDesigner.Relayout(page, wider, BuiltInTemplates.All, TestData.Now);
        var after = relaid.Slots.First(s => s.Polygon != null);
        after.Polygon.Should().NotBeNull().And.HaveCount(3);
        after.Bounds.Should().NotBe(before, "boşluk büyüyünce üçgen hipotenüs boyunca daralır");
    }

    [Fact]
    public void ShapeFramesCutTheSlotAndRemovingThemRestoresTheTemplateShape()
    {
        UnitPoint[] triangle = [new(0, 0), new(1, 0), new(0, 1)];
        var slot = new Slot { Id = "s", Bounds = new RectMm(0, 0, 150, 100), Shape = SlotShape.Polygon, Polygon = triangle };

        var hexagon = FramePresets.Find("hexagon")!.ApplyTo(slot);
        hexagon.Shape.Should().Be(SlotShape.Hexagon);
        hexagon.Polygon.Should().Equal(triangle, "şablon biçimi saklanır");
        SlotOutline.Outer(hexagon).Should().HaveCount(6);

        var rounded = FramePresets.Find("rounded")!.ApplyTo(hexagon);
        rounded.Shape.Should().Be(SlotShape.Polygon, "biçimsiz çerçeve şablon biçimine döner");
        rounded.CornerRadiusMm.Should().Be(0);

        var plain = FramePresets.Find("rounded")!.ApplyTo(new Slot { Id = "r", Bounds = new RectMm(0, 0, 150, 100) });
        plain.Shape.Should().Be(SlotShape.Rounded, "dikdörtgen yuvada yuvarlak köşe eskisi gibi");
    }

    [Theory]
    [InlineData(SlotShape.Triangle, 3)]
    [InlineData(SlotShape.Diamond, 4)]
    [InlineData(SlotShape.Hexagon, 6)]
    [InlineData(SlotShape.Octagon, 8)]
    [InlineData(SlotShape.Slant, 4)]
    public void NamedShapesFillTheirBox(SlotShape shape, int corners)
    {
        var slot = new Slot { Id = "s", Bounds = new RectMm(10, 20, 150, 100), Shape = shape };
        var outer = SlotOutline.Outer(slot)!;
        outer.Should().HaveCount(corners);
        Polygon.Bounds(outer).Should().Be(slot.Bounds);
        SlotOutline.IsPolygonal(slot).Should().BeTrue();
    }

    [Fact]
    public void ArchIsSmoothAndMatShrinksThePhotoEvenly()
    {
        var slot = new Slot
        {
            Id = "s",
            Bounds = new RectMm(0, 0, 100, 160),
            Shape = SlotShape.Arch,
            Mat = new MatSpec(EdgesMm.Uniform(5), RgbaColor.White),
        };
        SlotOutline.Outer(slot)!.Count.Should().BeGreaterThan(SlotOutline.ArchSegments);
        var area = slot.PhotoArea;
        area.X.Should().BeApproximately(5, 0.05, "paspartu kemerin her kenarından eşit");
        area.Y.Should().BeApproximately(5, 0.05);
        area.Width.Should().BeApproximately(90, 0.1);
        area.Bottom.Should().BeApproximately(155, 0.05);
        SlotOutline.Outer(new Slot { Id = "r", Bounds = new RectMm(0, 0, 10, 10) }).Should().BeNull("dikdörtgen doğrudan çizilir");
    }

    [Fact]
    public void AutoLayoutUsesGeometricPagesAsAccentsOnly()
    {
        var project = TestData.Project().AddAssets(Enumerable.Range(1, 120).Select(i => TestData.Asset($"asset-{i:000}", i % 3 == 0 ? 4000 : 6000, i % 3 == 0 ? 6000 : 4000)));
        var result = new AutoLayoutEngine().Layout(LayoutRequest.ForProject(project, BuiltInTemplates.All, new LayoutOptions { Seed = 7 }, TestData.Now));
        var categories = result.Pages.Select(p => BuiltInTemplates.Find(p.TemplateId!)!.Category).ToList();

        for (var i = 0; i < categories.Count; i++)
        {
            if (categories[i] != TemplateCategory.Geometric)
            {
                continue;
            }

            categories.Skip(i + 1).Take(TemplateSelector.GeometricRhythm - 1).Should().NotContain(TemplateCategory.Geometric, $"sayfa {i + 1}'den sonra");
        }

        var preferred = new AutoLayoutEngine().Layout(LayoutRequest.ForProject(
            project,
            BuiltInTemplates.All,
            new LayoutOptions { Seed = 7, Mode = LayoutMode.Controlled, FixedPhotosPerPage = 3, PreferredCategories = [TemplateCategory.Geometric] },
            TestData.Now));
        preferred.Pages.Should().OnlyContain(p => BuiltInTemplates.Find(p.TemplateId!)!.Category == TemplateCategory.Geometric, "tercih edilince ritim sınırı yok");
    }
}
