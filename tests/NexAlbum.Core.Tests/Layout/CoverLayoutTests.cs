using FluentAssertions;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Layout;
using NexAlbum.Core.Profiles;
using NexAlbum.Core.Projects;
using NexAlbum.Core.Validation;
using Xunit;

namespace NexAlbum.Core.Tests.Layout;

public class CoverLayoutTests
{
    private static AlbumProfile WithCover(CoverSpec? cover) => TestData.CompleteProfile() with { Cover = cover };

    private static AlbumProject Project(CoverSpec cover) =>
        AlbumProject.Create("Yaz 2026", WithCover(cover), TestData.Now, "0.1.0", TestData.ProjectId)
            .AddAssets(
            [
                TestData.Asset("dikey", 4000, 6000),
                TestData.Asset("yatay", 6000, 4000),
                TestData.Asset("favori", 4000, 6000) with { Favorite = true },
            ]);

    [Fact]
    public void SheetIsBackSpineFront()
    {
        var g = CoverLayout.Geometry(WithCover(new CoverSpec(CoverType.Hardcover, new SizeMm(310, 310), null, new SpineSpec(18))), out var issues)!;

        g.Sheet.Should().Be(new SizeMm(638, 310));
        g.Back.Should().Be(new RectMm(0, 0, 310, 310));
        g.Spine.Should().Be(new RectMm(310, 0, 18, 310));
        g.Front.Should().Be(new RectMm(328, 0, 310, 310));
        g.Folds.Should().Equal(310, 328);
        issues.Should().ContainSingle(i => i.Code == "cover.back.assumed" && i.Severity == ValidationSeverity.Info);
    }

    [Fact]
    public void NoValueIsInvented()
    {
        CoverLayout.Geometry(WithCover(null), out var none).Should().BeNull();
        none.Should().ContainSingle(i => i.Code == "cover.none");

        CoverLayout.Geometry(WithCover(new CoverSpec(CoverType.Softcover, new SizeMm(300, 300))), out var noSpine).Should().BeNull();
        noSpine.Should().ContainSingle(i => i.Code == "cover.spine.missing").Which.Message.Should().Contain("Baskı evinizden");

        CoverLayout.Geometry(WithCover(new CoverSpec(CoverType.Hardcover, null, null, new SpineSpec(12))), out var noFront).Should().BeNull();
        noFront.Should().ContainSingle(i => i.Code == "cover.front.missing");
    }

    [Fact]
    public void CoverPutsTheFavoriteOnTheFrontAndTheTitle()
    {
        var project = Project(new CoverSpec(CoverType.Hardcover, new SizeMm(300, 300), new SizeMm(300, 300), new SpineSpec(15)));
        var g = CoverLayout.Geometry(project.Profile, out _)!;

        var cover = CoverLayout.Create(project, g, TestData.Now);

        cover.Id.Should().Be(CoverLayout.CoverPageId);
        cover.Type.Should().Be(PageType.FrontCover);
        var front = cover.Slots.Single(s => s.Id == "cover-front");
        front.Placement!.AssetId.Should().Be("favori");
        front.Bounds.X.Should().Be(g.Front.X, "ön kapak fotoğrafı katlama çizgisinde başlar");
        front.Bounds.Right.Should().Be(g.Front.Right + 3, "sağdaki taşma payına uzanır");
        cover.Slots.Single(s => s.Id == "cover-back").Placement!.AssetId.Should().NotBe("favori");
        var title = cover.Elements.OfType<TextElement>().Single();
        title.Content.Should().Be("Yaz 2026");
        g.Front.Contains(title.Bounds).Should().BeTrue("başlık ön kapakta");

        CoverLayout.Create(project, g, TestData.Now).Should().BeEquivalentTo(cover, o => o.ComparingRecordsByMembers(), "deterministik");
        CoverLayout.Create(project, g, TestData.Now, "Bizim Yaz").Elements.OfType<TextElement>().Single().Content.Should().Be("Bizim Yaz");
    }

    [Fact]
    public void ReplacePageAndRemoveAssetReachTheCover()
    {
        var project = Project(new CoverSpec(CoverType.Hardcover, new SizeMm(300, 300), null, new SpineSpec(15)));
        var cover = CoverLayout.Create(project, CoverLayout.Geometry(project.Profile, out _)!, TestData.Now);
        project = project with { Cover = cover };

        var renamed = cover with { Notes = "değişti" };
        project.ReplacePage(renamed).Cover.Should().BeSameAs(renamed);

        project.RemoveAsset("favori").Cover!.Slots.Should().NotContain(s => s.Placement != null && s.Placement.AssetId == "favori");
    }

    [Fact]
    public void CheckWarnsWhenTheSpineChanged()
    {
        var project = Project(new CoverSpec(CoverType.Hardcover, new SizeMm(300, 300), null, new SpineSpec(15)));
        project = project with { Cover = CoverLayout.Create(project, CoverLayout.Geometry(project.Profile, out _)!, TestData.Now) };

        CoverLayout.Check(project).Should().ContainSingle().Which.Code.Should().Be("cover.spine.confirm");

        var wider = project.WithProfile(project.Profile with { Cover = project.Profile.Cover! with { Spine = new SpineSpec(22) } });
        CoverLayout.Check(wider).Select(i => i.Code).Should().Contain("cover.outdated");

        var gone = project.WithProfile(project.Profile with { Cover = project.Profile.Cover! with { Spine = null } });
        CoverLayout.Check(gone).Should().Contain(i => i.Severity == ValidationSeverity.Warning && i.Message.StartsWith("Kapak:", StringComparison.Ordinal));
    }
}
