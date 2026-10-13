using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using NexAlbum.Core.Video;

namespace NexAlbum.Video;

/// <summary>Yerel müzik parçası.</summary>
public sealed record MusicTrack(string Path, string Title, string Folder);

/// <summary>Kullanıcının seçtiği klasörlerdeki müzikler (Karar 21 §4). NexAlbum müzik dağıtmaz; telif kullanıcıdadır.</summary>
public static class MusicLibrary
{
    public static IReadOnlySet<string> Extensions { get; } = new HashSet<string>([".mp3", ".m4a", ".aac", ".wav", ".flac", ".ogg", ".opus"], StringComparer.OrdinalIgnoreCase);

    /// <summary>Klasörleri (alt klasörlerle) tarar; en fazla <paramref name="limit"/> parça, ada göre sıralı.</summary>
    public static IReadOnlyList<MusicTrack> Scan(IEnumerable<string> folders, int limit = 2000)
    {
        ArgumentNullException.ThrowIfNull(folders);
        var tracks = new List<MusicTrack>();
        foreach (var folder in folders.Where(Directory.Exists))
        {
            try
            {
                foreach (var file in Directory.EnumerateFiles(folder, "*", new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true }))
                {
                    if (Extensions.Contains(System.IO.Path.GetExtension(file)))
                    {
                        tracks.Add(new MusicTrack(file, System.IO.Path.GetFileNameWithoutExtension(file), folder));
                        if (tracks.Count >= limit)
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Erişilemeyen klasör atlanır.
            }
        }

        return [.. tracks.OrderBy(t => t.Title, StringComparer.CurrentCultureIgnoreCase)];
    }
}

/// <summary>Müziği FFmpeg ile tek kanallı PCM'e çözer ve vuruşları bulur (Karar 21 §4).</summary>
public static class AudioAnalyzer
{
    public const int SampleRate = 11025;

    /// <summary>İlk <paramref name="maxSeconds"/> saniyeyi çözümler; çözülemezse null.</summary>
    public static async Task<BeatGrid?> AnalyzeAsync(string ffmpeg, string musicPath, double maxSeconds = 900, CancellationToken cancellationToken = default)
    {
        var start = new ProcessStartInfo(ffmpeg)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var arg in new[] { "-hide_banner", "-loglevel", "error", "-i", musicPath, "-t", Num(maxSeconds), "-ac", "1", "-ar", Num(SampleRate), "-f", "f32le", "pipe:1" })
        {
            start.ArgumentList.Add(arg);
        }

        using var process = Process.Start(start);
        if (process is null)
        {
            return null;
        }

        var errors = process.StandardError.ReadToEndAsync(cancellationToken);
        using var memory = new MemoryStream();
        await process.StandardOutput.BaseStream.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        await errors.ConfigureAwait(false);
        if (process.ExitCode != 0 || memory.Length < 4)
        {
            return null;
        }

        var samples = MemoryMarshal.Cast<byte, float>(memory.GetBuffer().AsSpan(0, (int)(memory.Length / 4 * 4)));
        return BeatDetector.Detect(samples, SampleRate);
    }

    internal static string Num(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}

/// <summary>Dışa aktarma ilerlemesi.</summary>
public sealed record VideoProgress(int Frame, int TotalFrames, TimeSpan Elapsed)
{
    public double Fraction => TotalFrames == 0 ? 1 : (double)Frame / TotalFrames;

    public TimeSpan? Remaining => Frame <= 0 ? null : TimeSpan.FromSeconds(Elapsed.TotalSeconds / Frame * (TotalFrames - Frame));
}

/// <summary>
/// Kareleri FFmpeg'e ham (BGRA) verir; FFmpeg H.264 MP4 üretir, müziği ekler, sonda yumuşakça kısar (Karar 21 §2, §4).
/// Bağımsız süreçtir; iptal ya da hatada süreç sonlandırılır ve yarım dosya silinir.
/// </summary>
public sealed class FfmpegEncoder : IAsyncDisposable
{
    private readonly Process process;
    private readonly Stream input;
    private readonly Task<string> errors;
    private readonly string output;
    private bool completed;

    private FfmpegEncoder(Process process, string output)
    {
        this.process = process;
        this.output = output;
        input = process.StandardInput.BaseStream;
        errors = process.StandardError.ReadToEndAsync();
    }

    /// <summary>FFmpeg argümanları (test edilebilir; tırnak sorunu olmasın diye liste).</summary>
    public static IReadOnlyList<string> Arguments(VideoTimeline timeline, string output, string? music, double musicOffset)
    {
        ArgumentNullException.ThrowIfNull(timeline);
        var f = timeline.Format;
        var args = new List<string>
        {
            "-y", "-hide_banner", "-loglevel", "error",
            "-f", "rawvideo", "-pix_fmt", "bgra", "-s", $"{f.Width}x{f.Height}", "-r", Num(timeline.Fps), "-i", "pipe:0",
        };
        if (music is not null)
        {
            args.AddRange(["-ss", Num(musicOffset), "-i", music]);
        }

        args.AddRange(["-map", "0:v"]);
        if (music is not null)
        {
            args.AddRange(["-map", "1:a", "-c:a", "aac", "-b:a", "192k", "-af", $"afade=t=out:st={Num(Math.Max(0, timeline.Duration - 2))}:d=2"]);
        }

        var crf = Math.Max(f.Width, f.Height) >= 3000 ? "20" : "18";
        args.AddRange(["-c:v", "libx264", "-preset", "medium", "-crf", crf, "-pix_fmt", "yuv420p", "-movflags", "+faststart", "-t", Num(timeline.Duration), output]);
        return args;
    }

    public static FfmpegEncoder Start(string ffmpeg, VideoTimeline timeline, string output, string? music, double musicOffset)
    {
        var start = new ProcessStartInfo(ffmpeg)
        {
            RedirectStandardInput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var arg in Arguments(timeline, output, music, musicOffset))
        {
            start.ArgumentList.Add(arg);
        }

        var process = Process.Start(start) ?? throw new InvalidOperationException("FFmpeg başlatılamadı.");
        return new FfmpegEncoder(process, output);
    }

    public async Task WriteFrameAsync(ReadOnlyMemory<byte> bgra, CancellationToken cancellationToken)
    {
        try
        {
            await input.WriteAsync(bgra, cancellationToken).ConfigureAwait(false);
        }
        catch (IOException)
        {
            // FFmpeg erken kapandıysa asıl hata standart hata çıktısındadır.
            throw new InvalidOperationException("FFmpeg kodlamayı durdurdu: " + await ErrorsAsync().ConfigureAwait(false));
        }
    }

    public async Task CompleteAsync(CancellationToken cancellationToken)
    {
        await input.FlushAsync(cancellationToken).ConfigureAwait(false);
        input.Close();
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException("FFmpeg hatası: " + await ErrorsAsync().ConfigureAwait(false));
        }

        completed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (!completed)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
            {
            }

            try
            {
                File.Delete(output);
            }
            catch (IOException)
            {
            }
        }

        process.Dispose();
    }

    private async Task<string> ErrorsAsync()
    {
        var text = await errors.ConfigureAwait(false);
        var trimmed = text.Trim();
        return trimmed.Length > 600 ? trimmed[^600..] : trimmed;
    }

    private static string Num(double value) => AudioAnalyzer.Num(value);
}

/// <summary>Çok çekirdekte kare çizip sırayla FFmpeg'e veren dışa aktarıcı.</summary>
public static class VideoExporter
{
    /// <param name="render">t anının karesini verilen BGRA tamponuna çizer (tampon: genişlik × yükseklik × 4 bayt).</param>
    /// <param name="ffmpeg">FFmpeg yolu.</param>
    /// <param name="timeline">Zaman çizelgesi.</param>
    /// <param name="output">MP4 yolu.</param>
    /// <param name="music">Müzik dosyası (yoksa sessiz).</param>
    /// <param name="progress">İlerleme.</param>
    /// <param name="cancellationToken">İptal (yarım dosya silinir).</param>
    public static async Task ExportAsync(
        Action<double, byte[]> render,
        string ffmpeg,
        VideoTimeline timeline,
        string output,
        string? music,
        IProgress<VideoProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(render);
        ArgumentNullException.ThrowIfNull(timeline);
        var frameBytes = timeline.Format.Width * timeline.Format.Height * 4;
        var batch = Math.Clamp(Environment.ProcessorCount, 1, 8);
        var buffers = Enumerable.Range(0, batch).Select(_ => new byte[frameBytes]).ToArray();
        var clock = Stopwatch.StartNew();
        await using var encoder = FfmpegEncoder.Start(ffmpeg, timeline, output, music, timeline.MusicOffset);
        var total = timeline.FrameCount;
        for (var first = 0; first < total; first += batch)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var count = Math.Min(batch, total - first);
            var start = first;
            Parallel.For(0, count, new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = batch }, i =>
                render((start + i) / (double)timeline.Fps, buffers[i]));
            for (var i = 0; i < count; i++)
            {
                await encoder.WriteFrameAsync(buffers[i], cancellationToken).ConfigureAwait(false);
            }

            progress?.Report(new VideoProgress(first + count, total, clock.Elapsed));
        }

        await encoder.CompleteAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Arayüz için kısa metinler.</summary>
public static class VideoText
{
    public static string Duration(double seconds)
    {
        var span = TimeSpan.FromSeconds(Math.Round(seconds));
        return span.TotalHours >= 1
            ? span.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture)
            : span.ToString(@"m\:ss", CultureInfo.InvariantCulture);
    }

    public static string Describe(VideoTimeline timeline)
    {
        ArgumentNullException.ThrowIfNull(timeline);
        var sb = new StringBuilder();
        sb.Append(CultureInfo.CurrentCulture, $"{timeline.Format.Width}×{timeline.Format.Height} · {timeline.Fps} kare/sn · {Duration(timeline.Duration)}");
        sb.Append(CultureInfo.CurrentCulture, $" · {timeline.Clips.OfType<PhotoClip>().Count()} fotoğraf");
        return sb.ToString();
    }
}
