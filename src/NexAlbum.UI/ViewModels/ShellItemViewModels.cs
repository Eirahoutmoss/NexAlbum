using System.Globalization;
using System.IO;
using NexAlbum.UI.Services;

namespace NexAlbum.UI.ViewModels;

/// <summary>Galeri süzgeci.</summary>
public enum GalleryFilter
{
    All,
    Unused,
    Used,
    Edited,
    Favorites,

    /// <summary>Hikâye panelinde seçili bölümün fotoğrafları.</summary>
    Chapter,
}

/// <summary>Galeri sıralaması (Karar 12 §5.6).</summary>
public enum GallerySort
{
    /// <summary>Eklenme sırası (içe aktarma sırası).</summary>
    Imported,

    /// <summary>Çekim zamanı (EXIF); zamanı olmayanlar sonda.</summary>
    Captured,

    /// <summary>Dosya adı (sayılar sayı olarak: IMG_2 &lt; IMG_10).</summary>
    Name,
}

/// <summary>Sıralama seçeneği.</summary>
public sealed record GallerySortOption(GallerySort Sort, string Name);

/// <summary>
/// Galeri ızgarasının bir satırı. Izgara satırlara bölünür ki liste sanallaştırılabilsin (binlerce fotoğrafta yalnızca görünen
/// satırların küçük resimleri yüklenir).
/// </summary>
public sealed record GalleryRow(IReadOnlyList<AssetItemViewModel> Items);

/// <summary>Başlangıç ekranındaki son proje.</summary>
public sealed class RecentProjectItem(RecentProject project, bool exists, DateTimeOffset now)
{
    public RecentProject Project { get; } = project;

    public string Name => Project.Name;

    public string Path => Project.Path;

    public string Folder => System.IO.Path.GetDirectoryName(Project.Path) ?? string.Empty;

    public bool Exists { get; } = exists;

    /// <summary>"Bugün 14:32", "Dün 09:10", "3 gün önce", "12 Eki 2026".</summary>
    public string WhenText { get; } = Relative(project.OpenedAt, now);

    public static RecentProjectItem Create(RecentProject project, DateTimeOffset now) =>
        new(project, File.Exists(project.Path), now);

    public static string Relative(DateTimeOffset when, DateTimeOffset now)
    {
        var culture = CultureInfo.CurrentCulture;
        var local = when.ToLocalTime();
        var days = (now.ToLocalTime().Date - local.Date).Days;
        return days switch
        {
            <= 0 => "Bugün " + local.ToString("HH:mm", culture),
            1 => "Dün " + local.ToString("HH:mm", culture),
            < 7 => string.Format(culture, "{0} gün önce", days),
            _ => local.ToString("d MMM yyyy", culture),
        };
    }
}
