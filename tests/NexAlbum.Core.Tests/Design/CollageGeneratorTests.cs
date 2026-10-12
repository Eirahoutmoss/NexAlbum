using FluentAssertions;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Templates;
using Xunit;

namespace NexAlbum.Core.Tests.Design;

public class CollageGeneratorTests
{
    private static readonly IReadOnlyList<TemplateDefinition> Generated = CollageGenerator.Generate();

    [Fact]
    public void ProducesARichDeterministicLibrary()
    {
        Generated.Should().HaveCountGreaterThan(200);
        Generated.Select(t => t.Id).Should().OnlyHaveUniqueItems();
        Generated.Should().OnlyContain(t => t.Id.StartsWith("gen-", StringComparison.Ordinal));
        CollageGenerator.Generate().Select(t => t.Id).Should().Equal(Generated.Select(t => t.Id), "aynı sırayla, aynı kimliklerle");
    }

    [Fact]
    public void EveryPhotoCountFromOneToTwelveHasManyChoices()
    {
        for (var n = 1; n <= 12; n++)
        {
            var count = BuiltInTemplates.All.Count(t => t.Slots.Count == n);
            count.Should().BeGreaterThanOrEqualTo(n is 1 or 2 ? 6 : 10, $"{n} fotoğraflı sayfa");
        }
    }

    [Theory]
    [InlineData("gen-rows-2-3")]
    [InlineData("gen-cols-1-2")]
    [InlineData("gen-feat-left-4-1")]
    [InlineData("gen-mosaic-tl-6")]
    [InlineData("gen-center-5")]
    [InlineData("gen-mag-3-steps")]
    [InlineData("gen-rows-3-3-3-edge")]
    [InlineData("gen-single-center")]
    public void KnownIdsStayStable(string id)
    {
        // Bu kimlikler proje dosyalarına yazılır; kaybolmaları kayıtlı albümleri bozar.
        BuiltInTemplates.Find(id).Should().NotBeNull();
    }

    [Fact]
    public void SlotCountMatchesPhotoCountAndCellsTileWithoutOverlap()
    {
        foreach (var t in Generated)
        {
            t.PhotoCount.Min.Should().Be(t.Slots.Count, t.Id);
            var rects = t.Resolve(new RectMm(0, 0, 300, 200));
            for (var i = 0; i < rects.Count; i++)
            {
                for (var j = i + 1; j < rects.Count; j++)
                {
                    rects[i].Bounds.Intersects(rects[j].Bounds).Should().BeFalse($"{t.Id}: {rects[i].SlotId}/{rects[j].SlotId}");
                }
            }
        }
    }

    [Fact]
    public void RowGridsCoverTheWholeArea()
    {
        var grid = BuiltInTemplates.Find("gen-rows-3-3-3-edge")!;
        grid.Category.Should().Be(TemplateCategory.FullBleed);
        var area = grid.Slots.Sum(s => s.Rect.Width * s.Rect.Height);
        area.Should().BeApproximately(100 * 100, 1e-3);
    }

    [Fact]
    public void FeaturedTemplatesMarkTheBigSlotAsPriority()
    {
        var featured = BuiltInTemplates.Find("gen-feat-left-4-1")!;
        featured.Slots.Count(s => s.Priority == 1).Should().Be(1);
        featured.Slots.Single(s => s.Priority == 1).Rect.Width.Should().BeApproximately(62, 1e-6);
    }

    [Fact]
    public void WeightedRowsGiveSinglePhotoMoreRoom()
    {
        var t = BuiltInTemplates.Find("gen-rows-1-3")!;
        var single = t.Slots.First();
        var smalls = t.Slots.Skip(1).ToList();
        single.Rect.Height.Should().BeGreaterThan(smalls[0].Rect.Height);
    }

    [Fact]
    public void CompositionsFollowAestheticRules()
    {
        foreach (var parts in CollageGenerator.Compositions(9))
        {
            parts.Should().OnlyContain(p => p >= 1 && p <= 4);
            parts.Zip(parts.Skip(1)).Should().OnlyContain(x => Math.Abs(x.First - x.Second) <= 2);
        }

        CollageGenerator.Compositions(7).Should().NotContain(p => p.Length > 2 && p.Min() == 1);
    }

    [Fact]
    public void HandcraftedDuplicatesAreRemoved()
    {
        var geometries = BuiltInTemplates.All.Select(CollageGenerator.Geometry).ToList();
        geometries.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void GapAndMarginOverridesApply()
    {
        var grid = BuiltInTemplates.Find("grid-4")!;
        var tight = grid.Resolve(new RectMm(0, 0, 300, 300), gapPercent: 0, marginPercent: 0);
        tight[0].Bounds.Should().Be(new RectMm(0, 0, 150, 150));

        var wide = grid.Resolve(new RectMm(0, 0, 300, 300), gapPercent: 4, marginPercent: 10);
        wide[0].Bounds.Should().Be(new RectMm(30, 30, 114, 114));

        // Kenarsız şablonlar kenar payı ayarından etkilenmez.
        var edge = BuiltInTemplates.Find("fullbleed-1")!.Resolve(new RectMm(0, 0, 300, 300), marginPercent: 10);
        edge[0].Bounds.Should().Be(new RectMm(0, 0, 300, 300));
    }
}
