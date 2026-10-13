using System.IO;
using System.Text.Json;

namespace NexAlbum.UI.Services;

/// <summary>
/// Bu bilgisayara özgü uygulama ayarları (Karar 08 §8: %APPDATA%/NexAlbum/settings.json). Proje dosyasına girmez.
/// </summary>
public sealed record AppSettings
{
    /// <summary>"Düzenleyicide aç" için program; null ise Photoshop aranır, o da yoksa Windows'un "düzenle" eylemi.</summary>
    public string? EditorPath { get; init; }

    /// <summary>Düzenlenmiş dosyaların kök klasörü; null ise Belgeler/NexAlbum/Düzenlenmiş.</summary>
    public string? EditsFolder { get; init; }

    /// <summary>Komut satırı araçlarının son kullanılan yolları (hazır ayar kimliği → yol).</summary>
    public Dictionary<string, string> ToolPaths { get; init; } = new(StringComparer.Ordinal);

    /// <summary>Arayüz teması: "Gece" (varsayılan) ya da "Fildisi" (Karar 17). Null ya da tanınmayan değer = Gece.</summary>
    public string? Theme { get; init; }

    /// <summary>Tuvalde karşılıklı iki sayfa (kitap görünümü) gösterilsin mi?</summary>
    public bool BookView { get; init; }

    /// <summary>Son açılan/kaydedilen projeler, en yenisi başta (başlangıç ekranı).</summary>
    public IReadOnlyList<RecentProject> RecentProjects { get; init; } = [];
}

/// <summary>Başlangıç ekranındaki son proje kaydı.</summary>
public sealed record RecentProject(string Path, string Name, DateTimeOffset OpenedAt);

/// <summary>Ayarların okunup yazıldığı yer; testte bellek içi kullanılır.</summary>
public interface IAppSettingsStore
{
    AppSettings Load();

    void Save(AppSettings settings);
}

/// <summary>JSON dosyası. Bozuk ya da okunamayan dosya varsayılanlarla değiştirilir; uygulama asla bu yüzden açılmamazlık etmez.</summary>
public sealed class JsonAppSettingsStore(string? path = null) : IAppSettingsStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly string path = path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NexAlbum", "settings.json");

    public AppSettings Load()
    {
        try
        {
            return File.Exists(path) ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), Options) ?? new AppSettings() : new AppSettings();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(settings, Options));
        File.Move(temp, path, overwrite: true);
    }
}

/// <summary>Bellek içi ayarlar (testler).</summary>
public sealed class MemoryAppSettingsStore(AppSettings? initial = null) : IAppSettingsStore
{
    private AppSettings current = initial ?? new AppSettings();

    public AppSettings Load() => current;

    public void Save(AppSettings settings) => current = settings ?? throw new ArgumentNullException(nameof(settings));
}
