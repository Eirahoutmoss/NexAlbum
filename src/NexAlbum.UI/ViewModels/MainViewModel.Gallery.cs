using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using NexAlbum.Core.Layout;

namespace NexAlbum.UI.ViewModels;

/// <summary>Galeri: favoriler, sıralama ve fotoğrafı şeritteki başka bir sayfaya koyma/taşıma.</summary>
public sealed partial class MainViewModel
{
    private GallerySortOption selectedGallerySort = null!;

    public IReadOnlyList<GallerySortOption> GallerySortOptions { get; } =
    [
        new(GallerySort.Imported, "Eklenme sırası"),
        new(GallerySort.Captured, "Çekim zamanı"),
        new(GallerySort.Name, "Dosya adı"),
    ];

    public GallerySortOption SelectedGallerySort
    {
        get => selectedGallerySort;
        set
        {
            if (value is not null && SetProperty(ref selectedGallerySort, value))
            {
                RebuildGallery();
            }
        }
    }

    /// <summary>Fotoğrafı favori yapar ya da favoriden çıkarır; parametresizse galeride ya da yuvada seçili fotoğraf.</summary>
    public RelayCommand<AssetItemViewModel> ToggleFavoriteCommand { get; private set; } = null!;

    public void ToggleFavorite(AssetItemViewModel? item)
    {
        var id = item?.Asset.Id ?? SelectedAsset?.Asset.Id ?? SelectedSlotAsset?.Id;
        if (project is null || id is null || project.FindAsset(id) is not { } asset)
        {
            return;
        }

        var now = clock.GetUtcNow();
        var favorite = !asset.Favorite;
        UpdateProject(project.ReplaceAsset(asset with { Favorite = favorite }).Touch(now), favorite ? "Favori yap" : "Favoriden çıkar");
        Status = favorite
            ? asset.FileName + " favorilere eklendi; otomatik yerleşimde önce yerleşir."
            : asset.FileName + " favorilerden çıkarıldı.";
    }

    /// <summary>Galeriden sürüklenen fotoğrafı şeritteki sayfanın en uygun boş yuvasına koyar.</summary>
    public bool PlaceOnPage(PageItemViewModel? target, string assetId)
    {
        if (project is null || target is null)
        {
            return false;
        }

        var (updated, result) = PhotoMover.PlaceOnPage(project, target.Page.Id, assetId, clock.GetUtcNow());
        return Apply(updated, result, target, "Fotoğraf " + target.Number.ToString(CultureInfo.CurrentCulture) + ". sayfaya yerleşti.", "Sayfaya fotoğraf koy");
    }

    /// <summary>Tuvaldeki yuvadan sürüklenen fotoğrafı şeritteki başka bir sayfaya taşır; kaynak yuva boşalır.</summary>
    public bool MovePhotoToPage(string sourcePageId, string slotId, PageItemViewModel? target)
    {
        if (project is null || target is null)
        {
            return false;
        }

        var (updated, result) = PhotoMover.MoveToPage(project, sourcePageId, slotId, target.Page.Id, clock.GetUtcNow());
        return Apply(updated, result, target, "Fotoğraf " + target.Number.ToString(CultureInfo.CurrentCulture) + ". sayfaya taşındı. Geri almak için Ctrl+Z.", "Fotoğrafı sayfaya taşı");
    }

    /// <summary>Kitap görünümünde yuvadaki fotoğrafı karşı sayfanın yuvasına taşır (doluysa yer değiştirir).</summary>
    public bool MovePhotoToSlot(string sourcePageId, string slotId, PageItemViewModel? target, SlotItemViewModel? targetSlot)
    {
        if (project is null || target is null || targetSlot is null)
        {
            return false;
        }

        var (updated, result) = PhotoMover.MoveToSlot(project, sourcePageId, slotId, target.Page.Id, targetSlot.Id, clock.GetUtcNow());
        return Apply(updated, result, target, "Fotoğraf " + target.Number.ToString(CultureInfo.CurrentCulture) + ". sayfaya taşındı. Geri almak için Ctrl+Z.", "Fotoğrafı sayfaya taşı");
    }

    /// <summary>Şeritte bırakma hedefi olabilir mi (kilitsiz ve boş yuvası var)?</summary>
    public static bool CanReceivePhoto(PageItemViewModel? page) => page is { IsLocked: false } && page.Slots.Any(s => s.IsEmpty);

    private bool Apply(Core.Projects.AlbumProject updated, PhotoMoveResult result, PageItemViewModel target, string done, string label)
    {
        var number = target.Number.ToString(CultureInfo.CurrentCulture);
        switch (result)
        {
            case PhotoMoveResult.Done:
                UpdateProject(updated, label);
                Status = done;
                return true;
            case PhotoMoveResult.NoEmptySlot:
                Status = number + ". sayfada boş yuva yok. Önce bir yuvayı boşaltın ya da daha çok fotoğraflı bir şablon seçin.";
                break;
            case PhotoMoveResult.Locked:
                Status = "Sayfa kilitli; taşımak için önce kilidi açın.";
                break;
            case PhotoMoveResult.AlreadyThere:
                Status = "Bu fotoğraf zaten " + number + ". sayfada.";
                break;
            default:
                break;
        }

        return false;
    }

    private IEnumerable<AssetItemViewModel> Sorted(IEnumerable<AssetItemViewModel> items) => selectedGallerySort?.Sort switch
    {
        GallerySort.Captured => items.OrderBy(a => a.Asset.CapturedAt is null).ThenBy(a => a.Asset.CapturedAt).ThenBy(a => a.FileName, NaturalComparer.Instance),
        GallerySort.Name => items.OrderBy(a => a.FileName, NaturalComparer.Instance),
        _ => items,
    };

    private void InitializeGallery()
    {
        selectedGallerySort = GallerySortOptions[0];
        ToggleFavoriteCommand = new RelayCommand<AssetItemViewModel>(
            ToggleFavorite,
            item => HasProject && !IsBusy && (item is not null || SelectedAsset is not null || SelectedSlotAsset is not null));
    }

    private void RefreshGalleryCommands() => ToggleFavoriteCommand?.NotifyCanExecuteChanged();
}

/// <summary>Dosya adlarını "doğal" sıralar: rakam dizileri sayı olarak (IMG_2 &lt; IMG_10); harfler büyük/küçük ayırmadan.</summary>
public sealed class NaturalComparer : IComparer<string>
{
    public static NaturalComparer Instance { get; } = new();

    public int Compare(string? x, string? y)
    {
        if (x is null || y is null)
        {
            return x is null ? (y is null ? 0 : -1) : 1;
        }

        int i = 0, j = 0;
        while (i < x.Length && j < y.Length)
        {
            if (char.IsAsciiDigit(x[i]) && char.IsAsciiDigit(y[j]))
            {
                var si = i;
                var sj = j;
                while (i < x.Length && char.IsAsciiDigit(x[i]))
                {
                    i++;
                }

                while (j < y.Length && char.IsAsciiDigit(y[j]))
                {
                    j++;
                }

                var a = x[si..i].TrimStart('0');
                var b = y[sj..j].TrimStart('0');
                var byNumber = a.Length != b.Length ? a.Length.CompareTo(b.Length) : string.CompareOrdinal(a, b);
                if (byNumber != 0)
                {
                    return byNumber;
                }

                continue;
            }

            var c = char.ToUpperInvariant(x[i]).CompareTo(char.ToUpperInvariant(y[j]));
            if (c != 0)
            {
                return c;
            }

            i++;
            j++;
        }

        var rest = (x.Length - i).CompareTo(y.Length - j);
        return rest != 0 ? rest : string.CompareOrdinal(x, y);
    }
}
