using FluentAssertions;
using NexAlbum.Core.Video;
using NexAlbum.Rendering.Video;
using Xunit;

namespace NexAlbum.Video.Tests;

public class FfmpegTests
{
    private static VideoTimeline Short(double seconds = 1.5) => new(
        new VideoFormat("test", "Deneme", 160, 90, false),
        24,
        [
            new PhotoClip(0, 1, TransitionKind.Cut, 0, VideoLook.Natural, "red", new Frame01(0, 0, 1, 1), new Frame01(0, 0, 1, 1)),
            new PhotoClip(0.6, seconds - 0.6, TransitionKind.Crossfade, 0.4, VideoLook.Warm, "blue", new Frame01(0, 0, 1, 1), new Frame01(0, 0, 1, 1)),
        ],
        seconds,
        true,
        true,
        0,
        []);

    [Fact]
    public void EncoderArgumentsDescribeTheVideoAndMusic()
    {
        var args = FfmpegEncoder.Arguments(Short(), "çıktı.mp4", "müzik.mp3", 1.25);

        args.Should().ContainInOrder("-f", "rawvideo", "-pix_fmt", "bgra", "-s", "160x90", "-r", "24", "-i", "pipe:0");
        args.Should().ContainInOrder("-ss", "1.25", "-i", "müzik.mp3");
        args.Should().Contain("afade=t=out:st=0:d=2", "kısa videoda kısılma baştan başlar");
        args.Should().ContainInOrder("-c:v", "libx264").And.ContainInOrder("-t", "1.5", "çıktı.mp4");
        args[^1].Should().Be("çıktı.mp4");
        FfmpegEncoder.Arguments(Short(), "a.mp4", null, 0).Should().NotContain("-c:a", "müziksiz video sessizdir");
    }

    [Fact]
    public void LocatorRejectsFilesThatAreNotFfmpeg()
    {
        FfmpegLocator.Verify(Path.Combine(Path.GetTempPath(), "yok", "ffmpeg")).Should().BeNull();
        var notFfmpeg = Path.GetTempFileName();
        try
        {
            FfmpegLocator.Verify(notFfmpeg).Should().BeNull();
        }
        finally
        {
            File.Delete(notFfmpeg);
        }

        FfmpegLocator.ToolsFolder.Should().EndWith(Path.Combine("NexAlbum", "tools", "ffmpeg"));
        FfmpegInstaller.ManualHint.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void MusicLibraryFindsAudioFiles()
    {
        var dir = Directory.CreateTempSubdirectory("nexalbum-muzik-").FullName;
        try
        {
            Directory.CreateDirectory(Path.Combine(dir, "Düğün"));
            File.WriteAllText(Path.Combine(dir, "Düğün", "İlk Dans.mp3"), "x");
            File.WriteAllText(Path.Combine(dir, "Açılış.FLAC"), "x");
            File.WriteAllText(Path.Combine(dir, "not.txt"), "x");

            var tracks = MusicLibrary.Scan([dir, Path.Combine(dir, "yok")]);

            tracks.Select(t => t.Title).Should().BeEquivalentTo(["İlk Dans", "Açılış"]);
            MusicLibrary.Scan([dir], limit: 1).Should().HaveCount(1);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>
    /// Uçtan uca: kareler çizilir, FFmpeg MP4 üretir, müzik eklenir; ardından müzik çözümlenip vuruşlar bulunur.
    /// CI'da Linux'ta FFmpeg kuruludur (test.yml); Windows'ta FFmpeg yoksa bu bütünleşme adımı çalışmaz.
    /// </summary>
    [Fact]
    public async Task FramesAreEncodedToMp4WithMusic()
    {
        var ffmpeg = FfmpegLocator.Find();
        if (ffmpeg is null)
        {
            OperatingSystem.IsLinux().Should().BeFalse("Linux CI'da FFmpeg kurulu olmalı");
            return;
        }

        ffmpeg.Version.Should().StartWith("ffmpeg version");
        var dir = Directory.CreateTempSubdirectory("nexalbum-video-").FullName;
        try
        {
            // 1. Müzik: FFmpeg ile 120 BPM tıklar (lavfi), 4 sn.
            var music = Path.Combine(dir, "tik.wav");
            await Run(ffmpeg.Path, "-y", "-hide_banner", "-loglevel", "error", "-f", "lavfi", "-i", "sine=frequency=880:beep_factor=8:duration=4", music);
            var grid = await AudioAnalyzer.AnalyzeAsync(ffmpeg.Path, music);
            grid.Should().NotBeNull();
            grid!.Duration.Should().BeApproximately(4, 0.1);

            // 2. Video.
            var timeline = Short();
            var renderer = new FrameRenderer(timeline, new ColorPhotos());
            var output = Path.Combine(dir, "film.mp4");
            var reports = new List<VideoProgress>();
            await VideoExporter.ExportAsync((t, buffer) => renderer.RenderInto(buffer, 160, 90, t), ffmpeg.Path, timeline, output, music, new Progress<VideoProgress>(reports.Add), CancellationToken.None);

            File.Exists(output).Should().BeTrue();
            new FileInfo(output).Length.Should().BeGreaterThan(1000);
            timeline.FrameCount.Should().Be(36);

            // 3. İptal: yarım dosya silinir.
            var cancelled = Path.Combine(dir, "iptal.mp4");
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();
            var act = () => VideoExporter.ExportAsync((t, b) => renderer.RenderInto(b, 160, 90, t), ffmpeg.Path, timeline, cancelled, null, null, cts.Token);
            await act.Should().ThrowAsync<OperationCanceledException>();
            File.Exists(cancelled).Should().BeFalse();
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private static async Task Run(string exe, params string[] args)
    {
        var start = new System.Diagnostics.ProcessStartInfo(exe) { UseShellExecute = false, RedirectStandardError = true };
        foreach (var a in args)
        {
            start.ArgumentList.Add(a);
        }

        using var process = System.Diagnostics.Process.Start(start)!;
        var errors = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        process.ExitCode.Should().Be(0, errors);
    }
}
