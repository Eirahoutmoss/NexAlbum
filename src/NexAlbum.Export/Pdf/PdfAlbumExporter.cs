using System.Globalization;
using NexAlbum.Core.Common;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Projects;
using NexAlbum.Core.Validation;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace NexAlbum.Export.Pdf;

/// <summary>PDF çıktısının sonucu.</summary>
public sealed record PdfExportResult(IReadOnlyList<string> Files, int PageCount, ValidationResult Issues);

/// <summary>
/// Baskı PDF'i (Karar 11 §3). Sayfa = bitmiş ölçü + taşma payı (+ kesim işaretleri); TrimBox ve BleedBox yazılır.
/// Metin için yazı tipi çözümleyicisi uygulama tarafından <c>GlobalFontSettings.FontResolver</c> ile verilir;
/// çözülemeyen metin atlanır ve uyarı olarak raporlanır.
/// JPEG'ler yeniden sıkıştırılmadan, kaynak çözünürlükte gömülür. Değer uydurulmaz: profilde taşma payı yoksa
/// sıfır kullanılır ve uyarı verilir; ICC profili gömülemiyorsa açıkça bildirilir.
/// </summary>
public static class PdfAlbumExporter
{
    private const double PointsPerMm = 72 / 25.4;

    /// <summary>Kesim işareti kalınlığı (Karar 11 §3.5: 0,25 pt).</summary>
    private const double MarkThicknessPt = 0.25;

    public static PdfExportResult Export(
        AlbumProject project,
        string path,
        PdfExportSettings? settings = null,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        settings ??= PdfExportSettings.FromProfile(project.Profile);

        var pages = PageSelection.Parse(settings.Pages, project.Pages.Count);
        var issues = new List<ValidationIssue>();
        var bleed = settings.IncludeBleed ? project.Profile.Bleed.Value : null;
        if (settings.IncludeBleed && bleed is null)
        {
            issues.Add(new("export.bleed.missing", ValidationSeverity.Warning, "Profilde taşma payı yok; PDF taşma payı olmadan üretildi."));
        }

        if (project.Profile.Color.IccProfile is not null)
        {
            issues.Add(new("export.icc.not-embedded", ValidationSeverity.Info,
                "ICC profili PDF'e ayrıca gömülmez; fotoğraflar kendi renk profilleriyle gömülür (Karar 11 §3.6). Baskı evinizle teyit edin."));
        }

        var geometry = SheetGeometry.Create(project.PageSize, bleed, settings.IncludeCropMarks, settings.CropMarkOffsetMm, settings.CropMarkLengthMm);
        var assets = project.Assets.ToDictionary(a => a.Id, StringComparer.Ordinal);
        using var images = new ImageCache();
        var files = new List<string>();

        PdfDocument? document = null;
        try
        {
            for (var i = 0; i < pages.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var number = pages[i];
                document ??= NewDocument(project, settings);
                DrawPage(document.AddPage(), project.Pages[number - 1], number, geometry, assets, images, issues);

                if (settings.SeparateFiles)
                {
                    files.Add(SaveDocument(document, NumberedPath(path, number)));
                    document.Dispose();
                    document = null;
                }

                progress?.Report(i + 1);
            }

            if (!settings.SeparateFiles && document is not null)
            {
                files.Add(SaveDocument(document, path));
            }
        }
        finally
        {
            document?.Dispose();
        }

        return new PdfExportResult(files, pages.Count, new ValidationResult(issues));
    }

    /// <summary>"album.pdf" → "album_003.pdf".</summary>
    public static string NumberedPath(string path, int pageNumber)
    {
        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        var name = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);
        return Path.Combine(directory, string.Create(CultureInfo.InvariantCulture, $"{name}_{pageNumber:000}{extension}"));
    }

    private static PdfDocument NewDocument(AlbumProject project, PdfExportSettings settings)
    {
        var document = new PdfDocument();

        // JPEG'ler olduğu gibi (DCTDecode) gömülür; ek sıkıştırma ya da yeniden kodlama yok (Karar 11 §3.2).
        document.Options.UseFlateDecoderForJpegImages = PdfUseFlateDecoderForJpegImages.Never;
        document.Info.Title = project.Name;
        document.Info.Creator = settings.Creator;
        document.Info.Subject = string.Create(CultureInfo.InvariantCulture, $"{project.PageSize.Width:0.##}×{project.PageSize.Height:0.##} mm");
        return document;
    }

    /// <summary>Geçici dosyaya yazıp yerine taşır; yarım PDF kalmaz.</summary>
    private static string SaveDocument(PdfDocument document, string path)
    {
        var full = Path.GetFullPath(path);
        var temp = full + ".tmp";
        try
        {
            document.Save(temp);
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

    private static void DrawPage(
        PdfPage page,
        AlbumPage albumPage,
        int number,
        SheetGeometry geometry,
        Dictionary<string, PhotoAsset> assets,
        ImageCache images,
        List<ValidationIssue> issues)
    {
        page.Width = XUnit.FromMillimeter(geometry.Media.Width);
        page.Height = XUnit.FromMillimeter(geometry.Media.Height);
        page.TrimBox = PdfBox(geometry.Trim, geometry.Media);
        page.BleedBox = PdfBox(geometry.Bleed, geometry.Media);

        using var gfx = XGraphics.FromPdfPage(page);

        // Kökeni bitmiş sayfanın sol üst köşesine al; birim mm.
        gfx.TranslateTransform(geometry.Trim.X * PointsPerMm, geometry.Trim.Y * PointsPerMm);
        gfx.ScaleTransform(PointsPerMm);

        var bleedArea = new RectMm(geometry.Bleed.X - geometry.Trim.X, geometry.Bleed.Y - geometry.Trim.Y, geometry.Bleed.Width, geometry.Bleed.Height);
        Fill(gfx, albumPage.Background, bleedArea, albumPage, number, issues);

        foreach (var element in albumPage.ElementsInDrawOrder)
        {
            switch (element)
            {
                case BackgroundElement background:
                    Fill(gfx, background.Fill, bleedArea, albumPage, number, issues);
                    break;
                case Slot slot when slot.Placement is { } placement:
                    DrawPhoto(gfx, slot, placement, assets, images, albumPage, number, issues);
                    break;
                case TextElement text:
                    DrawText(gfx, text, albumPage, number, issues);
                    break;
            }
        }

        // Kesim işaretleri: mm dönüşümü dışında, sabit 0,25 pt kalınlıkla.
        gfx.ScaleTransform(1 / PointsPerMm);
        gfx.TranslateTransform(-geometry.Trim.X * PointsPerMm, -geometry.Trim.Y * PointsPerMm);
        var pen = new XPen(XColors.Black, MarkThicknessPt);
        foreach (var m in geometry.CropMarks)
        {
            gfx.DrawLine(pen, m.X1 * PointsPerMm, m.Y1 * PointsPerMm, m.X2 * PointsPerMm, m.Y2 * PointsPerMm);
        }
    }

    private static void Fill(XGraphics gfx, BackgroundFill fill, RectMm area, AlbumPage page, int number, List<ValidationIssue> issues)
    {
        switch (fill.Mode)
        {
            case BackgroundMode.Solid when fill.Color is { } c:
                gfx.DrawRectangle(new XSolidBrush(ToXColor(c, fill.Opacity)), area.X, area.Y, area.Width, area.Height);
                break;
            case BackgroundMode.Gradient when fill.Gradient is { } g:
                var brush = new XLinearGradientBrush(
                    new XPoint(area.X, area.Y),
                    new XPoint(area.Right, area.Bottom),
                    ToXColor(g.From, fill.Opacity),
                    ToXColor(g.To, fill.Opacity));
                gfx.DrawRectangle(brush, area.X, area.Y, area.Width, area.Height);
                break;
            case BackgroundMode.Image:
                issues.Add(new("export.background-image.unsupported", ValidationSeverity.Warning,
                    Format("Sayfa {0}: fotoğraflı arka plan bu sürümde PDF'e yazılmıyor.", number), page.Id));
                break;
        }
    }

    private static void DrawPhoto(
        XGraphics gfx,
        Slot slot,
        PhotoPlacement placement,
        Dictionary<string, PhotoAsset> assets,
        ImageCache images,
        AlbumPage page,
        int number,
        List<ValidationIssue> issues)
    {
        if (!assets.TryGetValue(placement.AssetId, out var asset))
        {
            issues.Add(new("export.photo.unknown", ValidationSeverity.Error, Format("Sayfa {0}: yuva projede olmayan bir fotoğrafa bağlı.", number), page.Id, slot.Id));
            return;
        }

        var image = images.Get(asset, out var problem);
        if (image is null)
        {
            issues.Add(new("export.photo.unreadable", ValidationSeverity.Error,
                Format("Sayfa {0}: {1} PDF'e eklenemedi: {2}", number, asset.FileName, problem ?? "bilinmeyen hata"), page.Id, slot.Id));
            return;
        }

        var orientation = OrientationTransform.FromExif(asset.ExifOrientation);
        var rect = ImageFit.FullImageRect(slot.Bounds, asset.DisplayAspectRatio, placement, slot.CropMode);

        var state = gfx.Save();
        gfx.IntersectClip(new XRect(slot.Bounds.X, slot.Bounds.Y, slot.Bounds.Width, slot.Bounds.Height));
        gfx.TranslateTransform(rect.X + (rect.Width / 2), rect.Y + (rect.Height / 2));
        if (placement.Rotation != 0)
        {
            gfx.RotateTransform(placement.Rotation);
        }

        gfx.RotateTransform(orientation.RotationDegrees);
        if (orientation.MirrorX)
        {
            gfx.ScaleTransform(-1, 1);
        }

        var (w, h) = orientation.SwapsAxes ? (rect.Height, rect.Width) : (rect.Width, rect.Height);
        gfx.DrawImage(image, -w / 2, -h / 2, w, h);
        gfx.Restore(state);
    }

    private static void DrawText(XGraphics gfx, TextElement text, AlbumPage page, int number, List<ValidationIssue> issues)
    {
        try
        {
            var style = (text.Font.Weight >= 600, text.Font.Italic) switch
            {
                (true, true) => XFontStyleEx.BoldItalic,
                (true, false) => XFontStyleEx.Bold,
                (false, true) => XFontStyleEx.Italic,
                _ => XFontStyleEx.Regular,
            };

            // Punto → mm (çizim birimi mm).
            var font = new XFont(text.Font.Family, text.Font.Size * 25.4 / 72, style);
            var format = new XStringFormat
            {
                Alignment = text.Alignment switch
                {
                    TextAlignment.Center => XStringAlignment.Center,
                    TextAlignment.Right => XStringAlignment.Far,
                    _ => XStringAlignment.Near,
                },
                LineAlignment = text.VerticalAlignment switch
                {
                    VerticalAlignment.Middle => XLineAlignment.Center,
                    VerticalAlignment.Bottom => XLineAlignment.Far,
                    _ => XLineAlignment.Near,
                },
            };
            var b = text.Bounds;
            gfx.DrawString(text.Content, font, new XSolidBrush(ToXColor(text.Font.Color, 1)), new XRect(b.X, b.Y, b.Width, b.Height), format);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or NotSupportedException)
        {
            issues.Add(new("export.text.font-unavailable", ValidationSeverity.Warning,
                Format("Sayfa {0}: metin yazılamadı (yazı tipi: {1}).", number, text.Font.Family), page.Id, text.Id));
        }
    }

    /// <summary>mm dikdörtgeni PDF kutusuna çevirir (PDF kökeni sol alt köşedir).</summary>
    private static PdfRectangle PdfBox(RectMm r, SizeMm media) => new(new XRect(
        r.X * PointsPerMm,
        (media.Height - r.Bottom) * PointsPerMm,
        r.Width * PointsPerMm,
        r.Height * PointsPerMm));

    private static XColor ToXColor(RgbaColor c, double opacity) =>
        XColor.FromArgb((int)Math.Round(c.A * opacity), c.R, c.G, c.B);

    private static string Format(string format, params object[] args) => string.Format(CultureInfo.CurrentCulture, format, args);

    /// <summary>Aynı fotoğraf birden çok sayfada kullanılsa da bir kez okunur.</summary>
    private sealed class ImageCache : IDisposable
    {
        private readonly Dictionary<string, (XImage? Image, string? Problem)> cache = new(StringComparer.Ordinal);

        public XImage? Get(PhotoAsset asset, out string? problem)
        {
            if (!cache.TryGetValue(asset.Id, out var entry))
            {
                entry = Load(asset);
                cache[asset.Id] = entry;
            }

            problem = entry.Problem;
            return entry.Image;
        }

        public void Dispose()
        {
            foreach (var (image, _) in cache.Values)
            {
                image?.Dispose();
            }

            cache.Clear();
        }

        private static (XImage? Image, string? Problem) Load(PhotoAsset asset)
        {
            if (asset.SourceType != AssetSourceType.Reference)
            {
                return (null, "gömülü fotoğraflar bu sürümde desteklenmiyor");
            }

            if (!File.Exists(asset.SourcePath))
            {
                return (null, "dosya bulunamadı");
            }

            try
            {
                return (XImage.FromFile(asset.SourcePath), null);
            }
            catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException or NotImplementedException or IOException or ArgumentException)
            {
                return (null, "biçim desteklenmiyor (JPEG, PNG veya BMP kullanın)");
            }
        }
    }
}
