using NexAlbum.Persistence.Format;

namespace NexAlbum.Persistence.Packaging;

/// <summary>
/// Paket içi yolların güvenliği (Karar 09 §21.1, §21.3): mutlak yol ve "../" reddedilir.
/// </summary>
public static class PackagePaths
{
    /// <summary>Yolu "/" ayraçlı göreceli biçime getirir; güvensizse <see cref="ProjectFormatException"/> fırlatır.</summary>
    public static string Normalize(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        var p = path.Replace('\\', '/');
        if (p.Length == 0
            || p.StartsWith('/')
            || (p.Length >= 2 && p[1] == ':')
            || p.Contains('\0', StringComparison.Ordinal))
        {
            throw Unsafe(path);
        }

        var segments = p.Split('/');
        foreach (var segment in segments)
        {
            if (segment is "" or "." or "..")
            {
                throw Unsafe(path);
            }
        }

        return p;
    }

    public static bool IsSafe(string path)
    {
        try
        {
            Normalize(path);
            return true;
        }
        catch (ProjectFormatException)
        {
            return false;
        }
    }

    private static ProjectFormatException Unsafe(string path) =>
        new(ProjectLoadError.UnsafeArchive, $"Güvensiz paket yolu: '{path}'.");
}
