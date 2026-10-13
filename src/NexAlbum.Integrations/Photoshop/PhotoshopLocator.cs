using Microsoft.Win32;

namespace NexAlbum.Integrations.Photoshop;

/// <summary>Kurulu Photoshop'un yerini bulur (Windows kayıt defteri; en yeni sürüm).</summary>
public static class PhotoshopLocator
{
    /// <summary>Photoshop.exe'nin yolu; bulunamazsa null.</summary>
    public static string? FindExecutable()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        try
        {
            using var root = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Adobe\Photoshop");
            if (root is null)
            {
                return null;
            }

            foreach (var version in root.GetSubKeyNames().OrderByDescending(VersionKey))
            {
                using var key = root.OpenSubKey(version);
                if (key?.GetValue("ApplicationPath") is string folder)
                {
                    var exe = Path.Combine(folder, "Photoshop.exe");
                    if (File.Exists(exe))
                    {
                        return exe;
                    }
                }
            }
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
        }

        return null;
    }

    private static double VersionKey(string name) =>
        double.TryParse(name, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0;
}
