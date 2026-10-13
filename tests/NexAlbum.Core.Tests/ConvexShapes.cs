using NexAlbum.Core.Geometry;
using NexAlbum.Core.Templates;

namespace NexAlbum.Core.Tests;

/// <summary>Testler için yuva biçimleri: çözülmüş yuvanın köşeleri ve iki dışbükey biçimin pozitif alanla çakışması.</summary>
internal static class ConvexShapes
{
    public static IReadOnlyList<PointMm> Outline(ResolvedSlot slot) => slot.Polygon is { } polygon
        ? Polygon.Map(polygon, slot.Bounds)
        : [new(slot.Bounds.X, slot.Bounds.Y), new(slot.Bounds.Right, slot.Bounds.Y), new(slot.Bounds.Right, slot.Bounds.Bottom), new(slot.Bounds.X, slot.Bounds.Bottom)];

    /// <summary>Ayırma ekseni teoremi: hiçbir kenar normali biçimleri ayırmıyorsa (tolerans payıyla) çakışırlar.</summary>
    public static bool Overlap(IReadOnlyList<PointMm> a, IReadOnlyList<PointMm> b, double tolerance = 1e-6)
    {
        foreach (var shape in new[] { a, b })
        {
            for (var i = 0; i < shape.Count; i++)
            {
                var p = shape[i];
                var q = shape[(i + 1) % shape.Count];
                var nx = -(q.Y - p.Y);
                var ny = q.X - p.X;
                var length = Math.Sqrt((nx * nx) + (ny * ny));
                if (length < 1e-12)
                {
                    continue;
                }

                var (minA, maxA) = Project(a, nx / length, ny / length);
                var (minB, maxB) = Project(b, nx / length, ny / length);
                if (maxA <= minB + tolerance || maxB <= minA + tolerance)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>İki biçim arasındaki en küçük ayrılma (ayırma eksenleri üzerinde ölçülür; çakışmada negatif).</summary>
    public static double Separation(IReadOnlyList<PointMm> a, IReadOnlyList<PointMm> b)
    {
        var best = double.NegativeInfinity;
        foreach (var shape in new[] { a, b })
        {
            for (var i = 0; i < shape.Count; i++)
            {
                var p = shape[i];
                var q = shape[(i + 1) % shape.Count];
                var nx = -(q.Y - p.Y);
                var ny = q.X - p.X;
                var length = Math.Sqrt((nx * nx) + (ny * ny));
                if (length < 1e-12)
                {
                    continue;
                }

                var (minA, maxA) = Project(a, nx / length, ny / length);
                var (minB, maxB) = Project(b, nx / length, ny / length);
                best = Math.Max(best, Math.Max(minB - maxA, minA - maxB));
            }
        }

        return best;
    }

    private static (double Min, double Max) Project(IReadOnlyList<PointMm> points, double nx, double ny)
    {
        var values = points.Select(p => (p.X * nx) + (p.Y * ny)).ToList();
        return (values.Min(), values.Max());
    }
}
