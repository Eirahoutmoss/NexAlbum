using System.Windows.Media;

namespace NexAlbum.UI.Services;

/// <summary>Sistemde yüklü yazı tipleri (ön kontrol: eksik font uyarısı, Karar 11 §9.2 #13).</summary>
public static class FontCatalog
{
    private static readonly Lazy<HashSet<string>> Installed = new(() =>
        new HashSet<string>(Fonts.SystemFontFamilies.Select(f => f.Source), StringComparer.OrdinalIgnoreCase));

    public static bool IsInstalled(string family) => Installed.Value.Contains(family);
}
