using NexAlbum.Core.Geometry;
using NexAlbum.Core.Templates;

namespace NexAlbum.Core.Layout;

/// <summary>Sayfaya çözülmüş şablon yuvası.</summary>
public sealed record LayoutSlot(TemplateSlot Template, RectMm Bounds)
{
    public double AspectRatio => Bounds.Width / Bounds.Height;

    public PageOrientation Orientation => PhotoClassifier.Classify(Bounds.Width, Bounds.Height);
}

/// <summary>Bir fotoğrafın bir yuvaya ataması.</summary>
public sealed record SlotAssignment(LayoutSlot Slot, ClassifiedPhoto? Photo, double Cost, double CropLoss);

/// <summary>
/// Aşama 4 — yuva eşleştirme (Karar 10 §9). MVP: aç gözlü; öncelikli (favori) fotoğraflar önce atanır.
/// </summary>
public static class SlotMatcher
{
    /// <summary>Karar 10 §9.3 maliyet fonksiyonu; düşük olan iyidir.</summary>
    public static double Cost(ClassifiedPhoto photo, LayoutSlot slot)
    {
        ArgumentNullException.ThrowIfNull(photo);
        ArgumentNullException.ThrowIfNull(slot);
        var cost = Math.Abs(photo.AspectRatio - slot.AspectRatio) * 0.5
            + (OrientationMismatch(photo, slot) ? 0.3 : 0)
            + (slot.Template.Priority == 1 ? -0.1 : 0)
            + (CropLoss(photo.AspectRatio, slot.AspectRatio) * 0.2);

        // Yuvanın kabul ettiği oran aralığı dışındaki fotoğraf ciddi biçimde cezalandırılır.
        if ((slot.Template.MinAspectRatio is { } min && photo.AspectRatio < min)
            || (slot.Template.MaxAspectRatio is { } max && photo.AspectRatio > max))
        {
            cost += 1;
        }

        return cost;
    }

    /// <summary>Kaplama (cover) modunda kırpılan alan oranı: 1 − kullanılan/toplam (Karar 10 §9.8).</summary>
    public static double CropLoss(double photoAspectRatio, double slotAspectRatio)
    {
        var small = Math.Min(photoAspectRatio, slotAspectRatio);
        var large = Math.Max(photoAspectRatio, slotAspectRatio);
        return 1 - (small / large);
    }

    public static bool OrientationMismatch(ClassifiedPhoto photo, LayoutSlot slot)
    {
        ArgumentNullException.ThrowIfNull(photo);
        ArgumentNullException.ThrowIfNull(slot);
        var recommended = slot.Template.RecommendedOrientations;
        return recommended.Count > 0 ? !recommended.Contains(photo.Orientation) : photo.Orientation != slot.Orientation;
    }

    /// <summary>
    /// Fotoğrafları yuvalara atar. Fotoğraf yuvadan azsa kalan yuvalar boş döner (Photo = null).
    /// Sıra: favoriler önce, sonra verilen sıra; eşit maliyette yuva sırası korunur (deterministik).
    /// </summary>
    public static IReadOnlyList<SlotAssignment> Match(IReadOnlyList<ClassifiedPhoto> photos, IReadOnlyList<LayoutSlot> slots)
    {
        ArgumentNullException.ThrowIfNull(photos);
        ArgumentNullException.ThrowIfNull(slots);
        if (photos.Count > slots.Count)
        {
            throw new ArgumentException("Fotoğraf sayısı yuva sayısından fazla olamaz.", nameof(photos));
        }

        var available = slots.ToList();
        var result = new List<SlotAssignment>(slots.Count);
        foreach (var photo in photos.OrderByDescending(p => p.Favorite))
        {
            var bestIndex = 0;
            var bestCost = double.PositiveInfinity;
            for (var i = 0; i < available.Count; i++)
            {
                var c = Cost(photo, available[i]);
                if (c < bestCost - 1e-12)
                {
                    bestCost = c;
                    bestIndex = i;
                }
            }

            var slot = available[bestIndex];
            available.RemoveAt(bestIndex);
            result.Add(new SlotAssignment(slot, photo, bestCost, CropLoss(photo.AspectRatio, slot.AspectRatio)));
        }

        result.AddRange(available.Select(s => new SlotAssignment(s, null, 0, 0)));

        // Sonuç şablondaki yuva sırasıyla döner.
        return [.. result.OrderBy(a => IndexOf(slots, a.Slot))];
    }

    private static int IndexOf(IReadOnlyList<LayoutSlot> slots, LayoutSlot slot)
    {
        for (var i = 0; i < slots.Count; i++)
        {
            if (ReferenceEquals(slots[i], slot))
            {
                return i;
            }
        }

        return -1;
    }
}
