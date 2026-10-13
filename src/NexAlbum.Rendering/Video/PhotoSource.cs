using NexAlbum.Core.Projects;
using SkiaSharp;

namespace NexAlbum.Rendering.Video;

/// <summary>Kareler için fotoğraf sağlayıcı: verilen kimliğin görüntüsü, uzun kenarı en az <c>minLongSide</c> olacak kadar.</summary>
public interface IPhotoSource
{
    SKImage? Get(string assetId, int minLongSide);
}

/// <summary>
/// Diskteki fotoğrafları SkiaSharp ile çözer: JPEG'lerde kodlayıcının küçültme desteğiyle hızlı, EXIF yönü uygulanmış.
/// Son kullanılan birkaç görüntü bellekte tutulur (video sırayla ilerlediği için yeterlidir). Çözülemeyen dosya null döner.
/// </summary>
public sealed class FilePhotoSource(IReadOnlyDictionary<string, PhotoAsset> assets, int capacity = 6) : IPhotoSource, IDisposable
{
    private readonly LinkedList<(string Key, int Size, SKImage Image)> cache = new();
    private readonly Lock gate = new();

    public SKImage? Get(string assetId, int minLongSide)
    {
        if (!assets.TryGetValue(assetId, out var asset))
        {
            return null;
        }

        lock (gate)
        {
            for (var node = cache.First; node is not null; node = node.Next)
            {
                if (node.Value.Key == assetId && node.Value.Size >= minLongSide)
                {
                    cache.Remove(node);
                    cache.AddFirst(node);
                    return node.Value.Image;
                }
            }
        }

        var effective = asset.Effective();
        var image = Decode(effective.SourcePath, minLongSide, effective.ExifOrientation);
        if (image is null)
        {
            return null;
        }

        lock (gate)
        {
            cache.AddFirst((assetId, minLongSide, image));
            while (cache.Count > capacity)
            {
                cache.Last!.Value.Image.Dispose();
                cache.RemoveLast();
            }
        }

        return image;
    }

    /// <summary>Dosyayı çözer; kodlayıcı destekliyorsa küçük ölçekte (uzun kenar ≥ istenen), sonra EXIF yönü uygulanır.</summary>
    public static SKImage? Decode(string path, int minLongSide, int? orientation = null)
    {
        try
        {
            using var codec = SKCodec.Create(path);
            if (codec is null)
            {
                return null;
            }

            // Projedeki yön (kullanıcı döndürmüş olabilir) dosyadaki EXIF'ten önce gelir.
            var origin = orientation is { } o ? (SKEncodedOrigin)o : codec.EncodedOrigin;
            var info = codec.Info;
            var longSide = Math.Max(info.Width, info.Height);
            var scale = Math.Clamp((float)minLongSide / longSide, 0.0625f, 1f);
            var size = codec.GetScaledDimensions(scale);
            using var bitmap = new SKBitmap(new SKImageInfo(size.Width, size.Height, SKColorType.Bgra8888, SKAlphaType.Premul));
            var result = codec.GetPixels(bitmap.Info, bitmap.GetPixels());
            if (result is not (SKCodecResult.Success or SKCodecResult.IncompleteInput))
            {
                // Kodlayıcı bu ölçeği desteklemiyorsa tam boyut çöz.
                using var full = SKBitmap.Decode(codec);
                return full is null ? null : Orient(full, origin);
            }

            return Orient(bitmap, origin);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            foreach (var item in cache)
            {
                item.Image.Dispose();
            }

            cache.Clear();
        }
    }

    private static SKImage Orient(SKBitmap source, SKEncodedOrigin origin)
    {
        if (origin is SKEncodedOrigin.TopLeft or SKEncodedOrigin.Default)
        {
            return SKImage.FromBitmap(source.Copy());
        }

        var swap = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var width = swap ? source.Height : source.Width;
        var height = swap ? source.Width : source.Height;
        using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;

        // x' = scaleX·x + skewX·y + transX ; y' = skewY·x + scaleY·y + transY (EXIF yönleri 2–8).
        var matrix = origin switch
        {
            SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, width, 0, 1, 0, 0, 0, 1),
            SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, width, 0, -1, height, 0, 0, 1),
            SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, height, 0, 0, 1),
            SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
            SKEncodedOrigin.RightTop => new SKMatrix(0, -1, width, 1, 0, 0, 0, 0, 1),
            SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, width, -1, 0, height, 0, 0, 1),
            SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, height, 0, 0, 1),
            _ => SKMatrix.Identity,
        };
        canvas.SetMatrix(matrix);
        canvas.DrawBitmap(source, 0, 0);
        return surface.Snapshot();
    }
}
