using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NexAlbum.Core.Layout;
using NexAlbum.Core.Projects;
using NexAlbum.UI.ViewModels;
using NexAlbum.UI.Views;

namespace NexAlbum.UI.Services;

/// <summary>Görüntü çıktısı biçimi.</summary>
public enum ImageFormat
{
    Jpeg,
    Png,
}

/// <summary>Görüntü çıktısı sonucu: yazılan dosyalar.</summary>
public sealed record ImageExportResult(IReadOnlyList<string> Files);

/// <summary>
/// Sayfaları JPG/PNG olarak kaydeder (Karar 11 §4, §7.1): önizleme ve paylaşım içindir, baskı dosyası değildir. Ekrandaki
/// sayfa çizimiyle (PageView) aynı sonuç; kılavuzlar ve boş yuva ipuçları çizilmez, bitmiş ölçüye (taşma payı hariç)
/// kırpılır. Adlar: <c>ad_sayfa-001.jpg</c>, kapak <c>ad_kapak.jpg</c>. JPG'de "önizleme amaçlıdır" notu meta veriye yazılır.
/// WPF çizimi olduğundan arayüz iş parçacığında çalışır.
/// </summary>
public static class PageImageExporter
{
    /// <summary>Her çıktıda kullanıcıya ve meta veriye yazılan uyarı (Karar 11 §4.3).</summary>
    public const string PreviewNotice = "Bu dosya önizleme amaçlıdır. Baskı için PDF çıktısı kullanın.";

    /// <summary>Varsayılan çözünürlük (Karar 11 §4.2: sayfa önizleme 150 DPI).</summary>
    public const int DefaultDpi = 150;

    /// <summary>Varsayılan JPEG kalitesi (Karar 11 §4.4).</summary>
    public const int DefaultJpegQuality = 90;

    /// <summary>Kaydedilecek dosya adları: "klasör/ad_sayfa-001.jpg" …</summary>
    public static string PagePath(string basePath, int number)
    {
        var directory = Path.GetDirectoryName(basePath) ?? string.Empty;
        var name = Path.GetFileNameWithoutExtension(basePath);
        return Path.Combine(directory, string.Create(CultureInfo.InvariantCulture, $"{name}_sayfa-{number:000}{Path.GetExtension(basePath)}"));
    }

    public static string CoverImagePath(string basePath)
    {
        var directory = Path.GetDirectoryName(basePath) ?? string.Empty;
        return Path.Combine(directory, Path.GetFileNameWithoutExtension(basePath) + "_kapak" + Path.GetExtension(basePath));
    }

    public static ImageFormat FormatOf(string path) =>
        string.Equals(Path.GetExtension(path), ".png", StringComparison.OrdinalIgnoreCase) ? ImageFormat.Png : ImageFormat.Jpeg;

    /// <summary>Tüm sayfaları (ve varsa kapağı) kaydeder.</summary>
    public static ImageExportResult Export(AlbumProject project, CoverGeometry? cover, string basePath, int dpi = DefaultDpi, int jpegQuality = DefaultJpegQuality, IProgress<int>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(basePath);
        var format = FormatOf(basePath);
        var files = new List<string>();
        if (project.Cover is { } coverPage && cover is not null)
        {
            files.Add(Save(Render(new PageItemViewModel(coverPage, -1, project, cover) { ShowGuides = false }, project, dpi), CoverImagePath(basePath), format, jpegQuality));
        }

        for (var i = 0; i < project.Pages.Count; i++)
        {
            var page = new PageItemViewModel(project.Pages[i], i, project) { ShowGuides = false };
            files.Add(Save(Render(page, project, dpi), PagePath(basePath, i + 1), format, jpegQuality));
            progress?.Report(i + 1);
        }

        return new ImageExportResult(files);
    }

    /// <summary>Sayfayı bitmiş ölçüde, verilen DPI'da çizer.</summary>
    public static BitmapSource Render(PageItemViewModel page, AlbumProject project, int dpi)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentOutOfRangeException.ThrowIfLessThan(dpi, 36);
        var pxPerMm = dpi / 25.4;
        var dipPerMm = 96 / 25.4;
        page.LoadNow(project, (int)Math.Ceiling(Math.Max(page.CanvasWidth, page.CanvasHeight) * pxPerMm));

        var view = new PageView { DataContext = page };
        var size = new Size(page.CanvasWidth * dipPerMm, page.CanvasHeight * dipPerMm);
        view.Measure(size);
        view.Arrange(new Rect(size));
        view.UpdateLayout();

        var width = (int)Math.Round(page.CanvasWidth * pxPerMm);
        var height = (int)Math.Round(page.CanvasHeight * pxPerMm);
        var bitmap = new RenderTargetBitmap(width, height, dpi, dpi, PixelFormats.Pbgra32);
        bitmap.Render(view);

        // Bitmiş ölçüye kırp (taşma payı dışarıda kalır).
        var trim = page.PageArea;
        var crop = new Int32Rect(
            (int)Math.Round(trim.X * pxPerMm),
            (int)Math.Round(trim.Y * pxPerMm),
            Math.Min(width, (int)Math.Round(trim.Width * pxPerMm)),
            Math.Min(height, (int)Math.Round(trim.Height * pxPerMm)));
        crop.Width = Math.Min(crop.Width, width - crop.X);
        crop.Height = Math.Min(crop.Height, height - crop.Y);
        var result = new CroppedBitmap(bitmap, crop);
        result.Freeze();
        return result;
    }

    private static string Save(BitmapSource image, string path, ImageFormat format, int quality)
    {
        BitmapEncoder encoder;
        if (format == ImageFormat.Png)
        {
            encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
        }
        else
        {
            var metadata = new BitmapMetadata("jpg") { Comment = PreviewNotice, ApplicationName = AppInfo.Name };
            encoder = new JpegBitmapEncoder { QualityLevel = Math.Clamp(quality, 1, 100) };
            encoder.Frames.Add(BitmapFrame.Create(image, null, metadata, null));
        }

        var full = Path.GetFullPath(path);
        var temp = full + ".tmp";
        try
        {
            using (var stream = File.Create(temp))
            {
                encoder.Save(stream);
            }

            File.Move(temp, full, overwrite: true);
        }
        finally
        {
            if (File.Exists(temp))
            {
                File.Delete(temp);
            }
        }

        return full;
    }
}
