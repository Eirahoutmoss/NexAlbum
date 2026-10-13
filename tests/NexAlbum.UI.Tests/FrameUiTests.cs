using System.IO;
using FluentAssertions;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Layout;
using NexAlbum.Core.Projects;
using NexAlbum.Persistence.Packaging;
using NexAlbum.UI.Services;
using NexAlbum.UI.ViewModels;
using Xunit;

namespace NexAlbum.UI.Tests;

/// <summary>Çerçeve kipi: yuvayı taşıma, boyutlandırma, yapışma ve geometrik köşe düzenleme.</summary>
public sealed partial class MainViewModelTests
{
    private string FrameProject()
    {
        var path = Path.Combine(dir, "cerceve.albumproj");
        var project = SampleProject(0).WithPages(
        [
            new AlbumPage
            {
                Id = "p1",
                Elements =
                [
                    new Slot { Id = "kare", Bounds = new RectMm(20, 20, 100, 100) },
                    new Slot { Id = "ucgen", Bounds = new RectMm(160, 160, 100, 100), Shape = SlotShape.Triangle },
                ],
            },
        ]);
        ProjectPackageWriter.Save(path, project, new SaveOptions { AppVersion = project.AppVersion });
        return path;
    }

    [Fact]
    public void FrameIsMovedResizedAndUndone()
    {
        using var vm = new MainViewModel(new FakeDialogs(), settings: new MemoryAppSettingsStore());
        vm.History.MergeWindow = TimeSpan.Zero;
        vm.Open(FrameProject());
        var page = vm.SelectedPage!;
        vm.SelectSlot(page.Slots.Single(s => s.Id == "kare"));
        page.FrameEdit.Should().BeNull("kip kapalı");

        vm.IsFrameEditing = true;
        var edit = vm.SelectedPage!.FrameEdit!;
        edit.Handles.Should().HaveCount(8);
        edit.Corners.Should().HaveCount(4, "düz dikdörtgenin dört köşesi");

        // Serbest taşıma (yapışma yok): 10,3 mm sağa.
        vm.DragFrame(FrameHandle.Body, edit.Rect, 10.3, 0, toleranceMm: 0, snap: false, keepAspect: false);
        vm.CommitFrame();
        var origin = vm.SelectedPage!.CanvasBounds;
        var moved = vm.Project!.Pages[0].Slots.Single(s => s.Id == "kare").Bounds;
        Same(moved, new RectMm(30.3, 20, 100, 100));
        vm.SelectedSlot!.Id.Should().Be("kare", "seçim korunur");
        vm.SelectedPage.FrameEdit.Should().NotBeNull("kip açık kalır");

        // Sağ alt tutamaç: boyutlandırma.
        edit = vm.SelectedPage.FrameEdit!;
        vm.DragFrame(FrameHandle.BottomRight, edit.Rect, 20, -10, toleranceMm: 0, snap: false, keepAspect: false);
        vm.CommitFrame();
        Same(vm.Project!.Pages[0].Slots.Single(s => s.Id == "kare").Bounds, new RectMm(30.3, 20, 120, 90));

        vm.UndoCommand.Execute(null);
        vm.UndoCommand.Execute(null);
        vm.Project!.Pages[0].Slots.Single(s => s.Id == "kare").Bounds.Should().Be(new RectMm(20, 20, 100, 100));
        origin.Should().Be(vm.SelectedPage!.CanvasBounds);
    }

    private static void Same(RectMm actual, RectMm expected)
    {
        actual.X.Should().BeApproximately(expected.X, 1e-9);
        actual.Y.Should().BeApproximately(expected.Y, 1e-9);
        actual.Width.Should().BeApproximately(expected.Width, 1e-9);
        actual.Height.Should().BeApproximately(expected.Height, 1e-9);
    }

    [Fact]
    public void DraggedFrameSnapsToThePageCenter()
    {
        using var vm = new MainViewModel(new FakeDialogs(), settings: new MemoryAppSettingsStore());
        vm.Open(FrameProject());
        var page = vm.SelectedPage!;
        vm.SelectSlot(page.Slots.Single(s => s.Id == "kare"));
        vm.IsFrameEditing = true;
        var edit = page.FrameEdit!;

        // Yuva ortası sayfa ortasına 1 mm kalacak kadar taşınır.
        var center = page.PageArea.X + (page.PageArea.Width / 2);
        var dx = center - (edit.Rect.X + (edit.Rect.Width / 2)) - 1;
        vm.DragFrame(FrameHandle.Body, edit.Rect, dx, 0, toleranceMm: 2, snap: true, keepAspect: false);

        (edit.Rect.X + (edit.Rect.Width / 2)).Should().BeApproximately(center, 1e-9);
        page.SnapLines.Should().Contain(l => l.IsCenter);
        vm.CommitFrame();
        vm.SelectedPage!.SnapLines.Should().BeEmpty();
    }

    [Fact]
    public void TriangleCornerIsDraggedAndCanBeReset()
    {
        using var vm = new MainViewModel(new FakeDialogs(), settings: new MemoryAppSettingsStore());
        vm.History.MergeWindow = TimeSpan.Zero;
        vm.Open(FrameProject());
        vm.SelectSlot(vm.SelectedPage!.Slots.Single(s => s.Id == "ucgen"));
        vm.IsFrameEditing = true;
        var edit = vm.SelectedPage!.FrameEdit!;
        edit.Corners.Should().HaveCount(3);
        vm.ResetFrameCornersCommand.CanExecute(null).Should().BeTrue();

        // Tepe köşesini sola çek (yuvanın %20'si).
        var top = edit.UnitCorners!.OrderBy(c => c.Y).First();
        var index = edit.UnitCorners!.ToList().IndexOf(top);
        vm.DragFrameCorner(index, edit.Rect.X + (0.2 * edit.Rect.Width), edit.Rect.Y);
        vm.CommitFrame();

        var slot = vm.Project!.Pages[0].Slots.Single(s => s.Id == "ucgen");
        slot.Shape.Should().Be(SlotShape.Polygon);
        slot.Polygon.Should().Contain(p => Math.Abs(p.X - 0.2) < 1e-9 && p.Y < 1e-9);

        vm.ResetFrameCornersCommand.Execute(null);
        vm.Project!.Pages[0].Slots.Single(s => s.Id == "ucgen").Shape.Should().Be(SlotShape.Rectangle);
        vm.UndoCommand.Execute(null);
        vm.Project!.Pages[0].Slots.Single(s => s.Id == "ucgen").Shape.Should().Be(SlotShape.Polygon);

        vm.IsFrameEditing = false;
        vm.SelectedPage!.FrameEdit.Should().BeNull();
    }
}
