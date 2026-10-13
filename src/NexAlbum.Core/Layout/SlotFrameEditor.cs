using NexAlbum.Core.Geometry;
using NexAlbum.Core.Projects;

namespace NexAlbum.Core.Layout;

/// <summary>Çerçeve düzenlemede tutamaç: köşeler ve kenar ortaları (yeniden boyutlandırma) ya da gövde (taşıma).</summary>
public enum FrameHandle
{
    Body,
    TopLeft,
    Top,
    TopRight,
    Right,
    BottomRight,
    Bottom,
    BottomLeft,
    Left,
}

/// <summary>
/// Yuva çerçevesini elle düzenleme (Karar 10 Rev. 4): taşıma, yeniden boyutlandırma ve geometrik yuvalarda köşe düzenleme.
/// Hesaplar saftır; sürüklerken ekranda önizlenir, bırakınca <see cref="SetFrame"/> ile sayfaya yazılır. Çokgen köşeleri
/// yuva içinde normalize (0–1) saklandığından taşıma ve boyutlandırma biçimi korur; köşe düzenleme dışbükeyliği bozamaz.
/// </summary>
public static class SlotFrameEditor
{
    /// <summary>En küçük yuva kenarı (mm).</summary>
    public const double MinSizeMm = 5;

    /// <summary>Tutamaçla yeniden boyutlandırma (tuval sınırı içinde; Shift ile oran korunur).</summary>
    public static RectMm Resize(RectMm start, FrameHandle handle, double dx, double dy, RectMm limits, bool keepAspect = false)
    {
        if (handle == FrameHandle.Body)
        {
            return Move(start, dx, dy, limits);
        }

        double left = start.X, top = start.Y, right = start.Right, bottom = start.Bottom;
        var moveLeft = handle is FrameHandle.Left or FrameHandle.TopLeft or FrameHandle.BottomLeft;
        var moveRight = handle is FrameHandle.Right or FrameHandle.TopRight or FrameHandle.BottomRight;
        var moveTop = handle is FrameHandle.Top or FrameHandle.TopLeft or FrameHandle.TopRight;
        var moveBottom = handle is FrameHandle.Bottom or FrameHandle.BottomLeft or FrameHandle.BottomRight;
        if (moveLeft)
        {
            left = Math.Clamp(left + dx, limits.X, right - MinSizeMm);
        }

        if (moveRight)
        {
            right = Math.Clamp(right + dx, left + MinSizeMm, limits.Right);
        }

        if (moveTop)
        {
            top = Math.Clamp(top + dy, limits.Y, bottom - MinSizeMm);
        }

        if (moveBottom)
        {
            bottom = Math.Clamp(bottom + dy, top + MinSizeMm, limits.Bottom);
        }

        var result = new RectMm(left, top, right - left, bottom - top);
        var corner = (moveLeft || moveRight) && (moveTop || moveBottom);
        if (keepAspect && corner && start.Height > 0)
        {
            // Oran korunur: genişlik belirleyici; karşı köşe sabit kalır.
            var aspect = start.Width / start.Height;
            var height = Math.Max(MinSizeMm, result.Width / aspect);
            var width = height * aspect;
            var x = moveLeft ? start.Right - width : start.X;
            var y = moveTop ? start.Bottom - height : start.Y;
            var candidate = new RectMm(x, y, width, height);
            if (candidate.X >= limits.X - 1e-6 && candidate.Y >= limits.Y - 1e-6 && candidate.Right <= limits.Right + 1e-6 && candidate.Bottom <= limits.Bottom + 1e-6)
            {
                result = candidate;
            }
        }

        return result;
    }

    /// <summary>Gövdeyi taşır; tuval dışına çıkmaz.</summary>
    public static RectMm Move(RectMm start, double dx, double dy, RectMm limits)
    {
        var x = Math.Clamp(start.X + dx, limits.X, Math.Max(limits.X, limits.Right - start.Width));
        var y = Math.Clamp(start.Y + dy, limits.Y, Math.Max(limits.Y, limits.Bottom - start.Height));
        return new RectMm(x, y, start.Width, start.Height);
    }

    /// <summary>
    /// Yuvanın düzenlenebilir köşeleri (normalize): çokgen ve hazır geometrik biçimlerde biçimin köşeleri, düz dikdörtgende dört
    /// köşe. Daire, kemer ve yuvarlak köşelide köşe düzenleme yoktur (null).
    /// </summary>
    public static IReadOnlyList<UnitPoint>? Corners(Slot slot)
    {
        ArgumentNullException.ThrowIfNull(slot);
        return slot.Shape switch
        {
            SlotShape.Circle or SlotShape.Rounded or SlotShape.Arch => null,
            SlotShape.Rectangle => slot.CornerRadiusMm > 0 ? null : [new(0, 0), new(1, 0), new(1, 1), new(0, 1)],
            _ => SlotOutline.Unit(slot.Shape, slot.Polygon, slot.Bounds.Width, slot.Bounds.Height),
        };
    }

    /// <summary>
    /// Bir köşeyi taşır (yuva içinde kalır). Çokgen dışbükey kalmıyorsa ya da köşe komşusuyla çakışıyorsa null döner (önceki
    /// biçim korunur).
    /// </summary>
    public static IReadOnlyList<UnitPoint>? MoveCorner(IReadOnlyList<UnitPoint> corners, int index, double x, double y)
    {
        ArgumentNullException.ThrowIfNull(corners);
        if (index < 0 || index >= corners.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        var moved = UnitPoint.Clamped(x, y);
        var list = corners.ToList();
        list[index] = moved;
        var tooClose = list.Where((p, i) => i != index).Any(p => Math.Abs(p.X - moved.X) < 0.02 && Math.Abs(p.Y - moved.Y) < 0.02);
        return tooClose || !Polygon.IsConvex(list) ? null : list;
    }

    /// <summary>
    /// Yuvanın çerçevesini (ve köşeleri verilmişse biçimini) yazar. Köşeler verildiğinde yuva serbest çokgene dönüşür; dört köşesi
    /// tam dikdörtgen olan çokgen düz dikdörtgen olarak saklanır. Değişiklik yoksa aynı sayfa döner.
    /// </summary>
    public static AlbumPage SetFrame(AlbumPage page, string slotId, RectMm bounds, IReadOnlyList<UnitPoint>? corners, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(page);
        var slot = page.Slots.FirstOrDefault(s => string.Equals(s.Id, slotId, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Yuva bulunamadı: {slotId}");
        if (bounds.Width < MinSizeMm - 1e-6 || bounds.Height < MinSizeMm - 1e-6)
        {
            throw new ArgumentException("Yuva çok küçük.", nameof(bounds));
        }

        var changed = slot with { Bounds = bounds };
        if (corners is not null)
        {
            if (!Polygon.IsConvex(corners))
            {
                throw new ArgumentException("Köşeler dışbükey bir çokgen oluşturmalı.", nameof(corners));
            }

            changed = IsFullRectangle(corners)
                ? changed with { Shape = SlotShape.Rectangle, Polygon = null }
                : changed with { Shape = SlotShape.Polygon, Polygon = corners };
        }

        return changed == slot ? page : page.ReplaceElement(changed) with { UpdatedAt = now };
    }

    private static bool IsFullRectangle(IReadOnlyList<UnitPoint> corners)
    {
        if (corners.Count != 4)
        {
            return false;
        }

        UnitPoint[] full = [new(0, 0), new(1, 0), new(1, 1), new(0, 1)];
        return full.All(f => corners.Any(c => Math.Abs(c.X - f.X) < 1e-6 && Math.Abs(c.Y - f.Y) < 1e-6));
    }
}
