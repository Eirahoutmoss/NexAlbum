using System.IO;
using FluentAssertions;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Projects;
using NexAlbum.Persistence.Packaging;
using NexAlbum.UI.Services;
using NexAlbum.UI.ViewModels;
using Xunit;

namespace NexAlbum.UI.Tests;

/// <summary>Galeri: favoriler, sıralama; fotoğrafı şeritteki başka bir sayfaya koyma ve taşıma.</summary>
public sealed partial class MainViewModelTests
{
    [Fact]
    public void FavoritesAreToggledFilteredSavedAndUndoable()
    {
        var path = PagedProject(3);
        using var vm = new MainViewModel(new FakeDialogs { SavePath = path }, settings: new MemoryAppSettingsStore());
        vm.History.MergeWindow = TimeSpan.Zero;
        vm.Open(path);

        vm.FilterFavoritesText.Should().Be("0");
        vm.ToggleFavoriteCommand.CanExecute(null).Should().BeFalse("seçili fotoğraf yok");
        vm.ToggleFavoriteCommand.Execute(vm.Assets[1]);
        vm.Assets[1].IsFavorite.Should().BeTrue();
        vm.FilterFavoritesText.Should().Be("1");
        vm.Status.Should().Contain("favorilere eklendi");
        vm.UndoToolTip.Should().Contain("Favori yap");

        vm.IsFilterFavorites = true;
        vm.GalleryRows.SelectMany(r => r.Items).Should().ContainSingle().Which.Asset.Id.Should().Be(vm.Assets[1].Asset.Id);

        vm.Save(saveAs: false).Should().BeTrue();
        ProjectPackageReader.Load(path).Project.Assets.Count(a => a.Favorite).Should().Be(1, "favori proje dosyasına yazılır");

        vm.UndoCommand.Execute(null);
        vm.Assets.Should().OnlyContain(a => !a.IsFavorite);
        vm.HasVisiblePhotos.Should().BeFalse();
        vm.GalleryEmptyText.Should().Contain("Favori yok");

        vm.SelectedAsset = vm.Assets[0];
        vm.ToggleFavoriteCommand.Execute(null);
        vm.Assets[0].IsFavorite.Should().BeTrue("parametresiz: seçili fotoğraf");
        vm.ToggleFavoriteCommand.Execute(null);
        vm.Assets[0].IsFavorite.Should().BeFalse();
    }

    [Fact]
    public void GalleryCanBeSortedByNameAndCaptureTime()
    {
        var path = Path.Combine(dir, "siralama.albumproj");
        var start = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var project = SampleProject(0).AddAssets(
        [
            MakePhoto("a", "IMG_10.jpg", start.AddHours(1)),
            MakePhoto("b", "IMG_2.jpg", start.AddHours(3)),
            MakePhoto("c", "img_1.jpg", null),
            MakePhoto("d", "IMG_3.jpg", start),
        ]);
        ProjectPackageWriter.Save(path, project, new SaveOptions { AppVersion = project.AppVersion });

        using var vm = new MainViewModel(new FakeDialogs(), settings: new MemoryAppSettingsStore());
        vm.Open(path);
        vm.GalleryColumns = 4;
        GalleryOrder(vm).Should().Equal("a", "b", "c", "d");

        vm.SelectedGallerySort = vm.GallerySortOptions.Single(o => o.Sort == GallerySort.Name);
        GalleryOrder(vm).Should().Equal("c", "b", "d", "a");

        vm.SelectedGallerySort = vm.GallerySortOptions.Single(o => o.Sort == GallerySort.Captured);
        GalleryOrder(vm).Should().Equal("d", "a", "b", "c");
    }

    [Fact]
    public void NaturalComparerOrdersNumbersAsNumbers()
    {
        string[] names = ["IMG_10.jpg", "IMG_2.jpg", "img_02b.jpg", "DSC_9.jpg", "IMG_1.jpg"];
        names.Order(NaturalComparer.Instance).Should().Equal("DSC_9.jpg", "IMG_1.jpg", "IMG_2.jpg", "img_02b.jpg", "IMG_10.jpg");
    }

    [Fact]
    public void PhotosMoveToOtherPagesFromTheCanvasAndTheGallery()
    {
        var path = Path.Combine(dir, "tasima.albumproj");
        var project = SampleProject(3).WithPages(
        [
            new AlbumPage { Id = "p0", Elements = [MakeSlot("s1", "asset-0001"), MakeSlot("s2", "asset-0002", x: 160)] },
            new AlbumPage { Id = "p1", Elements = [MakeSlot("t1", null), MakeSlot("t2", null, x: 160)] },
            new AlbumPage { Id = "p2", Elements = [MakeSlot("u1", "asset-0003")], Locked = true },
        ]);
        ProjectPackageWriter.Save(path, project, new SaveOptions { AppVersion = project.AppVersion });

        using var vm = new MainViewModel(new FakeDialogs(), settings: new MemoryAppSettingsStore());
        vm.History.MergeWindow = TimeSpan.Zero;
        vm.Open(path);
        vm.SelectedPage = vm.Pages[0];

        MainViewModel.CanReceivePhoto(vm.Pages[1]).Should().BeTrue();
        MainViewModel.CanReceivePhoto(vm.Pages[2]).Should().BeFalse("kilitli");

        vm.MovePhotoToPage("p0", "s1", vm.Pages[1]).Should().BeTrue();
        vm.Pages[0].PhotoCount.Should().Be(1);
        vm.Pages[1].PhotoCount.Should().Be(1);
        vm.Status.Should().Contain("2. sayfaya taşındı");
        vm.SelectedPage!.Index.Should().Be(0, "çalışılan sayfada kalınır");

        vm.PlaceOnPage(vm.Pages[1], "asset-0002").Should().BeTrue("galeriden koymak kopyalar");
        vm.Pages[1].PhotoCount.Should().Be(2);
        vm.Pages[0].PhotoCount.Should().Be(1);

        vm.PlaceOnPage(vm.Pages[1], "asset-0003").Should().BeFalse();
        vm.Status.Should().Contain("boş yuva yok");
        vm.MovePhotoToPage("p0", "s2", vm.Pages[2]).Should().BeFalse();
        vm.Status.Should().Contain("kilitli");

        vm.UndoCommand.Execute(null);
        vm.UndoCommand.Execute(null);
        vm.Pages[0].PhotoCount.Should().Be(2);
        vm.Pages[1].PhotoCount.Should().Be(0);
    }

    private static IEnumerable<string> GalleryOrder(MainViewModel vm) => vm.GalleryRows.SelectMany(r => r.Items).Select(i => i.Asset.Id);

    private static PhotoAsset MakePhoto(string id, string name, DateTimeOffset? captured) => new()
    {
        Id = id,
        SourcePath = @"C:\yok\" + name,
        FileName = name,
        PixelWidth = 6000,
        PixelHeight = 4000,
        CapturedAt = captured,
        ImportedAt = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero),
    };

    private static Slot MakeSlot(string id, string? assetId, double x = 10) => new()
    {
        Id = id,
        Bounds = new RectMm(x, 10, 130, 90),
        Placement = assetId is null ? null : new PhotoPlacement { AssetId = assetId },
    };
}
