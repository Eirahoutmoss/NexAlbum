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

    // Pro efektler (Karar 23): hesap Core'da (Effects.TransitionEffects), video, sunum ve HTML provada aynı.
    Aperture,
    Diaphragm,
    DoubleIris,
    DiamondReveal,
    RackFocus,
    TiltShift,
    DepthFocus,
    LightLeak,
    FilmBurn,
    Flash,
    Projector,
    Curtain,
    Blinds,
    DiagonalWipe,
    Mosaic,
    Honeycomb,
    InkBlot,
    Split,
    Parallax,
    WhipPan,
    CardStack,
    PolaroidDevelop,
    PageTurn,
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

/// <summary>Başlık kartında yazının girişi (Karar 23).</summary>
public enum TitleAnimation
{
    /// <summary>Yavaş yaklaşma (varsayılan).</summary>
    Fade,

    /// <summary>Satırlar maskenin altından yükselir.</summary>
    Rise,

    /// <summary>Yazı kalemle yazılıyormuş gibi soldan sağa belirir.</summary>
    Handwriting,

    /// <summary>Kelimeler sırayla bulanıktan netleşir.</summary>
    BlurWords,
}

/// <summary>Birden çok fotoğrafı bir arada gösteren sahne (Karar 23).</summary>
public enum VideoScene
{
    /// <summary>Her fotoğraf ayrı kare.</summary>
    None,

    /// <summary>Odak galerisi: yatay şerit kayar, ortadaki net, yanlar bulanık.</summary>
    FocusGallery,

    /// <summary>Kolaj akışı: ızgara hücreleri sırayla dolar.</summary>
    Collage,
}

/// <summary>Filmin açılışı.</summary>
public enum VideoIntro
{
    /// <summary>Başlık kartı.</summary>
    Title,

    /// <summary>Albüm açılışı: kapak açılır, ilk çift sayfa görünür.</summary>
    Book,
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

        // Pro efekt stilleri (Karar 23).
        new("aperture", "Aperture", "Objektif irisi gibi açılan geçişler, kenarda ince halka; başlıklar maskeden yükselir.",
            [TransitionKind.Aperture, TransitionKind.Aperture, TransitionKind.RackFocus, TransitionKind.DoubleIris], TransitionKind.Diaphragm, 3.8, 1.1, 0.09, VideoLook.Natural, true, false)
        { Title = TitleAnimation.Rise },
        new("objektif", "Objektif", "Netlik kaydırma, derinlikten gelen kareler ve tilt-shift; sakin ve sinematik.",
            [TransitionKind.RackFocus, TransitionKind.DepthFocus, TransitionKind.TiltShift, TransitionKind.RackFocus], TransitionKind.Diaphragm, 3.6, 1.2, 0.08, VideoLook.Natural, false, false)
        { Title = TitleAnimation.BlurWords },
        new("odak-galerisi", "Odak Galerisi", "Bölümün fotoğrafları yatay şeritte akar; ortadaki net, yanlar bulanık.",
            [TransitionKind.RackFocus, TransitionKind.Crossfade], TransitionKind.Aperture, 3.4, 1.1, 0.06, VideoLook.Natural, false, false)
        { Title = TitleAnimation.Rise, Scene = VideoScene.FocusGallery },
        new("isik", "Işık", "Sıcak ışık sızıntıları, yanıklar ve flaşlar; yaz ve kutlama filmleri için.",
            [TransitionKind.LightLeak, TransitionKind.Crossfade, TransitionKind.LightLeak, TransitionKind.Flash], TransitionKind.FilmBurn, 3.4, 1.2, 0.1, VideoLook.Warm, true, false)
        { Title = TitleAnimation.Handwriting },
        new("analog", "Analog", "Projektör titremesi, Polaroid banyosu ve film yanığı; gren ve vinyetle.",
            [TransitionKind.Projector, TransitionKind.PolaroidDevelop, TransitionKind.FilmBurn, TransitionKind.Crossfade], TransitionKind.Projector, 3.6, 1.1, 0.07, VideoLook.Warm, true, true)
        { Title = TitleAnimation.Handwriting },
        new("geometri", "Geometri", "Petek, mozaik, elmas ve panjur; geometrik şablonlarla uyumlu modern akış.",
            [TransitionKind.Honeycomb, TransitionKind.Mosaic, TransitionKind.DiamondReveal, TransitionKind.Blinds, TransitionKind.DiagonalWipe], TransitionKind.Honeycomb, 3.2, 1.0, 0.08, VideoLook.Natural, false, false)
        { Title = TitleAnimation.Rise },
        new("kolaj", "Kolaj Akışı", "Fotoğraflar dörtlü ızgaralarda sırayla yerleşir; albüm sayfası gibi.",
            [TransitionKind.Curtain, TransitionKind.DiagonalWipe], TransitionKind.Curtain, 3.4, 1.0, 0.05, VideoLook.Natural, false, false)
        { Title = TitleAnimation.Rise, Scene = VideoScene.Collage },
        new("sahne", "Sahne", "Perde ve yarıktan açılan kareler, paralaks kayma; tiyatro havası.",
            [TransitionKind.Curtain, TransitionKind.Split, TransitionKind.Parallax], TransitionKind.Curtain, 3.6, 1.1, 0.08, VideoLook.Natural, true, false)
        { Title = TitleAnimation.Rise },
        new("album", "Albüm", "Albüm kapağı açılarak başlar; sayfa çevirme, kart yığını ve Polaroid.",
            [TransitionKind.PageTurn, TransitionKind.CardStack, TransitionKind.PolaroidDevelop, TransitionKind.Crossfade], TransitionKind.PageTurn, 3.8, 1.2, 0.07, VideoLook.Warm, false, false)
        { Title = TitleAnimation.Handwriting, Intro = VideoIntro.Book },
        new("murekkep", "Mürekkep", "Organik mürekkep lekeleri ve çift iris; yumuşak ve sanatsal.",
            [TransitionKind.InkBlot, TransitionKind.DoubleIris, TransitionKind.Crossfade], TransitionKind.InkBlot, 3.8, 1.3, 0.08, VideoLook.Natural, false, false)
        { Title = TitleAnimation.BlurWords },
        new("hiz", "Hız", "Hızlı kaydırma, flaş ve yakınlaşma; kısa sosyal medya klipleri için.",
            [TransitionKind.WhipPan, TransitionKind.ZoomThrough, TransitionKind.Flash, TransitionKind.PushLeft, TransitionKind.Cut], TransitionKind.WhipPan, 1.6, 0.4, 0.12, VideoLook.Natural, false, false)
        { Title = TitleAnimation.Rise },
    ];

    /// <summary>Başlık kartlarında yazının girişi.</summary>
    public TitleAnimation Title { get; init; } = TitleAnimation.Fade;

    /// <summary>Çok fotoğraflı sahne (galeri, kolaj); yoksa her fotoğraf ayrı.</summary>
    public VideoScene Scene { get; init; } = VideoScene.None;

    /// <summary>Açılış: başlık kartı ya da albüm açılışı.</summary>
    public VideoIntro Intro { get; init; } = VideoIntro.Title;

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
    : VideoClip(Start, Duration, TransitionIn, TransitionSeconds, Look)
{
    /// <summary>Yazının girişi.</summary>
    public TitleAnimation Animation { get; init; } = TitleAnimation.Fade;
}

/// <summary>Odak galerisi: fotoğraflar yatay şeritte kayar; ortadaki net (Karar 23).</summary>
public sealed record GalleryClip(
    double Start,
    double Duration,
    TransitionKind TransitionIn,
    double TransitionSeconds,
    VideoLook Look,
    IReadOnlyList<string> AssetIds,
    string? Caption)
    : VideoClip(Start, Duration, TransitionIn, TransitionSeconds, Look);

/// <summary>Kolaj akışı: ızgara hücreleri sırayla dolar (Karar 23).</summary>
public sealed record CollageClip(
    double Start,
    double Duration,
    TransitionKind TransitionIn,
    double TransitionSeconds,
    VideoLook Look,
    IReadOnlyList<string> AssetIds)
    : VideoClip(Start, Duration, TransitionIn, TransitionSeconds, Look);

/// <summary>Albüm açılışı: kapak (albüm adı) açılır, ilk çift sayfa görünür ve kareyi doldurur (Karar 23).</summary>
public sealed record BookClip(
    double Start,
    double Duration,
    TransitionKind TransitionIn,
    double TransitionSeconds,
    VideoLook Look,
    string Title,
    string? Subtitle,
    string? LeftAssetId,
    string? RightAssetId)
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
