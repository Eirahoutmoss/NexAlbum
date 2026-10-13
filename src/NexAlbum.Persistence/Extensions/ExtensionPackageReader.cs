using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using NexAlbum.Core.Extensions;
using NexAlbum.Core.Video;
using NexAlbum.Persistence.Json;

namespace NexAlbum.Persistence.Extensions;

/// <summary>Paket okunurken karşılaşılan sorun (paket ya da tek bir öğe atlanır; uygulama çalışmaya devam eder).</summary>
public sealed record ExtensionProblem(string Source, string Message);

/// <summary>Eklenti klasörünün okunma sonucu.</summary>
public sealed record ExtensionLoadResult(IReadOnlyList<ExtensionPackage> Packages, IReadOnlyList<ExtensionProblem> Problems);

/// <summary>
/// Eklenti paketlerini okur (Karar 22). Paket ya bir klasördür (içinde <c>manifest.json</c>) ya da aynı içeriği taşıyan
/// <c>.nexpaket</c> (ZIP) dosyasıdır. Yalnızca veri okunur: betik, DLL ya da çalıştırılabilir dosya yüklenmez. Geçersiz paket
/// ya da öğe atlanır ve nedeni bildirilir; bir paketin hatası diğerlerini etkilemez.
/// </summary>
public static partial class ExtensionPackageReader
{
    /// <summary>Paket bildirim dosyasının biçim adı.</summary>
    public const string Format = "nexalbum-extension";

    /// <summary>Bu sürümün okuyabildiği en yeni paket biçimi.</summary>
    public const int FormatVersion = 1;

    /// <summary>ZIP paket uzantısı.</summary>
    public const string PackageExtension = ".nexpaket";

    /// <summary>Bildirim dosyası en fazla bu kadar olabilir (bayt).</summary>
    public const int MaxManifestBytes = 1024 * 1024;

    private static readonly TextEnum<TransitionKind> Transitions = new();
    private static readonly TextEnum<VideoLook> Looks = new();

    /// <summary>Klasördeki tüm paketleri okur (alt klasörler ve .nexpaket dosyaları, ada göre sıralı). Klasör yoksa boş.</summary>
    public static ExtensionLoadResult LoadFolder(string folder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        var packages = new List<ExtensionPackage>();
        var problems = new List<ExtensionProblem>();
        if (!Directory.Exists(folder))
        {
            return new ExtensionLoadResult(packages, problems);
        }

        var sources = Directory.EnumerateDirectories(folder)
            .Concat(Directory.EnumerateFiles(folder, "*" + PackageExtension))
            .Order(StringComparer.OrdinalIgnoreCase);
        foreach (var source in sources)
        {
            try
            {
                var json = Directory.Exists(source) ? ReadFolderManifest(source) : ReadZipManifest(source);
                if (json is null)
                {
                    continue;
                }

                var package = Read(json, source, problems);
                if (packages.Any(p => string.Equals(p.Id, package.Id, StringComparison.Ordinal)))
                {
                    problems.Add(new(source, "Aynı kimlikli paket zaten yüklü: " + package.Id));
                    continue;
                }

                packages.Add(package);
            }
            catch (Exception ex) when (ex is FormatException or JsonException or IOException or InvalidDataException or UnauthorizedAccessException)
            {
                problems.Add(new(source, ex.Message));
            }
        }

        return new ExtensionLoadResult(packages, problems);
    }

    /// <summary>Tek bir .nexpaket dosyasını okur (yüklemeden önce doğrulamak için); geçersizse <see cref="FormatException"/>.</summary>
    public static ExtensionPackage ReadPackageFile(string file)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(file);
        return Read(ReadZipManifest(file), file);
    }

    /// <summary>Bildirimi okur ve doğrular. Geçersiz öğeler atlanıp <paramref name="problems"/>'a yazılır.</summary>
    public static ExtensionPackage Read(string json, string source, List<ExtensionProblem>? problems = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        problems ??= [];
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 32, CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object || !string.Equals(Text(root, "format"), Format, StringComparison.Ordinal))
        {
            throw new FormatException("NexAlbum eklenti paketi değil (\"format\": \"" + Format + "\" bekleniyor).");
        }

        var version = root.TryGetProperty("formatVersion", out var fv) && fv.TryGetInt32(out var v) ? v : 0;
        if (version is < 1 or > FormatVersion)
        {
            throw new FormatException(string.Create(CultureInfo.InvariantCulture, $"Paket biçimi {version} desteklenmiyor (bu sürüm en fazla {FormatVersion}). NexAlbum'ü güncelleyin."));
        }

        var id = Text(root, "id") ?? throw new FormatException("Paket kimliği (\"id\") yok.");
        if (!IdPattern().IsMatch(id))
        {
            throw new FormatException("Paket kimliği yalnızca küçük harf, rakam ve tire içerebilir: " + id);
        }

        var name = Text(root, "name") ?? id;
        var packageVersion = Text(root, "version") ?? "1.0.0";
        var styles = new List<VideoStyle>();
        if (root.TryGetProperty("videoStyles", out var list) && list.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in list.EnumerateArray())
            {
                try
                {
                    styles.Add(ReadVideoStyle(item, id));
                }
                catch (FormatException ex)
                {
                    problems.Add(new(source, name + ": " + ex.Message));
                }
            }
        }

        foreach (var reserved in new[] { "textStyles", "frameStyles", "photoLooks", "templates" })
        {
            if (root.TryGetProperty(reserved, out _))
            {
                problems.Add(new(source, name + ": \"" + reserved + "\" bu sürümde henüz desteklenmiyor; atlandı."));
            }
        }

        return new ExtensionPackage(id, name, packageVersion, Text(root, "author"), Text(root, "license"), Text(root, "description"), source)
        {
            VideoStyles = styles,
        };
    }

    private static VideoStyle ReadVideoStyle(JsonElement e, string packageId)
    {
        if (e.ValueKind != JsonValueKind.Object)
        {
            throw new FormatException("Film stili nesne olmalı.");
        }

        var localId = Text(e, "id") ?? throw new FormatException("Film stilinin kimliği yok.");
        if (!IdPattern().IsMatch(localId))
        {
            throw new FormatException("Film stili kimliği geçersiz: " + localId);
        }

        var name = Text(e, "name") ?? localId;
        var transitions = new List<TransitionKind>();
        if (e.TryGetProperty("transitions", out var t) && t.ValueKind == JsonValueKind.Array)
        {
            transitions.AddRange(t.EnumerateArray().Select(x => Transitions.Parse(x.GetString(), name + ".transitions")));
        }

        if (transitions.Count == 0)
        {
            transitions.Add(TransitionKind.Crossfade);
        }

        return new VideoStyle(
            packageId + "/" + localId,
            name,
            Text(e, "hint") ?? string.Empty,
            transitions,
            Transitions.Parse(Text(e, "chapterTransition"), name + ".chapterTransition", TransitionKind.DipToBlack),
            Number(e, "photoSeconds", 3.5, 0.8, 15, name),
            Number(e, "transitionSeconds", 1.0, 0, 4, name),
            Number(e, "motion", 0.08, 0, 0.3, name),
            Looks.Parse(Text(e, "look"), name + ".look", VideoLook.Natural),
            e.TryGetProperty("vignette", out var vg) && vg.ValueKind == JsonValueKind.True,
            e.TryGetProperty("grain", out var gr) && gr.ValueKind == JsonValueKind.True);
    }

    private static double Number(JsonElement e, string property, double fallback, double min, double max, string owner)
    {
        if (!e.TryGetProperty(property, out var p))
        {
            return fallback;
        }

        if (p.ValueKind != JsonValueKind.Number || !p.TryGetDouble(out var value) || !double.IsFinite(value) || value < min || value > max)
        {
            throw new FormatException(string.Create(CultureInfo.InvariantCulture, $"{owner}.{property}: {min}–{max} arasında sayı olmalı."));
        }

        return value;
    }

    private static string? Text(JsonElement e, string property) =>
        e.TryGetProperty(property, out var p) && p.ValueKind == JsonValueKind.String && p.GetString() is { Length: > 0 } s ? s.Trim() : null;

    private static string? ReadFolderManifest(string folder)
    {
        var path = Path.Combine(folder, "manifest.json");
        if (!File.Exists(path))
        {
            return null;
        }

        if (new FileInfo(path).Length > MaxManifestBytes)
        {
            throw new FormatException("manifest.json çok büyük.");
        }

        return File.ReadAllText(path);
    }

    private static string ReadZipManifest(string file)
    {
        using var zip = ZipFile.OpenRead(file);
        var entry = zip.GetEntry("manifest.json") ?? throw new FormatException("Pakette manifest.json yok.");
        if (entry.Length > MaxManifestBytes)
        {
            throw new FormatException("manifest.json çok büyük.");
        }

        using var reader = new StreamReader(entry.Open());
        return reader.ReadToEnd();
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex IdPattern();
}
