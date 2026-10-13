using NexAlbum.Core.Geometry;

namespace NexAlbum.Core.Templates;

/// <summary>
/// Uygulamayla gelen şablonlar. Yüzde tabanlıdır; her albüm ölçüsünde çalışır (Karar 09 §10.2).
/// Elle tasarlanmış temel şablonlar, geometrik şablonlar (<see cref="GeometricTemplates"/>) ve
/// <see cref="CollageGenerator"/> ile 1–12 fotoğraflık yüzlerce düzen.
/// </summary>
public static class BuiltInTemplates
{
    private const double Third = 100.0 / 3;

    /// <summary>Elle tasarlanmış temel şablonlar.</summary>
    public static IReadOnlyList<TemplateDefinition> Handcrafted { get; } =
    [
        Create("single-1", "Tek Fotoğraf", TemplateCategory.Single, 8, 0, [S("s1", 0, 0, 100, 100)]),
        Create("fullbleed-1", "Kenardan Kenara", TemplateCategory.FullBleed, 0, 0, [S("s1", 0, 0, 100, 100)]),
        Create("minimal-1", "Sade Merkez", TemplateCategory.Minimal, 0, 0, [S("s1", 20, 20, 60, 60)]),
        Create("grid-2-side", "İkili Yan Yana", TemplateCategory.Grid, 6, 2,
            [S("s1", 0, 0, 50, 100, portrait: true), S("s2", 50, 0, 50, 100, portrait: true)]),
        Create("grid-2-stack", "İkili Alt Alta", TemplateCategory.Grid, 6, 2,
            [S("s1", 0, 0, 100, 50, landscape: true), S("s2", 0, 50, 100, 50, landscape: true)]),
        Create("featured-1plus2", "Öne Çıkan + İki Küçük", TemplateCategory.Featured, 5, 2,
            [S("slot-main", 0, 0, 100, 65), S("slot-small-1", 0, 65, 50, 35, priority: 2), S("slot-small-2", 50, 65, 50, 35, priority: 2)]),
        Create("collage-3", "Üçlü Kolaj", TemplateCategory.Collage, 5, 2,
            [S("s1", 0, 0, 60, 100, portrait: true), S("s2", 60, 0, 40, 50, priority: 2), S("s3", 60, 50, 40, 50, priority: 2)]),
        Create("grid-4", "4'lü Izgara", TemplateCategory.Grid, 5, 2,
            [S("s1", 0, 0, 50, 50), S("s2", 50, 0, 50, 50), S("s3", 0, 50, 50, 50), S("s4", 50, 50, 50, 50)]),
        Create("featured-1plus3", "Öne Çıkan + Üç Küçük", TemplateCategory.Featured, 5, 2,
            [
                S("slot-main", 0, 0, 100, 60),
                S("s2", 0, 60, Third, 40, priority: 2),
                S("s3", Third, 60, Third, 40, priority: 2),
                S("s4", 2 * Third, 60, Third, 40, priority: 2),
            ]),
        Create("collage-5", "Beşli Kolaj", TemplateCategory.Collage, 5, 2,
            [
                S("s1", 0, 0, 50, 55),
                S("s2", 50, 0, 50, 55),
                S("s3", 0, 55, Third, 45, priority: 2),
                S("s4", Third, 55, Third, 45, priority: 2),
                S("s5", 2 * Third, 55, Third, 45, priority: 2),
            ]),
        Create("grid-6", "6'lı Izgara", TemplateCategory.Grid, 5, 2, Grid(3, 2)),
        Create("grid-9", "9'lu Izgara", TemplateCategory.Grid, 5, 1.5, Grid(3, 3)),
    ];

    /// <summary>
    /// Tüm şablonlar: elle tasarlananlar + geometrikler + kolaj üreticisinin düzenleri (aynı geometrideki kopyalar hariç).
    /// Not: <see cref="Handcrafted"/>'tan sonra tanımlanmalı (statik başlatma sırası).
    /// </summary>
    public static IReadOnlyList<TemplateDefinition> All { get; } = Combine();

    public static TemplateDefinition? Find(string id) => All.FirstOrDefault(t => t.Id == id);

    private static List<TemplateDefinition> Combine()
    {
        var handcrafted = Handcrafted.Select(CollageGenerator.Geometry).ToHashSet(StringComparer.Ordinal);
        return [.. Handcrafted, .. GeometricTemplates.All, .. CollageGenerator.Generate().Where(t => !handcrafted.Contains(CollageGenerator.Geometry(t)))];
    }

    private static TemplateDefinition Create(string id, string name, TemplateCategory category, double margin, double gutter, TemplateSlot[] slots) => new()
    {
        Id = id,
        Name = name,
        Category = category,
        PhotoCount = new PhotoCountRange(slots.Length, slots.Length),
        Slots = slots,
        OuterMarginPercent = margin,
        GutterPercent = gutter,
        Tags = [$"{slots.Length}-foto"],
    };

    private static TemplateSlot S(string id, double x, double y, double w, double h, int priority = 1, bool portrait = false, bool landscape = false) => new()
    {
        Id = id,
        Rect = new PercentRect(x, y, Math.Min(w, 100 - x), Math.Min(h, 100 - y)),
        Priority = priority,
        RecommendedOrientations = portrait ? [PageOrientation.Portrait] : landscape ? [PageOrientation.Landscape] : [],
    };

    private static TemplateSlot[] Grid(int columns, int rows)
    {
        var slots = new List<TemplateSlot>(columns * rows);
        var w = 100.0 / columns;
        var h = 100.0 / rows;
        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < columns; c++)
            {
                slots.Add(S($"s{slots.Count + 1}", c * w, r * h, w, h));
            }
        }

        return [.. slots];
    }
}
