using NexAlbum.Core.Geometry;

namespace NexAlbum.Core.Projects;

/// <summary>
/// Yuvanın etkin biçimini çokgene çevirir (Karar 10 Rev. 1). Dikdörtgen, yuvarlak köşe ve elips doğrudan çizilir
/// (null döner); diğer biçimler milimetre cinsinden köşe listesidir. Ekran, PDF ve ön kontrol aynı hesabı kullanır.
/// </summary>
public static class SlotOutline
{
    /// <summary>Kemerin yarım dairesindeki parça sayısı (baskıda pürüzsüz görünecek kadar).</summary>
    public const int ArchSegments = 40;

    /// <summary>Etkin biçim çokgen olarak mı çizilir?</summary>
    public static bool IsPolygonal(Slot slot)
    {
        ArgumentNullException.ThrowIfNull(slot);
        return IsPolygonal(slot.Shape, slot.Polygon);
    }

    public static bool IsPolygonal(SlotShape shape, IReadOnlyList<UnitPoint>? polygon) => shape switch
    {
        SlotShape.Rectangle or SlotShape.Rounded or SlotShape.Circle => false,
        SlotShape.Polygon => polygon is not null,
        _ => true,
    };

    /// <summary>Etkin biçimin yuva kutusu içindeki normalize köşeleri; doğrudan çizilen biçimlerde null.</summary>
    public static IReadOnlyList<UnitPoint>? Unit(SlotShape shape, IReadOnlyList<UnitPoint>? polygon, double width, double height)
    {
        var w = Math.Max(width, 1e-6);
        var h = Math.Max(height, 1e-6);
        switch (shape)
        {
            case SlotShape.Polygon:
                return polygon;
            case SlotShape.Triangle:
                return [new(0.5, 0), new(1, 1), new(0, 1)];
            case SlotShape.Diamond:
                return [new(0.5, 0), new(1, 0.5), new(0.5, 1), new(0, 0.5)];
            case SlotShape.Hexagon:
                {
                    // Düz tepeli düzgün altıgende uçların içeri girişi yüksekliğin 1/(2√3) katıdır.
                    var dx = Math.Min(0.25, h / (2 * Math.Sqrt(3)) / w);
                    return [new(dx, 0), new(1 - dx, 0), new(1, 0.5), new(1 - dx, 1), new(dx, 1), new(0, 0.5)];
                }

            case SlotShape.Octagon:
                {
                    var cut = 0.22 * Math.Min(w, h);
                    var cx = cut / w;
                    var cy = cut / h;
                    return [new(cx, 0), new(1 - cx, 0), new(1, cy), new(1, 1 - cy), new(1 - cx, 1), new(cx, 1), new(0, 1 - cy), new(0, cy)];
                }

            case SlotShape.Arch:
                {
                    // Üst kenar: genişliğin yarısı yarıçaplı yay; çok basık yuvalarda yükseklikle sınırlı elips yay.
                    var ry = Math.Min(w / 2, h * 0.7) / h;
                    var points = new List<UnitPoint>(ArchSegments + 4) { new(0, 1), UnitPoint.Clamped(0, ry) };
                    for (var i = 1; i < ArchSegments; i++)
                    {
                        var angle = Math.PI + (Math.PI * i / ArchSegments);
                        points.Add(UnitPoint.Clamped(0.5 + (0.5 * Math.Cos(angle)), ry + (ry * Math.Sin(angle))));
                    }

                    points.Add(UnitPoint.Clamped(1, ry));
                    points.Add(new(1, 1));
                    return points;
                }

            case SlotShape.Slant:
                {
                    var s = Math.Min(0.25, 0.2 * h / w);
                    return [new(s, 0), new(1, 0), new(1 - s, 1), new(0, 1)];
                }

            default:
                return null;
        }
    }

    /// <summary>Yuvanın dış biçimi (mm); doğrudan çizilen biçimlerde null.</summary>
    public static IReadOnlyList<PointMm>? Outer(Slot slot)
    {
        ArgumentNullException.ThrowIfNull(slot);
        return Unit(slot.Shape, slot.Polygon, slot.Bounds.Width, slot.Bounds.Height) is { } unit ? Polygon.Map(unit, slot.Bounds) : null;
    }

    /// <summary>Fotoğrafın kırpıldığı biçim: dış biçimden paspartunun en ince kenarı kadar içeri. Çöken biçimde boş liste.</summary>
    public static IReadOnlyList<PointMm>? Photo(Slot slot)
    {
        ArgumentNullException.ThrowIfNull(slot);
        var outer = Outer(slot);
        return outer is null || slot.Mat is not { } mat ? outer : Polygon.Offset(outer, mat.Edges.Min);
    }

    /// <summary>Dış biçimi her kenardan eşit öteler: pozitif içe (kontur), negatif dışa (gölge).</summary>
    public static IReadOnlyList<PointMm>? Offset(Slot slot, double distance)
    {
        ArgumentNullException.ThrowIfNull(slot);
        return Outer(slot) is { } outer ? Polygon.Offset(outer, distance) : null;
    }
}
