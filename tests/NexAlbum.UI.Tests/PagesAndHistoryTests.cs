using System.IO;
using FluentAssertions;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Profiles;
using NexAlbum.Core.Projects;
using NexAlbum.Persistence.Packaging;
using NexAlbum.UI.Services;
using NexAlbum.UI.ViewModels;
using Xunit;

namespace NexAlbum.UI.Tests;

/// <summary>Geri al / yinele ve sayfa yönetimi.</summary>
public sealed partial class MainViewModelTests
{
    [Fact]
    public void PageOperationsAreUndoableAndCleanStateIsRestored()
    {
        var path = PagedProject(3);
        using var vm = new MainViewModel(new FakeDialogs { SavePath = path }, settings: new MemoryAppSettingsStore());
        vm.History.MergeWindow = TimeSpan.Zero;
        vm.Open(path);
        vm.Save(saveAs: false).Should().BeTrue();
        vm.IsDirty.Should().BeFalse();
        vm.UndoCommand.CanExecute(null).Should().BeFalse("açılışta geçmiş boştur");

        vm.SelectedPage = vm.Pages[0];
        vm.AddPageCommand.Execute(null);
        vm.Pages.Should().HaveCount(4);
        vm.SelectedPage!.Index.Should().Be(1, "yeni sayfa seçili sayfanın arkasına eklenir");
        vm.SelectedPage.Slots.Should().BeEmpty();
        vm.UndoToolTip.Should().Contain("Sayfa ekle");

        vm.DuplicatePageCommand.Execute(null);
        vm.Pages.Should().HaveCount(5);

        vm.SelectedPage = vm.Pages[4];
        vm.MovePageLeftCommand.Execute(null);
        vm.SelectedPage!.Index.Should().Be(3);

        vm.DeletePageCommand.Execute(null);
        vm.Pages.Should().HaveCount(4);
        vm.Status.Should().Contain("Ctrl+Z");

        vm.UndoCommand.Execute(null);
        vm.Pages.Should().HaveCount(5);
        vm.Status.Should().StartWith("Geri alındı: Sayfayı sil");
        vm.RedoCommand.CanExecute(null).Should().BeTrue();

        vm.UndoCommand.Execute(null);
        vm.UndoCommand.Execute(null);
        vm.UndoCommand.Execute(null);
        vm.Pages.Should().HaveCount(3);
        vm.IsDirty.Should().BeFalse("kaydedilmiş hâle geri dönüldü");
        vm.UndoCommand.CanExecute(null).Should().BeFalse();

        vm.RedoCommand.Execute(null);
        vm.Pages.Should().HaveCount(4);
        vm.IsDirty.Should().BeTrue();
        vm.Status.Should().StartWith("Yinelendi: Sayfa ekle");

        vm.AddPageCommand.Execute(null);
        vm.RedoCommand.CanExecute(null).Should().BeFalse("yeni düzenleme yinele geçmişini siler");
    }

    [Fact]
    public void LockedPagesCannotBeDeletedOrMoved()
    {
        using var vm = new MainViewModel(new FakeDialogs(), settings: new MemoryAppSettingsStore());
        vm.Open(PagedProject(3));
        vm.SelectedPage = vm.Pages[1];
        vm.ToggleLock();

        vm.DeletePageCommand.CanExecute(null).Should().BeFalse();
        vm.MovePageLeftCommand.CanExecute(null).Should().BeFalse();
        vm.DeletePage();
        vm.Pages.Should().HaveCount(3);
        vm.Status.Should().Contain("kilitli");

        vm.MovePageTo(vm.Pages[0], 2).Should().BeFalse("kilitli sayfanın üstünden atlanamaz");
        vm.MovePageTo(vm.Pages[2], 2).Should().BeFalse("sıra değişmez");
    }

    [Fact]
    public void DraggingAPageReordersIt()
    {
        using var vm = new MainViewModel(new FakeDialogs(), settings: new MemoryAppSettingsStore());
        vm.Open(PagedProject(4));
        var ids = vm.Pages.Select(p => p.Page.Id).ToList();

        vm.MovePageTo(vm.Pages[0], 3).Should().BeTrue();

        vm.Pages.Select(p => p.Page.Id).Should().Equal(ids[1], ids[2], ids[3], ids[0]);
        vm.SelectedPage!.Page.Id.Should().Be(ids[0], "taşınan sayfa seçili kalır");
        vm.Undo();
        vm.Pages.Select(p => p.Page.Id).Should().Equal(ids);
    }

    [Fact]
    public void EditsAreRecordedAndOpeningAnotherProjectClearsHistory()
    {
        var first = PagedProject(2);
        using var vm = new MainViewModel(new FakeDialogs(), settings: new MemoryAppSettingsStore());
        vm.History.MergeWindow = TimeSpan.Zero;
        vm.Open(first);
        vm.SelectedPage = vm.Pages[0];

        vm.ToggleLock();
        vm.UndoToolTip.Should().Be("Geri al: Kilitle (Ctrl+Z)");
        vm.ToggleLock();
        vm.History.UndoCount.Should().Be(2);

        vm.Open(PagedProject(3));
        vm.History.UndoCount.Should().Be(0);
        vm.UndoCommand.CanExecute(null).Should().BeFalse();
    }
}

public class ProjectHistoryTests
{
    private static readonly AlbumProject A = AlbumProject.Create("A", BuiltInProfiles.All[0], DateTimeOffset.UnixEpoch, "0.1.0");
    private static readonly AlbumProject B = A with { Name = "B" };
    private static readonly AlbumProject C = A with { Name = "C" };

    [Fact]
    public void UndoAndRedoWalkSnapshots()
    {
        var history = new ProjectHistory(new ManualClock()) { MergeWindow = TimeSpan.Zero };
        history.Record(A, "bir");
        history.Record(B, "iki");

        history.UndoLabel.Should().Be("iki");
        history.Undo(C).Should().BeSameAs(B);
        history.RedoLabel.Should().Be("iki");
        history.Undo(B).Should().BeSameAs(A);
        history.Undo(A).Should().BeNull();
        history.Redo(A).Should().BeSameAs(B);
        history.Redo(B).Should().BeSameAs(C);
        history.CanRedo.Should().BeFalse();
    }

    [Fact]
    public void QuickSuccessiveChangesMergeIntoOneStep()
    {
        var clock = new ManualClock();
        var history = new ProjectHistory(clock);
        history.Record(A, "Yakınlaştır");
        clock.Advance(TimeSpan.FromMilliseconds(100));
        history.Record(B, "Yakınlaştır");
        history.UndoCount.Should().Be(1, "kaydırıcı sürükleme tek adımdır");
        history.Undo(C).Should().BeSameAs(A, "en eski hâle dönülür");

        clock.Advance(TimeSpan.FromSeconds(5));
        history.Record(B, "Şablon");
        clock.Advance(TimeSpan.FromSeconds(5));
        history.Record(C, "Fon");
        history.UndoCount.Should().Be(2);
    }

    [Fact]
    public void CapacityDropsTheOldestSteps()
    {
        var history = new ProjectHistory(new ManualClock()) { MergeWindow = TimeSpan.Zero };
        for (var i = 0; i < ProjectHistory.Capacity + 20; i++)
        {
            history.Record(A, "x");
        }

        history.UndoCount.Should().Be(ProjectHistory.Capacity);
        history.Clear();
        history.CanUndo.Should().BeFalse();
    }

    private sealed class ManualClock : TimeProvider
    {
        private DateTimeOffset now = new(2026, 10, 13, 9, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => now;

        public void Advance(TimeSpan by) => now += by;
    }
}
