using System.IO;
using CommunityToolkit.Mvvm.Input;
using FluentAssertions;
using NexAlbum.Persistence.Packaging;
using NexAlbum.UI.Services;
using NexAlbum.UI.ViewModels;
using Xunit;

namespace NexAlbum.UI.Tests;

/// <summary>Uygulama kabuğu: tema, son projeler, galeri süzgeci ve komut paleti (Karar 12 Rev. 1, Karar 17).</summary>
public sealed partial class MainViewModelTests
{
    [Fact]
    public void RecentProjectsAreRememberedNewestFirstWithoutDuplicates()
    {
        var store = new MemoryAppSettingsStore();
        var first = Path.Combine(dir, "bir.albumproj");
        var second = Path.Combine(dir, "iki.albumproj");
        ProjectPackageWriter.Save(first, SampleProject(2), new SaveOptions { AppVersion = "0.1.0" });

        var dialogs = new FakeDialogs { SavePath = second };
        using var vm = new MainViewModel(dialogs, settings: store);
        vm.HasRecentProjects.Should().BeFalse();

        vm.Open(first);
        vm.RecentProjects.Should().ContainSingle().Which.Path.Should().Be(Path.GetFullPath(first));
        vm.Save(saveAs: true).Should().BeTrue();
        vm.RecentProjects.Select(r => r.Path).Should().Equal(Path.GetFullPath(second), Path.GetFullPath(first));

        vm.Open(first);
        vm.RecentProjects.Select(r => r.Path).Should().Equal(Path.GetFullPath(first), Path.GetFullPath(second));
        vm.RecentProjects[0].Exists.Should().BeTrue();
        vm.RecentProjects[0].WhenText.Should().StartWith("Bugün");
        store.Load().RecentProjects.Should().HaveCount(2, "liste ayar dosyasına yazılır");

        vm.ForgetRecent(vm.RecentProjects[1]);
        vm.RecentProjects.Should().ContainSingle();
    }

    [Fact]
    public void RecentProjectListIsCapped()
    {
        var now = DateTimeOffset.UtcNow;
        var many = Enumerable.Range(0, 12).Select(i => new RecentProject(Path.Combine(dir, $"p{i}.albumproj"), $"P{i}", now)).ToList();
        var store = new MemoryAppSettingsStore(new AppSettings { RecentProjects = many });
        using var vm = new MainViewModel(new FakeDialogs(), settings: store);

        vm.RecentProjects.Should().HaveCount(MainViewModel.MaxRecentProjects);
        vm.RecentProjects.Should().OnlyContain(r => !r.Exists);
    }

    [Fact]
    public void OpeningAMissingRecentProjectWarnsAndForgetsIt()
    {
        var missing = Path.Combine(dir, "tasindi.albumproj");
        var store = new MemoryAppSettingsStore(new AppSettings { RecentProjects = [new RecentProject(missing, "Taşındı", DateTimeOffset.UtcNow)] });
        var dialogs = new FakeDialogs();
        using var vm = new MainViewModel(dialogs, settings: store);

        vm.OpenRecentCommand.Execute(vm.RecentProjects[0]);

        vm.HasProject.Should().BeFalse();
        dialogs.Warnings.Should().ContainSingle().Which.Should().Contain("bulunamadı");
        vm.HasRecentProjects.Should().BeFalse();
        store.Load().RecentProjects.Should().BeEmpty();
    }

    [Fact]
    public void ThemeToggleIsSavedAndAnnounced()
    {
        var store = new MemoryAppSettingsStore();
        using var vm = new MainViewModel(new FakeDialogs(), settings: store);
        var announced = new List<AppTheme>();
        vm.ThemeChanged += (_, theme) => announced.Add(theme);

        vm.Theme.Should().Be(AppTheme.Gece, "varsayılan tema Gece (Karar 17)");
        vm.IsDarkTheme.Should().BeTrue();

        vm.ToggleThemeCommand.Execute(null);
        vm.Theme.Should().Be(AppTheme.Fildisi);
        store.Load().Theme.Should().Be("Fildisi");
        announced.Should().Equal(AppTheme.Fildisi);

        vm.ToggleThemeCommand.Execute(null);
        announced.Should().Equal(AppTheme.Fildisi, AppTheme.Gece);
        store.Load().Theme.Should().Be("Gece");
    }

    [Fact]
    public void SettingsWindowThemeChoiceIsApplied()
    {
        var store = new MemoryAppSettingsStore();
        var dialogs = new FakeDialogs { EditedSettings = new AppSettings { Theme = "Fildisi" } };
        using var vm = new MainViewModel(dialogs, settings: store);
        AppTheme? announced = null;
        vm.ThemeChanged += (_, theme) => announced = theme;

        vm.EditSettings();

        vm.Theme.Should().Be(AppTheme.Fildisi);
        announced.Should().Be(AppTheme.Fildisi);
        store.Load().Theme.Should().Be("Fildisi");
    }

    [Fact]
    public async Task GalleryIsChunkedIntoRowsAndFilteredKeepingSelection()
    {
        var path = Path.Combine(dir, "galeri.albumproj");
        ProjectPackageWriter.Save(path, SampleProject(10), new SaveOptions { AppVersion = "0.1.0" });
        using var vm = new MainViewModel(new FakeDialogs(), settings: new MemoryAppSettingsStore());
        vm.Open(path);

        vm.GalleryColumns = 3;
        vm.GalleryRows.Select(r => r.Items.Count).Should().Equal(3, 3, 3, 1);
        vm.FilterAllText.Should().Be("Tümü · 10");
        vm.FilterUnusedText.Should().Be("Kullanılmamış · 10");

        vm.SelectedAsset = vm.Assets[4];
        vm.Assets[4].IsSelected.Should().BeTrue();

        vm.GalleryFilter = GalleryFilter.Used;
        vm.HasVisiblePhotos.Should().BeFalse("henüz albüm oluşturulmadı");
        vm.GalleryEmptyText.Should().Contain("Oto Oluştur");
        vm.IsFilterUsed.Should().BeTrue();

        await vm.AutoLayoutAsync(newSeed: false);
        vm.GalleryRows.SelectMany(r => r.Items).Should().HaveCount(10, "albüm oluşunca hepsi kullanıldı");
        vm.SelectedAsset.Should().NotBeNull("seçim kimliğiyle korunur");
        vm.SelectedAsset!.Asset.Id.Should().Be("asset-0005");
        vm.SelectedAsset.IsSelected.Should().BeTrue();

        vm.IsFilterAll = true;
        vm.GalleryFilter.Should().Be(GalleryFilter.All);
        vm.GalleryColumns = 50;
        vm.GalleryColumns.Should().Be(8, "sütun sayısı sınırlıdır");
    }

    [Fact]
    public void ZoomFollowsTheSelectedPage()
    {
        var path = Path.Combine(dir, "zoom.albumproj");
        var project = SampleProject(1);
        project = project.WithPages([new Core.Projects.AlbumPage { Id = "p1", Elements = [] }]);
        ProjectPackageWriter.Save(path, project, new SaveOptions { AppVersion = "0.1.0" });
        using var vm = new MainViewModel(new FakeDialogs(), settings: new MemoryAppSettingsStore());

        vm.Zoom.HasPage.Should().BeFalse();
        vm.Open(path);
        vm.Zoom.HasPage.Should().BeTrue();
        vm.PageNavText.Should().Be("1 / 1");
        vm.PagesHeader.Should().Be("SAYFALAR · 1");
        vm.ProjectDisplayName.Should().Be("Örnek");
    }

    [Fact]
    public void PaletteListsEveryMainCommandWithAnIcon()
    {
        using var vm = new MainViewModel(new FakeDialogs(), settings: new MemoryAppSettingsStore());

        vm.Palette.Commands.Should().OnlyContain(c => c.IconKey.StartsWith("Icon.", StringComparison.Ordinal));
        vm.Palette.Commands.Select(c => c.Command).Should().Contain(
            [vm.NewCommand, vm.OpenCommand, vm.SaveCommand, vm.AutoLayoutCommand, vm.ExportPdfCommand, vm.ToggleThemeCommand, vm.EditSettingsCommand]);

        vm.OpenPaletteCommand.Execute(null);
        vm.Palette.IsOpen.Should().BeTrue();
        vm.Palette.Query = "tema";
        vm.Palette.Selected!.Command.Command.Should().BeSameAs(vm.ToggleThemeCommand);
        vm.Palette.ExecuteSelected().Should().BeTrue();
        vm.Palette.IsOpen.Should().BeFalse();
        vm.Theme.Should().Be(AppTheme.Fildisi);
    }
}

public class CommandPaletteTests
{
    [Theory]
    [InlineData("Ön kontrol", "ON KONTROL")]
    [InlineData("PDF çıktı al", "PDF CIKTI AL")]
    [InlineData("İçe aktar · ĞÜŞ", "ICE AKTAR · GUS")]
    [InlineData("ılık IŞIK", "ILIK ISIK")]
    public void FoldIgnoresTurkishLettersAndCase(string text, string folded) =>
        CommandPaletteViewModel.Fold(text).Should().Be(folded);

    [Fact]
    public void SearchIsTurkishInsensitiveAndRanksPrefixFirst()
    {
        var palette = Create(out _);
        palette.Open();

        palette.Query = "on kontrol";
        palette.Results.Select(r => r.Command.Title).Should().Equal("Ön kontrol");

        palette.Query = "cikti";
        palette.Results[0].Command.Title.Should().Be("PDF çıktı al");

        palette.Query = "kaydet";
        palette.Results.Select(r => r.Command.Title).Should().Equal("Kaydet", "Farklı kaydet");

        palette.Query = "baskı";
        palette.Results.Select(r => r.Command.Title).Should().Contain("PDF çıktı al", "anahtar sözcükler de aranır");

        palette.Query = "zzz";
        palette.HasResults.Should().BeFalse();
        palette.Selected.Should().BeNull();
    }

    [Fact]
    public void DisabledCommandsSinkAndDoNotRun()
    {
        var palette = Create(out var ran);
        palette.Open();

        palette.Results.Last().Command.Title.Should().Be("Devre dışı");
        palette.Results.Last().IsEnabled.Should().BeFalse();

        palette.Execute(palette.Results.Last()).Should().BeFalse();
        palette.IsOpen.Should().BeTrue("devre dışı komut paleti kapatmaz");
        ran.Should().BeEmpty();
    }

    [Fact]
    public void KeyboardNavigationWrapsAndRecentlyUsedComesFirst()
    {
        var palette = Create(out var ran);
        palette.Open();
        palette.Selected.Should().BeSameAs(palette.Results[0]);
        palette.Results[0].IsSelected.Should().BeTrue();

        palette.MoveSelection(-1);
        palette.Selected.Should().BeSameAs(palette.Results[^1]);
        palette.MoveSelection(1);
        palette.Selected.Should().BeSameAs(palette.Results[0]);

        palette.Query = "pdf";
        palette.ExecuteSelected().Should().BeTrue();
        ran.Should().Equal("PDF çıktı al");
        palette.IsOpen.Should().BeFalse();

        palette.Open();
        palette.Query.Should().BeEmpty("her açılışta arama temizlenir");
        palette.Results[0].Command.Title.Should().Be("PDF çıktı al", "son kullanılan üstte");
    }

    private static CommandPaletteViewModel Create(out List<string> ran)
    {
        var log = new List<string>();
        ran = log;
        PaletteCommand Make(string title, string category, string? keywords = null, bool enabled = true) =>
            new(title, category, "Icon.Info", null, new RelayCommand(() => log.Add(title), () => enabled), keywords);

        return new CommandPaletteViewModel(
        [
            Make("Kaydet", "Proje"),
            Make("Devre dışı", "Proje", enabled: false),
            Make("Farklı kaydet", "Proje"),
            Make("Ön kontrol", "Albüm", "denetle"),
            Make("PDF çıktı al", "Albüm", "dışa aktar baskı"),
        ]);
    }
}

public class CanvasZoomTests
{
    [Fact]
    public void FitUsesTheViewportAndPercentIsRelativeToPrintSize()
    {
        var zoom = new CanvasZoomViewModel();
        zoom.HasPage.Should().BeFalse();
        zoom.ZoomInCommand.CanExecute(null).Should().BeFalse();

        zoom.SetPage(300, 300);
        zoom.SetViewport(1000 + (2 * CanvasZoomViewModel.FitPadding), 800 + (2 * CanvasZoomViewModel.FitPadding));

        zoom.IsFit.Should().BeTrue();
        zoom.DisplayWidth.Should().BeApproximately(800, 0.001);
        zoom.Percent.Should().BeApproximately(800.0 / 300 / CanvasZoomViewModel.DipPerMm * 100, 0.001);
        zoom.PercentText.Should().StartWith("%");

        zoom.SetViewport(500 + (2 * CanvasZoomViewModel.FitPadding), 2000);
        zoom.DisplayWidth.Should().BeApproximately(500, 0.001, "sığdır modu pencereyi izler");
    }

    [Fact]
    public void StepsClampAndActualSize()
    {
        var zoom = new CanvasZoomViewModel();
        zoom.SetPage(300, 200);
        zoom.SetViewport(1000, 800);

        zoom.SetPercent(70);
        zoom.ZoomIn();
        zoom.Percent.Should().Be(75);
        zoom.ZoomOut();
        zoom.Percent.Should().Be(67);
        zoom.IsFit.Should().BeFalse();

        zoom.SetPercent(5000);
        zoom.Percent.Should().Be(CanvasZoomViewModel.MaxPercent);
        zoom.ZoomInCommand.CanExecute(null).Should().BeFalse();

        zoom.ZoomBy(0.0001);
        zoom.Percent.Should().Be(CanvasZoomViewModel.MinPercent);

        zoom.ActualSizeCommand.Execute(null);
        zoom.DisplayWidth.Should().BeApproximately(300 * CanvasZoomViewModel.DipPerMm, 0.001, "%100 = gerçek baskı boyutu");

        zoom.FitCommand.Execute(null);
        zoom.IsFit.Should().BeTrue();
    }
}

public class ThemeServiceTests
{
    [Theory]
    [InlineData(null, AppTheme.Gece)]
    [InlineData("", AppTheme.Gece)]
    [InlineData("Gece", AppTheme.Gece)]
    [InlineData("Fildisi", AppTheme.Fildisi)]
    [InlineData("bilinmeyen", AppTheme.Gece)]
    public void UnknownValuesFallBackToTheDefaultTheme(string? value, AppTheme expected) =>
        ThemeService.Parse(value).Should().Be(expected);

    [Fact]
    public void SettingRoundTrips()
    {
        ThemeService.Parse(ThemeService.ToSetting(AppTheme.Fildisi)).Should().Be(AppTheme.Fildisi);
        ThemeService.DisplayName(AppTheme.Fildisi).Should().Be("Fildişi");
    }
}
