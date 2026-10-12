using System.Diagnostics;
using FluentAssertions;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Layout;
using NexAlbum.Core.Projects;
using NexAlbum.Core.Templates;
using Xunit;

namespace NexAlbum.Core.Tests.Layout;

public class AutoLayoutEngineTests
{
    private static readonly AutoLayoutEngine Engine = new();

    /// <summary>Karışık yönlü, çekim zamanı sıralı fotoğraflar.</summary>
    private static List<PhotoAsset> Photos(int count) =>
        [.. Enumerable.Range(1, count).Select(i => TestData.Asset($"a{i:0000}", i % 3 == 0 ? 4000 : 6000, i % 3 == 0 ? 6000 : 4000) with
        {
            CapturedAt = TestData.Now.AddMinutes(i),
            Favorite = i % 10 == 0,
        })];

    private static LayoutRequest Request(IReadOnlyList<PhotoAsset> photos, LayoutOptions? options = null, IReadOnlyList<AlbumPage>? existing = null, IReadOnlyList<TemplateDefinition>? templates = null) => new()
    {
        Photos = photos,
        PageSize = new SizeMm(300, 300),
        Profile = TestData.CompleteProfile(),
        Templates = templates ?? BuiltInTemplates.All,
        ExistingPages = existing ?? [],
        Options = options ?? new LayoutOptions { Seed = 42 },
        Now = TestData.Now,
    };

    private static IEnumerable<Slot> AllSlots(LayoutResult r) => r.Pages.SelectMany(p => p.Slots);

    private static string Signature(LayoutResult r) =>
        string.Join("|", r.Pages.Select(p => p.TemplateId + ":" + string.Join(",", p.Slots.Select(s => s.Placement?.AssetId))));

    [Theory]
    [InlineData(LayoutMode.Quick)]
    [InlineData(LayoutMode.Balanced)]
    [InlineData(LayoutMode.Controlled)]
    public void HundredPhotosArePlacedOnceWithoutStretching(LayoutMode mode)
    {
        var photos = Photos(100);
        var result = Engine.Layout(Request(photos, new LayoutOptions { Mode = mode, Seed = 1 }));

        AllSlots(result).Should().OnlyContain(s => s.CropMode != CropMode.Fill);
        AllSlots(result).Select(s => s.Placement?.AssetId).OfType<string>().Should().BeEquivalentTo(photos.Select(p => p.Id));
        result.Issues.HasErrors.Should().BeFalse();
        result.Issues.Contains("layout.photo.duplicate").Should().BeFalse();
        result.Pages.Select(p => p.Id).Should().OnlyHaveUniqueItems();
        result.Explanations.Should().HaveCount(result.Pages.Count);
        result.Seed.Should().Be(1);
    }

    [Fact]
    public void SameSeedProducesTheSameAlbum()
    {
        var photos = Photos(80);
        var a = Engine.Layout(Request(photos, new LayoutOptions { Seed = 42 }));
        var b = Engine.Layout(Request(photos, new LayoutOptions { Seed = 42 }));
        Signature(a).Should().Be(Signature(b));
    }

    [Fact]
    public void DifferentSeedsCanProduceDifferentAlbums()
    {
        var photos = Photos(80);
        var signatures = Enumerable.Range(1, 5)
            .Select(s => Signature(Engine.Layout(Request(photos, new LayoutOptions { Seed = (ulong)s }))))
            .Distinct();
        signatures.Should().HaveCountGreaterThan(1, "kullanıcı 'farklı sonuç' için tohumu değiştirebilmeli");
    }

    [Fact]
    public void DefaultSeedComesFromProjectId()
    {
        var project = TestData.Project().AddAssets(Photos(20));
        var request = LayoutRequest.ForProject(project, BuiltInTemplates.All, new LayoutOptions(), TestData.Now);
        Engine.Layout(request).Seed.Should().Be(LayoutSeed.FromProjectId(project.Id));
    }

    [Theory]
    [InlineData(LayoutMode.Quick)]
    [InlineData(LayoutMode.Balanced)]
    [InlineData(LayoutMode.Controlled)]
    public void ConsecutivePagesNeverRepeatATemplate(LayoutMode mode)
    {
        var result = Engine.Layout(Request(Photos(120), new LayoutOptions { Mode = mode, Seed = 3 }));
        var ids = result.Pages.Select(p => p.TemplateId).ToList();
        ids.Zip(ids.Skip(1)).Should().OnlyContain(pair => pair.First != pair.Second);
    }

    [Fact]
    public void TemplatesDoNotRepeatWithinThreePagesWhenEnoughTemplatesExist()
    {
        // Tek fotoğraflık 3 yerleşik şablon var: Karar 10 §17.5 penceresi tam sağlanmalı.
        var options = new LayoutOptions { Mode = LayoutMode.Controlled, FixedPhotosPerPage = 1, Seed = 3 };
        var result = Engine.Layout(Request(Photos(30), options));
        for (var i = 0; i + 3 <= result.Pages.Count; i++)
        {
            result.Pages.Skip(i).Take(3).Select(p => p.TemplateId).Should().OnlyHaveUniqueItems($"sayfa {i + 1}–{i + 3}");
        }
    }

    [Fact]
    public void BalancedModeAvoidsSameCategoryBackToBackWhenPossible()
    {
        var result = Engine.Layout(Request(Photos(120), new LayoutOptions { Mode = LayoutMode.Balanced, Seed = 9 }));
        var categories = result.Pages.Select(p => BuiltInTemplates.Find(p.TemplateId!)!.Category).ToList();
        var repeats = categories.Zip(categories.Skip(1)).Count(pair => pair.First == pair.Second);
        repeats.Should().BeLessThan(categories.Count / 3);
    }

    [Fact]
    public void LockedPageIsKeptInPlaceAndItsPhotosAreNotReused()
    {
        var photos = Photos(30);
        var locked = TestData.Page("benim-sayfam", TestData.Slot("s1", assetId: photos[5].Id)) with { Locked = true, TemplateId = "single-1" };
        var other = TestData.Page("eski", TestData.Slot("s2", assetId: photos[6].Id));
        var result = Engine.Layout(Request(photos, existing: [other, locked]));

        result.Pages[1].Should().BeSameAs(locked, "kilitli sayfa özgün sırasında ve değişmeden kalmalı");
        result.Pages.Should().NotContain(p => p.Id == "eski", "kilitsiz sayfalar yeniden üretilir");
        AllSlots(result).Count(s => s.Placement?.AssetId == photos[5].Id).Should().Be(1);
        AllSlots(result).Select(s => s.Placement?.AssetId).OfType<string>().Should().BeEquivalentTo(photos.Select(p => p.Id));
    }

    [Fact]
    public void LockedPageBeyondNewPagesIsAppended()
    {
        var photos = Photos(2);
        var locked = TestData.Page("son", TestData.Slot("s1")) with { Locked = true };
        var existing = Enumerable.Range(0, 5).Select(i => TestData.Page($"p{i}")).Append(locked).ToList();
        var result = Engine.Layout(Request(photos, existing: existing));
        result.Pages[^1].Should().BeSameAs(locked);
    }

    [Fact]
    public void StartingFromScratchIgnoresLocks()
    {
        var photos = Photos(10);
        var locked = TestData.Page("benim", TestData.Slot("s1", assetId: photos[0].Id)) with { Locked = true };
        var result = Engine.Layout(Request(photos, new LayoutOptions { IgnoreLocks = true, Seed = 1 }, [locked]));
        result.Pages.Should().NotContain(p => p.Id == "benim");
        AllSlots(result).Select(s => s.Placement?.AssetId).OfType<string>().Should().Contain(photos[0].Id);
    }

    [Fact]
    public void ReuseCanBeAllowed()
    {
        var photos = Photos(4);
        var locked = TestData.Page("l", TestData.Slot("s1", assetId: photos[0].Id)) with { Locked = true };
        var result = Engine.Layout(Request(photos, new LayoutOptions { AllowPhotoReuse = true, Seed = 1 }, [locked]));
        AllSlots(result).Count(s => s.Placement?.AssetId == photos[0].Id).Should().Be(2);
        result.Issues.Contains("layout.photo.duplicate").Should().BeFalse();
    }

    [Fact]
    public void InsufficientPhotosLeaveVisibleEmptySlots()
    {
        var result = Engine.Layout(Request(Photos(1), new LayoutOptions { AllowedTemplateIds = ["grid-4"], Seed = 1 }));
        result.Pages.Should().ContainSingle();
        result.Pages[0].Slots.Count(s => s.IsEmpty).Should().Be(3);
        result.Issues.Contains("layout.photos.insufficient").Should().BeTrue();
        result.Issues.Contains("layout.slot.empty").Should().BeTrue();
    }

    [Fact]
    public void TargetPageCountIsHonoured()
    {
        var result = Engine.Layout(Request(Photos(12), new LayoutOptions { TargetPageCount = 4, Seed = 1 }));
        result.Pages.Should().HaveCount(4);
        result.Pages.Should().OnlyContain(p => p.Slots.Count() == 3);
        result.Issues.Contains("layout.page-count.mismatch").Should().BeFalse();
    }

    [Fact]
    public void UnreachableTargetIsReported()
    {
        var result = Engine.Layout(Request(Photos(2), new LayoutOptions { TargetPageCount = 5, Seed = 1 }));
        result.Issues.Contains("layout.page-count.mismatch").Should().BeTrue();
    }

    [Fact]
    public void PhotosFollowCaptureTimeUnlessAskedOtherwise()
    {
        var photos = Photos(6);
        photos.Reverse();
        var byTime = Engine.Layout(Request(photos, new LayoutOptions { FixedPhotosPerPage = 1, Mode = LayoutMode.Controlled, Seed = 1 }));
        byTime.Pages[0].Slots.Single().Placement!.AssetId.Should().Be("a0001");

        var asGiven = Engine.Layout(Request(photos, new LayoutOptions { FixedPhotosPerPage = 1, Mode = LayoutMode.Controlled, Order = PhotoOrder.AsGiven, Seed = 1 }));
        asGiven.Pages[0].Slots.Single().Placement!.AssetId.Should().Be("a0006");
    }

    [Fact]
    public void ControlledModeUsesPreferredCategoriesAndFixedDensity()
    {
        var options = new LayoutOptions { Mode = LayoutMode.Controlled, FixedPhotosPerPage = 4, PreferredCategories = [TemplateCategory.Grid], Seed = 1 };
        var result = Engine.Layout(Request(Photos(16), options));
        result.Pages.Should().HaveCount(4);
        result.Pages.Should().OnlyContain(p => p.TemplateId == "grid-4", "tek uygun ızgara; tekrar kuralı gevşetilir");
    }

    [Fact]
    public void FullBleedTemplateUsesTheBleedArea()
    {
        var result = Engine.Layout(Request(Photos(1), new LayoutOptions { AllowedTemplateIds = ["fullbleed-1"], Seed = 1 }));
        result.Pages[0].Slots.Single().Bounds.Should().Be(new RectMm(-3, -3, 306, 306));
        result.Issues.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void FillSlotsAreNeverUsedToStretch()
    {
        var stretchy = BuiltInTemplates.Find("single-1")! with
        {
            Id = "esnek",
            Slots = [new TemplateSlot { Id = "s1", Rect = new PercentRect(0, 0, 100, 100), CropMode = CropMode.Fill }],
        };
        var result = Engine.Layout(Request(Photos(1), templates: [stretchy]));
        result.Pages[0].Slots.Single().CropMode.Should().Be(CropMode.Cover);
    }

    [Fact]
    public void GeneratedPagesCarryTemplateAndTimestamps()
    {
        var result = Engine.Layout(Request(Photos(4)));
        result.Pages.Should().OnlyContain(p => p.TemplateId != null && p.CreatedAt == TestData.Now && !p.Locked);
        result.Pages[0].Id.Should().Be("page-001");
        result.Pages[0].Slots.Should().OnlyContain(s => s.Id.StartsWith("page-001-", StringComparison.Ordinal));
    }

    [Fact]
    public void GeneratedIdsAvoidLockedPageIds()
    {
        var locked = TestData.Page("page-001") with { Locked = true };
        var result = Engine.Layout(Request(Photos(6), existing: [locked]));
        result.Pages.Select(p => p.Id).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AutoLayoutResultPassesProjectPreflight()
    {
        var project = TestData.Project().AddAssets(Photos(40));
        var result = Engine.Layout(LayoutRequest.ForProject(project, BuiltInTemplates.All, new LayoutOptions(), TestData.Now));
        var laidOut = project.WithPages(result.Pages);

        var preflight = ProjectPreflight.Check(laidOut, new PreflightEnvironment { IccProfileExists = _ => true });
        preflight.HasErrors.Should().BeFalse(string.Join("; ", preflight.Errors.Select(e => e.Message)));
        laidOut.UnusedAssets().Should().BeEmpty();
    }

    [Fact]
    public void CancellationIsHonoured()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        FluentActions.Invoking(() => Engine.Layout(Request(Photos(10)), cts.Token)).Should().Throw<OperationCanceledException>();
    }

    [Fact]
    public void InvalidRequestsAreRejected()
    {
        FluentActions.Invoking(() => Engine.Layout(Request(Photos(3), templates: []))).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Engine.Layout(null!)).Should().Throw<ArgumentNullException>();
        Engine.Layout(Request([])).Pages.Should().BeEmpty();
    }

    [Fact]
    public void ThousandPhotosWithinDecision10Target()
    {
        var photos = Photos(1000);
        var watch = Stopwatch.StartNew();
        var result = Engine.Layout(Request(photos, new LayoutOptions { Seed = 7 }));
        watch.Stop();

        watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10), "Karar 10 §16.1: 1000 fotoğraf < 10 sn");
        AllSlots(result).Count(s => !s.IsEmpty).Should().Be(1000);
    }
}
