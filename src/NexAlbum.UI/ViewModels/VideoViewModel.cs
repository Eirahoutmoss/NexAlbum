using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexAlbum.Core.Projects;
using NexAlbum.Core.Video;
using NexAlbum.Rendering.Video;
using NexAlbum.UI.Services;
using NexAlbum.Video;
using SkiaSharp;

namespace NexAlbum.UI.ViewModels;

/// <summary>Film penceresinin pencereye bağlı diyalogları (dosya/klasör seçimi, onay, uyarı).</summary>
public interface IVideoDialogs
{
    string? AskVideoPath(string suggestedName);

    string? AskMusicFolder();

    string? AskFfmpeg();

    bool Confirm(string message);

    void Warn(string message);

    void Reveal(string path);
}

/// <summary>Süre seçeneği (null = tüm hikâye).</summary>
public sealed record DurationOption(double? Seconds, string Name);

/// <summary>Elle seçim listesinin öğesi (null = stilden, "Otomatik").</summary>
public sealed record Choice<T>(T? Value, string Name)
    where T : struct;

/// <summary>Bölüm satırı: elle geçiş ve görünüm (Karar 21 §3, elle kip).</summary>
public sealed class ChapterVideoRow(VideoViewModel owner, StoryChapter chapter, ChapterVideoOverride? current) : ObservableObject
{
    public string Name => chapter.Name;

    public IReadOnlyList<Choice<TransitionKind>> Transitions => VideoViewModel.TransitionChoices;

    public IReadOnlyList<Choice<VideoLook>> Looks => VideoViewModel.LookChoices;

    public Choice<TransitionKind> Transition
    {
        get => Transitions.First(c => c.Value == current?.Transition);
        set => owner.SetOverride(chapter.Id, value?.Value, current?.Look);
    }

    public Choice<VideoLook> Look
    {
        get => Looks.First(c => c.Value == current?.Look);
        set => owner.SetOverride(chapter.Id, current?.Transition, value?.Value);
    }
}

/// <summary>
/// Hikâye filmi penceresi (Karar 21): biçim, stil, süre, kare hızı, müzik; elle bölüm efektleri; canlı önizleme (oynat,
/// müzikle); FFmpeg yoksa izin isteyip kurma; MP4 dışa aktarma (ilerleme, iptal). Ayarlar projeye yazılır.
/// </summary>
public sealed partial class VideoViewModel : ObservableObject, IDisposable
{
    public const int PreviewLongSide = 560;

    public static readonly IReadOnlyList<Choice<TransitionKind>> TransitionChoices =
    [
        new(null, "Otomatik"), new(TransitionKind.Crossfade, "Çapraz geçiş"), new(TransitionKind.DipToBlack, "Siyaha geçiş"),
        new(TransitionKind.DipToWhite, "Beyaza geçiş"), new(TransitionKind.FocusShift, "Odak kaydırma"), new(TransitionKind.Iris, "Dairesel iris"),
        new(TransitionKind.PushLeft, "Sola itme"), new(TransitionKind.PushRight, "Sağa itme"), new(TransitionKind.PushUp, "Yukarı itme"),
        new(TransitionKind.ZoomThrough, "Yakınlaşarak"), new(TransitionKind.Cut, "Kesme"),
    ];

    public static readonly IReadOnlyList<Choice<VideoLook>> LookChoices =
    [
        new(null, "Otomatik"), new(VideoLook.Natural, "Doğal"), new(VideoLook.Warm, "Sıcak"), new(VideoLook.Cool, "Soğuk"),
        new(VideoLook.BlackWhite, "Siyah-beyaz"), new(VideoLook.Sepia, "Sepya"),
    ];

    private static readonly MusicTrack Silence = new(string.Empty, "Müziksiz", string.Empty);
    private static readonly Dictionary<string, SKTypeface?> Typefaces = new(StringComparer.Ordinal);

    private readonly AlbumProject project;
    private readonly Action<VideoSettings> saveVideo;
    private readonly Func<AppSettings> loadSettings;
    private readonly Action<AppSettings> saveSettings;
    private readonly FilePhotoSource previewPhotos;
    private readonly DispatcherTimer playback;
    private MediaPlayer? player;
    private VideoSettings settings;
    private BeatGrid? beats;
    private FrameRenderer renderer;
    private VideoTimeline timeline;
    private double previewSeconds;
    private bool rendering;
    private double? pendingPreview;
    private DateTime playStarted;
    private double playFrom;
    private string musicStatus = string.Empty;
    private FfmpegInfo? ffmpeg;
    private bool isExporting;
    private double exportProgress;
    private string exportStatus = string.Empty;
    private string? lastOutput;
    private CancellationTokenSource? exportCancel;
    private CancellationTokenSource? analysisCancel;

    public VideoViewModel(AlbumProject project, Action<VideoSettings> saveVideo, Func<AppSettings> loadSettings, Action<AppSettings> saveSettings)
    {
        this.project = project ?? throw new ArgumentNullException(nameof(project));
        this.saveVideo = saveVideo ?? throw new ArgumentNullException(nameof(saveVideo));
        this.loadSettings = loadSettings ?? throw new ArgumentNullException(nameof(loadSettings));
        this.saveSettings = saveSettings ?? throw new ArgumentNullException(nameof(saveSettings));
        settings = project.Video;
        previewPhotos = new FilePhotoSource(project.Assets.ToDictionary(a => a.Id, StringComparer.Ordinal), capacity: 16);
        Preload("Great Vibes");
        Preload("Montserrat");

        timeline = VideoPlanner.Plan(project, settings);
        renderer = new FrameRenderer(timeline, previewPhotos, Typeface);
        playback = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(40) };
        playback.Tick += (_, _) => OnPlaybackTick();

        PlayCommand = new RelayCommand(TogglePlay, () => timeline.Clips.Count > 0 && !isExporting);
        ExportCommand = new AsyncRelayCommand(ExportAsync, () => timeline.Clips.Count > 0 && !isExporting);
        CancelExportCommand = new RelayCommand(() => exportCancel?.Cancel(), () => isExporting);
        AddMusicFolderCommand = new RelayCommand(AddMusicFolder);
        InstallFfmpegCommand = new AsyncRelayCommand(async () => await EnsureFfmpegAsync(askFirst: false).ConfigureAwait(true), () => !isExporting);
        LocateFfmpegCommand = new RelayCommand(LocateFfmpeg);
        RevealCommand = new RelayCommand(() => Dialogs?.Reveal(lastOutput!), () => lastOutput is not null);

        ffmpeg = FfmpegLocator.Find(loadSettings().FfmpegPath);
        RefreshMusic();
        Chapters = [];
        RebuildChapters();
        Replan();
    }

    public IVideoDialogs? Dialogs { get; set; }

    public static IReadOnlyList<VideoFormat> Formats => VideoFormat.All;

    public static IReadOnlyList<VideoStyle> Styles => VideoStyle.All;

    public static IReadOnlyList<int> FpsOptions { get; } = [24, 30, 60];

    public string Title => project.Name + " — Hikâye Filmi";

    public VideoFormat SelectedFormat
    {
        get => VideoFormat.Find(settings.FormatId);
        set
        {
            if (value is not null && value.Id != settings.FormatId)
            {
                // Sosyalden filme geçerken süre "tüm hikâye", filmden sosyale geçerken 30 sn.
                var target = value.Social ? settings.TargetSeconds ?? 30 : (VideoFormat.Find(settings.FormatId).Social ? null : settings.TargetSeconds);
                Update(settings with { FormatId = value.Id, TargetSeconds = target });
                OnPropertyChanged(nameof(DurationOptions));
                OnPropertyChanged(nameof(SelectedDuration));
            }
        }
    }

    public VideoStyle SelectedStyle
    {
        get => VideoStyle.Find(settings.StyleId);
        set
        {
            if (value is not null && value.Id != settings.StyleId)
            {
                Update(settings with { StyleId = value.Id });
            }
        }
    }

    public IReadOnlyList<DurationOption> DurationOptions => SelectedFormat.Social
        ? [new(15, "15 sn"), new(30, "30 sn"), new(60, "60 sn"), new(90, "90 sn")]
        : [new(null, "Tüm hikâye"), new(60, "1 dk"), new(180, "3 dk"), new(300, "5 dk")];

    public DurationOption SelectedDuration
    {
        get => DurationOptions.FirstOrDefault(d => d.Seconds == settings.TargetSeconds) ?? DurationOptions[0];
        set
        {
            if (value is not null && value.Seconds != settings.TargetSeconds)
            {
                Update(settings with { TargetSeconds = value.Seconds });
            }
        }
    }

    public int SelectedFps
    {
        get => settings.Fps;
        set
        {
            if (value != settings.Fps && FpsOptions.Contains(value))
            {
                Update(settings with { Fps = value });
            }
        }
    }

    public bool ChapterTitles
    {
        get => settings.ChapterTitles;
        set => Update(settings with { ChapterTitles = value });
    }

    public bool ReduceMotion
    {
        get => settings.ReduceMotion;
        set => Update(settings with { ReduceMotion = value });
    }

    public ObservableCollection<MusicTrack> MusicTracks { get; } = [];

    public MusicTrack SelectedTrack
    {
        get => MusicTracks.FirstOrDefault(t => string.Equals(t.Path, settings.MusicPath ?? string.Empty, StringComparison.OrdinalIgnoreCase)) ?? Silence;
        set
        {
            var path = value is null || value == Silence ? null : value.Path;
            if (!string.Equals(path, settings.MusicPath, StringComparison.OrdinalIgnoreCase))
            {
                beats = null;
                Update(settings with { MusicPath = path });
                _ = AnalyzeMusicAsync();
            }
        }
    }

    public bool HasMusicFolders => loadSettings().MusicFolders.Count > 0;

    /// <summary>"118 BPM · kesmeler vuruşa oturur" / "Çözümleniyor…" / uyarılar.</summary>
    public string MusicStatus
    {
        get => musicStatus;
        private set => SetProperty(ref musicStatus, value);
    }

    public ObservableCollection<ChapterVideoRow> Chapters { get; }

    public bool HasChapters => Chapters.Count > 0;

    public VideoTimeline Timeline => timeline;

    public string Summary => timeline.Clips.Count == 0 ? "Videoya girecek fotoğraf yok." : VideoText.Describe(timeline);

    public string Notes => string.Join("  ·  ", timeline.Notes);

    public WriteableBitmap? Preview { get; private set; }

    public double Duration => timeline.Duration;

    public double PreviewSeconds
    {
        get => previewSeconds;
        set
        {
            if (SetProperty(ref previewSeconds, Math.Clamp(value, 0, Math.Max(0, timeline.Duration - 0.01))))
            {
                OnPropertyChanged(nameof(TimeText));
                RequestPreview();
                if (IsPlaying && Math.Abs(previewSeconds - ExpectedPlayPosition()) > 0.3)
                {
                    StartPlayback(previewSeconds);
                }
            }
        }
    }

    public string TimeText => VideoText.Duration(previewSeconds) + " / " + VideoText.Duration(timeline.Duration);

    public bool IsPlaying => playback.IsEnabled;

    public bool HasFfmpeg => ffmpeg is not null;

    public string FfmpegStatus => ffmpeg is { } f
        ? "FFmpeg hazır · " + f.Version.Replace("ffmpeg version ", string.Empty, StringComparison.Ordinal).Split(' ')[0]
        : "FFmpeg kurulu değil: dışa aktarırken kurulması için izin istenir.";

    public bool IsExporting
    {
        get => isExporting;
        private set
        {
            if (SetProperty(ref isExporting, value))
            {
                RefreshCommands();
            }
        }
    }

    public double ExportProgress
    {
        get => exportProgress;
        private set => SetProperty(ref exportProgress, value);
    }

    public string ExportStatus
    {
        get => exportStatus;
        private set => SetProperty(ref exportStatus, value);
    }

    public RelayCommand PlayCommand { get; }

    public AsyncRelayCommand ExportCommand { get; }

    public RelayCommand CancelExportCommand { get; }

    public RelayCommand AddMusicFolderCommand { get; }

    public AsyncRelayCommand InstallFfmpegCommand { get; }

    public RelayCommand LocateFfmpegCommand { get; }

    public RelayCommand RevealCommand { get; }

    /// <summary>Önizleme ölçüsü: uzun kenar <see cref="PreviewLongSide"/>, oran videonunki.</summary>
    public (int Width, int Height) PreviewSize
    {
        get
        {
            var f = timeline.Format;
            var scale = (double)PreviewLongSide / Math.Max(f.Width, f.Height);
            return (Math.Max(2, (int)Math.Round(f.Width * scale / 2) * 2), Math.Max(2, (int)Math.Round(f.Height * scale / 2) * 2));
        }
    }

    public void SetOverride(string chapterId, TransitionKind? transition, VideoLook? look) =>
        Update(settings.WithOverride(new ChapterVideoOverride(chapterId, transition, look)));

    /// <summary>Önizlemeyi senkron çizer (testler ve ilk açılış).</summary>
    public void RenderPreviewNow()
    {
        var (w, h) = PreviewSize;
        var buffer = new byte[w * h * 4];
        renderer.RenderInto(buffer, w, h, previewSeconds);
        Present(buffer, w, h);
    }

    /// <summary>
    /// FFmpeg'i hazırlar (Karar 21 §5): bulunursa döner; yoksa izin ister ve Windows'ta indirip kurar. Reddedilirse ya da
    /// kurulamazsa null.
    /// </summary>
    public async Task<FfmpegInfo?> EnsureFfmpegAsync(bool askFirst = true)
    {
        ffmpeg ??= FfmpegLocator.Find(loadSettings().FfmpegPath);
        if (ffmpeg is not null)
        {
            return ffmpeg;
        }

        if (!FfmpegInstaller.CanInstall)
        {
            Dialogs?.Warn("Hikâyeleri videoya dönüştürmek için FFmpeg gerekiyor.\n\n" + FfmpegInstaller.ManualHint);
            return null;
        }

        if (askFirst && Dialogs?.Confirm(
                "Hikâyeleri videoya dönüştürmek için FFmpeg eklentisi gerekiyor (ücretsiz, açık kaynak, GPL; yaklaşık 100 MB).\n\n" +
                "İndirilip kurulmasına izin veriyor musunuz?\n\nKaynak: " + FfmpegInstaller.WindowsPackage.Host + " (BtbN/FFmpeg-Builds)") != true)
        {
            return null;
        }

        IsExporting = true;
        ExportStatus = "FFmpeg indiriliyor…";
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            var progress = new Progress<double>(p =>
            {
                ExportProgress = p < 0 ? 0 : p;
                ExportStatus = p < 0 ? "FFmpeg indiriliyor…" : string.Format(CultureInfo.CurrentCulture, "FFmpeg indiriliyor… %{0:0}", p * 100);
            });
            ffmpeg = await new FfmpegInstaller(http).InstallAsync(progress, CancellationToken.None).ConfigureAwait(true);
            ExportStatus = "FFmpeg kuruldu.";
            OnPropertyChanged(nameof(HasFfmpeg));
            OnPropertyChanged(nameof(FfmpegStatus));
            _ = AnalyzeMusicAsync();
            return ffmpeg;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidOperationException or TaskCanceledException or UnauthorizedAccessException or InvalidDataException)
        {
            ExportStatus = "FFmpeg kurulamadı.";
            Dialogs?.Warn("FFmpeg kurulamadı: " + ex.Message + "\n\nİnternet bağlantınızı denetleyin ya da Ayarlar'dan elle gösterin.");
            return null;
        }
        finally
        {
            ExportProgress = 0;
            IsExporting = false;
        }
    }

    public void Dispose()
    {
        playback.Stop();
        player?.Close();
        exportCancel?.Cancel();
        analysisCancel?.Cancel();
        previewPhotos.Dispose();
    }

    private static void Preload(string family)
    {
        lock (Typefaces)
        {
            if (Typefaces.ContainsKey(family))
            {
                return;
            }

            SKTypeface? typeface = null;
            if (AlbumFonts.Resolve(family, bold: false, italic: false) is { } face && AlbumFonts.Load(face.Face) is { } bytes)
            {
                typeface = SKTypeface.FromData(SKData.CreateCopy(bytes));
            }

            Typefaces[family] = typeface;
        }
    }

    private static SKTypeface? Typeface(string family)
    {
        lock (Typefaces)
        {
            return Typefaces.GetValueOrDefault(family);
        }
    }

    private void Update(VideoSettings value)
    {
        if (value == settings)
        {
            return;
        }

        settings = value;
        saveVideo(value);
        Replan();
        RebuildChapters();
    }

    private void Replan()
    {
        timeline = VideoPlanner.Plan(project with { Video = settings }, settings, beats);
        renderer = new FrameRenderer(timeline, previewPhotos, Typeface);
        previewSeconds = Math.Min(previewSeconds, Math.Max(0, timeline.Duration - 0.01));
        OnPropertyChanged(string.Empty);
        RequestPreview();
        RefreshCommands();
    }

    private void RebuildChapters()
    {
        Chapters.Clear();
        foreach (var chapter in project.Story.Chapters)
        {
            Chapters.Add(new ChapterVideoRow(this, chapter, settings.Overrides.FirstOrDefault(o => o.ChapterId == chapter.Id)));
        }

        OnPropertyChanged(nameof(HasChapters));
    }

    private void RefreshMusic()
    {
        var keep = settings.MusicPath;
        MusicTracks.Clear();
        MusicTracks.Add(Silence);
        foreach (var track in MusicLibrary.Scan(loadSettings().MusicFolders))
        {
            MusicTracks.Add(track);
        }

        // Projede seçili parça klasör dışındaysa da listede görünsün.
        if (keep is not null && File.Exists(keep) && MusicTracks.All(t => !string.Equals(t.Path, keep, StringComparison.OrdinalIgnoreCase)))
        {
            MusicTracks.Add(new MusicTrack(keep, Path.GetFileNameWithoutExtension(keep), Path.GetDirectoryName(keep) ?? string.Empty));
        }

        OnPropertyChanged(nameof(SelectedTrack));
        OnPropertyChanged(nameof(HasMusicFolders));
        _ = AnalyzeMusicAsync();
    }

    private void AddMusicFolder()
    {
        if (Dialogs?.AskMusicFolder() is not { } folder)
        {
            return;
        }

        var current = loadSettings();
        if (!current.MusicFolders.Contains(folder, StringComparer.OrdinalIgnoreCase))
        {
            saveSettings(current with { MusicFolders = [.. current.MusicFolders, folder] });
        }

        RefreshMusic();
    }

    private void LocateFfmpeg()
    {
        if (Dialogs?.AskFfmpeg() is not { } path)
        {
            return;
        }

        if (FfmpegLocator.Verify(path) is { } found)
        {
            ffmpeg = found;
            saveSettings(loadSettings() with { FfmpegPath = path });
            OnPropertyChanged(nameof(HasFfmpeg));
            OnPropertyChanged(nameof(FfmpegStatus));
            _ = AnalyzeMusicAsync();
        }
        else
        {
            Dialogs.Warn("Seçilen dosya çalışan bir FFmpeg değil.");
        }
    }

    private async Task AnalyzeMusicAsync()
    {
        analysisCancel?.Cancel();
        var path = settings.MusicPath;
        if (path is null)
        {
            MusicStatus = "Müziksiz: geçişler stilin ritmiyle.";
            return;
        }

        if (!File.Exists(path))
        {
            MusicStatus = "Müzik dosyası bulunamadı.";
            return;
        }

        if (ffmpeg is null)
        {
            MusicStatus = "Vuruşa oturtma için FFmpeg gerekiyor (dışa aktarırken kurulabilir).";
            return;
        }

        var cancel = analysisCancel = new CancellationTokenSource();
        MusicStatus = "Müzik çözümleniyor…";
        try
        {
            var grid = await Task.Run(() => AudioAnalyzer.AnalyzeAsync(ffmpeg.Path, path, cancellationToken: cancel.Token), cancel.Token).ConfigureAwait(true);
            if (cancel.IsCancellationRequested || !string.Equals(path, settings.MusicPath, StringComparison.Ordinal))
            {
                return;
            }

            beats = grid;
            MusicStatus = grid is { IsUsable: true }
                ? string.Format(CultureInfo.CurrentCulture, "{0:0} BPM · {1} · kesmeler vuruşa oturur", grid.Bpm, VideoText.Duration(grid.Duration))
                : grid is null ? "Müzik okunamadı." : "Belirgin vuruş yok; stilin ritmi kullanılır.";
            Replan();
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task ExportAsync()
    {
        StopPlayback();
        if (await EnsureFfmpegAsync().ConfigureAwait(true) is not { } tool)
        {
            return;
        }

        var f = timeline.Format;
        var name = string.Format(CultureInfo.InvariantCulture, "{0} - {1}.mp4", project.Name, f.Id);
        if (Dialogs?.AskVideoPath(name) is not { } output)
        {
            return;
        }

        var cancel = exportCancel = new CancellationTokenSource();
        IsExporting = true;
        ExportProgress = 0;
        ExportStatus = "Hazırlanıyor…";
        var plan = timeline;
        using var photos = new FilePhotoSource(project.Assets.ToDictionary(a => a.Id, StringComparer.Ordinal), capacity: 12);
        var full = new FrameRenderer(plan, photos, Typeface);
        var progress = new Progress<VideoProgress>(p =>
        {
            ExportProgress = p.Fraction;
            ExportStatus = string.Format(CultureInfo.CurrentCulture, "Kare {0} / {1}", p.Frame, p.TotalFrames)
                + (p.Remaining is { } r ? " · kalan " + VideoText.Duration(r.TotalSeconds) : string.Empty);
        });
        try
        {
            await Task.Run(() => VideoExporter.ExportAsync((t, buffer) => full.RenderInto(buffer, f.Width, f.Height, t), tool.Path, plan, output, settings.MusicPath, progress, cancel.Token), cancel.Token).ConfigureAwait(true);
            lastOutput = output;
            ExportProgress = 1;
            ExportStatus = "Film hazır: " + Path.GetFileName(output);
        }
        catch (OperationCanceledException)
        {
            ExportStatus = "Dışa aktarma iptal edildi.";
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            ExportStatus = "Dışa aktarılamadı.";
            Dialogs?.Warn("Film dışa aktarılamadı: " + ex.Message);
        }
        finally
        {
            IsExporting = false;
            exportCancel = null;
            RefreshCommands();
        }
    }

    private void TogglePlay()
    {
        if (IsPlaying)
        {
            StopPlayback();
        }
        else
        {
            StartPlayback(previewSeconds >= timeline.Duration - 0.1 ? 0 : previewSeconds);
        }
    }

    private void StartPlayback(double from)
    {
        playFrom = from;
        playStarted = DateTime.UtcNow;
        previewSeconds = from;
        if (settings.MusicPath is { } music && File.Exists(music))
        {
            player ??= new MediaPlayer();
            player.Open(new Uri(music));
            player.Position = TimeSpan.FromSeconds(timeline.MusicOffset + from);
            player.Play();
        }

        playback.Start();
        OnPropertyChanged(nameof(IsPlaying));
    }

    private void StopPlayback()
    {
        playback.Stop();
        player?.Stop();
        OnPropertyChanged(nameof(IsPlaying));
    }

    private double ExpectedPlayPosition() => playFrom + (DateTime.UtcNow - playStarted).TotalSeconds;

    private void OnPlaybackTick()
    {
        var t = ExpectedPlayPosition();
        if (t >= timeline.Duration)
        {
            StopPlayback();
            t = timeline.Duration - 0.01;
        }

        previewSeconds = Math.Max(0, t);
        OnPropertyChanged(nameof(PreviewSeconds));
        OnPropertyChanged(nameof(TimeText));
        RequestPreview();
    }

    /// <summary>Önizlemeyi arka planda çizer; çizim sürerken gelen istekler en sona birleşir.</summary>
    private void RequestPreview()
    {
        if (SynchronizationContext.Current is null)
        {
            // Arayüz iş parçacığı dışında (testler): senkron çiz.
            RenderPreviewNow();
            return;
        }

        if (rendering)
        {
            pendingPreview = previewSeconds;
            return;
        }

        rendering = true;
        var t = previewSeconds;
        var current = renderer;
        var (w, h) = PreviewSize;
        _ = Task.Run(() =>
        {
            var buffer = new byte[w * h * 4];
            current.RenderInto(buffer, w, h, t);
            return buffer;
        }).ContinueWith(
            task =>
            {
                rendering = false;
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    Present(task.Result, w, h);
                }

                if (pendingPreview is not null)
                {
                    pendingPreview = null;
                    RequestPreview();
                }
            },
            TaskScheduler.FromCurrentSynchronizationContext());
    }

    private void Present(byte[] buffer, int w, int h)
    {
        if (Preview is null || Preview.PixelWidth != w || Preview.PixelHeight != h)
        {
            Preview = new WriteableBitmap(w, h, 96, 96, PixelFormats.Pbgra32, null);
            OnPropertyChanged(nameof(Preview));
        }

        Preview.WritePixels(new Int32Rect(0, 0, w, h), buffer, w * 4, 0);
    }

    private void RefreshCommands()
    {
        PlayCommand.NotifyCanExecuteChanged();
        ExportCommand.NotifyCanExecuteChanged();
        CancelExportCommand.NotifyCanExecuteChanged();
        InstallFfmpegCommand.NotifyCanExecuteChanged();
        RevealCommand.NotifyCanExecuteChanged();
    }
}
