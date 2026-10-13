using NexAlbum.Core.Projects;

namespace NexAlbum.Assets.Import;

/// <summary>
/// Kaynak dosya denetimi ve yeniden bağlama (Karar 09 §7.4).
/// </summary>
public static class AssetFiles
{
    /// <summary>
    /// Referans modundaki her varlığın dosyasını denetler ve <see cref="PhotoAsset.Missing"/> bayrağını günceller;
    /// düzenlenmiş sürümlerin dosyaları da denetlenir (Karar 20).
    /// Gömülü varlıklar paketin içindedir; dokunulmaz.
    /// </summary>
    public static AlbumProject RefreshMissing(AlbumProject project, Func<string, bool>? exists = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        exists ??= File.Exists;
        var updated = project;
        foreach (var asset in project.Assets.Where(a => a.SourceType == AssetSourceType.Reference))
        {
            var missing = !exists(asset.SourcePath);
            if (missing != asset.Missing)
            {
                updated = updated.ReplaceAsset(asset with { Missing = missing });
            }
        }

        return PhotoEdits.RefreshMissing(updated, exists);
    }

    /// <summary>Eksik varlıkların listesi.</summary>
    public static IReadOnlyList<PhotoAsset> Missing(AlbumProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        return [.. project.Assets.Where(a => a.Missing)];
    }

    /// <summary>Taşınan bir dosyayı yeni yoluna bağlar ("Yeniden bağla").</summary>
    public static AlbumProject Relink(AlbumProject project, string assetId, string newPath, Func<string, bool>? exists = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(newPath);
        exists ??= File.Exists;
        var asset = project.FindAsset(assetId) ?? throw new KeyNotFoundException($"Varlık bulunamadı: {assetId}");
        var full = Path.GetFullPath(newPath);
        return project.ReplaceAsset(asset with { SourcePath = full, FileName = Path.GetFileName(full), Missing = !exists(full) });
    }

    /// <summary>
    /// Eksik dosyaları verilen klasörde aynı adla arar ve bulduklarını bağlar (toplu yeniden bağlama).
    /// </summary>
    public static AlbumProject RelinkFromFolder(AlbumProject project, string folder, bool recursive = true)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        var byName = Directory.EnumerateFiles(folder, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
            .GroupBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() == 1)
            .ToDictionary(g => g.Key!, g => g.Single(), StringComparer.OrdinalIgnoreCase);

        var updated = project;
        foreach (var asset in project.Assets.Where(a => a.Missing))
        {
            if (byName.TryGetValue(asset.FileName, out var found))
            {
                updated = Relink(updated, asset.Id, found);
            }
        }

        return updated;
    }
}
