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

    public double CornerRadiusMm { get; init => field = Guard.NonNegative(value, nameof(CornerRadiusMm)); }
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

/// <summary>Şablon yuvasının belirli bir sayfa için milimetreye çözülmüş hali.</summary>
public sealed record ResolvedSlot(string SlotId, RectMm Bounds);

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
        return [.. Slots.Select(s => new ResolvedSlot(s.Id, ApplyGutter(s.Rect, s.Rect.Resolve(inner), half)))];
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
