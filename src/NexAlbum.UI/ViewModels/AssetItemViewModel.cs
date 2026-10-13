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
    private bool isSelected;
    private Brush? chapterColor;

    public PhotoAsset Asset { get; } = asset;

    /// <summary>Galeride seçili mi (ızgara kendi seçimini tutar; bkz. MainViewModel.SelectedAsset).</summary>
    public bool IsSelected
    {
        get => isSelected;
        set => SetProperty(ref isSelected, value);
    }

    /// <summary>Izgaradaki ipucu: dosya adı, boyut, çekim zamanı ve durum.</summary>
    public string ToolTipText => FileName + (IsFavorite ? "  ★" : string.Empty) + "\n" + Details
        + (IsUsed ? "\nAlbümde" : "\nHenüz kullanılmadı")
        + (IsEdited ? "\n" + EditBadge : string.Empty)
        + (IsMissing ? "\nDosya bulunamadı" : string.Empty);

    public string FileName => Asset.FileName;

    public bool IsUsed { get; } = used;

    /// <summary>Hikâye bölümünün rengi (karenin altında ince çizgi); bölüm yoksa null.</summary>
    public Brush? ChapterColor
    {
        get => chapterColor;
        set => SetProperty(ref chapterColor, value);
    }

    /// <summary>Favori (yıldızlı): otomatik yerleşimde önce yerleşir (Karar 10 §9.5).</summary>
    public bool IsFavorite => Asset.Favorite;

    public bool IsMissing => Asset.Missing && !Asset.IsEditActive;

    /// <summary>Düzenlenmiş sürümü var mı (etkin ya da değil)?</summary>
    public bool IsEdited => Asset.Edit is not null;

    public string EditBadge => Asset.Edit is null
        ? string.Empty
        : Asset.Edit.Missing ? "Düzenleme kayıp" : Asset.UseEdit ? "Düzenlendi" : "Orijinal gösteriliyor";

    public string Details => $"{Asset.DisplayWidth}×{Asset.DisplayHeight}" + (Asset.CapturedAt is { } t ? " · " + t.ToString("g", System.Globalization.CultureInfo.CurrentCulture) : string.Empty);

    /// <summary>Küçük resim ilk istendiğinde arka planda yüklenir.</summary>
    public ImageSource? Thumbnail
    {
        get
        {
            if (thumbnail is null && !loading)
            {
                loading = true;
                thumbnail = ImageLoader.Shared.TryGetCached(Asset.Effective(), ImageLoader.ThumbnailWidth);
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
        var image = await Task.Run(() => ImageLoader.Shared.Load(Asset.Effective(), ImageLoader.ThumbnailWidth)).ConfigureAwait(true);
        thumbnail = image;
        OnPropertyChanged(nameof(Thumbnail));
    }
}
