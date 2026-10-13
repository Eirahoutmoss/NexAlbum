using NexAlbum.Core.Video;
using SkiaSharp;

namespace NexAlbum.Rendering.Video;

/// <summary>
/// Hikâye filminin karelerini çizer (Karar 21): fotoğraf (Ken Burns kadrajı, gerekirse bulanık zemin), başlık kartı, geçişler,
/// görünüm (sıcak/soğuk/siyah-beyaz/sepya), vinyet, gren, başta ve sonda karartma. Aynı zaman çizelgesi ve zaman → aynı kare.
/// Ölçü serbesttir: önizleme küçük, dışa aktarım tam boyut çizer (her şey kare ölçüsüne oranlıdır).
/// </summary>
public sealed class FrameRenderer(VideoTimeline timeline, IPhotoSource photos, Func<string, SKTypeface?>? typefaces = null)
{
    /// <summary>Başta karadan açılma (sn).</summary>
    public const double FadeIn = 0.6;

    /// <summary>Sonda karaya kapanma (sn).</summary>
    public const double FadeOut = 1.2;

    private static readonly SKColor Ink = new(0x14, 0x13, 0x12);
    private static readonly SKColor Paper = new(0xF2, 0xED, 0xE4);
    private static readonly SKColor Gold = new(0xC9, 0xA8, 0x5E);

    public VideoTimeline Timeline { get; } = timeline;

    /// <summary>t anının karesini verilen tuvale (width×height) çizer.</summary>
    public void Render(SKCanvas canvas, int width, int height, double t)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        canvas.Clear(SKColors.Black);
        var (current, next, p) = Timeline.At(t);
        if (current is null)
        {
            return;
        }

        var size = new SKSize(width, height);
        if (next is null)
        {
            DrawClip(canvas, size, current, t, 1, 0, SKMatrix.Identity);
        }
        else
        {
            DrawTransition(canvas, size, current, next, t, Ease(p));
        }

        if (Timeline.Vignette)
        {
            DrawVignette(canvas, size);
        }

        if (Timeline.Grain)
        {
            DrawGrain(canvas, size, (int)Math.Round(t * Timeline.Fps));
        }

        // Baş ve son karartma.
        var fade = Math.Min(Math.Clamp(t / FadeIn, 0, 1), Math.Clamp((Timeline.Duration - t) / FadeOut, 0, 1));
        if (fade < 1)
        {
            using var black = new SKPaint { Color = SKColors.Black.WithAlpha((byte)Math.Round((1 - fade) * 255)) };
            canvas.DrawRect(0, 0, width, height, black);
        }
    }

    /// <summary>Kareyi yeni bir bitmap'e çizer (BGRA, önceden çarpılmış alfa).</summary>
    public SKBitmap RenderBitmap(int width, int height, double t)
    {
        var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(bitmap);
        Render(canvas, width, height, t);
        canvas.Flush();
        return bitmap;
    }

    /// <summary>Kareyi verilen BGRA tamponuna çizer (dışa aktarım; tampon genişlik × yükseklik × 4 bayt, sabitlenir).</summary>
    public void RenderInto(byte[] buffer, int width, int height, double t)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        if (buffer.Length < info.BytesSize)
        {
            throw new ArgumentException("Tampon kare için küçük.", nameof(buffer));
        }

        var handle = System.Runtime.InteropServices.GCHandle.Alloc(buffer, System.Runtime.InteropServices.GCHandleType.Pinned);
        try
        {
            using var surface = SKSurface.Create(info, handle.AddrOfPinnedObject(), info.RowBytes);
            Render(surface.Canvas, width, height, t);
            surface.Canvas.Flush();
        }
        finally
        {
            handle.Free();
        }
    }

    /// <summary>Görünüm renk matrisi (yalnızca video; baskıyı etkilemez).</summary>
    public static SKColorFilter? LookFilter(VideoLook look) => look switch
    {
        VideoLook.Warm => SKColorFilter.CreateColorMatrix(
        [
            1.08f, 0, 0, 0, 6,
            0, 1.0f, 0, 0, 2,
            0, 0, 0.88f, 0, -4,
            0, 0, 0, 1, 0,
        ]),
        VideoLook.Cool => SKColorFilter.CreateColorMatrix(
        [
            0.92f, 0, 0, 0, -2,
            0, 1.0f, 0, 0, 0,
            0, 0, 1.1f, 0, 8,
            0, 0, 0, 1, 0,
        ]),
        VideoLook.BlackWhite => SKColorFilter.CreateColorMatrix(
        [
            0.30f * 1.1f, 0.59f * 1.1f, 0.11f * 1.1f, 0, -8,
            0.30f * 1.1f, 0.59f * 1.1f, 0.11f * 1.1f, 0, -8,
            0.30f * 1.1f, 0.59f * 1.1f, 0.11f * 1.1f, 0, -8,
            0, 0, 0, 1, 0,
        ]),
        VideoLook.Sepia => SKColorFilter.CreateColorMatrix(
        [
            0.393f, 0.769f, 0.189f, 0, 0,
            0.349f, 0.686f, 0.168f, 0, 0,
            0.272f, 0.534f, 0.131f, 0, 0,
            0, 0, 0, 1, 0,
        ]),
        _ => null,
    };

    private static double Ease(double p) => p * p * (3 - (2 * p));

    private void DrawTransition(SKCanvas canvas, SKSize size, VideoClip from, VideoClip to, double t, double p)
    {
        var w = size.Width;
        var h = size.Height;
        switch (to.TransitionIn)
        {
            case TransitionKind.Cut:
                DrawClip(canvas, size, p < 0.5 ? from : to, t, 1, 0, SKMatrix.Identity);
                break;

            case TransitionKind.DipToBlack:
            case TransitionKind.DipToWhite:
                var color = to.TransitionIn == TransitionKind.DipToBlack ? SKColors.Black : SKColors.White;
                DrawClip(canvas, size, p < 0.5 ? from : to, t, 1, 0, SKMatrix.Identity);
                using (var dip = new SKPaint { Color = color.WithAlpha((byte)Math.Round((1 - Math.Abs((p * 2) - 1)) * 255)) })
                {
                    canvas.DrawRect(0, 0, w, h, dip);
                }

                break;

            case TransitionKind.FocusShift:
                var blur = (float)(Math.Min(w, h) * 0.03);
                DrawClip(canvas, size, from, t, 1, blur * p, SKMatrix.Identity);
                DrawClip(canvas, size, to, t, p, blur * (1 - p), SKMatrix.Identity);
                break;

            case TransitionKind.Iris:
                DrawClip(canvas, size, from, t, 1, 0, SKMatrix.Identity);
                canvas.Save();
                using (var circle = new SKPath())
                {
                    var radius = (float)(p * Math.Sqrt((w * w) + (h * h)) / 2);
                    circle.AddCircle(w / 2, h / 2, Math.Max(0.5f, radius));
                    canvas.ClipPath(circle, antialias: true);
                    DrawClip(canvas, size, to, t, 1, 0, SKMatrix.Identity);
                }

                canvas.Restore();
                break;

            case TransitionKind.PushLeft:
            case TransitionKind.PushRight:
            case TransitionKind.PushUp:
                var (dx, dy) = to.TransitionIn switch
                {
                    TransitionKind.PushLeft => (-w, 0f),
                    TransitionKind.PushRight => (w, 0f),
                    _ => (0f, -h),
                };
                var k = (float)p;
                DrawClip(canvas, size, from, t, 1, 0, SKMatrix.CreateTranslation(dx * k, dy * k));
                DrawClip(canvas, size, to, t, 1, 0, SKMatrix.CreateTranslation(-dx * (1 - k), -dy * (1 - k)));
                break;

            case TransitionKind.ZoomThrough:
                var outScale = (float)(1 + (p * 0.5));
                var inScale = (float)(0.85 + (p * 0.15));
                DrawClip(canvas, size, from, t, 1, (float)(Math.Min(w, h) * 0.02 * p), SKMatrix.CreateScale(outScale, outScale, w / 2, h / 2));
                DrawClip(canvas, size, to, t, p, (float)(Math.Min(w, h) * 0.02 * (1 - p)), SKMatrix.CreateScale(inScale, inScale, w / 2, h / 2));
                break;

            default:
                DrawClip(canvas, size, from, t, 1, 0, SKMatrix.Identity);
                DrawClip(canvas, size, to, t, p, 0, SKMatrix.Identity);
                break;
        }
    }

    /// <summary>Öğeyi verilen opaklık, bulanıklık ve ek dönüşümle çizer (katman yalnızca gerektiğinde açılır).</summary>
    private void DrawClip(SKCanvas canvas, SKSize size, VideoClip clip, double t, double alpha, double blur, SKMatrix transform)
    {
        if (alpha <= 0.001)
        {
            return;
        }

        var u = Math.Clamp((t - clip.Start) / Math.Max(1e-6, clip.Duration), 0, 1);
        using var colorFilter = LookFilter(clip.Look);
        using var blurFilter = blur > 0.3 ? SKImageFilter.CreateBlur((float)blur, (float)blur) : null;
        using var layerPaint = new SKPaint
        {
            Color = SKColors.White.WithAlpha((byte)Math.Round(Math.Clamp(alpha, 0, 1) * 255)),
            ColorFilter = colorFilter,
            ImageFilter = blurFilter,
        };
        var layered = alpha < 0.999 || colorFilter is not null || blurFilter is not null;
        canvas.Save();
        canvas.SetMatrix(canvas.TotalMatrix.PreConcat(transform));
        if (layered)
        {
            canvas.SaveLayer(layerPaint);
        }

        switch (clip)
        {
            case PhotoClip photo:
                DrawPhoto(canvas, size, photo, u);
                break;
            case TitleClip title:
                DrawTitle(canvas, size, title, u);
                break;
        }

        if (layered)
        {
            canvas.Restore();
        }

        canvas.Restore();
    }

    private void DrawPhoto(SKCanvas canvas, SKSize size, PhotoClip clip, double u)
    {
        var longSide = (int)Math.Ceiling(Math.Max(size.Width, size.Height) * 1.25);
        var image = photos.Get(clip.AssetId, longSide);
        if (image is null)
        {
            DrawMissing(canvas, size);
            return;
        }

        var window = Frame01.Lerp(clip.From, clip.To, Ease(u));
        var src = new SKRect(
            (float)(window.X * image.Width),
            (float)(window.Y * image.Height),
            (float)((window.X + window.Width) * image.Width),
            (float)((window.Y + window.Height) * image.Height));
        var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
        if (!clip.BlurredBackdrop)
        {
            canvas.DrawImage(image, src, new SKRect(0, 0, size.Width, size.Height), sampling);
            return;
        }

        // Bulanık, karartılmış zemin (fotoğrafın kendisi, kaplayacak kadar büyük) + üstte fotoğrafın tamamı.
        var cover = Fit(image.Width, image.Height, size, cover: true);
        var sigma = Math.Max(size.Width, size.Height) * 0.035f;
        using (var backdropBlur = SKImageFilter.CreateBlur(sigma, sigma))
        using (var darken = SKColorFilter.CreateColorMatrix([0.55f, 0, 0, 0, 0, 0, 0.55f, 0, 0, 0, 0, 0, 0.55f, 0, 0, 0, 0, 0, 1, 0]))
        using (var backdrop = new SKPaint { ImageFilter = backdropBlur, ColorFilter = darken })
        {
            // Karartma yalnızca rengi ölçekler (saydam alan saydam kalır); katman kare alanıyla sınırlı.
            canvas.SaveLayer(new SKRect(0, 0, size.Width, size.Height), backdrop);
            canvas.DrawImage(image, new SKRect(0, 0, image.Width, image.Height), cover, sampling);
            canvas.Restore();
        }

        var dest = Fit(src.Width, src.Height, size, cover: false);
        using var dropShadow = SKImageFilter.CreateDropShadow(0, size.Height * 0.01f, size.Height * 0.02f, size.Height * 0.02f, SKColors.Black.WithAlpha(140));
        using var shadow = new SKPaint { ImageFilter = dropShadow };
        canvas.SaveLayer(shadow);
        canvas.DrawImage(image, src, dest, sampling);
        canvas.Restore();
    }

    private void DrawTitle(SKCanvas canvas, SKSize size, TitleClip clip, double u)
    {
        var w = size.Width;
        var h = size.Height;
        using (var glow = SKShader.CreateRadialGradient(new SKPoint(w / 2, h * 0.45f), Math.Max(w, h) * 0.75f, [new SKColor(0x2A, 0x25, 0x1F), Ink], SKShaderTileMode.Clamp))
        using (var background = new SKPaint { Shader = glow })
        {
            canvas.DrawRect(0, 0, w, h, background);
        }

        // Yavaş yaklaşma: yazı 1,00 → 1,04.
        var zoom = (float)(1 + (0.04 * Ease(u)));
        canvas.Save();
        canvas.Scale(zoom, zoom, w / 2, h / 2);
        var unit = Math.Min(w, h);
        var title = typefaces?.Invoke("Great Vibes") ?? SKTypeface.Default;
        var small = typefaces?.Invoke("Montserrat") ?? SKTypeface.Default;
        using var titleFont = new SKFont(title, unit * 0.13f);
        using var smallFont = new SKFont(small, unit * 0.032f);
        using var titlePaint = new SKPaint { Color = Paper, IsAntialias = true };
        using var goldPaint = new SKPaint { Color = Gold, IsAntialias = true, StrokeWidth = Math.Max(1, unit * 0.002f) };

        var lines = Wrap(clip.Title, titleFont, w * 0.84f);
        var lineHeight = titleFont.Size * 1.1f;
        var y = (h / 2) - ((lines.Count - 1) * lineHeight / 2) + (titleFont.Size * 0.25f);
        foreach (var line in lines)
        {
            canvas.DrawText(line, w / 2, y, SKTextAlign.Center, titleFont, titlePaint);
            y += lineHeight;
        }

        var ruleY = y - lineHeight + (titleFont.Size * 0.45f);
        canvas.DrawLine((w / 2) - (unit * 0.06f), ruleY, (w / 2) + (unit * 0.06f), ruleY, goldPaint);
        if (!string.IsNullOrWhiteSpace(clip.Subtitle))
        {
            canvas.DrawText(Spaced(clip.Subtitle), w / 2, ruleY + (smallFont.Size * 2.2f), SKTextAlign.Center, smallFont, goldPaint);
        }

        canvas.Restore();
    }

    private static List<string> Wrap(string text, SKFont font, float width)
    {
        var lines = new List<string>();
        var current = string.Empty;
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = current.Length == 0 ? word : current + " " + word;
            if (current.Length > 0 && font.MeasureText(candidate) > width)
            {
                lines.Add(current);
                current = word;
            }
            else
            {
                current = candidate;
            }
        }

        if (current.Length > 0)
        {
            lines.Add(current);
        }

        return lines.Count == 0 ? [string.Empty] : lines;
    }

    /// <summary>Büyük harfli alt yazı için harf arası (ince boşluk).</summary>
    private static string Spaced(string text) => string.Join(' ', text.EnumerateRunes().Select(r => r.ToString()));

    private static SKRect Fit(float sourceWidth, float sourceHeight, SKSize size, bool cover)
    {
        var scale = cover
            ? Math.Max(size.Width / sourceWidth, size.Height / sourceHeight)
            : Math.Min(size.Width / sourceWidth, size.Height / sourceHeight) * 0.92f;
        var w = sourceWidth * scale;
        var h = sourceHeight * scale;
        return SKRect.Create((size.Width - w) / 2, (size.Height - h) / 2, w, h);
    }

    private static void DrawMissing(SKCanvas canvas, SKSize size)
    {
        using var paint = new SKPaint { Color = new SKColor(0x22, 0x20, 0x1E) };
        canvas.DrawRect(0, 0, size.Width, size.Height, paint);
    }

    private static void DrawVignette(SKCanvas canvas, SKSize size)
    {
        var radius = (float)Math.Sqrt((size.Width * size.Width) + (size.Height * size.Height)) / 2;
        using var shader = SKShader.CreateRadialGradient(
            new SKPoint(size.Width / 2, size.Height / 2),
            radius,
            [SKColors.Transparent, SKColors.Transparent, SKColors.Black.WithAlpha(150)],
            [0f, 0.55f, 1f],
            SKShaderTileMode.Clamp);
        using var paint = new SKPaint { Shader = shader };
        canvas.DrawRect(0, 0, size.Width, size.Height, paint);
    }

    private static void DrawGrain(SKCanvas canvas, SKSize size, int frame)
    {
        // Perlin gürültüsü tohumlu ve deterministiktir; her karede farklı tohum "kaynayan" film dokusu verir.
        var unit = Math.Min(size.Width, size.Height);
        using var noise = SKShader.CreatePerlinNoiseFractalNoise(0.75f * 1080 / unit, 0.75f * 1080 / unit, 2, frame % 997);
        using var paint = new SKPaint { Shader = noise, BlendMode = SKBlendMode.Overlay, Color = SKColors.White.WithAlpha(46) };
        canvas.DrawRect(0, 0, size.Width, size.Height, paint);
    }
}
