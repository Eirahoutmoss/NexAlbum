using NexAlbum.Core.Common;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Projects;

namespace NexAlbum.Core.Templates;

/// <summary>Şablondaki fotoğraf yuvası; yüzde cinsinden.</summary>
public sealed record TemplateSlot
{
    public required string Id { get; init => field = Guard.NotBlank(value, nameof(Id)); }

    public required PercentRect Rect { get; init; }
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
/// Sayfa şablonu (Karar 09 §10). Ölçüler yüzde tabanlıdır; her sayfa ölçüsüne ölçeklenir.
/// </summary>
public sealed record TemplateDefinition
{
    public required string Id { get; init => field = Guard.NotBlank(value, nameof(Id)); }

    public required string Name { get; init => field = Guard.NotBlank(value, nameof(Name)); }

    public TemplateCategory Category { get; init; } = TemplateCategory.Custom;

    public IReadOnlyList<string> Tags { get; init => field = value ?? throw new ArgumentNullException(nameof(Tags)); } = [];

    public TemplateOrientation Orientation { get; init; } = TemplateOrientation.Any;

    public required PhotoCountRange PhotoCount { get; init => field = value ?? throw new ArgumentNullException(nameof(PhotoCount)); }

    public IReadOnlyList<TemplateSlot> Slots { get; init => field = ValidateSlots(value); } = [];

    public IReadOnlyList<TemplateTextField> TextFields { get; init => field = value ?? throw new ArgumentNullException(nameof(TextFields)); } = [];

    public BackgroundFill? Background { get; init; }

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

    /// <summary>Yuvaları verilen alan (genellikle sayfa ya da güvenli alan) içinde milimetreye çözer.</summary>
    public IReadOnlyList<ResolvedSlot> Resolve(RectMm area) =>
        [.. Slots.Select(s => new ResolvedSlot(s.Id, s.Rect.Resolve(area)))];

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
