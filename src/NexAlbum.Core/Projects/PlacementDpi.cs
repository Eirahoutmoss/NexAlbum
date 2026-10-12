using NexAlbum.Core.Geometry;

namespace NexAlbum.Core.Projects;

/// <summary>
/// Yuvaya yerleştirilmiş fotoğrafın baskıdaki etkin çözünürlüğü.
/// </summary>
public static class PlacementDpi
{
    /// <summary>
    /// Kırpma, sığdırma modu ve kullanıcı ölçeği dikkate alınarak en kötü eksendeki DPI.
    /// Döndürme, kırpmanın kendisi değil yalnızca yerleşimi etkilediğinden hesaba katılmaz.
    /// </summary>
    public static double Calculate(PhotoAsset asset, Slot slot, PhotoPlacement placement)
    {
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentNullException.ThrowIfNull(slot);
        ArgumentNullException.ThrowIfNull(placement);

        // Paspartu varsa fotoğraf daha küçük bir alanda durur; çözünürlük o alana göre hesaplanır.
        var area = slot.PhotoArea;
        if (area.IsEmpty)
        {
            return 0;
        }

        var croppedWidthPx = asset.DisplayWidth * placement.Crop.Width;
        var croppedHeightPx = asset.DisplayHeight * placement.Crop.Height;
        var dpiX = Length.EffectiveDpi(croppedWidthPx, area.Width);
        var dpiY = Length.EffectiveDpi(croppedHeightPx, area.Height);

        var fitted = slot.CropMode switch
        {
            // Kaplama: görüntü yuvayı tamamen doldurana kadar büyütülür; düşük eksen belirleyicidir.
            CropMode.Cover => Math.Min(dpiX, dpiY),

            // Sığdırma: görüntü tamamen görünür; yüksek eksen ölçeği belirler, diğer eksende boşluk kalır.
            CropMode.Contain => Math.Max(dpiX, dpiY),

            // Esnetme: eksenler ayrı ölçeklenir; en kötü eksen raporlanır.
            CropMode.Fill => Math.Min(dpiX, dpiY),
            _ => throw new ArgumentOutOfRangeException(nameof(slot), slot.CropMode, "Bilinmeyen sığdırma modu."),
        };

        return fitted / placement.Scale;
    }
}
