using NexAlbum.Core.Geometry;
using NexAlbum.Core.Projects;

namespace NexAlbum.Core.Layout;

/// <summary>
/// Albüm içi tahribatsız fotoğraf düzenleme (efekt belgesi §4.3): yuvadaki yerleşimin renk ayarları, düzeltme açısı ve
/// fotoğrafın çeyrek döndürülmesi. Orijinal dosyaya dokunulmaz; saftır.
/// </summary>
public static class PhotoEditing
{
    /// <summary>Düzeltme (eğri ufku doğrultma) sınırı, derece.</summary>
    public const double MaxStraighten = 15;

    /// <summary>Yuvadaki fotoğrafın renk ayarlarını değiştirir; aynıysa sayfa aynen döner.</summary>
    public static AlbumPage SetAdjustments(AlbumPage page, string slotId, ImageAdjustments adjustments, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(adjustments);
        if (page.FindElement(slotId) is not Slot { Placement: { } placement } slot || placement.Adjustments == adjustments)
        {
            return page;
        }

        return page.ReplaceElement(slot with { Placement = placement with { Adjustments = adjustments } }) with { UpdatedAt = now };
    }

    /// <summary>Sayfadaki tüm fotoğraflara aynı ayarları uygular (tutarlı renk).</summary>
    public static AlbumPage SetAdjustmentsForAll(AlbumPage page, ImageAdjustments adjustments, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(page);
        var result = page;
        foreach (var slot in page.Slots.Where(s => s.Placement is not null).ToList())
        {
            result = SetAdjustments(result, slot.Id, adjustments, now);
        }

        return result;
    }

    /// <summary>Düzeltme açısı (±15°); fotoğraf yuvayı kaplamaya devam eder (bkz. <see cref="StraightenCover"/>).</summary>
    public static AlbumPage SetStraighten(AlbumPage page, string slotId, double degrees, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(page);
        var angle = Math.Round(Math.Clamp(degrees, -MaxStraighten, MaxStraighten), 2);
        if (page.FindElement(slotId) is not Slot { Placement: { } placement } slot || Math.Abs(placement.Rotation - angle) < 1e-9)
        {
            return page;
        }

        return page.ReplaceElement(slot with { Placement = placement with { Rotation = angle } }) with { UpdatedAt = now };
    }

    /// <summary>
    /// Döndürülen görüntünün <paramref name="areaWidth"/>×<paramref name="areaHeight"/> alanı boşluksuz kaplaması için gereken
    /// büyütme (≥ 1). Görüntü (genişlik×yükseklik) alanı döndürmeden önce kaplıyor kabul edilir.
    /// </summary>
    public static double StraightenCover(double areaWidth, double areaHeight, double imageWidth, double imageHeight, double degrees)
    {
        if (degrees == 0 || areaWidth <= 0 || areaHeight <= 0 || imageWidth <= 0 || imageHeight <= 0)
        {
            return 1;
        }

        var radians = Math.Abs(degrees) * Math.PI / 180;
        var (sin, cos) = Math.SinCos(radians);
        var needWidth = (areaWidth * cos) + (areaHeight * sin);
        var needHeight = (areaWidth * sin) + (areaHeight * cos);
        return Math.Max(1, Math.Max(needWidth / imageWidth, needHeight / imageHeight));
    }

    /// <summary>
    /// Fotoğrafı saat yönünde <paramref name="quarterTurns"/> × 90° döndürür (yan çekilmiş ya da EXIF'i yanlış fotoğraf).
    /// Görüntüleme yönü tüm albümde değişir: yön bilgisi varlığa (ve varsa düzenlenmiş sürüme) işlenir; fotoğrafı kullanan
    /// yuvalarda kırpma ve kaydırma, yeni en-boy oranına göre sıfırlanır.
    /// </summary>
    public static AlbumProject RotateQuarter(AlbumProject project, string assetId, int quarterTurns, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(project);
        var asset = project.FindAsset(assetId) ?? throw new ArgumentException("Fotoğraf projede yok.", nameof(assetId));
        var turns = ((quarterTurns % 4) + 4) % 4;
        if (turns == 0)
        {
            return project;
        }

        var rotated = asset with
        {
            ExifOrientation = RotateOrientation(asset.ExifOrientation, turns),
            Edit = asset.Edit is { } edit ? edit with { ExifOrientation = RotateOrientation(edit.ExifOrientation, turns) } : null,
        };
        var pages = project.Pages.Select(page =>
        {
            var result = page;
            foreach (var slot in page.Slots.Where(s => s.Placement?.AssetId == assetId).ToList())
            {
                var placement = slot.Placement! with { OffsetX = 0, OffsetY = 0, Crop = NormalizedRect.Full };
                var clamped = PageEditor.Clamp(placement, slot.PhotoArea, rotated.Effective().DisplayAspectRatio, slot.CropMode);
                result = result.ReplaceElement(slot with { Placement = clamped }) with { UpdatedAt = now };
            }

            return result;
        });
        return project.ReplaceAsset(rotated).WithPages(pages.ToList());
    }

    /// <summary>EXIF yön kodunu (1–8) saat yönünde çeyrek turlarla döndürür; ayna korunur.</summary>
    public static int RotateOrientation(int? orientation, int quarterTurns)
    {
        // Kod → (saat yönünde çeyrek tur, ayna): görüntüleme = önce ayna, sonra döndürme.
        var (turns, mirror) = (orientation ?? 1) switch
        {
            2 => (0, true),
            3 => (2, false),
            4 => (2, true),
            5 => (3, true),
            6 => (1, false),
            7 => (1, true),
            8 => (3, false),
            _ => (0, false),
        };
        var total = (((turns + quarterTurns) % 4) + 4) % 4;
        return (total, mirror) switch
        {
            (0, false) => 1,
            (0, true) => 2,
            (2, false) => 3,
            (2, true) => 4,
            (3, true) => 5,
            (1, false) => 6,
            (1, true) => 7,
            _ => 8,
        };
    }
}
