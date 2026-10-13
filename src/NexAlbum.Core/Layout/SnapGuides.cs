using NexAlbum.Core.Geometry;

namespace NexAlbum.Core.Layout;

/// <summary>Yapışma çizgisinin kaynağı (öncelik sırasıyla).</summary>
public enum SnapKind
{
    PageCenter,
    PageEdge,
    SafeArea,
    Element,
}

/// <summary>Ekranda gösterilecek kılavuz: dikey çizgi (x konumunda) ya da yatay çizgi (y konumunda), mm.</summary>
public readonly record struct SnapGuide(bool Vertical, double Position, SnapKind Kind);

/// <summary>Yapıştırılmış sol üst köşe ve gösterilecek kılavuzlar.</summary>
public sealed record SnapResult(double X, double Y, IReadOnlyList<SnapGuide> Guides);

/// <summary>
/// Akıllı hizalama: sürüklenen kutunun sol/orta/sağ (ve üst/orta/alt) çizgileri; sayfanın ortasına ve kenarlarına, güvenli
/// alana ve sayfadaki diğer öğelere tolerans içindeyse yapışır. Her eksen ayrı ve en yakın hedefe göre hesaplanır; eşit
/// uzaklıkta sayfa ortası önce gelir. Güvenli alan profilde yoksa hedef de yoktur (değer uydurulmaz). Saftır.
/// </summary>
public static class SnapGuides
{
    private const double Same = 0.01;

    public static SnapResult Snap(RectMm moving, RectMm page, RectMm? safeArea, IEnumerable<RectMm> others, double tolerance)
    {
        ArgumentNullException.ThrowIfNull(others);
        var list = others.ToList();
        var xTargets = new List<(double At, SnapKind Kind)> { (page.X + (page.Width / 2), SnapKind.PageCenter), (page.X, SnapKind.PageEdge), (page.Right, SnapKind.PageEdge) };
        var yTargets = new List<(double At, SnapKind Kind)> { (page.Y + (page.Height / 2), SnapKind.PageCenter), (page.Y, SnapKind.PageEdge), (page.Bottom, SnapKind.PageEdge) };
        if (safeArea is { } safe)
        {
            xTargets.AddRange([(safe.X, SnapKind.SafeArea), (safe.Right, SnapKind.SafeArea)]);
            yTargets.AddRange([(safe.Y, SnapKind.SafeArea), (safe.Bottom, SnapKind.SafeArea)]);
        }

        foreach (var r in list)
        {
            xTargets.AddRange([(r.X, SnapKind.Element), (r.X + (r.Width / 2), SnapKind.Element), (r.Right, SnapKind.Element)]);
            yTargets.AddRange([(r.Y, SnapKind.Element), (r.Y + (r.Height / 2), SnapKind.Element), (r.Bottom, SnapKind.Element)]);
        }

        var (dx, gx) = Axis([moving.X, moving.X + (moving.Width / 2), moving.Right], xTargets, tolerance, vertical: true);
        var (dy, gy) = Axis([moving.Y, moving.Y + (moving.Height / 2), moving.Bottom], yTargets, tolerance, vertical: false);
        return new SnapResult(moving.X + dx, moving.Y + dy, [.. gx, .. gy]);
    }

    private static (double Delta, List<SnapGuide> Guides) Axis(double[] anchors, List<(double At, SnapKind Kind)> targets, double tolerance, bool vertical)
    {
        double? best = null;
        var bestKind = SnapKind.Element;
        foreach (var anchor in anchors)
        {
            foreach (var (at, kind) in targets)
            {
                var delta = at - anchor;
                if (Math.Abs(delta) > tolerance)
                {
                    continue;
                }

                if (best is null || Math.Abs(delta) < Math.Abs(best.Value) - Same || (Math.Abs(Math.Abs(delta) - Math.Abs(best.Value)) <= Same && kind < bestKind))
                {
                    best = delta;
                    bestKind = kind;
                }
            }
        }

        if (best is not { } d)
        {
            return (0, []);
        }

        // Yapıştıktan sonra çakışan tüm hedefler gösterilir (ör. sol kenar hem güvenli alana hem başka metne denk gelir).
        var guides = new List<SnapGuide>();
        foreach (var anchor in anchors)
        {
            foreach (var (at, kind) in targets.OrderBy(t => t.Kind))
            {
                if (Math.Abs(at - (anchor + d)) <= Same && !guides.Any(g => Math.Abs(g.Position - at) <= Same))
                {
                    guides.Add(new SnapGuide(vertical, at, kind));
                }
            }
        }

        return (d, guides);
    }
}
