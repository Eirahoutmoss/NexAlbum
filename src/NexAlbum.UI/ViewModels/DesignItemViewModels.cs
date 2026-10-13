using System.Globalization;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using NexAlbum.Core.Design;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Projects;
using NexAlbum.Core.Templates;
using NexAlbum.UI.Services;

namespace NexAlbum.UI.ViewModels;

/// <summary>Çerçeve galerisindeki bir stil; önizleme örnek bir yuvaya uygulanmış hâlidir.</summary>
public sealed class FrameItemViewModel
{
    /// <summary>Önizleme yuvasının ölçüsü (mm); oranı 4:3.</summary>
    public const double SampleWidth = 60;

    public const double SampleHeight = 45;

    public FrameItemViewModel(FrameStyle frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        Frame = frame;
        var slot = frame.ApplyTo(new Slot { Id = "sample", Bounds = new RectMm(0, 0, SampleWidth, SampleHeight) });
        Sample = new SlotItemViewModel(slot, slot.Bounds, asset: null, sample: true);
    }

    public FrameStyle Frame { get; }

    public string Id => Frame.Id;

    public string Name => Frame.Name;

    public SlotItemViewModel Sample { get; }
}

/// <summary>Fon galerisindeki bir seçenek: hazır renk/gradyan ya da kullanıcının yüklediği görsel.</summary>
public sealed class BackgroundItemViewModel : ObservableObject
{
    private Brush preview;

    public BackgroundItemViewModel(BackgroundPreset preset)
    {
        ArgumentNullException.ThrowIfNull(preset);
        Id = preset.Id;
        Name = preset.Name;
        Fill = preset.Fill;
        preview = DesignBrushes.Fill(preset.Fill);
    }

    public BackgroundItemViewModel(PhotoAsset image)
    {
        ArgumentNullException.ThrowIfNull(image);
        Id = image.Id;
        Name = image.FileName;
        Fill = new BackgroundFill { Mode = BackgroundMode.Image, Color = null, ImageAssetId = image.Id };
        Asset = image;
        preview = DesignBrushes.EmptySlot;
        if (!image.Missing)
        {
            _ = LoadAsync(image);
        }
    }

    public string Id { get; }

    public string Name { get; }

    /// <summary>Opaklık 1 olan dolgu; uygulanırken paneldeki opaklık eklenir.</summary>
    public BackgroundFill Fill { get; }

    public PhotoAsset? Asset { get; }

    public bool IsImage => Asset is not null;

    public Brush Preview
    {
        get => preview;
        private set => SetProperty(ref preview, value);
    }

    private async Task LoadAsync(PhotoAsset image)
    {
        var source = await Task.Run(() => ImageLoader.Shared.Load(image, ImageLoader.ThumbnailWidth)).ConfigureAwait(true);
        if (source is not null)
        {
            var brush = new ImageBrush(source) { Stretch = Stretch.UniformToFill };
            brush.Freeze();
            Preview = brush;
        }
    }
}

/// <summary>Şablon galerisindeki bir şablon; hücreler projenin sayfa oranında ve tasarım boşluklarıyla çözülür.</summary>
public sealed class TemplateItemViewModel
{
    /// <summary>Fotoğraf yokken hücre dolgusu (kâğıt üzerinde sıcak gri-altın).</summary>
    public static Brush EmptyCellFill { get; } = CreateEmptyFill();

    /// <param name="template">Şablon.</param>
    /// <param name="page">Projenin sayfa ölçüsü.</param>
    /// <param name="design">Boşluk ve kenar payı.</param>
    /// <param name="photos">Canlı önizleme: seçili sayfanın fotoğrafları (okuma sırasıyla); hücreler bunlarla doldurulur.</param>
    public TemplateItemViewModel(TemplateDefinition template, SizeMm page, DesignSettings design, IReadOnlyList<Brush>? photos = null)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(design);
        Template = template;
        Width = page.Width;
        Height = page.Height;
        var area = new RectMm(0, 0, page.Width, page.Height);
        var shapes = template.Slots.ToDictionary(s => s.Id, s => s, StringComparer.Ordinal);
        var fills = photos ?? [];
        Cells = [.. template.Resolve(area, design.GapPercent, design.MarginPercent).Select((r, i) =>
        {
            var slot = shapes[r.SlotId];
            var fill = i < fills.Count ? fills[i] : EmptyCellFill;
            if (r.Polygon is { } polygon)
            {
                return new TemplateCell(r.Bounds.X, r.Bounds.Y, DesignBrushes.Polygon(Polygon.Map(polygon, r.Bounds), r.Bounds.X, r.Bounds.Y), fill);
            }

            var radius = slot.Shape == SlotShape.Rounded ? Math.Max(slot.CornerRadiusMm, Math.Min(r.Bounds.Width, r.Bounds.Height) * 0.08) : slot.CornerRadiusMm;
            return new TemplateCell(r.Bounds.X, r.Bounds.Y, DesignBrushes.Shape(slot.Shape, 0, 0, r.Bounds.Width, r.Bounds.Height, radius), fill);
        })];
        IsGeometric = template.Category == TemplateCategory.Geometric;
    }

    public TemplateDefinition Template { get; }

    public bool IsGeometric { get; }

    public string Id => Template.Id;

    public string Name => Template.Name;

    public string ToolTip => string.Format(
        CultureInfo.CurrentCulture,
        "{0} · {1}",
        Template.Name,
        Template.PhotoCount.Min == Template.PhotoCount.Max
            ? string.Format(CultureInfo.CurrentCulture, "{0} fotoğraf", Template.PhotoCount.Min)
            : string.Format(CultureInfo.CurrentCulture, "{0}–{1} fotoğraf", Template.PhotoCount.Min, Template.PhotoCount.Max));

    public double Width { get; }

    public double Height { get; }

    public IReadOnlyList<TemplateCell> Cells { get; }

    private static Brush CreateEmptyFill()
    {
        var brush = new LinearGradientBrush(Color.FromRgb(0xD8, 0xCB, 0xAE), Color.FromRgb(0xC2, 0xB0, 0x8A), 90);
        brush.Freeze();
        return brush;
    }
}

/// <summary>Şablon önizlemesindeki bir hücre (mm, sayfanın sol üst köşesine göre); dolgu sayfanın fotoğrafı olabilir.</summary>
public sealed record TemplateCell(double X, double Y, Geometry Shape, Brush Fill);

/// <summary>Şablon süzgeci: fotoğraf sayısı (0 = tümü).</summary>
public sealed record TemplateCountOption(int Count, string Name);

/// <summary>Şablon galerisinin biçim süzgeci.</summary>
public enum TemplateStyle
{
    All,
    Geometric,
    Collage,
    Grid,
    Simple,
}

/// <summary>Biçim süzgeci seçeneği.</summary>
public sealed record TemplateStyleOption(TemplateStyle Style, string Name)
{
    public bool Matches(TemplateCategory category) => Style switch
    {
        TemplateStyle.Geometric => category == TemplateCategory.Geometric,
        TemplateStyle.Collage => category is TemplateCategory.Collage or TemplateCategory.Featured,
        TemplateStyle.Grid => category == TemplateCategory.Grid,
        TemplateStyle.Simple => category is TemplateCategory.Single or TemplateCategory.FullBleed or TemplateCategory.Minimal,
        _ => true,
    };
}
