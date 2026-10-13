using FluentAssertions;
using NexAlbum.Core.Layout;
using NexAlbum.Core.Projects;
using Xunit;

namespace NexAlbum.Core.Tests.Layout;

public class PhotoMoverTests
{
    private static readonly DateTimeOffset Later = TestData.Now.AddMinutes(3);

    private static AlbumProject Project(bool lockTarget = false) =>
        TestData.Project()
            .AddAssets([TestData.Asset("wide", 6000, 4000), TestData.Asset("tall", 4000, 6000)])
            .WithPages(
            [
                TestData.Page("a", TestData.Slot("a1", assetId: "wide"), TestData.Slot("a2", x: 150, width: 100, height: 100, assetId: "tall")),
                TestData.Page("b", TestData.Slot("b-tall", width: 100, height: 200), TestData.Slot("b-wide", y: 230, width: 200, height: 60)) with { Locked = lockTarget },
                TestData.Page("c", TestData.Slot("c1", assetId: "wide")),
            ]);

    [Fact]
    public void MovePicksTheEmptySlotClosestToThePhotoShape()
    {
        var (moved, result) = PhotoMover.MoveToPage(Project(), "a", "a1", "b", Later);

        result.Should().Be(PhotoMoveResult.Done);
        ((Slot)moved.FindPage("a")!.FindElement("a1")!).Placement.Should().BeNull("kaynak yuva boşalır");
        ((Slot)moved.FindPage("b")!.FindElement("b-wide")!).Placement!.AssetId.Should().Be("wide", "yatay fotoğraf yatay yuvaya");
        ((Slot)moved.FindPage("b")!.FindElement("b-tall")!).Placement.Should().BeNull();
        moved.FindPage("b")!.UpdatedAt.Should().Be(Later);

        var (again, second) = PhotoMover.MoveToPage(moved, "a", "a2", "b", Later);
        second.Should().Be(PhotoMoveResult.Done);
        ((Slot)again.FindPage("b")!.FindElement("b-tall")!).Placement!.AssetId.Should().Be("tall");

        PhotoMover.MoveToPage(again, "c", "c1", "b", Later).Result.Should().Be(PhotoMoveResult.AlreadyThere, "fotoğraf zaten hedefte");
    }

    [Fact]
    public void PanAndZoomAreResetButAdjustmentsKept()
    {
        var project = Project();
        var page = project.FindPage("a")!;
        var slot = (Slot)page.FindElement("a1")!;
        project = project.ReplacePage(page.ReplaceElement(slot with
        {
            Placement = slot.Placement! with { OffsetX = 0.4, Scale = 2, Adjustments = ImageAdjustments.None with { Brightness = 0.2 } },
        }));

        var (moved, _) = PhotoMover.MoveToPage(project, "a", "a1", "b", Later);
        var placement = ((Slot)moved.FindPage("b")!.FindElement("b-wide")!).Placement!;
        placement.OffsetX.Should().Be(0);
        placement.Scale.Should().Be(1);
        placement.Adjustments.Brightness.Should().Be(0.2);
    }

    [Fact]
    public void RefusalsLeaveTheProjectUntouched()
    {
        var locked = Project(lockTarget: true);
        var refused = PhotoMover.MoveToPage(locked, "a", "a1", "b", Later);
        refused.Result.Should().Be(PhotoMoveResult.Locked);
        refused.Project.Should().BeSameAs(locked);
        PhotoMover.PlaceOnPage(locked, "b", "wide", Later).Result.Should().Be(PhotoMoveResult.Locked);

        var project = Project();
        PhotoMover.MoveToPage(project, "a", "a1", "a", Later).Result.Should().Be(PhotoMoveResult.AlreadyThere);
        PhotoMover.MoveToPage(project, "b", "b-tall", "c", Later).Result.Should().Be(PhotoMoveResult.NotFound, "boş yuvadan taşınamaz");
        PhotoMover.MoveToPage(project, "a", "a2", "c", Later).Result.Should().Be(PhotoMoveResult.NoEmptySlot);
        PhotoMover.PlaceOnPage(project, "yok", "wide", Later).Result.Should().Be(PhotoMoveResult.NotFound);
    }

    [Fact]
    public void GalleryPhotoGoesToTheBestEmptySlot()
    {
        var (placed, result) = PhotoMover.PlaceOnPage(Project(), "b", "tall", Later);
        result.Should().Be(PhotoMoveResult.Done);
        ((Slot)placed.FindPage("b")!.FindElement("b-tall")!).Placement!.AssetId.Should().Be("tall");
        ((Slot)placed.FindPage("a")!.FindElement("a2")!).Placement!.AssetId.Should().Be("tall", "galeriden koymak kopyalar; kaynak sayfa değişmez");
        PhotoMover.PlaceOnPage(placed, "b", "tall", Later).Result.Should().Be(PhotoMoveResult.AlreadyThere);
    }

    [Fact]
    public void DroppingOnASlotOfAnotherPageMovesOrSwaps()
    {
        var (moved, result) = PhotoMover.MoveToSlot(Project(), "a", "a1", "b", "b-tall", Later);
        result.Should().Be(PhotoMoveResult.Done, "kullanıcının seçtiği yuva, oranı uymasa da");
        ((Slot)moved.FindPage("b")!.FindElement("b-tall")!).Placement!.AssetId.Should().Be("wide");
        ((Slot)moved.FindPage("a")!.FindElement("a1")!).Placement.Should().BeNull();

        var pair = TestData.Project()
            .AddAssets([TestData.Asset("wide", 6000, 4000), TestData.Asset("tall", 4000, 6000)])
            .WithPages([TestData.Page("x", TestData.Slot("x1", assetId: "wide")), TestData.Page("y", TestData.Slot("y1", assetId: "tall"))]);
        var (swapped, swap) = PhotoMover.MoveToSlot(pair, "x", "x1", "y", "y1", Later);
        swap.Should().Be(PhotoMoveResult.Done);
        ((Slot)swapped.FindPage("x")!.FindElement("x1")!).Placement!.AssetId.Should().Be("tall", "dolu yuvaya bırakınca yer değiştirir");
        ((Slot)swapped.FindPage("y")!.FindElement("y1")!).Placement!.AssetId.Should().Be("wide");

        PhotoMover.MoveToSlot(Project(), "a", "a2", "c", "c1", Later).Result
            .Should().Be(PhotoMoveResult.AlreadyThere, "takasta «wide» a sayfasına ikinci kez girerdi");
    }
}
