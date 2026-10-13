using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NexAlbum.Core.Design;
using NexAlbum.Core.Projects;

namespace NexAlbum.UI.Services;

/// <summary>
/// Ekran önizlemesi için fotoğraf ayarları: PDF'teki Skia filtresiyle aynı <see cref="ColorMatrix"/> piksellere uygulanır,
/// böylece tuvalde görülen baskıda da görülür. Sonuçlar kaynak görüntü başına küçük bir önbellekte tutulur (kaydırıcıyı geri
/// çekince yeniden hesaplanmaz).
/// </summary>
public static class AdjustedImages
{
    private const int PerSource = 4;

    private static readonly ConditionalWeakTable<ImageSource, List<(ImageAdjustments Key, BitmapSource Image)>> Cache = new();

    /// <summary>Ayarlı görüntü; ayar yoksa ya da görüntü bitmap değilse kaynak aynen döner.</summary>
    public static ImageSource? Get(ImageSource? source, ImageAdjustments? adjustments)
    {
        if (source is not BitmapSource bitmap || adjustments is null || adjustments.IsNone)
        {
            return source;
        }

        var list = Cache.GetOrCreateValue(source);
        lock (list)
        {
            var hit = list.FindIndex(e => e.Key == adjustments);
            if (hit >= 0)
            {
                var entry = list[hit];
                list.RemoveAt(hit);
                list.Insert(0, entry);
                return entry.Image;
            }
        }

        var image = Apply(bitmap, ColorMatrix.From(adjustments));
        lock (list)
        {
            list.Insert(0, (adjustments, image));
            if (list.Count > PerSource)
            {
                list.RemoveAt(list.Count - 1);
            }
        }

        return image;
    }

    /// <summary>Renk matrisini BGRA piksellerine uygular (dondurulmuş yeni bitmap).</summary>
    public static BitmapSource Apply(BitmapSource source, ColorMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(matrix);
        var bgra = source.Format == PixelFormats.Bgra32 ? source : new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        var width = bgra.PixelWidth;
        var height = bgra.PixelHeight;
        var stride = width * 4;
        var pixels = new byte[stride * height];
        bgra.CopyPixels(pixels, stride, 0);

        // 0–1 ölçeğindeki matris 0–255 bayt değerlerine uyarlanır; kaydırma 255 ile çarpılır.
        var m = matrix.ToFloats();
        float rr = m[0], rg = m[1], rb = m[2], ro = m[4] * 255;
        float gr = m[5], gg = m[6], gb = m[7], go = m[9] * 255;
        float br = m[10], bg = m[11], bb = m[12], bo = m[14] * 255;
        for (var i = 0; i < pixels.Length; i += 4)
        {
            float b = pixels[i], g = pixels[i + 1], r = pixels[i + 2];
            pixels[i] = Clamp((br * r) + (bg * g) + (bb * b) + bo);
            pixels[i + 1] = Clamp((gr * r) + (gg * g) + (gb * b) + go);
            pixels[i + 2] = Clamp((rr * r) + (rg * g) + (rb * b) + ro);
        }

        var result = BitmapSource.Create(width, height, source.DpiX, source.DpiY, PixelFormats.Bgra32, null, pixels, stride);
        result.Freeze();
        return result;
    }

    private static byte Clamp(float v) => v <= 0 ? (byte)0 : v >= 255 ? (byte)255 : (byte)(v + 0.5f);
}
