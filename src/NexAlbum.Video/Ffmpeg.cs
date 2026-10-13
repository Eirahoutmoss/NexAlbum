using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;

namespace NexAlbum.Video;

/// <summary>Bulunan FFmpeg: çalıştırılabilir dosya ve sürüm satırı.</summary>
public sealed record FfmpegInfo(string Path, string Version);

/// <summary>
/// FFmpeg'i bulur (Karar 21 §5): Ayarlar'daki yol → NexAlbum araç klasörü → PATH. Her aday "-version" ile doğrulanır;
/// çalışmayan dosya bulunmuş sayılmaz.
/// </summary>
public static class FfmpegLocator
{
    private static string Executable => OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";

    /// <summary>NexAlbum'ün indirdiği araçların klasörü (%LOCALAPPDATA%\NexAlbum\tools\ffmpeg).</summary>
    public static string ToolsFolder => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NexAlbum", "tools", "ffmpeg");

    public static FfmpegInfo? Find(string? configured = null)
    {
        foreach (var candidate in Candidates(configured))
        {
            if (Verify(candidate) is { } info)
            {
                return info;
            }
        }

        return null;
    }

    /// <summary>Dosyayı "-version" ile çalıştırır; ilk satır "ffmpeg version …" ise geçerlidir.</summary>
    public static FfmpegInfo? Verify(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            using var process = Process.Start(new ProcessStartInfo(path, "-hide_banner -version")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            if (process is null)
            {
                return null;
            }

            var first = process.StandardOutput.ReadLine() ?? string.Empty;
            process.StandardOutput.ReadToEnd();
            if (!process.WaitForExit(5000))
            {
                process.Kill(entireProcessTree: true);
                return null;
            }

            return process.ExitCode == 0 && first.StartsWith("ffmpeg version", StringComparison.OrdinalIgnoreCase)
                ? new FfmpegInfo(path, first.Trim())
                : null;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException)
        {
            return null;
        }
    }

    private static IEnumerable<string> Candidates(string? configured)
    {
        if (!string.IsNullOrWhiteSpace(configured))
        {
            yield return Directory.Exists(configured) ? System.IO.Path.Combine(configured, Executable) : configured;
            yield return System.IO.Path.Combine(configured, "bin", Executable);
        }

        yield return System.IO.Path.Combine(ToolsFolder, "bin", Executable);
        if (Directory.Exists(ToolsFolder))
        {
            foreach (var nested in Directory.EnumerateFiles(ToolsFolder, Executable, SearchOption.AllDirectories).Order(StringComparer.Ordinal))
            {
                yield return nested;
            }
        }

        foreach (var folder in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(System.IO.Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            yield return System.IO.Path.Combine(folder.Trim('"'), Executable);
        }
    }
}

/// <summary>
/// Kullanıcı onayıyla FFmpeg kurar (Karar 21 §5): Windows'ta resmi GPL derlemesi (BtbN/FFmpeg-Builds) indirilir, araç
/// klasörüne açılır ve doğrulanır. Diğer sistemlerde paket yöneticisi önerilir. İnternet yalnızca bu adımda kullanılır.
/// </summary>
public sealed class FfmpegInstaller(HttpClient http)
{
    /// <summary>Windows 64 bit GPL derlemesi (yaklaşık 100 MB zip).</summary>
    public static Uri WindowsPackage { get; } = new("https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip");

    public static bool CanInstall => OperatingSystem.IsWindows() && Environment.Is64BitOperatingSystem;

    /// <summary>Kurulamayan sistemde kullanıcıya gösterilecek yol.</summary>
    public static string ManualHint => OperatingSystem.IsLinux()
        ? "Terminalde: sudo apt install ffmpeg (ya da dağıtımınızın paket yöneticisi)."
        : "ffmpeg.org adresinden indirip Ayarlar'da ffmpeg dosyasının yerini gösterin.";

    /// <param name="progress">0–1 indirme ilerlemesi (boyut bilinmiyorsa −1).</param>
    /// <param name="cancellationToken">İptal.</param>
    public async Task<FfmpegInfo> InstallAsync(IProgress<double>? progress, CancellationToken cancellationToken)
    {
        if (!CanInstall)
        {
            throw new PlatformNotSupportedException(ManualHint);
        }

        var temp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nexalbum-ffmpeg-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
        var zip = temp + ".zip";
        try
        {
            using (var response = await http.GetAsync(WindowsPackage, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                var total = response.Content.Headers.ContentLength;
                await using var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                await using var target = File.Create(zip);
                var buffer = new byte[1 << 16];
                long done = 0;
                int read;
                while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    done += read;
                    progress?.Report(total is > 0 ? (double)done / total.Value : -1);
                }
            }

            ZipFile.ExtractToDirectory(zip, temp);
            var exe = Directory.EnumerateFiles(temp, "ffmpeg.exe", SearchOption.AllDirectories).FirstOrDefault()
                ?? throw new InvalidOperationException("İndirilen pakette ffmpeg.exe bulunamadı.");

            // Paketin "bin" klasörünün üstü araç klasörü olur (…\tools\ffmpeg\bin\ffmpeg.exe).
            var root = Directory.GetParent(System.IO.Path.GetDirectoryName(exe)!)!.FullName;
            if (Directory.Exists(FfmpegLocator.ToolsFolder))
            {
                Directory.Delete(FfmpegLocator.ToolsFolder, recursive: true);
            }

            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(FfmpegLocator.ToolsFolder)!);
            Directory.Move(root, FfmpegLocator.ToolsFolder);
            return FfmpegLocator.Verify(System.IO.Path.Combine(FfmpegLocator.ToolsFolder, "bin", "ffmpeg.exe"))
                ?? throw new InvalidOperationException("FFmpeg kuruldu ama çalıştırılamadı.");
        }
        finally
        {
            TryDelete(zip);
            if (Directory.Exists(temp))
            {
                try
                {
                    Directory.Delete(temp, recursive: true);
                }
                catch (IOException)
                {
                }
            }
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
    }
}
