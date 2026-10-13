using System.Globalization;
using NexAlbum.Core.Common;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Layout;
using NexAlbum.Core.Projects;
using NexAlbum.Core.Validation;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
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
        var assets = project.Assets.ToDictionary(a => a.Id, a => a.Effective(), StringComparer.Ordinal);
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
        Fill(gfx, albumPage.Background, bleedArea, assets, images, albumPage, number, issues);

        foreach (var element in albumPage.ElementsInDrawOrder)
        {
            switch (element)
            {
                case BackgroundElement background:
                    Fill(gfx, background.Fill, bleedArea, assets, images, albumPage, number, issues);
                    break;
                case Slot slot when slot.Placement is { } placement:
                    DrawSlot(gfx, slot, placement, assets, images, albumPage, number, issues);
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

    private static void Fill(
        XGraphics gfx,
        BackgroundFill fill,
        RectMm area,
        Dictionary<string, PhotoAsset> assets,
        ImageCache images,
        AlbumPage page,
        int number,
        List<ValidationIssue> issues)
    {
        switch (fill.Mode)
        {
            case BackgroundMode.Solid when fill.Color is { } c:
                gfx.DrawRectangle(new XSolidBrush(ToXColor(c, fill.Opacity)), area.X, area.Y, area.Width, area.Height);
                break;
            case BackgroundMode.Gradient when fill.Gradient is { } g:
                var line = GradientGeometry.Line(area, g.AngleDegrees);
                var brush = new XLinearGradientBrush(
                    new XPoint(line.X1, line.Y1),
                    new XPoint(line.X2, line.Y2),
                    ToXColor(g.From, fill.Opacity),
                    ToXColor(g.To, fill.Opacity));
                gfx.DrawRectangle(brush, area.X, area.Y, area.Width, area.Height);
                break;
            case BackgroundMode.Image when fill.ImageAssetId is { } id:
                var asset = Resolve(id, assets, images, out var image, page, number, null, issues);
                if (asset is null || image is null)
                {
                    break;
                }

                var state = gfx.Save();
                gfx.IntersectClip(new XRect(area.X, area.Y, area.Width, area.Height));
                DrawImage(gfx, image, asset, area, new PhotoPlacement { AssetId = id }, CropMode.Cover);
                gfx.Restore(state);

                // Saydamlık: PDF görüntüsü yarı saydam çizilemediğinden fon, sayfa beyazıyla soluklaştırılır.
                if (fill.Opacity < 1)
                {
                    var veil = XColor.FromArgb((int)Math.Round((1 - fill.Opacity) * 255), 255, 255, 255);
                    gfx.DrawRectangle(new XSolidBrush(veil), area.X, area.Y, area.Width, area.Height);
                }

                break;
        }
    }

    /// <summary>
    /// Yuva: gölge → paspartu → (biçime kırpılmış) fotoğraf → kontur. Kontur yuva sınırının içine çizilir;
    /// böylece komşu fotoğrafa ya da taşma payına taşmaz.
    /// </summary>
    private static void DrawSlot(
        XGraphics gfx,
        Slot slot,
        PhotoPlacement placement,
        Dictionary<string, PhotoAsset> assets,
        ImageCache images,
        AlbumPage page,
        int number,
        List<ValidationIssue> issues)
    {
        var asset = Resolve(placement.AssetId, assets, images, out var image, page, number, slot.Id, issues);
        if (asset is null || image is null)
        {
            return;
        }

        var bounds = slot.Bounds;
        if (slot.Shadow is { } shadow)
        {
            DrawShadow(gfx, slot, shadow);
        }

        var polygonal = SlotOutline.IsPolygonal(slot);
        if (slot.Mat is { } mat)
        {
            var matPath = polygonal ? PolygonPath(SlotOutline.Outer(slot)) : ShapePath(bounds, slot.Shape, slot.CornerRadiusMm);
            if (matPath is not null)
            {
                gfx.DrawPath(new XSolidBrush(ToXColor(mat.Color, 1)), matPath);
            }
        }

        var area = slot.PhotoArea;
        var clip = polygonal
            ? PolygonPath(SlotOutline.Photo(slot))
            : area.IsEmpty ? null : ShapePath(area, slot.Shape, Math.Max(0, slot.CornerRadiusMm - (slot.Mat?.Edges.Min ?? 0)));
        if (!area.IsEmpty && clip is not null)
        {
            var state = gfx.Save();
            gfx.IntersectClip(clip);
            DrawImage(gfx, image, asset, area, placement, slot.CropMode);
            gfx.Restore(state);
        }

        if (slot.Border is { WidthMm: > 0 } border)
        {
            var half = border.WidthMm / 2;
            var inset = bounds.Deflate(EdgesMm.Uniform(half));
            var path = polygonal
                ? PolygonPath(SlotOutline.Offset(slot, half))
                : inset.IsEmpty ? null : ShapePath(inset, slot.Shape, Math.Max(0, slot.CornerRadiusMm - half));
            if (path is not null)
            {
                gfx.DrawPath(new XPen(ToXColor(border.Color, 1), border.WidthMm) { LineJoin = XLineJoin.Miter, MiterLimit = 4 }, path);
            }
        }
    }

    /// <summary>Çokgen yol (Karar 10 Rev. 1); boş ya da çökmüş çokgende null.</summary>
    private static XGraphicsPath? PolygonPath(IReadOnlyList<PointMm>? points)
    {
        if (points is not { Count: >= 3 })
        {
            return null;
        }

        var path = new XGraphicsPath();
        path.AddPolygon([.. points.Select(p => new XPoint(p.X, p.Y))]);
        return path;
    }

    private static PhotoAsset? Resolve(
        string assetId,
        Dictionary<string, PhotoAsset> assets,
        ImageCache images,
        out XImage? image,
        AlbumPage page,
        int number,
        string? elementId,
        List<ValidationIssue> issues)
    {
        image = null;
        if (!assets.TryGetValue(assetId, out var asset))
        {
            issues.Add(new("export.photo.unknown", ValidationSeverity.Error, Format("Sayfa {0}: projede olmayan bir görsele bağlı öğe.", number), page.Id, elementId ?? assetId));
            return null;
        }

        image = images.Get(asset, out var problem);
        if (image is null)
        {
            issues.Add(new("export.photo.unreadable", ValidationSeverity.Error,
                Format("Sayfa {0}: {1} PDF'e eklenemedi: {2}", number, asset.FileName, problem ?? "bilinmeyen hata"), page.Id, elementId ?? assetId));
        }

        return asset;
    }

    /// <summary>Görüntüyü EXIF yönü, kırpma, ölçek ve döndürmeyle verilen alana çizer (kırpma çağıranın işidir).</summary>
    private static void DrawImage(XGraphics gfx, XImage image, PhotoAsset asset, RectMm area, PhotoPlacement placement, CropMode mode)
    {
        var orientation = OrientationTransform.FromExif(asset.ExifOrientation);
        var rect = ImageFit.FullImageRect(area, asset.DisplayAspectRatio, placement, mode);

        var state = gfx.Save();
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

    /// <summary>
    /// Yumuşak gölge. PDF'te bulanıklık filtresi olmadığından, büyüyen yarı saydam katmanlar üst üste çizilir;
    /// merkezde toplam saydamlık gölgenin rengine eşit olur, kenarlara doğru azalır.
    /// </summary>
    private static void DrawShadow(XGraphics gfx, Slot slot, ShadowSpec shadow)
    {
        const int layers = 6;
        var b = slot.Bounds;
        var shifted = new RectMm(b.X + shadow.OffsetXMm, b.Y + shadow.OffsetYMm, b.Width, b.Height);
        var targetAlpha = shadow.Color.A / 255.0;
        var layerAlpha = 1 - Math.Pow(1 - targetAlpha, 1.0 / layers);
        var color = XColor.FromArgb(Math.Max(1, (int)Math.Round(layerAlpha * 255)), shadow.Color.R, shadow.Color.G, shadow.Color.B);
        var brush = new XSolidBrush(color);
        var outline = SlotOutline.Outer(slot);
        for (var i = 0; i < layers; i++)
        {
            var spread = shadow.BlurMm * (layers - i) / (2.0 * layers);
            var path = outline is not null
                ? PolygonPath(Polygon.Offset([.. outline.Select(p => new PointMm(p.X + shadow.OffsetXMm, p.Y + shadow.OffsetYMm))], -spread))
                : ShapePath(shifted.Inflate(EdgesMm.Uniform(spread)), slot.Shape, slot.CornerRadiusMm + spread);
            if (path is not null)
            {
                gfx.DrawPath(brush, path);
            }
        }
    }

    /// <summary>Dikdörtgen, yuvarlak köşeli dikdörtgen ya da elips yol.</summary>
    private static XGraphicsPath ShapePath(RectMm r, SlotShape shape, double radius)
    {
        var path = new XGraphicsPath();
        var rect = new XRect(r.X, r.Y, r.Width, r.Height);
        if (shape == SlotShape.Circle)
        {
            path.AddEllipse(rect);
        }
        else if (radius > 0)
        {
            var d = Math.Min(2 * radius, Math.Min(r.Width, r.Height));
            path.AddRoundedRectangle(r.X, r.Y, r.Width, r.Height, d, d);
        }
        else
        {
            path.AddRectangle(rect);
        }

        return path;
    }

    /// <summary>Windows'ta PDFsharp sistem fontlarını çözer; diğer sistemlerde çözümleyici verilmiş olmalı.</summary>
    private static bool CanRenderText => OperatingSystem.IsWindows() || GlobalFontSettings.FontResolver is not null;

    private static void DrawText(XGraphics gfx, TextElement text, AlbumPage page, int number, List<ValidationIssue> issues)
    {
        if (!CanRenderText)
        {
            issues.Add(new("export.text.font-unavailable", ValidationSeverity.Warning,
                Format("Sayfa {0}: bu sistemde yazı tipi çözümleyici yok; metin yazılamadı.", number), page.Id, text.Id));
            return;
        }

        try
        {
            var style = (text.Font.Weight >= 600, text.Font.Italic) switch
            {
                (true, true) => XFontStyleEx.BoldItalic,
                (true, false) => XFontStyleEx.Bold,
                (false, true) => XFontStyleEx.Italic,
                _ => XFontStyleEx.Regular,
            };

            // Punto → mm (çizim birimi mm). Satır kırma, hizalama, harf aralığı ve etkiler ekranla ortak hesaptan gelir
            // (TextLayout): satır yüksekliği = punto × satır aralığı, satırlar bloğu kutu içinde hizalanır.
            var sizeMm = text.Font.Size * 25.4 / 72;
            var font = new XFont(text.Font.Family, sizeMm, style);
            var layout = TextLayout.Compute(text, s => gfx.MeasureString(s, font).Width);
            var format = new XStringFormat { Alignment = XStringAlignment.Near, LineAlignment = XLineAlignment.Center };

            if (TextLayout.Band(text.Effect, layout) is { } band)
            {
                var box = band.Box;
                gfx.DrawRoundedRectangle(new XSolidBrush(ToXColor(band.Color, 1)), box.X, box.Y, box.Width, box.Height, band.Radius * 2, band.Radius * 2);
            }

            void Draw(double dx, double dy, XBrush brush)
            {
                foreach (var line in layout.Lines)
                {
                    foreach (var piece in line.Pieces)
                    {
                        gfx.DrawString(piece.Text, font, brush, new XRect(line.Left + piece.X + dx, line.Top + dy, Math.Max(text.Bounds.Width, sizeMm) * 2, layout.Step), format);
                    }
                }
            }

            foreach (var pass in TextLayout.Passes(text.Effect, sizeMm))
            {
                Draw(pass.Dx, pass.Dy, new XSolidBrush(ToXColor(pass.Color, 1)));
            }

            Draw(0, 0, new XSolidBrush(ToXColor(text.Font.Color, 1)));
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
