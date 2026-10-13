using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FluentAssertions;
using NexAlbum.Assets.Import;
using NexAlbum.Core.Layout;
using NexAlbum.Core.Profiles;
using NexAlbum.Core.Projects;
using NexAlbum.Core.Templates;
using NexAlbum.Persistence.Packaging;
using NexAlbum.UI.Services;
using NexAlbum.UI.Views;
using Xunit;

namespace NexAlbum.UI.Tests;

/// <summary>
/// Arayüzün iki temada gerçekten çizildiğini denetler (düzen, şablonlar, bağlamalar ölçüm sırasında hata vermemeli).
/// <c>NEXALBUM_EKRAN_KLASORU</c> ortam değişkeni verilirse görüntüler PNG olarak oraya yazılır
/// (<c>.github/workflows/screenshots.yml</c> bunları <c>ekran/&lt;dal&gt;</c> dalına koyar).
/// </summary>
[Collection(WpfApp.Collection)]
public sealed class ScreenshotTests : IDisposable
{
    private readonly string dir = Path.Combine(Path.GetTempPath(), "nexalbum-ekran", Guid.NewGuid().ToString("N"));
    private readonly string? output = Environment.GetEnvironmentVariable("NEXALBUM_EKRAN_KLASORU");

    public ScreenshotTests()
    {
        Directory.CreateDirectory(dir);
    }

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
    public void MainScreensRenderInBothThemes()
    {
        WpfApp.Run(() =>
        {
            try
            {
                ThemeService.Apply(AppTheme.Gece);
                var now = DateTimeOffset.Now;
                var store = new MemoryAppSettingsStore(new AppSettings { RecentProjects = RecentSamples(now) });
                var main = Show(new MainWindow(store), 1440, 900);

                Capture(main, "01-baslangic-gece").Should().BeTrue();

                main.ViewModel.Open(SampleProject(now));
                main.ViewModel.HasProject.Should().BeTrue();
                main.ViewModel.Pages.Should().NotBeEmpty("örnek albüm otomatik oluşturuldu");
                var vm = main.ViewModel;
                vm.SelectedPage = vm.Pages[Math.Min(1, vm.Pages.Count - 1)];
                ShowGeometricPage(vm);
                vm.ToggleFavorite(vm.Assets[0]);
                vm.ToggleFavorite(vm.Assets[4]);
                vm.SelectedAsset = vm.Assets[2];
                Settle(main);
                Capture(main, "02-calisma-gece").Should().BeTrue();

                var tabs = Find<TabControl>(main);
                tabs.SelectedIndex = 2;
                vm.SelectedTemplateStyle = vm.TemplateStyles.Single(t => t.Style == ViewModels.TemplateStyle.Geometric);
                Settle(main);
                Capture(main, "03-sablonlar-gece").Should().BeTrue();

                vm.SelectedTemplateStyle = vm.TemplateStyles[0];
                tabs.SelectedIndex = 1;
                vm.IsBookView = true;
                Settle(main);
                All<ItemsControl>(main).First(c => c.Items.OfType<ViewModels.FrameItemViewModel>().Any(f => f.Frame.OverridesShape)).BringIntoView();
                Settle(main);
                Capture(main, "04-kitap-gece").Should().BeTrue();

                vm.Palette.Open();
                vm.Palette.Query = "pdf";
                vm.Palette.Query = string.Empty;
                Settle(main);
                Capture(main, "05-komut-paleti-gece").Should().BeTrue();
                vm.Palette.Close();

                ThemeService.Apply(AppTheme.Fildisi);
                tabs.SelectedIndex = 2;
                Settle(main);
                Capture(main, "06-calisma-fildisi").Should().BeTrue();

                vm.IsBookView = false;
                tabs.SelectedIndex = 0;
                vm.GalleryFilter = ViewModels.GalleryFilter.Unused;
                vm.Zoom.SetPercent(100);
                Settle(main);
                Capture(main, "07-yakinlastirma-fildisi").Should().BeTrue();

                ThemeService.Apply(AppTheme.Gece);
                vm.GalleryFilter = ViewModels.GalleryFilter.All;
                vm.AddPageCommand.Execute(null);
                vm.AddText(Core.Projects.TextPresets.Find("names"));
                vm.TextContent = "Ayşe & Mert";
                vm.AddText(Core.Projects.TextPresets.Find("title"));
                vm.AddText(Core.Projects.TextPresets.Find("date"));
                vm.AddText(Core.Projects.TextPresets.Find("quote"));
                vm.Zoom.FitCommand.Execute(null);
                Settle(main);
                vm.SelectedPage!.Texts.Should().HaveCount(4);
                Capture(main, "12-metin-gece").Should().BeTrue();

                // Fotoğraf üstünde yazı: koyu yazıya hale, açık yazıya bant; ardından yuvada konumlandırma kipi.
                vm.SelectedPage = vm.Pages.First(p => p.PhotoCount >= 2);
                vm.AddText(Core.Projects.TextPresets.Find("title"));
                vm.TextContent = "Özel Anlar";
                vm.TextSize = 72;
                vm.SelectedTextEffect = vm.TextEffectOptions.Single(o => o.Kind == Core.Projects.TextEffectKind.Glow);
                vm.TextEffectStrength = 95;
                vm.AddText(Core.Projects.TextPresets.Find("date"));
                vm.TextColorHex = "#FFFFFF";
                vm.TextLetterSpacing = 30;
                vm.SelectedTextEffect = vm.TextEffectOptions.Single(o => o.Kind == Core.Projects.TextEffectKind.Band);
                vm.ApplyTextEffectColorCommand.Execute(vm.TextEffectColors.Single(c => c.Name == "Mürekkep"));
                vm.TextEffectStrength = 55;
                Settle(main);
                Capture(main, "13-yazi-etkileri-gece").Should().BeTrue();

                var photoSlot = vm.SelectedPage!.Slots.OrderByDescending(x => x.Width * x.Height).First(x => !x.IsEmpty);
                vm.SelectSlot(photoSlot);
                vm.ZoomSlotAt(vm.SelectedSlot!, 1.8, photoSlot.Width * 0.7, photoSlot.Height * 0.4);
                vm.IsPositioning = true;
                tabs.SelectedIndex = 0;
                Settle(main);
                vm.IsPositioning.Should().BeTrue();
                Capture(main, "14-konumlandirma-gece").Should().BeTrue();
                vm.IsPositioning = false;

                // Fotoğraf ayarları: sıcak, pozlamalı ve hafif düzeltilmiş; panel görünür.
                vm.AdjustExposure = 25;
                vm.AdjustTemperature = 45;
                vm.AdjustContrast = 20;
                vm.Straighten = 3;
                Settle(main);
                All<TextBlock>(main).First(t => t.Text.StartsWith("Orijinal dosya değişmez", StringComparison.Ordinal)).BringIntoView();
                Settle(main);
                Capture(main, "17-fotograf-ayarlari-gece").Should().BeTrue();

                // Akıllı hizalama: başlık sürüklenirken sayfa ortasına yapışır, kılavuz çizgisi görünür.
                var title = vm.SelectedPage!.Texts[0];
                vm.DragText(title, title.X, title.Y, 1.2, 0, toleranceMm: 3);
                Settle(main);
                vm.SelectedPage!.SnapLines.Should().NotBeEmpty();
                Capture(main, "18-hizalama-gece").Should().BeTrue();
                vm.EndTextDrag();

                vm.DetectStory();
                vm.Chapters.Should().HaveCount(4);
                vm.SelectedChapter = vm.Chapters[1];
                Settle(main);
                All<ListBox>(main).First(l => ReferenceEquals(l.ItemsSource, vm.Chapters)).BringIntoView();
                Settle(main);
                Capture(main, "15-hikaye-gece").Should().BeTrue();
                vm.IsFilterAll = true;

                // Hikâye filmi penceresi: dikey Reels, açılış başlığı sonrasında bir fotoğraf karesi.
                using (var film = vm.CreateFilm()!)
                {
                    film.SelectedStyle = Core.Video.VideoStyle.Find("cinematic");
                    film.PreviewSeconds = 4.2;
                    var filmWindow = Show(new VideoWindow(film), 1320, 840);
                    film.RenderPreviewNow();
                    Settle(filmWindow);
                    Capture(filmWindow, "16-film-gece").Should().BeTrue();
                    filmWindow.Close();
                }

                var show = new PresentationWindow(new ViewModels.PresentationViewModel(vm.Project!, vm.SelectedPage!.Index, spreads: true)) { WindowState = WindowState.Normal };
                Capture(Show(show, 1440, 900), "08-sunum").Should().BeTrue();
                show.Hide();

                Capture(Show(new AboutWindow(), 540, double.NaN), "09-hakkinda").Should().BeTrue();
                Capture(Show(new NewProjectWindow(), 540, double.NaN), "10-yeni-proje").Should().BeTrue();
                Capture(Show(new SettingsWindow(new AppSettings()), 640, double.NaN), "11-ayarlar").Should().BeTrue();

                // Kapatmak "kaydedilsin mi?" sorabilir (test kilitlenir); gizleyip proje kilidini bırakmak yeterli.
                main.ViewModel.Dispose();
                foreach (var window in Application.Current.Windows.OfType<Window>().ToList())
                {
                    window.Hide();
                }
            }
            finally
            {
                ThemeService.Apply(AppTheme.Gece);
            }
        });
    }

    /// <summary>
    /// Fotoğrafı en çok olan sayfaya "Karşılıklı Üçgenler + Üçlü" uygulanır: hipotenüsleri karşılıklı iki üçgen ve
    /// yanında üç dikdörtgen (geometrik şablonların tipik örneği).
    /// </summary>
    private static void ShowGeometricPage(ViewModels.MainViewModel vm)
    {
        vm.SelectedPage = vm.Pages.OrderByDescending(p => p.PhotoCount).ThenBy(p => p.Index).First();
        vm.SelectedTemplateCount = vm.TemplateCounts[0];
        vm.SelectedTemplateStyle = vm.TemplateStyles.Single(t => t.Style == ViewModels.TemplateStyle.Geometric);
        var template = vm.Templates.Single(t => t.Id == "geo-triangles-3");
        vm.ApplyTemplateCommand.Execute(template);
        vm.SelectedTemplateStyle = vm.TemplateStyles[0];
        vm.Project!.Pages[vm.SelectedPage!.Index].TemplateId.Should().Be("geo-triangles-3");
    }

    private static IEnumerable<T> All<T>(DependencyObject root)
        where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
            {
                yield return match;
            }

            foreach (var nested in All<T>(child))
            {
                yield return nested;
            }
        }
    }

    private static T Find<T>(DependencyObject root)
        where T : DependencyObject =>
        FindOrNull<T>(root) ?? throw new InvalidOperationException(typeof(T).Name + " bulunamadı.");

    private static T? FindOrNull<T>(DependencyObject root)
        where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
            {
                return match;
            }

            if (FindOrNull<T>(child) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    private static T Show<T>(T window, double width, double height)
        where T : Window
    {
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = -20000;
        window.Top = -20000;
        window.ShowActivated = false;
        window.ShowInTaskbar = false;
        window.Width = width;
        if (!double.IsNaN(height))
        {
            window.Height = height;
        }

        window.Show();
        Settle(window);
        return window;
    }

    /// <summary>Düzen ve arka planda yüklenen küçük resimler otursun.</summary>
    private static void Settle(Window window)
    {
        window.UpdateLayout();
        WpfApp.Pump(TimeSpan.FromMilliseconds(1500));
        window.UpdateLayout();
    }

    /// <summary>Pencereyi çizer; klasör verildiyse PNG yazar. Çizim boş değilse true.</summary>
    private bool Capture(Window window, string name)
    {
        var width = (int)Math.Ceiling(window.ActualWidth);
        var height = (int)Math.Ceiling(window.ActualHeight);
        if (width <= 0 || height <= 0)
        {
            return false;
        }

        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window);
        bitmap.Freeze();
        if (output is not null)
        {
            Directory.CreateDirectory(output);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var file = File.Create(Path.Combine(output, name + ".png"));
            encoder.Save(file);
        }

        return true;
    }

    private List<RecentProject> RecentSamples(DateTimeOffset now)
    {
        string Touch(string name)
        {
            var path = Path.Combine(dir, name + ".albumproj");
            File.WriteAllText(path, string.Empty);
            return path;
        }

        return
        [
            new(Touch("Yaz Düğünü 2026"), "Yaz Düğünü 2026", now.AddHours(-2)),
            new(Touch("Nişan — Kapadokya"), "Nişan — Kapadokya", now.AddDays(-1)),
            new(Touch("Dış Çekim — Ege"), "Dış Çekim — Ege", now.AddDays(-4)),
        ];
    }

    /// <summary>Gerçek JPEG'lerle (sıcak tonlu degrade "fotoğraflar") otomatik oluşturulmuş bir albüm kaydeder.</summary>
    private string SampleProject(DateTimeOffset now)
    {
        var photos = Path.Combine(dir, "fotograflar");
        Directory.CreateDirectory(photos);
        var files = new List<string>();
        for (var i = 0; i < 18; i++)
        {
            var path = Path.Combine(photos, string.Format(CultureInfo.InvariantCulture, "DSC_{0:0000}.jpg", 1000 + i));
            WriteSamplePhoto(path, i);
            files.Add(path);
        }

        var project = AlbumProject.Create("Aile Albümü 2026", BuiltInProfiles.Find("builtin-30x30")!, now, "0.1.0", Guid.Parse("6d1f8a3e-0000-4000-8000-000000000001"));
        project = new PhotoImporter().ImportFiles(files, project).ApplyTo(project);

        // Düğün günü akışı: örnek dosyalarda EXIF yok; çekim saatleri hikâye sekmesi için verilir.
        var day = new DateTimeOffset(2026, 6, 13, 0, 0, 0, TimeSpan.FromHours(3));
        double[] starts = [9.25, 13, 16.5, 21];
        int[] sizes = [5, 4, 4, 5];
        var times = new List<DateTimeOffset>();
        for (var c = 0; c < starts.Length; c++)
        {
            for (var k = 0; k < sizes[c]; k++)
            {
                times.Add(day.AddHours(starts[c]).AddMinutes(k * 6));
            }
        }

        project = project with { Assets = [.. project.Assets.Select((a, i) => a with { CapturedAt = times[Math.Min(i, times.Count - 1)] })] };
        var layout = new AutoLayoutEngine().Layout(LayoutRequest.ForProject(project, BuiltInTemplates.All, new LayoutOptions { Mode = LayoutMode.Balanced }, now));
        project = project.WithPages(layout.Pages);

        var file = Path.Combine(dir, "Aile Albümü 2026.albumproj");
        ProjectPackageWriter.Save(file, project, new SaveOptions { AppVersion = "0.1.0" });
        return file;
    }

    private static readonly (Color Top, Color Bottom, Color Glow)[] Palettes =
    [
        (Color.FromRgb(0xF2, 0xB8, 0x7A), Color.FromRgb(0x5A, 0x3B, 0x2E), Color.FromRgb(0xFF, 0xE3, 0xB0)),
        (Color.FromRgb(0xE9, 0xDF, 0xD3), Color.FromRgb(0x9C, 0x8C, 0x7C), Color.FromRgb(0xFF, 0xFF, 0xFF)),
        (Color.FromRgb(0x9F, 0xB7, 0xA3), Color.FromRgb(0x2F, 0x45, 0x3A), Color.FromRgb(0xE8, 0xF0, 0xD8)),
        (Color.FromRgb(0xC9, 0xA2, 0xA0), Color.FromRgb(0x4A, 0x2E, 0x36), Color.FromRgb(0xFF, 0xD9, 0xD2)),
        (Color.FromRgb(0x8F, 0xA9, 0xC4), Color.FromRgb(0x23, 0x30, 0x45), Color.FromRgb(0xF6, 0xD6, 0xA8)),
        (Color.FromRgb(0xD8, 0xC0, 0x8A), Color.FromRgb(0x5E, 0x4A, 0x22), Color.FromRgb(0xFF, 0xF1, 0xC8)),
    ];

    private static void WriteSamplePhoto(string path, int index)
    {
        var portrait = index % 3 == 1;
        var (width, height) = portrait ? (800, 1200) : (1200, 800);
        var palette = Palettes[index % Palettes.Length];
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(new LinearGradientBrush(palette.Top, palette.Bottom, 90), null, new Rect(0, 0, width, height));
            var glow = new RadialGradientBrush(Color.FromArgb(0xCC, palette.Glow.R, palette.Glow.G, palette.Glow.B), Color.FromArgb(0, palette.Glow.R, palette.Glow.G, palette.Glow.B));
            dc.DrawEllipse(glow, null, new Point(width * (0.25 + (0.1 * (index % 5))), height * 0.38), width * 0.45, width * 0.45);
            for (var b = 0; b < 7; b++)
            {
                var x = width * (((index * 37) + (b * 53)) % 100) / 100.0;
                var y = height * (((index * 17) + (b * 29)) % 60) / 100.0;
                var r = 18 + ((index + b) % 4 * 14);
                dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xF4, 0xDC)), null, new Point(x, y), r, r);
            }

            // Ufuk ve iki siluet: fotoğraf hissi için yalın biçimler
            var ground = new SolidColorBrush(Color.FromArgb(0x99, 0x1A, 0x14, 0x10));
            dc.DrawRectangle(ground, null, new Rect(0, height * 0.78, width, height * 0.22));
            var cx = width * (0.4 + (0.05 * (index % 4)));
            var figure = new SolidColorBrush(Color.FromArgb(0xD0, 0x1C, 0x16, 0x14));
            dc.DrawEllipse(figure, null, new Point(cx, height * 0.5), height * 0.035, height * 0.035);
            dc.DrawRoundedRectangle(figure, null, new Rect(cx - (height * 0.05), height * 0.54, height * 0.1, height * 0.26), 20, 20);
            var bride = new SolidColorBrush(Color.FromArgb(0xE6, 0xF7, 0xF2, 0xEA));
            dc.DrawEllipse(bride, null, new Point(cx + (height * 0.11), height * 0.52), height * 0.033, height * 0.033);
            var dress = new StreamGeometry();
            using (var g = dress.Open())
            {
                g.BeginFigure(new Point(cx + (height * 0.08), height * 0.56), true, true);
                g.LineTo(new Point(cx + (height * 0.14), height * 0.56), true, false);
                g.LineTo(new Point(cx + (height * 0.2), height * 0.8), true, false);
                g.LineTo(new Point(cx + (height * 0.02), height * 0.8), true, false);
            }

            dc.DrawGeometry(bride, null, dress);
        }

        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new JpegBitmapEncoder { QualityLevel = 88 };
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(path);
        encoder.Save(file);
    }
}
