using System.Windows.Media;

namespace NexAlbum.UI.Services;

/// <summary>Kullanılabilir yazı tipleri (ön kontrol: eksik font uyarısı, Karar 11 §9.2 #13).</summary>
public static class FontCatalog
{
    private static readonly Lazy<HashSet<string>> Installed = new(() =>
        new HashSet<string>(Fonts.SystemFontFamilies.Select(f => f.Source), StringComparer.OrdinalIgnoreCase));

    /// <summary>Gömülü albüm yazı tipi ya da sistemde kurulu.</summary>
    public static bool IsInstalled(string family) => AlbumFonts.IsAvailable(family);

    /// <summary>Yalnızca sistemde kurulu yazı tipleri.</summary>
    public static bool IsSystemFont(string family) => Installed.Value.Contains(family);
}
