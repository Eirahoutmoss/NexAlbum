using FluentAssertions;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Layout;
using NexAlbum.Core.Projects;
using Xunit;

namespace NexAlbum.Core.Tests.Layout;

public class PhotoEditingTests
{
    private static readonly DateTimeOffset Later = TestData.Now.AddHours(1);

    private static AlbumPage TwoPhotos() => TestData.Page(
        "page-001",
        TestData.Slot("a", assetId: "asset-001"),
        TestData.Slot("b", x: 300, assetId: "asset-002"),
        TestData.Slot("bos", x: 600));

    [Fact]
    public void AdjustmentsAreStoredOnThePlacementAndCanBeCopiedToThePage()
    {
        var warm = new ImageAdjustments { Exposure = 0.2, Temperature = 0.5 };
        var page = PhotoEditing.SetAdjustments(TwoPhotos(), "a", warm, Later);

        ((Slot)page.FindElement("a")!).Placement!.Adjustments.Should().Be(warm);
        ((Slot)page.FindElement("b")!).Placement!.Adjustments.IsNone.Should().BeTrue();
        page.UpdatedAt.Should().Be(Later);
        PhotoEditing.SetAdjustments(page, "a", warm, Later.AddHours(1)).Should().BeSameAs(page, "değişiklik yoksa aynı sayfa");
        PhotoEditing.SetAdjustments(page, "bos", warm, Later).Should().BeSameAs(page, "boş yuvaya ayar yazılmaz");

        var all = PhotoEditing.SetAdjustmentsForAll(page, warm, Later);
        all.Slots.Where(s => s.Placement is not null).Should().OnlyContain(s => s.Placement!.Adjustments == warm);
        ((Slot)all.FindElement("bos")!).Placement.Should().BeNull();
    }

    [Fact]
    public void StraighteningIsLimitedAndKeepsTheSlotCovered()
    {
        var page = PhotoEditing.SetStraighten(TwoPhotos(), "a", 40, Later);
        ((Slot)page.FindElement("a")!).Placement!.Rotation.Should().Be(PhotoEditing.MaxStraighten);
        PhotoEditing.SetStraighten(page, "a", 15, Later).Should().BeSameAs(page);

        PhotoEditing.StraightenCover(100, 100, 100, 100, 0).Should().Be(1);
        var cover = PhotoEditing.StraightenCover(100, 100, 100, 100, 10);
        cover.Should().BeApproximately(Math.Cos(Math.PI / 18) + Math.Sin(Math.PI / 18), 1e-9, "kare yuva için cos + sin");
        PhotoEditing.StraightenCover(100, 100, 300, 300, 10).Should().Be(1, "zaten büyük görüntü büyütülmez");
        PhotoEditing.StraightenCover(100, 100, 100, 100, -10).Should().Be(cover, "yön fark etmez");
    }

    [Theory]
    [InlineData(null, 1, 6)]
    [InlineData(1, -1, 8)]
    [InlineData(6, 1, 3)]
    [InlineData(8, 1, 1)]
    [InlineData(3, 2, 1)]
    [InlineData(2, 1, 7)]
    [InlineData(5, 1, 2)]
    [InlineData(4, 4, 4)]
    public void OrientationRotatesByQuarterTurnsAndKeepsTheMirror(int? orientation, int turns, int expected) =>
        PhotoEditing.RotateOrientation(orientation, turns).Should().Be(expected);

    [Fact]
    public void QuarterRotationTurnsThePhotoEverywhereAndResetsItsFraming()
    {
        var page = TestData.Page(
            "page-001",
            TestData.Slot("a", assetId: "asset-001") with
            {
                Placement = new PhotoPlacement { AssetId = "asset-001", OffsetX = 0.1, Scale = 2, Crop = new NormalizedRect(0.1, 0.1, 0.5, 0.5) },
            },
            TestData.Slot("b", x: 300, assetId: "asset-002"));
        var project = TestData.Project().AddAssets([TestData.Asset("asset-001"), TestData.Asset("asset-002")]).AddPage(page);

        var rotated = PhotoEditing.RotateQuarter(project, "asset-001", 1, Later);

        var asset = rotated.FindAsset("asset-001")!;
        asset.ExifOrientation.Should().Be(6);
        asset.DisplayAspectRatio.Should().BeApproximately(4000.0 / 6000, 1e-9, "yatay fotoğraf dikey oldu");
        var placement = ((Slot)rotated.Pages[0].FindElement("a")!).Placement!;
        placement.Crop.Should().Be(NormalizedRect.Full);
        placement.OffsetX.Should().Be(0);
        placement.Scale.Should().Be(2, "yakınlaştırma korunur");
        ((Slot)rotated.Pages[0].FindElement("b")!).Placement.Should().Be(((Slot)page.FindElement("b")!).Placement, "başka fotoğrafa dokunulmaz");

        PhotoEditing.RotateQuarter(PhotoEditing.RotateQuarter(rotated, "asset-001", -1, Later), "asset-001", 4, Later)
            .FindAsset("asset-001")!.ExifOrientation.Should().Be(1);
        PhotoEditing.RotateQuarter(project, "asset-001", 0, Later).Should().BeSameAs(project);
        FluentActions.Invoking(() => PhotoEditing.RotateQuarter(project, "yok", 1, Later)).Should().Throw<ArgumentException>();
    }
}
