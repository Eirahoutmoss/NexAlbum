using FluentAssertions;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Profiles;
using NexAlbum.Core.Projects;
using NexAlbum.Core.Validation;
using Xunit;

namespace NexAlbum.Core.Tests.Projects;

public class ProjectPreflightTests
{
    private static readonly PreflightEnvironment AllPresent = new() { IccProfileExists = _ => true, FontAvailable = _ => true };

    private static AlbumProject ProjectWith(params AlbumPage[] pages)
    {
        var project = TestData.Project()
            .AddAssets([TestData.Asset("a1"), TestData.Asset("a2"), TestData.Asset("tiny", 600, 400), TestData.Asset("gone", missing: true)]);
        foreach (var page in pages)
        {
            project = project.AddPage(page);
        }

        return project;
    }

    [Fact]
    public void CleanProjectHasNoIssues()
    {
        var project = ProjectWith(TestData.Page("p1", TestData.Slot("s1", assetId: "a1"), TestData.Text()));
        ProjectPreflight.Check(project, AllPresent).Issues.Should().BeEmpty();
    }

    [Fact]
    public void ProfileProblemsAreIncluded()
    {
        var project = TestData.Project(BuiltInProfiles.Find("builtin-30x30")!);
        var result = ProjectPreflight.Check(project);
        result.Contains("profile.bleed.missing").Should().BeTrue();
        result.HasErrors.Should().BeTrue();
    }

    [Fact]
    public void EmptySlotIsWarned()
    {
        var result = ProjectPreflight.Check(ProjectWith(TestData.Page("p1", TestData.Slot("s1"))), AllPresent);
        var issue = result.Issues.Should().ContainSingle().Subject;
        issue.Code.Should().Be("project.slot.empty");
        issue.Severity.Should().Be(ValidationSeverity.Warning);
        issue.PageId.Should().Be("p1");
        issue.ElementId.Should().Be("s1");
        issue.Suggestion.Should().NotBeNull();
    }

    [Fact]
    public void MissingSourceFileIsAnError()
    {
        var result = ProjectPreflight.Check(ProjectWith(TestData.Page("p1", TestData.Slot("s1", assetId: "gone"))), AllPresent);
        result.Errors.Should().ContainSingle(i => i.Code == "project.photo.source-missing");
    }

    [Fact]
    public void PlacementOfUnknownAssetIsAnError()
    {
        var result = ProjectPreflight.Check(ProjectWith(TestData.Page("p1", TestData.Slot("s1", assetId: "yok"))), AllPresent);
        result.Errors.Should().ContainSingle(i => i.Code == "project.placement.asset-unknown");
    }

    [Fact]
    public void VeryLowResolutionIsAnErrorAndBelowTargetIsAWarning()
    {
        // 600×400 px, 260 mm yuvada ≈ 39 DPI → hata.
        var critical = ProjectPreflight.Check(ProjectWith(TestData.Page("p1", TestData.Slot("s1", assetId: "tiny"))), AllPresent);
        critical.Errors.Should().ContainSingle(i => i.Code == "project.photo.dpi-critical");

        // 6000×4000, 260 mm kare yuva → ≈ 391 DPI; hedef 500 ise uyarı.
        var slot = TestData.Slot("s1", assetId: "a1") with { TargetDpi = 500 };
        var low = ProjectPreflight.Check(ProjectWith(TestData.Page("p1", slot)), AllPresent);
        low.HasErrors.Should().BeFalse();
        low.Warnings.Should().ContainSingle(i => i.Code == "project.photo.dpi-low");
    }

    [Fact]
    public void DuplicatePhotoIsWarned()
    {
        var project = ProjectWith(
            TestData.Page("p1", TestData.Slot("s1", assetId: "a1")),
            TestData.Page("p2", TestData.Slot("s2", assetId: "a1")));
        ProjectPreflight.Check(project, AllPresent).Warnings.Should().ContainSingle(i => i.Code == "project.photo.duplicate" && i.ElementId == "a1");
    }

    [Fact]
    public void PhotoCrossingSafeAreaIsWarnedButFullBleedIsNot()
    {
        var crossing = TestData.Slot("s1", x: 2, y: 20, width: 100, height: 100, assetId: "a1");
        ProjectPreflight.Check(ProjectWith(TestData.Page("p1", crossing)), AllPresent)
            .Contains("project.photo.outside-safe-area").Should().BeTrue();

        var fullBleed = TestData.Slot("s1", x: -3, y: -3, width: 306, height: 306, assetId: "a1");
        var result = ProjectPreflight.Check(ProjectWith(TestData.Page("p1", fullBleed)), AllPresent);
        result.Contains("project.photo.outside-safe-area").Should().BeFalse();
        result.Contains("project.element.outside-page").Should().BeFalse();
    }

    [Fact]
    public void ElementBeyondBleedIsWarned()
    {
        var slot = TestData.Slot("s1", x: 250, y: 20, width: 100, height: 100, assetId: "a1");
        var text = TestData.Text("t1", x: 250, y: 20, width: 100, height: 10);
        var result = ProjectPreflight.Check(ProjectWith(TestData.Page("p1", slot, text)), AllPresent);
        result.Issues.Count(i => i.Code == "project.element.outside-page").Should().Be(2);
        result.Contains("project.text.outside-safe-area").Should().BeTrue();
    }

    [Fact]
    public void MissingFontIsWarned()
    {
        var env = AllPresent with { FontAvailable = _ => false };
        ProjectPreflight.Check(ProjectWith(TestData.Page("p1", TestData.Text())), env)
            .Contains("project.font.missing").Should().BeTrue();
    }

    [Fact]
    public void TextInGutterOfSpreadIsWarned()
    {
        var profile = TestData.CompleteProfile() with { Spread = new SpreadSpec(true), Gutter = new GutterSpec(10) };
        var left = TestData.Page("p1", TestData.Text("t1", x: 280, y: 100, width: 15, height: 10)) with { SpreadPosition = SpreadPosition.Left };
        var right = TestData.Page("p2", TestData.Text("t2", x: 6, y: 100, width: 15, height: 10)) with { SpreadPosition = SpreadPosition.Right };
        var single = TestData.Page("p3", TestData.Text("t3", x: 280, y: 100, width: 15, height: 10));
        var project = TestData.Project(profile).AddPage(left).AddPage(right).AddPage(single);

        var issues = ProjectPreflight.Check(project, AllPresent).Issues.Where(i => i.Code == "project.text.in-gutter");
        issues.Select(i => i.PageId).Should().BeEquivalentTo(new[] { "p1", "p2" });
    }

    [Fact]
    public void LockedPagesAreReportedAsInfo()
    {
        var project = ProjectWith(TestData.Page("p1") with { Locked = true });
        var info = ProjectPreflight.Check(project, AllPresent).Issues.Should().ContainSingle().Subject;
        info.Code.Should().Be("project.pages.locked");
        info.Severity.Should().Be(ValidationSeverity.Info);
    }

    [Fact]
    public void MissingIccProfileIsWarnedWhenColorModeKnown()
    {
        var profile = TestData.CompleteProfile() with { Color = new ColorSpec(ColorMode.Cmyk) };
        ProjectPreflight.Check(TestData.Project(profile), AllPresent).Contains("project.icc.missing").Should().BeTrue();
    }

    [Fact]
    public void PageSizeDifferentFromProfileIsWarned()
    {
        var project = TestData.Project() with { PageSize = new SizeMm(200, 200) };
        ProjectPreflight.Check(project, AllPresent).Contains("project.page-size.profile-mismatch").Should().BeTrue();
    }

    [Fact]
    public void BackgroundImagesAreChecked()
    {
        var missing = TestData.Page("p1") with { Background = new BackgroundFill { Mode = BackgroundMode.Image, ImageAssetId = "gone" } };
        var unknown = TestData.Page("p2", new BackgroundElement { Id = "bg", Fill = new BackgroundFill { Mode = BackgroundMode.Image, ImageAssetId = "yok" } });
        var fine = TestData.Page("p3") with { Background = new BackgroundFill { Mode = BackgroundMode.Image, ImageAssetId = "a1" } };
        var result = ProjectPreflight.Check(ProjectWith(missing, unknown, fine), AllPresent);
        result.Errors.Select(e => e.Code).Should().BeEquivalentTo(new[] { "project.photo.source-missing", "project.placement.asset-unknown" });
    }

    [Fact]
    public void WithoutSafeAreaOrBleedNoAreaWarningsAreInvented()
    {
        var profile = TestData.CompleteProfile() with { Bleed = EdgeSpec.Unknown, SafeArea = EdgeSpec.Unknown };
        var slot = TestData.Slot("s1", x: 0, y: 0, width: 300, height: 300, assetId: "a1");
        var result = ProjectPreflight.Check(TestData.Project(profile).AddAsset(TestData.Asset("a1")).AddPage(TestData.Page("p1", slot)), AllPresent);
        result.Contains("project.photo.outside-safe-area").Should().BeFalse();
        result.Contains("profile.bleed.missing").Should().BeTrue();
    }
}
