using NexAlbum.Core.Video;

namespace NexAlbum.Core.Extensions;

/// <summary>
/// Yüklü bir eklenti paketi (Karar 22): yalnızca veri taşır (stil tanımları), kod çalıştırmaz. Kimlik paketler arasında eşsizdir;
/// paketin getirdiği öğelerin kimlikleri "paket-kimliği/öğe-kimliği" biçimindedir, yerleşiklerle çakışmaz.
/// </summary>
/// <param name="Id">Paket kimliği (küçük harf, rakam, tire).</param>
/// <param name="Name">Görünen ad.</param>
/// <param name="Version">Paket sürümü (SemVer).</param>
/// <param name="Author">Yayıncı; yoksa null.</param>
/// <param name="License">Lisans (ör. CC-BY-4.0); yoksa null.</param>
/// <param name="Description">Kısa açıklama; yoksa null.</param>
/// <param name="Source">Paketin okunduğu dosya ya da klasör.</param>
public sealed record ExtensionPackage(
    string Id,
    string Name,
    string Version,
    string? Author,
    string? License,
    string? Description,
    string Source)
{
    /// <summary>Pakette gelen film stilleri (kimlikleri paket önekli).</summary>
    public IReadOnlyList<VideoStyle> VideoStyles { get; init; } = [];

    /// <summary>"Pro Sinema 1.2.0 · 3 film stili"</summary>
    public string Summary => Name + " " + Version + " · " + VideoStyles.Count.ToString(System.Globalization.CultureInfo.CurrentCulture) + " film stili";
}

/// <summary>
/// Stillerin tek kaynağı (Karar 22): yerleşik stiller + yüklü eklenti paketlerindekiler. Uygulama açılışta paketleri okuyup
/// <see cref="Use"/> ile etkinleştirir; Core'daki stil aramaları (<see cref="VideoStyle.Find"/>) buradan yapılır. Değiştirilemez:
/// yeni paket yüklenince yeni katalog kurulur. Bilinmeyen stil kimliği (paketi kaldırılmış proje) yerleşik ilk stile düşer;
/// proje dosyasındaki kimlik silinmez, paket geri yüklenince yeniden çalışır.
/// </summary>
public sealed class ExtensionCatalog
{
    private static ExtensionCatalog current = new([]);

    public ExtensionCatalog(IEnumerable<ExtensionPackage> packages)
    {
        ArgumentNullException.ThrowIfNull(packages);

        // Aynı kimlikli ikinci paket yok sayılır (ilk okunan kazanır); stil kimlikleri de eşsiz kalır.
        var list = new List<ExtensionPackage>();
        var styles = new List<VideoStyle>(VideoStyle.BuiltIn);
        var ids = new HashSet<string>(styles.Select(s => s.Id), StringComparer.Ordinal);
        foreach (var package in packages)
        {
            if (list.Any(p => string.Equals(p.Id, package.Id, StringComparison.Ordinal)))
            {
                continue;
            }

            var accepted = package.VideoStyles.Where(s => ids.Add(s.Id)).Select(s => s with { Package = package.Name }).ToList();
            styles.AddRange(accepted);
            list.Add(package with { VideoStyles = accepted });
        }

        Packages = list;
        VideoStyles = styles;
    }

    /// <summary>Etkin katalog (açılışta paketlerle kurulur; başlangıçta yalnızca yerleşikler).</summary>
    public static ExtensionCatalog Current => Volatile.Read(ref current);

    public IReadOnlyList<ExtensionPackage> Packages { get; }

    public IReadOnlyList<VideoStyle> VideoStyles { get; }

    /// <summary>Kataloğu etkinleştirir.</summary>
    public static void Use(ExtensionCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        Volatile.Write(ref current, catalog);
    }

    public VideoStyle FindVideoStyle(string? id) =>
        VideoStyles.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.Ordinal)) ?? VideoStyles[0];

    /// <summary>Stil bu katalogda var mı (paketi eksik projeyi uyarmak için)?</summary>
    public bool HasVideoStyle(string? id) => VideoStyles.Any(s => string.Equals(s.Id, id, StringComparison.Ordinal));
}
