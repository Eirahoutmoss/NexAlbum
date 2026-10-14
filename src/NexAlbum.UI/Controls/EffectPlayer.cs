using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NexAlbum.Core.Effects;
using NexAlbum.Core.Video;
using NexAlbum.Rendering.Effects;
using SkiaSharp;

namespace NexAlbum.UI.Controls;

/// <summary>
/// Ekrandaki iki görüntü arasında pro geçiş efektini oynatır (Karar 23): her karede efekt Core'da hesaplanır, filmdeki gibi
/// SkiaSharp ile (<see cref="EffectPainter"/>) çizilir ve verilen <see cref="Image"/>'e aktarılır. Böylece sunumdaki efekt
/// filmdekiyle birebir aynıdır. Uzun kenar en fazla 1600 piksel çizilir (akıcılık); bitince çağırana haber verir.
/// </summary>
public sealed class EffectPlayer : IDisposable
{
    /// <summary>Çizimin uzun kenar sınırı (piksel).</summary>
    public const int MaxLongSide = 1600;

    private readonly Image target;
    private readonly Stopwatch clock = new();
    private SKImage? from;
    private SKImage? to;
    private WriteableBitmap? bitmap;
    private byte[] buffer = [];
    private TransitionKind kind;
    private EffectPoint? origin;
    private TimeSpan duration;
    private Action? done;
    private SKColor background;

    public EffectPlayer(Image target)
    {
        this.target = target ?? throw new ArgumentNullException(nameof(target));
    }

    public bool IsPlaying { get; private set; }

    /// <summary>Oynatmayı başlatır. <paramref name="originPx"/> başlangıç noktası (görüntü pikseli); null ise efekte göre.</summary>
    public void Play(BitmapSource fromImage, BitmapSource toImage, TransitionKind effect, Point? originPx, TimeSpan length, Color backdrop, Action? finished = null)
    {
        ArgumentNullException.ThrowIfNull(fromImage);
        ArgumentNullException.ThrowIfNull(toImage);
        Stop();
        var scale = Math.Min(1, (double)MaxLongSide / Math.Max(fromImage.PixelWidth, fromImage.PixelHeight));
        var width = Math.Max(2, (int)Math.Round(fromImage.PixelWidth * scale));
        var height = Math.Max(2, (int)Math.Round(fromImage.PixelHeight * scale));
        from = ToSkia(fromImage);
        to = ToSkia(toImage);
        bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
        buffer = new byte[width * height * 4];
        kind = effect;
        origin = originPx is { } o ? new EffectPoint(o.X / fromImage.PixelHeight, o.Y / fromImage.PixelHeight) : null;
        duration = length;
        done = finished;
        background = new SKColor(backdrop.R, backdrop.G, backdrop.B, backdrop.A);
        target.Source = bitmap;
        target.Visibility = Visibility.Visible;
        IsPlaying = true;
        RenderFrame(0);
        clock.Restart();
        CompositionTarget.Rendering += OnRendering;
    }

    /// <summary>Verilen ilerlemedeki kareyi çizer (testler ve ekran görüntüsü için de kullanılır).</summary>
    public void RenderFrame(double p)
    {
        if (bitmap is null || from is null || to is null)
        {
            return;
        }

        var w = bitmap.PixelWidth;
        var h = bitmap.PixelHeight;
        var info = new SKImageInfo(w, h, SKColorType.Bgra8888, SKAlphaType.Premul);
        var handle = System.Runtime.InteropServices.GCHandle.Alloc(buffer, System.Runtime.InteropServices.GCHandleType.Pinned);
        try
        {
            using var surface = SKSurface.Create(info, handle.AddrOfPinnedObject(), info.RowBytes);
            var canvas = surface.Canvas;
            canvas.Clear(background);
            var frame = TransitionEffects.Compute(kind, p, (double)w / h, origin, 7);
            var rect = new SKRect(0, 0, w, h);
            var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None);
            var source = from;
            var destination = to;
            EffectPainter.Paint(
                canvas,
                w,
                h,
                frame,
                c => c.DrawImage(source, new SKRect(0, 0, source.Width, source.Height), rect, sampling),
                c => c.DrawImage(destination, new SKRect(0, 0, destination.Width, destination.Height), rect, sampling));
            canvas.Flush();
        }
        finally
        {
            handle.Free();
        }

        bitmap.WritePixels(new Int32Rect(0, 0, w, h), buffer, w * 4, 0);
    }

    /// <summary>Oynatmayı verilen ilerlemede dondurur (görüntü kalır; ekran görüntüsü ve testler için).</summary>
    public void Hold(double p)
    {
        CompositionTarget.Rendering -= OnRendering;
        clock.Stop();
        RenderFrame(p);
    }

    /// <summary>Oynatmayı bitirir (görüntü gizlenir); bitiş işlevi çağrılmaz.</summary>
    public void Stop()
    {
        CompositionTarget.Rendering -= OnRendering;
        clock.Stop();
        IsPlaying = false;
        target.Visibility = Visibility.Collapsed;
        target.Source = null;
        from?.Dispose();
        to?.Dispose();
        from = null;
        to = null;
        bitmap = null;
        done = null;
    }

    public void Dispose() => Stop();

    /// <summary>WPF görüntüsünü Skia görüntüsüne kopyalar (BGRA, önceden çarpılmış alfa).</summary>
    public static SKImage ToSkia(BitmapSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var converted = source.Format == PixelFormats.Pbgra32 ? source : new FormatConvertedBitmap(source, PixelFormats.Pbgra32, null, 0);
        var stride = converted.PixelWidth * 4;
        var pixels = new byte[stride * converted.PixelHeight];
        converted.CopyPixels(pixels, stride, 0);
        var info = new SKImageInfo(converted.PixelWidth, converted.PixelHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
        return SKImage.FromPixelCopy(info, pixels, stride);
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        var p = duration <= TimeSpan.Zero ? 1 : clock.Elapsed.TotalMilliseconds / duration.TotalMilliseconds;
        if (p >= 1)
        {
            var finished = done;
            Stop();
            finished?.Invoke();
            return;
        }

        RenderFrame(p);
    }
}
