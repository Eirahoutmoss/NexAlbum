using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using NexAlbum.Core.Projects;
using NexAlbum.UI.Services;

namespace NexAlbum.UI.ViewModels;

/// <summary>Galerideki fotoğraf kartı (Karar 12 §5.4).</summary>
public sealed class AssetItemViewModel(PhotoAsset asset, bool used) : ObservableObject
{
    private ImageSource? thumbnail;
    private bool loading;

    public PhotoAsset Asset { get; } = asset;

    public string FileName => Asset.FileName;

    public bool IsUsed { get; } = used;

    public bool IsMissing => Asset.Missing;

    public string Details => $"{Asset.DisplayWidth}×{Asset.DisplayHeight}" + (Asset.CapturedAt is { } t ? " · " + t.ToString("g", System.Globalization.CultureInfo.CurrentCulture) : string.Empty);

    /// <summary>Küçük resim ilk istendiğinde arka planda yüklenir.</summary>
    public ImageSource? Thumbnail
    {
        get
        {
            if (thumbnail is null && !loading)
            {
                loading = true;
                thumbnail = ImageLoader.Shared.TryGetCached(Asset, ImageLoader.ThumbnailWidth);
                if (thumbnail is not null)
                {
                    return thumbnail;
                }

                _ = LoadAsync();
            }

            return thumbnail;
        }
    }

    private async Task LoadAsync()
    {
        var image = await Task.Run(() => ImageLoader.Shared.Load(Asset, ImageLoader.ThumbnailWidth)).ConfigureAwait(true);
        thumbnail = image;
        OnPropertyChanged(nameof(Thumbnail));
    }
}
