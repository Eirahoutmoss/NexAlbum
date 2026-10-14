using NexAlbum.Core.Common;
using NexAlbum.Core.Video;

namespace NexAlbum.Core.Effects;

/// <summary>Efekt alanında nokta. Alan: genişlik = en/boy oranı, yükseklik = 1 (daireler her biçimde daire kalır).</summary>
public readonly record struct EffectPoint(double X, double Y);

/// <summary>
/// Bir katmanın (çıkan ya da giren görüntü) o andaki hâli. Dönüşüm sırası: ölçek (pivot çevresinde) → döndürme (pivot
/// çevresinde, derece) → kaydırma. Bulanıklık kısa kenarın oranıdır (0,03 = kısa kenarın %3'ü).
/// </summary>
public sealed record EffectLayer
{
    public double Scale { get; init; } = 1;

    public double ScaleX { get; init; } = 1;

    public double ScaleY { get; init; } = 1;

    public double OffsetX { get; init; }

    public double OffsetY { get; init; }

    public double Rotation { get; init; }

    public double PivotX { get; init; }

    public double PivotY { get; init; } = 0.5;

    public double Opacity { get; init; } = 1;

    public double Blur { get; init; }

    /// <summary>Doygunluk çarpanı (0 = siyah-beyaz).</summary>
    public double Saturation { get; init; } = 1;

    /// <summary>Parlaklık çarpanı (renk kanalları; 1 = değişmez).</summary>
    public double Brightness { get; init; } = 1;

    /// <summary>Gölge gücü (0–1): katmanın altına yumuşak gölge.</summary>
    public double Shadow { get; init; }

    /// <summary>Katman görünür mü (çizimden önce hızlı eleme).</summary>
    public bool Visible => Opacity > 0.001;
}

/// <summary>Katmanların üstüne çizilen ışık/çizgi öğesi.</summary>
public abstract record EffectOverlay;

/// <summary>İnce halka (iris kenarı).</summary>
public sealed record RingOverlay(double Cx, double Cy, double R, double Width, double Alpha, RgbaColor Color) : EffectOverlay;

/// <summary>Tüm kareyi kaplayan renk (flaş, karartma).</summary>
public sealed record SolidOverlay(RgbaColor Color, double Alpha) : EffectOverlay;

/// <summary>Işıma: merkezden dışa sönen renk, ekran karışımıyla (ışık sızıntısı, yanık).</summary>
public sealed record GlowOverlay(double Cx, double Cy, double R, RgbaColor Color, double Alpha) : EffectOverlay;

/// <summary>Dikey bant: x0'dan x1'e alfa0 → alfa1 geçişli renk (sayfa kıvrımı, kenar gölgesi).</summary>
public sealed record BandOverlay(double X0, double X1, double Alpha0, double Alpha1, RgbaColor Color) : EffectOverlay;

/// <summary>
/// Geçişin bir anı: çıkan ve giren katman, giren katmanın maskesi (çokgenler birleşimi; null = maskesiz), çizim sırası ve
/// üst katmanlar.
/// </summary>
public sealed record EffectFrame(
    EffectLayer From,
    EffectLayer To,
    IReadOnlyList<IReadOnlyList<EffectPoint>>? Mask,
    bool ToOnTop,
    IReadOnlyList<EffectOverlay> Overlays);

/// <summary>Efektin arayüz bilgisi.</summary>
public sealed record EffectInfo(TransitionKind Kind, string Name, string Group, string Hint, bool UsesOrigin);

/// <summary>
/// Pro geçiş efektleri (Karar 23): her efekt, ilerleme (0–1), kare oranı, isteğe bağlı başlangıç noktası (tıklanan yer) ve
/// tohumdan saf olarak bir <see cref="EffectFrame"/> hesaplar. Video (SkiaSharp), sunum (WPF) ve HTML prova (JavaScript) bu
/// hesabı aynen uygular; HTML'deki karşılığı <c>Assets/Effects/effects.js</c> dosyasındadır ve testle eşitliği denetlenir.
/// Rastgelelik 32 bit "mulberry32" ile (JavaScript'te birebir aynı) üretilir.
/// </summary>
public static class TransitionEffects
{
    private const int CircleSegments = 72;

    private static readonly RgbaColor White = RgbaColor.White;
    private static readonly RgbaColor Black = RgbaColor.Black;
    private static readonly RgbaColor Amber = new(255, 170, 90);
    private static readonly RgbaColor Rose = new(255, 120, 140);
    private static readonly RgbaColor Ember = new(255, 140, 40);

    /// <summary>Tüm efektler, arayüzde gösterilecek sırayla.</summary>
    public static IReadOnlyList<EffectInfo> All { get; } =
    [
        new(TransitionKind.Aperture, "Aperture", "Diyafram", "Tıklanan noktadan iris açılır, kenarda ince halka.", true),
        new(TransitionKind.Diaphragm, "Diyafram kanatları", "Diyafram", "Dönerek açılan altıgen objektif diyaframı.", false),
        new(TransitionKind.DoubleIris, "Çift iris", "Diyafram", "İki odaktan açılan daireler birleşir.", false),
        new(TransitionKind.DiamondReveal, "Elmas açılış", "Diyafram", "Ortadan büyüyen elmas.", false),
        new(TransitionKind.Iris, "Dairesel iris", "Diyafram", "Ortadan açılan daire.", false),
        new(TransitionKind.RackFocus, "Netlik kaydırma", "Odak", "Eski kare bulanıklaşır, yenisi netleşerek gelir.", false),
        new(TransitionKind.FocusShift, "Odak kaydırma", "Odak", "Yumuşak bulanık geçiş.", false),
        new(TransitionKind.TiltShift, "Tilt-shift", "Odak", "Ortadaki net bant genişleyerek açılır.", false),
        new(TransitionKind.DepthFocus, "Derinlikten", "Odak", "Yeni kare uzaktan, bulanık yaklaşır.", false),
        new(TransitionKind.LightLeak, "Işık sızıntısı", "Işık ve film", "Sıcak turuncu-pembe parlama geçer.", false),
        new(TransitionKind.FilmBurn, "Film yanığı", "Işık ve film", "Bir noktadan yanarak açılan sıcak leke.", true),
        new(TransitionKind.Flash, "Flaş", "Işık ve film", "Beyaz patlama, ardından pozlama oturur.", false),
        new(TransitionKind.Projector, "Projektör", "Işık ve film", "Kısa titreme ve atlama ile kare değişir.", false),
        new(TransitionKind.PolaroidDevelop, "Polaroid banyosu", "Işık ve film", "Fotoğraf beyazdan renge yavaşça belirir.", false),
        new(TransitionKind.Crossfade, "Çapraz geçiş", "Klasik", "Yumuşak karışım.", false),
        new(TransitionKind.DipToBlack, "Siyaha geçiş", "Klasik", "Siyaha kararıp açılır.", false),
        new(TransitionKind.DipToWhite, "Beyaza geçiş", "Klasik", "Beyaza açılıp yeni kare gelir.", false),
        new(TransitionKind.Cut, "Kesme", "Klasik", "Doğrudan geçiş.", false),
        new(TransitionKind.Curtain, "Perde", "Maske", "Ortadan iki yana açılır.", false),
        new(TransitionKind.Split, "Yarık", "Maske", "Yatay yarıktan yeni kare yaklaşarak çıkar.", false),
        new(TransitionKind.Blinds, "Panjur", "Maske", "Yatay şeritler sırayla açılır.", false),
        new(TransitionKind.DiagonalWipe, "Çapraz silme", "Maske", "Eğik çizgiyle silinir.", false),
        new(TransitionKind.Mosaic, "Mozaik", "Maske", "Kareler sırayla belirir.", false),
        new(TransitionKind.Honeycomb, "Petek", "Maske", "Altıgen hücrelerle açılır.", false),
        new(TransitionKind.InkBlot, "Mürekkep lekesi", "Maske", "Organik lekeler yayılarak açar.", true),
        new(TransitionKind.Parallax, "Paralaks", "Hareket", "Katmanlar farklı hızda kayar.", false),
        new(TransitionKind.WhipPan, "Hızlı kaydırma", "Hareket", "Kamera hızla döner; hareket bulanıklığı.", false),
        new(TransitionKind.PushLeft, "Sola itme", "Hareket", "Yeni kare sağdan iter.", false),
        new(TransitionKind.PushRight, "Sağa itme", "Hareket", "Yeni kare soldan iter.", false),
        new(TransitionKind.PushUp, "Yukarı itme", "Hareket", "Yeni kare alttan iter.", false),
        new(TransitionKind.ZoomThrough, "Yakınlaşarak", "Hareket", "İçinden geçer gibi yakınlaşır.", false),
        new(TransitionKind.CardStack, "Kart yığını", "Albüm", "Yeni fotoğraf hafif dönerek üste düşer.", false),
        new(TransitionKind.PageTurn, "Sayfa çevirme", "Albüm", "Sayfa sırt çevresinde döner.", false),
    ];

    public static EffectInfo Info(TransitionKind kind) => All.FirstOrDefault(e => e.Kind == kind) ?? All[0];

    /// <summary>Önerilen geçiş süresi çarpanı (efektin tadı için; 1 = stilin süresi).</summary>
    public static double DurationFactor(TransitionKind kind) => kind switch
    {
        TransitionKind.Flash or TransitionKind.WhipPan or TransitionKind.Cut => 0.6,
        TransitionKind.Projector => 0.8,
        TransitionKind.PolaroidDevelop or TransitionKind.LightLeak or TransitionKind.PageTurn => 1.4,
        TransitionKind.Aperture or TransitionKind.Diaphragm or TransitionKind.InkBlot or TransitionKind.Honeycomb or TransitionKind.Mosaic => 1.2,
        _ => 1,
    };

    /// <summary>
    /// Geçişin <paramref name="p"/> anındaki hâli.
    /// </summary>
    /// <param name="kind">Efekt.</param>
    /// <param name="p">İlerleme 0–1 (doğrusal; yumuşatma efektin içinde).</param>
    /// <param name="aspect">Kare en/boy oranı.</param>
    /// <param name="origin">Başlangıç noktası (efekt alanında); null ise efekte göre orta ya da tohumdan.</param>
    /// <param name="seed">Tohum (aynı tohum → aynı desen).</param>
    public static EffectFrame Compute(TransitionKind kind, double p, double aspect, EffectPoint? origin = null, uint seed = 1)
    {
        if (!(aspect > 0) || !double.IsFinite(aspect))
        {
            throw new ArgumentOutOfRangeException(nameof(aspect));
        }

        p = Math.Clamp(double.IsFinite(p) ? p : 0, 0, 1);
        var a = aspect;
        var cx = a / 2;
        var still = Layer(a);
        var r = new Mulberry32(seed);
        var o = origin ?? (Info(kind).UsesOrigin ? new EffectPoint(a * (0.25 + (0.5 * r.Next())), 0.3 + (0.4 * r.Next())) : new EffectPoint(cx, 0.5));
        var cover = CoverRadius(o.X, o.Y, a);
        var e = Smooth(p);

        switch (kind)
        {
            case TransitionKind.Cut:
                return Frame(still with { Opacity = p < 0.5 ? 1 : 0 }, still with { Opacity = p < 0.5 ? 0 : 1 });

            case TransitionKind.DipToBlack:
            case TransitionKind.DipToWhite:
                return Frame(
                    still with { Opacity = p < 0.5 ? 1 : 0 },
                    still with { Opacity = p < 0.5 ? 0 : 1 },
                    overlays: [new SolidOverlay(kind == TransitionKind.DipToBlack ? Black : White, 1 - Math.Abs((p * 2) - 1))]);

            case TransitionKind.FocusShift:
                return Frame(still with { Blur = 0.03 * e }, still with { Opacity = e, Blur = 0.03 * (1 - e) });

            case TransitionKind.Iris:
                return Frame(still, still, [Circle(cx, 0.5, e * CoverRadius(cx, 0.5, a))]);

            case TransitionKind.PushLeft:
            case TransitionKind.PushRight:
            case TransitionKind.PushUp:
                {
                    var (dx, dy) = kind switch
                    {
                        TransitionKind.PushLeft => (-a, 0.0),
                        TransitionKind.PushRight => (a, 0.0),
                        _ => (0.0, -1.0),
                    };
                    return Frame(still with { OffsetX = dx * e, OffsetY = dy * e }, still with { OffsetX = -dx * (1 - e), OffsetY = -dy * (1 - e) });
                }

            case TransitionKind.ZoomThrough:
                return Frame(
                    still with { Scale = 1 + (e * 0.5), Blur = 0.02 * e },
                    still with { Scale = 0.85 + (e * 0.15), Opacity = e, Blur = 0.02 * (1 - e) });

            case TransitionKind.Aperture:
                {
                    var k = IrisEase(p);
                    var radius = k * cover;
                    var ringAlpha = p < 0.7 ? 1 : 1 - ((p - 0.7) / 0.3);
                    return Frame(
                        still,
                        still with { Scale = 1.18 - (0.18 * OutCubic(Math.Min(1, p / 0.8))), PivotX = o.X, PivotY = o.Y },
                        [Circle(o.X, o.Y, radius)],
                        overlays: [new RingOverlay(o.X, o.Y, radius, 0.0022, ringAlpha * (radius > 0.002 ? 1 : 0), White)]);
                }

            case TransitionKind.Diaphragm:
                {
                    var k = IrisEase(p);
                    var radius = k * cover * 1.16;
                    var turn = (1 - k) * 60;
                    return Frame(
                        still with { Brightness = 1 - (0.3 * k) },
                        still with { Scale = 1.08 - (0.08 * k) },
                        [RegularPolygon(cx, 0.5, radius, 6, turn)],
                        overlays: [new RingOverlay(cx, 0.5, radius * 0.87, 0.0016, (1 - k) * 0.8, White)]);
                }

            case TransitionKind.DoubleIris:
                {
                    var k = IrisEase(p);
                    var c1 = new EffectPoint(a * 0.3, 0.4);
                    var c2 = new EffectPoint(a * 0.7, 0.6);
                    var radius = k * Math.Max(CoverRadius(c1.X, c1.Y, a), CoverRadius(c2.X, c2.Y, a)) * 0.9;
                    return Frame(still, still, [Circle(c1.X, c1.Y, radius), Circle(c2.X, c2.Y, radius)]);
                }

            case TransitionKind.DiamondReveal:
                {
                    var d = IrisEase(p) * (cx + 0.5) * 1.02;
                    return Frame(still, still, [[new(cx - d, 0.5), new(cx, 0.5 - d), new(cx + d, 0.5), new(cx, 0.5 + d)]]);
                }

            case TransitionKind.RackFocus:
                return Frame(
                    still with { Blur = 0.05 * e, Scale = 1 + (0.04 * e) },
                    still with { Opacity = SmoothStep(0.3, 0.8, p), Blur = 0.05 * (1 - e), Scale = 1.04 - (0.04 * e) });

            case TransitionKind.TiltShift:
                {
                    var half = 0.5 * e;
                    return Frame(
                        still with { Blur = 0.03 * e },
                        still with { Blur = 0.02 * (1 - e) },
                        [Rect(-1, 0.5 - half, a + 2, half * 2)]);
                }

            case TransitionKind.DepthFocus:
                return Frame(
                    still with { Scale = 1 + (0.15 * e), Blur = 0.03 * e },
                    still with { Scale = 0.6 + (0.4 * OutCubic(p)), Blur = 0.06 * (1 - e), Opacity = SmoothStep(0, 0.6, p), Shadow = 0.6 * (1 - e) });

            case TransitionKind.LightLeak:
                {
                    var bell = Math.Sin(Math.PI * p);
                    return Frame(
                        still with { Brightness = 1 + (0.25 * bell) },
                        still with { Opacity = SmoothStep(0.35, 0.75, p), Brightness = 1 + (0.25 * bell) },
                        overlays:
                        [
                            new GlowOverlay((-0.3 * a) + (1.6 * a * p), 0.3, 0.9, Amber, bell * 0.85),
                            new GlowOverlay((1.2 * a) - (1.4 * a * p), 0.8, 0.7, Rose, bell * 0.6),
                        ]);
                }

            case TransitionKind.FilmBurn:
                {
                    var radius = e * cover * 1.25;
                    return Frame(
                        still with { Brightness = 1 + (0.6 * e) },
                        still,
                        [Blob(o.X, o.Y, radius, 0.18, r)],
                        overlays: [new GlowOverlay(o.X, o.Y, (radius * 1.3) + 0.1, Ember, Math.Sin(Math.PI * p) * 0.9)]);
                }

            case TransitionKind.Flash:
                {
                    var flash = p < 0.5 ? Math.Pow(p / 0.5, 3) : 1 - Math.Pow((p - 0.5) / 0.5, 0.6);
                    return Frame(
                        still with { Opacity = p < 0.5 ? 1 : 0 },
                        still with { Opacity = p < 0.5 ? 0 : 1, Brightness = 1 + (0.5 * (1 - e)) },
                        overlays: [new SolidOverlay(White, flash)]);
                }

            case TransitionKind.Projector:
                {
                    var step = (int)Math.Floor(p * 12);
                    var showNew = p >= 0.6 || (p >= 0.25 && step % 2 == 1);
                    var jitter = 0.012 * Math.Sin(p * 50) * (1 - e);
                    var flicker = p is > 0.15 and < 0.75 ? 0.25 * (step % 2) : 0;
                    return Frame(
                        still with { Opacity = showNew ? 0 : 1, OffsetY = jitter, Brightness = 1 - flicker },
                        still with { Opacity = showNew ? 1 : 0, OffsetY = -jitter, Brightness = 1 - flicker });
                }

            case TransitionKind.PolaroidDevelop:
                return Frame(
                    still,
                    still with
                    {
                        Opacity = SmoothStep(0, 0.3, p),
                        Saturation = e * e,
                        Brightness = 1 + (1.2 * (1 - e)),
                        Scale = 0.96 + (0.04 * e),
                        Shadow = 0.4 * (1 - e),
                    });

            case TransitionKind.Crossfade:
                return Frame(still, still with { Opacity = e });

            case TransitionKind.Curtain:
                {
                    var half = InOutCubic(p) * cx;
                    return Frame(
                        still with { Brightness = 1 - (0.35 * e) },
                        still,
                        [Rect(cx - half, -1, half * 2, 3)],
                        overlays: half > 0.001 ? new EffectOverlay[] { new BandOverlay(cx - half - 0.04, cx - half, 0, 0.45 * (1 - e), Black), new BandOverlay(cx + half, cx + half + 0.04, 0.45 * (1 - e), 0, Black) } : []);
                }

            case TransitionKind.Split:
                {
                    var half = InOutCubic(p) * 0.5;
                    return Frame(
                        still with { Brightness = 1 - (0.4 * e) },
                        still with { Scale = 1.15 - (0.15 * e) },
                        [Rect(-1, 0.5 - half, a + 2, half * 2)]);
                }

            case TransitionKind.Blinds:
                {
                    const int Strips = 8;
                    var rects = new List<IReadOnlyList<EffectPoint>>(Strips);
                    for (var i = 0; i < Strips; i++)
                    {
                        var local = InOutCubic(Math.Clamp((p - (i * 0.05)) / 0.65, 0, 1));
                        if (local > 0)
                        {
                            rects.Add(Rect(-1, (double)i / Strips, a + 2, local / Strips));
                        }
                    }

                    return Frame(still, still, rects);
                }

            case TransitionKind.DiagonalWipe:
                {
                    var c = -0.5 + (InOutCubic(p) * (a + 1));
                    return Frame(still, still, [[new(-1, -1), new(c + 0.5, -1), new(c - 0.5, 2), new(-1, 2)]]);
                }

            case TransitionKind.Mosaic:
                {
                    const int Rows = 6;
                    var cols = (int)Math.Ceiling(a * Rows);
                    var size = 1.0 / Rows;
                    var order = Shuffle(cols * Rows, r);
                    var cells = new List<IReadOnlyList<EffectPoint>>();
                    for (var i = 0; i < order.Length; i++)
                    {
                        var delay = (double)order[i] / order.Length * 0.6;
                        var local = OutCubic(Math.Clamp((p - delay) / 0.4, 0, 1));
                        if (local <= 0)
                        {
                            continue;
                        }

                        var col = i % cols;
                        var row = i / cols;
                        var s = size * local * 1.02;
                        cells.Add(Rect((col * size) + ((size - s) / 2), (row * size) + ((size - s) / 2), s, s));
                    }

                    return Frame(still, still, cells);
                }

            case TransitionKind.Honeycomb:
                {
                    const double R = 0.09;
                    var w = Math.Sqrt(3) * R;
                    var cells = new List<IReadOnlyList<EffectPoint>>();
                    var maxDistance = CoverRadius(cx, 0.5, a);
                    for (var row = -1; row * 1.5 * R < 1 + R; row++)
                    {
                        for (var col = -1; col * w < a + w; col++)
                        {
                            var hx = (col * w) + (row % 2 == 0 ? 0 : w / 2);
                            var hy = row * 1.5 * R;
                            var distance = Math.Sqrt(((hx - cx) * (hx - cx)) + ((hy - 0.5) * (hy - 0.5))) / maxDistance;
                            var local = OutCubic(Math.Clamp((p - (distance * 0.55)) / 0.45, 0, 1));
                            if (local > 0)
                            {
                                cells.Add(RegularPolygon(hx, hy, R * 1.04 * local, 6, 30));
                            }
                        }
                    }

                    return Frame(still, still, cells);
                }

            case TransitionKind.InkBlot:
                {
                    var blobs = new List<IReadOnlyList<EffectPoint>>();
                    var main = OutCubic(p) * cover * 1.4;
                    if (main > 0)
                    {
                        blobs.Add(Blob(o.X, o.Y, main, 0.25, r));
                    }

                    for (var i = 0; i < 3; i++)
                    {
                        var bx = a * (0.1 + (0.8 * r.Next()));
                        var by = 0.1 + (0.8 * r.Next());
                        var local = OutCubic(Math.Clamp((p - 0.1 - (i * 0.12)) / 0.6, 0, 1));
                        var radius = local * CoverRadius(bx, by, a) * 0.8;
                        if (radius > 0)
                        {
                            blobs.Add(Blob(bx, by, radius, 0.3, r));
                        }
                    }

                    return Frame(still, still, blobs);
                }

            case TransitionKind.Parallax:
                {
                    var k = InOutCubic(p);
                    var edge = a * (1 - k);
                    return Frame(
                        still with { OffsetX = -0.35 * a * k, Brightness = 1 - (0.5 * k) },
                        still with { OffsetX = edge },
                        overlays: edge > 0.001 && edge < a ? new EffectOverlay[] { new BandOverlay(edge - 0.06, edge, 0, 0.35, Black) } : []);
                }

            case TransitionKind.WhipPan:
                {
                    var k = InOutExpo(p);
                    var streak = 0.035 * Math.Sin(Math.PI * p);
                    return Frame(still with { OffsetX = -a * k, Blur = streak }, still with { OffsetX = a * (1 - k), Blur = streak });
                }

            case TransitionKind.CardStack:
                {
                    var k = OutBack(p);
                    return Frame(
                        still with { Brightness = 1 - (0.35 * e), Scale = 1 - (0.04 * e) },
                        still with { OffsetY = -1.1 * (1 - k), Rotation = -8 * (1 - k), Shadow = 0.7 * (1 - (0.6 * e)), Scale = 0.94 + (0.06 * e) });
                }

            case TransitionKind.PageTurn:
                {
                    var k = InOutCubic(p);
                    var fold = a * (1 - k);
                    return new EffectFrame(
                        still with { ScaleX = Math.Max(0.0001, 1 - k), PivotX = 0, Brightness = 1 - (0.45 * k) },
                        still with { Brightness = 0.8 + (0.2 * k) },
                        null,
                        false,
                        fold > 0.001 && fold < a ? new EffectOverlay[] { new BandOverlay(fold, Math.Min(a, fold + 0.12), 0.45 * (1 - k), 0, Black) } : []);
                }

            default:
                return Frame(still, still with { Opacity = e });
        }
    }

    /// <summary>(x, y) noktasından kareyi kaplayan yarıçap: en uzak köşeye uzaklık.</summary>
    public static double CoverRadius(double x, double y, double aspect) =>
        Math.Sqrt(Math.Pow(Math.Max(x, aspect - x), 2) + Math.Pow(Math.Max(y, 1 - y), 2));

    public static double Smooth(double p) => p * p * (3 - (2 * p));

    public static double SmoothStep(double a, double b, double p) => Smooth(Math.Clamp((p - a) / (b - a), 0, 1));

    public static double OutCubic(double p) => 1 - Math.Pow(1 - p, 3);

    public static double InOutCubic(double p) => p < 0.5 ? 4 * p * p * p : 1 - (Math.Pow((-2 * p) + 2, 3) / 2);

    /// <summary>İris açılışı: hızlı ortası, yumuşak başı ve sonu (cubic-bezier(.7,0,.2,1) yaklaşığı).</summary>
    public static double IrisEase(double p) => p < 0.5 ? 8 * Math.Pow(p, 4) : 1 - (Math.Pow((-2 * p) + 2, 4) / 2);

    public static double InOutExpo(double p) => p <= 0 ? 0 : p >= 1 ? 1 : p < 0.5 ? Math.Pow(2, (20 * p) - 10) / 2 : (2 - Math.Pow(2, (-20 * p) + 10)) / 2;

    /// <summary>Hafif sekmeli varış (kart yığını).</summary>
    public static double OutBack(double p)
    {
        const double C1 = 1.2;
        const double C3 = C1 + 1;
        return 1 + (C3 * Math.Pow(p - 1, 3)) + (C1 * Math.Pow(p - 1, 2));
    }

    private static EffectLayer Layer(double aspect) => new() { PivotX = aspect / 2, PivotY = 0.5 };

    private static EffectFrame Frame(EffectLayer from, EffectLayer to, IReadOnlyList<IReadOnlyList<EffectPoint>>? mask = null, IReadOnlyList<EffectOverlay>? overlays = null) =>
        new(from, to, mask, true, overlays ?? []);

    private static EffectPoint[] Circle(double cx, double cy, double radius)
    {
        var points = new EffectPoint[CircleSegments];
        for (var i = 0; i < CircleSegments; i++)
        {
            var t = 2 * Math.PI * i / CircleSegments;
            points[i] = new EffectPoint(cx + (radius * Math.Cos(t)), cy + (radius * Math.Sin(t)));
        }

        return points;
    }

    private static EffectPoint[] RegularPolygon(double cx, double cy, double radius, int sides, double rotationDegrees)
    {
        var points = new EffectPoint[sides];
        for (var i = 0; i < sides; i++)
        {
            var t = (2 * Math.PI * i / sides) + (rotationDegrees * Math.PI / 180);
            points[i] = new EffectPoint(cx + (radius * Math.Cos(t)), cy + (radius * Math.Sin(t)));
        }

        return points;
    }

    private static EffectPoint[] Rect(double x, double y, double w, double h) =>
        [new(x, y), new(x + w, y), new(x + w, y + h), new(x, y + h)];

    /// <summary>Kenarı dalgalı organik leke (tohumlu): yarıçap üç sinüsün toplamıyla oynar.</summary>
    private static EffectPoint[] Blob(double cx, double cy, double radius, double roughness, Mulberry32 random)
    {
        var f1 = 3 + Math.Floor(random.Next() * 3);
        var f2 = 7 + Math.Floor(random.Next() * 4);
        var f3 = 13 + Math.Floor(random.Next() * 5);
        var p1 = random.Next() * Math.PI * 2;
        var p2 = random.Next() * Math.PI * 2;
        var p3 = random.Next() * Math.PI * 2;
        var points = new EffectPoint[CircleSegments];
        for (var i = 0; i < CircleSegments; i++)
        {
            var t = 2 * Math.PI * i / CircleSegments;
            var wobble = (0.6 * Math.Sin((f1 * t) + p1)) + (0.3 * Math.Sin((f2 * t) + p2)) + (0.1 * Math.Sin((f3 * t) + p3));
            var rr = radius * (1 + (roughness * wobble));
            points[i] = new EffectPoint(cx + (rr * Math.Cos(t)), cy + (rr * Math.Sin(t)));
        }

        return points;
    }

    /// <summary>0..n-1'in tohumlu karışık sırası (Fisher–Yates); dizi[i] = i. hücrenin sırası.</summary>
    private static int[] Shuffle(int n, Mulberry32 random)
    {
        var order = new int[n];
        for (var i = 0; i < n; i++)
        {
            order[i] = i;
        }

        for (var i = n - 1; i > 0; i--)
        {
            var j = (int)Math.Floor(random.Next() * (i + 1));
            (order[i], order[j]) = (order[j], order[i]);
        }

        return order;
    }

    /// <summary>32 bit "mulberry32" üreteci; JavaScript karşılığıyla birebir aynı diziyi verir.</summary>
    private sealed class Mulberry32(uint seed)
    {
        private uint state = seed;

        public double Next()
        {
            unchecked
            {
                state += 0x6D2B79F5;
                var t = state;
                t = (t ^ (t >> 15)) * (t | 1);
                t ^= t + ((t ^ (t >> 7)) * (t | 61));
                return (t ^ (t >> 14)) / 4294967296.0;
            }
        }
    }
}
