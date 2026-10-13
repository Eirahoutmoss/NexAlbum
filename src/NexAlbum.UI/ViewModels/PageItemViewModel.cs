using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Media.Effects;
using CommunityToolkit.Mvvm.ComponentModel;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Projects;
using NexAlbum.Export.Pdf;
using NexAlbum.UI.Services;

namespace NexAlbum.UI.ViewModels;

/// <summary>
/// Bir sayfanın çizim modeli. Tuval birimi milimetredir; görüntü kutusu (Viewbox) ekrana ölçekler.
/// Koordinatlar taşma payı alanının sol üst köşesine göredir.
/// </summary>
public sealed class PageItemViewModel : ObservableObject
{
    private Brush background;

    public PageItemViewModel(AlbumPage page, int index, AlbumProject project)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(project);
        Page = page;
        Index = index;

        var pageRect = project.PageRect;
        var bleed = project.Profile.Bleed.Value is { } b ? pageRect.Inflate(b) : pageRect;
        Origin = bleed;
        CanvasWidth = bleed.Width;
        CanvasHeight = bleed.Height;
        PageArea = Offset(pageRect);
        SafeArea = project.Profile.SafeArea.Value is { } s ? Offset(pageRect.Deflate(s)) : null;
        HasBleed = project.Profile.Bleed.Value is not null;

        // Etkin kaynak: düzenlenmiş sürüm varsa ekranda o görünür (PDF ve ön kontrolle aynı, Karar 20 §4).
        var assets = project.Assets.ToDictionary(a => a.Id, a => a.Effective(), StringComparer.Ordinal);
        Slots = [.. page.Slots.OrderBy(s => s.ZIndex).Select(s => new SlotItemViewModel(s, Offset(s.Bounds), s.Placement is { } p && assets.TryGetValue(p.AssetId, out var a) ? a : null))];

        background = DesignBrushes.Fill(page.Background);
        if (page.Background is { Mode: BackgroundMode.Image, ImageAssetId: { } id } fill && assets.TryGetValue(id, out var image))
        {
            _ = LoadBackgroundAsync(image, fill.Opacity);
        }
    }

    public AlbumPage Page { get; }

    public int Index { get; }

    public int Number => Index + 1;

    public string Title => string.Format(CultureInfo.CurrentCulture, "Sayfa {0}", Number);

    public bool IsLocked => Page.Locked;

    public string? TemplateId => Page.TemplateId;

    public int PhotoCount => Slots.Count(s => !s.IsEmpty);

    /// <summary>Sayfa şeridindeki küçük resmin yüksekliği (DIP).</summary>
    public const double ThumbHeight = 76;

    /// <summary>Küçük resmin genişliği: sayfa oranı korunur (yatay, dikey, kare albümler).</summary>
    public double ThumbWidth => CanvasHeight > 0 ? Math.Round(ThumbHeight * CanvasWidth / CanvasHeight) : ThumbHeight;

    public RectMm Origin { get; }

    public double CanvasWidth { get; }

    public double CanvasHeight { get; }

    public RectMm PageArea { get; }

    public RectMm? SafeArea { get; }

    public bool HasBleed { get; }

    public bool HasSafeArea => SafeArea is not null;

    /// <summary>Sayfa fonu: düz renk, gradyan ya da (arka planda yüklenen) görsel.</summary>
    public Brush Background
    {
        get => background;
        private set => SetProperty(ref background, value);
    }

    public ObservableCollection<SlotItemViewModel> Slots { get; }

    /// <summary>Tuvalde gösterilirken yüksek çözünürlüklü önizleme yüklenir; ayrılınca bırakılır (bellek).</summary>
    public void SetDetail(bool detail)
    {
        foreach (var slot in Slots)
        {
            slot.SetDetail(detail);
        }
    }

    private async Task LoadBackgroundAsync(PhotoAsset asset, double opacity)
    {
        var image = await Task.Run(() => ImageLoader.Shared.Load(asset, ImageLoader.ThumbnailWidth * 3)).ConfigureAwait(true);
        if (image is not null)
        {
            var brush = new ImageBrush(image) { Stretch = Stretch.UniformToFill, Opacity = opacity };
            brush.Freeze();
            Background = brush;
        }
    }

    private RectMm Offset(RectMm r) => new(r.X - Origin.X, r.Y - Origin.Y, r.Width, r.Height);
}

/// <summary>
/// Bir fotoğraf yuvasının çizim modeli: gölgeli biçim (paspartu rengiyle), içte biçime kırpılmış fotoğraf, üstte kontur.
/// Koordinatlar yuvanın sol üst köşesine göredir (mm).
/// </summary>
public sealed class SlotItemViewModel : ObservableObject
{
    private readonly bool sample;
    private ImageSource? thumbnail;
    private ImageSource? detail;
    private bool loadingThumbnail;
    private int detailVersion;
    private bool isSelected;
    private bool isDropTarget;
    private PhotoPlacement? placement;
    private RectMm imageRect;

    public SlotItemViewModel(Slot slot, RectMm bounds, PhotoAsset? asset, bool sample = false)
    {
        ArgumentNullException.ThrowIfNull(slot);
        Slot = slot;
        Asset = asset;
        this.sample = sample;
        X = bounds.X;
        Y = bounds.Y;
        Width = bounds.Width;
        Height = bounds.Height;

        var area = slot.PhotoArea;
        PhotoX = area.X - slot.Bounds.X;
        PhotoY = area.Y - slot.Bounds.Y;
        PhotoWidth = area.Width;
        PhotoHeight = area.Height;

        var radius = slot.CornerRadiusMm;
        var polygonal = SlotOutline.IsPolygonal(slot);
        OuterGeometry = polygonal
            ? DesignBrushes.Polygon(SlotOutline.Outer(slot), slot.Bounds.X, slot.Bounds.Y)
            : DesignBrushes.Shape(slot.Shape, 0, 0, Width, Height, radius);
        PhotoClip = polygonal
            ? DesignBrushes.Polygon(SlotOutline.Photo(slot), area.X, area.Y)
            : DesignBrushes.Shape(slot.Shape, 0, 0, PhotoWidth, PhotoHeight, Math.Max(0, radius - (slot.Mat?.Edges.Min ?? 0)));
        ShapeFill = slot.Mat is { } mat ? DesignBrushes.Solid(mat.Color) : sample ? DesignBrushes.SamplePhoto : DesignBrushes.EmptySlot;
        Shadow = slot.Shadow is { } shadow ? DesignBrushes.Shadow(shadow) : null;

        placement = slot.Placement;
        imageRect = ComputeImageRect(placement);
        if (asset is not null)
        {
            thumbnail = ImageLoader.Shared.TryGetCached(asset, ImageLoader.ThumbnailWidth);
        }

        if (slot.Border is { WidthMm: > 0 } border)
        {
            var half = border.WidthMm / 2;
            StrokeThickness = border.WidthMm;
            StrokeBrush = DesignBrushes.Solid(border.Color);
            StrokeGeometry = polygonal
                ? DesignBrushes.Polygon(SlotOutline.Offset(slot, half), slot.Bounds.X, slot.Bounds.Y)
                : DesignBrushes.Shape(slot.Shape, half, half, Width - border.WidthMm, Height - border.WidthMm, Math.Max(0, radius - half));
        }
    }

    public Slot Slot { get; }

    public string Id => Slot.Id;

    public PhotoAsset? Asset { get; }

    public double X { get; }

    public double Y { get; }

    public double Width { get; }

    public double Height { get; }

    public double PhotoX { get; }

    public double PhotoY { get; }

    public double PhotoWidth { get; }

    public double PhotoHeight { get; }

    public Geometry OuterGeometry { get; }

    public Geometry PhotoClip { get; }

    public Brush ShapeFill { get; }

    public Effect? Shadow { get; }

    public Geometry? StrokeGeometry { get; }

    public Brush? StrokeBrush { get; }

    public double StrokeThickness { get; }

    /// <summary>Görüntünün (tamamının) fotoğraf alanına göre çizim dikdörtgeni; PDF ile aynı hesap (ImageFit).</summary>
    public double ImageX => imageRect.X;

    public double ImageY => imageRect.Y;

    public double ImageWidth => imageRect.Width;

    public double ImageHeight => imageRect.Height;

    public double ImageRotation => placement?.Rotation ?? 0;

    /// <summary>Geçerli yerleşim (sürükleme sırasında önizleme yerleşimi).</summary>
    public PhotoPlacement? Placement => placement;

    public bool IsSelected
    {
        get => isSelected;
        set => SetProperty(ref isSelected, value);
    }

    /// <summary>Sürüklenen fotoğraf bu yuvanın üzerinde.</summary>
    public bool IsDropTarget
    {
        get => isDropTarget;
        set => SetProperty(ref isDropTarget, value);
    }

    public bool IsEmpty => Asset is null;

    public bool ShowEmptyLabel => IsEmpty && !sample;

    public bool IsMissing => Asset?.Missing == true;

    /// <summary>Fotoğraf alanının zemini: örnekte "gökyüzü", boş yuvada gri, fotoğraf varsa yok.</summary>
    public Brush? PhotoBackground => sample ? DesignBrushes.SamplePhoto : IsEmpty ? DesignBrushes.EmptySlot : null;

    /// <summary>Yerleşimi kaydetmeden gösterir (sürükleyerek kaydırma, tekerlekle yakınlaştırma).</summary>
    public void Preview(PhotoPlacement value)
    {
        ArgumentNullException.ThrowIfNull(value);
        placement = value;
        imageRect = ComputeImageRect(value);
        OnPropertyChanged(nameof(Placement));
        OnPropertyChanged(nameof(ImageX));
        OnPropertyChanged(nameof(ImageY));
        OnPropertyChanged(nameof(ImageWidth));
        OnPropertyChanged(nameof(ImageHeight));
        OnPropertyChanged(nameof(ImageRotation));
    }

    private RectMm ComputeImageRect(PhotoPlacement? value) =>
        Asset is null || value is null || PhotoWidth <= 0 || PhotoHeight <= 0
            ? new RectMm(0, 0, Math.Max(0, PhotoWidth), Math.Max(0, PhotoHeight))
            : ImageFit.FullImageRect(new RectMm(0, 0, PhotoWidth, PhotoHeight), Asset.DisplayAspectRatio, value, Slot.CropMode);

    public ImageSource? Image
    {
        get
        {
            if (detail is not null)
            {
                return detail;
            }

            if (thumbnail is null && !loadingThumbnail && Asset is not null)
            {
                loadingThumbnail = true;
                _ = LoadAsync(ImageLoader.ThumbnailWidth, image => thumbnail = image, version: null);
            }

            return thumbnail;
        }
    }

    public void SetDetail(bool enabled)
    {
        var version = ++detailVersion;
        if (!enabled)
        {
            if (detail is not null)
            {
                detail = null;
                OnPropertyChanged(nameof(Image));
            }

            return;
        }

        if (Asset is not null && detail is null)
        {
            if (ImageLoader.Shared.TryGetCached(Asset, ImageLoader.DetailWidth) is { } cached)
            {
                detail = cached;
                OnPropertyChanged(nameof(Image));
                return;
            }

            _ = LoadAsync(ImageLoader.DetailWidth, image => detail = image, version);
        }
    }

    private async Task LoadAsync(int width, Action<ImageSource?> assign, int? version)
    {
        var asset = Asset!;
        var image = await Task.Run(() => ImageLoader.Shared.Load(asset, width)).ConfigureAwait(true);
        if (version is { } v && v != detailVersion)
        {
            return;
        }

        assign(image);
        OnPropertyChanged(nameof(Image));
    }
}
