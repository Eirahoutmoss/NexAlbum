using System.Reflection;

namespace NexAlbum.UI;

/// <summary>Uygulama kimliği; Hakkında penceresi ve proje dosyaları için.</summary>
public static class AppInfo
{
    public const string Name = "NexAlbum";
    public const string Author = "Hasan Güler";
    public const string AuthorUrl = "https://github.com/Eirahoutmoss";
    public const string RepositoryUrl = "https://github.com/Eirahoutmoss/NexAlbum";
    public const string License = "GPL-3.0-or-later";

    /// <summary>"0.1.0" biçiminde sürüm ("+commit" eki olmadan).</summary>
    public static string Version { get; } = ReadVersion();

    private static string ReadVersion()
    {
        var version = typeof(AppInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
        var plus = version.IndexOf('+', StringComparison.Ordinal);
        return plus >= 0 ? version[..plus] : version;
    }
}
