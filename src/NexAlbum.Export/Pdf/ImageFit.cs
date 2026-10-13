using NexAlbum.Core.Geometry;
using NexAlbum.Core.Projects;

namespace NexAlbum.Export.Pdf;

/// <summary>EXIF yönünü görüntülenen hale getiren dönüşüm: önce yatay ayna, sonra saat yönünde döndürme.</summary>
public readonly record struct OrientationTransform(double RotationDegrees, bool MirrorX)
{
    /// <summary>Döndürme 90° veya 270° ise ham görüntünün genişliği/yüksekliği yer değiştirir.</summary>
    public bool SwapsAxes => RotationDegrees is 90 or 270;

    public static OrientationTransform FromExif(int? orientation) => orientation switch
    {
        2 => new(0, true),
        3 => new(180, false),
        4 => new(180, true),
        5 => new(270, true),
        6 => new(90, false),
        7 => new(90, true),
        8 => new(270, false),
        _ => new(0, false),
    };
}

/// <summary>
/// Fotoğrafın yuvaya yerleşim geometrisi. Tüm görüntünün çizileceği dikdörtgen hesaplanır;
/// yuva dışı kısım kırpma (clip) ile gizlenir. Kırpma seçimi, kullanıcı ölçeği ve kaydırma uygulanır.
/// </summary>
public static class ImageFit
{
    /// <param name="slot">Yuva (mm).</param>
    /// <param name="displayAspect">Görüntünün EXIF yönü uygulanmış en-boy oranı.</param>
    /// <param name="placement">Kırpma, ölçek ve kaydırma.</param>
    /// <param name="mode">Sığdırma modu.</param>
    /// <returns>Görüntülenen (döndürülmüş) tüm görüntünün çizim dikdörtgeni (mm).</returns>
    public static RectMm FullImageRect(RectMm slot, double displayAspect, PhotoPlacement placement, CropMode mode)
    {
        ArgumentNullException.ThrowIfNull(placement);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(displayAspect);

        var crop = placement.Crop;
        var cropAspect = displayAspect * crop.Width / crop.Height;
        var slotAspect = slot.Width / slot.Height;

        double width;
        double height;
        switch (mode)
        {
            case CropMode.Fill:
                width = slot.Width;
                height = slot.Height;
                break;
            case CropMode.Contain:
                (width, height) = cropAspect > slotAspect ? (slot.Width, slot.Width / cropAspect) : (slot.Height * cropAspect, slot.Height);
                break;
            default:
                (width, height) = cropAspect > slotAspect ? (slot.Height * cropAspect, slot.Height) : (slot.Width, slot.Width / cropAspect);
                break;
        }

        // Düzeltme açısı: döndürülen görüntü yuvayı boşluksuz kaplasın diye büyütülür (sığdırmada büyütülmez).
        if (placement.Rotation != 0 && mode != CropMode.Contain)
        {
            var cover = Core.Layout.PhotoEditing.StraightenCover(slot.Width, slot.Height, width, height, placement.Rotation);
            width *= cover;
            height *= cover;
        }

        width *= placement.Scale;
        height *= placement.Scale;

        var centerX = slot.X + (slot.Width / 2) + (placement.OffsetX * slot.Width);
        var centerY = slot.Y + (slot.Height / 2) + (placement.OffsetY * slot.Height);

        var fullWidth = width / crop.Width;
        var fullHeight = height / crop.Height;
        var x = centerX - (width / 2) - (crop.X * fullWidth);
        var y = centerY - (height / 2) - (crop.Y * fullHeight);
        return new RectMm(x, y, fullWidth, fullHeight);
    }
}
