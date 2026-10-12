using System.Globalization;
using System.Security.Cryptography;
using NexAlbum.Assets.Metadata;
using NexAlbum.Core.Projects;

namespace NexAlbum.Assets.Import;

/// <summary>İçe aktarma seçenekleri.</summary>
public sealed record ImportOptions
{
    /// <summary>Desteklenen uzantılar (Karar 08 §7.1). RAW ileride (v1.2).</summary>
    public static IReadOnlySet<string> DefaultExtensions { get; } =
        new HashSet<string>([".jpg", ".jpeg", ".png", ".tif", ".tiff", ".webp", ".bmp", ".gif"], StringComparer.OrdinalIgnoreCase);

    /// <summary>Alt klasörler de taransın mı?</summary>
    public bool Recursive { get; init; }

    /// <summary>SHA-256 özeti hesaplansın mı? (Bütünlük için; büyük arşivlerde yavaştır.)</summary>
    public bool ComputeHash { get; init; }

    public IReadOnlySet<string> Extensions { get; init; } = DefaultExtensions;
}

/// <summary>Atlanan dosya ve nedeni.</summary>
public sealed record ImportProblem(string Path, string Reason);

/// <summary>İçe aktarma sonucu.</summary>
public sealed record ImportResult(
    IReadOnlyList<PhotoAsset> Imported,
    IReadOnlyList<ImportProblem> Skipped,
    IReadOnlyList<string> AlreadyInProject)
{
    /// <summary>Yeni varlıkları projeye ekler.</summary>
    public AlbumProject ApplyTo(AlbumProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        return project.AddAssets(Imported);
    }
}

/// <summary>
/// Klasör veya dosyalardan fotoğraf içe aktarır. Orijinal dosyalara dokunmaz (referans modu, Karar 09 §13.1).
/// Sıra ve kimlikler deterministiktir: dosyalar ada göre sıralanır, kimlikler "asset-0001" biçiminde sürer.
/// </summary>
public sealed class PhotoImporter(IPhotoMetadataReader reader, TimeProvider clock)
{
    public PhotoImporter()
        : this(new MetadataExtractorReader(), TimeProvider.System)
    {
    }

    public ImportResult ImportFolder(
        string folder,
        AlbumProject project,
        ImportOptions? options = null,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        options ??= new ImportOptions();
        var search = options.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.EnumerateFiles(folder, "*", search)
            .Where(f => options.Extensions.Contains(Path.GetExtension(f)));
        return ImportFiles(files, project, options, progress, cancellationToken);
    }

    public ImportResult ImportFiles(
        IEnumerable<string> files,
        AlbumProject project,
        ImportOptions? options = null,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(project);
        options ??= new ImportOptions();

        var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var known = new HashSet<string>(project.Assets.Select(a => a.SourcePath), comparer);
        var ids = new AssetIdGenerator(project.Assets.Select(a => a.Id));
        var now = clock.GetUtcNow();

        var imported = new List<PhotoAsset>();
        var skipped = new List<ImportProblem>();
        var already = new List<string>();
        var ordered = files.Select(f => Path.GetFullPath(f)).Distinct(comparer).Order(StringComparer.Ordinal).ToList();

        for (var i = 0; i < ordered.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = ordered[i];
            if (!known.Add(path))
            {
                already.Add(path);
                continue;
            }

            try
            {
                var asset = ReadAsset(path, ids, now, options);
                if (asset is null)
                {
                    skipped.Add(new ImportProblem(path, "Görüntü boyutu okunamadı; desteklenmeyen ya da bozuk dosya."));
                }
                else
                {
                    imported.Add(asset);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                skipped.Add(new ImportProblem(path, ex.Message));
            }

            progress?.Report(i + 1);
        }

        return new ImportResult(imported, skipped, already);
    }

    private PhotoAsset? ReadAsset(string path, AssetIdGenerator ids, DateTimeOffset now, ImportOptions options)
    {
        PhotoMetadata? meta;
        long size;
        string? hash = null;
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            size = stream.Length;
            meta = reader.Read(stream);
            if (meta is not null && options.ComputeHash)
            {
                stream.Position = 0;
                hash = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(stream));
            }
        }

        if (meta is null || meta.Width <= 0 || meta.Height <= 0)
        {
            return null;
        }

        return new PhotoAsset
        {
            Id = ids.Next(),
            SourcePath = path,
            SourceType = AssetSourceType.Reference,
            FileName = Path.GetFileName(path),
            FileSize = size,
            FileHash = hash,
            PixelWidth = meta.Width,
            PixelHeight = meta.Height,
            ExifOrientation = meta.Orientation,
            CapturedAt = meta.CapturedAt,
            ImportedAt = now,
            CameraModel = meta.CameraModel,
            Lens = meta.Lens,
            Iso = meta.Iso,
            FocalLength = meta.FocalLength,
        };
    }
}

/// <summary>"asset-0001" biçiminde, projede kullanılmamış kimlikler üretir.</summary>
internal sealed class AssetIdGenerator(IEnumerable<string> existing)
{
    private readonly HashSet<string> used = new(existing, StringComparer.Ordinal);
    private int next = 1;

    public string Next()
    {
        string id;
        do
        {
            id = "asset-" + next.ToString("0000", CultureInfo.InvariantCulture);
            next++;
        }
        while (!used.Add(id));

        return id;
    }
}
