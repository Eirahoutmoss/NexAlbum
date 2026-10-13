using System.Diagnostics;
using NexAlbum.Core.Projects;

namespace NexAlbum.Integrations.Editing;

/// <summary>Düzenlenmiş dosyaların klasörü (Karar 20 §4).</summary>
public static class EditFolders
{
    /// <summary>Varsayılan kök: Belgeler/NexAlbum/Düzenlenmiş.</summary>
    public static string DefaultRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "NexAlbum", "Düzenlenmiş");

    /// <summary>Projenin klasörü: <c>&lt;kök&gt;/&lt;proje adı&gt;-&lt;kimlik8&gt;</c>. Ad değişse de kimlik sayesinde karışmaz.</summary>
    public static string ForProject(string root, AlbumProject project)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        ArgumentNullException.ThrowIfNull(project);
        var invalid = Path.GetInvalidFileNameChars();
        var name = new string([.. project.Name.Select(c => invalid.Contains(c) || c is '/' or '\\' or ':' ? '_' : c)]).Trim().TrimEnd('.');
        if (name.Length > 40)
        {
            name = name[..40].TrimEnd();
        }

        return Path.Combine(root, (name.Length == 0 ? "proje" : name) + "-" + project.Id.ToString("N")[..8]);
    }
}

/// <summary>
/// "Düzenleyicide aç" (Karar 20 §1): orijinalin kopyası (ya da sürmekte olan düzenleme) harici düzenleyicide açılır;
/// kullanıcı kaydedince <see cref="EditFileWatcher"/> haber verir.
/// </summary>
public static class ExternalEditor
{
    /// <summary>
    /// Düzenlenecek dosyayı hazırlar. Bir düzenleme varsa o dosya açılır (çalışma sürer); yoksa orijinal
    /// <c>&lt;id&gt;-elle&lt;uzantı&gt;</c> adıyla kopyalanır. Orijinale asla yazılmaz.
    /// </summary>
    public static string PrepareWorkingFile(PhotoAsset asset, string folder)
    {
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        if (asset.Edit is { } edit && File.Exists(edit.Path))
        {
            return edit.Path;
        }

        if (asset.SourceType != AssetSourceType.Reference || !File.Exists(asset.SourcePath))
        {
            throw new FileNotFoundException("Orijinal dosya bulunamadı.", asset.SourcePath);
        }

        Directory.CreateDirectory(folder);
        var target = Path.GetFullPath(Path.Combine(folder, asset.Id + "-elle" + Path.GetExtension(asset.SourcePath).ToLowerInvariant()));
        File.Copy(asset.SourcePath, target, overwrite: true);
        return target;
    }

    /// <summary>
    /// Dosyayı düzenleyicide açar. <paramref name="executable"/> null ise Windows'un "düzenle" eylemi kullanılır
    /// (yoksa varsayılan uygulama ile açılır).
    /// </summary>
    public static void Open(string file, string? executable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(file);
        if (!string.IsNullOrWhiteSpace(executable))
        {
            var info = new ProcessStartInfo(executable) { UseShellExecute = false };
            info.ArgumentList.Add(file);
            using var started = Process.Start(info);
            return;
        }

        try
        {
            using var edit = Process.Start(new ProcessStartInfo(file) { UseShellExecute = true, Verb = "edit" });
        }
        catch (System.ComponentModel.Win32Exception)
        {
            using var open = Process.Start(new ProcessStartInfo(file) { UseShellExecute = true });
        }
    }
}

/// <summary>
/// Tek bir dosyanın kaydedilmesini izler. Düzenleyiciler çoğu zaman geçici dosyaya yazıp yeniden adlandırır ya da
/// birkaç adımda yazar; bu yüzden olaylar sessizlik süresi boyunca birleştirilir ve dosya okunabilir olunca haber verilir.
/// </summary>
public sealed class EditFileWatcher : IDisposable
{
    private readonly FileSystemWatcher watcher;
    private readonly Timer timer;
    private readonly TimeSpan quiet;
    private readonly Lock gate = new();
    private DateTime lastReported;
    private bool disposed;

    public EditFileWatcher(string path, TimeSpan? quiet = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Path = System.IO.Path.GetFullPath(path);
        this.quiet = quiet ?? TimeSpan.FromMilliseconds(800);
        lastReported = File.Exists(Path) ? File.GetLastWriteTimeUtc(Path) : DateTime.MinValue;
        timer = new Timer(_ => Fire(), null, Timeout.Infinite, Timeout.Infinite);
        watcher = new FileSystemWatcher(System.IO.Path.GetDirectoryName(Path)!, System.IO.Path.GetFileName(Path))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName | NotifyFilters.CreationTime,
        };
        watcher.Changed += (_, _) => Poke();
        watcher.Created += (_, _) => Poke();
        watcher.Renamed += (_, e) =>
        {
            if (string.Equals(e.FullPath, Path, StringComparison.OrdinalIgnoreCase))
            {
                Poke();
            }
        };
        watcher.EnableRaisingEvents = true;
    }

    /// <summary>İzlenen dosya.</summary>
    public string Path { get; }

    /// <summary>Dosya kaydedildi ve okunabilir (iş parçacığı havuzundan çağrılır).</summary>
    public event EventHandler<string>? Saved;

    public void Dispose()
    {
        lock (gate)
        {
            disposed = true;
        }

        watcher.Dispose();
        timer.Dispose();
    }

    private void Poke()
    {
        lock (gate)
        {
            if (!disposed)
            {
                timer.Change(quiet, Timeout.InfiniteTimeSpan);
            }
        }
    }

    private void Fire()
    {
        DateTime written;
        try
        {
            if (!File.Exists(Path))
            {
                return;
            }

            written = File.GetLastWriteTimeUtc(Path);
            using var probe = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        catch (IOException)
        {
            Poke();
            return;
        }
        catch (UnauthorizedAccessException)
        {
            Poke();
            return;
        }

        lock (gate)
        {
            if (disposed || written == lastReported)
            {
                return;
            }

            lastReported = written;
        }

        Saved?.Invoke(this, Path);
    }
}
