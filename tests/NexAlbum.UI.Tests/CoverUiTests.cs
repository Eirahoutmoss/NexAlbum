using System.IO;
using System.Windows.Media.Imaging;
using FluentAssertions;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Profiles;
using NexAlbum.Core.Projects;
using NexAlbum.Persistence.Packaging;
using NexAlbum.UI.Services;
using NexAlbum.UI.ViewModels;
using Xunit;

namespace NexAlbum.UI.Tests;

/// <summary>Kapak: profilden kurulur, iç sayfa gibi düzenlenir, ayrı PDF olarak çıkar.</summary>
public sealed partial class MainViewModelTests
{
    private string CoverProject(bool withCover = true)
    {
        var path = Path.Combine(dir, "kapak.albumproj");
        var project = SampleProject(4);
        var profile = project.Profile with
        {
            Bleed = new EdgeSpec(EdgesMm.Uniform(3)),
            SafeArea = new EdgeSpec(EdgesMm.Uniform(5)),
            Resolution = new ResolutionSpec(300),
            Color = new ColorSpec(ColorMode.Cmyk),
            Verified = true,
            Cover = withCover ? new CoverSpec(CoverType.Hardcover, new SizeMm(300, 300), null, new SpineSpec(14)) : null,
        };
        project = project.WithProfile(profile).WithPages(
        [
            new AlbumPage { Id = "p1", Elements = [new Slot { Id = "s", Bounds = new RectMm(20, 20, 100, 100) }] },
            new AlbumPage { Id = "p2", Elements = [new Slot { Id = "s", Bounds = new RectMm(20, 20, 100, 100) }] },
        ]);
        ProjectPackageWriter.Save(path, project, new SaveOptions { AppVersion = project.AppVersion });
        return path;
    }

    [Fact]
    public async Task CoverIsCreatedEditedAndExportedAsItsOwnPdf()
    {
        var path = CoverProject();
        var dialogs = new FakeDialogs { SavePath = path, PdfPath = Path.Combine(dir, "album.pdf") };
        using var vm = new MainViewModel(dialogs, settings: new MemoryAppSettingsStore());
        vm.History.MergeWindow = TimeSpan.Zero;
        vm.Open(path);
        vm.HasCover.Should().BeFalse();
        vm.CoverInfo.Should().Contain("sırt 14");

        vm.CreateCover();

        vm.HasCover.Should().BeTrue();
        vm.IsCoverSelected.Should().BeTrue();
        vm.StripSelectedPage.Should().BeNull("şeritte seçim yok");
        vm.ShowSingleCanvas.Should().BeTrue();
        vm.SelectedPageInfo.Should().StartWith("Kapak");
        vm.SelectedPage!.FoldLines.Should().HaveCount(2);
        vm.SelectedPage.CanvasWidth.Should().BeApproximately(614 + 6, 1e-9, "arka + sırt + ön + taşma payı");
        vm.DeletePageCommand.CanExecute(null).Should().BeFalse("kapak sayfa gibi silinmez");
        vm.DuplicatePageCommand.CanExecute(null).Should().BeFalse();

        // Örnek fotoğrafların dosyası yok (ön kontrolde hata olmasın diye sayfalarda kullanılmaz).
        // Kapaktaki başlık iç sayfadaki gibi düzenlenir; kapak seçili kalır.
        vm.SelectText(vm.SelectedPage.Texts.Single());
        vm.TextContent = "Bizim Yaz";
        vm.Project!.Cover!.Elements.OfType<TextElement>().Single().Content.Should().Be("Bizim Yaz");
        vm.IsCoverSelected.Should().BeTrue();
        vm.Pages.Should().HaveCount(2, "kapak sayfa sayısına girmez");

        // Şeritten sayfaya geç, kapak kutusundan geri dön.
        vm.StripSelectedPage = vm.Pages[1];
        vm.IsCoverSelected.Should().BeFalse();
        vm.StripSelectedPage = null;
        vm.SelectedPage.Should().BeSameAs(vm.Pages[1], "şeritten gelen boş seçim sayfayı bırakmaz");
        vm.ShowCoverCommand.Execute(null);
        vm.IsCoverSelected.Should().BeTrue();

        // Yeniden oluşturmak başlığı korur.
        vm.CreateCover();
        vm.Project!.Cover!.Elements.OfType<TextElement>().Single().Content.Should().Be("Bizim Yaz");

        await vm.ExportPdfAsync();
        File.Exists(Path.Combine(dir, "album.pdf")).Should().BeTrue();
        File.Exists(Path.Combine(dir, "album_kapak.pdf")).Should().BeTrue();
        vm.Status.Should().Contain("kapak");

        vm.Save(saveAs: false).Should().BeTrue();
        ProjectPackageReader.Load(path).Project.Cover!.Elements.OfType<TextElement>().Single().Content.Should().Be("Bizim Yaz");

        vm.RemoveCover();
        vm.HasCover.Should().BeFalse();
        vm.SelectedPage.Should().NotBeNull();
        vm.UndoCommand.Execute(null);
        vm.HasCover.Should().BeTrue();
    }

    [Fact]
    public void CoverNeedsProfileValues()
    {
        var dialogs = new FakeDialogs();
        using var vm = new MainViewModel(dialogs, settings: new MemoryAppSettingsStore());
        vm.Open(CoverProject(withCover: false));

        vm.CreateCover();

        vm.HasCover.Should().BeFalse();
        vm.Status.Should().Contain("Kapak oluşturulamadı");
        vm.CoverInfo.Should().Contain("Profilde kapak yok");
    }

    [Fact]
    public void ProfileFormReadsCoverValues()
    {
        var profile = SampleProject(0).Profile;
        var form = new ProfileForm(profile)
        {
            CoverType = ProfileForm.CoverTypes.Single(o => o.Type == CoverType.Hardcover),
            FrontWidth = "305",
            FrontHeight = "305",
            SpineWidth = "12,5",
        };

        var built = form.TryBuild(DateTimeOffset.UnixEpoch, out var errors, out _);

        errors.Should().BeEmpty();
        built!.Cover.Should().Be(new CoverSpec(CoverType.Hardcover, new SizeMm(305, 305), null, new SpineSpec(12.5)));
        new ProfileForm(built).SpineWidth.Should().Be(12.5.ToString("0.###", System.Globalization.CultureInfo.CurrentCulture));

        form.FrontHeight = string.Empty;
        form.TryBuild(DateTimeOffset.UnixEpoch, out var missing, out _).Should().BeNull();
        missing.Should().Contain(e => e.Contains("Ön kapak", StringComparison.Ordinal));
    }
}

/// <summary>Görüntü çıktısı WPF ile çizer: STA iş parçacığında, uygulama kaynaklarıyla.</summary>
[Collection(WpfApp.Collection)]
public sealed class ImageExportTests : IDisposable
{
    private readonly string dir = Path.Combine(Path.GetTempPath(), "nexalbum-image-tests", Guid.NewGuid().ToString("N"));

    public ImageExportTests() => Directory.CreateDirectory(dir);

    public void Dispose()
    {
        try
        {
            Directory.Delete(dir, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    public void PagesAndCoverAreSavedAsTrimmedPreviews() => WpfApp.Run(() =>
    {
        var now = new DateTimeOffset(2026, 10, 12, 10, 0, 0, TimeSpan.Zero);
        var profile = BuiltInProfiles.Find("builtin-30x30")! with
        {
            Bleed = new EdgeSpec(EdgesMm.Uniform(3)),
            Cover = new CoverSpec(CoverType.Hardcover, new SizeMm(300, 300), null, new SpineSpec(14)),
        };
        var project = AlbumProject.Create("Tatil", profile, now, "0.1.0", Guid.NewGuid()).WithPages(
        [
            new AlbumPage { Id = "p1", Background = BackgroundFill.Solid(Core.Common.RgbaColor.Parse("#336699")) },
            new AlbumPage { Id = "p2" },
        ]);
        var geometry = Core.Layout.CoverLayout.Geometry(profile, out _)!;
        project = project with { Cover = Core.Layout.CoverLayout.Create(project, geometry, now) };

        var basePath = Path.Combine(dir, "tatil.jpg");
        var result = PageImageExporter.Export(project, geometry, basePath, dpi: 72);

        result.Files.Select(Path.GetFileName).Should().Equal("tatil_kapak.jpg", "tatil_sayfa-001.jpg", "tatil_sayfa-002.jpg");
        var page = BitmapFrame.Create(new Uri(result.Files[1]), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        page.PixelWidth.Should().BeCloseTo((int)Math.Round(300 / 25.4 * 72), 1, "bitmiş ölçü (taşma payı hariç)");
        ((BitmapMetadata)page.Metadata).Comment.Should().Be(PageImageExporter.PreviewNotice);
        var cover = BitmapFrame.Create(new Uri(result.Files[0]), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        cover.PixelWidth.Should().BeCloseTo((int)Math.Round(614 / 25.4 * 72), 1, "arka + sırt + ön");

        // Sayfa rengi çizilmiş olmalı (boş beyaz değil).
        var pixels = new byte[4];
        new FormatConvertedBitmap(page, System.Windows.Media.PixelFormats.Bgra32, null, 0).CopyPixels(new System.Windows.Int32Rect(page.PixelWidth / 2, page.PixelHeight / 2, 1, 1), pixels, 4, 0);
        pixels[0].Should().BeGreaterThan(pixels[2], "mavi ağırlıklı fon");

        var png = PageImageExporter.Export(project, null, Path.Combine(dir, "tatil.png"), dpi: 72);
        png.Files.Should().HaveCount(2, "geometri verilmezse kapak atlanır").And.OnlyContain(f => f.EndsWith(".png", StringComparison.Ordinal));
    });
}
