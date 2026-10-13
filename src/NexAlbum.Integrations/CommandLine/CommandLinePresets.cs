using NexAlbum.Core.Projects;

namespace NexAlbum.Integrations.CommandLine;

/// <summary>Hazır komut satırı aracı: bilinen kurulum yerleri ve başlangıç bağımsız değişkenleri.</summary>
public sealed record CommandLinePreset(string Id, string Name, IReadOnlyList<string> Arguments, string Hint, IReadOnlyList<string> KnownPaths)
{
    /// <summary>Bu bilgisayarda bulunan ilk kurulum; yoksa null.</summary>
    public string? Locate(Func<string, bool>? exists = null)
    {
        exists ??= File.Exists;
        return KnownPaths.FirstOrDefault(exists);
    }

    /// <summary>Hazır ayardan tarif taslağı (yol bulunamazsa boş bırakılır; kullanıcı seçer).</summary>
    public EditRecipe ToRecipe(string name, string? executable) => new()
    {
        Engine = EditEngineKind.CommandLine,
        Name = name,
        Executable = executable,
        Arguments = Arguments,
    };
}

/// <summary>
/// Ücretsiz araçlar için başlangıç ayarları (Karar 20 §3). Yollar Windows varsayılan kurulum yerleridir;
/// sürüm klasörü değişebileceğinden RawTherapee için olası klasörler taranır.
/// </summary>
public static class CommandLinePresets
{
    private static readonly string ProgramFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

    /// <summary>darktable: girdi, (isteğe bağlı) stil .xmp, çıktı. Stil eklemek için {input} ile {output} arasına .xmp yolunu yazın.</summary>
    public static CommandLinePreset Darktable { get; } = new(
        "darktable",
        "darktable-cli",
        ["{input}", "{output}", "--core", "--conf", "plugins/imageio/format/jpeg/quality=97"],
        "Stil uygulamak için {input} ile {output} arasına darktable'dan dışa aktardığınız .xmp dosyasının yolunu ekleyin.",
        [Path.Combine(ProgramFiles, "darktable", "bin", "darktable-cli.exe"), "/usr/bin/darktable-cli"]);

    /// <summary>RawTherapee: -p ile işleme profili (.pp3), -j97 JPEG kalitesi; -c en sonda olmalı.</summary>
    public static CommandLinePreset RawTherapee { get; } = new(
        "rawtherapee",
        "rawtherapee-cli",
        ["-o", "{output}", "-p", "profil.pp3", "-j97", "-Y", "-c", "{input}"],
        "«profil.pp3» yerine RawTherapee'de kaydettiğiniz işleme profilinin tam yolunu yazın.",
        [.. RawTherapeeCandidates(), "/usr/bin/rawtherapee-cli"]);

    /// <summary>Genel: herhangi bir araç; yer tutucular kullanıcı tarafından yerleştirilir.</summary>
    public static CommandLinePreset Custom { get; } = new(
        "custom",
        "Özel araç",
        ["{input}", "{output}"],
        "Aracın beklediği sırayla bağımsız değişkenleri yazın; {input} orijinal fotoğraf, {output} yazılacak dosyadır.",
        []);

    public static IReadOnlyList<CommandLinePreset> All { get; } = [Darktable, RawTherapee, Custom];

    private static IEnumerable<string> RawTherapeeCandidates()
    {
        var root = Path.Combine(ProgramFiles, "RawTherapee");
        yield return Path.Combine(root, "rawtherapee-cli.exe");
        string[] versions;
        try
        {
            versions = Directory.Exists(root) ? Directory.GetDirectories(root) : [];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            versions = [];
        }

        foreach (var dir in versions.OrderDescending(StringComparer.OrdinalIgnoreCase))
        {
            yield return Path.Combine(dir, "rawtherapee-cli.exe");
        }
    }
}
