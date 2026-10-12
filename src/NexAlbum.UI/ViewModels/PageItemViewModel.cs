using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Projects;
using NexAlbum.UI.Services;

namespace NexAlbum.UI.ViewModels;

/// <summary>
/// Bir sayfanın çizim modeli. Tuval birimi milimetredir; görüntü kutusu (Viewbox) ekrana ölçekler.
/// Koordinatlar taşma payı alanının sol üst köşesine göredir.
/// </summary>
public sealed class PageItemViewModel : ObservableObject
{
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

        var assets = project.Assets.ToDictionary(a => a.Id, StringComparer.Ordinal);
        Slots = [.. page.Slots.OrderBy(s => s.ZIndex).Select(s => new SlotItemViewModel(s, Offset(s.Bounds), s.Placement is { } p && assets.TryGetValue(p.AssetId, out var a) ? a : null))];
        Background = page.Background.Color is { } c ? new SolidColorBrush(Color.FromArgb(c.A, c.R, c.G, c.B)) : Brushes.White;
        if (!Background.IsFrozen)
        {
            Background.Freeze();
        }
    }

    public AlbumPage Page { get; }

    public int Index { get; }

    public int Number => Index + 1;

    public string Title => string.Format(CultureInfo.CurrentCulture, "Sayfa {0}", Number);

    public bool IsLocked => Page.Locked;

    public string? TemplateId => Page.TemplateId;

    public RectMm Origin { get; }

    public double CanvasWidth { get; }

    public double CanvasHeight { get; }

    public RectMm PageArea { get; }

    public RectMm? SafeArea { get; }

    public bool HasBleed { get; }

    public bool HasSafeArea => SafeArea is not null;

    public Brush Background { get; }

    public ObservableCollection<SlotItemViewModel> Slots { get; }

    /// <summary>Tuvalde gösterilirken yüksek çözünürlüklü önizleme yüklenir; ayrılınca bırakılır (bellek).</summary>
    public void SetDetail(bool detail)
    {
        foreach (var slot in Slots)
        {
            slot.SetDetail(detail);
        }
    }

    private RectMm Offset(RectMm r) => new(r.X - Origin.X, r.Y - Origin.Y, r.Width, r.Height);
}

/// <summary>Bir fotoğraf yuvasının çizim modeli.</summary>
public sealed class SlotItemViewModel(Slot slot, RectMm bounds, PhotoAsset? asset) : ObservableObject
{
    private ImageSource? thumbnail;
    private ImageSource? detail;
    private bool loadingThumbnail;
    private int detailVersion;

    public Slot Slot { get; } = slot;

    public PhotoAsset? Asset { get; } = asset;

    public double X { get; } = bounds.X;

    public double Y { get; } = bounds.Y;

    public double Width { get; } = bounds.Width;

    public double Height { get; } = bounds.Height;

    public bool IsEmpty => Asset is null;

    public bool IsMissing => Asset?.Missing == true;

    /// <summary>"cover" kaplar (kırpar), "contain" sığdırır. Otomatik yerleşim asla esnetmez.</summary>
    public Stretch Stretch => Slot.CropMode == CropMode.Contain ? Stretch.Uniform : Stretch.UniformToFill;

    public double CornerRadius => Slot.CornerRadiusMm;

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
