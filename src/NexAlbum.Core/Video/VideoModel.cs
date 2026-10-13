using NexAlbum.Core.Common;

namespace NexAlbum.Core.Video;

/// <summary>Video biçimi (Karar 21 §2).</summary>
/// <param name="Id">Kalıcı kimlik.</param>
/// <param name="Name">Görünen ad.</param>
/// <param name="Width">Genişlik (piksel, çift sayı).</param>
/// <param name="Height">Yükseklik (piksel, çift sayı).</param>
/// <param name="Social">Sosyal medya biçimi mi (hedef süreli kısa klip).</param>
public sealed record VideoFormat(string Id, string Name, int Width, int Height, bool Social)
{
    public double Aspect => (double)Width / Height;

    public static IReadOnlyList<VideoFormat> All { get; } =
    [
        new("reels", "Dikey 9:16 · Reels / TikTok / Shorts", 1080, 1920, true),
        new("square", "Kare 1:1 · Gönderi", 1080, 1080, true),
        new("portrait", "Dikey 4:5 · Akış", 1080, 1350, true),
        new("fullhd", "Full HD 16:9 · 1920×1080", 1920, 1080, false),
        new("2k", "2K 16:9 · 2560×1440", 2560, 1440, false),
        new("4k", "4K 16:9 · 3840×2160", 3840, 2160, false),
    ];

    public static VideoFormat Find(string? id) => All.FirstOrDefault(f => f.Id == id) ?? All[0];
}

/// <summary>Geçiş türü (efekt belgesi §2.3–§2.6).</summary>
public enum TransitionKind
{
    Cut,
    Crossfade,
    DipToBlack,
    DipToWhite,
    FocusShift,
    Iris,
    PushLeft,
    PushRight,
    PushUp,
    ZoomThrough,
}

/// <summary>Videodaki görünüm (yalnızca video; baskıyı etkilemez).</summary>
public enum VideoLook
{
    Natural,
    Warm,
    Cool,
    BlackWhite,
    Sepia,
}

/// <summary>Fotoğraf üzerindeki hareket.</summary>
public enum MotionKind
{
    /// <summary>Hareketsiz (hareket azaltma).</summary>
    Still,

    /// <summary>Yavaş yakınlaşma / uzaklaşma ve kayma (Ken Burns).</summary>
    KenBurns,
}

/// <summary>Hazır stil (otomatik kip): geçiş havuzu, süreler, hareket ve görünüm (Karar 21 §3).</summary>
public sealed record VideoStyle(
    string Id,
    string Name,
    string Hint,
    IReadOnlyList<TransitionKind> Transitions,
    TransitionKind ChapterTransition,
    double PhotoSeconds,
    double TransitionSeconds,
    double MotionAmount,
    VideoLook Look,
    bool Vignette,
    bool Grain)
{
    /// <summary>Uygulamayla gelen stiller.</summary>
    public static IReadOnlyList<VideoStyle> BuiltIn { get; } =
    [
        new("elegant", "Zarif", "Yumuşak çapraz geçişler, yavaş yaklaşma; her hikâyeye yakışır.",
            [TransitionKind.Crossfade, TransitionKind.Crossfade, TransitionKind.FocusShift], TransitionKind.DipToBlack, 3.6, 1.1, 0.08, VideoLook.Natural, false, false),
        new("cinematic", "Sinematik", "Siyaha geçişler, geniş ve ağır hareket, sıcak ton ve vinyet.",
            [TransitionKind.Crossfade, TransitionKind.DipToBlack, TransitionKind.FocusShift], TransitionKind.DipToBlack, 4.4, 1.4, 0.11, VideoLook.Warm, true, false),
        new("dynamic", "Dinamik", "Kısa kesmeler, itme ve yakınlaşarak geçiş; müziğin vuruşuna oturur.",
            [TransitionKind.PushLeft, TransitionKind.ZoomThrough, TransitionKind.Cut, TransitionKind.PushUp, TransitionKind.Iris, TransitionKind.PushRight], TransitionKind.ZoomThrough, 1.8, 0.45, 0.12, VideoLook.Natural, false, false),
        new("nostalgia", "Nostalji", "Sepya, film greni ve vinyet; beyaza geçişlerle hatıra hissi.",
            [TransitionKind.Crossfade, TransitionKind.DipToWhite, TransitionKind.Crossfade], TransitionKind.DipToWhite, 3.8, 1.2, 0.07, VideoLook.Sepia, true, true),
    ];

    /// <summary>Kullanılabilir tüm stiller: yerleşikler ve yüklü eklenti paketlerindekiler (<see cref="Extensions.ExtensionCatalog.Current"/>).</summary>
    public static IReadOnlyList<VideoStyle> All => Extensions.ExtensionCatalog.Current.VideoStyles;

    /// <summary>Stil; bulunamazsa (ör. paketi kaldırılmış) ilk yerleşik stil.</summary>
    public static VideoStyle Find(string? id) => Extensions.ExtensionCatalog.Current.FindVideoStyle(id);

    /// <summary>Stili getiren eklenti paketinin adı; yerleşikse null.</summary>
    public string? Package { get; init; }

    /// <summary>Listede gösterilen ad: paket stilinde paket adıyla.</summary>
    public string DisplayName => Package is null ? Name : Name + " · " + Package;
}

/// <summary>Bölüm için elle seçilmiş geçiş ve görünüm (otomatiğin yerine geçer; null olan alan stilden gelir).</summary>
public sealed record ChapterVideoOverride(string ChapterId, TransitionKind? Transition = null, VideoLook? Look = null);

/// <summary>Projede saklanan video ayarları (Karar 21 §6, Karar 09 Rev. 4).</summary>
public sealed record VideoSettings
{
    public static VideoSettings Default { get; } = new();

    public string FormatId { get; init; } = "reels";

    public string StyleId { get; init; } = "elegant";

    /// <summary>Kare hızı: 24, 30 ya da 60.</summary>
    public int Fps { get; init => field = value is 24 or 25 or 30 or 50 or 60 ? value : throw new ArgumentOutOfRangeException(nameof(Fps)); } = 30;

    /// <summary>Hedef süre (sn); null = tüm hikâye (film).</summary>
    public double? TargetSeconds { get; init => field = value is null ? null : Guard.InRange(value.Value, 5, 3600, nameof(TargetSeconds)); }

    /// <summary>Müzik dosyası (yerel yol); yoksa sessiz.</summary>
    public string? MusicPath { get; init; }

    /// <summary>Hareket azaltma: Ken Burns ve hareketli geçişler kapalı.</summary>
    public bool ReduceMotion { get; init; }

    /// <summary>Bölüm başlarında başlık kartı.</summary>
    public bool ChapterTitles { get; init; } = true;

    public IReadOnlyList<ChapterVideoOverride> Overrides { get; init => field = value ?? throw new ArgumentNullException(nameof(Overrides)); } = [];

    public VideoSettings WithOverride(ChapterVideoOverride value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var rest = Overrides.Where(o => o.ChapterId != value.ChapterId);
        return this with { Overrides = value.Transition is null && value.Look is null ? [.. rest] : [.. rest, value] };
    }
}

/// <summary>Normalleştirilmiş kadraj: fotoğrafın görünen bölgesi (0–1, fotoğraf genişliği/yüksekliği oranı).</summary>
public readonly record struct Frame01(double X, double Y, double Width, double Height)
{
    public static Frame01 Lerp(Frame01 a, Frame01 b, double t) => new(
        a.X + ((b.X - a.X) * t), a.Y + ((b.Y - a.Y) * t), a.Width + ((b.Width - a.Width) * t), a.Height + ((b.Height - a.Height) * t));
}

/// <summary>Zaman çizelgesindeki bir öğe (fotoğraf ya da başlık kartı).</summary>
public abstract record VideoClip(double Start, double Duration, TransitionKind TransitionIn, double TransitionSeconds, VideoLook Look)
{
    public double End => Start + Duration;
}

/// <summary>Fotoğraf: kadraj başlangıçtan bitişe yumuşakça değişir.</summary>
public sealed record PhotoClip(
    double Start,
    double Duration,
    TransitionKind TransitionIn,
    double TransitionSeconds,
    VideoLook Look,
    string AssetId,
    Frame01 From,
    Frame01 To)
    : VideoClip(Start, Duration, TransitionIn, TransitionSeconds, Look)
{
    /// <summary>Fotoğraf bütün gösterilir; boşluklar fotoğrafın bulanık, karartılmış kopyasıyla dolar.</summary>
    public bool BlurredBackdrop { get; init; }
}

/// <summary>Bölüm başlığı kartı: koyu zemin, bölüm adı ve saat.</summary>
public sealed record TitleClip(
    double Start,
    double Duration,
    TransitionKind TransitionIn,
    double TransitionSeconds,
    VideoLook Look,
    string Title,
    string? Subtitle)
    : VideoClip(Start, Duration, TransitionIn, TransitionSeconds, Look);

/// <summary>Planlanmış video: öğeler (her öğe öncekiyle geçiş süresi kadar örtüşür), müzik başlangıcı, toplam süre.</summary>
public sealed record VideoTimeline(
    VideoFormat Format,
    int Fps,
    IReadOnlyList<VideoClip> Clips,
    double Duration,
    bool Vignette,
    bool Grain,
    double MusicOffset,
    IReadOnlyList<string> Notes)
{
    public int FrameCount => (int)Math.Ceiling(Duration * Fps);

    /// <summary>t anında görünen öğeler: (çıkan, giren, geçiş ilerlemesi 0–1). Geçiş yoksa giren null.</summary>
    public (VideoClip? Current, VideoClip? Next, double Progress) At(double t)
    {
        for (var i = 0; i < Clips.Count; i++)
        {
            var clip = Clips[i];
            var next = i + 1 < Clips.Count ? Clips[i + 1] : null;
            if (next is not null && t >= next.Start && t < clip.End)
            {
                var span = Math.Max(1e-6, clip.End - next.Start);
                return (clip, next, Math.Clamp((t - next.Start) / span, 0, 1));
            }

            if (t >= clip.Start && t < clip.End)
            {
                return (clip, null, 0);
            }
        }

        return (Clips.Count > 0 ? Clips[^1] : null, null, 0);
    }
}
