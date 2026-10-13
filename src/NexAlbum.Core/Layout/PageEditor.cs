using NexAlbum.Core.Design;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Projects;

namespace NexAlbum.Core.Layout;

/// <summary>
/// Elle düzenleme: yuvaya fotoğraf koyma, iki yuvanın fotoğrafını değiştirme, çıkarma, yuva içinde kaydırma/yakınlaştırma,
/// sığdırma modu ve tek yuvaya çerçeve. Kilit denetimi çağıranın işidir; tüm işlemler saftır (yeni sayfa döner).
/// </summary>
public static class PageEditor
{
    /// <summary>Yakınlaştırma alt sınırı: 1 = yuvayı tam kaplar (cover) ya da tam sığar (contain).</summary>
    public const double MinScale = 1;

    /// <summary>Yakınlaştırma üst sınırı.</summary>
    public const double MaxScale = 5;

    /// <summary>
    /// Fotoğrafı yuvaya koyar (önceki fotoğraf sayfadan çıkar). Fotoğraf aynı sayfada başka bir yuvadaysa iki yuva yer değiştirir.
    /// </summary>
    public static AlbumPage PlacePhoto(AlbumPage page, string slotId, string assetId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentException.ThrowIfNullOrWhiteSpace(assetId);
        var slot = FindSlot(page, slotId);
        if (slot.Placement?.AssetId == assetId)
        {
            return page;
        }

        var holder = page.Slots.FirstOrDefault(s => s.Id != slotId && s.Placement?.AssetId == assetId);
        return holder is not null
            ? Swap(page, holder.Id, slotId, now)
            : page.ReplaceElement(slot with { Placement = new PhotoPlacement { AssetId = assetId } }) with { UpdatedAt = now };
    }

    /// <summary>İki yuvanın fotoğraflarını değiştirir. Kaydırma ve yakınlaştırma sıfırlanır (yuva oranları farklıdır); renk ayarları korunur.</summary>
    public static AlbumPage Swap(AlbumPage page, string firstSlotId, string secondSlotId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(page);
        var first = FindSlot(page, firstSlotId);
        var second = FindSlot(page, secondSlotId);
        if (first.Id == second.Id)
        {
            return page;
        }

        var swapped = page
            .ReplaceElement(first with { Placement = Reset(second.Placement) })
            .ReplaceElement(second with { Placement = Reset(first.Placement) });
        return swapped with { UpdatedAt = now };
    }

    /// <summary>Yuvadaki fotoğrafı çıkarır; fotoğraf "kullanılmamış" olur.</summary>
    public static AlbumPage Clear(AlbumPage page, string slotId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(page);
        var slot = FindSlot(page, slotId);
        return slot.Placement is null ? page : page.ReplaceElement(slot with { Placement = null }) with { UpdatedAt = now };
    }

    /// <summary>Yerleşimi (kaydırma, ölçek) sınırlara uydurarak yazar.</summary>
    /// <param name="page">Sayfa.</param>
    /// <param name="slotId">Yuva.</param>
    /// <param name="placement">Yeni yerleşim; varlık kimliği yuvadakiyle aynı olmalıdır.</param>
    /// <param name="displayAspect">Fotoğrafın EXIF yönü uygulanmış en-boy oranı.</param>
    /// <param name="now">Zaman.</param>
    public static AlbumPage SetPlacement(AlbumPage page, string slotId, PhotoPlacement placement, double displayAspect, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(placement);
        var slot = FindSlot(page, slotId);
        if (slot.Placement?.AssetId != placement.AssetId)
        {
            throw new ArgumentException("Yerleşim yuvadaki fotoğrafa ait değil.", nameof(placement));
        }

        var clamped = Clamp(placement, slot.PhotoArea, displayAspect, slot.CropMode);
        return clamped == slot.Placement ? page : page.ReplaceElement(slot with { Placement = clamped }) with { UpdatedAt = now };
    }

    /// <summary>Sığdırma modunu değiştirir (kapla ↔ sığdır); kaydırma ve ölçek sıfırlanır.</summary>
    public static AlbumPage SetCropMode(AlbumPage page, string slotId, CropMode mode, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(page);
        var slot = FindSlot(page, slotId);
        if (slot.CropMode == mode)
        {
            return page;
        }

        return page.ReplaceElement(slot with { CropMode = mode, Placement = Reset(slot.Placement) }) with { UpdatedAt = now };
    }

    /// <summary>Tek yuvaya çerçeve stili uygular.</summary>
    public static AlbumPage ApplyFrame(AlbumPage page, string slotId, FrameStyle frame, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(frame);
        return page.ReplaceElement(frame.ApplyTo(FindSlot(page, slotId))) with { UpdatedAt = now };
    }

    /// <summary>
    /// Ölçeği [<see cref="MinScale"/>, <see cref="MaxScale"/>] aralığına, kaydırmayı görüntünün yuvadan taşmayacağı
    /// (kapla modunda boşluk kalmayacağı, sığdır modunda yuvadan çıkmayacağı) aralığa sınırlar.
    /// Kaydırma, yuva genişliği/yüksekliğinin oranıdır (görüntü merkezinin yuva merkezinden uzaklığı).
    /// </summary>
    public static PhotoPlacement Clamp(PhotoPlacement placement, RectMm area, double displayAspect, CropMode mode)
    {
        ArgumentNullException.ThrowIfNull(placement);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(displayAspect);
        if (area.IsEmpty)
        {
            return placement with { OffsetX = 0, OffsetY = 0 };
        }

        var scale = Math.Clamp(placement.Scale, MinScale, MaxScale);
        var (width, height) = BaseSize(area, displayAspect * placement.Crop.Width / placement.Crop.Height, mode);
        var maxX = Math.Min(1, Math.Abs((width * scale / area.Width) - 1) / 2);
        var maxY = Math.Min(1, Math.Abs((height * scale / area.Height) - 1) / 2);
        return placement with
        {
            Scale = scale,
            OffsetX = Math.Clamp(placement.OffsetX, -maxX, maxX),
            OffsetY = Math.Clamp(placement.OffsetY, -maxY, maxY),
        };
    }

    /// <summary>
    /// İmlecin altındaki nokta yerinde kalacak biçimde yakınlaştırır/uzaklaştırır (Karar 12 §10.3): kullanıcı fotoğrafın
    /// hangi bölgesine yaklaşmak istiyorsa tekerleği oraya getirir.
    /// </summary>
    /// <param name="placement">Geçerli yerleşim.</param>
    /// <param name="area">Fotoğraf alanı (mm).</param>
    /// <param name="displayAspect">Fotoğrafın görünen en-boy oranı.</param>
    /// <param name="mode">Sığdırma modu.</param>
    /// <param name="factor">Çarpan (&gt;1 yakınlaştırır).</param>
    /// <param name="anchorX">İmleç, yuva merkezine göre yuva genişliğinin oranı (−0,5 sol kenar … 0,5 sağ kenar).</param>
    /// <param name="anchorY">İmleç, yuva merkezine göre yuva yüksekliğinin oranı.</param>
    public static PhotoPlacement ZoomAt(PhotoPlacement placement, RectMm area, double displayAspect, CropMode mode, double factor, double anchorX, double anchorY)
    {
        ArgumentNullException.ThrowIfNull(placement);
        var scale = Math.Clamp(placement.Scale * factor, MinScale, MaxScale);
        var k = scale / placement.Scale;
        var x = anchorX - ((anchorX - placement.OffsetX) * k);
        var y = anchorY - ((anchorY - placement.OffsetY) * k);
        return Clamp(placement with { Scale = scale, OffsetX = Math.Clamp(x, -1, 1), OffsetY = Math.Clamp(y, -1, 1) }, area, displayAspect, mode);
    }

    /// <summary>Fotoğrafı yuva ölçüsünün oranı kadar kaydırır (yön tuşları); sınırlar korunur.</summary>
    public static PhotoPlacement Nudge(PhotoPlacement placement, RectMm area, double displayAspect, CropMode mode, double dx, double dy)
    {
        ArgumentNullException.ThrowIfNull(placement);
        return Clamp(
            placement with { OffsetX = Math.Clamp(placement.OffsetX + dx, -1, 1), OffsetY = Math.Clamp(placement.OffsetY + dy, -1, 1) },
            area,
            displayAspect,
            mode);
    }

    /// <summary>
    /// Fotoğrafın hangi bölgesinin görüneceğini seçer: −1 sol/üst, 0 orta, 1 sağ/alt (ör. sol üst = fotoğrafın sol üst köşesi
    /// yuvanın sol üst köşesine oturur). Yakınlaştırma korunur.
    /// </summary>
    public static PhotoPlacement Align(PhotoPlacement placement, RectMm area, double displayAspect, CropMode mode, int horizontal, int vertical)
    {
        ArgumentNullException.ThrowIfNull(placement);
        var edge = Clamp(placement with { OffsetX = 1, OffsetY = 1 }, area, displayAspect, mode);
        return placement with
        {
            Scale = edge.Scale,
            OffsetX = -Math.Sign(horizontal) * edge.OffsetX,
            OffsetY = -Math.Sign(vertical) * edge.OffsetY,
        };
    }

    /// <summary>Ölçek 1'deki görüntü boyutu (ImageFit ile aynı kural).</summary>
    private static (double Width, double Height) BaseSize(RectMm area, double aspect, CropMode mode)
    {
        var slotAspect = area.Width / area.Height;
        return mode switch
        {
            CropMode.Fill => (area.Width, area.Height),
            CropMode.Contain => aspect > slotAspect ? (area.Width, area.Width / aspect) : (area.Height * aspect, area.Height),
            _ => aspect > slotAspect ? (area.Height * aspect, area.Height) : (area.Width, area.Width / aspect),
        };
    }

    private static PhotoPlacement? Reset(PhotoPlacement? placement) =>
        placement is null ? null : placement with { OffsetX = 0, OffsetY = 0, Scale = 1 };

    private static Slot FindSlot(AlbumPage page, string slotId) =>
        page.FindElement(slotId) as Slot ?? throw new ArgumentException("Yuva bulunamadı: " + slotId, nameof(slotId));
}
