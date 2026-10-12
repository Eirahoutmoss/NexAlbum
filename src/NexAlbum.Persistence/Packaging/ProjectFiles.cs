using System.Globalization;

namespace NexAlbum.Persistence.Packaging;

/// <summary>
/// Proje dosyasının yanındaki yardımcı dosyaların adları (Karar 09 §14.5, §15).
/// </summary>
public static class ProjectFiles
{
    public static string TempPath(string projectPath) => projectPath + ".tmp";

    public static string BackupPath(string projectPath) => projectPath + ".bak";

    public static string AutosavePath(string projectPath) => projectPath + ".autosave";

    public static string LockPath(string projectPath) => projectPath + ".lock";

    /// <summary>Göç öncesi yedek: proje.albumproj.bak-v1.</summary>
    public static string MigrationBackupPath(string projectPath, int fromVersion) =>
        projectPath + ".bak-v" + fromVersion.ToString(CultureInfo.InvariantCulture);

    /// <summary>Ana dosyadan daha yeni bir otomatik kayıt var mı? (Çökme sonrası kurtarma, Karar 09 §15.3.)</summary>
    public static bool HasNewerAutosave(string projectPath)
    {
        var autosave = AutosavePath(projectPath);
        return File.Exists(autosave)
            && (!File.Exists(projectPath) || File.GetLastWriteTimeUtc(autosave) > File.GetLastWriteTimeUtc(projectPath));
    }
}
