using FluentAssertions;
using NexAlbum.Core.Common;
using NexAlbum.Core.Design;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Layout;
using NexAlbum.Core.Projects;
using NexAlbum.Core.Templates;
using Xunit;

namespace NexAlbum.Core.Tests.Design;

public class PageDesignerTests
{
    private static readonly AutoLayoutEngine Engine = new();

    private static AlbumProject LaidOut(DesignSettings? design = null, int photos = 12)
    {
        var project = TestData.Project().AddAssets(Enumerable.Range(1, photos).Select(i => TestData.Asset($"a{i:00}") with { CapturedAt = TestData.Now.AddMinutes(i) }));
        if (design is not null)
        {
            project = PageDesigner.WithDesign(project, design);
        }

        var result = Engine.Layout(LayoutRequest.ForProject(project, BuiltInTemplates.All, new LayoutOptions { Seed = 1, Mode = LayoutMode.Controlled, FixedPhotosPerPage = 4 }, TestData.Now));
        return project.WithPages(result.Pages);
    }

    [Fact]
    public void AutoLayoutAppliesDesignFrameBackgroundAndGap()
    {
        var design = new DesignSettings
        {
            FrameStyleId = "polaroid",
            GapPercent = 6,
            Background = BackgroundPresets.Find("blush")!.Fill,
        };
        var project = LaidOut(design);

        project.Pages.Should().OnlyContain(p => p.Background == design.Background);
        project.Pages.SelectMany(p => p.Slots).Should().OnlyContain(s => s.StyleId == "polaroid" && s.Mat != null);
        ProjectPreflight.Check(project, new PreflightEnvironment { IccProfileExists = _ => true }).HasErrors.Should().BeFalse();
    }

    [Fact]
    public void BackgroundAssetsAreNeverPlacedAsPhotos()
    {
        var project = TestData.Project()
            .AddAssets([TestData.Asset("foto"), TestData.Asset("fon") with { Role = AssetRole.Background }]);
        var result = Engine.Layout(LayoutRequest.ForProject(project, BuiltInTemplates.All, new LayoutOptions { Seed = 1 }, TestData.Now));
        result.Pages.SelectMany(p => p.Slots).Select(s => s.Placement?.AssetId).OfType<string>().Should().Equal("foto");
    }

    [Fact]
    public void FrameAndBackgroundCanBeAppliedToOnePage()
    {
        var project = LaidOut();
        var page = PageDesigner.ApplyFrame(project.Pages[0], FramePresets.Find("gold")!, TestData.Now.AddHours(1));
        page.Slots.Should().OnlyContain(s => s.StyleId == "gold" && s.Border!.Color == RgbaColor.Parse("#C9A227"));
        page.Slots.Select(s => s.Placement?.AssetId).Should().Equal(project.Pages[0].Slots.Select(s => s.Placement?.AssetId));
        page.UpdatedAt.Should().Be(TestData.Now.AddHours(1));

        var gradient = BackgroundPresets.Find("dawn")!.Fill;
        PageDesigner.ApplyBackground(page, gradient, TestData.Now).Background.Should().Be(gradient);
    }

    [Fact]
    public void BulkChangesSkipLockedPages()
    {
        var project = LaidOut();
        project = project.ReplacePage(project.Pages[1] with { Locked = true });
        var (updated, changed) = PageDesigner.ApplyToUnlocked(project, p => PageDesigner.ApplyFrame(p, FramePresets.Find("oval")!, TestData.Now));

        changed.Should().Be(project.Pages.Count - 1);
        updated.Pages[1].Should().BeSameAs(project.Pages[1]);
        updated.Pages[0].Slots.Should().OnlyContain(s => s.Shape == SlotShape.Circle);
    }

    [Fact]
    public void ChangingTemplateKeepsPhotosStyleAndLock()
    {
        var project = LaidOut(new DesignSettings { FrameStyleId = "rounded" });
        var page = project.Pages[0] with { Locked = true, Notes = "not" };
        page = page.AddElement(TestData.Text("baslik"));
        var photos = page.Slots.Select(s => s.Placement!.AssetId).ToHashSet();

        var result = PageDesigner.ApplyTemplate(page, BuiltInTemplates.Find("gen-feat-top-4-1")!, project, TestData.Now);

        result.RemovedAssetIds.Should().BeEmpty();
        result.Page.TemplateId.Should().Be("gen-feat-top-4-1");
        result.Page.Slots.Select(s => s.Placement!.AssetId).Should().BeEquivalentTo(photos);
        result.Page.Slots.Should().OnlyContain(s => s.StyleId == "rounded");
        result.Page.Locked.Should().BeTrue();
        result.Page.Notes.Should().Be("not");
        result.Page.FindElement("baslik").Should().NotBeNull("metinler korunur");
        result.Page.Id.Should().Be(page.Id);
    }

    [Fact]
    public void SmallerTemplateReportsRemovedPhotosAndLargerLeavesEmptySlots()
    {
        var project = LaidOut();
        var page = project.Pages[0];

        var smaller = PageDesigner.ApplyTemplate(page, BuiltInTemplates.Find("grid-2-side")!, project, TestData.Now);
        smaller.Page.Slots.Should().HaveCount(2);
        smaller.RemovedAssetIds.Should().HaveCount(2);

        var larger = PageDesigner.ApplyTemplate(page, BuiltInTemplates.Find("grid-6")!, project, TestData.Now);
        larger.Page.Slots.Count(s => s.IsEmpty).Should().Be(2);
        larger.RemovedAssetIds.Should().BeEmpty();
    }

    [Fact]
    public void RelayoutFollowsNewGapAndKeepsPhotosAndFrames()
    {
        var project = LaidOut(new DesignSettings { FrameStyleId = "mat-white" });
        var page = PageDesigner.ApplyTemplate(project.Pages[0], BuiltInTemplates.Find("grid-4")!, project, TestData.Now).Page;
        var wider = PageDesigner.WithDesign(project, project.Settings.Design with { GapPercent = 8, MarginPercent = 10 });

        var relaid = PageDesigner.Relayout(page, wider, BuiltInTemplates.All, TestData.Now);

        relaid.Slots.Select(s => s.Placement?.AssetId).Should().Equal(page.Slots.Select(s => s.Placement?.AssetId));
        relaid.Slots.Should().OnlyContain(s => s.StyleId == "mat-white");
        relaid.Slots.First().Bounds.X.Should().BeGreaterThan(page.Slots.First().Bounds.X);

        var unknown = page with { TemplateId = "silinmiş" };
        PageDesigner.Relayout(unknown, wider, BuiltInTemplates.All, TestData.Now).Should().BeSameAs(unknown);
    }

    [Fact]
    public void FullBleedTemplatesUseTheBleedArea()
    {
        var project = TestData.Project();
        PageDesigner.AreaFor(BuiltInTemplates.Find("fullbleed-1")!, project).Should().Be(new RectMm(-3, -3, 306, 306));
        PageDesigner.AreaFor(BuiltInTemplates.Find("grid-4")!, project).Should().Be(project.PageRect);
        PageDesigner.SlotId("page-001", "s1").Should().Be("page-001-s1");
    }
}
