using FluentAssertions;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Profiles;
using NexAlbum.Core.Projects;
using Xunit;

namespace NexAlbum.Core.Tests.Projects;

public class AlbumProjectTests
{
    [Fact]
    public void CreateTakesPageSizeAndSnapshotFromProfile()
    {
        var project = TestData.Project();
        project.Id.Should().Be(TestData.ProjectId);
        project.PageSize.Should().Be(new SizeMm(300, 300));
        project.ProfileId.Should().Be("kullanici-profil-01");
        project.CreatedAt.Should().Be(TestData.Now);
        project.UpdatedAt.Should().Be(TestData.Now);
        project.Pages.Should().BeEmpty();
        project.Settings.Should().Be(ProjectSettings.Default);
    }

    [Fact]
    public void CreateGeneratesTimeOrderedIdWhenNotGiven()
    {
        var project = AlbumProject.Create("x", TestData.CompleteProfile(), TestData.Now, "0.1.0");
        project.Id.Should().NotBe(Guid.Empty);
        project.Id.Version.Should().Be(7);
    }

    [Fact]
    public void CreateRefusesProfileWithoutPageSize()
    {
        var profile = new AlbumProfile { Id = "bos", Name = "Boş" };
        var act = () => AlbumProject.Create("x", profile, TestData.Now, "0.1.0");
        act.Should().Throw<InvalidOperationException>().WithMessage("*sayfa ölçüsü*");
    }

    [Fact]
    public void PagesCanBeAddedInsertedMovedAndRemoved()
    {
        var project = TestData.Project()
            .AddPage(TestData.Page("a"))
            .AddPage(TestData.Page("c"))
            .AddPage(TestData.Page("b"), 1);

        project.Pages.Select(p => p.Id).Should().Equal("a", "b", "c");
        project.IndexOfPage("c").Should().Be(2);
        project.IndexOfPage("yok").Should().Be(-1);

        var moved = project.MovePage("c", 0);
        moved.Pages.Select(p => p.Id).Should().Equal("c", "a", "b");

        var removed = moved.RemovePage("a");
        removed.Pages.Select(p => p.Id).Should().Equal("c", "b");

        project.Pages.Should().HaveCount(3, "önceki örnek değişmemeli");
    }

    [Fact]
    public void ReplacePageKeepsPosition()
    {
        var project = TestData.Project().AddPage(TestData.Page("a")).AddPage(TestData.Page("b"));
        var updated = project.ReplacePage(TestData.Page("a") with { Locked = true });
        updated.Pages[0].Locked.Should().BeTrue();
        updated.FindPage("b").Should().NotBeNull();
        updated.FindPage("yok").Should().BeNull();
    }

    [Fact]
    public void PageOperationsRejectUnknownIdsAndBadIndexes()
    {
        var project = TestData.Project().AddPage(TestData.Page("a"));
        FluentActions.Invoking(() => project.RemovePage("yok")).Should().Throw<KeyNotFoundException>();
        FluentActions.Invoking(() => project.ReplacePage(TestData.Page("yok"))).Should().Throw<KeyNotFoundException>();
        FluentActions.Invoking(() => project.MovePage("a", 1)).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => project.AddPage(TestData.Page("b"), 5)).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void DuplicatePageOrAssetIdsAreRejected()
    {
        var project = TestData.Project().AddPage(TestData.Page("a")).AddAsset(TestData.Asset("x"));
        FluentActions.Invoking(() => project.AddPage(TestData.Page("a"))).Should().Throw<ArgumentException>().WithMessage("*a*");
        FluentActions.Invoking(() => project.AddAsset(TestData.Asset("x"))).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AssetUsageIsDerivedFromPages()
    {
        var project = TestData.Project()
            .AddAssets([TestData.Asset("a1"), TestData.Asset("a2"), TestData.Asset("a3")])
            .AddPage(TestData.Page("p1", TestData.Slot("s1", assetId: "a1")))
            .AddPage(TestData.Page("p2", TestData.Slot("s2", assetId: "a1"), TestData.Slot("s3", assetId: "a2")));

        project.PagesUsingAsset("a1").Should().Equal("p1", "p2");
        project.PagesUsingAsset("a2").Should().Equal("p2");
        project.UnusedAssets().Select(a => a.Id).Should().Equal("a3");
        project.FindAsset("a2").Should().NotBeNull();
        project.FindAsset("yok").Should().BeNull();
    }

    [Fact]
    public void RemovingAssetClearsItsPlacements()
    {
        var project = TestData.Project()
            .AddAsset(TestData.Asset("a1"))
            .AddPage(TestData.Page("p1", TestData.Slot("s1", assetId: "a1")));

        var result = project.RemoveAsset("a1");
        result.Assets.Should().BeEmpty();
        result.Pages[0].Slots.Single().Placement.Should().BeNull();
        FluentActions.Invoking(() => result.RemoveAsset("a1")).Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void ReplaceAssetUpdatesInPlace()
    {
        var project = TestData.Project().AddAsset(TestData.Asset("a1")).AddAsset(TestData.Asset("a2"));
        var updated = project.ReplaceAsset(TestData.Asset("a1", missing: true));
        updated.Assets[0].Missing.Should().BeTrue();
        updated.Assets[1].Id.Should().Be("a2");
        FluentActions.Invoking(() => project.ReplaceAsset(TestData.Asset("yok"))).Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void BackgroundImagesCountAsUsage()
    {
        var page = TestData.Page("p1") with { Background = new BackgroundFill { Mode = BackgroundMode.Image, ImageAssetId = "bg" } };
        var page2 = TestData.Page("p2", new BackgroundElement { Id = "b", Fill = new BackgroundFill { Mode = BackgroundMode.Image, ImageAssetId = "bg2" } });
        var project = TestData.Project().AddAsset(TestData.Asset("bg")).AddAsset(TestData.Asset("bg2")).AddPage(page).AddPage(page2);
        project.PagesUsingAsset("bg").Should().Equal("p1");
        project.PagesUsingAsset("bg2").Should().Equal("p2");
    }

    [Fact]
    public void TouchAndWithProfileReturnNewInstances()
    {
        var project = TestData.Project();
        var later = TestData.Now.AddHours(1);
        project.Touch(later).UpdatedAt.Should().Be(later);

        var profile = TestData.CompleteProfile() with { Version = "1.1.0" };
        project.WithProfile(profile).Profile.Version.Should().Be("1.1.0");
        project.Profile.Version.Should().Be("1.0.0");
    }

    [Fact]
    public void ProjectRejectsInvalidIdentity()
    {
        FluentActions.Invoking(() => TestData.Project() with { Id = Guid.Empty }).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => TestData.Project() with { Name = "" }).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => TestData.Project() with { Settings = null! }).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => TestData.Project() with { Pages = null! }).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void PageRectStartsAtOrigin()
    {
        TestData.Project().PageRect.Should().Be(new RectMm(0, 0, 300, 300));
    }

    [Fact]
    public void SettingsValidateRanges()
    {
        FluentActions.Invoking(() => new ExportSettings { JpegQuality = 0 }).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => new SaveSettings { AutosaveIntervalMinutes = 0 }).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => new UiSettings { Theme = "" }).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => new HistorySettings { MaxSteps = 0 }).Should().Throw<ArgumentOutOfRangeException>();
        new HistorySettings().Enabled.Should().BeTrue();
    }
}
