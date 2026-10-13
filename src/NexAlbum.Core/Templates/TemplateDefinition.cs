using NexAlbum.Core.Common;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Projects;

namespace NexAlbum.Core.Templates;

/// <summary>Şablondaki fotoğraf yuvası; yüzde cinsinden (Karar 10 §3.3).</summary>
public sealed record TemplateSlot
{
    public required string Id { get; init => field = Guard.NotBlank(value, nameof(Id)); }

    public required PercentRect Rect { get; init; }

    /// <summary>Sığdırma modu. "fill" fotoğrafı esnetir; otomatik yerleşim onu kullanmaz (Karar 10 §9.6).</summary>
    public CropMode CropMode { get; init; } = CropMode.Cover;

    /// <summary>1 = en önemli yuva; öncelikli fotoğraflar önce buraya atanır.</summary>
    public int Priority { get; init => field = Guard.Positive(value, nameof(Priority)); } = 1;

    /// <summary>Bu yuvaya önerilen fotoğraf yönleri; boşsa yuvanın kendi biçimi esas alınır.</summary>
    public IReadOnlyList<PageOrientation> RecommendedOrientations { get; init => field = value ?? throw new ArgumentNullException(nameof(RecommendedOrientations)); } = [];

    public double? MinAspectRatio { get; init => field = Guard.PositiveOrNull(value, nameof(MinAspectRatio)); }

    public double? MaxAspectRatio { get; init => field = Guard.PositiveOrNull(value, nameof(MaxAspectRatio)); }

    public SlotShape Shape { get; init; } = SlotShape.Rectangle;

    /// <summary>
    /// Çokgen yuva (Karar 10 Rev. 1): <see cref="Rect"/> içinde normalize dışbükey köşeler. Verilirse biçim
    /// <see cref="SlotShape.Polygon"/> olur; komşu yuvalarla boşluk çapraz kenarlarda da eşit bırakılır.
    /// </summary>
    public IReadOnlyList<UnitPoint>? Polygon
    {
        get;
        init => field = value is null ? null : Geometry.Polygon.IsConvex(value)
            ? [.. value]
            : throw new ArgumentException("Şablon çokgeni dışbükey olmalı ve en az üç köşe içermeli.", nameof(Polygon));
    }

    public double CornerRadiusMm { get; init => field = Guard.NonNegative(value, nameof(CornerRadiusMm)); }

    /// <summary>Etkin biçim: çokgen verilmişse <see cref="SlotShape.Polygon"/>.</summary>
    public SlotShape EffectiveShape => Polygon is null ? Shape : SlotShape.Polygon;
}

/// <summary>Şablondaki metin alanı; yüzde cinsinden.</summary>
public sealed record TemplateTextField
{
    public required string Id { get; init => field = Guard.NotBlank(value, nameof(Id)); }

    public required PercentRect Rect { get; init; }

    public string? Placeholder { get; init; }
}

/// <summary>Şablonun kabul ettiği fotoğraf sayısı aralığı.</summary>
public sealed record PhotoCountRange
{
    public PhotoCountRange(int min, int max)
    {
        Min = Guard.InRange(min, 0, int.MaxValue, nameof(min));
        Max = Guard.InRange(max, min, int.MaxValue, nameof(max));
    }

    public int Min { get; }

    public int Max { get; }

    public bool Accepts(int count) => count >= Min && count <= Max;
}

public sealed record TemplateRules(bool PreserveAspectRatio = true, bool AllowUserEdit = true);

/// <summary>Şablon yuvasının belirli bir sayfa için milimetreye çözülmüş hali; çokgen yuvada köşeler <paramref name="Bounds"/> içinde normalize.</summary>
public sealed record ResolvedSlot(string SlotId, RectMm Bounds, IReadOnlyList<UnitPoint>? Polygon = null);

/// <summary>
/// Sayfa şablonu (Karar 09 §10, Karar 10 §3). Ölçüler yüzde tabanlıdır; her sayfa ölçüsüne ölçeklenir.
/// </summary>
public sealed record TemplateDefinition
{
    public required string Id { get; init => field = Guard.NotBlank(value, nameof(Id)); }

    public required string Name { get; init => field = Guard.NotBlank(value, nameof(Name)); }

    public string? Description { get; init; }

    public TemplateCategory Category { get; init; } = TemplateCategory.Custom;

    public IReadOnlyList<string> Tags { get; init => field = value ?? throw new ArgumentNullException(nameof(Tags)); } = [];

    public TemplateOrientation Orientation { get; init; } = TemplateOrientation.Any;

    public required PhotoCountRange PhotoCount { get; init => field = value ?? throw new ArgumentNullException(nameof(PhotoCount)); }

    public IReadOnlyList<TemplateSlot> Slots { get; init => field = ValidateSlots(value); } = [];

    public IReadOnlyList<TemplateTextField> TextFields { get; init => field = value ?? throw new ArgumentNullException(nameof(TextFields)); } = [];

    public BackgroundFill? Background { get; init; }

    /// <summary>Yuvalar arası boşluk; alanın kısa kenarının yüzdesi (Karar 10 §3.5).</summary>
    public double GutterPercent { get; init => field = Guard.InRange(value, 0, 50, nameof(GutterPercent)); }

    /// <summary>Sayfa kenarı boşluğu; alanın kısa kenarının yüzdesi (Karar 10 §3.5).</summary>
    public double OuterMarginPercent { get; init => field = Guard.InRange(value, 0, 45, nameof(OuterMarginPercent)); }

    public TemplateRules Rules { get; init => field = value ?? throw new ArgumentNullException(nameof(Rules)); } = new();

    /// <summary>Önizleme görselinin göreceli yolu.</summary>
    public string? Preview { get; init; }

    public string Author { get; init => field = Guard.NotBlank(value, nameof(Author)); } = "builtin";

    public string Version { get; init => field = Guard.NotBlank(value, nameof(Version)); } = "1.0.0";

    /// <summary>Şablon verilen sayfa yönüne uygun mu?</summary>
    public bool Fits(PageOrientation orientation) => Orientation switch
    {
        TemplateOrientation.Any => true,
        TemplateOrientation.Portrait => orientation == PageOrientation.Portrait,
        TemplateOrientation.Landscape => orientation == PageOrientation.Landscape,
        TemplateOrientation.Square => orientation == PageOrientation.Square,
        _ => false,
    };

    /// <summary>
    /// Yuvaları verilen alan içinde milimetreye çözer. Önce kenar boşluğu uygulanır; sonra komşu yuvalar
    /// arasına boşluk (gutter) bırakılır. Alan kenarına değen yuva kenarları daraltılmaz.
    /// </summary>
    /// <param name="area">Yerleşim alanı.</param>
    /// <param name="gapPercent">Yuvalar arası boşluk; null ise şablonun kendi değeri.</param>
    /// <param name="marginPercent">Kenar payı; null ise şablonun kendi değeri. Kenarsız (fullBleed) şablonlarda yok sayılır.</param>
    public IReadOnlyList<ResolvedSlot> Resolve(RectMm area, double? gapPercent = null, double? marginPercent = null)
    {
        var unit = Math.Min(area.Width, area.Height) / 100;
        var margin = Category == TemplateCategory.FullBleed ? OuterMarginPercent : marginPercent ?? OuterMarginPercent;
        var gap = Slots.Count > 1 ? gapPercent ?? GutterPercent : GutterPercent;
        var inner = area.Deflate(EdgesMm.Uniform(margin * unit));
        var half = gap * unit / 2;
        var edges = Slots.Any(s => s.Polygon is not null) ? Slots.Select(PercentEdges).ToList() : null;
        return [.. Slots.Select((s, index) => s.Polygon is { } polygon
            ? ResolvePolygon(s.Id, polygon, s.Rect.Resolve(inner), half, Shared(edges!, index))
            : new ResolvedSlot(s.Id, ApplyGutter(s.Rect, s.Rect.Resolve(inner), half)))];
    }

    /// <summary>
    /// Çokgen yuva: başka bir yuvayla paylaşılan her kenar (çaprazlar dahil) boşluğun yarısı kadar içe ötelenir; sayfa
    /// kenarına ya da boşluğa bakan kenarlar yerinde kalır. Böylece karşılıklı iki üçgenin hipotenüsleri arasındaki boşluk
    /// düz kenarlardakiyle aynıdır, tek başına duran kemer ya da elmas ise küçülmez.
    /// </summary>
    private static ResolvedSlot ResolvePolygon(string id, IReadOnlyList<UnitPoint> polygon, RectMm rect, double half, bool[] shared)
    {
        var points = Geometry.Polygon.Map(polygon, rect);
        if (half > 0 && shared.Any(x => x))
        {
            var inset = Geometry.Polygon.Offset(points, i => i < shared.Length && shared[i] ? half : 0);
            if (inset.Count >= 3)
            {
                points = inset;
            }
        }

        var bounds = Geometry.Polygon.Bounds(points);
        return new ResolvedSlot(id, bounds, Geometry.Polygon.Normalize(points, bounds));
    }

    /// <summary>Yuvanın kenarları şablon yüzdesiyle (eksen ölçeklemesi doğrudaşlığı bozmaz, bu yüzden mm gerekmez).</summary>
    private static List<(PointMm A, PointMm B)> PercentEdges(TemplateSlot slot)
    {
        var r = slot.Rect;
        var points = slot.Polygon is { } polygon
            ? Geometry.Polygon.Map(polygon, new RectMm(r.X, r.Y, r.Width, r.Height))
            : [new(r.X, r.Y), new(r.X + r.Width, r.Y), new(r.X + r.Width, r.Y + r.Height), new(r.X, r.Y + r.Height)];
        return [.. points.Select((p, i) => (p, points[(i + 1) % points.Count]))];
    }

    /// <summary>Yuvanın her kenarı başka bir yuvanın kenarıyla aynı doğru üzerinde örtüşüyor mu?</summary>
    private static bool[] Shared(List<List<(PointMm A, PointMm B)>> edges, int index) =>
        [.. edges[index].Select(edge => edges.Where((_, other) => other != index).Any(list => list.Any(o => Overlaps(edge, o))))];

    private static bool Overlaps((PointMm A, PointMm B) e, (PointMm A, PointMm B) o)
    {
        const double tolerance = 0.05;
        var dx = e.B.X - e.A.X;
        var dy = e.B.Y - e.A.Y;
        var length = Math.Sqrt((dx * dx) + (dy * dy));
        if (length < tolerance)
        {
            return false;
        }

        // o'nun iki ucu da e'nin doğrusu üzerinde mi?
        double Distance(PointMm p) => Math.Abs(((p.X - e.A.X) * dy) - ((p.Y - e.A.Y) * dx)) / length;
        if (Distance(o.A) > tolerance || Distance(o.B) > tolerance)
        {
            return false;
        }

        // Doğru üzerindeki izdüşümler pozitif uzunlukla örtüşmeli.
        double Along(PointMm p) => (((p.X - e.A.X) * dx) + ((p.Y - e.A.Y) * dy)) / length;
        var lo = Math.Max(0, Math.Min(Along(o.A), Along(o.B)));
        var hi = Math.Min(length, Math.Max(Along(o.A), Along(o.B)));
        return hi - lo > tolerance;
    }

    private static RectMm ApplyGutter(PercentRect percent, RectMm rect, double half)
    {
        if (half <= 0)
        {
            return rect;
        }

        var left = percent.X > Length.Tolerance ? half : 0;
        var top = percent.Y > Length.Tolerance ? half : 0;
        var right = percent.X + percent.Width < 100 - Length.Tolerance ? half : 0;
        var bottom = percent.Y + percent.Height < 100 - Length.Tolerance ? half : 0;
        return rect.Deflate(new EdgesMm(top, right, bottom, left));
    }

    private static IReadOnlyList<TemplateSlot> ValidateSlots(IReadOnlyList<TemplateSlot>? slots)
    {
        var list = Guard.NotNullItems(slots, nameof(Slots));
        if (list.Select(s => s.Id).Distinct(StringComparer.Ordinal).Count() != list.Count)
        {
            throw new ArgumentException("Şablonda yinelenen yuva kimliği var.", nameof(Slots));
        }

        return list;
    }
}
