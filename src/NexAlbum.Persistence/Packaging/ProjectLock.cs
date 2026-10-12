using System.Text.Json;
using NexAlbum.Persistence.Json;

namespace NexAlbum.Persistence.Packaging;

/// <summary>Kilidi tutan örneğin bilgisi.</summary>
public sealed record LockInfo(int ProcessId, string User, string Machine, DateTimeOffset AcquiredAt);

/// <summary>
/// Proje açıkken oluşturulan kilit dosyası (Karar 09 §15.4). İkinci bir örnek aynı projeyi açamaz;
/// çökme sonrası kalan kilit kullanıcı onayıyla <see cref="ForceRelease"/> ile kaldırılır.
/// </summary>
public sealed class ProjectLock : IDisposable
{
    private readonly FileStream stream;
    private bool disposed;

    private ProjectLock(string path, FileStream stream, LockInfo info)
    {
        Path = path;
        this.stream = stream;
        Info = info;
    }

    public string Path { get; }

    public LockInfo Info { get; }

    /// <summary>Kilidi almaya çalışır. Başkası tutuyorsa false ve (okunabiliyorsa) sahibinin bilgisi döner.</summary>
    public static bool TryAcquire(string projectPath, TimeProvider clock, out ProjectLock? acquired, out LockInfo? holder)
    {
        ArgumentNullException.ThrowIfNull(clock);
        var path = ProjectFiles.LockPath(projectPath);
        var info = new LockInfo(Environment.ProcessId, Environment.UserName, Environment.MachineName, clock.GetUtcNow());
        try
        {
            var stream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.DeleteOnClose);
            JsonSerializer.Serialize(stream, info, JsonDefaults.Options);
            stream.Flush(flushToDisk: true);
            acquired = new ProjectLock(path, stream, info);
            holder = null;
            return true;
        }
        catch (IOException) when (File.Exists(path))
        {
            acquired = null;
            holder = ReadHolder(path);
            return false;
        }
    }

    /// <summary>Çökme sonrası kalan kilidi kaldırır. Yalnızca kullanıcı onayıyla çağrılmalıdır.</summary>
    public static void ForceRelease(string projectPath)
    {
        var path = ProjectFiles.LockPath(projectPath);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            stream.Dispose();
        }
    }

    private static LockInfo? ReadHolder(string path)
    {
        try
        {
            using var s = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            return JsonSerializer.Deserialize<LockInfo>(s, JsonDefaults.Options);
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
