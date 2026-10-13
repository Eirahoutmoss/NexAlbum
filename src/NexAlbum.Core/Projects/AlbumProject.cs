using NexAlbum.Core.Common;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Profiles;

namespace NexAlbum.Core.Projects;

/// <summary>
/// Proje kökü (Karar 09 §4). Değiştirilemez: her işlem yeni bir örnek döndürür
/// (geri al/yinele ve deterministik yerleşim için güvenli).
/// </summary>
public sealed record AlbumProject
{
    public required Guid Id { get; init => field = value == Guid.Empty ? throw new ArgumentException("Proje kimliği boş olamaz.", nameof(Id)) : value; }

    public required string Name { get; init => field = Guard.NotBlank(value, nameof(Name)); }

    public string? Description { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    /// <summary>Projeyi oluşturan/son kaydeden uygulama sürümü.</summary>
    public string AppVersion { get; init => field = Guard.NotBlank(value, nameof(AppVersion)); } = "0.0.0";

    /// <summary>Profilin proje oluşturulduğu (ya da son güncellendiği) andaki kopyası (Karar 09 §4.3).</summary>
    public required AlbumProfile Profile { get; init => field = value ?? throw new ArgumentNullException(nameof(Profile)); }

    /// <summary>Bitmiş sayfa ölçüsü.</summary>
    public required SizeMm PageSize { get; init; }

    public IReadOnlyList<AlbumPage> Pages { get; init => field = ValidateUnique(value, p => p.Id, nameof(Pages)); } = [];

    public IReadOnlyList<PhotoAsset> Assets { get; init => field = ValidateUnique(value, a => a.Id, nameof(Assets)); } = [];

    public ProjectSettings Settings { get; init => field = value ?? throw new ArgumentNullException(nameof(Settings)); } = ProjectSettings.Default;

    public HistorySettings? History { get; init; }

    /// <summary>Hikâye akışı: zamansal bölümler (Karar 10 Rev. 2–3); yoksa boş.</summary>
    public StoryPlan Story { get; init => field = value ?? throw new ArgumentNullException(nameof(Story)); } = StoryPlan.Empty;

    /// <summary>Hikâye filmi ayarları (Karar 21 §6).</summary>
    public global::NexAlbum.Core.Video.VideoSettings Video { get; init => field = value ?? throw new ArgumentNullException(nameof(Video)); } = global::NexAlbum.Core.Video.VideoSettings.Default;

    public string ProfileId => Profile.Id;

    /// <summary>Sayfa dikdörtgeni (0,0 köşeli, bitmiş ölçü).</summary>
    public RectMm PageRect => RectMm.FromSize(PageSize);

    /// <summary>Yeni boş proje. Profilde sayfa ölçüsü yoksa oluşturulamaz; ölçü uydurulmaz.</summary>
    public static AlbumProject Create(string name, AlbumProfile profile, DateTimeOffset now, string appVersion, Guid? id = null)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var size = profile.Page.Size
            ?? throw new InvalidOperationException("Profilde sayfa ölçüsü yok. Önce profili tamamlayın.");

        return new AlbumProject
        {
            Id = id ?? Guid.CreateVersion7(now),
            Name = name,
            CreatedAt = now,
            UpdatedAt = now,
            AppVersion = appVersion,
            Profile = profile,
            PageSize = size,
        };
    }

    public AlbumPage? FindPage(string pageId) => Pages.FirstOrDefault(p => p.Id == pageId);

    public PhotoAsset? FindAsset(string assetId) => Assets.FirstOrDefault(a => a.Id == assetId);

    /// <summary>Sayfanın 0 tabanlı sırası; yoksa -1.</summary>
    public int IndexOfPage(string pageId)
    {
        for (var i = 0; i < Pages.Count; i++)
        {
            if (Pages[i].Id == pageId)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Sayfa ekler; <paramref name="index"/> verilmezse sona.</summary>
    public AlbumProject AddPage(AlbumPage page, int? index = null)
    {
        ArgumentNullException.ThrowIfNull(page);
        var at = index ?? Pages.Count;
        Guard.InRange(at, 0, Pages.Count, nameof(index));
        var list = Pages.ToList();
        list.Insert(at, page);
        return this with { Pages = list };
    }

    public AlbumProject ReplacePage(AlbumPage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        var index = RequirePage(page.Id);
        var list = Pages.ToList();
        list[index] = page;
        return this with { Pages = list };
    }

    public AlbumProject RemovePage(string pageId)
    {
        var index = RequirePage(pageId);
        var list = Pages.ToList();
        list.RemoveAt(index);
        return this with { Pages = list };
    }

    public AlbumProject MovePage(string pageId, int newIndex)
    {
        var index = RequirePage(pageId);
        Guard.InRange(newIndex, 0, Pages.Count - 1, nameof(newIndex));
        var list = Pages.ToList();
        var page = list[index];
        list.RemoveAt(index);
        list.Insert(newIndex, page);
        return this with { Pages = list };
    }

    /// <summary>Tüm sayfaları değiştirir (ör. otomatik yerleşim sonucu).</summary>
    public AlbumProject WithPages(IEnumerable<AlbumPage> pages)
    {
        ArgumentNullException.ThrowIfNull(pages);
        return this with { Pages = [.. pages] };
    }

    public AlbumProject AddAsset(PhotoAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        return this with { Assets = [.. Assets, AlignClock(asset)] };
    }

    public AlbumProject AddAssets(IEnumerable<PhotoAsset> assets)
    {
        ArgumentNullException.ThrowIfNull(assets);
        return this with { Assets = [.. Assets, .. assets.Select(AlignClock)] };
    }

    /// <summary>Yeni fotoğraf, aynı makine için daha önce girilmiş saat düzeltmesini alır.</summary>
    private PhotoAsset AlignClock(PhotoAsset asset)
    {
        var offset = CameraClocks.OffsetOf(this, CameraClocks.KeyOf(asset));
        return offset == asset.ClockOffset ? asset : asset.WithClockOffset(offset);
    }

    public AlbumProject ReplaceAsset(PhotoAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        var index = RequireAsset(asset.Id);
        var list = Assets.ToList();
        list[index] = asset;
        return this with { Assets = list };
    }

    /// <summary>Varlığı kaldırır; yuvalardaki yerleşimleri de temizlenir (askıda referans kalmaz).</summary>
    public AlbumProject RemoveAsset(string assetId)
    {
        RequireAsset(assetId);
        return this with
        {
            Assets = [.. Assets.Where(a => a.Id != assetId)],
            Pages = [.. Pages.Select(p => p.UsesAsset(assetId) ? p.ClearPlacementsOf(assetId) : p)],
        };
    }

    /// <summary>Varlığın kullanıldığı sayfaların kimlikleri, sayfa sırasıyla.</summary>
    public IReadOnlyList<string> PagesUsingAsset(string assetId) =>
        [.. Pages.Where(p => p.UsesAsset(assetId)).Select(p => p.Id)];

    /// <summary>Yuvalara yerleşebilecek fotoğraflar (fon görselleri hariç).</summary>
    public IReadOnlyList<PhotoAsset> Photos => [.. Assets.Where(a => a.IsPhoto)];

    /// <summary>Fon olarak kullanılabilecek görseller.</summary>
    public IReadOnlyList<PhotoAsset> Backgrounds => [.. Assets.Where(a => !a.IsPhoto)];

    /// <summary>Hiçbir sayfada kullanılmayan fotoğraflar (fon görselleri sayılmaz).</summary>
    public IReadOnlyList<PhotoAsset> UnusedAssets()
    {
        var used = new HashSet<string>(
            Pages.SelectMany(p => p.Slots).Select(s => s.Placement?.AssetId).OfType<string>(),
            StringComparer.Ordinal);
        return [.. Assets.Where(a => a.IsPhoto && !used.Contains(a.Id))];
    }

    /// <summary>Profil kopyasını günceller (Karar 03 §10.3).</summary>
    public AlbumProject WithProfile(AlbumProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return this with { Profile = profile };
    }

    public AlbumProject Touch(DateTimeOffset now) => this with { UpdatedAt = now };

    private int RequirePage(string pageId)
    {
        var index = IndexOfPage(pageId);
        return index >= 0 ? index : throw new KeyNotFoundException($"Sayfa bulunamadı: {pageId}");
    }

    private int RequireAsset(string assetId)
    {
        for (var i = 0; i < Assets.Count; i++)
        {
            if (Assets[i].Id == assetId)
            {
                return i;
            }
        }

        throw new KeyNotFoundException($"Varlık bulunamadı: {assetId}");
    }

    private static IReadOnlyList<T> ValidateUnique<T>(IReadOnlyList<T>? items, Func<T, string> key, string paramName)
        where T : class
    {
        var list = Guard.NotNullItems(items, paramName);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in list)
        {
            if (!seen.Add(key(item)))
            {
                throw new ArgumentException($"Yinelenen kimlik: {key(item)}", paramName);
            }
        }

        return list;
    }
}
