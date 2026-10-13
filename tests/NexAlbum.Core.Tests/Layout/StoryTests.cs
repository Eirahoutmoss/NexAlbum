using FluentAssertions;
using NexAlbum.Core.Layout;
using NexAlbum.Core.Projects;
using NexAlbum.Core.Templates;
using Xunit;

namespace NexAlbum.Core.Tests.Layout;

public class StoryTests
{
    private static readonly TimeSpan Istanbul = TimeSpan.FromHours(3);

    private static DateTimeOffset At(int day, int hour, int minute = 0) => new(2026, 6, day, hour, minute, 0, Istanbul);

    /// <summary>Bir düğün günü: önceki akşam kına, sabah hazırlık, öğlen gelin alma, ikindi dış çekim, akşam nikâh ve düğün.</summary>
    private static List<PhotoAsset> Wedding()
    {
        var photos = new List<PhotoAsset>();
        void Burst(string prefix, DateTimeOffset start, int count, int everyMinutes = 3)
        {
            for (var i = 0; i < count; i++)
            {
                photos.Add(TestData.Asset($"{prefix}-{i:00}", i % 3 == 0 ? 4000 : 6000, i % 3 == 0 ? 6000 : 4000) with { CapturedAt = start.AddMinutes(i * everyMinutes) });
            }
        }

        Burst("kina", At(12, 20), 6);
        Burst("hazirlik", At(13, 9, 30), 10);
        Burst("gelin", At(13, 13), 8);
        Burst("dis", At(13, 16), 12);
        Burst("nikah", At(13, 19), 5);
        Burst("dugun", At(13, 21), 9);
        photos.Add(TestData.Asset("kayip-1") with { CapturedAt = At(13, 21, 50) });
        photos.Add(TestData.Asset("zamansiz"));
        return photos;
    }

    [Fact]
    public void WeddingDayIsSplitIntoNamedPhases()
    {
        var plan = StoryDetector.Detect(Wedding());

        plan.Chapters.Select(c => c.Name).Should().Equal("Kına Gecesi", "Hazırlık", "Gelin Alma", "Dış Çekim", "Nikâh", "Düğün");
        plan.Chapters[1].Start.Should().Be(At(13, 9, 30));
        plan.Chapters.Select(c => c.Id).Should().OnlyHaveUniqueItems();
        StoryDetector.Detect(Wedding()).Should().BeEquivalentTo(plan, "deterministik");
    }

    [Fact]
    public void SmallBurstsJoinTheirNearestNeighbour()
    {
        var photos = Wedding();
        photos.Add(TestData.Asset("ara-1") with { CapturedAt = At(13, 11, 0) });
        photos.Add(TestData.Asset("ara-2") with { CapturedAt = At(13, 11, 2) });

        var plan = StoryDetector.Detect(photos);

        plan.Chapters.Should().HaveCount(6, "iki fotoğraflık ara çekim hazırlığa katılır");
        plan.ChapterOf(photos.Single(p => p.Id == "ara-2"))!.Name.Should().Be("Hazırlık");
    }

    [Fact]
    public void RepeatedPhasesAreNumberedAndGapIsAdjustable()
    {
        DateTimeOffset[] times = [At(13, 20), At(13, 20, 20), At(13, 21, 30), At(13, 21, 50), At(14, 0, 10), At(14, 0, 30)];
        var photos = times.Select((t, i) => TestData.Asset($"a{i}") with { CapturedAt = t }).ToList();

        var plan = StoryDetector.Detect(photos, new StoryDetectionOptions { Gap = TimeSpan.FromMinutes(45), MinPhotos = 1 });
        plan.Chapters.Select(c => c.Name).Should().Equal(new[] { "Düğün", "Düğün II", "Ertesi Gün" }, "gece yarısından sonrası ertesi gündür");

        StoryDetector.Detect(photos, new StoryDetectionOptions { Gap = TimeSpan.FromHours(2), MinPhotos = 1 }).Chapters.Should().HaveCount(2, "gün değişimi her zaman böler");
        StoryDetector.Detect([TestData.Asset("x")]).HasChapters.Should().BeFalse("zamanı olan fotoğraf yok");
    }

    [Fact]
    public void PlanDistributesRenamesMergesAndSplits()
    {
        var photos = Wedding();
        var plan = StoryDetector.Detect(photos);

        var groups = plan.Distribute(photos);
        groups.Should().HaveCount(7, "6 bölüm + zamansızlar");
        groups[^1].Chapter.Should().BeNull();
        groups[^1].Photos.Should().ContainSingle(p => p.Id == "zamansiz");
        groups.Single(g => g.Chapter?.Name == "Düğün").Photos.Should().Contain(p => p.Id == "kayip-1");
        groups.SelectMany(g => g.Photos).Should().HaveCount(photos.Count);

        var renamed = plan.Rename(plan.Chapters[4].Id, "  Nikâh Töreni ");
        renamed.Chapters[4].Name.Should().Be("Nikâh Töreni");

        var merged = renamed.MergeWithPrevious(renamed.Chapters[5].Id);
        merged.Chapters.Should().HaveCount(5);
        merged.ChapterOf(photos.Single(p => p.Id == "dugun-03"))!.Name.Should().Be("Nikâh Töreni");
        merged.MergeWithPrevious(merged.Chapters[0].Id).Chapters[0].Name.Should().Be("Kına Gecesi", "ilk bölüm sonrakini yutar");

        var split = merged.SplitAt(At(13, 16, 20), "Gün Batımı");
        split.Chapters.Should().HaveCount(6);
        split.ChapterOf(photos.Single(p => p.Id == "dis-11"))!.Name.Should().Be("Gün Batımı");
        split.SplitAt(At(13, 16, 20), "Altın Saat").Chapters.Should().HaveCount(6, "aynı anda ikinci bölüm açılmaz, adı değişir");
    }

    [Fact]
    public void AutoLayoutStartsEveryChapterOnANewPageWithOptionalOpeners()
    {
        var photos = Wedding();
        var project = TestData.Project().AddAssets(photos);
        var plan = StoryDetector.Detect(photos) with { Openers = true };
        var request = LayoutRequest.ForProject(project with { Story = plan }, BuiltInTemplates.All, new LayoutOptions(), TestData.Now);

        var result = new AutoLayoutEngine().Layout(request);

        // Hiçbir sayfa iki bölümden fotoğraf taşımaz.
        var assets = photos.ToDictionary(p => p.Id);
        foreach (var page in result.Pages)
        {
            page.Slots.Select(s => s.Placement?.AssetId).OfType<string>()
                .Select(id => plan.ChapterOf(assets[id])?.Id ?? "yok").Distinct().Should().HaveCountLessThanOrEqualTo(1, page.Id);
        }

        var openers = result.Pages.Where(p => p.Elements.OfType<TextElement>().Any()).ToList();
        openers.Select(p => p.Elements.OfType<TextElement>().Single().Content).Should().Equal(plan.Chapters.Select(c => c.Name));
        openers.Should().OnlyContain(p => p.Slots.Count() == 1);
        openers[0].Elements.OfType<TextElement>().Single().Effect.IsNone.Should().BeFalse("başlık fotoğraf üstünde okunur");
        result.Pages.SelectMany(p => p.Slots).Select(s => s.Placement?.AssetId).OfType<string>().Should().OnlyHaveUniqueItems().And.HaveCount(photos.Count);
        result.Explanations.Should().Contain(e => e.Contains("Hikâye akışı", StringComparison.Ordinal));

        new AutoLayoutEngine().Layout(request).Pages.Should().BeEquivalentTo(result.Pages, "aynı tohum aynı albüm");

        var flat = new AutoLayoutEngine().Layout(LayoutRequest.ForProject(project, BuiltInTemplates.All, new LayoutOptions(), TestData.Now));
        flat.Pages.SelectMany(p => p.Elements.OfType<TextElement>()).Should().BeEmpty("bölüm yokken açılış da yok");
    }
}
