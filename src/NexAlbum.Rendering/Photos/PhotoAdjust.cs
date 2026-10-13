using NexAlbum.Core.Design;
using NexAlbum.Core.Projects;
using NexAlbum.Rendering.Video;
using SkiaSharp;

namespace NexAlbum.Rendering.Photos;

/// <summary>
/// Fotoğraf ayarlarını (Core <see cref="ColorMatrix"/>) tam çözünürlükte uygular: PDF ve dışa aktarım için ayarlanmış JPEG
/// üretir. Orijinal dosya değişmez (efekt belgesi §5); EXIF yönü uygulanmış olarak döner.
/// </summary>
public static class PhotoAdjust
{
    /// <summary>Ayarlanmış, yönü uygulanmış JPEG baytları; dosya çözülemezse null.</summary>
    public static byte[]? AdjustedJpeg(string path, ImageAdjustments adjustments, int? orientation = null, int quality = 92)
    {
        ArgumentNullException.ThrowIfNull(adjustments);
        using var image = FilePhotoSource.Decode(path, int.MaxValue, orientation);
        if (image is null)
        {
            return null;
        }

        using var adjusted = Apply(image, adjustments);
        using var data = adjusted.Encode(SKEncodedImageFormat.Jpeg, quality);
        return data?.ToArray();
    }

    /// <summary>Görüntüye renk matrisini uygular (yeni görüntü).</summary>
    public static SKImage Apply(SKImage image, ImageAdjustments adjustments)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(adjustments);
        using var surface = SKSurface.Create(new SKImageInfo(image.Width, image.Height, SKColorType.Rgba8888, SKAlphaType.Premul));
        using var filter = SKColorFilter.CreateColorMatrix(ColorMatrix.From(adjustments).ToFloats());
        using var paint = new SKPaint { ColorFilter = filter };
        surface.Canvas.Clear(SKColors.White);
        surface.Canvas.DrawImage(image, 0, 0, paint);
        return surface.Snapshot();
    }
}
