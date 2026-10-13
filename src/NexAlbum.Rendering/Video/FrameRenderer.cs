using NexAlbum.Core.Effects;
using NexAlbum.Core.Video;
using NexAlbum.Rendering.Effects;
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
            DrawTransition(canvas, size, current, next, t, p);
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
        // Skia renk matrisinde kaydırma sütunu 0–1 ölçeğindedir (255'e bölünür).
        VideoLook.Warm => SKColorFilter.CreateColorMatrix(
        [
            1.06f, 0, 0, 0, 4 / 255f,
            0, 1.0f, 0, 0, 1 / 255f,
            0, 0, 0.9f, 0, -3 / 255f,
            0, 0, 0, 1, 0,
        ]),
        VideoLook.Cool => SKColorFilter.CreateColorMatrix(
        [
            0.93f, 0, 0, 0, -2 / 255f,
            0, 1.0f, 0, 0, 0,
            0, 0, 1.08f, 0, 6 / 255f,
            0, 0, 0, 1, 0,
        ]),
        VideoLook.BlackWhite => SKColorFilter.CreateColorMatrix(
        [
            0.30f * 1.1f, 0.59f * 1.1f, 0.11f * 1.1f, 0, -8 / 255f,
            0.30f * 1.1f, 0.59f * 1.1f, 0.11f * 1.1f, 0, -8 / 255f,
            0.30f * 1.1f, 0.59f * 1.1f, 0.11f * 1.1f, 0, -8 / 255f,
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

    /// <summary>
    /// Geçiş: efektin hâli Core'da hesaplanır (<see cref="TransitionEffects"/>), burada uygulanır. Başlangıç noktası kullanan
    /// efektlerde (Aperture, yanık, mürekkep) nokta öğenin başlangıç zamanından tohumlanır; aynı film → aynı geçiş.
    /// </summary>
    private void DrawTransition(SKCanvas canvas, SKSize size, VideoClip from, VideoClip to, double t, double p)
    {
        var seed = (uint)Math.Abs(Math.Round(to.Start * 1000)) + 1;
        var frame = TransitionEffects.Compute(to.TransitionIn, p, size.Width / size.Height, null, seed);
        EffectPainter.Paint(
            canvas,
            size.Width,
            size.Height,
            frame,
            c => DrawClip(c, size, from, t, 1, 0, SKMatrix.Identity),
            c => DrawClip(c, size, to, t, 1, 0, SKMatrix.Identity));
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
            case GalleryClip gallery:
                DrawGallery(canvas, size, gallery, u);
                break;
            case CollageClip collage:
                DrawCollage(canvas, size, collage, u);
                break;
            case BookClip book:
                DrawBook(canvas, size, book, u);
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
        DrawDarkGround(canvas, size);

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

        var seconds = u * clip.Duration;
        var lines = Wrap(clip.Title, titleFont, w * 0.84f);
        var lineHeight = titleFont.Size * 1.1f;
        var y = (h / 2) - ((lines.Count - 1) * lineHeight / 2) + (titleFont.Size * 0.25f);
        for (var i = 0; i < lines.Count; i++)
        {
            DrawAnimatedLine(canvas, lines[i], w / 2, y, titleFont, titlePaint, clip.Animation, seconds - (i * 0.12));
            y += lineHeight;
        }

        var ruleY = y - lineHeight + (titleFont.Size * 0.45f);
        var after = Math.Clamp((seconds - 0.55) / 0.6, 0, 1);
        var reveal = clip.Animation == TitleAnimation.Fade ? 1 : TransitionEffects.OutCubic(after);
        var half = unit * 0.06f * (float)reveal;
        canvas.DrawLine((w / 2) - half, ruleY, (w / 2) + half, ruleY, goldPaint);
        if (!string.IsNullOrWhiteSpace(clip.Subtitle))
        {
            using var subtitlePaint = new SKPaint { Color = Gold.WithAlpha((byte)Math.Round(255 * reveal)), IsAntialias = true };
            canvas.DrawText(Spaced(clip.Subtitle), w / 2, ruleY + (smallFont.Size * 2.2f) + (float)((1 - reveal) * smallFont.Size), SKTextAlign.Center, smallFont, subtitlePaint);
        }

        canvas.Restore();
    }

    /// <summary>Başlık satırının girişi (Karar 23): yükselme, el yazısı, bulanıktan netleşen kelimeler ya da doğrudan.</summary>
    private static void DrawAnimatedLine(SKCanvas canvas, string line, float centerX, float baseline, SKFont font, SKPaint paint, TitleAnimation animation, double seconds)
    {
        var width = font.MeasureText(line);
        var left = centerX - (width / 2);
        switch (animation)
        {
            case TitleAnimation.Rise:
                {
                    var r = (float)TransitionEffects.OutCubic(Math.Clamp((seconds - 0.15) / 0.9, 0, 1));
                    canvas.Save();
                    canvas.ClipRect(new SKRect(left - font.Size, baseline - (font.Size * 1.05f), left + width + font.Size, baseline + (font.Size * 0.4f)));
                    canvas.DrawText(line, centerX, baseline + ((1 - r) * font.Size * 1.45f), SKTextAlign.Center, font, paint);
                    canvas.Restore();
                    break;
                }

            case TitleAnimation.Handwriting:
                {
                    var r = (float)TransitionEffects.InOutCubic(Math.Clamp((seconds - 0.2) / 1.6, 0, 1));
                    if (r <= 0)
                    {
                        break;
                    }

                    // Soldan sağa açılan maske; ucu yumuşak (kalem izi gibi).
                    var edge = left - (font.Size * 0.3f) + ((width + (font.Size * 0.6f)) * r);
                    canvas.SaveLayer();
                    canvas.DrawText(line, centerX, baseline, SKTextAlign.Center, font, paint);
                    using (var fade = SKShader.CreateLinearGradient(new SKPoint(edge - (font.Size * 0.3f), 0), new SKPoint(edge, 0), [SKColors.Black, SKColors.Transparent], SKShaderTileMode.Clamp))
                    using (var mask = new SKPaint { Shader = fade, BlendMode = SKBlendMode.DstIn })
                    {
                        canvas.DrawRect(left - font.Size, baseline - (font.Size * 1.5f), width + (font.Size * 2), font.Size * 2.2f, mask);
                    }

                    canvas.Restore();
                    break;
                }

            case TitleAnimation.BlurWords:
                {
                    var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var space = font.MeasureText(" ");
                    var x = left;
                    for (var j = 0; j < words.Length; j++)
                    {
                        var local = Math.Clamp((seconds - 0.2 - (j * 0.18)) / 0.6, 0, 1);
                        var wordWidth = font.MeasureText(words[j]);
                        if (local > 0)
                        {
                            var sigma = (float)((1 - local) * font.Size * 0.22);
                            using var blur = sigma > 0.3f ? SKImageFilter.CreateBlur(sigma, sigma) : null;
                            using var layer = new SKPaint { Color = SKColors.White.WithAlpha((byte)Math.Round(255 * Ease(local))), ImageFilter = blur };
                            canvas.SaveLayer(layer);
                            canvas.DrawText(words[j], x, baseline, SKTextAlign.Left, font, paint);
                            canvas.Restore();
                        }

                        x += wordWidth + space;
                    }

                    break;
                }

            default:
                canvas.DrawText(line, centerX, baseline, SKTextAlign.Center, font, paint);
                break;
        }
    }

    private static void DrawDarkGround(SKCanvas canvas, SKSize size)
    {
        var w = size.Width;
        var h = size.Height;
        using var glow = SKShader.CreateRadialGradient(new SKPoint(w / 2, h * 0.45f), Math.Max(w, h) * 0.75f, [new SKColor(0x2A, 0x25, 0x1F), Ink], SKShaderTileMode.Clamp);
        using var background = new SKPaint { Shader = glow };
        canvas.DrawRect(0, 0, w, h, background);
    }

    /// <summary>Odak galerisi (Karar 23): şerit kayar; ortadaki kart net, yanlar küçülür, bulanıklaşır, soluklaşır.</summary>
    private void DrawGallery(SKCanvas canvas, SKSize size, GalleryClip clip, double u)
    {
        var w = size.Width;
        var h = size.Height;
        using (var ground = new SKPaint { Color = new SKColor(0x11, 0x11, 0x10) })
        {
            canvas.DrawRect(0, 0, w, h, ground);
        }

        var n = clip.AssetIds.Count;
        var cardW = w * (w >= h ? 0.62f : 0.8f);
        var cardH = cardW / 1.5f;
        if (cardH > h * 0.62f)
        {
            cardH = h * 0.62f;
            cardW = cardH * 1.5f;
        }

        var stride = cardW * 1.06f;
        var position = FocusPull.Position(u, n);
        var centerY = h * 0.44f;
        var unit = Math.Min(w, h);
        var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);

        // Uzaktakiler önce: ortadaki en üstte.
        foreach (var i in Enumerable.Range(0, n).OrderByDescending(k => Math.Abs(k - position)))
        {
            var d = i - position;
            var cx = (w / 2) + (float)(d * stride);
            if (cx + cardW < 0 || cx - cardW > w)
            {
                continue;
            }

            var focus = FocusPull.Of(d, 0.009);
            var image = photos.Get(clip.AssetIds[i], (int)Math.Ceiling(cardW * 1.2));
            var rect = SKRect.Create(cx - (cardW / 2), centerY - (cardH / 2), cardW, cardH);
            canvas.Save();
            canvas.Scale((float)focus.Scale, (float)focus.Scale, cx, centerY);
            using var colorFilter = EffectPainter.ColorFilter(focus.Saturation, 1);
            var sigma = (float)(focus.Blur * unit);
            using var blur = sigma > 0.3f ? SKImageFilter.CreateBlur(sigma, sigma) : null;
            using var layer = new SKPaint { Color = SKColors.White.WithAlpha((byte)Math.Round(255 * focus.Opacity)), ColorFilter = colorFilter, ImageFilter = blur };
            canvas.SaveLayer(layer);
            if (image is null)
            {
                using var empty = new SKPaint { Color = new SKColor(0x2A, 0x28, 0x24) };
                canvas.DrawRect(rect, empty);
            }
            else
            {
                canvas.ClipRoundRect(new SKRoundRect(rect, unit * 0.004f), antialias: true);
                canvas.DrawImage(image, Crop(image.Width, image.Height, rect.Width / rect.Height), rect, sampling);
            }

            canvas.Restore();
            canvas.Restore();
        }

        // Alt bilgi: bölüm adı, sayaç, ilerleme çizgisi.
        var margin = w * 0.06f;
        var metaY = centerY + (cardH / 2) + (unit * 0.08f);
        var nearest = FocusPull.Nearest(position, n);
        using var serif = new SKFont(typefaces?.Invoke("Cormorant Garamond") ?? SKTypeface.Default, unit * 0.045f);
        using var mono = new SKFont(typefaces?.Invoke("Montserrat") ?? SKTypeface.Default, unit * 0.022f);
        using var ink = new SKPaint { Color = Paper, IsAntialias = true };
        using var muted = new SKPaint { Color = Paper.WithAlpha(140), IsAntialias = true };
        if (!string.IsNullOrWhiteSpace(clip.Caption))
        {
            canvas.DrawText(clip.Caption, margin, metaY, SKTextAlign.Left, serif, ink);
        }

        var counter = (nearest + 1).ToString("00", System.Globalization.CultureInfo.InvariantCulture) + " / " + n.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
        canvas.DrawText(counter, w - margin, metaY, SKTextAlign.Right, mono, muted);
        var barY = metaY + (unit * 0.04f);
        using var line = new SKPaint { Color = Paper.WithAlpha(46), StrokeWidth = Math.Max(1, unit * 0.0015f) };
        canvas.DrawLine(margin, barY, w - margin, barY, line);
        line.Color = Paper;
        var progress = n > 1 ? (float)(position / (n - 1)) : 1;
        canvas.DrawLine(margin, barY, margin + ((w - (2 * margin)) * progress), barY, line);
    }

    /// <summary>Kolaj akışı (Karar 23): albüm sayfası zemininde hücreler sırayla, hafif büyüyerek yerleşir.</summary>
    private void DrawCollage(SKCanvas canvas, SKSize size, CollageClip clip, double u)
    {
        var w = size.Width;
        var h = size.Height;
        using (var paper = new SKPaint { Color = Paper })
        {
            canvas.DrawRect(0, 0, w, h, paper);
        }

        var unit = Math.Min(w, h);
        var zoom = (float)(1 + (0.04 * Ease(u)));
        canvas.Save();
        canvas.Scale(zoom, zoom, w / 2, h / 2);
        var cells = CollageCells(clip.AssetIds.Count, w, h, unit * 0.06f, unit * 0.022f);
        var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
        for (var i = 0; i < cells.Count; i++)
        {
            var local = TransitionEffects.OutCubic(Math.Clamp((u - (i * 0.14)) / 0.24, 0, 1));
            if (local <= 0)
            {
                continue;
            }

            var rect = cells[i];
            var s = (float)(0.9 + (0.1 * local));
            canvas.Save();
            canvas.Scale(s, s, rect.MidX, rect.MidY);
            using (var shadowBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, unit * 0.012f))
            using (var shadow = new SKPaint { Color = SKColors.Black.WithAlpha((byte)Math.Round(60 * local)), MaskFilter = shadowBlur })
            {
                canvas.DrawRect(rect.Left, rect.Top + (unit * 0.008f), rect.Width, rect.Height, shadow);
            }

            using var layer = new SKPaint { Color = SKColors.White.WithAlpha((byte)Math.Round(255 * local)) };
            canvas.SaveLayer(layer);
            var image = photos.Get(clip.AssetIds[i], (int)Math.Ceiling(Math.Max(rect.Width, rect.Height) * 1.2));
            if (image is null)
            {
                using var empty = new SKPaint { Color = new SKColor(0xD8, 0xD0, 0xC2) };
                canvas.DrawRect(rect, empty);
            }
            else
            {
                canvas.DrawImage(image, Crop(image.Width, image.Height, rect.Width / rect.Height), rect, sampling);
            }

            canvas.Restore();
            canvas.Restore();
        }

        canvas.Restore();
    }

    /// <summary>Kolaj hücreleri: 4 → 2×2; 3 → yatayda solda büyük + sağda iki, dikeyde üstte büyük + altta iki.</summary>
    private static List<SKRect> CollageCells(int count, float w, float h, float margin, float gap)
    {
        var area = SKRect.Create(margin, margin, w - (2 * margin), h - (2 * margin));
        if (count >= 4)
        {
            var cw = (area.Width - gap) / 2;
            var ch = (area.Height - gap) / 2;
            return
            [
                SKRect.Create(area.Left, area.Top, cw, ch),
                SKRect.Create(area.Left + cw + gap, area.Top, cw, ch),
                SKRect.Create(area.Left, area.Top + ch + gap, cw, ch),
                SKRect.Create(area.Left + cw + gap, area.Top + ch + gap, cw, ch),
            ];
        }

        if (w >= h)
        {
            var big = (area.Width - gap) * 0.6f;
            var rest = area.Width - gap - big;
            var half = (area.Height - gap) / 2;
            return
            [
                SKRect.Create(area.Left, area.Top, big, area.Height),
                SKRect.Create(area.Left + big + gap, area.Top, rest, half),
                SKRect.Create(area.Left + big + gap, area.Top + half + gap, rest, half),
            ];
        }

        var top = (area.Height - gap) * 0.58f;
        var bottom = area.Height - gap - top;
        var halfW = (area.Width - gap) / 2;
        return
        [
            SKRect.Create(area.Left, area.Top, area.Width, top),
            SKRect.Create(area.Left, area.Top + top + gap, halfW, bottom),
            SKRect.Create(area.Left + halfW + gap, area.Top + top + gap, halfW, bottom),
        ];
    }

    /// <summary>
    /// Albüm açılışı (Karar 23): kapalı albüm (deri kapak, altın yazı) → kapak sırt çevresinde açılır → çift sayfa kareyi
    /// doldurana kadar yaklaşır.
    /// </summary>
    private void DrawBook(SKCanvas canvas, SKSize size, BookClip clip, double u)
    {
        var w = size.Width;
        var h = size.Height;
        DrawDarkGround(canvas, size);
        var page = Math.Min(h * 0.6f, w * 0.44f);
        var cx = w / 2;
        var cy = h / 2;
        var open = (float)TransitionEffects.InOutCubic(Math.Clamp((u - 0.28) / 0.4, 0, 1));
        var grow = TransitionEffects.InOutCubic(Math.Clamp((u - 0.68) / 0.32, 0, 1));
        var target = Math.Max(w / (2 * page), h / page) * 1.02f;
        var zoom = (float)(1 + ((target - 1) * grow));
        var spine = cx - (page / 2) + (page / 2 * open);

        canvas.Save();
        canvas.Scale(zoom, zoom, cx, cy);
        var top = cy - (page / 2);

        // Sağ sayfa (ilk fotoğraf) kapağın altında.
        DrawBookPage(canvas, SKRect.Create(spine, top, page, page), clip.RightAssetId, page);

        if (open < 0.5f)
        {
            // Kapak: sırt çevresinde kapanır gibi daralır, açıldıkça kararır.
            var k = 1 - (open * 2);
            canvas.Save();
            canvas.Scale(Math.Max(0.0001f, k), 1, spine, cy);
            DrawCover(canvas, SKRect.Create(spine, top, page, page), clip, page, 1 - (0.5f * (1 - k)));
            canvas.Restore();
        }
        else
        {
            // Kapağın içi: sol sayfa (ikinci fotoğraf) sırttan sola açılır.
            var k = (open - 0.5f) * 2;
            canvas.Save();
            canvas.Scale(Math.Max(0.0001f, k), 1, spine, cy);
            DrawBookPage(canvas, SKRect.Create(spine - page, top, page, page), clip.LeftAssetId, page);
            canvas.Restore();
        }

        // Sırt gölgesi.
        if (open > 0)
        {
            using var shade = SKShader.CreateLinearGradient(new SKPoint(spine - (page * 0.06f), 0), new SKPoint(spine + (page * 0.06f), 0), [SKColors.Transparent, SKColors.Black.WithAlpha(70), SKColors.Transparent], SKShaderTileMode.Clamp);
            using var paint = new SKPaint { Shader = shade };
            canvas.DrawRect(spine - (page * 0.06f), top, page * 0.12f, page, paint);
        }

        canvas.Restore();
    }

    private void DrawBookPage(SKCanvas canvas, SKRect rect, string? assetId, float page)
    {
        using (var paper = new SKPaint { Color = new SKColor(0xFB, 0xF8, 0xF2) })
        {
            canvas.DrawRect(rect, paper);
        }

        var inset = rect;
        inset.Inflate(-page * 0.07f, -page * 0.07f);
        var image = assetId is null ? null : photos.Get(assetId, (int)Math.Ceiling(page * 2));
        if (image is not null)
        {
            canvas.DrawImage(image, Crop(image.Width, image.Height, inset.Width / inset.Height), inset, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
        }
    }

    private void DrawCover(SKCanvas canvas, SKRect rect, BookClip clip, float page, float light)
    {
        using (var leather = SKShader.CreateLinearGradient(new SKPoint(rect.Left, rect.Top), new SKPoint(rect.Right, rect.Bottom), [new SKColor(0x4A, 0x3A, 0x2C), new SKColor(0x2B, 0x22, 0x1A)], SKShaderTileMode.Clamp))
        using (var cover = new SKPaint { Shader = leather })
        {
            canvas.DrawRect(rect, cover);
        }

        using var gold = new SKPaint { Color = Gold, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = Math.Max(1, page * 0.004f) };
        var frame = rect;
        frame.Inflate(-page * 0.06f, -page * 0.06f);
        canvas.DrawRect(frame, gold);
        using var titleFont = new SKFont(typefaces?.Invoke("Great Vibes") ?? SKTypeface.Default, page * 0.12f);
        using var smallFont = new SKFont(typefaces?.Invoke("Montserrat") ?? SKTypeface.Default, page * 0.035f);
        using var fill = new SKPaint { Color = Gold, IsAntialias = true };
        var lines = Wrap(clip.Title, titleFont, frame.Width * 0.86f);
        var y = rect.MidY - ((lines.Count - 1) * titleFont.Size * 0.55f);
        foreach (var line in lines)
        {
            canvas.DrawText(line, rect.MidX, y, SKTextAlign.Center, titleFont, fill);
            y += titleFont.Size * 1.1f;
        }

        if (!string.IsNullOrWhiteSpace(clip.Subtitle))
        {
            canvas.DrawText(Spaced(clip.Subtitle), rect.MidX, y + (smallFont.Size * 0.8f), SKTextAlign.Center, smallFont, fill);
        }

        if (light < 1)
        {
            using var dark = new SKPaint { Color = SKColors.Black.WithAlpha((byte)Math.Round((1 - light) * 255)) };
            canvas.DrawRect(rect, dark);
        }
    }

    /// <summary>Görüntüden hedef orana kaplayan (ortadan) kırpma alanı.</summary>
    private static SKRect Crop(int width, int height, float aspect)
    {
        var w = (float)width;
        var h = (float)height;
        if (w / h > aspect)
        {
            var cw = h * aspect;
            return SKRect.Create((w - cw) / 2, 0, cw, h);
        }

        var ch = w / aspect;
        return SKRect.Create(0, (h - ch) / 2, w, ch);
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
