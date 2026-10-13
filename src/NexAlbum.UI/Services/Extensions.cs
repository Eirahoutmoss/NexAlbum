using System.IO;
using NexAlbum.Core.Extensions;
using NexAlbum.Persistence.Extensions;

namespace NexAlbum.UI.Services;

/// <summary>
/// Eklenti paketleri (Karar 22): <c>%LOCALAPPDATA%\NexAlbum\eklentiler</c> klasöründeki paketler açılışta okunur ve stil kataloğu
/// kurulur. Paket yüklemek = .nexpaket dosyasını (ya da paket klasörünü) bu klasöre kopyalayıp yeniden okumak.
/// </summary>
public static class ExtensionService
{
    /// <summary>Kullanıcının eklenti klasörü.</summary>
    public static string Folder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NexAlbum", "eklentiler");

    /// <summary>Son okumanın sorunları (ayarlar/hakkında penceresinde gösterilir).</summary>
    public static IReadOnlyList<ExtensionProblem> Problems { get; private set; } = [];

    /// <summary>Klasörü okur ve kataloğu etkinleştirir. Sonuç: yüklü paketler.</summary>
    public static IReadOnlyList<ExtensionPackage> Reload(string? folder = null)
    {
        var result = ExtensionPackageReader.LoadFolder(folder ?? Folder);
        Problems = result.Problems;
        var catalog = new ExtensionCatalog(result.Packages);
        ExtensionCatalog.Use(catalog);
        return catalog.Packages;
    }

    /// <summary>.nexpaket dosyasını eklenti klasörüne kopyalar (aynı adlı paketin üzerine yazar) ve yeniden okur.</summary>
    public static ExtensionPackage Install(string packageFile, string? folder = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageFile);
        var target = folder ?? Folder;

        // Önce doğrula: geçersiz dosya klasöre girmesin.
        var probe = ExtensionPackageReader.ReadPackageFile(packageFile);
        Directory.CreateDirectory(target);
        File.Copy(packageFile, Path.Combine(target, probe.Id + ExtensionPackageReader.PackageExtension), overwrite: true);
        Reload(target);
        return ExtensionCatalog.Current.Packages.FirstOrDefault(p => string.Equals(p.Id, probe.Id, StringComparison.Ordinal)) ?? probe;
    }
}
