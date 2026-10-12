using FluentAssertions;
using NexAlbum.Core.Design;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Layout;
using NexAlbum.Core.Projects;
using Xunit;

namespace NexAlbum.Core.Tests.Design;

public class PageEditorTests
{
    private static readonly DateTimeOffset Later = TestData.Now.AddHours(1);

    private static AlbumPage Page() => new()
    {
        Id = "p",
        Elements =
        [
            new Slot { Id = "s1", Bounds = new RectMm(0, 0, 100, 100), Placement = new PhotoPlacement { AssetId = "a", OffsetX = 0.1, Scale = 2 } },
            new Slot { Id = "s2", Bounds = new RectMm(100, 0, 100, 50), Placement = new PhotoPlacement { AssetId = "b" } },
            new Slot { Id = "s3", Bounds = new RectMm(100, 50, 100, 50) },
        ],
    };

    private static Slot S(AlbumPage page, string id) => (Slot)page.FindElement(id)!;

    [Fact]
    public void PlacingAPhotoReplacesTheSlotContent()
    {
        var page = PageEditor.PlacePhoto(Page(), "s3", "c", Later);
        S(page, "s3").Placement!.AssetId.Should().Be("c");
        page.UpdatedAt.Should().Be(Later);

        page = PageEditor.PlacePhoto(page, "s1", "d", Later);
        S(page, "s1").Placement.Should().Be(new PhotoPlacement { AssetId = "d" }, "yeni fotoğraf ortalanmış ve ölçeksiz gelir");
        page.UsesAsset("a").Should().BeFalse();
    }

    [Fact]
    public void PlacingAPhotoAlreadyOnThePageSwapsTheSlots()
    {
        var page = PageEditor.PlacePhoto(Page(), "s2", "a", Later);
        S(page, "s2").Placement!.AssetId.Should().Be("a");
        S(page, "s2").Placement!.Scale.Should().Be(1, "farklı oranlı yuvada kaydırma/ölçek sıfırlanır");
        S(page, "s1").Placement!.AssetId.Should().Be("b");

        PageEditor.PlacePhoto(page, "s2", "a", Later).Should().BeSameAs(page, "aynı yuvaya bırakmak değişiklik değildir");
    }

    [Fact]
    public void SwapMovesAPhotoIntoAnEmptySlot()
    {
        var page = PageEditor.Swap(Page(), "s1", "s3", Later);
        S(page, "s1").Placement.Should().BeNull();
        S(page, "s3").Placement!.AssetId.Should().Be("a");
        PageEditor.Swap(page, "s3", "s3", Later).Should().BeSameAs(page);
    }

    [Fact]
    public void ClearRemovesThePlacement()
    {
        var page = PageEditor.Clear(Page(), "s2", Later);
        S(page, "s2").Placement.Should().BeNull();
        PageEditor.Clear(page, "s2", Later).Should().BeSameAs(page);
        FluentActions.Invoking(() => PageEditor.Clear(page, "yok", Later)).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CoverPanIsLimitedSoNoGapAppears()
    {
        // Kare yuva, 3:2 yatay fotoğraf: ölçek 1'de 150×100 mm; yatayda en çok (1.5-1)/2 = 0.25 kayabilir, dikeyde hiç.
        var area = new RectMm(0, 0, 100, 100);
        var p = PageEditor.Clamp(new PhotoPlacement { AssetId = "a", OffsetX = 0.9, OffsetY = -0.4 }, area, 1.5, CropMode.Cover);
        p.OffsetX.Should().BeApproximately(0.25, 1e-9);
        p.OffsetY.Should().Be(0);

        var zoomed = PageEditor.Clamp(new PhotoPlacement { AssetId = "a", Scale = 2, OffsetY = -0.9 }, area, 1.5, CropMode.Cover);
        zoomed.OffsetY.Should().BeApproximately(-0.5, 1e-9, "200×300 mm görüntü 100 mm yuvada ±0.5 kayar");
    }

    [Fact]
    public void ScaleIsLimited()
    {
        var area = new RectMm(0, 0, 100, 100);
        PageEditor.Clamp(new PhotoPlacement { AssetId = "a", Scale = 0.2 }, area, 1, CropMode.Cover).Scale.Should().Be(PageEditor.MinScale);
        PageEditor.Clamp(new PhotoPlacement { AssetId = "a", Scale = 50 }, area, 1, CropMode.Cover).Scale.Should().Be(PageEditor.MaxScale);
    }

    [Fact]
    public void ContainKeepsThePhotoInsideTheSlot()
    {
        // 2:1 fotoğraf kare yuvada 100×50 mm; dikeyde (1-0.5)/2 = 0.25 kayabilir.
        var p = PageEditor.Clamp(new PhotoPlacement { AssetId = "a", OffsetY = 1 }, new RectMm(0, 0, 100, 100), 2, CropMode.Contain);
        p.OffsetY.Should().BeApproximately(0.25, 1e-9);
        p.OffsetX.Should().Be(0);
    }

    [Fact]
    public void SetPlacementClampsAndRejectsForeignAssets()
    {
        var page = PageEditor.SetPlacement(Page(), "s2", new PhotoPlacement { AssetId = "b", OffsetX = 1, Scale = 3 }, 1, Later);
        var placement = S(page, "s2").Placement!;
        placement.Scale.Should().Be(3);
        placement.OffsetX.Should().BeApproximately(1, 1e-9, "300 mm görüntü 100 mm yuvada ±1 kayar");

        FluentActions.Invoking(() => PageEditor.SetPlacement(Page(), "s2", new PhotoPlacement { AssetId = "a" }, 1, Later))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CropModeChangeResetsPanAndZoom()
    {
        var page = PageEditor.SetCropMode(Page(), "s1", CropMode.Contain, Later);
        S(page, "s1").CropMode.Should().Be(CropMode.Contain);
        S(page, "s1").Placement!.Scale.Should().Be(1);
        S(page, "s1").Placement!.OffsetX.Should().Be(0);
        PageEditor.SetCropMode(page, "s1", CropMode.Contain, Later).Should().BeSameAs(page);
    }

    [Fact]
    public void FrameCanBeAppliedToASingleSlot()
    {
        var page = PageEditor.ApplyFrame(Page(), "s2", FramePresets.Find("polaroid")!, Later);
        S(page, "s2").StyleId.Should().Be("polaroid");
        S(page, "s1").StyleId.Should().BeNull();
    }
}
