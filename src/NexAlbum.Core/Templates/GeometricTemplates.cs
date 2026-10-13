using NexAlbum.Core.Geometry;

namespace NexAlbum.Core.Templates;

/// <summary>
/// Geometrik şablonlar (Karar 10 Rev. 1): üçgen, çapraz kesim, elmas, eğik şerit, yelpaze, petek ve kemer yuvaları.
/// Köşeler sayfa yüzdesiyle (0–100) yazılır; yuvanın dikdörtgeni köşelerin sınır kutusudur. Komşu kenarlar arasındaki
/// boşluk çapraz kenarlarda da eşittir (<see cref="TemplateDefinition.Resolve"/>). Kimlikler kalıcıdır.
/// </summary>
public static class GeometricTemplates
{
    private const double Third = 100.0 / 3;

    /// <summary>Düz tepeli altıgenin yüksekliği / genişliği.</summary>
    private static readonly double HexRatio = Math.Sqrt(3) / 2;

    public static IReadOnlyList<TemplateDefinition> All { get; } = Build();

    private static List<TemplateDefinition> Build() =>
    [
        // ── Çapraz kesimler ───────────────────────────────────────────────
        T("geo-diagonal-2", "Çapraz İkili", "Sayfa köşeden köşeye ikiye bölünür; iki fotoğraf hipotenüsleriyle karşılaşır.",
            P("s1", 0, 0, 100, 0, 0, 100),
            P("s2", 100, 0, 100, 100, 0, 100)),
        T("geo-diagonal-2r", "Çapraz İkili (ters)", "Diğer köşegen boyunca iki üçgen.",
            P("s1", 0, 0, 100, 0, 100, 100),
            P("s2", 0, 0, 100, 100, 0, 100)),
        T("geo-triangles-3", "Karşılıklı Üçgenler + Üçlü", "Hipotenüsleri karşılıklı iki üçgen ve yanında üç dikdörtgen.",
            P("s1", 0, 0, 60, 0, 0, 100),
            P("s2", 60, 0, 60, 100, 0, 100),
            R("s3", 60, 0, 40, Third, 2),
            R("s4", 60, Third, 40, Third, 2),
            R("s5", 60, 2 * Third, 40, Third, 2)),
        T("geo-triangles-3r", "Üçlü + Karşılıklı Üçgenler", "Solda üç dikdörtgen, sağda hipotenüsleri karşılıklı iki üçgen.",
            R("s1", 0, 0, 40, Third, 2),
            R("s2", 0, Third, 40, Third, 2),
            R("s3", 0, 2 * Third, 40, Third, 2),
            P("s4", 40, 0, 100, 0, 40, 100),
            P("s5", 100, 0, 100, 100, 40, 100)),
        T("geo-triangles-2", "Karşılıklı Üçgenler + İkili", "Çapraz bölünmüş kare ve yanında iki dikdörtgen.",
            P("s1", 0, 0, 55, 0, 0, 100),
            P("s2", 55, 0, 55, 100, 0, 100),
            R("s3", 55, 0, 45, 50, 2),
            R("s4", 55, 50, 45, 50, 2)),
        T("geo-corner-cut", "Köşe Kesimli İkili", "Büyük fotoğrafın sağ üst köşesi çapraz kesilir; köşede ikinci fotoğraf.",
            P("s1", 0, 0, 55, 0, 100, 45, 100, 100, 0, 100),
            P("s2", 55, 0, 100, 0, 100, 45, 2)),
        T("geo-cut-featured", "Çapraz Öne Çıkan", "Ortada büyük fotoğraf; iki karşı köşede çapraz kesilmiş üçgenler.",
            P("s1", 0, 0, 65, 0, 100, 35, 100, 100, 35, 100, 0, 65),
            P("s2", 65, 0, 100, 0, 100, 35, 2),
            P("s3", 0, 65, 35, 100, 0, 100, 2)),
        T("geo-band-3", "Diyagonal Bant", "Köşegen boyunca uzanan bant ve iki köşe üçgeni.",
            P("s1", 0, 0, 70, 0, 0, 70, 2),
            P("s2", 70, 0, 100, 0, 100, 30, 30, 100, 0, 100, 0, 70),
            P("s3", 100, 30, 100, 100, 30, 100, 2)),
        T("geo-x-4", "Dörtlü Kesişim", "Sayfa iki köşegenle dört üçgene ayrılır.",
            P("s1", 0, 0, 100, 0, 50, 50),
            P("s2", 100, 0, 100, 100, 50, 50),
            P("s3", 100, 100, 0, 100, 50, 50),
            P("s4", 0, 100, 0, 0, 50, 50)),
        T("geo-triangle-mosaic", "Üçgen Mozaik", "Ters ve düz üçgenlerden oluşan beşli mozaik.",
            P("s1", 0, 0, 50, 0, 25, 100),
            P("s2", 50, 0, 100, 0, 75, 100),
            P("s3", 50, 0, 75, 100, 25, 100),
            P("s4", 0, 0, 25, 100, 0, 100, 2),
            P("s5", 100, 0, 100, 100, 75, 100, 2)),

        // ── Elmas ve sekizgen ────────────────────────────────────────────
        T("geo-diamond-5", "Elmas Merkez", "Ortada elmas; dört köşede üçgen fotoğraflar.",
            P("s1", 50, 0, 100, 50, 50, 100, 0, 50),
            P("s2", 0, 0, 50, 0, 0, 50, 2),
            P("s3", 50, 0, 100, 0, 100, 50, 2),
            P("s4", 100, 50, 100, 100, 50, 100, 2),
            P("s5", 0, 50, 50, 100, 0, 100, 2)),
        T("geo-diamond-1", "Tek Elmas", "Sade zeminde tek elmas fotoğraf.", margin: 6,
            slots: [P("s1", 50, 0, 100, 50, 50, 100, 0, 50)]),
        T("geo-diamonds-3", "Üç Elmas", "Yan yana üç elmas; sade ve ritimli.", margin: 6,
            slots:
            [
                P("s1", 16, 22, 30, 50, 16, 78, 2, 50),
                P("s2", 50, 22, 64, 50, 50, 78, 36, 50),
                P("s3", 84, 22, 98, 50, 84, 78, 70, 50),
            ]),
        T("geo-octagon-5", "Sekizgen Merkez", "Köşeleri kesik merkez fotoğraf; kesiklerde dört küçük üçgen.",
            P("s1", 25, 0, 75, 0, 100, 25, 100, 75, 75, 100, 25, 100, 0, 75, 0, 25),
            P("s2", 0, 0, 25, 0, 0, 25, 2),
            P("s3", 75, 0, 100, 0, 100, 25, 2),
            P("s4", 100, 75, 100, 100, 75, 100, 2),
            P("s5", 0, 75, 25, 100, 0, 100, 2)),

        // ── Eğik şeritler ve yelpaze ─────────────────────────────────────
        T("geo-slant-2", "Eğik İkili", "Sayfa eğik bir çizgiyle ikiye ayrılır.",
            Strips([0, 58, 100], [0, 42, 100])),
        T("geo-slant-3", "Eğik Üçlü", "Üç eğik şerit; hareket ve akış hissi.",
            Strips([0, 38, 70, 100], [0, 30, 62, 100])),
        T("geo-slant-4", "Eğik Dörtlü", "Dört eğik şerit.",
            Strips([0, 28, 53, 78, 100], [0, 22, 47, 72, 100])),
        T("geo-slant-h-2", "Eğik Yatay İkili", "Yatayda eğik kesim: üstte ve altta birer fotoğraf.",
            P("s1", 0, 0, 100, 0, 100, 42, 0, 58),
            P("s2", 0, 58, 100, 42, 100, 100, 0, 100)),
        T("geo-slant-h-3", "Eğik Yatay Üçlü", "Üç yatay eğik bant.",
            P("s1", 0, 0, 100, 0, 100, 26, 0, 40),
            P("s2", 0, 40, 100, 26, 100, 60, 0, 74),
            P("s3", 0, 74, 100, 60, 100, 100, 0, 100)),
        T("geo-slant-featured", "Eğik Kesim + Üçlü", "Solda büyük eğik fotoğraf, sağda eğik kenarlı üçlü.",
            P("s1", 0, 0, 62, 0, 48, 100, 0, 100),
            P("s2", 62, 0, 100, 0, 100, Third, 62 - (14.0 / 3), Third, 2),
            P("s3", 62 - (14.0 / 3), Third, 100, Third, 100, 2 * Third, 62 - (28.0 / 3), 2 * Third, 2),
            P("s4", 62 - (28.0 / 3), 2 * Third, 100, 2 * Third, 100, 100, 48, 100, 2)),
        T("geo-fan-3", "Yelpaze Üçlü", "Ortadaki fotoğraf aşağı doğru genişler; iki yanda yamuklar.",
            P("s1", 0, 0, 40, 0, 25, 100, 0, 100, 2),
            P("s2", 40, 0, 60, 0, 75, 100, 25, 100),
            P("s3", 60, 0, 100, 0, 100, 100, 75, 100, 2)),

        // ── Petek (kare albümler) ────────────────────────────────────────
        T("geo-hex-3", "Petek Üçlü", "Bal peteği gibi birbirine yaslanan üç altıgen.", margin: 4, orientation: TemplateOrientation.Square,
            slots: [Hex("s1", 35, 32.68, 40), Hex("s2", 65, 50, 40), Hex("s3", 35, 67.32, 40)]),
        T("geo-hex-4", "Petek Dörtlü", "Dört altıgenden petek.", margin: 4, orientation: TemplateOrientation.Square,
            slots: [Hex("s1", 37.25, 42.64, 34), Hex("s2", 37.25, 72.08, 34), Hex("s3", 62.75, 27.92, 34), Hex("s4", 62.75, 57.36, 34)]),
        T("geo-hex-7", "Petek Yedili", "Merkez altıgen ve çevresinde altı altıgen.", margin: 2, orientation: TemplateOrientation.Square,
            slots:
            [
                Hex("s1", 50, 50, 26),
                Hex("s2", 50, 50 - (26 * HexRatio), 26, 2),
                Hex("s3", 50, 50 + (26 * HexRatio), 26, 2),
                Hex("s4", 30.5, 50 - (13 * HexRatio), 26, 2),
                Hex("s5", 69.5, 50 - (13 * HexRatio), 26, 2),
                Hex("s6", 30.5, 50 + (13 * HexRatio), 26, 2),
                Hex("s7", 69.5, 50 + (13 * HexRatio), 26, 2),
            ]),

        // ── Kemerler ────────────────────────────────────────────────────
        T("geo-arch-1", "Kemer", "Ortada tek kemer; zarif ve sade.", margin: 6,
            slots: [Arch("s1", 22, 0, 56, 100)]),
        T("geo-arch-2", "İkili Kemer", "Yan yana iki kemer.", margin: 6,
            slots: [Arch("s1", 4, 6, 44, 88), Arch("s2", 52, 6, 44, 88)]),
        T("geo-arch-3", "Üçlü Kemer", "Yan yana üç kemer.", margin: 6,
            slots: [Arch("s1", 2, 10, 30, 80), Arch("s2", 35, 10, 30, 80), Arch("s3", 68, 10, 30, 80)]),
        T("geo-arch-featured", "Kemer + İkili", "Solda büyük kemer, sağda iki dikdörtgen.",
            Arch("s1", 0, 0, 52, 100),
            R("s2", 55, 0, 45, 50, 2),
            R("s3", 55, 50, 45, 50, 2)),
    ];

    private static TemplateDefinition T(string id, string name, string description, params TemplateSlot[] slots) =>
        T(id, name, description, 5, TemplateOrientation.Any, slots);

    private static TemplateDefinition T(string id, string name, string description, double margin = 5, TemplateOrientation orientation = TemplateOrientation.Any, TemplateSlot[]? slots = null)
    {
        ArgumentNullException.ThrowIfNull(slots);
        return new TemplateDefinition
        {
            Id = id,
            Name = name,
            Description = description,
            Category = TemplateCategory.Geometric,
            Orientation = orientation,
            PhotoCount = new PhotoCountRange(slots.Length, slots.Length),
            Slots = slots,
            OuterMarginPercent = margin,
            GutterPercent = 2,
            Tags = [$"{slots.Length}-foto", "geometrik"],
        };
    }

    private static TemplateSlot R(string id, double x, double y, double w, double h, int priority = 1) => new()
    {
        Id = id,
        Rect = new PercentRect(x, y, Math.Min(w, 100 - x), Math.Min(h, 100 - y)),
        Priority = priority,
    };

    /// <summary>
    /// Sayfa yüzdesiyle köşeler: (x1, y1, x2, y2, …). Tek sayıda değer verilirse sonuncusu önceliktir.
    /// </summary>
    private static TemplateSlot P(string id, params double[] values)
    {
        var priority = values.Length % 2 == 1 ? (int)values[^1] : 1;
        var points = new List<PointMm>(values.Length / 2);
        for (var i = 0; i + 1 < values.Length; i += 2)
        {
            points.Add(new PointMm(values[i], values[i + 1]));
        }

        return FromPoints(id, points, priority);
    }

    private static TemplateSlot FromPoints(string id, IReadOnlyList<PointMm> points, int priority = 1)
    {
        var bounds = Polygon.Bounds(points);
        var rect = new PercentRect(bounds.X, bounds.Y, Math.Min(bounds.Width, 100 - bounds.X), Math.Min(bounds.Height, 100 - bounds.Y));
        return new TemplateSlot
        {
            Id = id,
            Rect = rect,
            Priority = priority,
            Polygon = Polygon.Normalize(points, bounds),
        };
    }

    /// <summary>Üst ve alt kesişim x'lerinden dikey eğik şeritler.</summary>
    private static TemplateSlot[] Strips(double[] top, double[] bottom)
    {
        var slots = new TemplateSlot[top.Length - 1];
        for (var i = 0; i < slots.Length; i++)
        {
            slots[i] = FromPoints($"s{i + 1}", [new(top[i], 0), new(top[i + 1], 0), new(bottom[i + 1], 100), new(bottom[i], 100)]);
        }

        return slots;
    }

    /// <summary>Düz tepeli altıgen: merkez ve genişlik (sayfa yüzdesi; kare sayfada düzgün altıgen).</summary>
    private static TemplateSlot Hex(string id, double cx, double cy, double width, int priority = 1)
    {
        var h = width * HexRatio / 2;
        var q = width / 4;
        return FromPoints(id, [new(cx - (2 * q), cy), new(cx - q, cy - h), new(cx + q, cy - h), new(cx + (2 * q), cy), new(cx + q, cy + h), new(cx - q, cy + h)], priority);
    }

    /// <summary>Üstü yarım daire kemer (sayfa yüzdesi; yarıçap genişliğin yarısı).</summary>
    private static TemplateSlot Arch(string id, double x, double y, double w, double h, int priority = 1)
    {
        var r = w / 2;
        var points = new List<PointMm> { new(x, y + h), new(x, y + r) };
        const int segments = 32;
        for (var i = 1; i < segments; i++)
        {
            var angle = Math.PI + (Math.PI * i / segments);
            points.Add(new(x + r + (r * Math.Cos(angle)), y + r + (r * Math.Sin(angle))));
        }

        points.Add(new(x + w, y + r));
        points.Add(new(x + w, y + h));
        return FromPoints(id, points, priority);
    }
}
